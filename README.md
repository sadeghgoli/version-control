# Update Center

SSO → پنل React → Update API (ASP.NET Core) → SQLite/SQL Server + `storage/`

کلاینت‌ها فقط با `appId` نسخه را چک می‌کنند: Windows / Android / PWA / Service.

## اجرا

ترمینال ۱:

```bash
cd api
dotnet run --launch-profile http
```

API روی `http://localhost:5106`

ترمینال ۲:

```bash
cd panel
npm install
npm run dev
```

پنل روی `http://localhost:5174`

ورود آزمایشی تا زمان اتصال SSO: کاربر `admin`.

برای SSO، در `api/appsettings.json`:

```json
"Sso": {
  "Enabled": true,
  "AuthorizeUrl": "https://sso.example/authorize",
  "CallbackUrl": "http://localhost:5106/api/auth/callback",
  "ClientId": "update-center"
}
```

برای SQL Server:

```json
"Database": { "Provider": "SqlServer" }
```

## شناسه‌های اولیه

| appId | برنامه |
|---|---|
| `APP-137-WIN` | نرم‌افزار اپراتور ۱۳۷ |
| `APP-SABZEVAR-ANDROID` | سبزوار من – Android |
| `APP-SABZEVAR-PWA` | سبزوار من – PWA |
| `APP-CAR-ANDROID` | نرم‌افزار خودرو |
| `SERVICE-137-WIN` | سرویس ویندوزی ۱۳۷ |

## API کلاینت

```http
POST /api/update/check
{
  "appId": "APP-137-WIN",
  "platform": "windows",
  "version": "1.0.2",
  "build": 102
}
```

اگر `blockApplication: true` باشد (Critical یا پایین‌تر از MinimumVersion) کلاینت نباید وارد برنامه شود.

دانلود: `GET /api/update/{releaseId}/download`

## اتصال اپراتور ۱۳۷

در `apps/windows/.env`:

```
UPDATE_API_URL=http://localhost:5106
```

`appId` ثابت کلاینت: `APP-137-WIN`
