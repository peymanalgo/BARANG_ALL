# BARANG — Docker Infrastructure Audit & Handoff

**Purpose:** Give Z Code a strict, evidence-based checklist to inspect, document, and validate the existing BARANG Docker infrastructure before making any implementation changes.

**Repository:** https://github.com/peymanalgo/BARANG_ALL  
**Expected local root:** `D:\00_BARANG`  
**Core:** `D:\00_BARANG\01_CORE`  
**Documentation:** `D:\00_BARANG\04_Document`

> This document is an audit specification, not a claim that any particular container, port, volume, credential, or service is currently configured or running. Derive actual facts from the repository and the user's local environment.

## 1. Mandatory first step: audit only

Start with a **read-only audit**. Do not implement, refactor, upgrade, recreate, reset, or delete infrastructure until the user reviews the findings and explicitly approves the next step.

Do not:
- Run `docker compose down`, especially with `-v`.
- Delete, prune, recreate, or reset Docker volumes or database data.
- Run database resets, destructive migrations, or seed scripts that overwrite data.
- Change ports, service names, container names, networks, volume mappings, environment variables, secrets, or health checks.
- Upgrade base images, packages, Docker Compose, .NET, PostgreSQL, Redis, or MinIO as part of the audit.
- Commit, push, tag, force-push, or perform destructive Git operations.
- Print or copy secret values into reports. Report variable names and whether values are set, redacting credentials, tokens, connection strings, and keys.
- Assume the current checkout, Docker state, or documentation is up to date without checking.

Safe read-only commands may include `git status --short --branch`, `git log -5 --oneline`, `docker version`, `docker compose version`, `docker compose config --services`, `docker compose ps`, `docker ps -a`, `docker volume ls`, `docker network ls`, and targeted `docker inspect`/logs with secrets redacted. Before any command that might pull images, start/stop containers, alter data, or modify files, explain its effect and obtain approval if it is not clearly read-only.

## 2. Inspect the real repository

Inspect the current working tree and locate the actual infrastructure files. At minimum, check:

- `compose.yaml` and any Compose override files.
- Dockerfiles and build contexts for Core API and Backoffice.
- `.dockerignore` files.
- `.env.example`, sample environment files, configuration templates, and relevant application configuration.
- Health checks, entrypoints, initialization scripts, database migration/seed mechanisms, and service dependency definitions.
- Application connection strings and service endpoint configuration.
- Documentation mentioning Docker setup, startup, troubleshooting, backups, restore, or local development.
- Git status and recent commits to identify uncommitted or untracked infrastructure changes.

Do not assume file names, service names, ports, profiles, or paths. List what exists and identify what is absent.

## 3. Build an evidence-based service inventory

For every Compose service, report:

| Field | Required finding |
|---|---|
| Compose service name | Exact name from current Compose configuration |
| Purpose | What the service supports in BARANG |
| Image/build | Exact image reference or Dockerfile/build context |
| Current state | Running, stopped, unhealthy, absent, or unable to verify |
| Health | Health-check result and details, if configured |
| Ports | Host-to-container mappings; distinguish published ports from internal ports |
| Networks | Networks attached and communication paths |
| Volumes | Named/bind mounts and their purpose; do not expose sensitive host paths unnecessarily |
| Dependencies | `depends_on`, health conditions, and actual application dependency |
| Persistent data | Whether data persists across container replacement and where it resides |
| Configuration | Required environment-variable NAMES and whether they are set; redact values |
| Logs | Relevant startup/runtime errors, with secrets and personal data redacted |
| Evidence | Command/file and concise output or path supporting the finding |

Explicitly check for the presence and role of the following, **without presuming they are currently configured**:

- BARANG Core API / backend
- Blazor Backoffice, if containerized in the current implementation
- PostgreSQL
- Redis
- MinIO or another S3-compatible file service
- Any additional services discovered in Compose (for example, workflow, job processing, reverse proxy, or observability components)

Do not invent missing services. Distinguish “not configured”, “configured but stopped”, “running”, and “not verifiable”.

## 4. Docker Compose and networking

Inspect and document:

- Compose project name, profiles, service definitions, build contexts, image tags, restart policies, and startup dependencies.
- Health checks, their commands, intervals, timeouts, retries, and start periods.
- Host-published ports versus container-only ports.
- Which services can reach each other by Compose service DNS name.
- Whether any service unnecessarily exposes a database/cache/object store to all host interfaces.
- Any reverse proxy or TLS termination, if present.
- Whether application configuration uses container service names internally rather than `localhost` where appropriate.
- Whether port conflicts exist on the host.
- Whether the API and Backoffice are reachable from the intended browser URLs, if this can be verified safely.

Do not change bindings or firewall rules during the audit. Do not publish private URLs or credentials in the final report.

## 5. Persistence and recovery

For PostgreSQL, MinIO, and every other stateful service:

- Identify the exact named volumes or bind mounts.
- Explain which data each mount holds.
- Determine whether the mount is persistent across normal container recreation.
- Identify initialization scripts and whether they run only on first initialization.
- Identify the migration owner/process and when migrations are applied.
- Locate any documented backup and restore procedure.
- State clearly if backups are not configured or if recovery has not been tested.
- Check for risks such as anonymous volumes, container-only data directories, accidental volume replacement, or conflicting development/production settings.

