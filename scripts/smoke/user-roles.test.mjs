import assert from 'node:assert/strict';
import test from 'node:test';
import { runUserRolesIsolationSmoke, runUserRolesSmoke } from './user-roles.mjs';

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

const isolationEnv = {
  GERIT_SMOKE_BASE_URL: 'https://gerit-api.vianahub.pt',
  GERIT_SMOKE_EMAIL: 'primary-fixture',
  GERIT_SMOKE_PASSWORD: 'primary-fixture-password',
  GERIT_SMOKE_SECONDARY_EMAIL: 'secondary-fixture',
  GERIT_SMOKE_SECONDARY_PASSWORD: 'secondary-fixture-password',
};

test('isolation requires both secondary credentials before making requests', async () => {
  for (const key of ['GERIT_SMOKE_SECONDARY_EMAIL', 'GERIT_SMOKE_SECONDARY_PASSWORD']) {
    await assert.rejects(runUserRolesIsolationSmoke({
      env: { ...isolationEnv, [key]: '' },
      fetchImpl: () => assert.fail('network'),
    }), { message: `Missing required environment variable: ${key}` });
  }
});

function isolationFixture({ leak = false, sameTenant = false, unexpectedCreate = false } = {}) {
  const { client, deletions } = fixture();
  const log = [];
  const calls = [];
  let secondaryAssociation;
  const options = {
    env: isolationEnv,
    log: entry => log.push(entry),
    async fetchImpl(url, request) {
      const path = url.pathname + url.search;
      const body = request.body && JSON.parse(request.body);
      if (path === '/v1/auth/login') {
        const secondary = body.email === isolationEnv.GERIT_SMOKE_SECONDARY_EMAIL;
        calls.push(['login', secondary]);
        assert.equal(body.password, secondary ? isolationEnv.GERIT_SMOKE_SECONDARY_PASSWORD : isolationEnv.GERIT_SMOKE_PASSWORD);
        return Response.json({ tenantId: secondary && !sameTenant ? 2 : 1, accessToken: secondary ? 'secondary-token' : 'primary-token' });
      }
      const secondary = request.headers.Authorization === 'Bearer secondary-token';
      assert.equal(request.headers.Authorization, secondary ? 'Bearer secondary-token' : 'Bearer primary-token');
      calls.push([request.method, secondary, url.pathname]);
      if (secondary && unexpectedCreate) {
        if (request.method === 'POST') {
          const primary = await client.get(`/v1/user-roles/users/${body.userId}/roles/${body.roleId}`);
          secondaryAssociation = { ...primary.data, id: 31 };
          return Response.json(secondaryAssociation, { status: 201 });
        }
        if (path.includes('/users/') && secondaryAssociation) {
          if (request.method === 'GET') return Response.json(secondaryAssociation);
          secondaryAssociation = undefined;
          return Response.json(null);
        }
      }
      const result = secondary
        ? request.method === 'POST' ? { status: 404 }
          : path.includes('/users/') ? { status: 410 }
            : { status: 200, data: leak && path.endsWith('/user/10') ? [{ userId: 10, roleId: 20 }] : [] }
        : await client[request.method.toLowerCase()](path, body);
      return Response.json(result.data ?? null, { status: result.status });
    },
  };
  return { options, deletions, calls, log };
}

test('isolation authenticates independently and keeps credentials and tokens out of logs', async () => {
  const { options, deletions, calls, log } = isolationFixture();
  assert.equal((await runUserRolesIsolationSmoke(options)).associationResidual, 0);
  assert.deepEqual(calls.slice(0, 2), [['login', true], ['login', false]]);
  assert.deepEqual(calls.slice(2).filter(call => call[1]).map(call => call[0]), ['GET', 'GET', 'GET', 'GET', 'GET', 'POST', 'DELETE']);
  assert.doesNotMatch(log.join(''), /fixture|token|Bearer|password/i);
  assert.deepEqual(deletions, ['/v1/user-roles/users/10/roles/20', '/v1/roles/20', '/v1/users/10']);
});

test('isolation rejects same-tenant credentials before fixture creation', async () => {
  const { options, calls } = isolationFixture({ sameTenant: true });
  await assert.rejects(runUserRolesIsolationSmoke(options), /distinct authenticated tenants/);
  assert.deepEqual(calls, [['login', true], ['login', false]]);
});

test('isolation still cleans primary fixtures when a secondary read fails the assertion', async () => {
  const { options, deletions } = isolationFixture({ leak: true });
  await assert.rejects(runUserRolesIsolationSmoke(options), /isolation visibility mismatch/);
  assert.deepEqual(deletions, ['/v1/user-roles/users/10/roles/20', '/v1/roles/20', '/v1/users/10']);
});

test('isolation cleans an unexpected secondary creation before primary fixtures', async () => {
  const { options, deletions, calls } = isolationFixture({ unexpectedCreate: true });
  await assert.rejects(runUserRolesIsolationSmoke(options), /secondary create failed \(201\)/);
  const createIndex = calls.findIndex(([method, secondary]) => method === 'POST' && secondary);
  assert.ok(createIndex >= 0);
  assert.deepEqual(calls.slice(createIndex + 1, createIndex + 5).map(call => call.slice(0, 2)), [
    ['GET', true], ['DELETE', true], ['GET', true], ['GET', false],
  ]);
  assert.deepEqual(deletions, ['/v1/user-roles/users/10/roles/20', '/v1/roles/20', '/v1/users/10']);
});
