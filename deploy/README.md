# Deployment — mmfit.space

Production deployment for the FIT API, which also serves the MAKE ME FIT frontend.

| | |
|---|---|
| **Live site** | https://mmfit.space · https://www.mmfit.space |
| **Server** | Windows Server 2025, `38.247.143.33` |
| **Web server** | IIS site `MMFit.Web` (id 4), app pool `MMFitWeb` (No Managed Code, in-process) |
| **Physical path** | `C:\MMFit\sites\Web` |
| **Database** | PostgreSQL 16, `C:\PostgreSQL\16`, database `fit`, role `fit`, localhost only |
| **Trigger** | push to `main`, or manual **Run workflow** |
| **Certificate** | Let's Encrypt, auto-renewed by win-acme |

This server also hosts Healixa (`healixa.cloud`). The two deployments are fully
independent: separate folders, IIS sites, app pools, service accounts and GitHub
Actions runners. Nothing here touches Healixa.

---

## Architecture

The site is **not** static. `server/FitApi` is an ASP.NET Core 10 application that
serves both the JSON API and the frontend pages from a single origin — which is
what makes the auth cookie work. IIS hosts it in-process through
`AspNetCoreModuleV2`.

```
browser ──► IIS (:443, TLS)
              └─► MMFitWeb app pool ──► FitApi (in-process)
                                          ├─ /api/*        controllers + Identity
                                          ├─ /*.html       wwwroot (frontend)
                                          └─ /uploads/*    user avatars
                                                └─► PostgreSQL 127.0.0.1:5432
```

The repo's `docker-compose.yml` describes a different topology (api + postgres +
mailpit containers). That is the local-development path. Production on this VPS
runs the same code under IIS with a natively installed PostgreSQL, because IIS
already owns :80/:443 here and terminates TLS for the domain.

---

## How a deploy works

```
push to main
  ├─ job 1: test    (ubuntu-latest, GitHub-hosted)
  │    └─ dotnet test server/FitApi.sln     ← Testcontainers spins up postgres:16
  │                                            a failure blocks the deploy
  └─ job 2: deploy  (self-hosted runner on the VPS)  needs: test
       ├─ checkout
       ├─ dotnet publish            → C:\MMFit\staging\Web
       ├─ stage *.html + assets\    → C:\MMFit\staging\Web\wwwroot
       └─ deploy\Deploy-Site.ps1
            ├─ [2] pre-flight  FitApi.dll + web.config + wwwroot\index.html present?
            │    ▼▼▼ only from here do we touch production ▼▼▼
            ├─ [3] backup      sites\Web → backups\Web\<timestamp>   (last 5 kept)
            ├─ [4] offline     app_offline.htm + stop the App Pool
            ├─ [5] copy        robocopy /MIR staging → sites, with exclusions
            ├─ [6] online      start the App Pool, remove app_offline.htm
            ├─ [7] health      GET /api/health, 24 × 5s
            └─ [8] rollback    health failed → restore [3], fail the job
```

Steps up to [3] are safe — the live site keeps serving. Downtime is steps [4]–[6],
roughly 10–20 seconds.

### Why the tests run on a GitHub-hosted runner

13 of the 14 test classes use Testcontainers, which starts a real `postgres:16`
container per fixture. Docker is not installed on the production server and
installing it there purely to run tests would add a large attack surface to the
box serving the live site. GitHub's Ubuntu runners already have Docker, and this
repository is public, so those minutes are free. The hosted runner only runs
tests — it never touches the server, holds no credential and opens no port.

### Why the App Pool is stopped during the swap

The worker process holds `FitApi.dll` and its dependencies locked; they cannot be
overwritten while it runs. An earlier revision of this site was pure static HTML
and needed no offline window at all — that is no longer true.

---

## What actually gets deployed

The repository is ~212 MB. What ships is the publish output plus ~58 MB of frontend.

