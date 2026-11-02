import { createAuthenticatedSmokeClient, SmokeError } from './client.mjs';

try {
  const client = await createAuthenticatedSmokeClient();
  await client.run(async () => {
    const result = await client.get('/v1/teams');
    if (result.status !== 200 || !Array.isArray(result.data)) {
      throw new SmokeError('Authenticated read failed');
    }
  });
  console.log('Authentication smoke: PASS');
} catch (error) {
  console.error(error instanceof SmokeError ? error.message : 'Authentication smoke failed');
  console.error('Authentication smoke: FAIL');
  process.exitCode = 1;
}
