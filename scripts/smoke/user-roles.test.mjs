import assert from 'node:assert/strict';
import test from 'node:test';
import { runUserRolesSmoke } from './user-roles.mjs';

function fixture(wrongUser = false) {
  const callbacks = [];
  const deletions = [];
  const parents = new Map();
  let association;
  const response = (status, data) => ({ status, data });
  const client = {
    async run(scenario) {
      try { return await scenario(client); }
      finally { for (const cleanup of callbacks.reverse()) await cleanup(); }
    },
    deferCleanup: action => callbacks.push(action),
    async get(path) {
      if (path.includes('/paged?')) return response(200, { items: [parents.get('/v1/users/10')] });
      if (path.startsWith('/v1/user-roles/users/')) return association ? response(200, association) : response(410);
      if (path.startsWith('/v1/user-roles')) return response(200, association ? [association] : []);
      return parents.has(path) ? response(200, parents.get(path)) : response(410);
    },
    async post(path, body) {
      if (path === '/v1/user-roles/') {
        association = { id: 30, ...body, userName: parents.get('/v1/users/10').name, roleName: parents.get('/v1/roles/20').name };
        return response(201, association);
      }
      const id = path === '/v1/users/' ? 10 : 20;
      parents.set(path + id, { id, name: wrongUser && id === 10 ? 'Existing user' : body.name });
      return response(201, { id });
    },
    async delete(path) {
      deletions.push(path);
      if (path.startsWith('/v1/user-roles/')) association = undefined;
      else parents.delete(path);
      return response(200);
    },
  };
  return { client, deletions };
}

test('UserRoles smoke cleans the association before the parents', async () => {
  const { client, deletions } = fixture();
  assert.deepEqual(await runUserRolesSmoke(client), { temporaryUserId: 10, temporaryRoleId: 20, associationResidual: 0 });
  assert.deepEqual(deletions, ['/v1/user-roles/users/10/roles/20', '/v1/roles/20', '/v1/users/10']);
});

test('UserRoles smoke refuses to mutate an unverified parent', async () => {
  const { client, deletions } = fixture(true);
  await assert.rejects(runUserRolesSmoke(client), /parent identity mismatch/);
  assert.deepEqual(deletions, []);
});