| Deployed | Why |
|---|---|
| `server/FitApi` publish output | the application |
| `*.html` (repo root) → `wwwroot\` | the pages |
| `assets/` → `wwwroot\assets\` | css, js, images, fonts |

| Never deployed | Why |
|---|---|
| `html/` | the purchased *Gymort* template, 160 MB. **The Dockerfile does copy this into its image**; nothing here does, because no page references it and publishing it would redistribute a licensed template. |
| `Source/` | 53 MB of source PDFs (Arabic nutrition / workout plans). Unreferenced; publishing them would expose them for direct download. |
| `docs/`, `.superpowers/`, `.playwright-mcp/` | development artefacts |

Verified 2026-07-29 against `origin/main`: no page or JS file references `html/`,
`Source/`, any `.pdf` or `.mp4`.

---

## Server-owned state — never deleted by a deploy

`Deploy-Site.ps1` excludes these from `robocopy /MIR`, and the workflow asserts
they still exist after every run.

| Path | Why it must survive |
|---|---|
| `uploads\` | user-uploaded profile avatars. Deleting these is unrecoverable. |
| `logs\` | ASP.NET Core Module stdout logs, written live by the worker |
| `.well-known\` | Let's Encrypt challenge root — see *The certificate* below |
| `appsettings.Production.json` | connection string, Google secret. Never in git. |

Exclusions are passed to robocopy **twice**, as a full path *and* as a bare name.
The full path alone is not enough: robocopy excludes the named directory but still
descends into it and marks its *children* as extras to delete, because the child's
path does not match the exclusion string. With only a full-path exclusion,
`uploads\avatars` would be deleted while `uploads` survived.

---

## Manual operations

All commands run on the server, from `C:\MMFit\src`.

```powershell
# See what a deploy would change, without changing anything
.\deploy\Deploy-Site.ps1 -DryRun

# Full manual deploy from the working tree (no GitHub needed)
git pull
.\deploy\Deploy-Site.ps1 -Publish

# Roll back
.\deploy\Deploy-Site.ps1 -Rollback                              # most recent
.\deploy\Deploy-Site.ps1 -Rollback -BackupName 20260729-100942   # a specific one
Get-ChildItem C:\MMFit\backups\Web -Directory | Sort-Object Name -Descending
```

**Rollback restores files, not schema.** The app applies its own EF migrations at
startup (`AppDbContext.Database.Migrate()` in `Program.cs`), so a bad migration
ships with a bad deploy and rolling the files back does not undo it. This differs
from the Healixa pipeline, where the database is strictly manual and the pipeline
never touches it. If a deploy fails after the app has booted once, check the
database state before redeploying.

---

## Configuration and secrets

`C:\MMFit\sites\Web\appsettings.Production.json` — not in git, never deployed.
`deploy/appsettings.Production.json.example` is the annotated template.

`ASPNETCORE_ENVIRONMENT=Production` is set **on the app pool**, not in web.config,
because `dotnet publish` regenerates web.config on every deploy. Without it the app
silently falls back to `appsettings.json`, whose defaults are the Docker hostnames
(`Host=db`, `Smtp=mail`), and fails to reach the database.

Generated credentials live in `C:\MMFit\secrets\` (Administrators only):
`postgres-credentials.txt`, `runner-credentials.txt`.

### Known gaps

- **Google sign-in is not configured.** `Authentication:Google:ClientId` /
  `ClientSecret` are still `REPLACE_ME`. The app boots regardless — `Program.cs`
  falls back to dummy values — but the Google button fails when clicked. Add the
  real values, and register `https://mmfit.space/signin-google` as an authorised
  redirect URI in Google Cloud Console.
- **Email does not send.** `Services/SmtpEmailSender.cs` connects with
  `SecureSocketOptions.None` and never calls `AuthenticateAsync`; it reads only
  `Smtp:Host`, `Smtp:Port` and `Smtp:From`. That suits the Mailpit dev container
  but cannot talk to Gmail, SendGrid, Mailgun or Office 365, all of which require
  STARTTLS and credentials — putting a password in config would have no effect,
  because nothing reads it. Email confirmation and password reset therefore do not
  deliver. Fixing it means either adding TLS + auth to `SmtpEmailSender`, or
  running a local relay that listens unauthenticated on 127.0.0.1 and forwards
  upstream with credentials.
- **`SQLitePCLRaw.lib.e_sqlite3` 2.1.11 has a known high-severity advisory**
  (NU1903, GHSA-2m69-gcr7-jv3q), pulled in by `Microsoft.EntityFrameworkCore.Sqlite`.
  Production uses Postgres, so the SQLite provider is never exercised, but the
  package still ships in the publish output. Bumping the EF Core Sqlite package
  clears it.

---

## The certificate

