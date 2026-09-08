export type RuntimeConfig = {
  apiUrl: string;
};

let runtimeApiUrl = '';

function normalizeBase(value: string): string {
  return value.trim().replace(/\/+$/, '').replace(/\/api$/i, '');
}

export async function loadRuntimeConfig(): Promise<void> {
  try {
    const response = await fetch('/config.json', { cache: 'no-store' });
    if (!response.ok) {
      return;
    }
    const data = (await response.json()) as { apiUrl?: string };
    runtimeApiUrl = normalizeBase(data.apiUrl ?? '');
  } catch {
    runtimeApiUrl = '';
  }
}

export function apiBase(): string {
  const fromEnv = normalizeBase(import.meta.env.VITE_API_URL ?? '');
  if (fromEnv) {
    return fromEnv;
  }
  if (import.meta.env.DEV) {
    return '';
  }
  return runtimeApiUrl;
}

export function apiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }
  const suffix = path.startsWith('/') ? path : `/${path}`;
  return `${apiBase()}${suffix}`;
}
