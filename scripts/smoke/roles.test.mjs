import test from 'node:test';
import assert from 'node:assert/strict';
import { runRolesSmoke } from './roles.mjs';

test('Roles smoke covers preflight, CRUD, lifecycle repeats and logical cleanup', async () => {
  const calls = [];
  let cleanup;
  const client = {
    run: async fn => fn({
      get: async path => { calls.push(['GET', path]); if (path.includes('/paged')) return { status: 200, data: { items: [{ id: 1, name: 'SMOKE_ROLE_1' }] } }; return { status: 200, data: { id: 1, name: 'SMOKE_ROLE_1', description: 'Controlled Roles smoke' } }; },
      post: async path => { calls.push(['POST', path]); return { status: 201, data: { id: 1 } }; },
      put: async path => { calls.push(['PUT', path]); return { status: 200 }; },
      patch: async path => { calls.push(['PATCH', path]); return { status: 200 }; },
      delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; },
      deferCleanup: fn => { cleanup = fn; calls.push(['CLEANUP']); },
    }),
  };
  const result = await runRolesSmoke(client, { now: () => 1 });
  assert.equal(result.id, 1);
  assert.equal(calls.filter(call => call[0] === 'PATCH').length, 4);
  assert.equal(calls.some(call => call[0] === 'DELETE'), false);
  assert.equal(calls.some(call => call[0] === 'CLEANUP'), true);
  assert.equal(typeof cleanup, 'function');
});
