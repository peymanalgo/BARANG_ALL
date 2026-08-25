# BARANG Git Workflow

This workflow keeps repository changes reviewable, validated, and recoverable. It applies together with `ARCHITECTURE.md`, `NAMING_CONVENTIONS.md`, and `CODEX.md`.

## Repository model

- The repository root is `D:\00_BARANG`.
- One Git repository contains `01_CORE`, `02_WEB_APP`, `03_MOB_APP`, and `04_Document`.
- The current implementation scope is only `01_CORE`. Do not modify a client or project-document area unless a later task explicitly includes it.
- The canonical remote is `https://github.com/peymanalgo/BARANG_ALL.git`; the expected primary branch is `main`.

## Main branch

- `main` represents the validated project state.
- Do not push broken, incomplete, or only partially validated implementation to `main`.
- Complete every task's required build, tests, and other checks before committing it.
- Review each completed phase before pushing it.
- Keep local commits until the user explicitly approves a push.

## Task execution

Treat each Roadmap task as one Codex task/prompt unless the user explicitly groups or splits tasks. For each task:

1. Inspect the current code, documentation, instructions, Git status, and relevant history before editing.
2. Implement only the requested scope and preserve unrelated user work.
3. Run the task's required build, tests, and infrastructure validation.
4. Inspect `git diff`, `git diff --check`, and `git status` for scope, secrets, and artifacts.
5. Stage only task-related files and inspect the staged diff.
6. Commit only after every required check passes.
7. Report changed files, decisions, actual validation results, status, commit hash, and unresolved issues before starting another task.

If validation fails, fix and rerun it. Never label an unexecuted or failing check as passed.

## Commits

- Make small, atomic, task-scoped commits. Do not mix refactoring, formatting, documentation, or unrelated user files into a task commit.
- Use Conventional Commits with a BARANG area as scope and an imperative lowercase summary:
  - `feat(core): add ...`
  - `fix(core): correct ...`
  - `chore(core): configure ...`
  - `docs(core): define ...`
  - `test(core): cover ...`
  - `refactor(core): simplify ...`
- Use the applicable client scope only after that client enters implementation, for example `feat(web): ...` or `fix(mobile): ...`.
- Never commit secrets, local `.env` files, build/test outputs, logs, editor state, or other generated artifacts unless a generated artifact is intentionally part of the requested deliverable.
- Never include accidental files from `04_Document` or temporary Office lock files such as `$*.docx`, `$*.xlsx`, and `$*.pptx`.

## Pushes and tags

- Codex must never push unless the user explicitly instructs it to push.
- Normal task completion produces one or more requested local commits only.
- Push only after explicit review and approval. At minimum, push after each completed and approved phase; push more frequently only when explicitly instructed.
- Do not create tags during ordinary tasks or phase completion.
- Create release tags only after explicit approval. Future release tags use Semantic Versioning as `vMAJOR.MINOR.PATCH`, for example `v1.0.0`; an approved prerelease may use `v1.0.0-rc.1`.

## Branches

- `main` is the primary branch. Keep the strategy simple and do not introduce GitFlow.
- Direct local task commits on `main` are acceptable while the project uses the explicit validation-and-review gate.
- Use a short-lived branch only when isolation, parallel work, or review makes it useful.
- Optional branch names use `<type>/<scope>-<short-description>` in lowercase kebab-case, for example `feat/core-order-api`, `fix/core-healthcheck`, or `docs/core-git-workflow`.
- Rebase or merge a short-lived branch according to the explicit review context; do not rewrite already shared history without approval.

## Conflicts and recovery

- Never force-push `main` as normal workflow and never rewrite shared history without explicit approval.
- Do not run destructive operations such as `git reset --hard`, `git clean`, or destructive checkout/restore commands unless the user explicitly approves the exact action and scope.
- Preserve unrelated modified or untracked files. Do not stage, discard, move, or reformat them.
- If unexpected modified or untracked files appear, stop and report them before touching them. Continue only when they are confirmed as known, unrelated files or the user provides direction.
- Resolve conflicts by understanding both sides and retaining intended work; do not choose a side mechanically. Rebuild and retest after resolution.

## Phase completion gate

A phase is eligible for review and push only when:

- every requested phase task is complete;
- the Release build passes;
- all applicable tests pass;
- Docker build/startup/health/endpoint validation passes when relevant;
- working-tree and staged diffs/status have been reviewed;
- no secrets, temporary files, or generated build/test artifacts are included;
- required architecture, operations, API, and user documentation is current; and
- warnings, skipped checks, and unresolved issues are explicitly reported.

Passing this gate makes a phase eligible for review; it does not authorize Codex to push or tag.

## Documents and generated artifacts

- Source documents may be versioned deliberately when they are an intended project deliverable.
- Temporary Office lock files (`$*.docx`, `$*.xlsx`, `$*.pptx`) must never be committed.
- Generated files may be committed only when intentionally required as a project deliverable and reviewed for reproducibility, size, licensing, and sensitive content.
- Do not modify or stage legitimate files under `04_Document` unless the task explicitly includes them.

## Permanent project safeguards

- Preserve the approved .NET 10, ASP.NET Core, PostgreSQL, modular-monolith, REST/OpenAPI, and Docker architecture. Architecture changes require explicit approval.
- BARANG supports `fa`, `en`, and `ar`: Persian and Arabic are RTL; English is LTR. Persian UI displays Jalali dates, while English and Arabic UI display Gregorian dates. Canonical persisted dates remain locale-independent.
- A modern, polished UI is a first-class requirement when client work begins; UI clients must not own or duplicate Core business rules.
- Before adopting an external or vendor dependency, review licensing, self-hostability, Iran accessibility risk, paid dependency risk, replaceability, maintenance health, and security.
- Critical runtime behavior must not require a mandatory foreign, cloud-only, paid service. Any exception requires explicit architectural and risk approval.
