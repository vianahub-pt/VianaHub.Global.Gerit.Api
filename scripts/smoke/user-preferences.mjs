import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

const fields = [
  'appearance', 'currencyCode', 'locale', 'timezone', 'dateFormat', 'timeFormat',
  'dayStart', 'dayEnd', 'emailNewsletter', 'emailWeeklyReport', 'emailApproval',
  'emailAlerts', 'emailReminders', 'emailPlanner',
];
const timePattern = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]$/;

function snapshot(data) {
  if (!data || !fields.every(key => Object.hasOwn(data, key)) ||
      !['light', 'dark'].includes(data.appearance) || !['24h', '12h'].includes(data.timeFormat) ||
      !timePattern.test(data.dayStart) || !timePattern.test(data.dayEnd) ||
      !['currencyCode', 'locale', 'timezone', 'dateFormat'].every(key => typeof data[key] === 'string' && data[key].length > 0) ||
      data.currencyCode.length > 3 ||
      !fields.filter(key => key.startsWith('email')).every(key => typeof data[key] === 'boolean')) {
    throw new SmokeError('Preference snapshot cannot be safely restored');
  }
  return Object.fromEntries(fields.map(key => [key, data[key]]));
}

function equalFields(actual, expected) {
  return fields.every(key => actual[key] === expected[key]);
}

// Somente o operador conhece o userId da identidade configurada; nunca inferir por listagem.
// Nenhum Create/Delete é executado, inclusive quando não há preferência existente.
export async function runUserPreferencesSmoke(client, {
  userId, allowMutation = false, log = console.log,
} = {}) {
  if (!Number.isSafeInteger(userId) || userId <= 0) throw new SmokeError('Invalid GERIT_SMOKE_USER_ID');
  return client.run(async c => {
    const found = await c.get(`/v1/user-preferences/user/${userId}`);
    if (found.status === 410) {
      log('UserPreferences Create: NOT EXECUTED — dedicated identity scenario requires separate authorization');
      log('UserPreferences mutation: NOT EXECUTED — no existing preference');
      return { mutation: 'NOT EXECUTED', create: 'NOT EXECUTED' };
    }
    if (found.status !== 200 || found.data?.userId !== userId ||
        !Number.isSafeInteger(found.data.id) || found.data.id <= 0) {
      throw new SmokeError('Preference discovery failed');
    }
    const id = found.data.id;
    const path = `/v1/user-preferences/${id}`;
    const detail = await c.get(path);
    if (detail.status !== 200 || detail.data?.id !== id || detail.data.userId !== userId ||
        detail.data.tenantId !== found.data.tenantId || detail.data.isDeleted !== false) {
      throw new SmokeError('Preference detail identity mismatch');
    }
    const original = snapshot(detail.data);
    log(JSON.stringify({ preferenceId: id }));
    log('UserPreferences Create: NOT EXECUTED — existing preference preserved');
    if (!allowMutation) {
      log('UserPreferences read-only smoke: PASS');
      return { mutation: 'NOT EXECUTED', create: 'NOT EXECUTED' };
    }
    // Registrar antes do PUT: mesmo uma falha de transporte pode ter persistido a alteração.
    c.deferCleanup(async () => {
      const restored = await c.put(path, original);
      if (restored.status !== 200 || restored.data !== true) throw new SmokeError('Preference restoration failed');
      const verified = await c.get(path);
      if (verified.status !== 200 || verified.data?.id !== id || verified.data.userId !== userId ||
          verified.data.tenantId !== detail.data.tenantId || verified.data.isActive !== detail.data.isActive ||
          verified.data.isDeleted !== false || !equalFields(verified.data, original)) {
        throw new SmokeError('Preference restoration verification failed');
      }
      log('UserPreferences restoration: PASS');
    });
    const changed = { ...original, appearance: original.appearance === 'light' ? 'dark' : 'light' };
    const updated = await c.put(path, changed);
    if (updated.status !== 200 || updated.data !== true) throw new SmokeError('Controlled preference update failed');
    const verified = await c.get(path);
    if (verified.status !== 200 || verified.data?.id !== id || !equalFields(verified.data, changed)) {
      throw new SmokeError('Controlled preference update verification failed');
    }
    return { mutation: 'PASS', create: 'NOT EXECUTED' };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const rawUserId = process.env.GERIT_SMOKE_USER_ID;
    if (!rawUserId || !/^[1-9][0-9]*$/.test(rawUserId)) throw new SmokeError('Missing or invalid GERIT_SMOKE_USER_ID');
    const mode = process.env.GERIT_SMOKE_RESTORE_MUTATION;
    if (mode !== undefined && mode !== 'YES') throw new SmokeError('Invalid GERIT_SMOKE_RESTORE_MUTATION');
    const userId = Number(rawUserId);
    if (!Number.isSafeInteger(userId)) throw new SmokeError('Invalid GERIT_SMOKE_USER_ID');
    const client = await createAuthenticatedSmokeClient();
    await runUserPreferencesSmoke(client, { userId, allowMutation: mode === 'YES' });
    console.log('UserPreferences production smoke: PASS');
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'UserPreferences production smoke failed');
    console.error('UserPreferences production smoke: FAIL');
    process.exitCode = 1;
  }
}
