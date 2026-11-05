import test from 'node:test';
import assert from 'node:assert/strict';
import { runClientDocumentsSmoke } from './client-documents.mjs';

test('smoke creates, exercises lifecycle and cleans documents', async () => {
  const calls = []; let reads = 0; const cleanups = [];
  const client = {
    get: async p => { calls.push(['GET', p]); reads += 1; if (reads === 1) return { status: 200, data: [] }; if (reads === 3) return { status: 200, data: [{ id: 10, documentNumber: '1-bulk-1' }, { id: 11, documentNumber: '1-bulk-2' }] }; return { status: reads === 2 ? 200 : 410, data: { id: 9 } }; },
    post: async (p, b) => { calls.push(['POST', p, b]); return { status: 201, data: { id: 9 } }; },
    postMultipart: async (p, b) => { calls.push(['POST_MULTIPART', p, b]); return { status: 200, data: true }; },
    put: async p => { calls.push(['PUT', p]); return { status: 200 }; },
    patch: async p => { calls.push(['PATCH', p]); return { status: 200 }; },
    delete: async p => { calls.push(['DELETE', p]); return { status: 200 }; },
    deferCleanup(a) { cleanups.push(a); },
    async run(s) { try { return await s(this); } finally { while (cleanups.length) await cleanups.pop()(); } },
  };
  assert.deepEqual(await runClientDocumentsSmoke(client, { clientId: 7, now: () => 1 }), { id: 9 });
  assert.equal(calls.filter(([method]) => method === 'PATCH').length, 4);
  assert.equal(calls.filter(([method]) => method === 'POST_MULTIPART').length, 1);
  assert.equal(calls.filter(([method]) => method === 'DELETE').length, 4);
});

test('smoke refuses non-empty preflight', async () => {
  const client = { run: async s => s({ get: async () => ({ status: 200, data: [{ id: 1 }] }) }) };
  await assert.rejects(() => runClientDocumentsSmoke(client, { clientId: 7 }), /already exist/);
});
