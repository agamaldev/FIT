# Migrating MAKE ME FIT from Firebase to ASP.NET Core + Postgres

- **Date:** 2026-06-23
- **Status:** Approved (design) — pending spec review → implementation plan
- **Author:** brainstorming session

## 1. Context & motivation

The MAKE ME FIT static site currently uses Firebase (Auth + Firestore + Storage)
for its user-accounts / personal-space feature. All access goes through two
global bridges in `assets/js/auth.js`: `window.FITAuth` (auth + user state) and
`window.FITData` (per-user data). Owner-only access is enforced by
`firestore.rules`; avatars live in Firebase Storage under `storage.rules`.

The owner wants to replace this backend-as-a-service with a **self-owned backend**:
an **ASP.NET Core (C#) Web API** backed by **PostgreSQL**, containerized with
**Docker**. This trades Firebase's zero-server convenience for full control over
the data, the schema, and the server-side logic.

Firebase was never wired to a live project (`firebase-config.js` still contains
`REPLACE_ME` placeholders), so **there is no existing user data to migrate** —
this is a greenfield schema.

## 2. Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Backend stack | ASP.NET Core Web API (C#) |
| 2 | Database | PostgreSQL (Docker container) |
| 3 | Runtime version | .NET 8 LTS |
| 4 | Data access | EF Core + Npgsql, code-first migrations |
| 5 | Auth methods | Email/Password (with email verification + password reset) **and** Google OAuth. Microsoft OAuth dropped. |
| 6 | Session mechanism | ASP.NET Core Identity + **httpOnly cookie** (`Secure`, `SameSite=Strict`) |
| 7 | Serving topology | **API serves the static site** (wwwroot) and `/api/*` from one origin |
| 8 | Avatar storage | Files on a mounted Docker volume, served at `/uploads/avatars/{id}` |
| 9 | Dev email | Mailpit container catches verification/reset mail (no real SMTP locally) |
| 10 | Data migration | None (greenfield) |

### Non-goals

- Microsoft OAuth (was in Firebase; intentionally deferred — can be added later
  via another external-login provider).
- Production hosting / cloud deployment (local Docker for now; prod SMTP and
  hosting are a later concern).
- Object storage (MinIO/S3) for avatars — a mounted volume is sufficient.
- Changing any of the 7 content pages' markup or behavior.

## 3. Architecture & topology

```
Browser ──HTTP──▶ ASP.NET Core API (one container) ──▶ Postgres (one container)
                   ├─ serves static site (wwwroot)         ▲
                   ├─ /api/* JSON endpoints                 │
                   └─ /uploads/* avatars (mounted volume)   │
                                                     Mailpit (dev email catcher)
```

- **Single origin.** The API serves the existing HTML/CSS/JS *and* exposes
  `/api/*`. Eliminates CORS and the long-standing `file://` breakage class
  (YouTube embeds, ES modules, fetch/CORS all break on `file://`). Dev URL:
  `http://localhost:8080`.
- **`docker-compose.yml`** with three services:
  - `db` — `postgres:16`, named volume for data, healthcheck.
  - `api` — the .NET app, depends on `db` (and `mail` in dev).
  - `mail` — `axllent/mailpit`, SMTP catcher with a web UI on `:8025` (dev only).

## 4. Data model (Firestore document → relational schema)

Firestore stored one nested `users/{uid}` document plus two growing
sub-collections. Postgres normalizes this into one table per concept, each child
keyed by `user_id` with `ON DELETE CASCADE`. Where the original object shape was
flexible, a typed dedup/display key is kept as a column and the full original
object is preserved in a `jsonb data` column (faithful to the document model and
leveraging Postgres `jsonb`).

| Table | Replaces | Shape |
|---|---|---|
| `AspNetUsers` (+ profile columns) | `users/{uid}.profile` | fully typed (see below) |
| `favorites` | `.favorites[]` | typed key + `jsonb data` |
| `nutrition_plans` | `.nutritionPlans[]` | typed key + `jsonb data` |
| `custom_plan_items` | `.customPlan[]` | fully typed (fixed shape) |
| `completed_dates` | `.completedDates[]` | fully typed |
| `calc_history` | `calcHistory/` subcoll | typed timestamp + `jsonb data` |
| `weight_log` | `weightLog/` subcoll | fully typed |

### `AspNetUsers` (extend Identity's `ApplicationUser`)

Identity provides `Id`, `Email`, `PasswordHash`, `EmailConfirmed`, etc. Add
profile columns (formerly `profile.*`):

- `DisplayName text`
- `PhotoUrl text null`
- `Weight numeric null`, `Height numeric null`
- `Goal text null`, `Activity text null`
- `UpdatedAt timestamptz`

### `favorites`

- `id bigserial PK`
- `user_id text FK → AspNetUsers(Id) ON DELETE CASCADE`
- `item_id text not null` — the original `fav.id` (dedup/toggle key)
- `type text not null default 'exercise'`
- `data jsonb not null` — full original favorite object
- `added_at timestamptz not null`
- unique `(user_id, item_id)`

### `nutrition_plans`

- `id bigserial PK`, `user_id text FK … CASCADE`
- `plan_id text not null` — `plan.id` or `${calId}-${varId}`
- `cal_id text`, `var_id text`
- `data jsonb not null` — full plan object
- `saved_at timestamptz not null`
- unique `(user_id, plan_id)`

### `custom_plan_items`

- `id bigserial PK`, `user_id text FK … CASCADE`
- `item_id text not null` — exercise id
- `name_ar text`, `name_en text`, `tab text`
- `sets int not null default 4`, `reps int not null default 10`
- `order_index int not null`
- unique `(user_id, item_id)`

### `completed_dates`

- `id bigserial PK`, `user_id text FK … CASCADE`
- `date date not null`
- unique `(user_id, date)`

### `calc_history`

- `id bigserial PK`, `user_id text FK … CASCADE`
- `data jsonb not null` — full result object
- `created_at timestamptz not null`

### `weight_log`

- `id bigserial PK`, `user_id text FK … CASCADE`
- `date date not null`
- `weight numeric not null` (validated 10–500)
- `waist numeric null`, `chest numeric null`, `arms numeric null`
- `created_at timestamptz not null`

### Owner isolation

`firestore.rules`' owner-only guarantee moves server-side: **every** authenticated
endpoint derives `user_id` from the auth cookie (never from the request body or
URL) and filters all queries by it. Proven by an explicit isolation test (§8).

## 5. Authentication

- **ASP.NET Core Identity** for the user store, password hashing, and built-in
  token providers (email-confirmation + password-reset tokens).
- **Google** via `Microsoft.AspNetCore.Authentication.Google`, linked into
  Identity's external-login flow (challenge → Google → callback → create/link
  user → issue cookie).
