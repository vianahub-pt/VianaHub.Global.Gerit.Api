import test from 'node:test';
import assert from 'node:assert/strict';
import { runRolePermissionsSmoke } from './role-permissions.mjs';

test('RolePermissions smoke creates an isolated role and cleans the aggregate', async () => {
  const calls = [];
  const cleanups = [];
  let deletedPermission = false;
  let deletedRole = false;
  const client = {
    run: async fn => fn({
      get: async path => {
        calls.push(['GET', path]);
        if (path.startsWith('/v1/roles/')) return { status: deletedRole ? 410 : 200, data: { id: 42, name: 'SMOKE_ROLE_PERMISSION_1' } };
        if (path.includes('/role/')) return { status: 200, data: deletedPermission ? [] : [{ roleId: 42, resourceId: 3, actionId: 4 }] };
        if (path.includes('/resource/')) return { status: 200, data: deletedPermission ? [] : [{ roleId: 42, resourceId: 3, actionId: 4 }] };
        if (path.includes('/roles/42/')) return { status: deletedPermission ? 410 : 200, data: { roleId: 42, resourceId: 3, actionId: 4 } };
        return { status: 200, data: [] };
      },
      post: async (path, body) => {
        calls.push(['POST', path, body]);
        if (path === '/v1/roles/') return { status: 201, data: { id: 42 } };
        return { status: 201, data: { id: 10 } };
      },
      delete: async path => {
        calls.push(['DELETE', path]);
        if (path.startsWith('/v1/roles/')) deletedRole = true;
        if (path.includes('/role-permissions/')) deletedPermission = true;
        return { status: 200 };
      },
      deferCleanup: fn => { cleanups.push(fn); calls.push(['CLEANUP']); },
    }),
  };

  const result = await runRolePermissionsSmoke(client, { resourceId: 3, actionId: 4, now: () => 1 });
  assert.equal(result.roleId, 42);
  assert.equal(result.resourceId, 3);
  assert.equal(result.actionId, 4);
  assert.equal(calls.some(call => call[0] === 'POST' && call[1] === '/v1/roles/'), true);
  assert.equal(calls.some(call => call[0] === 'POST' && call[1] === '/v1/role-permissions'), true);
  assert.equal(calls.some(call => call[0] === 'POST' && call[1] === '/v1/role-permissions' && call[2].roleId === 42), true);
  assert.equal(cleanups.length, 2);

  for (const cleanup of cleanups.reverse()) await cleanup();
  assert.equal(deletedPermission, true);
  assert.equal(deletedRole, true);
  assert.equal(calls.some(call => call[0] === 'DELETE' && call[1].startsWith('/v1/role-permissions/')), true);
  assert.equal(calls.some(call => call[0] === 'DELETE' && call[1] === '/v1/roles/42'), true);
});
