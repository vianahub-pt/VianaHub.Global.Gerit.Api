import test from 'node:test';
import assert from 'node:assert/strict';
import { runRolePermissionsSmoke } from './role-permissions.mjs';

test('RolePermissions smoke proves composite-id read/delete roundtrip', async () => {
  const calls = [];
  let cleanup;
  const client = {
    run: async fn => fn({
      get: async path => {
        calls.push(['GET', path]);
        if (path.includes('/role/')) return { status: 200, data: [{ roleId: 2, resourceId: 3, actionId: 4 }] };
        if (path.includes('/resource/')) return { status: 200, data: [{ roleId: 2, resourceId: 3, actionId: 4 }] };
        return { status: 200, data: { roleId: 2, resourceId: 3, actionId: 4 } };
      },
      post: async path => { calls.push(['POST', path]); return { status: 201, data: { id: 10 } }; },
      delete: async path => { calls.push(['DELETE', path]); return { status: 200 }; },
      deferCleanup: fn => { cleanup = fn; calls.push(['CLEANUP']); },
    }),
  };

  const result = await runRolePermissionsSmoke(client, { roleId: 2, resourceId: 3, actionId: 4 });
  assert.deepEqual(result, { roleId: 2, resourceId: 3, actionId: 4 });
  assert.equal(typeof cleanup, 'function');
  assert.equal(calls.filter(call => call[0] === 'GET').length, 4);
  assert.equal(calls.some(call => call[0] === 'DELETE'), false);
});