Never inspect or output secret contents. Do not perform a backup, restore, migration, or volume change unless approved.

## 6. Environment variables and secrets

Create a configuration matrix containing:

- Variable name
- Which service uses it
- Whether it is required or optional according to code/configuration
- Whether it is defined in Compose/sample config
- Whether it appears to be set in the current environment (yes/no/unknown)
- Safe example or description, only when non-sensitive

Never print actual passwords, tokens, access keys, private URLs containing credentials, or full connection strings. Flag committed secrets or suspicious defaults without reproducing their values. Do not rotate secrets during this audit.

## 7. Application-level connectivity

Do not treat “container is running” as proof that BARANG works. Check, where safely possible:

- API health/readiness endpoint as defined by the project.
- API-to-PostgreSQL connectivity and migration state, without altering data.
- API-to-Redis connectivity if Redis is used by the current implementation.
- API-to-MinIO/S3 connectivity and bucket configuration if file storage is used.
- Backoffice-to-API reachability from the browser's network context.
- Whether CORS, base URLs, and environment-specific endpoints appear consistent.
- Whether logs show startup loops, failed connections, missing configuration, or failed health checks.

Use existing documented health endpoints. Do not invent endpoint paths. If testing requires starting a stopped service, changing state, or making a write request, report the limitation and ask for approval first.

## 8. Operational safety and expected behavior

Preserve these BARANG project rules:

- Keep required Docker Compose services running and accessible after an approved implementation phase, unless the user explicitly requests otherwise.
- Never run `docker compose down` or delete volumes as routine cleanup.
- Preserve database and object-storage data.
- Do not rely on Docker Hub or an external registry if the project/user has explicitly configured a local or restricted-image workflow; inspect the actual current setup and report its dependency.
- Avoid adding paid or region-restricted dependencies without approval.
- No infrastructure redesign or scope expansion during a read-only audit.
- No commit/push/tag without the user-approved workflow for the current task.

If the environment cannot be inspected from the current agent session, state that clearly and provide the exact read-only commands the user should run locally. Do not claim to have observed local Docker state unless actual command output was obtained.

## 9. Required deliverables from the audit

Produce a concise but complete report, preferably saved as:

`04_Document/Infrastructure/Docker_Infrastructure_Audit.md`

Do not create or overwrite that file without first checking whether the directory/file exists and preserving any existing user work. If a different documentation convention exists, follow it and report the chosen path.

The report must include:

1. Audit timestamp and machine/repository context, without exposing personal or secret data.
2. Git branch, working-tree status, and latest relevant commit.
3. Exact inventory of Compose files, Dockerfiles, services, images, ports, networks, volumes, profiles, and health checks.
4. Actual observed container status, clearly time-stamped.
5. Configuration-variable matrix with all secret values redacted.
6. Data persistence, backup/restore, and migration findings.
7. Connectivity and health-test results, including the exact checks performed.
8. Problems classified as:
   - **P0 — Data loss/security risk**
   - **P1 — Blocks reliable startup or core usage**
   - **P2 — Degraded functionality/operability**
   - **P3 — Documentation or maintainability**
9. Evidence for every important claim: file path, Compose service, command, or redacted log excerpt.
10. Recommended next steps ordered by risk and dependency, with no changes performed.
11. Explicit unknowns and checks that could not be performed.

Also return a short summary in the chat:
- What is configured?
- What is actually running and healthy?
- What is not configured or stopped?
- Are API, PostgreSQL, Redis, and MinIO/S3 connected where applicable?
- What is the highest-priority blocker?
- What approval is needed before any remediation?

## 10. Validation gates before any future infrastructure change

Do not proceed from audit to implementation until the user reviews the report and approves a specific scope.

For each approved remediation:
1. State the problem and evidence.
2. Explain the smallest safe change.
3. Identify expected impact on containers, ports, data, and availability.
4. Identify rollback/recovery steps.
5. Make one focused change.
6. Validate Compose configuration.
7. Validate build/startup and relevant health endpoints.
8. Validate data persistence without destructive testing.
9. Run relevant application and integration tests.
10. Report changed files, test evidence, current service state, and any remaining risk.

Do not declare success based only on a successful image build or a running container.

## 11. Copy/paste task for Z Code

Perform a **read-only Docker infrastructure audit** for the existing BARANG repository at `D:\00_BARANG` (or the actual checked-out root if different). Read this specification completely and follow it strictly.

First inspect the repository's real Compose files, Dockerfiles, configuration templates, application service endpoints, volumes, networks, health checks, and documentation. Then inspect current Docker/container state only if the local environment is available. Do not assume that documented services are configured or running; distinguish observed facts from assumptions.

Do not change code/configuration, start or stop services, pull images, run migrations, reset data, create/delete volumes, run `docker compose down`, or perform any other state-changing/destructive operation. Do not commit, push, tag, or perform destructive Git operations. Redact all credentials and secrets.

Create an evidence-based Markdown report following Section 9, after checking for existing documentation and preserving it. If local Docker state is inaccessible, say so and give the user the exact safe commands needed to collect it; do not fabricate results.

Finally, provide the required concise chat summary and **stop for user review and approval**. Do not implement any remediation yet.
