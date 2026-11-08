import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runActionsSmoke(client, { now = Date.now } = {}) {
  return client.run(async c => {
    const suffix = now();
    const payload = { name: `SMOKE_ACTION_${suffix}`, description: 'Controlled Actions smoke' };
    const preflight = await c.get('/v1/actions/paged?PageNumber=1&PageSize=1&SortBy=Id&SortDirection=asc');
    if (preflight.status !== 200) throw new SmokeError('Actions preflight failed');

    const created = await c.post('/v1/actions/', payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id) || created.data.id <= 0) {
      throw new SmokeError('Action creation failed; verify uncertain creation before retry');
    }
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/actions/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Action cleanup failed');
      if ((await c.get(`/v1/actions/${id}`)).status !== 410) throw new SmokeError('Action cleanup verification failed');
      const active = await c.get('/v1/actions/');
      if (active.status !== 200 || active.data?.some?.(item => item.name === payload.name)) {
        throw new SmokeError('Action cleanup residual verification failed');
      }
    });

    const detail = await c.get(`/v1/actions/${id}`);
    if (detail.status !== 200 || detail.data?.name !== payload.name) throw new SmokeError('Action detail failed');
    const page = await c.get('/v1/actions/paged?PageNumber=1&PageSize=100&SortBy=Name&SortDirection=asc');
    if (page.status !== 200 || !page.data?.items?.some(item => item.id === id)) throw new SmokeError('Action pagination failed');
    if ((await c.put(`/v1/actions/${id}`, { description: 'Controlled Actions smoke updated' })).status !== 200) throw new SmokeError('Action update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/actions/${id}/${operation}`)).status !== 200) throw new SmokeError('Action lifecycle failed');
    }
    return { id, name: payload.name };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const client = await createAuthenticatedSmokeClient();
    const result = await runActionsSmoke(client);
    console.log(JSON.stringify({ temporaryActionId: result.id, cleanup: 'active-residual-zero' }));
    console.log('Actions production smoke: PASS');
    console.log('Warning: public API cannot prove removal of the soft-deleted row; production closure requires database-level residual evidence.');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Actions production smoke failed');
    process.exitCode = 1;
  }
}
