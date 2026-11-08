import { test } from 'node:test';
import assert from 'node:assert/strict';
import { runResourcesSmoke } from './resources.mjs';

test('Resources smoke covers lifecycle, search and logical cleanup', async () => {
  const calls = []; const cleanups = []; let deleted = false;
  const client = {
    async get(path) { calls.push(['GET', path]); if (path.includes('/paged')) return { status: 200, data: { items: deleted ? [] : [{ id: 7 }] } }; if (path === '/v1/resources/') return { status: 200, data: deleted ? [] : [{ id: 7, name: 'SMOKE_RESOURCE_1' }] }; return deleted ? { status: 410 } : { status: 200, data: { id: 7, name: 'SMOKE_RESOURCE_1', description: 'Controlled Resources smoke' } }; },
    async post(path, body) { calls.push(['POST', path, body]); return { status: 201, data: { id: 7 } }; },
    async put(path, body) { calls.push(['PUT', path, body]); return { status: 200 }; },
    async patch(path) { calls.push(['PATCH', path]); return { status: 200 }; },
    async delete(path) { calls.push(['DELETE', path]); deleted = true; return { status: 200 }; },
    deferCleanup(fn) { cleanups.push(fn); },
    async run(fn) { try { return await fn(client); } finally { for (const cleanup of cleanups.reverse()) await cleanup(); } },
  };
  const result = await runResourcesSmoke(client, { now: () => 1 });
  assert.equal(result.id, 7);
  assert.equal(calls.filter(([method]) => method === 'DELETE').length, 1);
  assert.ok(calls.some(([method, path]) => method === 'GET' && path === '/v1/resources/7'));
});
