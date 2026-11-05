import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

// Executar somente após merge, promoção e deploy da versão certificada.
try {
  const client = await createAuthenticatedSmokeClient();
  await client.run(async c => {
    const name = `SMOKE_EQUIPMENT_TYPE_${Date.now()}`;
    const payload = { name, description: 'Controlled production smoke' };
    const created = await c.post('/v1/equipment-types', payload);
    if (created.status !== 201 || !Number.isInteger(created.data?.id) || created.data.id <= 0) {
      throw new SmokeError('EquipmentType creation failed; verify any uncertain creation before retry');
    }
    const id = created.data.id;
    const registerCleanup = resourceId => c.deferCleanup(async () => {
      const deleted = await c.delete(`/v1/equipment-types/${resourceId}`);
      if (![200, 410].includes(deleted.status)) throw new SmokeError('Cleanup delete failed');
      if ((await c.get(`/v1/equipment-types/${resourceId}`)).status !== 410) throw new SmokeError('Cleanup verification failed');
    });
    registerCleanup(id);
    console.log(JSON.stringify({ temporaryResourceId: id }));
    const detail = await c.get(`/v1/equipment-types/${id}`);
    if (detail.status !== 200 || detail.data?.name !== name || detail.data?.description !== payload.description) {
      throw new SmokeError('EquipmentType detail failed');
    }
    for (const locale of ['pt-PT', 'pt-BR', 'en-US', 'es-ES']) {
      const duplicate = await c.post('/v1/equipment-types', payload, { locale });
      if (duplicate.status === 201 && Number.isInteger(duplicate.data?.id) && duplicate.data.id > 0) {
        registerCleanup(duplicate.data.id);
        console.log(JSON.stringify({ temporaryResourceId: duplicate.data.id }));
      }
      if (duplicate.status !== 409) throw new SmokeError('Duplicate contract failed');
    }
    if ((await c.put(`/v1/equipment-types/${id}`, payload)).status !== 204) throw new SmokeError('Own-name update failed');
    for (const operation of ['deactivate', 'deactivate', 'activate', 'activate']) {
      if ((await c.patch(`/v1/equipment-types/${id}/${operation}`)).status !== 200) throw new SmokeError('Lifecycle failed');
      const state = await c.get(`/v1/equipment-types/${id}`);
      if (state.status !== 200 || state.data?.isActive !== (operation === 'activate')) throw new SmokeError('State persistence failed');
    }
  });
  console.log('EquipmentTypes production smoke: PASS');
} catch (error) {
  console.error(error instanceof SmokeError ? error.message : 'EquipmentTypes production smoke failed');
  process.exitCode = 1;
}
