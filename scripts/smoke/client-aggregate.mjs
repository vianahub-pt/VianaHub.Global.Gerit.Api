import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

function requireId(name, value) {
  if (!Number.isSafeInteger(value) || value <= 0) throw new SmokeError(`Invalid ${name}`);
}

function expect(status, expected, message) {
  if (status !== expected) throw new SmokeError(`${message} (${status})`);
}

export async function runClientAggregateSmoke(client, {
  clientId,
  partyTypeId,
  acquisitionSourceTypeId,
  statusDomainId,
  statusDefinitionId,
  addressTypeId,
  documentTypeId,
  now = Date.now,
} = {}) {
  for (const [name, value] of Object.entries({ clientId, partyTypeId, acquisitionSourceTypeId, statusDomainId, statusDefinitionId, addressTypeId, documentTypeId })) requireId(name, value);
  return client.run(async c => {
    const suffix = now();
    const root = {
      partyTypeId, acquisitionSourceTypeId, statusDomainId, statusDefinitionId,
      name: `SMOKE_AGGREGATE_${suffix}`, email: `smoke.aggregate.${suffix}@example.invalid`,
      isCellPhoneWhatsapp: false,
    };
    const contact = {
      name: `Smoke Contact ${suffix}`, email: `smoke.aggregate.contact.${suffix}@example.invalid`,
      phoneNumber: null, cellPhoneNumber: '+351900000000', jobTitle: 'Smoke', department: null,
      isWhatsapp: false, isCellPhoneWhatsapp: true, isPrimary: false,
    };
    const address = {
      addressTypeId, countryCode: 'PT', street: `Smoke Street ${suffix}`, streetNumber: '1',
      complement: null, neighborhood: 'Centro', city: 'Lisboa', district: 'Lisboa',
      postalCode: '1000-001', note: 'aggregate smoke', isPrimary: false,
    };
    const fiscal = {
      taxNumber: `SM${String(suffix).slice(-8)}`, vatNumber: 'PTVAT12345', fiscalCountry: 'PT',
      isVatRegistered: true, iban: 'PT50000000000000000000000', fiscalEmail: 'smoke@example.invalid',
    };
    const document = {
      documentTypeId, documentNumber: `SMOKE-${suffix}`, issuingCountryCode: 'PT',
      issuedAt: '2020-01-01', expiresAt: '2030-01-01', isPrimary: false,
    };

    const contactResponse = await c.post(`/v1/clients/${clientId}/contacts`, contact);
    expect(contactResponse.status, 201, 'Contact creation failed');
    const contactId = contactResponse.data?.id;
    const addressResponse = await c.post(`/v1/clients/${clientId}/addresses`, address);
    expect(addressResponse.status, 201, 'Address creation failed');
    const addressId = addressResponse.data?.id;
    const fiscalResponse = await c.post(`/v1/clients/${clientId}/fiscal-data`, fiscal);
    expect(fiscalResponse.status, 201, 'FiscalData creation failed');
    const fiscalId = fiscalResponse.data?.id;
    const documentResponse = await c.post(`/v1/clients/${clientId}/documents`, document);
    expect(documentResponse.status, 201, 'Document creation failed');
    const documentId = documentResponse.data?.id;
    for (const [name, id] of Object.entries({ contactId, addressId, fiscalId, documentId })) requireId(name, id);

    let deleted = false;
    c.deferCleanup(async () => {
      if (!deleted) {
        const response = await c.delete(`/v1/clients/${clientId}`);
        if (![200, 410].includes(response.status)) throw new SmokeError('Aggregate cleanup failed');
      }
      for (const path of [`/v1/clients/${clientId}`, `/v1/clients/${clientId}/contacts/${contactId}`, `/v1/clients/${clientId}/addresses/${addressId}`, `/v1/clients/${clientId}/fiscal-data/${fiscalId}`, `/v1/clients/${clientId}/documents/${documentId}`]) {
        expect((await c.get(path)).status, 410, 'Aggregate cleanup verification failed');
      }
    });

    for (const path of [`/v1/clients/${clientId}`, `/v1/clients/${clientId}/contacts/${contactId}`, `/v1/clients/${clientId}/addresses/${addressId}`, `/v1/clients/${clientId}/fiscal-data/${fiscalId}`, `/v1/clients/${clientId}/documents/${documentId}`]) expect((await c.get(path)).status, 200, 'Aggregate read failed');
    expect((await c.put(`/v1/clients/${clientId}/contacts/${contactId}`, { ...contact, jobTitle: 'Smoke Updated' })).status, 200, 'Contact update failed');
    expect((await c.put(`/v1/clients/${clientId}/addresses/${addressId}`, { ...address, street: `Updated Street ${suffix}` })).status, 200, 'Address update failed');
    expect((await c.put(`/v1/clients/${clientId}/fiscal-data/${fiscalId}`, { ...fiscal, taxNumber: `SU${String(suffix).slice(-8)}` })).status, 200, 'FiscalData update failed');
    expect((await c.put(`/v1/clients/${clientId}/documents/${documentId}`, { ...document, documentNumber: `UPDATED-${suffix}` })).status, 200, 'Document update failed');
    expect((await c.patch(`/v1/clients/${clientId}/deactivate`)).status, 200, 'Client deactivation failed');
    expect((await c.get(`/v1/clients/${clientId}/documents/${documentId}`)).status, 200, 'Inactive parent read failed');
    expect((await c.post(`/v1/clients/${clientId}/documents`, { ...document, documentNumber: `BLOCKED-${suffix}` })).status, 410, 'Inactive parent mutation was not blocked');
    expect((await c.patch(`/v1/clients/${clientId}/activate`)).status, 200, 'Client reactivation failed');
    expect((await c.delete(`/v1/clients/${clientId}`)).status, 200, 'Client deletion failed');
    deleted = true;
    return { clientId, contactId, addressId, fiscalId, documentId };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const client = await createAuthenticatedSmokeClient();
    const number = name => Number(process.env[name]);
    await runClientAggregateSmoke(client, {
      clientId: number('GERIT_SMOKE_CLIENT_ID'), partyTypeId: number('GERIT_SMOKE_PARTY_TYPE_ID'),
      acquisitionSourceTypeId: number('GERIT_SMOKE_ACQUISITION_SOURCE_TYPE_ID'), statusDomainId: number('GERIT_SMOKE_STATUS_DOMAIN_ID'),
      statusDefinitionId: number('GERIT_SMOKE_STATUS_DEFINITION_ID'), addressTypeId: number('GERIT_SMOKE_ADDRESS_TYPE_ID'), documentTypeId: number('GERIT_SMOKE_DOCUMENT_TYPE_ID'),
    });
    console.log('Client aggregate production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Client aggregate production smoke failed');
    process.exitCode = 1;
  }
}
