const API = '';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (init?.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API}${path}`, {
    ...init,
    headers,
    credentials: 'include',
  });

  if (response.status === 401) {
    throw new Error('UNAUTHORIZED');
  }

  if (!response.ok) {
    let message = 'خطا در ارتباط با سرور';
    try {
      const body = (await response.json()) as { message?: string };
      if (body.message) {
        message = body.message;
      }
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export type SessionUser = {
  username: string;
  displayName: string;
};

export type Application = {
  id: string;
  name: string;
  platform: string;
  description?: string | null;
  active: boolean;
  latestVersion?: string | null;
};

export type ReleaseFile = {
  id: number;
  name: string;
  size: number;
  sha256: string;
  contentType: string;
  downloadUrl: string;
};

export type Release = {
  id: string;
  applicationId: string;
  version: string;
  buildNumber: number;
  releaseNotes: string;
  minimumVersion?: string | null;
  updateType: string;
  status: string;
  channel: string;
  createdBy: string;
  createdAt: string;
  publishedAt?: string | null;
  downloadCount: number;
  files: ReleaseFile[];
};

export const api = {
  me: () => request<SessionUser>('/api/auth/me'),
  login: (username: string) =>
    request<SessionUser>('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify({ username }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  apps: () => request<Application[]>('/api/applications'),
  createApp: (body: { id: string; name: string; platform: string; description?: string }) =>
    request<Application>('/api/applications', { method: 'POST', body: JSON.stringify(body) }),
  releases: (appId: string) => request<Release[]>(`/api/applications/${encodeURIComponent(appId)}/releases`),
  createRelease: (body: {
    applicationId: string;
    version: string;
    buildNumber: number;
    releaseNotes?: string;
    minimumVersion?: string;
    updateType: string;
    channel: string;
  }) => request<Release>('/api/releases', { method: 'POST', body: JSON.stringify(body) }),
  uploadFile: async (releaseId: string, file: File) => {
    const data = new FormData();
    data.append('file', file);
    return request<ReleaseFile>(`/api/releases/${encodeURIComponent(releaseId)}/file`, {
      method: 'POST',
      body: data,
    });
  },
  publish: (id: string) => request<void>(`/api/releases/${encodeURIComponent(id)}/publish`, { method: 'POST' }),
  rollback: (id: string) => request<void>(`/api/releases/${encodeURIComponent(id)}/rollback`, { method: 'POST' }),
};
