# BARANG Backoffice

## Structure

- `src/BARANG.Backoffice/Components`: localized pages, layouts, and small UI components.
- `src/BARANG.Backoffice/Api`: typed Core REST contracts and the central request pipeline.
- `src/BARANG.Backoffice/Auth`: cookie-session endpoints, route policy, and company flow.
- `src/BARANG.Backoffice/Localization`: `fa`, `en`, and `ar` resources and locale/date policy.
- `tests/BARANG.Backoffice.Tests`: stable behavior tests (no cosmetic snapshots).

This is a .NET 10 Blazor Web App using server rendering/Interactive Server support. It is a UI client only; Core remains authoritative for identity, RBAC, company access, and all business behavior. No third-party UI library was added. The only test dependencies are the same MIT/Apache-compatible Microsoft/xUnit packages already used by Core; they are self-hostable and do not add runtime or Iran-access dependencies.

## Run

Start Core on `http://localhost:8080`, then:

```powershell
dotnet run --project .\02_WEB_APP\src\BARANG.Backoffice\BARANG.Backoffice.csproj --launch-profile http
```

Backoffice listens on `http://localhost:5100`. Configure Core using `CoreApi:BaseUrl` in `appsettings.json` or `CoreApi__BaseUrl`. Core calls originate on the Backoffice server, so browser CORS is not required.

## Authentication and company context

Login calls `/api/v1/auth/login`, then `/auth/me` and `/auth/me/companies`. No companies shows a safe terminal state; one is confirmed automatically with `/auth/me/company`; multiple companies require an explicit selection. A posted choice is rechecked against the self-service company list and confirmed by Core. The typed API client adds the validated company as `X-Company-ID` centrally; the header is selection context only and Core validates access on every request.

The opaque Core token is placed inside ASP.NET Core's encrypted authentication ticket and sent only in an HttpOnly, SameSite=Strict session cookie. Its expiry matches Core's 30-minute expiry; it is never written to `localStorage`, `sessionStorage`, or browser logs. The token is decrypted and used only server-side. Limitations: the default Data Protection key ring must be persisted/shared for multi-instance deployments, and the encrypted ticket still contains bearer material. A future hardened deployment should use an ASP.NET Core server-side ticket store backed by a protected distributed cache so the browser cookie contains only an opaque Backoffice session key. HTTPS should set an always-secure cookie in production.

## Localization and dates

Persian (`fa`) is the default. The language cookie persists runtime selection. Persian and Arabic use RTL; English uses LTR, including layout and directional icons. All component text comes from `.resx` resources.

Future Persian date inputs/display/pickers use Jalali/Shamsi. English and Arabic use Gregorian. Core requests and storage remain canonical and locale-independent; Jalali values must never be persisted. Date conversion belongs in one future shared UI service, not individual pages.

## Docker

The existing `01_CORE/compose.yaml` runs PostgreSQL, Core, and Backoffice together. From `01_CORE`, run `docker compose up -d --build`. Backoffice uses the internal `http://api:8080` address and publishes only `127.0.0.1:5100`. Do not remove the existing PostgreSQL volume.

Known scope limitation: this phase intentionally contains only login, company resolution, protected home shell, localization, and logout; no business modules or full date components are included.
