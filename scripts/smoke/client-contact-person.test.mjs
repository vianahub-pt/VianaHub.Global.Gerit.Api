import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runClientContactPersonSmoke } from './client-contact-person.mjs';

function fixture() {
  const calls = [], cleanups = [];
  let deleted = false;
  const client = {
    async post(path, body) { calls.push(['POST', path, body]); return { status: 201, data: { id: 42 } }; },
    async get(path) { calls.push(['GET', path]); return deleted ? { status: 410 } : { status: 200, data: { id: 42, clientId: 7, email: 'smoke.contact.123@example.invalid', isPrimary: false, isCellPhoneWhatsapp: true } }; },
    async put(path, body) { calls.push(['PUT', path, body]); return { status: 200, data: true }; },
    async patch(path) { calls.push(['PATCH', path]); return { status: 200 }; },
    async delete(path) { calls.push(['DELETE', path]); deleted = true; return { status: 200 }; },
    deferCleanup(fn) { cleanups.push(fn); },
    async run(fn) { try { return await fn(client); } finally { for (const cleanup of cleanups.reverse()) await cleanup(); } },
  };
  return { client, calls };
}

test('smoke uses only the configured parent and always cleans the temporary non-primary contact', async () => {
  const f = fixture();
  await runClientContactPersonSmoke(f.client, { clientId: 7, now: () => 123 });
  assert.ok(f.calls.every(([, path]) => path.startsWith('/v1/clients/7/contacts')));
  assert.equal(f.calls.filter(([method]) => method === 'POST').length, 1);
  assert.equal(f.calls.filter(([method]) => method === 'DELETE').length, 1);
  assert.equal(f.calls.find(([method]) => method === 'POST')[2].isPrimary, false);
});

test('invalid client id stops before any request', async () => {
  const f = fixture();
  await assert.rejects(runClientContactPersonSmoke(f.client, { clientId: 0 }), /Invalid GERIT_SMOKE_CLIENT_ID/);
  assert.equal(f.calls.length, 0);
});