Issued and renewed by win-acme (`C:\Healixa\win-acme\wacs.exe`), shared with the
Healixa deployment. One scheduled task —
`win-acme renew (acme-v02.api.letsencrypt.org)` — renews **all** registered
certificates. Next renewal ~2026-09-22; expiry 2026-10-27.

Validation is HTTP-01 via the filesystem plugin. **Two things must stay true or
renewal silently fails ~60 days from now:**

1. `.well-known\acme-challenge\web.config` must exist. ACME tokens are
   *extensionless* files and IIS returns `404.3` for a file whose extension has no
   MIME mapping; that web.config maps any extension to `text/plain`, scoped to the
   challenge folder.
2. `.well-known` must never be deleted by a deploy.

```powershell
.\deploy\Request-Certificate.ps1            # re-issue
.\deploy\Request-Certificate.ps1 -Staging   # debug against the staging CA, no rate limit
```

---

## The runner

Self-hosted, registered to **this repository only**, labels
`self-hosted,windows,mmfit-prod`. Runs as a Windows service under the
**non-administrative** local account `MMFitRunner`, which has only:

| Path | Rights |
|---|---|
| `C:\MMFit\_work` | full control (its own workspace) |
| `C:\MMFit\staging`, `sites\Web`, `backups` | modify |
| `C:\MMFit\actions-runner` | read + execute |

It cannot start or stop the App Pool directly — a non-admin has no such right.
Instead two scheduled tasks run as SYSTEM and do exactly one fixed thing each:

```
MMFit-StopPool-Web   → appcmd stop  apppool /apppool.name:MMFitWeb
MMFit-StartPool-Web  → appcmd start apppool /apppool.name:MMFitWeb
```

The runner may *trigger* those two tasks and nothing else; it cannot edit them.
The permission is granted through the task's security descriptor **at registration
time**, via the COM scheduler API — `Register-ScheduledTask` has no SDDL parameter,
and the descriptor cannot be widened afterwards because
`HKLM\...\Schedule\TaskCache\Tree\<task>` is owned by SYSTEM and denies writes even
to Administrators. Verified 2026-07-29: `MMFitRunner` can drive the pool through
both tasks.

```powershell
Get-Service 'actions.runner.agamaldev-FIT.*'
Restart-Service 'actions.runner.agamaldev-FIT.*'
Get-ChildItem C:\MMFit\actions-runner\_diag | Sort-Object LastWriteTime -Descending | Select-Object -First 3
```

---

## Troubleshooting

**A deploy failed its health check.** It already rolled itself back and the job
failed loudly; the previous release is live. Check `C:\MMFit\sites\Web\logs\` —
`Deploy-Site.ps1` forces `stdoutLogEnabled="true"` into web.config on every
publish precisely so this is diagnosable. Without it ANCM returns a bare HTTP
500.30 and writes nothing anywhere, not even an Application event log entry.

**App Pool will not start / times out after 120s.** Almost always the app crashing
on startup: IIS rapid-fail protection then stops the pool, so the deploy sees
`Stopped` and rolls back. Read the stdout log. To see the exception directly:

```powershell
cd C:\MMFit\staging\Web
$env:ASPNETCORE_ENVIRONMENT='Production'; $env:ASPNETCORE_URLS='http://127.0.0.1:5099'
& 'C:\Program Files\dotnet\dotnet.exe' FitApi.dll
```

**Npgsql "The operation has timed out" on startup.** The TCP connect to Postgres
exceeded its window. Seen once on 2026-07-29 while the box was saturated (a
104 MB robocopy plus stray `dotnet` processes). The production connection string
carries `Timeout=30;Command Timeout=60` for headroom. Confirm the server is up
with `Get-Service postgresql-x64-16` and `psql -U fit -h 127.0.0.1 -d fit -c 'select 1'`.

**`mmfit.space` fails but `www.mmfit.space` works (or vice versa) when testing
from the server itself.** Almost always a stale Windows DNS cache on the server —
the domain moved IP on 2026-07-29 and the old value lingers. Use
`Clear-DnsClientCache`, or bypass DNS entirely:

```powershell
Invoke-WebRequest http://38.247.143.33/api/health -Headers @{Host='mmfit.space'} -UseBasicParsing
```

**Site returns Healixa's content.** A binding was lost. `Healixa.Web` holds a
catch-all `http://*:80:` binding, so any hostname without an exact match lands
there. Re-run `.\deploy\iis-setup.ps1` — it is idempotent.
