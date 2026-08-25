# BARANG Identity and Access

The `Security` module owns internal users, roles, the controlled permission catalog, company access, company-scoped role assignments, credentials, sessions, security audit events, and the dormant external API-client boundary. Organizational units never authorize.

## Authentication and passwords

Internal clients use a self-hosted opaque bearer token. Login returns a random 256-bit token once; PostgreSQL stores only its SHA-256 hash, creation/expiry, and revocation metadata. Sessions expire after 30 minutes. Logout revokes the current session, and password reset revokes all active sessions. This supports backoffice, web, and mobile clients without a cloud identity dependency; future partner/client authentication remains separate.

Passwords use PBKDF2-HMAC-SHA256 with a random 128-bit salt, 210,000 iterations, a 256-bit derived key, versioned encoding, and fixed-time comparison. The minimum policy is 12 characters with uppercase, lowercase, and a digit. Login performs dummy hashing for unknown usernames and returns the same `security.login.invalid_credentials` result. Passwords, hashes, and tokens never appear in response contracts, logs, or audit data.

## RBAC evaluation

Permissions are a code-defined catalog persisted by migration. Codes are stable lowercase capabilities; administrators cannot invent executable permission codes. Roles are global technical definitions and assignments are company-scoped. Authorization requires, in order: authenticated internal actor, active user, active company access, current company, effective user-role period, active role, and role-permission mapping. Expected failures are 401 for missing/invalid authentication and 403 for insufficient authorization.

The selected `X-Company-ID` is a selection request, not proof. Authenticated requests validate active `UserCompany` access before establishing `CompanyContext`; Infrastructure then sets transaction-local PostgreSQL RLS context. Anonymous header selection remains Development/Testing-only for bootstrap/test compatibility and is rejected in Production.

## Bootstrap and administration

`POST /api/v1/security/bootstrap` works once, only while no users exist, and requires `X-Bootstrap-Secret` matching `SecurityBootstrap:Secret` of at least 24 characters. The secret comes from environment/secret configuration, has no source default, and is compared in fixed time. Bootstrap creates one internal administrator, company access, the system administrator role, all catalog permissions, and an audit event. Remove/disable the secret immediately afterward.

User, role, permission, assignment, company-access, password, login, and bootstrap changes create append-only `security_audit_events` with actor when known, target, company, UTC time, correlation ID, event code, and source channel. Secret material is excluded.

## Data and external clients

`application_users`, `roles`, `permissions`, `role_permissions`, `authentication_sessions`, and `api_clients` are controlled global identity tables. `user_companies`, `user_roles`, and `api_client_companies` are company-owned, query-filtered, and forced-RLS tables. Revocation ends effective mappings rather than deleting history.

`ApiClient` is separate from `ApplicationUser` and records identity, lifecycle, allowed companies, credential fingerprint/expiry metadata, and future rotation readiness. No client secret, partner API, OAuth server, B2C login, or mobile-specific login is activated in Phase 05.
