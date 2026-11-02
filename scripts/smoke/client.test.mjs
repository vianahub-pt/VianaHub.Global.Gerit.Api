import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createAuthenticatedSmokeClient } from './client.mjs';

const env = { GERIT_SMOKE_BASE_URL: 'https://gerit-api.vianahub.pt', GERIT_SMOKE_EMAIL: 'fixture', GERIT_SMOKE_PASSWORD: 'fixture-password' };
const response = (data, status = 200) => new Response(JSON.stringify(data), { status });
const auth = () => response({ accessToken: 'fixture-access', refreshToken: 'fixture-refresh' });

test('missing variables stop before network', async () => {
  for (const key of Object.keys(env)) {
    await assert.rejects(createAuthenticatedSmokeClient({ env: { ...env, [key]: '' }, fetchImpl: () => assert.fail('network') }), { message: `Missing required environment variable: ${key}` });
  }
});
test('production guard rejects alternate origins and ambiguous URLs', async () => {
  for (const base of ['http://gerit-api.vianahub.pt', 'https://example.org', 'https://gerit-api.vianahub.pt@evil.test', 'https://gerit-api.vianahub.pt/path', 'https://gerit-api.vianahub.pt:443']) {
    await assert.rejects(createAuthenticatedSmokeClient({ env: { ...env, GERIT_SMOKE_BASE_URL: base }, fetchImpl: () => assert.fail('network') }), /host mismatch/);
  }
});
test('real login contract, headers, locales and secret-free logging', async () => {
  const calls = [], logs = [];
  const client = await createAuthenticatedSmokeClient({ env, log: x => logs.push(x), fetchImpl: async (url, options) => { calls.push({ url, ...options }); return calls.length === 1 ? auth() : response([]); } });
  for (const locale of ['pt-PT', 'pt-BR', 'en-US', 'es-ES']) await client.get('/v1/teams?search=private', { locale });
  assert.deepEqual(JSON.parse(calls[0].body), { email: env.GERIT_SMOKE_EMAIL, password: env.GERIT_SMOKE_PASSWORD });
  assert.equal(calls[0].url.pathname, '/v1/auth/login');
  for (const call of calls) assert.equal(call.redirect, 'error');
  assert.equal(calls[1].headers.Authorization, 'Bearer fixture-access');
  assert.deepEqual(calls.slice(1).map(c => c.headers['Accept-Language']), ['pt-PT', 'pt-BR', 'en-US', 'es-ES']);
  assert.doesNotMatch(logs.join(''), /fixture|private|Bearer|refresh|password/i);
  await assert.rejects(client.get('//evil.test'), /Invalid API path/);
  await assert.rejects(client.get('/v1/../../auth'), /Invalid API path/);
  await assert.rejects(client.get('/v1/teams', { locale: 'fr-FR' }), /Unsupported locale/);
});
test('failed login never retries', async () => {
  let calls = 0;
  await assert.rejects(createAuthenticatedSmokeClient({ env, log: () => {}, fetchImpl: async () => { calls++; return response({}, 401); } }), /Authentication failed/);
  assert.equal(calls, 1);
});
test('transport and malformed JSON errors never expose payloads', async () => {
  for (const fetchImpl of [async () => { throw Error('fixture-password'); }, async () => new Response('fixture-access')]) {
    await assert.rejects(createAuthenticatedSmokeClient({ env, fetchImpl, log: () => {} }), e => !/fixture/.test(e.message));
  }
});
test('cleanup runs all actions even on failure and closes client', async () => {
  const client = await createAuthenticatedSmokeClient({ env, log: () => {}, fetchImpl: async () => auth() });
  const cleaned = [];
  await assert.rejects(client.run(async c => {
    c.deferCleanup(async () => cleaned.push(1));
    c.deferCleanup(async () => { cleaned.push(2); throw Error('private'); });
    throw Error('scenario');
  }), /Cleanup incomplete/);
  assert.deepEqual(cleaned, [2, 1]);
  await assert.rejects(client.get('/v1/teams'), /closed/);
});
