# Astro API Foundation (Phase-1)

This solution is a ready-to-run Phase-1 foundation for an AstroSage-like API platform:

✅ Register/Login (JWT)  
✅ Refresh token rotation (server-stored, hashed)  
✅ API key management (create/list/revoke)  
✅ API-key auth middleware + scope authorization  
✅ Rate limiting (partitioned by API key)  
✅ Usage logging (requests are recorded in DB)  
✅ First public endpoint: Planetary positions (placeholder engine)

> NOTE: The ephemeris engine is `placeholder-v1` (deterministic mock). Replace later with Swiss Ephemeris / real engine.

---

## Tech stack

- .NET 8 (ASP.NET Core)
- Swagger/OpenAPI
- SQL Server (MSSQL) + Dapper
  - Schema is auto-created at startup using an idempotent SQL script (`src/Astro.Api/Sql/sqlserver_init.sql`).

---

## How to run

1. Open `AstroApiFoundation.sln` in Visual Studio 2022.
2. Update `src/Astro.Api/appsettings.json`:
   - `Jwt:SigningKey` → set a long random secret (32+ chars).
3. Run `Astro.Api` project.

The API will:
- connect to your SQL Server using `Db:ConnectionString`
- auto-create tables at startup (idempotent)

Swagger: `/swagger`

Health: `/health`

---

## Step-by-step usage

### 1) Register
`POST /auth/register`

Body:
```json
{
  "email": "test@example.com",
  "password": "Pass@12345",
  "organizationName": "MyOrg"
}
```

Response includes:
- `accessToken` (JWT)
- `refreshToken` (opaque secret)

### 2) Create an API key
`POST /api-keys` with header: `Authorization: Bearer <accessToken>`

Body:
```json
{
  "name": "Local Dev Key",
  "scopes": ["ephemeris.read"]
}
```

Response includes:
- `secret` (shown once) in the format: `<prefix>.<secret>`

### 3) Call the public endpoint (API key auth)
`GET /v1/ephemeris/planet-positions?datetimeUtc=2026-01-30T00:00:00Z`

Header:
`X-Api-Key: <prefix>.<secret>`

You will get a deterministic response:
- `engine: placeholder-v1`
- planets with longitude/latitude/speed

---

## Notes / Next steps

- Replace `PlaceholderEphemerisService` with a real engine (Swiss Ephemeris).
- Add: IP allow-listing per API key, API key expiry, billing plans & quotas.
- Add: org-level dashboards (usage summaries from ApiUsageLogs).
