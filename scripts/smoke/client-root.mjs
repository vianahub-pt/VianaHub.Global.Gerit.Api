import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runClientRootSmoke(client, {
  partyTypeId, acquisitionSourceTypeId, statusDomainId, statusDefinitionId, now = Date.now,
} = {}) {
  const ids = { partyTypeId, acquisitionSourceTypeId, statusDomainId, statusDefinitionId };
  if (Object.values(ids).some(value => !Number.isSafeInteger(value) || value <= 0)) {
    throw new SmokeError('Invalid Client smoke dependency id');
  }
  return client.run(async c => {
    for (const path of ['/v1/party-types', '/v1/acquisition-source-types', '/v1/status-domains', '/v1/status-definitions']) {
      if ((await c.get(path)).status !== 200) throw new SmokeError('Client dependency preflight failed');
    }
    const suffix = now();
    const payload = {
      partyTypeId, acquisitionSourceTypeId, statusDomainId, statusDefinitionId,
      name: `GERIT PRODUCTION SMOKE CLIENT ${suffix}`,
      email: `gerit.client.smoke.${suffix}@example.invalid`,
      note: 'Temporary production smoke client', isCellPhoneWhatsapp: false,
    };
    const created = await c.post('/v1/clients', payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id)) throw new SmokeError('Client creation failed');
    const id = created.data.id;
    let deleted = false;
    c.deferCleanup(async () => {
      if (!deleted) {
        const response = await c.delete(`/v1/clients/${id}`);
        if (![200, 410].includes(response.status)) throw new SmokeError('Client cleanup failed');
      }
      if ((await c.get(`/v1/clients/${id}`)).status !== 410) throw new SmokeError('Client cleanup verification failed');
    });
    const detail = await c.get(`/v1/clients/${id}`);
    if (detail.status !== 200 || detail.data?.id !== id || detail.data?.name !== payload.name) throw new SmokeError('Client detail failed');
    if ((await c.get('/v1/clients')).status !== 200 || (await c.get('/v1/clients/paged?pageNumber=1&pageSize=10')).status !== 200) throw new SmokeError('Client list failed');
    const bulkName = `GERIT PRODUCTION SMOKE BULK ${suffix}`;
    const form = new FormData();
    form.append('file', new Blob([`PartyTypeId;AcquisitionSourceTypeId;StatusDomainId;StatusDefinitionId;Name;Email\n${partyTypeId};${acquisitionSourceTypeId};${statusDomainId};${statusDefinitionId};${bulkName};bulk.${suffix}@example.invalid\n`], { type: 'text/csv' }), 'clients.csv');
    if ((await c.postMultipart('/v1/clients/bulk-upload', form)).status !== 200) throw new SmokeError('Client bulk creation failed');
    c.deferCleanup(async () => {
      const listed = await c.get(`/v1/clients/paged?search=${encodeURIComponent(bulkName)}&pageNumber=1&pageSize=10`);
      for (const item of listed.data?.items ?? []) {
        const response = await c.delete(`/v1/clients/${item.id}`);
        if (![200, 410].includes(response.status)) throw new SmokeError('Client bulk cleanup failed');
      }
      const residual = await c.get(`/v1/clients/paged?search=${encodeURIComponent(bulkName)}&pageNumber=1&pageSize=10`);
      if ((residual.data?.items ?? []).length !== 0) throw new SmokeError('Client bulk residual detected');
    });
    const update = { ...payload, name: `${payload.name} updated` };
    if ((await c.put(`/v1/clients/${id}`, update)).status !== 200) throw new SmokeError('Client update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/clients/${id}/${operation}`)).status !== 200) throw new SmokeError('Client lifecycle failed');
    }
    const deletedResponse = await c.delete(`/v1/clients/${id}`);
    if (deletedResponse.status !== 200) throw new SmokeError('Client delete failed');
    deleted = true;
    if ((await c.get(`/v1/clients/${id}`)).status !== 410) throw new SmokeError('Client delete verification failed');
    return { id };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const client = await createAuthenticatedSmokeClient();
    await runClientRootSmoke(client, {
      partyTypeId: Number(process.env.GERIT_SMOKE_PARTY_TYPE_ID),
      acquisitionSourceTypeId: Number(process.env.GERIT_SMOKE_ACQUISITION_SOURCE_TYPE_ID),
      statusDomainId: Number(process.env.GERIT_SMOKE_STATUS_DOMAIN_ID),
      statusDefinitionId: Number(process.env.GERIT_SMOKE_STATUS_DEFINITION_ID),
    });
    console.log('Client root production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Client root production smoke failed');
    process.exitCode = 1;
  }
}
