import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runActionsSmoke } from './actions.mjs';

function fixture() {
  const calls = [], cleanups = [];
  let deleted = false;
  const client = {
    async get(path) {
      calls.push(['GET', path]);
      if (path.includes('/paged')) return { status: 200, data: { items: [{ id: 42 }] } };
      if (path === '/v1/actions/') return { status: 200, data: deleted ? [] : [{ id: 42, name: 'SMOKE_ACTION_123' }] };
      return deleted ? { status: 410 } : { status: 200, data: { id: 42, name: 'SMOKE_ACTION_123' } };
    },
    async post(path, body) { calls.push(['POST', path, body]); return { status: 201, data: { id: 42 } }; },
    async put(path, body) { calls.push(['PUT', path, body]); return { status: 200, data: true }; },
    async patch(path) { calls.push(['PATCH', path]); return { status: 200 }; },
    async delete(path) { calls.push(['DELETE', path]); deleted = true; return { status: 200 }; },
    deferCleanup(fn) { cleanups.push(fn); },
    async run(fn) { try { return await fn(client); } finally { for (const cleanup of cleanups.reverse()) await cleanup(); } },
  };
  return { client, calls };
}

test('Actions smoke covers lifecycle and cleanup without touching existing records', async () => {
  const f = fixture();
  const result = await runActionsSmoke(f.client, { now: () => 123 });
  assert.equal(result.id, 42);
  assert.equal(f.calls.filter(([method]) => method === 'DELETE').length, 1);
  assert.ok(f.calls.every(([, path]) => path.startsWith('/v1/actions')));
});
