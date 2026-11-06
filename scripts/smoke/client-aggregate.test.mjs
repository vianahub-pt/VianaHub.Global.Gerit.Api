import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runClientAggregateSmoke } from './client-aggregate.mjs';

const ids = { clientId: 1004, partyTypeId: 1, acquisitionSourceTypeId: 1, statusDomainId: 1, statusDefinitionId: 1, addressTypeId: 1, documentTypeId: 1 };

test('aggregate smoke validates required dependency ids', async () => {
  await assert.rejects(runClientAggregateSmoke({ run: async () => {} }, { ...ids, documentTypeId: 0 }), /Invalid documentTypeId/);
});

test('aggregate smoke cleans up after the complete lifecycle', async () => {
  const calls = [];
  const cleanups = [];
  const client = {
    run: async scenario => { try { return await scenario({
      post: async (path, body) => { calls.push(['POST', path, body]); return { status: body?.documentNumber?.startsWith('BLOCKED-') ? 410 : 201, data: { id: calls.length } }; },
      get: async path => { calls.push(['GET', path]); return { status: path.includes('/blocked') ? 410 : (calls.some(x => x[0] === 'DELETE' && x[1] === '/v1/clients/1004') ? 410 : 200), data: { id: 1 } }; },
      put: async path => { calls.push(['PUT', path]); return { status: 200 }; }, patch: async path => { calls.push(['PATCH', path]); return { status: 200 }; },
      delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; }, deferCleanup: action => cleanups.push(action),
    }); } finally { while (cleanups.length) await cleanups.pop()(); } },
  };
  const result = await runClientAggregateSmoke(client, { ...ids, now: () => 12345678 });
  assert.equal(result.clientId, 1004);
  assert.ok(calls.some(call => call[0] === 'DELETE' && call[1] === '/v1/clients/1004'));
  assert.equal(cleanups.length, 0);
});

test('aggregate smoke runs cleanup when a mutation fails', async () => {
  const cleanups = [];
  let deleted = false;
  const client = {
    run: async scenario => { try { return await scenario({
      post: async () => ({ status: 201, data: { id: 1 } }), get: async () => ({ status: deleted ? 410 : 200, data: { id: 1 } }),
      put: async () => { throw new Error('simulated failure'); }, patch: async () => ({ status: 200 }), delete: async () => ({ status: 200 }),
      delete: async () => { deleted = true; return { status: 200 }; }, deferCleanup: action => cleanups.push(action),
    }); } finally { while (cleanups.length) await cleanups.pop()(); } },
  };
  await assert.rejects(runClientAggregateSmoke(client, { ...ids, now: () => 55 }), /simulated failure/);
  assert.equal(cleanups.length, 0);
});
