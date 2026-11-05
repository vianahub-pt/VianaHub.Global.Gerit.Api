import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runClientDocumentsSmoke(client, { clientId, now = Date.now } = {}) {
  if (!Number.isSafeInteger(clientId) || clientId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_CLIENT_ID');
  return client.run(async c => {
    const existing = await c.get(`/v1/clients/${clientId}/documents`);
    if (existing.status !== 200 || !Array.isArray(existing.data)) throw new SmokeError('Documents preflight failed');
    if (existing.data.length > 0) throw new SmokeError('Documents already exist; refusing to mutate');
    const suffix = now();
    const payload = { documentTypeId: 1, documentNumber: `SMOKE-${suffix}`, issuingCountryCode: 'PT', issuedAt: '2026-01-01', expiresAt: '2026-01-01', isPrimary: false };
    const created = await c.post(`/v1/clients/${clientId}/documents`, payload);
    if (created.status !== 201 || !Number.isSafeInteger(created.data?.id)) throw new SmokeError('Document creation failed');
    const id = created.data.id;
    c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/clients/${clientId}/documents/${id}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Document cleanup failed');
      if ((await c.get(`/v1/clients/${clientId}/documents/${id}`)).status !== 410) throw new SmokeError('Document cleanup verification failed');
    });
    if ((await c.get(`/v1/clients/${clientId}/documents/${id}`)).status !== 200) throw new SmokeError('Document detail failed');
    if ((await c.put(`/v1/clients/${clientId}/documents/${id}`, { ...payload, documentNumber: `SMOKE-UPDATED-${suffix}` })).status !== 200) throw new SmokeError('Document update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/clients/${clientId}/documents/${id}/${operation}`)).status !== 200) throw new SmokeError('Document lifecycle failed');
    }
    const bulkSuffix = `${suffix}-bulk`;
    const csv = [
      'DocumentTypeId,DocumentNumber,IssuingCountryCode,IssuedAt,ExpiresAt,IsPrimary',
      `1,${bulkSuffix}-1,PT,2026-01-01,2027-01-01,false`,
      `1,${bulkSuffix}-2,PT,2026-01-01,,false`,
    ].join('\n');
    const form = new FormData();
    form.append('file', new Blob([csv], { type: 'text/csv' }), 'documents.csv');
    const bulk = await c.postMultipart(`/v1/clients/${clientId}/documents/bulk-upload`, form);
    if (bulk.status !== 200) throw new SmokeError('Document bulk upload failed');
    const listed = await c.get(`/v1/clients/${clientId}/documents`);
    if (listed.status !== 200 || !Array.isArray(listed.data)) throw new SmokeError('Bulk list verification failed');
    const bulkIds = listed.data.filter(item => [`${bulkSuffix}-1`, `${bulkSuffix}-2`].includes(item.documentNumber)).map(item => item.id);
    if (bulkIds.length !== 2) throw new SmokeError('Bulk records verification failed');
    for (const bulkId of bulkIds) {
      c.deferCleanup(async () => {
        const deleted = await c.delete(`/v1/clients/${clientId}/documents/${bulkId}`);
        if (![200, 410].includes(deleted.status)) throw new SmokeError('Bulk document cleanup failed');
        if ((await c.get(`/v1/clients/${clientId}/documents/${bulkId}`)).status !== 410) throw new SmokeError('Bulk cleanup verification failed');
      });
    }
    if ((await c.delete(`/v1/clients/${clientId}/documents/${id}`)).status !== 200) throw new SmokeError('Document delete failed');
    if ((await c.get(`/v1/clients/${clientId}/documents/${id}`)).status !== 410) throw new SmokeError('Document delete verification failed');
    return { id };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try { await runClientDocumentsSmoke(await createAuthenticatedSmokeClient(), { clientId: Number(process.env.GERIT_SMOKE_CLIENT_ID) }); console.log('ClientDocuments local smoke: PASS'); }
  catch (error) { console.error(error instanceof SmokeError ? error.message : 'ClientDocuments local smoke failed'); process.exitCode = 1; }
}
