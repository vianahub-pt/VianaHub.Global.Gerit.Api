import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runClientAddressSmoke(client, { clientId, addressTypeId, now = Date.now } = {}) {
  if (!Number.isSafeInteger(clientId) || clientId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_CLIENT_ID');
  if (!Number.isSafeInteger(addressTypeId) || addressTypeId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_ADDRESS_TYPE_ID');
  return client.run(async c => {
    const suffix = now();
    const payload = {
      addressTypeId, countryCode: 'PT', street: `SMOKE_STREET_${suffix}`,
      streetNumber: '1', complement: 'Smoke fixture', neighborhood: `SMOKE_NEIGHBORHOOD_${suffix}`,
      city: 'Smoke City', district: 'Smoke District', postalCode: '0000-000',
      latitude: null, longitude: null, note: 'Temporary production smoke address', isPrimary: false,
    };
    const created = await c.post(`/v1/clients/${clientId}/addresses`, payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id)) throw new SmokeError('Address creation failed');
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/clients/${clientId}/addresses/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Address cleanup failed');
      if ((await c.get(`/v1/clients/${clientId}/addresses/${id}`)).status !== 410) throw new SmokeError('Address cleanup verification failed');
    });
    const detail = await c.get(`/v1/clients/${clientId}/addresses/${id}`);
    if (detail.status !== 200 || detail.data?.clientId !== clientId || detail.data?.street !== payload.street) throw new SmokeError('Address detail failed');
    const updated = { ...payload, streetNumber: '2' };
    if ((await c.put(`/v1/clients/${clientId}/addresses/${id}`, updated)).status !== 200) throw new SmokeError('Address update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/clients/${clientId}/addresses/${id}/${operation}`)).status !== 200) throw new SmokeError('Address lifecycle failed');
    }
    return { id };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const clientId = Number(process.env.GERIT_SMOKE_CLIENT_ID);
    const addressTypeId = Number(process.env.GERIT_SMOKE_ADDRESS_TYPE_ID);
    const client = await createAuthenticatedSmokeClient();
    await runClientAddressSmoke(client, { clientId, addressTypeId });
    console.log('ClientAddress production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'ClientAddress production smoke failed');
    process.exitCode = 1;
  }
}
