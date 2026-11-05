const locales = new Set(['pt-PT', 'pt-BR', 'en-US', 'es-ES']);

export class SmokeError extends Error {}

export async function createAuthenticatedSmokeClient({
  env = process.env, locale = 'pt-PT', fetchImpl = fetch, log = console.log,
} = {}) {
  for (const key of ['GERIT_SMOKE_BASE_URL', 'GERIT_SMOKE_EMAIL', 'GERIT_SMOKE_PASSWORD']) {
    if (!env[key]?.trim()) throw new SmokeError(`Missing required environment variable: ${key}`);
  }
  const base = env.GERIT_SMOKE_BASE_URL;
  // A validação textual também rejeita userinfo, portas, paths e URLs normalizadas ambíguas.
  if (!/^https:\/\/gerit-api\.vianahub\.pt\/?$/.test(base)) {
    throw new SmokeError('Production host mismatch');
  }
  if (!locales.has(locale)) throw new SmokeError('Unsupported locale');
  let token;
  let usable = true;
  const cleanup = [];
  async function request(method, path, { body, formData, locale: language = locale } = {}) {
    if (!usable) throw new SmokeError('Smoke client closed');
    if (!locales.has(language)) throw new SmokeError('Unsupported locale');
    if (typeof path !== 'string' || !path.startsWith('/v1/') || /[\\#\r\n]/.test(path)) {
      throw new SmokeError('Invalid API path');
    }
    const url = new URL(path, base);
    if (url.origin !== 'https://gerit-api.vianahub.pt' || !url.pathname.startsWith('/v1/')) {
      throw new SmokeError('Invalid API path');
    }
    let response;
    try {
      response = await fetchImpl(url, {
        method, redirect: 'error', signal: AbortSignal.timeout(30000),
        headers: {
          'Accept-Language': language,
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
          ...(body === undefined && formData === undefined ? {} : formData === undefined ? { 'Content-Type': 'application/json' } : {}),
        },
        body: formData === undefined ? (body === undefined ? undefined : JSON.stringify(body)) : formData,
      });
    } catch {
      // Não propagar erros do transporte, que podem conter headers ou payloads.
      throw new SmokeError('HTTP request failed');
    }
    // Não registrar URL/query, headers ou corpos recebidos/enviados.
    log(JSON.stringify({ method, status: response.status, locale: language }));
    let data;
    try {
      const text = await response.text();
      data = text ? JSON.parse(text) : null;
    } catch {
      throw new SmokeError('Invalid JSON response');
    }
    if (response.status === 401) usable = false;
    return { status: response.status, data };
  }
  const auth = await request('POST', '/v1/auth/login', {
    body: { email: env.GERIT_SMOKE_EMAIL, password: env.GERIT_SMOKE_PASSWORD },
  });
  if (auth.status !== 200 || typeof auth.data?.accessToken !== 'string' || !auth.data.accessToken) {
    throw new SmokeError('Authentication failed');
  }
  token = auth.data.accessToken;
  // Refresh token não é retido, exposto ou persistido; não há retry/refresh automático.
  const client = {
    get: (path, options) => request('GET', path, options),
    post: (path, body, options) => request('POST', path, { ...options, body }),
    postMultipart: (path, formData, options) => request('POST', path, { ...options, formData }),
    put: (path, body, options) => request('PUT', path, { ...options, body }),
    patch: (path, body, options) => request('PATCH', path, { ...options, body }),
    delete: (path, options) => request('DELETE', path, options),
    deferCleanup(action) {
      if (typeof action !== 'function') throw new SmokeError('Invalid cleanup action');
      cleanup.push(action);
    },
    async cleanup() {
      let failed = false;
      while (cleanup.length) {
        try { await cleanup.pop()(); } catch { failed = true; }
      }
      if (failed) throw new SmokeError('Cleanup incomplete');
    },
    async run(scenario) {
      try { return await scenario(client); }
      finally {
        try { await client.cleanup(); }
        finally { usable = false; token = undefined; }
      }
    },
  };
  return client;
}
