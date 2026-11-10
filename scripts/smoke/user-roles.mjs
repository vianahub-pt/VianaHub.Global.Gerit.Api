import { randomUUID } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runUserRolesSmoke(admin, secondary) {
  return admin.run(async client => {
    const suffix = randomUUID();
    const userName = `SMOKE_USER_ROLE_USER_${suffix}`;
    const roleName = `SMOKE_USER_ROLE_ROLE_${suffix}`;
    const email = `smoke-${suffix}@example.invalid`;
    const password = `Aa1!${randomUUID()}`;
    const root = '/v1/user-roles';
    const expect = (response, status, step) => {
      if (response.status !== status) throw new SmokeError(`UserRoles ${step} failed (${response.status})`);
      return response.data;
    };
    const validId = id => Number.isSafeInteger(id) && id > 0;
    if (secondary) {
      if (!validId(client.tenantId) || !validId(secondary.tenantId) || client.tenantId === secondary.tenantId) {
        throw new SmokeError('UserRoles isolation requires distinct authenticated tenants');
      }
      expect(await secondary.get(root + '/'), 200, 'secondary preflight');
    }
    expect(await client.get(root + '/'), 200, 'preflight');

    async function createParent(resource, name, request, verifyExtra = async () => {}) {
      const id = expect(await client.post(`/v1/${resource}/`, request), 201, `create ${resource}`)?.id;
      if (!validId(id)) throw new SmokeError('UserRoles parent creation returned an invalid ID');
      const path = `/v1/${resource}/${id}`;
      async function verify(detail) {
        if (detail?.id !== id || detail?.name !== name) throw new SmokeError('UserRoles parent identity mismatch');
        await verifyExtra(id);
      }
      client.deferCleanup(async () => {
        await verify(expect(await client.get(path), 200, 'cleanup parent identity'));
        expect(await client.delete(path), 200, 'cleanup parent delete');
        expect(await client.get(path), 410, 'cleanup parent gone');
      });
      await verify(expect(await client.get(path), 200, 'parent identity'));
      return id;
    }

    const userId = await createParent('users', userName, { name: userName, email, password, confirmPassword: password }, async id => {
      const page = expect(await client.get(`/v1/users/paged?PageNumber=1&PageSize=10&Search=${encodeURIComponent(email)}`),
        200, 'verify user search');
      if (!Array.isArray(page?.items) || page.items.length !== 1 ||
          page.items[0]?.id !== id || page.items[0]?.name !== userName ||
          (page.items[0]?.email !== undefined && page.items[0].email !== email)) {
        throw new SmokeError('UserRoles user search identity mismatch');
      }
    });
    const roleId = await createParent('roles', roleName, { name: roleName, description: 'Controlled UserRoles smoke' });
    const association = expect(await client.post(root + '/', { userId, roleId }), 201, 'create association');
    const path = `${root}/users/${userId}/roles/${roleId}`;
    const matches = item => item?.userId === userId && item?.roleId === roleId;
    function verifyAssociation(item) {
      if (!validId(association?.id) || !matches(item) || item.id !== association.id ||
          item.userName !== userName || item.roleName !== roleName) {
        throw new SmokeError('UserRoles association identity mismatch');
      }
    }
    async function verifyLists(present) {
      for (const path of [`${root}/user/${userId}`, `${root}/role/${roleId}`]) {
        const items = expect(await client.get(path), 200, 'association list');
        if (!Array.isArray(items) || items.some(matches) !== present) {
          throw new SmokeError('UserRoles association visibility mismatch');
        }
      }
    }
    client.deferCleanup(async () => {
      const detail = await client.get(path);
      if (detail.status === 200) {
        verifyAssociation(detail.data);
        expect(await client.delete(path), 200, 'cleanup association');
      } else {
        expect(detail, 410, 'cleanup association gone');
      }
      expect(await client.get(path), 410, 'verify association gone');
      await verifyLists(false);
    });
    verifyAssociation(association);
    verifyAssociation(expect(await client.get(path), 200, 'association detail'));
    await verifyLists(true);
    if (secondary) {
      for (const listPath of [root + '/', `${root}/user/${userId}`, `${root}/role/${roleId}`]) {
        const items = expect(await secondary.get(listPath), 200, 'secondary list');
        if (!Array.isArray(items) || items.some(matches)) throw new SmokeError('UserRoles isolation visibility mismatch');
      }
      expect(await secondary.get(path), 410, 'secondary detail');
      const secondaryCreate = await secondary.post(root + '/', { userId, roleId });
      if (secondaryCreate.status === 201) {
        client.deferCleanup(async () => {
          const detail = await secondary.get(path);
          if (detail.status === 200) {
            const item = detail.data;
            if (!matches(item) || !validId(item.id) || item.id !== secondaryCreate.data?.id ||
                item.userName !== userName || item.roleName !== roleName) {
              throw new SmokeError('UserRoles secondary cleanup identity mismatch');
            }
            expect(await secondary.delete(path), 200, 'secondary cleanup delete');
          } else {
            expect(detail, 410, 'secondary cleanup gone');
          }
          expect(await secondary.get(path), 410, 'verify secondary cleanup');
        });
      }
      expect(secondaryCreate, 404, 'secondary create');
      expect(await secondary.delete(path), 410, 'secondary delete');
      verifyAssociation(expect(await client.get(path), 200, 'primary detail after secondary checks'));
    }
    expect(await client.delete(path), 200, 'delete association');
    expect(await client.get(path), 410, 'association gone');
    await verifyLists(false);
    return { temporaryUserId: userId, temporaryRoleId: roleId, associationResidual: 0 };
  });
}

export async function runUserRolesIsolationSmoke(options = {}) {
  const { env = process.env } = options;
  for (const key of ['GERIT_SMOKE_SECONDARY_EMAIL', 'GERIT_SMOKE_SECONDARY_PASSWORD']) {
    if (!env[key]?.trim()) throw new SmokeError(`Missing required environment variable: ${key}`);
  }
  const secondary = await createAuthenticatedSmokeClient({
    ...options,
    env: {
      ...env,
      GERIT_SMOKE_EMAIL: env.GERIT_SMOKE_SECONDARY_EMAIL,
      GERIT_SMOKE_PASSWORD: env.GERIT_SMOKE_SECONDARY_PASSWORD,
    },
  });
  return secondary.run(async client => runUserRolesSmoke(await createAuthenticatedSmokeClient(options), client));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const result = process.argv.includes('--isolation')
      ? await runUserRolesIsolationSmoke()
      : await runUserRolesSmoke(await createAuthenticatedSmokeClient());
    console.log(JSON.stringify(result));
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'UserRoles smoke failed');
    process.exitCode = 1;
  }
}
