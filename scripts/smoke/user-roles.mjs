import { randomUUID } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runUserRolesSmoke(admin) {
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
    expect(await client.delete(path), 200, 'delete association');
    expect(await client.get(path), 410, 'association gone');
    await verifyLists(false);
    return { temporaryUserId: userId, temporaryRoleId: roleId, associationResidual: 0 };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    console.log(JSON.stringify(await runUserRolesSmoke(await createAuthenticatedSmokeClient())));
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'UserRoles smoke failed');
    process.exitCode = 1;
  }
}
