import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runClientRootSmoke } from './client-root.mjs';

const dependencies = { partyTypeId: 1, acquisitionSourceTypeId: 2, statusDomainId: 3, statusDefinitionId: 4 };

test('Client root smoke rejects incomplete dependency configuration', async () => {
  await assert.rejects(runClientRootSmoke({ run: async () => {} }, { ...dependencies, statusDomainId: 0 }), /dependency id/);
});

test('Client root smoke exercises lifecycle and cleanup contract', async () => {
  const calls = [];
  const cleanups = [];
  const client = {
    run: async scenario => {
      try { return await scenario({
      post: async (path, body) => { calls.push(['POST', path, body]); return { status: 201, data: { id: 99 } }; },
      postMultipart: async (path, form) => { calls.push(['POST', path, form]); return { status: 200, data: true }; },
      get: async path => { calls.push(['GET', path]); return { status: path.endsWith('/99') && calls.filter(x => x[0] === 'DELETE').length ? 410 : 200, data: { id: 99, name: 'GERIT PRODUCTION SMOKE CLIENT 42' } }; },
      put: async (path, body) => { calls.push(['PUT', path, body]); return { status: 200 }; },
      patch: async path => { calls.push(['PATCH', path]); return { status: 200 }; },
      delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; },
      deferCleanup: action => cleanups.push(action),
    }); } finally { while (cleanups.length) await cleanups.pop()(); }
    },
  };
  const result = await runClientRootSmoke(client, { ...dependencies, now: () => 42 });
  assert.equal(result.id, 99);
  assert.equal(calls.find(call => call[0] === 'POST')[0], 'POST');
  assert.ok(calls.filter(x => x[0] === 'DELETE').length >= 1);
  assert.equal(calls.filter(x => x[0] === 'PATCH').length, 4);
});

test('Client root smoke runs deferred cleanup when a later mutation fails', async () => {
  const calls = [];
  const cleanups = [];
  const client = {
    run: async scenario => {
      try {
        return await scenario({
          get: async path => { calls.push(['GET', path]); return { status: path.endsWith('/99') && calls.some(x => x[0] === 'DELETE') ? 410 : 200, data: path.includes('/paged') ? { items: [] } : { id: 99, name: 'GERIT PRODUCTION SMOKE CLIENT 43' } }; },
          post: async path => { calls.push(['POST', path]); return { status: 201, data: { id: 99 } }; },
          postMultipart: async path => { calls.push(['POST', path]); return { status: 200, data: true }; },
          put: async () => { throw new Error('simulated transport failure'); },
          patch: async () => ({ status: 200 }),
          delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; },
          deferCleanup: action => cleanups.push(action),
        });
      } finally {
        while (cleanups.length) await cleanups.pop()();
      }
    },
  };
  await assert.rejects(runClientRootSmoke(client, { ...dependencies, now: () => 43 }), /simulated transport failure/);
  assert.ok(calls.some(call => call[0] === 'DELETE'));
  assert.equal(cleanups.length, 0);
});
