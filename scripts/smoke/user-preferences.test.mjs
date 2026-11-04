import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runUserPreferencesSmoke } from './user-preferences.mjs';

const original = () => ({
  id: 42, tenantId: 7, userId: 17, isActive: true, isDeleted: false,
  appearance: 'light', currencyCode: 'EUR', locale: 'es-ES', timezone: 'Europe/Lisbon',
  dateFormat: 'yyyy-MM-dd', timeFormat: '24h', dayStart: '22:00', dayEnd: '06:00',
  emailNewsletter: true, emailWeeklyReport: false, emailApproval: true,
  emailAlerts: false, emailReminders: true, emailPlanner: false,
});

function fixture({ missing = false, wrongOwner = false, invalidSnapshot = false, updateFailure = false, restoreFailure = false } = {}) {
  let data = original();
  if (wrongOwner) data.userId = 18;
  if (invalidSnapshot) delete data.dayEnd;
  const calls = [], logs = [], cleanups = [];
  let writes = 0;
  const client = {
    async get(path) { calls.push(['GET', path]); return { status: missing ? 410 : 200, data: structuredClone(data) }; },
    async put(path, body) {
      calls.push(['PUT', path, structuredClone(body)]); writes++;
      if (writes === 2 && restoreFailure) return { status: 500 };
      data = { ...data, ...body };
      if (writes === 1 && updateFailure) throw Error('transport failure after save');
      return { status: 200, data: true };
    },
    deferCleanup(action) { cleanups.push(action); },
    async run(scenario) {
      try { return await scenario(client); }
      finally { for (const cleanup of cleanups.reverse()) await cleanup(); }
    },
  };
  return { client, calls, logs, data: () => data, options: { userId: 17, log: value => logs.push(value) } };
}

test('default existing preference scenario is strictly read-only', async () => {
  const f = fixture();
  const result = await runUserPreferencesSmoke(f.client, f.options);
  assert.equal(result.mutation, 'NOT EXECUTED');
  assert.equal(result.create, 'NOT EXECUTED');
  assert.deepEqual(f.calls.map(x => x[0]), ['GET', 'GET']);
  assert.deepEqual(f.data(), original());
  assert.doesNotMatch(f.logs.join(''), /es-ES|22:00|currencyCode|emailNewsletter/);
});
test('missing preference never causes a Create or Delete', async () => {
  const f = fixture({ missing: true });
  assert.equal((await runUserPreferencesSmoke(f.client, { ...f.options, allowMutation: true })).mutation, 'NOT EXECUTED');
  assert.deepEqual(f.calls.map(x => x[0]), ['GET']);
});
test('invalid expected user and ownership mismatch stop before mutation', async () => {
  const f = fixture({ wrongOwner: true });
  await assert.rejects(runUserPreferencesSmoke(f.client, f.options), /discovery failed/);
  await assert.rejects(runUserPreferencesSmoke(f.client, { ...f.options, userId: -1 }), /Invalid GERIT_SMOKE_USER_ID/);
  assert.deepEqual(f.calls.map(x => x[0]), ['GET']);
});
test('incomplete snapshot refuses mutation', async () => {
  const f = fixture({ invalidSnapshot: true });
  await assert.rejects(runUserPreferencesSmoke(f.client, { ...f.options, allowMutation: true }), /cannot be safely restored/);
  assert.deepEqual(f.calls.map(x => x[0]), ['GET', 'GET']);
});
test('opt-in mutation restores all editable values and preserves state', async () => {
  const f = fixture();
  assert.equal((await runUserPreferencesSmoke(f.client, { ...f.options, allowMutation: true })).mutation, 'PASS');
  assert.deepEqual(f.data(), original());
  assert.deepEqual(f.calls.map(x => x[0]), ['GET', 'GET', 'PUT', 'GET', 'PUT', 'GET']);
  assert.match(f.logs.join(''), /restoration: PASS/);
});
test('finally restores even when mutation transport fails after saving', async () => {
  const f = fixture({ updateFailure: true });
  await assert.rejects(runUserPreferencesSmoke(f.client, { ...f.options, allowMutation: true }), /transport failure/);
  assert.deepEqual(f.data(), original());
});
test('failed restoration fails the whole smoke', async () => {
  const f = fixture({ restoreFailure: true });
  await assert.rejects(runUserPreferencesSmoke(f.client, { ...f.options, allowMutation: true }), /restoration failed/);
  assert.doesNotMatch(f.logs.join(''), /restoration: PASS/);
});
