import test from 'node:test';
import assert from 'node:assert/strict';
import { runClientFiscalDataSmoke } from './client-fiscal-data.mjs';

test('smoke creates, verifies and cleans fiscal data', async () => {
  const calls = []; let reads = 0;
  const client = { get: async p => { calls.push(['GET', p]); reads += 1; return reads === 1 ? { status: 410 } : { status: reads === 2 ? 200 : 410, data: { clientId: 7, taxNumber: 'SMOKE1' } }; }, post: async (p,b) => { calls.push(['POST',p,b]); return { status: 201, data: { id: 9 } }; }, put: async (p,b) => { calls.push(['PUT',p,b]); return { status: 200 }; }, patch: async p => { calls.push(['PATCH',p]); return { status: 200 }; }, delete: async p => { calls.push(['DELETE',p]); return { status: 200 }; }, deferCleanup(a) { this.cleanup = a; }, async run(s) { try { return await s(this); } finally { await this.cleanup?.(); } } };
  assert.deepEqual(await runClientFiscalDataSmoke(client, { clientId: 7, now: () => 1 }), { id: 9 });
  assert.equal(calls.at(-2)[0], 'DELETE');
  assert.equal(calls.filter(([method]) => method === 'PATCH').length, 4);
});

test('smoke refuses existing fiscal data', async () => {
  const client = { run: async s => s({ get: async () => ({ status: 200, data: [{ id: 1 }] }) }) };
  await assert.rejects(() => runClientFiscalDataSmoke(client, { clientId: 7 }), /already exists/);
});
