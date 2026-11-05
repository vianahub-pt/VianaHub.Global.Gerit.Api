import test from 'node:test';
import assert from 'node:assert/strict';
import { runClientAddressSmoke } from './client-address.mjs';

test('smoke uses configured parent and cleans temporary address', async () => {
  const calls = [];
  let getCount = 0;
  const client = {
    post: async (path, body) => { calls.push(['POST', path, body]); return { status: 201, data: { id: 42 } }; },
    get: async path => { calls.push(['GET', path]); getCount += 1; return { status: getCount === 1 ? 200 : 410, data: { clientId: 7, street: 'SMOKE_STREET_1' } }; },
    put: async (path, body) => { calls.push(['PUT', path, body]); return { status: 200 }; },
    patch: async (path) => { calls.push(['PATCH', path]); return { status: 200 }; },
    delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; },
    deferCleanup(action) { this.cleanupAction = action; },
    async run(scenario) { try { return await scenario(this); } finally { await this.cleanupAction?.(); } },
  };
  const result = await runClientAddressSmoke(client, { clientId: 7, addressTypeId: 1, now: () => 1 });
  assert.deepEqual(result, { id: 42 });
  assert.equal(calls.filter(([method]) => method === 'PATCH').length, 4);
  assert.equal(calls.at(-2)[0], 'DELETE');
  assert.equal(calls.at(-1)[0], 'GET');
});

test('smoke rejects missing address type before requests', async () => {
  const client = { run: async () => { throw new Error('must not run'); } };
  await assert.rejects(() => runClientAddressSmoke(client, { clientId: 7 }), /Invalid GERIT_SMOKE_ADDRESS_TYPE_ID/);
});
