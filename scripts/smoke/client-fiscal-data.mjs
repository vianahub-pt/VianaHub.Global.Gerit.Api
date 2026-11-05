import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runClientFiscalDataSmoke(client, { clientId, now = Date.now } = {}) {
  if (!Number.isSafeInteger(clientId) || clientId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_CLIENT_ID');
  return client.run(async c => {
    const existing = await c.get(`/v1/clients/${clientId}/fiscal-data`);
    if (![200, 410].includes(existing.status)) throw new SmokeError('Fiscal data preflight failed');
    if (existing.status === 200 && Array.isArray(existing.data) && existing.data.length > 0) throw new SmokeError('Fiscal data already exists; refusing to mutate');
    const payload = { taxNumber: `SMOKE${now()}`, vatNumber: null, fiscalCountry: 'PT', isVatRegistered: false, iban: null, fiscalEmail: 'smoke@example.invalid' };
    const created = await c.post(`/v1/clients/${clientId}/fiscal-data`, payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id)) throw new SmokeError('Fiscal data creation failed');
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/clients/${clientId}/fiscal-data/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Fiscal data cleanup failed');
      if ((await c.get(`/v1/clients/${clientId}/fiscal-data/${id}`)).status !== 410) throw new SmokeError('Fiscal data cleanup verification failed');
    });
    const detail = await c.get(`/v1/clients/${clientId}/fiscal-data/${id}`);
    if (detail.status !== 200 || detail.data?.clientId !== clientId || detail.data?.taxNumber !== payload.taxNumber) throw new SmokeError('Fiscal data detail failed');
    const updated = await c.put(`/v1/clients/${clientId}/fiscal-data/${id}`, { ...payload, fiscalEmail: 'updated-smoke@example.invalid' });
    if (updated.status !== 200) throw new SmokeError('Fiscal data update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/clients/${clientId}/fiscal-data/${id}/${operation}`)).status !== 200) throw new SmokeError('Fiscal data lifecycle failed');
    }
    return { id };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try { await runClientFiscalDataSmoke(await createAuthenticatedSmokeClient(), { clientId: Number(process.env.GERIT_SMOKE_CLIENT_ID) }); console.log('ClientFiscalData production smoke: PASS'); }
  catch (error) { console.error(error instanceof SmokeError ? error.message : 'ClientFiscalData production smoke failed'); process.exitCode = 1; }
}
