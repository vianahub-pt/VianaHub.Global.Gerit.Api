import assert from 'node:assert/strict';
import test from 'node:test';
import { runUsersSmoke } from './users.mjs';

function fixture(passwordStatus = 200, invalidIdentity) {
  let user;
  let credential;
  let deletionCount = 0;
  let passwordChanges = 0;
  let administrativeChanges = 0;
  const cleanup = [];
  const response = (status, data) => ({ status, data });
  const admin = {
    async run(scenario) {
      try { return await scenario(admin); }
      finally { for (const action of cleanup.reverse()) await action(); }
    },
    deferCleanup: action => cleanup.push(action),
    async get(path) {
      const users = user ? [user] : [];
      if (path.includes('Search=') && invalidIdentity === 'ambiguous') return response(200, { items: [...users, ...users] });
      if (path.startsWith('/v1/users/paged?')) return response(200, { items: users });
      if (path === '/v1/users/') return response(200, users);
      return user && invalidIdentity !== 'absent' ? response(200, user) : response(410);
    },
    async post(path, body) {
      assert.equal(path, '/v1/users/');
      credential = body.password;
      user = { id: 42, name: body.name, email: invalidIdentity === 'email' ? 'existing@example.invalid' : body.email, isActive: true };
      return response(201, { id: invalidIdentity === 'id' ? 43 : user.id });
    },
    async put(path, body) {
      assert.equal(path, '/v1/users/42');
      administrativeChanges++;
      user.name = body.name;
      return response(200);
    },
    async patch(path) {
      assert.match(path, /^\/v1\/users\/42\/(activate|deactivate)$/);
      administrativeChanges++;
      user.isActive = path.endsWith('/activate');
      return response(200);
    },
    async delete(path) {
      assert.equal(path, '/v1/users/42');
      deletionCount++;
      const status = user ? 200 : 410;
      user = undefined;
      return response(status);
    },
  };
  async function login(email, password) {
    assert.equal(email, user.email);
    assert.ok(password === credential);
    const owner = {
      run: scenario => scenario(owner),
      async patch(path, body) {
        assert.equal(path, '/v1/users/42/password');
        assert.ok(body.currentPassword === credential);
        passwordChanges++;
        if (passwordStatus === 200) credential = body.newPassword;
        return response(passwordStatus);
      },
    };
    return owner;
  }
  return { admin, login, deletionCount: () => deletionCount, passwordChanges: () => passwordChanges,
    administrativeChanges: () => administrativeChanges };
}

test('Users smoke changes the credential as owner and cleans up as admin', async () => {
  const scenario = fixture();
  assert.deepEqual(await runUsersSmoke(scenario.admin, scenario.login), { temporaryUserId: 42, logicalResidual: 0 });
  assert.equal(scenario.passwordChanges(), 1);
  assert.equal(scenario.deletionCount(), 2);
});

test('Users smoke runs deferred cleanup after a failed owner password step', async () => {
  const scenario = fixture(403);
  await assert.rejects(runUsersSmoke(scenario.admin, scenario.login), /own password failed/);
  assert.equal(scenario.passwordChanges(), 1);
  assert.equal(scenario.deletionCount(), 1);
});

for (const reason of ['id', 'email', 'ambiguous', 'absent']) {
  test(`Users smoke blocks all mutations after create when identity is ${reason}`, async () => {
    const scenario = fixture(200, reason);
    await assert.rejects(runUsersSmoke(scenario.admin, scenario.login), /identity/);
    assert.equal(scenario.passwordChanges(), 0);
    assert.equal(scenario.administrativeChanges(), 0);
    assert.equal(scenario.deletionCount(), 0);
  });
}
