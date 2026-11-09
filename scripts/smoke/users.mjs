import { randomUUID } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

export async function runUsersSmoke(admin, loginTemporary, evidence = {}) {
  return admin.run(async client => {
    const suffix = randomUUID();
    const name = `SMOKE_USER_${suffix}`;
    const email = `smoke-${suffix}@example.invalid`;
    const password = `Aa1!${randomUUID()}`;
    const newPassword = `Bb2!${randomUUID()}`;
    const pagePath = `/v1/users/paged?PageNumber=1&PageSize=10&Search=${encodeURIComponent(email)}`;
    const expect = (response, status, step) => {
      evidence.steps?.push({ step, status: response.status });
      if (response.status !== status) throw new SmokeError(`Users ${step} failed (${response.status})`);
      return response.data;
    };
    expect(await client.get('/v1/users/paged?PageNumber=1&PageSize=10'), 200, 'preflight');
    const created = expect(await client.post('/v1/users/', { name, email, password, confirmPassword: password }), 201, 'create');
    const id = created?.id;
    if (!Number.isSafeInteger(id) || id <= 0) throw new SmokeError('Users create returned an invalid ID');
    evidence.temporaryUserId = id;
    const path = `/v1/users/${id}`;
    const updatedName = `${name}_UPDATED`;
    let identityVerified = false;
    const matchesIdentity = (user, names) => user?.id === id && names.includes(user?.name) &&
      (user.email === undefined || user.email === email);
    async function verifyIdentity(names) {
      const detail = expect(await client.get(path), 200, 'identity detail');
      const page = expect(await client.get(pagePath), 200, 'identity search');
      if (!matchesIdentity(detail, names) || !Array.isArray(page?.items) ||
          page.items.length !== 1 || !matchesIdentity(page.items[0], names)) {
        throw new SmokeError('Users created identity mismatch');
      }
    }
    async function verifyVisibility(visible) {
      const all = expect(await client.get('/v1/users/'), 200, 'list');
      const page = expect(await client.get(pagePath), 200, 'page');
      if (!Array.isArray(all) || !Array.isArray(page?.items) ||
          [all, page.items].some(items => items.some(user => user.id === id || user.email === email) !== visible)) {
        throw new SmokeError('Users list visibility verification failed');
      }
    }
    client.deferCleanup(async () => {
      const candidate = await client.get(path);
      evidence.steps?.push({ step: 'cleanup identity', status: candidate.status });
      if (candidate.status === 200) {
        if (!matchesIdentity(candidate.data, [name, updatedName])) {
          throw new SmokeError('Users cleanup refused an unverified identity');
        }
        await verifyIdentity([name, updatedName]);
      } else if (candidate.status !== 410 || !identityVerified) {
        throw new SmokeError('Users cleanup could not verify the identity');
      }
      const response = await client.delete(path);
      evidence.steps?.push({ step: 'cleanup delete', status: response.status });
      if (![200, 410].includes(response.status)) throw new SmokeError('Users cleanup failed');
      expect(await client.get(path), 410, 'cleanup detail');
      await verifyVisibility(false);
      evidence.cleanup = 'PASS';
      evidence.logicalResidual = 0;
    });

    const detail = async () => expect(await client.get(path), 200, 'detail');
    await verifyIdentity([name]);
    identityVerified = true;
    await verifyVisibility(true);
    expect(await client.put(path, { name: updatedName }), 200, 'update');
    if ((await detail())?.name !== updatedName) throw new SmokeError('Users update verification failed');

    const owner = await loginTemporary(email, password);
    await owner.run(async self => {
      expect(await self.patch(`${path}/password`, { currentPassword: password, newPassword }), 200, 'own password');
    });
    await (await loginTemporary(email, newPassword)).run(async () => {});

    for (const [operation, active] of [['deactivate', false], ['activate', true]]) {
      for (let attempt = 0; attempt < 2; attempt++) {
        expect(await client.patch(`${path}/${operation}`), 200, operation);
      }
      if ((await detail())?.isActive !== active) throw new SmokeError('Users lifecycle verification failed');
    }
    expect(await client.delete(path), 200, 'delete');
    expect(await client.get(path), 410, 'deleted detail');
    await verifyVisibility(false);
    return { temporaryUserId: id, logicalResidual: 0 };
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const sha = process.env.GERIT_SMOKE_PRODUCTION_SHA;
  const directory = new URL('../../artifacts/production-smoke/', import.meta.url);
  const evidence = {
    capability: 'Users', productionSha: sha, timestamp: new Date().toISOString(),
    environment: 'production', baseUrl: 'https://gerit-api.vianahub.pt',
    http: [], steps: [], authStatus: {}, cleanup: 'UNVERIFIED', logicalResidual: null, result: 'FAIL',
  };
  const log = line => evidence.http.push(JSON.parse(line));
  let evidencePath;
  try {
    if (!/^[a-f0-9]{40}$/.test(sha ?? '')) throw new SmokeError('Missing verified production SHA');
    await mkdir(directory, { recursive: true });
    const target = new URL(`users-${sha}.json`, directory);
    await writeFile(target, JSON.stringify(evidence), { flag: 'wx' });
    evidencePath = target;
    const admin = await createAuthenticatedSmokeClient({
      log: line => {
        log(line);
        evidence.authStatus.admin ??= evidence.http.at(-1).status;
      },
    });
    const result = await runUsersSmoke(admin, async (email, password) => {
      const authStep = evidence.authStatus.initial === undefined ? 'initial' : 'replacement';
      return createAuthenticatedSmokeClient({
        log: line => {
          log(line);
          evidence.authStatus[authStep] ??= evidence.http.at(-1).status;
        },
        env: {
          GERIT_SMOKE_BASE_URL: process.env.GERIT_SMOKE_BASE_URL,
          GERIT_SMOKE_EMAIL: email,
          GERIT_SMOKE_PASSWORD: password,
        },
      });
    }, evidence);
    evidence.result = 'PASS';
    console.log(JSON.stringify(result));
  } catch (error) {
    console.error(error instanceof SmokeError ? error.message : 'Users smoke failed');
    process.exitCode = 1;
  } finally {
    if (evidencePath) {
      evidence.unexpected5xx = evidence.http.filter(request => request.status >= 500).length;
      evidence.mapperOrRuntimeErrors = evidence.unexpected5xx === 0 ? 0 : null;
      await writeFile(evidencePath, JSON.stringify(evidence, null, 2) + '\n');
    }
  }
}
