import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runRolesSmoke(client, { now = Date.now } = {}) {
  return client.run(async c => {
    const name = `SMOKE_ROLE_${now()}`;
    const description = 'Controlled Roles smoke';
    const preflight = await c.get('/v1/roles/paged?PageNumber=1&PageSize=1&SortBy=Id&SortDirection=asc');
    if (preflight.status !== 200) throw new SmokeError('Roles preflight failed');

    const created = await c.post('/v1/roles/', { name, description });
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id) || created.data.id <= 0) {
      throw new SmokeError('Role creation failed; verify uncertain creation before retry');
    }
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/roles/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Role cleanup failed');
      if ((await c.get(`/v1/roles/${id}`)).status !== 410) throw new SmokeError('Role cleanup verification failed');
      const all = await c.get('/v1/roles/');
      const page = await c.get('/v1/roles/paged?PageNumber=1&PageSize=100&SortBy=Name&SortDirection=asc');
      if (all.status !== 200 || all.data?.some?.(item => item.id === id || item.name === name) ||
          page.status !== 200 || page.data?.items?.some?.(item => item.id === id || item.name === name)) {
        throw new SmokeError('Role logical residual verification failed');
      }
    });

    const detail = await c.get(`/v1/roles/${id}`);
    if (detail.status !== 200 || detail.data?.name !== name || detail.data?.description !== description) {
      throw new SmokeError('Role detail failed');
    }
    const page = await c.get('/v1/roles/paged?PageNumber=1&PageSize=100&SortBy=Name&SortDirection=asc');
    if (page.status !== 200 || !page.data?.items?.some(item => item.id === id)) throw new SmokeError('Role pagination failed');
    if ((await c.put(`/v1/roles/${id}`, { description: `${description} updated` })).status !== 200) throw new SmokeError('Role update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/roles/${id}/${operation}`)).status !== 200) throw new SmokeError('Role lifecycle failed');
    }
    return { id, name };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const result = await runRolesSmoke(await createAuthenticatedSmokeClient());
    console.log(JSON.stringify({ temporaryRoleId: result.id, logicalResidualZero: true, cleanupPolicy: 'soft-delete' }));
    console.log('Roles production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Roles production smoke failed');
    process.exitCode = 1;
  }
}
