import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runResourcesSmoke(client, { now = Date.now } = {}) {
  return client.run(async c => {
    const name = `SMOKE_RESOURCE_${now()}`;
    const payload = { name, description: 'Controlled Resources smoke' };
    const preflight = await c.get('/v1/resources/paged?PageNumber=1&PageSize=1&SortBy=Id&SortDirection=asc');
    if (preflight.status !== 200) throw new SmokeError('Resources preflight failed');
    const created = await c.post('/v1/resources/', payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id) || created.data.id <= 0)
      throw new SmokeError('Resource creation failed; verify uncertain creation before retry');
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/resources/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Resource cleanup failed');
      if ((await c.get(`/v1/resources/${id}`)).status !== 410) throw new SmokeError('Resource cleanup detail failed');
      const all = await c.get('/v1/resources/');
      const page = await c.get('/v1/resources/paged?PageNumber=1&PageSize=100&SortBy=Name&SortDirection=asc');
      if (all.status !== 200 || all.data?.some?.(item => item.id === id || item.name === name) ||
          page.status !== 200 || page.data?.items?.some?.(item => item.id === id || item.name === name))
        throw new SmokeError('Resource logical residual verification failed');
    });
    const detail = await c.get(`/v1/resources/${id}`);
    if (detail.status !== 200 || detail.data?.name !== name || detail.data?.description !== payload.description)
      throw new SmokeError('Resource detail failed');
    const page = await c.get('/v1/resources/paged?PageNumber=1&PageSize=100&Search=SMOKE_RESOURCE&SortBy=Name&SortDirection=asc');
    if (page.status !== 200 || !page.data?.items?.some(item => item.id === id)) throw new SmokeError('Resource paging failed');
    if ((await c.put(`/v1/resources/${id}`, { description: 'Controlled Resources smoke updated' })).status !== 200)
      throw new SmokeError('Resource update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/resources/${id}/${operation}`)).status !== 200) throw new SmokeError('Resource lifecycle failed');
    }
    return { id, name };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const result = await runResourcesSmoke(await createAuthenticatedSmokeClient());
    console.log(JSON.stringify({ temporaryResourceId: result.id, logicalResidualZero: true, cleanupPolicy: 'soft-delete' }));
    console.log('Resources production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Resources production smoke failed');
    process.exitCode = 1;
  }
}