- **Session = httpOnly cookie.** `HttpOnly`, `Secure`, `SameSite=Strict`.
  Single-origin makes this both simpler and safer than JWT-in-localStorage
  (immune to XSS token theft). No bearer tokens in JS.
- **Email** via an `IEmailSender` abstraction → SMTP. Dev points at Mailpit;
  prod swaps in a real provider via config. Confirmation / reset links point at
  frontend pages (`confirm-email.html`, `reset-password.html`).
- **Arabic error messages:** the API returns stable error codes / problem-details;
  the frontend maps them to the same Arabic strings `errMessage()` produces today
  (mapping shifts from Firebase codes → API codes/HTTP statuses).

## 6. API surface

All under `/api`. All data endpoints require the auth cookie; `user_id` is taken
from the cookie. Signatures chosen to map 1:1 onto today's `FITAuth`/`FITData`
methods so the frontend's public bridge contract is unchanged.

**Auth** (`/api/auth`)
- `POST register` `{ name, email, password }` → creates user, sends verification
- `POST login` `{ email, password }` → sets cookie
- `POST logout` → clears cookie
- `GET  me` → current user (or 401) — replaces `onAuthStateChanged`
- `POST forgot-password` `{ email }` → sends reset link
- `POST reset-password` `{ token, email, password }`
- `GET  confirm-email?token&email`
- `GET  google` → challenge; `GET google/callback` → links/creates + cookie

**Profile** (`/api/profile`)
- `GET` → profile · `PUT` `{ displayName, weight, height, goal, activity }`
- `POST avatar` (multipart) → validates image type + ≤2 MB, stores on volume,
  saves `PhotoUrl`, returns URL

