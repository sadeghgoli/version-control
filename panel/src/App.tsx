import { FormEvent, useEffect, useMemo, useState } from 'react';
import { api, type Application, type Release, type SessionUser } from './api';

const platforms: Record<string, string> = {
  Windows: 'ویندوز',
  Android: 'اندروید',
  Pwa: 'PWA',
  Service: 'سرویس',
};

const statuses: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Testing: 'تست',
  Published: 'منتشرشده',
  Deprecated: 'منسوخ',
  Archived: 'آرشیو',
};

const updateTypes: Record<string, string> = {
  Optional: 'اختیاری',
  Mandatory: 'اجباری',
  Critical: 'بحرانی',
};

function formatBytes(size: number) {
  if (size < 1024) {
    return `${size} B`;
  }
  if (size < 1024 * 1024) {
    return `${(size / 1024).toFixed(1)} KB`;
  }
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(value?: string | null) {
  if (!value) {
    return '—';
  }
  return new Intl.DateTimeFormat('fa-IR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

export function App() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [ready, setReady] = useState(false);
  const [apps, setApps] = useState<Application[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [releases, setReleases] = useState<Release[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [showCreateApp, setShowCreateApp] = useState(false);
  const [showCreateRelease, setShowCreateRelease] = useState(false);

  const selected = useMemo(() => apps.find((item) => item.id === selectedId) ?? null, [apps, selectedId]);

  const loadApps = async () => {
    const list = await api.apps();
    setApps(list);
    setSelectedId((current) => current ?? list[0]?.id ?? null);
  };

  const loadReleases = async (appId: string) => {
    setReleases(await api.releases(appId));
  };

  useEffect(() => {
    void api
      .me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setReady(true));
  }, []);

  useEffect(() => {
    if (!user) {
      return;
    }
    void loadApps().catch((err: unknown) => setError(err instanceof Error ? err.message : 'بارگذاری ناموفق بود.'));
  }, [user]);

  useEffect(() => {
    if (!selectedId) {
      setReleases([]);
      return;
    }
    void loadReleases(selectedId).catch((err: unknown) => setError(err instanceof Error ? err.message : 'بارگذاری نسخه‌ها ناموفق بود.'));
  }, [selectedId]);

  const run = async (work: () => Promise<void>) => {
    setBusy(true);
    setError('');
    try {
      await work();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'عملیات ناموفق بود.');
    } finally {
      setBusy(false);
    }
  };

  if (!ready) {
    return <div className="center">در حال بارگذاری...</div>;
  }

  if (!user) {
    return <LoginScreen onLogin={setUser} />;
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div>
          <strong>مرکز انتشار نسخه‌ها</strong>
          <span>Update Center</span>
        </div>
        <div className="topbar-actions">
          <span>{user.displayName}</span>
          <button
            type="button"
            className="btn"
            onClick={() =>
              void run(async () => {
                await api.logout();
                setUser(null);
              })
            }
          >
            خروج
          </button>
        </div>
      </header>

      {error ? <div className="banner is-error">{error}</div> : null}

      <div className="layout">
        <aside className="apps">
          <div className="aside-head">
            <h2>برنامه‌ها</h2>
            <button type="button" className="btn btn-primary" onClick={() => setShowCreateApp(true)}>
              + برنامه
            </button>
          </div>
          {apps.map((app) => (
            <button
              key={app.id}
              type="button"
              className={app.id === selectedId ? 'app-item is-active' : 'app-item'}
              onClick={() => setSelectedId(app.id)}
            >
              <strong>{app.name}</strong>
              <small>
                {app.id} · {platforms[app.platform] ?? app.platform}
                {app.latestVersion ? ` · ${app.latestVersion}` : ''}
              </small>
            </button>
          ))}
        </aside>

        <main className="main">
          {selected ? (
            <>
              <div className="main-head">
                <div>
                  <h1>{selected.name}</h1>
                  <p>
                    شناسه: <code>{selected.id}</code> — {platforms[selected.platform] ?? selected.platform}
                  </p>
                </div>
                <button type="button" className="btn btn-primary" onClick={() => setShowCreateRelease(true)}>
                  + نسخه جدید
                </button>
              </div>

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>شناسه</th>
                      <th>نسخه</th>
                      <th>نوع</th>
                      <th>وضعیت</th>
                      <th>کانال</th>
                      <th>انتشار</th>
                      <th>دانلود</th>
                      <th>فایل</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {releases.map((release) => (
                      <tr key={release.id}>
                        <td>
                          <code>{release.id}</code>
                        </td>
                        <td>
                          {release.version}
                          <small> build {release.buildNumber}</small>
                        </td>
                        <td>{updateTypes[release.updateType] ?? release.updateType}</td>
                        <td>
                          <span className={`status status-${release.status.toLowerCase()}`}>
                            {statuses[release.status] ?? release.status}
                          </span>
                        </td>
                        <td>{release.channel}</td>
                        <td>{formatDate(release.publishedAt)}</td>
                        <td>{release.downloadCount.toLocaleString('fa-IR')}</td>
                        <td>
                          {release.files[0] ? (
                            <a
                              href={
                                release.status === 'Published'
                                  ? release.files[0].downloadUrl
                                  : `/api/releases/${encodeURIComponent(release.id)}/download`
                              }
                              target="_blank"
                              rel="noreferrer"
                            >
                              {release.files[0].name} ({formatBytes(release.files[0].size)})
                            </a>
                          ) : (
                            '—'
                          )}
                        </td>
                        <td className="actions">
                          {release.status !== 'Published' ? (
                            <button
                              type="button"
                              className="btn btn-primary"
                              disabled={busy}
                              onClick={() =>
                                void run(async () => {
                                  await api.publish(release.id);
                                  await loadApps();
                                  await loadReleases(selected.id);
                                })
                              }
                            >
                              انتشار
                            </button>
                          ) : (
                            <button
                              type="button"
                              className="btn"
                              disabled={busy}
                              onClick={() =>
                                void run(async () => {
                                  await api.rollback(release.id);
                                  await loadApps();
                                  await loadReleases(selected.id);
                                })
                              }
                            >
                              بازگشت
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                    {releases.length === 0 ? (
                      <tr>
                        <td colSpan={9} className="empty">
                          هنوز نسخه‌ای ثبت نشده است.
                        </td>
                      </tr>
                    ) : null}
                  </tbody>
                </table>
              </div>
            </>
          ) : (
            <div className="center">برنامه‌ای انتخاب نشده است.</div>
          )}
        </main>
      </div>

      {showCreateApp ? (
        <CreateAppDialog
          busy={busy}
          onClose={() => setShowCreateApp(false)}
          onSubmit={(body) =>
            void run(async () => {
              const created = await api.createApp(body);
              await loadApps();
              setSelectedId(created.id);
              setShowCreateApp(false);
            })
          }
        />
      ) : null}

      {showCreateRelease && selected ? (
        <CreateReleaseDialog
          app={selected}
          busy={busy}
          onClose={() => setShowCreateRelease(false)}
          onSubmit={(body, file) =>
            void run(async () => {
              const created = await api.createRelease(body);
              if (file) {
                await api.uploadFile(created.id, file);
              }
              await loadReleases(selected.id);
              setShowCreateRelease(false);
            })
          }
        />
      ) : null}
    </div>
  );
}

function LoginScreen({ onLogin }: { onLogin: (user: SessionUser) => void }) {
  const [username, setUsername] = useState('admin');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      onLogin(await api.login(username));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'ورود ناموفق بود.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="login">
      <form onSubmit={submit} className="card">
        <h1>مرکز انتشار نسخه‌ها</h1>
        <p>ورود آزمایشی تا زمان اتصال SSO</p>
        {error ? <div className="banner is-error">{error}</div> : null}
        <label>
          نام کاربری
          <input value={username} onChange={(event) => setUsername(event.target.value)} />
        </label>
        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? '...' : 'ورود'}
        </button>
        <a className="sso" href="/api/auth/sso-login">
          ورود با SSO
        </a>
      </form>
    </div>
  );
}

function CreateAppDialog({
  busy,
  onClose,
  onSubmit,
}: {
  busy: boolean;
  onClose: () => void;
  onSubmit: (body: { id: string; name: string; platform: string; description?: string }) => void;
}) {
  const [id, setId] = useState('APP-');
  const [name, setName] = useState('');
  const [platform, setPlatform] = useState('Windows');
  const [description, setDescription] = useState('');

  return (
    <div className="modal" onClick={onClose}>
      <form
        className="card"
        onClick={(event) => event.stopPropagation()}
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit({ id, name, platform, description });
        }}
      >
        <h2>برنامه جدید</h2>
        <label>
          شناسه
          <input value={id} onChange={(event) => setId(event.target.value)} required />
        </label>
        <label>
          نام
          <input value={name} onChange={(event) => setName(event.target.value)} required />
        </label>
        <label>
          پلتفرم
          <select value={platform} onChange={(event) => setPlatform(event.target.value)}>
            <option value="Windows">ویندوز</option>
            <option value="Android">اندروید</option>
            <option value="Pwa">PWA</option>
            <option value="Service">سرویس</option>
          </select>
        </label>
        <label>
          توضیحات
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={3} />
        </label>
        <div className="row">
          <button type="button" className="btn" onClick={onClose}>
            انصراف
          </button>
          <button type="submit" className="btn btn-primary" disabled={busy}>
            ذخیره
          </button>
        </div>
      </form>
    </div>
  );
}

function CreateReleaseDialog({
  app,
  busy,
  onClose,
  onSubmit,
}: {
  app: Application;
  busy: boolean;
  onClose: () => void;
  onSubmit: (
    body: {
      applicationId: string;
      version: string;
      buildNumber: number;
      releaseNotes?: string;
      minimumVersion?: string;
      updateType: string;
      channel: string;
    },
    file?: File,
  ) => void;
}) {
  const [version, setVersion] = useState('');
  const [buildNumber, setBuildNumber] = useState('1');
  const [releaseNotes, setReleaseNotes] = useState('');
  const [minimumVersion, setMinimumVersion] = useState('');
  const [updateType, setUpdateType] = useState('Optional');
  const [channel, setChannel] = useState('Stable');
  const [file, setFile] = useState<File | undefined>();

  return (
    <div className="modal" onClick={onClose}>
      <form
        className="card wide"
        onClick={(event) => event.stopPropagation()}
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit(
            {
              applicationId: app.id,
              version,
              buildNumber: Number(buildNumber) || 0,
              releaseNotes,
              minimumVersion: minimumVersion || undefined,
              updateType,
              channel,
            },
            file,
          );
        }}
      >
        <h2>نسخه جدید — {app.name}</h2>
        <div className="grid">
          <label>
            نسخه
            <input value={version} onChange={(event) => setVersion(event.target.value)} placeholder="1.0.3" required />
          </label>
          <label>
            Build
            <input value={buildNumber} onChange={(event) => setBuildNumber(event.target.value)} />
          </label>
          <label>
            حداقل نسخه
            <input value={minimumVersion} onChange={(event) => setMinimumVersion(event.target.value)} placeholder="1.0.0" />
          </label>
          <label>
            نوع بروزرسانی
            <select value={updateType} onChange={(event) => setUpdateType(event.target.value)}>
              <option value="Optional">اختیاری</option>
              <option value="Mandatory">اجباری</option>
              <option value="Critical">بحرانی — مسدود کردن برنامه</option>
            </select>
          </label>
          <label>
            کانال
            <select value={channel} onChange={(event) => setChannel(event.target.value)}>
              <option value="Stable">Stable</option>
              <option value="Beta">Beta</option>
              <option value="Development">Development</option>
            </select>
          </label>
          {app.platform !== 'Pwa' ? (
            <label>
              فایل نصب
              <input type="file" onChange={(event) => setFile(event.target.files?.[0])} />
            </label>
          ) : null}
        </div>
        <label>
          توضیحات نسخه
          <textarea
            rows={5}
            value={releaseNotes}
            onChange={(event) => setReleaseNotes(event.target.value)}
            placeholder={'رفع مشکل نمایش نقشه\nبهبود سرعت درخواست‌ها'}
          />
        </label>
        <div className="row">
          <button type="button" className="btn" onClick={onClose}>
            انصراف
          </button>
          <button type="submit" className="btn btn-primary" disabled={busy}>
            ذخیره نسخه
          </button>
        </div>
      </form>
    </div>
  );
}
