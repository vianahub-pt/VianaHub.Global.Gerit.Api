import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runRolePermissionsSmoke(client, {
  roleId = Number(process.env.GERIT_SMOKE_ROLE_ID),
  resourceId = Number(process.env.GERIT_SMOKE_RESOURCE_ID),
  actionId = Number(process.env.GERIT_SMOKE_ACTION_ID),
} = {}) {
  if (![roleId, resourceId, actionId].every(Number.isSafeInteger) || [roleId, resourceId, actionId].some(id => id <= 0)) {
    throw new SmokeError('GERIT_SMOKE_ROLE_ID, GERIT_SMOKE_RESOURCE_ID and GERIT_SMOKE_ACTION_ID are required');
  }

  return client.run(async c => {
    const preflight = await c.get('/v1/role-permissions');
    if (preflight.status !== 200) throw new SmokeError('RolePermissions preflight failed');

    const created = await c.post('/v1/role-permissions', { roleId, resourceId, actionId });
    if (created.status !== 201) throw new SmokeError('RolePermission creation failed');

    const detailPath = `/v1/role-permissions/roles/${roleId}/resources/${resourceId}/actions/${actionId}`;
    c.deferCleanup(async () => {
      const deleted = await c.delete(detailPath);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('RolePermission cleanup failed');
      const gone = await c.get(detailPath);
      if (![404, 410].includes(gone.status)) throw new SmokeError('RolePermission cleanup verification failed');
    });

    const detail = await c.get(detailPath);
    if (detail.status !== 200 || detail.data?.roleId !== roleId || detail.data?.resourceId !== resourceId || detail.data?.actionId !== actionId) {
      throw new SmokeError('RolePermission detail failed');
    }

    const byRole = await c.get(`/v1/role-permissions/role/${roleId}`);
    if (byRole.status !== 200 || !byRole.data?.some?.(item => item.roleId === roleId && item.resourceId === resourceId && item.actionId === actionId)) {
      throw new SmokeError('RolePermission role read failed');
    }

    const byResource = await c.get(`/v1/role-permissions/resource/${resourceId}`);
    if (byResource.status !== 200 || !byResource.data?.some?.(item => item.roleId === roleId && item.resourceId === resourceId && item.actionId === actionId)) {
      throw new SmokeError('RolePermission resource read failed');
    }

    return { roleId, resourceId, actionId };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const result = await runRolePermissionsSmoke(await createAuthenticatedSmokeClient());
    console.log(JSON.stringify({ roleId: result.roleId, resourceId: result.resourceId, actionId: result.actionId, cleanupPolicy: 'physical-delete' }));
    console.log('RolePermissions production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'RolePermissions production smoke failed');
    console.error('RolePermissions production smoke: FAIL');
    process.exitCode = 1;
  }
}