**Favorites** `GET /api/favorites` · `POST` (toggle, returns favorited bool) ·
`DELETE /api/favorites/{itemId}`

**Nutrition** `GET/POST /api/nutrition-plans` · `DELETE /api/nutrition-plans/{planId}`

**Calc history** `GET/POST /api/calc-history` · `DELETE /api/calc-history/{id}`

**Weight log** `GET/POST /api/weight-log` · `DELETE /api/weight-log/{id}`

**Workouts/streak** `GET /api/workouts/completed` ·
`POST /api/workouts/complete` (mark today) · `DELETE /api/workouts/complete`
(unmark today) — both return `{ streak, longestStreak, total }` computed
server-side (port of `_getStreakStats`).

**Custom plan** `GET /api/plan` · `POST /api/plan` (add) · `PUT /api/plan`
(reorder/replace) · `DELETE /api/plan/{itemId}` · `DELETE /api/plan` (clear)

## 7. Frontend changes (deliberately minimal)

The migration's low-risk core: **`window.FITAuth` and `window.FITData` keep the
exact same method names and signatures.** Only their internals change — Firebase
SDK calls → `fetch('/api/...', { credentials: 'include' })`.

- The **7 content pages and all inline `onclick` handlers are untouched.**
- **Rewrite only `assets/js/auth.js`:** drop the Firebase CDN imports; replace
  `onAuthStateChanged` with a `GET /api/auth/me` call on load that resolves the
  initial user then notifies listeners. The nav-injection + avatar UI code
  (`renderNavAuthUI`, `buildSlot`, etc.) is reused unchanged.
- **Add frontend pages** for `confirm-email.html` and `reset-password.html`
  (targets of the emailed links) and wire `auth.html`'s existing forms.
- **Delete:** `assets/js/firebase-config.js`, `firestore.rules`, `storage.rules`,
  `FIREBASE-SETUP.md`. Replace setup doc with a Postgres/Docker README.
- **Avatars:** stored on a mounted Docker volume, served at `/uploads/avatars/{id}`;
  same 2 MB + image-type validation, now in C#.

## 8. Testing

- **Backend, TDD** (xUnit + `WebApplicationFactory` + Testcontainers-Postgres):
  - Auth: register → (confirm) → login → me → logout; forgot/reset; Google
    callback (mocked external).
  - Each data endpoint: round-trip create/read/update/delete.
  - **Owner-isolation test:** user A cannot read or write user B's rows — the
    explicit replacement for `firestore.rules`.
  - Streak math: port + test `_getStreakStats` edge cases.
- **Frontend:** manual smoke over `http://localhost:8080` (with `file://` gone,
  YouTube embeds + modules also work in dev). Verify the existing
  `FIREBASE-SETUP.md` checklist items against the new backend.

## 9. Repo layout

```
FIT/
├─ (existing static site stays put: index.html, auth.html, profile.html,
│   assets/, html/, …) — plus new confirm-email.html, reset-password.html
├─ server/                       ← new ASP.NET Core project
│   ├─ Controllers/  Models/  Data/ (AppDbContext + Migrations/)  Services/
│   ├─ Program.cs  appsettings.json  appsettings.Development.json
│   └─ Dockerfile
└─ docker-compose.yml            ← db + api + mail
```

The API serves the existing site as its web root (mounted in dev, copied into the
image for prod), so current files don't move.

## 10. Cutover / build order (high level — detailed in the implementation plan)

1. Scaffold `server/` + docker-compose (`db`, `api`, `mail`); API serves a
   placeholder over `:8080`.
2. EF Core models + `AppDbContext` + initial migration; `dotnet ef` applies on
   startup in dev.
3. Identity + cookie auth + email/password endpoints + Mailpit email; tests.
4. Google external login; tests.
5. Data controllers (profile, favorites, nutrition, calc, weight, workouts,
   plan) + avatar upload; tests (incl. owner isolation).
6. Rewrite `auth.js` to call the API; add confirm/reset pages; delete Firebase
   files; manual smoke test.
7. Replace `FIREBASE-SETUP.md` with a Docker/Postgres run guide.

## 11. Open questions / future

- Microsoft OAuth can be re-added later as a second external provider.
- Production SMTP provider + hosting target TBD when deploying beyond local Docker.
- Optional later: object storage (MinIO/S3) if avatars outgrow a volume.
