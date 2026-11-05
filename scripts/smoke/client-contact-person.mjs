import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runClientContactPersonSmoke(client, { clientId, now = Date.now } = {}) {
  if (!Number.isSafeInteger(clientId) || clientId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_CLIENT_ID');
  return client.run(async c => {
    const suffix = now();
    const payload = {
      name: `SMOKE_CONTACT_${suffix}`, email: `smoke.contact.${suffix}@example.invalid`,
      phoneNumber: null, cellPhoneNumber: '+351900000000', isWhatsapp: false,
      jobTitle: 'Controlled smoke', department: null, isCellPhoneWhatsapp: true, isPrimary: false,
    };
    const created = await c.post(`/v1/clients/${clientId}/contacts`, payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id) || created.data.id <= 0) {
      throw new SmokeError('Client contact creation failed; verify uncertain creation before retry');
    }
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/clients/${clientId}/contacts/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Client contact cleanup failed');
      if ((await c.get(`/v1/clients/${clientId}/contacts/${id}`)).status !== 410) throw new SmokeError('Client contact cleanup verification failed');
    });
    console.log(JSON.stringify({ temporaryContactId: id, clientId }));
    const detail = await c.get(`/v1/clients/${clientId}/contacts/${id}`);
    if (detail.status !== 200 || detail.data?.clientId !== clientId || detail.data?.email !== payload.email ||
        detail.data?.isPrimary !== false || detail.data?.isCellPhoneWhatsapp !== true) {
      throw new SmokeError('Client contact detail failed');
    }
    const updated = { ...payload, jobTitle: 'Controlled smoke updated' };
    if ((await c.put(`/v1/clients/${clientId}/contacts/${id}`, updated)).status !== 200) throw new SmokeError('Client contact update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/clients/${clientId}/contacts/${id}/${operation}`)).status !== 200) throw new SmokeError('Client contact lifecycle failed');
    }
    return { id };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const raw = process.env.GERIT_SMOKE_CLIENT_ID;
    if (!raw || !/^[1-9][0-9]*$/.test(raw)) throw new SmokeError('Missing or invalid GERIT_SMOKE_CLIENT_ID');
    const client = await createAuthenticatedSmokeClient();
    await runClientContactPersonSmoke(client, { clientId: Number(raw) });
    console.log('ClientContactPerson production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'ClientContactPerson production smoke failed');
    process.exitCode = 1;
  }
}
