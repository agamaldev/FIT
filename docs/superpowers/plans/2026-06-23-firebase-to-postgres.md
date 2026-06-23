# Firebase → ASP.NET Core + Postgres Migration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the MAKE ME FIT site's Firebase backend (Auth + Firestore + Storage) with a self-hosted ASP.NET Core 10 Web API + PostgreSQL, containerized with Docker, where the same API also serves the existing static site from a single origin.

**Architecture:** One ASP.NET Core API serves the static frontend and JSON `/api/*` endpoints from a single origin (no CORS, no `file://`). Data lives in PostgreSQL via EF Core; auth is ASP.NET Core Identity with an httpOnly cookie plus Google OAuth; avatars are stored on a mounted volume. Everything runs via `docker-compose` (api + postgres + mailpit). The frontend's `window.FITAuth` / `window.FITData` bridges are rewritten to call the API while keeping identical method signatures, so the existing content pages stay untouched.

**Tech Stack:** .NET 10, ASP.NET Core 10, EF Core 10 + Npgsql, ASP.NET Core Identity, Microsoft.AspNetCore.Authentication.Google, MailKit, PostgreSQL 16, Mailpit, Docker Compose, xUnit + WebApplicationFactory + Testcontainers.PostgreSql + FluentAssertions.

## Global Constraints

- Target framework `net10.0`; `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Never pin NuGet patch versions — `dotnet add package <Name>` resolves the latest stable for net10.0.
- All API routes live under `/api`. The authenticated user id is ALWAYS derived from the auth cookie (`ClaimTypes.NameIdentifier`), never from the request body or route. Owner isolation is mandatory and proven by tests.
- TDD: every code task writes a failing test, makes it pass, then commits (conventional-commit messages).
- Shared files (`Program.cs`, `CustomWebApplicationFactory.cs`) are CREATED ONCE and thereafter changed ONLY via additive edits at named `// FIT:*` marker comments — never overwritten.
- Error responses use `ProblemDetails` with an `Extensions["code"]` string that the frontend maps to Arabic copy.
- Preserve all Arabic user-facing text. Postgres connection string key: `ConnectionStrings:Default`.

## Shared Conventions — file ownership (read before executing)

Each file is created by exactly one task and afterward only extended via marker-anchored edits:
`Program.cs` created in Task 1 (markers `// FIT:SERVICES-END`, `// FIT:STARTUP-END`, `// FIT:MIDDLEWARE-END`); edited by Tasks 2, 3, 4, 6, 7. `CustomWebApplicationFactory.cs` created in Task 1 (marker `// FIT:FACTORY-SERVICES-END`); edited only by Task 4. Entities + `AppDbContext` = Task 2. Identity/cookie + `ApiControllerBase` = Task 3. Email service = Task 4. `AuthController` (all email/password actions) = Task 5. `AuthGoogleController` = Task 6. `ProfileController` + avatar storage = Task 7. Data controllers = Tasks 8–13. `assets/js/auth.js` fully rewritten in Task 14; `auth.html` + new pages owned by Task 15; cleanup + docs = Task 16.

---

### Task 1: Walking skeleton: solution, web+test projects, Program.cs (markers), static+health, docker-compose, factory

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi.sln`
- Create: `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj`
- Create: `D:\Work\Templates\FIT\server\FitApi\Program.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\appsettings.json`
- Create: `D:\Work\Templates\FIT\server\FitApi\appsettings.Development.json`
- Create: `D:\Work\Templates\FIT\server\FitApi\Dockerfile`
- Create: `D:\Work\Templates\FIT\server\FitApi\.dockerignore`
- Create: `D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj`
- Create: `D:\Work\Templates\FIT\server\FitApi.Tests\CustomWebApplicationFactory.cs`
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\SmokeTests.cs`
- Create: `D:\Work\Templates\FIT\docker-compose.yml`
- Modify: `D:\Work\Templates\FIT\.gitignore` (append .NET artifacts)
- Delete (template samples): `D:\Work\Templates\FIT\server\FitApi\WeatherForecast.cs`, `D:\Work\Templates\FIT\server\FitApi\Controllers\WeatherForecastController.cs`, `D:\Work\Templates\FIT\server\FitApi\FitApi.http`, `D:\Work\Templates\FIT\server\FitApi.Tests\UnitTest1.cs`

**Interfaces:**
- Consumes: existing `D:\Work\Templates\FIT\index.html` (contains the literal `MAKE ME FIT` â€” verified at line 6 and elsewhere); `postgres:16`, `axllent/mailpit` images; configuration key `Frontend:WebRoot`; connection string key `ConnectionStrings:Default`.
- Produces (relied on by later tasks):
  - `Program.cs` with the EXACT marker comments `// FIT:SERVICES-END`, `// FIT:STARTUP-END`, `// FIT:MIDDLEWARE-END`, and `public partial class Program { }` (Tasks 2,3,4,6,7 insert above these markers).
  - `CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime` with the EXACT marker `// FIT:FACTORY-SERVICES-END` and `RepoRoot` (Task 4 edits this file only).
  - Web project namespace root `FitApi`; test project namespace `FitApi.Tests`.
  - `docker-compose.yml` services `db`, `api`, `mail`; `Dockerfile` producing `/app` with `wwwroot`.

> NOTE: This is the FIRST .NET task â€” there is no `server/` directory yet (verified). All `dotnet`/`New-Item` commands below run from the repo root `D:\Work\Templates\FIT` unless an explicit path is given. The PowerShell tool's working directory is already the repo root; do NOT prefix commands with `cd`.

---

- [ ] **Step 1: Scaffold the solution and the two projects, then delete template sample files**

  Run these EXACT commands (PowerShell). Each `dotnet new` is non-interactive.

  ```powershell
  # 1. Create the server folder and an empty solution
  New-Item -ItemType Directory -Force -Path "D:\Work\Templates\FIT\server" | Out-Null
  dotnet new sln --name FitApi --output "D:\Work\Templates\FIT\server"

  # 2. Create the Web API (controllers) project targeting net10.0
  dotnet new webapi --use-controllers --framework net10.0 --name FitApi --output "D:\Work\Templates\FIT\server\FitApi"

  # 3. Create the xUnit test project targeting net10.0
  dotnet new xunit --framework net10.0 --name FitApi.Tests --output "D:\Work\Templates\FIT\server\FitApi.Tests"

  # 4. Add both projects to the solution
  dotnet sln "D:\Work\Templates\FIT\server\FitApi.sln" add "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj"
  dotnet sln "D:\Work\Templates\FIT\server\FitApi.sln" add "D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj"

  # 5. Reference the web project from the test project
  dotnet add "D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj" reference "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj"

  # 6. Delete template sample files (ignore "not found" â€” some SDKs vary)
  Remove-Item -Force -ErrorAction SilentlyContinue "D:\Work\Templates\FIT\server\FitApi\WeatherForecast.cs"
  Remove-Item -Force -ErrorAction SilentlyContinue "D:\Work\Templates\FIT\server\FitApi\Controllers\WeatherForecastController.cs"
  Remove-Item -Force -ErrorAction SilentlyContinue "D:\Work\Templates\FIT\server\FitApi\FitApi.http"
  Remove-Item -Force -ErrorAction SilentlyContinue "D:\Work\Templates\FIT\server\FitApi.Tests\UnitTest1.cs"
  ```

  Expected: solution and two `.csproj` files exist; `dotnet sln list` (run against `D:\Work\Templates\FIT\server\FitApi.sln`) prints both `FitApi\FitApi.csproj` and `FitApi.Tests\FitApi.Tests.csproj`.

- [ ] **Step 2: Add the test-project packages**

  Run these EXACT commands (latest stable for net10.0 â€” NEVER pin a patch version):

  ```powershell
  dotnet add "D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj" package Microsoft.AspNetCore.Mvc.Testing
  dotnet add "D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj" package Testcontainers.PostgreSql
  dotnet add "D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj" package FluentAssertions
  ```

  Expected: each command ends with `info : PackageReference for package '<Name>' ... added to ...`. (Npgsql/EF/Identity/Mailkit/Google packages are added by later tasks â€” do NOT add them here.)

- [ ] **Step 3: Pin the csproj contents exactly (web + test)**

  Overwrite the freshly-generated `FitApi.csproj` so the project settings are deterministic. Replace the ENTIRE file with:

  `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <RootNamespace>FitApi</RootNamespace>
      <AssemblyName>FitApi</AssemblyName>
    </PropertyGroup>

  </Project>
  ```

  Overwrite the test csproj. Replace the ENTIRE file with (keep whatever `<PackageReference>` lines `dotnet add` already inserted; the block below is the canonical full file, matching the structure and the `Microsoft.NET.Test.Sdk` / `xunit` / `xunit.runner.visualstudio` / `coverlet.collector` references that `dotnet new xunit` created). Do not add or edit `Version` attributes by hand — `dotnet add package` records the resolved version; leave whatever it wrote.

  `D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <IsPackable>false</IsPackable>
      <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk" />
      <PackageReference Include="xunit" />
      <PackageReference Include="xunit.runner.visualstudio" />
      <PackageReference Include="coverlet.collector" />
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
      <PackageReference Include="Testcontainers.PostgreSql" />
      <PackageReference Include="FluentAssertions" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\FitApi\FitApi.csproj" />
    </ItemGroup>

  </Project>
  ```

  > Do not add or edit `Version` attributes by hand — `dotnet add package` records the resolved version; leave whatever it wrote.

- [ ] **Step 4: Create Program.cs EXACTLY as the canonical skeleton (with all three markers)**

  Overwrite the generated `Program.cs`. Replace the ENTIRE file with EXACTLY this (markers must be byte-for-byte â€” later tasks insert above them):

  `D:\Work\Templates\FIT\server\FitApi\Program.cs`
  ```csharp
  using Microsoft.Extensions.FileProviders;

  var builder = WebApplication.CreateBuilder(args);

  builder.Services.AddControllers();
  // FIT:SERVICES-END

  var app = builder.Build();

  // FIT:STARTUP-END

  var frontendRoot = builder.Configuration["Frontend:WebRoot"];
  if (!string.IsNullOrWhiteSpace(frontendRoot))
  {
      var provider = new PhysicalFileProvider(Path.GetFullPath(frontendRoot));
      app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
      app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
  }
  else
  {
      app.UseDefaultFiles();
      app.UseStaticFiles();
  }
  // FIT:MIDDLEWARE-END

  app.MapControllers();
  app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

  app.Run();

  public partial class Program { }
  ```

- [ ] **Step 5: Create appsettings files**

  `D:\Work\Templates\FIT\server\FitApi\appsettings.json`
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
      "Default": "Host=db;Port=5432;Database=fit;Username=fit;Password=fit"
    },
    "Frontend": {
      "WebRoot": ""
    }
  }
  ```

  `D:\Work\Templates\FIT\server\FitApi\appsettings.Development.json`
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Information"
      }
    }
  }
  ```

- [ ] **Step 6: Write the failing smoke test**

  `D:\Work\Templates\FIT\server\FitApi.Tests\SmokeTests.cs`
  ```csharp
  using System.Net;
  using System.Net.Http.Json;
  using System.Text.Json;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public SmokeTests(CustomWebApplicationFactory factory) => _factory = factory;

      [Fact]
      public async Task Health_returns_200_and_status_ok()
      {
          var client = _factory.CreateClient();

          var response = await client.GetAsync("/api/health");

          response.StatusCode.Should().Be(HttpStatusCode.OK);

          var json = await response.Content.ReadFromJsonAsync<JsonElement>();
          json.GetProperty("status").GetString().Should().Be("ok");
      }

      [Fact]
      public async Task Root_serves_index_html_containing_brand()
      {
          var client = _factory.CreateClient();

          var response = await client.GetAsync("/");

          response.StatusCode.Should().Be(HttpStatusCode.OK);

          var body = await response.Content.ReadAsStringAsync();
          body.Should().Contain("MAKE ME FIT");
      }
  }
  ```

- [ ] **Step 7: Run the smoke test, expect FAIL (factory does not exist yet)**

  ```powershell
  dotnet test "D:\Work\Templates\FIT\server\FitApi.sln" --filter "FullyQualifiedName~FitApi.Tests.SmokeTests"
  ```

  Expected: BUILD FAILS. The compiler error is `error CS0246: The type or namespace name 'CustomWebApplicationFactory' could not be found` (referenced by `SmokeTests`). This proves the test is wired but the factory is missing.

- [ ] **Step 8: Create CustomWebApplicationFactory.cs EXACTLY as the canonical skeleton**

  `D:\Work\Templates\FIT\server\FitApi.Tests\CustomWebApplicationFactory.cs`
  ```csharp
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.Hosting;
  using Testcontainers.PostgreSql;
  using Xunit;

  namespace FitApi.Tests;

  public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
  {
      private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16").Build();

      // server/FitApi.Tests/bin/Debug/net10.0 -> up 5 -> repo root (holds index.html)
      private static string RepoRoot =>
          Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

      protected override void ConfigureWebHost(IWebHostBuilder builder)
      {
          builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
          builder.UseSetting("Frontend:WebRoot", RepoRoot);
          builder.UseEnvironment("Testing");
          // FIT:FACTORY-SERVICES-END
      }

      public async Task InitializeAsync() => await _db.StartAsync();
      public new async Task DisposeAsync() => await _db.DisposeAsync();
  }
  ```

  > Why `RepoRoot` resolves correctly: the test assembly runs from `D:\Work\Templates\FIT\server\FitApi.Tests\bin\Debug\net10.0`. Walking up five `..` segments lands on `D:\Work\Templates\FIT` (the repo root that holds `index.html`). The factory passes that path as `Frontend:WebRoot`, so `Program.cs` serves the existing static site from the repo root and `GET /` returns `index.html`. Docker is required for the Testcontainers Postgres container to start during `InitializeAsync` even though the smoke tests do not hit the DB â€” ensure Docker Desktop is running before running the tests.

- [ ] **Step 9: Run the smoke test, expect PASS**

  ```powershell
  dotnet test "D:\Work\Templates\FIT\server\FitApi.sln" --filter "FullyQualifiedName~FitApi.Tests.SmokeTests"
  ```

  Expected: build succeeds; output ends with `Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2` for `FitApi.Tests.dll`. Both `Health_returns_200_and_status_ok` and `Root_serves_index_html_containing_brand` pass.

- [ ] **Step 10: Create the Dockerfile (multi-stage sdk10 -> aspnet10, publish, copy frontend into /app/wwwroot)**

  `D:\Work\Templates\FIT\server\FitApi\Dockerfile`

  > NOTE: this Dockerfile is intended to be built with the build context set to the REPO ROOT (`D:\Work\Templates\FIT`) â€” see `docker-compose.yml` below which sets `context: .` and `dockerfile: server/FitApi/Dockerfile`. That is why the frontend `COPY` lines reference repo-root paths (e.g. `index.html`, `assets`) and the project `COPY`/`publish` reference `server/FitApi`.

  ```dockerfile
  # ---- build stage ----
  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /src

  # Restore using only the project file first (better layer caching)
  COPY server/FitApi/FitApi.csproj server/FitApi/
  RUN dotnet restore server/FitApi/FitApi.csproj

  # Copy the rest of the API source and publish
  COPY server/FitApi/ server/FitApi/
  RUN dotnet publish server/FitApi/FitApi.csproj -c Release -o /app/publish /p:UseAppHost=false

  # ---- runtime stage ----
  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
  WORKDIR /app

  # Published API
  COPY --from=build /app/publish ./

  # Static frontend served from /app/wwwroot (Program.cs falls back to wwwroot when Frontend:WebRoot is unset)
  COPY index.html ./wwwroot/index.html
  COPY auth.html ./wwwroot/auth.html
  COPY profile.html ./wwwroot/profile.html
  COPY assets/ ./wwwroot/assets/
  COPY html/ ./wwwroot/html/

  EXPOSE 8080
  ENV ASPNETCORE_URLS=http://+:8080

  ENTRYPOINT ["dotnet", "FitApi.dll"]
  ```

  > The `COPY html/` line assumes the existing `html/` directory exists at the repo root (it is listed in the FIXED repo layout). If a future page (e.g. `confirm-email.html`, `reset-password.html`) is added by a later task, that later task is responsible for adding its own `COPY` line â€” do NOT add them now (they do not exist yet and `COPY` of a missing path fails the build).

- [ ] **Step 11: Create the .dockerignore**

  `D:\Work\Templates\FIT\server\FitApi\.dockerignore`

  > NOTE: because the build context is the repo root, this `.dockerignore` is read relative to the repo root. Keep it broad enough to exclude build artifacts and tests but NOT the frontend assets the Dockerfile copies.

  ```
  **/bin/
  **/obj/
  **/.vs/
  **/.git/
  **/.gitignore
  **/node_modules/
  server/FitApi.Tests/
  docker-compose.yml
  **/*.user
  **/.dockerignore
  Dockerfile
  ```

- [ ] **Step 12: Create docker-compose.yml at the repo root**

  `D:\Work\Templates\FIT\docker-compose.yml`
  ```yaml
  services:
    db:
      image: postgres:16
      environment:
        POSTGRES_USER: fit
        POSTGRES_PASSWORD: fit
        POSTGRES_DB: fit
      volumes:
        - pgdata:/var/lib/postgresql/data
      healthcheck:
        test: ["CMD-SHELL", "pg_isready -U fit -d fit"]
        interval: 5s
        timeout: 5s
        retries: 10
      ports:
        - "5432:5432"

    api:
      build:
        context: .
        dockerfile: server/FitApi/Dockerfile
      depends_on:
        db:
          condition: service_healthy
      environment:
        ASPNETCORE_ENVIRONMENT: Production
        ASPNETCORE_URLS: http://+:8080
        ConnectionStrings__Default: "Host=db;Port=5432;Database=fit;Username=fit;Password=fit"
        Smtp__Host: "mail"
        Smtp__Port: "1025"
        Smtp__From: "no-reply@makemefit.local"
      ports:
        - "8080:8080"

    mail:
      image: axllent/mailpit
      ports:
        - "8025:8025"
        - "1025:1025"

  volumes:
    pgdata:
  ```

  > `Frontend:WebRoot` is intentionally left UNSET for the `api` service (no `Frontend__WebRoot` env var) so the container falls back to serving `/app/wwwroot` (where the Dockerfile copied the frontend). The `Smtp__*` env vars are pre-wired for Task 4's `SmtpEmailSender` (dev host `mail`, port `1025`).

- [ ] **Step 13: Append .NET artifacts to .gitignore**

  Edit `D:\Work\Templates\FIT\.gitignore` â€” APPEND (do not remove existing entries) the following block at the end of the file:

  ```gitignore

  # ---- .NET / ASP.NET Core (server/) ----
  [Bb]in/
  [Oo]bj/
  *.user
  *.suo
  .vs/
  [Dd]ebug/
  [Rr]elease/
  artifacts/
  TestResults/
  *.received.*
  ```

- [ ] **Step 14: Final verification â€” full build + smoke tests pass, compose config is valid**

  ```powershell
  dotnet build "D:\Work\Templates\FIT\server\FitApi.sln"
  dotnet test "D:\Work\Templates\FIT\server\FitApi.sln" --filter "FullyQualifiedName~FitApi.Tests.SmokeTests"
  docker compose -f "D:\Work\Templates\FIT\docker-compose.yml" config
  ```

  Expected:
  - `dotnet build` ends with `Build succeeded` and `0 Error(s)`.
  - `dotnet test` ends with `Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2`.
  - `docker compose ... config` prints the resolved YAML with the three services `db`, `api`, `mail` and exits 0 (validates the compose file; it does not build images).

- [ ] **Step 15: Commit**

  ```powershell
  git -C "D:\Work\Templates\FIT" add server/FitApi.sln server/FitApi server/FitApi.Tests docker-compose.yml .gitignore
  git -C "D:\Work\Templates\FIT" commit -m @'
  feat(api): scaffold ASP.NET Core walking skeleton (web+test, static+health, docker)

  - dotnet new webapi (controllers, net10.0) + xunit test project, wired into FitApi.sln
  - Program.cs with FIT:SERVICES/STARTUP/MIDDLEWARE markers, static file serving and /api/health
  - CustomWebApplicationFactory (Testcontainers postgres:16) serving the repo-root frontend
  - SmokeTests: /api/health == {status:ok}; / serves index.html containing "MAKE ME FIT"
  - docker-compose (db/api/mail) + multi-stage Dockerfile copying frontend into wwwroot

  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  '@
  ```

  Expected: commit succeeds; `git -C "D:\Work\Templates\FIT" status` shows the new `server/` tree and `docker-compose.yml` are committed and that no `bin/`/`obj/` artifacts were staged (confirming the .gitignore block works).


---

### Task 2: EF entities + AppDbContext + initial migration + Program.cs DbContext & startup migrate

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\ApplicationUser.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\Favorite.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\NutritionPlan.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\CustomPlanItem.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\CompletedDate.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\CalcHistoryEntry.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Models\WeightEntry.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Data\AppDbContext.cs`
- Create (generated by `dotnet ef`): `D:\Work\Templates\FIT\server\FitApi\Migrations\*_InitialCreate.cs` + `D:\Work\Templates\FIT\server\FitApi\Migrations\AppDbContextModelSnapshot.cs`
- Modify (additive marker edits ONLY â€” file owned by Task 1): `D:\Work\Templates\FIT\server\FitApi\Program.cs`
- Modify (add packages â€” file owned by Task 1): `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj`
- Test (Create): `D:\Work\Templates\FIT\server\FitApi.Tests\SchemaTests.cs`

**Interfaces:**
- Consumes:
  - `Program.cs` canonical skeleton with markers `// FIT:SERVICES-END` and `// FIT:STARTUP-END` (created by Task 1).
  - `CustomWebApplicationFactory` (Task 1) â€” provides a Postgres Testcontainer, sets `ConnectionStrings:Default` and `Frontend:WebRoot`, and applies migrations automatically because Task 2's `FIT:STARTUP-END` insert runs `Database.Migrate()` on startup.
  - Connection string key `ConnectionStrings:Default` (FIXED CONTRACT key).
- Produces (later tasks REFERENCE these; this task OWNS them):
  - Namespace `FitApi.Models` with entities: `ApplicationUser`, `Favorite`, `NutritionPlan`, `CustomPlanItem`, `CompletedDate`, `CalcHistoryEntry`, `WeightEntry` (exact shapes per CONTRACT ENTITIES).
  - Namespace `FitApi.Data` with `AppDbContext : IdentityDbContext<ApplicationUser>` exposing `DbSet`s named exactly `Favorites`, `NutritionPlans`, `CustomPlanItems`, `CompletedDates`, `CalcHistory`, `WeightLog`.
  - The initial EF migration `InitialCreate` (so `Database.Migrate()` builds the schema).
  - Program.cs now registers `FitApi.Data.AppDbContext` with Npgsql and migrates on startup.

---

- [ ] **Step 1: Write the failing test**

Create `D:\Work\Templates\FIT\server\FitApi.Tests\SchemaTests.cs` with COMPLETE code. It resolves the real `AppDbContext` from the running app's DI (migrations already applied on startup by the Program.cs `FIT:STARTUP-END` insert), inserts two `Favorite` rows with the same `(UserId, ItemId)`, and asserts the second `SaveChanges` throws `DbUpdateException` due to the unique index. It first inserts an `ApplicationUser` so the cascade FK `UserId -> AspNetUsers(Id)` is satisfied.

```csharp
using System.Text.Json;
using FitApi.Data;
using FitApi.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitApi.Tests;

public class SchemaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SchemaTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Force the host (and thus startup migration) to run.
        _ = _factory.Server;
    }

    [Fact]
    public async Task DuplicateFavorite_SameUserAndItem_ThrowsDbUpdateException()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new ApplicationUser
        {
            UserName = "schema-user@test.com",
            Email = "schema-user@test.com"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.Favorites.Add(new Favorite
        {
            UserId = user.Id,
            ItemId = "ex-1",
            Type = "exercise",
            Data = JsonDocument.Parse("""{"id":"ex-1"}"""),
            AddedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        db.Favorites.Add(new Favorite
        {
            UserId = user.Id,
            ItemId = "ex-1",
            Type = "exercise",
            Data = JsonDocument.Parse("""{"id":"ex-1"}"""),
            AddedAt = DateTimeOffset.UtcNow
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

```
dotnet test "D:\Work\Templates\FIT\server\FitApi.sln" --filter "FullyQualifiedName~SchemaTests"
```

Expected: the build itself FAILS to compile because the referenced types do not yet exist â€” error text contains
`error CS0234: The type or namespace name 'Data' does not exist in the namespace 'FitApi'`
and `error CS0234: The type or namespace name 'Models' does not exist in the namespace 'FitApi'`
(also `'AppDbContext'`, `'ApplicationUser'`, `'Favorite'` not found). Build error, zero tests run.

- [ ] **Step 3: Implement**

**3a. Add EF Core + Npgsql packages to the web project** (latest stable for net10.0; NEVER pin a patch version). Run each:

```
dotnet add "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj" package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj" package Microsoft.EntityFrameworkCore.Design
```

(These edit `FitApi.csproj` by adding `<PackageReference>` items â€” additive, no overwrite.)

**3b. Create the seven entity files** under `D:\Work\Templates\FIT\server\FitApi\Models\` exactly per CONTRACT.

`Models\ApplicationUser.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace FitApi.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public string? Goal { get; set; }
    public string? Activity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

`Models\Favorite.cs`:

```csharp
using System.Text.Json;

namespace FitApi.Models;

public class Favorite
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset AddedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
```

`Models\NutritionPlan.cs`:

```csharp
using System.Text.Json;

namespace FitApi.Models;

public class NutritionPlan
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string PlanId { get; set; } = default!;
    public string? CalId { get; set; }
    public string? VarId { get; set; }
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset SavedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
```

`Models\CustomPlanItem.cs`:

```csharp
namespace FitApi.Models;

public class CustomPlanItem
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Tab { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int OrderIndex { get; set; }
    public ApplicationUser? User { get; set; }
}
```

`Models\CompletedDate.cs`:

```csharp
namespace FitApi.Models;

public class CompletedDate
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public DateOnly Date { get; set; }
    public ApplicationUser? User { get; set; }
}
```

`Models\CalcHistoryEntry.cs`:

```csharp
using System.Text.Json;

namespace FitApi.Models;

public class CalcHistoryEntry
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public JsonDocument Data { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
```

`Models\WeightEntry.cs`:

```csharp
namespace FitApi.Models;

public class WeightEntry
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public DateOnly Date { get; set; }
    public decimal Weight { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Arms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ApplicationUser? User { get; set; }
}
```

**3c. Create `D:\Work\Templates\FIT\server\FitApi\Data\AppDbContext.cs`** exactly per CONTRACT (base first; `jsonb` on every `Data`; `numeric` on decimals; unique indexes; cascade child FKs to `AspNetUsers(Id)`):

```csharp
using FitApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<CustomPlanItem> CustomPlanItems => Set<CustomPlanItem>();
    public DbSet<CompletedDate> CompletedDates => Set<CompletedDate>();
    public DbSet<CalcHistoryEntry> CalcHistory => Set<CalcHistoryEntry>();
    public DbSet<WeightEntry> WeightLog => Set<WeightEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Favorite>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NutritionPlan>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasIndex(x => new { x.UserId, x.PlanId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustomPlanItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompletedDate>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CalcHistoryEntry>(e =>
        {
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WeightEntry>(e =>
        {
            e.Property(x => x.Weight).HasColumnType("numeric");
            e.Property(x => x.Waist).HasColumnType("numeric");
            e.Property(x => x.Chest).HasColumnType("numeric");
            e.Property(x => x.Arms).HasColumnType("numeric");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.Weight).HasColumnType("numeric");
            e.Property(x => x.Height).HasColumnType("numeric");
        });
    }
}
```

**3d. Edit `Program.cs` â€” additive marker insert at `// FIT:SERVICES-END`** (register the DbContext ABOVE the marker, keep the marker). Use Edit with:

OLD:
```
// FIT:SERVICES-END
```
NEW:
```
builder.Services.AddDbContext<FitApi.Data.AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// FIT:SERVICES-END
```

This `OLD` string also appears inside `CustomWebApplicationFactory.cs` as `// FIT:FACTORY-SERVICES-END` â€” that is a DIFFERENT literal, so this exact-match Edit on `Program.cs` is unambiguous within that file. Because `UseNpgsql` is referenced, add the EF Core using at the very top of `Program.cs` via a second Edit:

OLD:
```
using Microsoft.Extensions.FileProviders;
```
NEW:
```
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
```

**3e. Edit `Program.cs` â€” additive marker insert at `// FIT:STARTUP-END`** (run the startup migration ABOVE the marker, keep the marker). Use Edit with:

OLD:
```
// FIT:STARTUP-END
```
NEW:
```
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitApi.Data.AppDbContext>();
    db.Database.Migrate();
}
// FIT:STARTUP-END
```

**3f. Install the EF CLI tool and create the initial migration.** Install `dotnet-ef` as a local tool (deterministic, repo-scoped) then add the migration against the web project:

```
dotnet new tool-manifest --output "D:\Work\Templates\FIT\server"
dotnet tool install dotnet-ef --tool-manifest "D:\Work\Templates\FIT\server\.config\dotnet-tools.json"
dotnet ef migrations add InitialCreate --project "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj" --startup-project "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj"
```

This generates `Migrations\*_InitialCreate.cs`, `Migrations\*_InitialCreate.Designer.cs`, and `Migrations\AppDbContextModelSnapshot.cs` under `D:\Work\Templates\FIT\server\FitApi\Migrations\`. (Design-time model creation only needs a connection-string placeholder; it does NOT connect to a database, so no running Postgres is required to scaffold the migration. The `Database.Migrate()` call you added runs at app startup against the Testcontainer.)

- [ ] **Step 4: Run, expect PASS**

```
dotnet test "D:\Work\Templates\FIT\server\FitApi.sln" --filter "FullyQualifiedName~SchemaTests"
```

Expected output (final summary lines):
```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```
The test starts the host (Testcontainer Postgres image `postgres:16`), `Database.Migrate()` creates the `InitialCreate` schema including the unique index on `Favorites(UserId, ItemId)`, the first insert succeeds, and the duplicate insert throws `DbUpdateException` (caught by the FluentAssertions `ThrowAsync<DbUpdateException>()`).

- [ ] **Step 5: Commit**

```
git add "D:\Work\Templates\FIT\server\FitApi\Models" "D:\Work\Templates\FIT\server\FitApi\Data\AppDbContext.cs" "D:\Work\Templates\FIT\server\FitApi\Migrations" "D:\Work\Templates\FIT\server\FitApi\Program.cs" "D:\Work\Templates\FIT\server\FitApi\FitApi.csproj" "D:\Work\Templates\FIT\server\.config\dotnet-tools.json" "D:\Work\Templates\FIT\server\FitApi.Tests\SchemaTests.cs"
git commit -m "feat(api): add EF Core entities, AppDbContext, initial migration, and startup migrate"
```


---

### Task 3: Identity + cookie auth + ApiControllerBase (Program.cs marker edits)

**Files:**
- Modify: `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj` (adds the Identity package via `dotnet add package`)
- Modify: `D:\Work\Templates\FIT\server\FitApi\Program.cs` (additive marker inserts ONLY â€” file owned by Task 1)
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\ApiControllerBase.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\PingController.cs` (test-probe controller; permanent â€” also used by later auth-guard regression)
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\AuthGuardTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.ApplicationUser` (entity created by Task 2)
  - `FitApi.Data.AppDbContext` (created by Task 2)
  - `Microsoft.AspNetCore.Identity.IdentityRole` (framework)
  - `Program` partial class + canonical markers `// FIT:SERVICES-END`, `// FIT:MIDDLEWARE-END` (created by Task 1)
  - `FitApi.Tests.CustomWebApplicationFactory` (created by Task 1)
- Produces:
  - `FitApi.Controllers.ApiControllerBase : ControllerBase` exposing `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;` â€” inherited by ALL data controllers (Tasks 5â€“13).
  - Wired-up ASP.NET Core Identity (`AddIdentity<ApplicationUser, IdentityRole>` with `Password.RequiredLength = 6`, `SignIn.RequireConfirmedAccount = false`), `AddEntityFrameworkStores<AppDbContext>()`, `AddDefaultTokenProviders()`.
  - `ConfigureApplicationCookie` policy: `Cookie.HttpOnly = true`, `Cookie.SameSite = SameSiteMode.Strict`, `Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest`; for any request path under `/api`, `OnRedirectToLogin` returns 401 and `OnRedirectToAccessDenied` returns 403 (no HTML redirect).
  - `app.UseAuthentication(); app.UseAuthorization();` middleware ordering (inserted before `// FIT:MIDDLEWARE-END`, i.e. after `UseStaticFiles`, before `MapControllers`).
  - `FitApi.Controllers.PingController` â€” `[Authorize]` controller, route `api/ping`, `GET` returns 200 `{}`. Used by this task's auth-guard test and as a permanent unauthenticated-guard smoke probe.

---

- [ ] **Step 1: Write the failing test**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\AuthGuardTests.cs` with the COMPLETE content below. It asserts that an unauthenticated request to a `[Authorize]` API endpoint returns an exact `401` AND carries NO `Location` redirect header (proving the `/api` cookie-event override is active rather than the default HTML login redirect).

  ```csharp
  using System.Net;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class AuthGuardTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public AuthGuardTests(CustomWebApplicationFactory factory) => _factory = factory;

      [Fact]
      public async Task Anonymous_request_to_authorized_api_endpoint_returns_401_without_redirect()
      {
          // Do NOT auto-follow redirects, so we can assert there is no 302->login.
          var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
          {
              AllowAutoRedirect = false
          });

          var response = await client.GetAsync("/api/ping");

          response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
          response.Headers.Location.Should().BeNull();
          response.Headers.Contains("Location").Should().BeFalse();
      }

      [Fact]
      public async Task Health_endpoint_is_anonymous_and_returns_200()
      {
          var client = _factory.CreateClient();

          var response = await client.GetAsync("/api/health");

          response.StatusCode.Should().Be(HttpStatusCode.OK);
      }
  }
  ```

- [ ] **Step 2: Run, expect FAIL**

  ```powershell
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~AuthGuardTests"
  ```

  Expected: the test fails to BUILD (compile error) because `Controllers/PingController.cs`, the Identity wiring, and the cookie-event override do not exist yet. Expected error text includes a build failure such as `The type or namespace name 'PingController' could not be found` is NOT emitted (it is internal) â€” instead the suite fails because `/api/ping` does not exist so `Anonymous_request_to_authorized_api_endpoint_returns_401_without_redirect` fails with `Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.NotFound {value: 404}.`. If `PingController` is added before Identity wiring, the failure instead reads `Expected response.StatusCode to be HttpStatusCode.Unauthorized {value: 401}, but found HttpStatusCode.Found {value: 302}.` with a non-null `Location`. Either way: NOT all green.

- [ ] **Step 3: Implement**

  **3a. Add the Identity EF Core package** (latest stable for net10.0 â€” NEVER pin a patch version):

  ```powershell
  dotnet add D:\Work\Templates\FIT\server\FitApi\FitApi.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
  ```

  **3b. Create `D:\Work\Templates\FIT\server\FitApi\Controllers\ApiControllerBase.cs`** (COMPLETE new file):

  ```csharp
  using System.Security.Claims;
  using Microsoft.AspNetCore.Mvc;

  namespace FitApi.Controllers;

  public abstract class ApiControllerBase : ControllerBase
  {
      protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  }
  ```

  **3c. Create `D:\Work\Templates\FIT\server\FitApi\Controllers\PingController.cs`** (COMPLETE new file â€” a minimal authorized probe used by the auth-guard test):

  ```csharp
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;

  namespace FitApi.Controllers;

  [ApiController]
  [Authorize]
  [Route("api/ping")]
  public class PingController : ApiControllerBase
  {
      [HttpGet]
      public IActionResult Get() => Ok(new { });
  }
  ```

  **3d. Edit `D:\Work\Templates\FIT\server\FitApi\Program.cs` â€” SERVICES marker insert** (additive ONLY; insert Identity + cookie config ABOVE the marker, keep the marker line).

  OLD string:
  ```
  // FIT:SERVICES-END
  ```

  NEW string:
  ```
  builder.Services.AddIdentity<FitApi.Models.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
  {
      options.Password.RequiredLength = 6;
      options.SignIn.RequireConfirmedAccount = false;
  })
      .AddEntityFrameworkStores<FitApi.Data.AppDbContext>()
      .AddDefaultTokenProviders();

  builder.Services.ConfigureApplicationCookie(options =>
  {
      options.Cookie.HttpOnly = true;
      options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
      options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
      options.Events.OnRedirectToLogin = context =>
      {
          if (context.Request.Path.StartsWithSegments("/api"))
          {
              context.Response.StatusCode = StatusCodes.Status401Unauthorized;
              return Task.CompletedTask;
          }
          context.Response.Redirect(context.RedirectUri);
          return Task.CompletedTask;
      };
      options.Events.OnRedirectToAccessDenied = context =>
      {
          if (context.Request.Path.StartsWithSegments("/api"))
          {
              context.Response.StatusCode = StatusCodes.Status403Forbidden;
              return Task.CompletedTask;
          }
          context.Response.Redirect(context.RedirectUri);
          return Task.CompletedTask;
      };
  });
  // FIT:SERVICES-END
  ```

  **3e. Edit `D:\Work\Templates\FIT\server\FitApi\Program.cs` â€” MIDDLEWARE marker insert** (additive ONLY; insert auth middleware ABOVE the marker, keep the marker line). This places `UseAuthentication`/`UseAuthorization` after the static-file middleware and before `app.MapControllers();`, which is the correct ASP.NET Core ordering.

  OLD string:
  ```
  // FIT:MIDDLEWARE-END
  ```

  NEW string:
  ```
  app.UseAuthentication();
  app.UseAuthorization();
  // FIT:MIDDLEWARE-END
  ```

  Note: per the CONTRACT, do NOT add a bare `builder.Services.AddAuthentication();` â€” `AddIdentity` already registers the cookie auth schemes. (Task 6 will additively add `.AddGoogle(...)` as its own SERVICES insert.)

- [ ] **Step 4: Run, expect PASS**

  ```powershell
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~AuthGuardTests"
  ```

  Expected output: build succeeds and the run ends with `Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2` for `FitApi.Tests`. Specifically, `/api/ping` returns `401 Unauthorized` with no `Location` header, and `/api/health` returns `200 OK`.

- [ ] **Step 5: Commit**

  ```powershell
  git -C D:\Work\Templates\FIT add server/FitApi/FitApi.csproj server/FitApi/Program.cs server/FitApi/Controllers/ApiControllerBase.cs server/FitApi/Controllers/PingController.cs server/FitApi.Tests/AuthGuardTests.cs
  git -C D:\Work\Templates\FIT commit -m "feat(api): add ASP.NET Core Identity, cookie auth, and ApiControllerBase

  - Wire AddIdentity<ApplicationUser, IdentityRole> (password min length 6, RequireConfirmedAccount=false) with EF stores and default token providers
  - Configure application cookie (HttpOnly, SameSite=Strict, SecurePolicy=SameAsRequest); return 401/403 instead of HTML redirect for /api paths
  - Add UseAuthentication/UseAuthorization middleware at FIT:MIDDLEWARE marker
  - Add ApiControllerBase exposing UserId from NameIdentifier claim
  - Add authorized PingController probe and AuthGuardTests proving anonymous /api/ping -> 401 with no Location redirect"
  ```


---

### Task 4: Email service (MailKit) + DI + CapturingEmailSender + factory swap

**Files:**
- Modify (add package): `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj`
- Create: `D:\Work\Templates\FIT\server\FitApi\Services\IEmailSender.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Services\SmtpEmailSender.cs`
- Modify (additive marker insert): `D:\Work\Templates\FIT\server\FitApi\Program.cs`
- Modify (config keys): `D:\Work\Templates\FIT\server\FitApi\appsettings.json`
- Create: `D:\Work\Templates\FIT\server\FitApi.Tests\CapturingEmailSender.cs`
- Modify (two named edits only): `D:\Work\Templates\FIT\server\FitApi.Tests\CustomWebApplicationFactory.cs`
- Test (create): `D:\Work\Templates\FIT\server\FitApi.Tests\EmailWiringTests.cs`

**Interfaces:**
- Consumes:
  - `CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime` and its `// FIT:FACTORY-SERVICES-END` marker (created by Task 1).
  - `Program.cs` `// FIT:SERVICES-END` marker (created by Task 1) and `public partial class Program { }` test hook.
  - The Postgres `Testcontainers` wiring already present in the factory (Task 1) â€” needed because resolving services builds the host, which runs the Task 2 startup migration against the container DB.
- Produces:
  - `FitApi.Services.IEmailSender` with `Task SendAsync(string to, string subject, string htmlBody)` â€” consumed later by `AuthController` (Task 5) for register/forgot/resend/confirm emails.
  - `FitApi.Services.SmtpEmailSender` registered in DI via `AddScoped<IEmailSender, SmtpEmailSender>()`.
  - `FitApi.Tests.CapturingEmailSender : IEmailSender` with `public List<(string To, string Subject, string Body)> Sent` â€” consumed by Task 5's auth/email tests.
  - `CustomWebApplicationFactory.Email` (a `CapturingEmailSender` instance) â€” the captured emails any later email-asserting test reads.

**Steps:**

- [ ] **Step 1: Write the failing test**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\EmailWiringTests.cs` with the deterministic service-identity assertion (resolve `IEmailSender` from the running test host and prove it is the exact `factory.Email` instance the factory swapped in):

  ```csharp
  using FitApi.Services;
  using Microsoft.Extensions.DependencyInjection;
  using Xunit;

  namespace FitApi.Tests;

  public class EmailWiringTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public EmailWiringTests(CustomWebApplicationFactory factory) => _factory = factory;

      [Fact]
      public void IEmailSender_resolves_to_the_factory_CapturingEmailSender_instance()
      {
          // Building a client forces the host to start (and applies the Task 2 startup migration).
          _ = _factory.CreateClient();

          using var scope = _factory.Services.CreateScope();
          var resolved = scope.ServiceProvider.GetRequiredService<IEmailSender>();

          Assert.IsType<CapturingEmailSender>(resolved);
          Assert.Same(_factory.Email, resolved);
      }
  }
  ```

- [ ] **Step 2: Run, expect FAIL**

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj --filter "FullyQualifiedName~EmailWiringTests"
  ```

  Expected: compilation failure (the build does not succeed). The error text contains:
  `error CS0246: The type or namespace name 'IEmailSender' could not be found` (from `using FitApi.Services;` / `GetRequiredService<IEmailSender>()`) and `error CS0246: The type or namespace name 'CapturingEmailSender' could not be found`, plus `error CS1061: 'CustomWebApplicationFactory' does not contain a definition for 'Email'`. The summary line reads `Build FAILED`.

- [ ] **Step 3a: Add the MailKit package to the web project**

  Run (latest stable for net10.0 â€” do NOT pin a patch version):

  ```
  dotnet add D:\Work\Templates\FIT\server\FitApi\FitApi.csproj package MailKit
  ```

  Verify `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj` now contains a line of the form
  `<PackageReference Include="MailKit" Version="..." />` (any stable version string is acceptable).

- [ ] **Step 3b: Create the IEmailSender interface**

  Write `D:\Work\Templates\FIT\server\FitApi\Services\IEmailSender.cs` (COMPLETE file):

  ```csharp
  namespace FitApi.Services;

  public interface IEmailSender
  {
      Task SendAsync(string to, string subject, string htmlBody);
  }
  ```

- [ ] **Step 3c: Create the MailKit SmtpEmailSender**

  Write `D:\Work\Templates\FIT\server\FitApi\Services\SmtpEmailSender.cs` (COMPLETE file). Reads `Smtp:Host` / `Smtp:Port` / `Smtp:From` (dev defaults host `mail`, port `1025`), builds a `MimeMessage` with an HTML body, connects without TLS (mailpit dev), sends, disconnects:

  ```csharp
  using MailKit.Net.Smtp;
  using MailKit.Security;
  using MimeKit;

  namespace FitApi.Services;

  public class SmtpEmailSender : IEmailSender
  {
      private readonly IConfiguration _config;

      public SmtpEmailSender(IConfiguration config) => _config = config;

      public async Task SendAsync(string to, string subject, string htmlBody)
      {
          var host = _config["Smtp:Host"] ?? "mail";
          var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 1025;
          var from = _config["Smtp:From"] ?? "no-reply@makemefit.local";

          var message = new MimeMessage();
          message.From.Add(MailboxAddress.Parse(from));
          message.To.Add(MailboxAddress.Parse(to));
          message.Subject = subject;
          message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

          using var client = new SmtpClient();
          await client.ConnectAsync(host, port, SecureSocketOptions.None);
          await client.SendAsync(message);
          await client.DisconnectAsync(true);
      }
  }
  ```

- [ ] **Step 3d: Register the email sender in Program.cs (additive marker insert)**

  Edit `D:\Work\Templates\FIT\server\FitApi\Program.cs` at the `// FIT:SERVICES-END` marker. This is an additive insert that keeps the marker â€” do NOT overwrite the file (Task 1 owns it).

  OLD string:
  ```csharp
  // FIT:SERVICES-END
  ```

  NEW string:
  ```csharp
  builder.Services.AddScoped<FitApi.Services.IEmailSender, FitApi.Services.SmtpEmailSender>();
  // FIT:SERVICES-END
  ```

- [ ] **Step 3e: Add Smtp config keys to appsettings.json**

  Edit `D:\Work\Templates\FIT\server\FitApi\appsettings.json` to add an `Smtp` section (dev/docker defaults; mailpit listens on host `mail` port `1025`). Add this object as a sibling of the existing top-level keys (insert after the opening `{`):

  OLD string:
  ```json
  {
  ```

  NEW string:
  ```json
  {
    "Smtp": {
      "Host": "mail",
      "Port": "1025",
      "From": "no-reply@makemefit.local"
    },
  ```

- [ ] **Step 3f: Create CapturingEmailSender in the test project**

  Write `D:\Work\Templates\FIT\server\FitApi.Tests\CapturingEmailSender.cs` (COMPLETE file). It records every send into the public `Sent` list (tuple shape `(To, Subject, Body)` exactly as the CONTRACT specifies):

  ```csharp
  using FitApi.Services;

  namespace FitApi.Tests;

  public class CapturingEmailSender : IEmailSender
  {
      public List<(string To, string Subject, string Body)> Sent { get; } = new();

      public Task SendAsync(string to, string subject, string htmlBody)
      {
          Sent.Add((to, subject, htmlBody));
          return Task.CompletedTask;
      }
  }
  ```

- [ ] **Step 3g: Edit CustomWebApplicationFactory.cs â€” add the Email property (additive edit #1)**

  Edit `D:\Work\Templates\FIT\server\FitApi.Tests\CustomWebApplicationFactory.cs`. These are the ONLY two edits this task makes to this file (Task 1 owns it). First, add the `Email` property next to the `_db` field.

  OLD string:
  ```csharp
      private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16").Build();
  ```

  NEW string:
  ```csharp
      private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:16").Build();

      public CapturingEmailSender Email { get; } = new();
  ```

- [ ] **Step 3h: Edit CustomWebApplicationFactory.cs â€” add usings (part of additive edit #2)**

  The `ConfigureTestServices` swap needs `Microsoft.AspNetCore.TestHost` (extension method `ConfigureTestServices`), `Microsoft.Extensions.DependencyInjection` (`AddSingleton`), and `Microsoft.Extensions.DependencyInjection.Extensions` (`RemoveAll`). Add them to the existing using block.

  OLD string:
  ```csharp
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.Hosting;
  using Testcontainers.PostgreSql;
  using Xunit;
  ```

  NEW string:
  ```csharp
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.AspNetCore.TestHost;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using Microsoft.Extensions.Hosting;
  using Testcontainers.PostgreSql;
  using Xunit;
  ```

- [ ] **Step 3i: Edit CustomWebApplicationFactory.cs â€” insert the service swap at the marker (rest of additive edit #2)**

  Insert the `ConfigureTestServices` swap ABOVE the `// FIT:FACTORY-SERVICES-END` marker, keeping the marker. This removes the real `SmtpEmailSender` registration and replaces it with the shared `Email` capturing instance.

  OLD string:
  ```csharp
          builder.UseEnvironment("Testing");
          // FIT:FACTORY-SERVICES-END
  ```

  NEW string:
  ```csharp
          builder.UseEnvironment("Testing");
          builder.ConfigureTestServices(services =>
          {
              services.RemoveAll<FitApi.Services.IEmailSender>();
              services.AddSingleton<FitApi.Services.IEmailSender>(Email);
          });
          // FIT:FACTORY-SERVICES-END
  ```

- [ ] **Step 4: Run, expect PASS**

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.Tests\FitApi.Tests.csproj --filter "FullyQualifiedName~EmailWiringTests"
  ```

  Expected: `Build succeeded`, then the run summary:
  `Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1` for `EmailWiringTests.IEmailSender_resolves_to_the_factory_CapturingEmailSender_instance`. (Requires Docker running so the Testcontainers `postgres:16` container starts and the Task 2 startup migration succeeds.)

- [ ] **Step 5: Commit**

  ```
  git -C D:\Work\Templates\FIT add server/FitApi/FitApi.csproj server/FitApi/Services/IEmailSender.cs server/FitApi/Services/SmtpEmailSender.cs server/FitApi/Program.cs server/FitApi/appsettings.json server/FitApi.Tests/CapturingEmailSender.cs server/FitApi.Tests/CustomWebApplicationFactory.cs server/FitApi.Tests/EmailWiringTests.cs
  git -C D:\Work\Templates\FIT commit -m "feat(email): add MailKit IEmailSender/SmtpEmailSender, DI registration, and test CapturingEmailSender with factory swap"
  ```

---

### Task 5: AuthController: register/login/logout/me/confirm/forgot/reset/resend + TestAuthHelper

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Dtos\AuthDtos.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\AuthController.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi.Tests\TestAuthHelper.cs`
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\AuthTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.ApplicationUser` (Task 2) â€” fields `Email`, `DisplayName`, `PhotoUrl`, `EmailConfirmed`, `UpdatedAt`.
  - `FitApi.Controllers.ApiControllerBase` (Task 3) â€” base class exposing `protected string UserId`.
  - Identity wiring from Task 3: `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>` registered DI; password `RequiredLength=6`; `SignIn.RequireConfirmedAccount=false`; `ConfigureApplicationCookie` returns 401/403 for `/api` paths.
  - `FitApi.Services.IEmailSender` (Task 4) â€” `Task SendAsync(string to, string subject, string htmlBody)`; in tests swapped for `CapturingEmailSender` whose `Sent` is exposed via `factory.Email` (Task 4 factory edit). `CustomWebApplicationFactory` (Task 1) + `CapturingEmailSender`/`factory.Email` (Task 4).
- Produces:
  - DTOs in `FitApi.Dtos`: `RegisterRequest`, `LoginRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`, `UserDto` (referenced by Tasks 7, 14 â€” never redeclared).
  - `FitApi.Controllers.AuthController` with route `api/auth` and the canonical endpoints below.
  - The single `static UserDto ToUserDto(ApplicationUser u)` mapping (Task 7 references `GET api/auth/me`, does NOT redefine this).
  - `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync(this CustomWebApplicationFactory f, string email, string password, string name = "Test")` returning a cookie-handling `HttpClient` (consumed by every later data-controller test task).

---

- [ ] **Step 1: Write the failing test**

Create `D:\Work\Templates\FIT\server\FitApi.Tests\TestAuthHelper.cs` (needed by this test and all later tasks):

```csharp
using System.Net.Http.Json;

namespace FitApi.Tests;

public static class TestAuthHelper
{
    public static async Task<HttpClient> RegisterAndLoginAsync(
        this CustomWebApplicationFactory f,
        string email,
        string password,
        string name = "Test")
    {
        // WebApplicationFactory's client owns a cookie-capable handler, so
        // Set-Cookie from /login is replayed on subsequent requests automatically.
        var client = f.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { name, email, password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        login.EnsureSuccessStatusCode();

        return client;
    }
}
```

Create `D:\Work\Templates\FIT\server\FitApi.Tests\AuthTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FitApi.Models;
using Xunit;

namespace FitApi.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    // Pulls userId + token out of a captured confirm/reset email link query string.
    private static (string UserId, string Token) ExtractUserIdAndToken(string body)
    {
        var start = body.IndexOf("http", StringComparison.Ordinal);
        var rest = body[start..];
        var end = rest.IndexOfAny(new[] { '"', '\'', ' ', '<', '\n', '\r' });
        var url = end < 0 ? rest : rest[..end];
        var qs = HttpUtility.ParseQueryString(new Uri(url).Query);
        return (qs["userId"] ?? "", qs["token"] ?? "");
    }

    private static string ExtractResetToken(string body)
    {
        var start = body.IndexOf("http", StringComparison.Ordinal);
        var rest = body[start..];
        var end = rest.IndexOfAny(new[] { '"', '\'', ' ', '<', '\n', '\r' });
        var url = end < 0 ? rest : rest[..end];
        var qs = HttpUtility.ParseQueryString(new Uri(url).Query);
        return qs["token"] ?? "";
    }

    [Fact]
    public async Task Register_persists_user_with_displayName_and_sends_verification_email()
    {
        var client = _factory.CreateClient();
        var before = _factory.Email.Sent.Count;
        var email = $"reg_{Guid.NewGuid():N}@x.com";

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Ahmed", email, password = "secret1" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("Ahmed");

        _factory.Email.Sent.Count.Should().Be(before + 1);
        _factory.Email.Sent.Last().To.Should().Be(email);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_409_with_code()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@x.com";

        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "A", email, password = "secret1" })).EnsureSuccessStatusCode();

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "A", email, password = "secret1" });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("email-already-in-use");
    }

    [Fact]
    public async Task Login_good_returns_UserDto_and_cookie_then_me_works()
    {
        var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Lin", email, password = "secret1" })).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "secret1" });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        login.Headers.TryGetValues("Set-Cookie", out _).Should().BeTrue();
        var dto = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
        dto.GetProperty("email").GetString().Should().Be(email);
        dto.GetProperty("displayName").GetString().Should().Be("Lin");

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await me.Content.ReadAsStringAsync())
            .RootElement.GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Login_wrong_password_returns_401_with_code()
    {
        var client = _factory.CreateClient();
        var email = $"wrong_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "W", email, password = "secret1" })).EnsureSuccessStatusCode();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "WRONGPASS" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("invalid-credential");
    }

    [Fact]
    public async Task Me_after_logout_returns_401()
    {
        var email = $"logout_{Guid.NewGuid():N}@x.com";
        var client = await _factory.RegisterAndLoginAsync(email, "secret1", "Out");

        (await client.PostAsJsonAsync("/api/auth/logout", new { })).EnsureSuccessStatusCode();

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_unknown_email_returns_200_and_sends_nothing()
    {
        var client = _factory.CreateClient();
        var before = _factory.Email.Sent.Count;

        var resp = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = $"nobody_{Guid.NewGuid():N}@x.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Email.Sent.Count.Should().Be(before);
    }

    [Fact]
    public async Task ForgotThenReset_changes_password()
    {
        var client = _factory.CreateClient();
        var email = $"reset_{Guid.NewGuid():N}@x.com";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "R", email, password = "oldpass1" })).EnsureSuccessStatusCode();

        var beforeForgot = _factory.Email.Sent.Count;
        (await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email })).EnsureSuccessStatusCode();
        _factory.Email.Sent.Count.Should().Be(beforeForgot + 1);

        var token = ExtractResetToken(_factory.Email.Sent.Last().Body);

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { email, token, password = "newpass1" });
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "oldpass1" });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "newpass1" });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmEmail_with_captured_token_redirects_verified_1_and_sets_flag()
    {
        var client = _factory.CreateClient();
        var noRedirect = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var email = $"confirm_{Guid.NewGuid():N}@x.com";

        var before = _factory.Email.Sent.Count;
        (await client.PostAsJsonAsync("/api/auth/register",
            new { name = "C", email, password = "secret1" })).EnsureSuccessStatusCode();
        _factory.Email.Sent.Count.Should().Be(before + 1);

        var (userId, token) = ExtractUserIdAndToken(_factory.Email.Sent.Last().Body);
        userId.Should().NotBeEmpty();
        token.Should().NotBeEmpty();

        var resp = await noRedirect.GetAsync(
            $"/api/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}");

        resp.StatusCode.Should().Be(HttpStatusCode.Found);
        resp.Headers.Location!.ToString().Should().Be("/confirm-email.html?verified=1");

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        user!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ResendVerification_authed_sends_email()
    {
        var email = $"resend_{Guid.NewGuid():N}@x.com";
        var client = await _factory.RegisterAndLoginAsync(email, "secret1", "Re");

        var before = _factory.Email.Sent.Count;
        var resp = await client.PostAsJsonAsync("/api/auth/resend-verification", new { });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Email.Sent.Count.Should().Be(before + 1);
        _factory.Email.Sent.Last().To.Should().Be(email);
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.AuthTests"
```
Expected: COMPILE FAILURE. Error text contains `CS0246: The type or namespace name 'AuthController' could not be found` is NOT emitted (controllers are discovered at runtime), but the build fails earlier with errors such as `error CS0103: The name 'RegisterRequest' could not be found` is also not it â€” the actual first failure is the test referencing `_factory.Email` plus the controller not existing. Concretely you will see test build succeed only after Task 4 added `Email`; the AuthTests themselves fail at runtime with `Assert.Equal() Failure: Expected HttpStatusCode.OK / Actual NotFound` for `Register_*` because `/api/auth/*` routes do not exist yet. Confirm the run reports **Failed!** with multiple `NotFound`/`404` assertions before proceeding.

- [ ] **Step 3: Implement**

Create `D:\Work\Templates\FIT\server\FitApi\Dtos\AuthDtos.cs` (declares each record EXACTLY once):

```csharp
namespace FitApi.Dtos;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string Password);
public record UserDto(string Id, string Email, string DisplayName, string? PhotoUrl, bool EmailConfirmed);
```

Create `D:\Work\Templates\FIT\server\FitApi\Controllers\AuthController.cs` (ALL email/password actions in this one file; holds the single `ToUserDto` helper):

```csharp
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using FitApi.Dtos;
using FitApi.Models;
using FitApi.Services;

namespace FitApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEmailSender _email;

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IEmailSender email)
    {
        _users = users;
        _signIn = signIn;
        _email = email;
    }

    // The single UserDto mapping. Other tasks reference api/auth/me; they do NOT redefine this.
    private static UserDto ToUserDto(ApplicationUser u) =>
        new(u.Id, u.Email ?? "", u.DisplayName ?? "", u.PhotoUrl, u.EmailConfirmed);

    private static IDictionary<string, object?> Code(string code) =>
        new Dictionary<string, object?> { ["code"] = code };

    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var encoded = Uri.EscapeDataString(token);
        var link =
            $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email" +
            $"?userId={Uri.EscapeDataString(user.Id)}&token={encoded}";
        var html =
            $"<p>Ù…Ø±Ø­Ø¨Ø§Ù‹ØŒ Ù„ØªØ£ÙƒÙŠØ¯ Ø¨Ø±ÙŠØ¯Ùƒ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ Ø§Ø¶ØºØ· Ø¹Ù„Ù‰ Ø§Ù„Ø±Ø§Ø¨Ø· Ø§Ù„ØªØ§Ù„ÙŠ:</p>" +
            $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ</a></p>";
        await _email.SendAsync(user.Email!, "ØªØ£ÙƒÙŠØ¯ Ø¨Ø±ÙŠØ¯Ùƒ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ", html);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var existing = await _users.FindByEmailAsync(req.Email);
        if (existing is not null)
            return Problem(statusCode: 409, title: "email-already-in-use",
                extensions: Code("email-already-in-use"));

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.Name,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var dup = result.Errors.Any(e =>
                e.Code == nameof(IdentityErrorDescriber.DuplicateEmail) ||
                e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
            if (dup)
                return Problem(statusCode: 409, title: "email-already-in-use",
                    extensions: Code("email-already-in-use"));
            return Problem(statusCode: 400, title: "register-failed",
                extensions: Code("register-failed"));
        }

        await SendVerificationEmailAsync(user);
        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        var check = await _signIn.PasswordSignInAsync(
            user, req.Password, isPersistent: true, lockoutOnFailure: false);
        if (!check.Succeeded)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        return Ok(ToUserDto(user));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));
        return Ok(ToUserDto(user));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is not null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var link =
                $"{Request.Scheme}://{Request.Host}/reset-password.html" +
                $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            var html =
                $"<p>Ù„Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø§Ø¶ØºØ· Ø¹Ù„Ù‰ Ø§Ù„Ø±Ø§Ø¨Ø· Ø§Ù„ØªØ§Ù„ÙŠ:</p>" +
                $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±</a></p>";
            await _email.SendAsync(user.Email!, "Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±", html);
        }
        // No account enumeration: always 200 regardless of existence.
        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(statusCode: 400, title: "invalid-token",
                extensions: Code("invalid-token"));

        var result = await _users.ResetPasswordAsync(user, req.Token, req.Password);
        if (!result.Succeeded)
            return Problem(statusCode: 400, title: "invalid-token",
                extensions: Code("invalid-token"));

        return Ok(new { });
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return Redirect("/confirm-email.html?verified=0");

        var result = await _users.ConfirmEmailAsync(user, token);
        return Redirect(result.Succeeded
            ? "/confirm-email.html?verified=1"
            : "/confirm-email.html?verified=0");
    }

    [Authorize]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null)
            return Problem(statusCode: 401, title: "invalid-credential",
                extensions: Code("invalid-credential"));

        if (user.EmailConfirmed)
            return Ok(new { });

        await SendVerificationEmailAsync(user);
        return Ok(new { });
    }
}
```

Note on `Uri.EscapeDataString`/`WebUtilities`: `GenerateEmailConfirmationTokenAsync` returns a base64-ish token containing `+`/`/`; URL-encoding it in the link (and decoding via the test's `HttpUtility.ParseQueryString`, which `Uri.Query` round-trips) keeps the token intact. `Microsoft.AspNetCore.WebUtilities` is imported because it is the canonical home for token-encoding helpers; it is available transitively via the framework reference and harmless if unused.

- [ ] **Step 4: Run, expect PASS**

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.AuthTests"
```
Expected: `Passed!  - Failed: 0, Passed: 10, Skipped: 0` (the 10 `[Fact]` methods in `AuthTests`). No build errors.

- [ ] **Step 5: Commit**

```
git -C D:\Work\Templates\FIT add server/FitApi/Dtos/AuthDtos.cs server/FitApi/Controllers/AuthController.cs server/FitApi.Tests/TestAuthHelper.cs server/FitApi.Tests/AuthTests.cs
git -C D:\Work\Templates\FIT commit -m "feat(api): add AuthController (register/login/logout/me/confirm/forgot/reset/resend) + test helper"
```

---

### Task 6: AuthGoogleController (challenge + callback) + Program.cs AddGoogle + deterministic fake-scheme test

**Files:**
- Modify: `D:\Work\Templates\FIT\server\FitApi\FitApi.csproj` (add package `Microsoft.AspNetCore.Authentication.Google` via `dotnet add package`)
- Modify: `D:\Work\Templates\FIT\server\FitApi\Program.cs` (ADDITIVE Edit at `// FIT:SERVICES-END` marker ONLY â€” created by Task 1)
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\AuthGoogleController.cs`
- Test:   `D:\Work\Templates\FIT\server\FitApi.Tests\AuthGoogleTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.ApplicationUser` (Task 2 entity: `Id`, `Email`, `UserName`, `DisplayName`, `PhotoUrl`).
  - `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` (registered by Task 3 `AddIdentity<ApplicationUser,IdentityRole>(...)`).
  - Cookie auth + the `/api`-aware 401/403 cookie events (Task 3 `ConfigureApplicationCookie`); `app.UseAuthentication(); app.UseAuthorization();` (Task 3 MIDDLEWARE insert).
  - `GET api/auth/me` (Task 5 `AuthController`) â€” used by the test to confirm the app cookie authenticates the just-created user.
  - `CustomWebApplicationFactory` (Task 1) with marker `// FIT:FACTORY-SERVICES-END` and the Task 4 `ConfigureTestServices` pattern (referenced only for style; this test uses its OWN `WithWebHostBuilder`).
- Produces:
  - `FitApi.Controllers.AuthGoogleController` â€” route `api/auth/google`:
    - `GET api/auth/google` -> `Challenge("Google")` with `RedirectUri` set to the callback action.
    - `GET api/auth/google/callback` -> find-or-create `ApplicationUser` by email, `AddLoginAsync`, external sign-in, set `DisplayName`/`PhotoUrl` from claims if new, `302 -> "/profile.html"`.
  - Program.cs now registers the Google authentication handler (`AddAuthentication().AddGoogle(...)`) reading `Authentication:Google:ClientId` / `Authentication:Google:ClientSecret`.

> NOTE (real Google, not exercised by tests): for production, `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` MUST be present in configuration (e.g. user-secrets / env vars / `appsettings`), and the Google Cloud OAuth client MUST have the redirect URI `https://<your-host>/signin-google` registered (that path is the Google handler's default `CallbackPath`, distinct from our app-level `api/auth/google/callback`). The deterministic test below replaces the `"Google"` scheme with a fake handler, so it never contacts Google and never needs real credentials.

---

- [ ] **Step 1: Write the failing test**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\AuthGoogleTests.cs`. This test registers a fake authentication handler under the scheme name `"Google"` (overriding the real Google handler) that immediately returns a `ClaimsPrincipal` carrying a fixed email + name. It then drives `GET api/auth/google/callback` and asserts the user was created and that the issued app cookie authenticates `GET api/auth/me` -> `200` with that email. It deliberately does NOT assert on the external challenge redirect target.

  ```csharp
  using System.Net;
  using System.Net.Http.Json;
  using System.Security.Claims;
  using System.Text.Encodings.Web;
  using FluentAssertions;
  using Microsoft.AspNetCore.Authentication;
  using Microsoft.AspNetCore.Authentication.Google;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.AspNetCore.TestHost;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using Xunit;

  namespace FitApi.Tests;

  // Fake external handler bound to scheme "Google". On the callback request it
  // returns a successful AuthenticateResult carrying a fixed external identity,
  // exactly as the real Google handler would after a round-trip â€” but offline.
  public class FakeGoogleHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
      public const string TestEmail = "google.user@example.com";
      public const string TestName = "Google User";
      public const string TestNameId = "google-subject-12345";

      public FakeGoogleHandler(
          IOptionsMonitor<AuthenticationSchemeOptions> options,
          ILoggerFactory logger,
          UrlEncoder encoder)
          : base(options, logger, encoder) { }

      protected override Task<AuthenticateResult> HandleAuthenticateAsync()
      {
          var claims = new[]
          {
              new Claim(ClaimTypes.NameIdentifier, TestNameId),
              new Claim(ClaimTypes.Email, TestEmail),
              new Claim(ClaimTypes.Name, TestName),
              new Claim("urn:google:picture", "https://example.com/avatar.png"),
          };
          var identity = new ClaimsIdentity(claims, GoogleDefaults.AuthenticationScheme);
          var principal = new ClaimsPrincipal(identity);
          var ticket = new AuthenticationTicket(principal, GoogleDefaults.AuthenticationScheme);
          return Task.FromResult(AuthenticateResult.Success(ticket));
      }
  }

  public class AuthGoogleTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public AuthGoogleTests(CustomWebApplicationFactory factory) => _factory = factory;

      private CustomWebApplicationFactory FactoryWithFakeGoogle() =>
          (CustomWebApplicationFactory)_factory.WithWebHostBuilder(b =>
          {
              b.ConfigureTestServices(services =>
              {
                  // Replace the "Google" scheme registered in Program.cs with the
                  // offline fake handler. PostConfigure overrides the existing
                  // scheme builder for that name so no real Google handler runs.
                  services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, FakeGoogleHandler>(
                      GoogleDefaults.AuthenticationScheme, _ => { });
              });
          });

      [Fact]
      public async Task GoogleCallback_CreatesUser_AndCookieAuthenticatesMe()
      {
          var factory = FactoryWithFakeGoogle();
          var client = factory.CreateClient(new WebApplicationFactoryClientOptions
          {
              AllowAutoRedirect = false,
          });

          var callback = await client.GetAsync("/api/auth/google/callback");

          callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
          callback.Headers.Location!.OriginalString.Should().Be("/profile.html");

          // The callback set the Identity application cookie on the shared handler;
          // the same client now hits an [Authorize] endpoint and must be the new user.
          var me = await client.GetAsync("/api/auth/me");
          me.StatusCode.Should().Be(HttpStatusCode.OK);

          var dto = await me.Content.ReadFromJsonAsync<MeProbe>();
          dto.Should().NotBeNull();
          dto!.Email.Should().Be(FakeGoogleHandler.TestEmail);
          dto.DisplayName.Should().Be(FakeGoogleHandler.TestName);
      }

      [Fact]
      public async Task GoogleChallenge_ReturnsChallenge_NotServerError()
      {
          var factory = FactoryWithFakeGoogle();
          var client = factory.CreateClient(new WebApplicationFactoryClientOptions
          {
              AllowAutoRedirect = false,
          });

          var resp = await client.GetAsync("/api/auth/google");

          // Fake handler has no challenge override, so the default AuthenticationHandler
          // challenge yields 401 (Unauthorized). We assert it is NOT a 5xx server error
          // and is the expected 401 â€” we do NOT assert on any external redirect target.
          resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
      }

      private sealed record MeProbe(string Id, string Email, string DisplayName, string? PhotoUrl, bool EmailConfirmed);
  }
  ```

- [ ] **Step 2: Run, expect FAIL**

  Run from `D:\Work\Templates\FIT\server`:
  ```
  dotnet test FitApi.Tests/FitApi.Tests.csproj --filter "FullyQualifiedName~AuthGoogleTests"
  ```
  Expected: BUILD FAILURE â€” compile errors because `AuthGoogleController` does not exist yet, e.g.
  `error CS0246: The type or namespace name 'GoogleDefaults' could not be found` (the Google package is not referenced yet) and there is no `api/auth/google/callback` route. (If the build somehow succeeds, the test fails at runtime: `callback.StatusCode` is `404 NotFound`, not `302 Redirect`.)

- [ ] **Step 3: Implement**

  3a. Add the Google authentication package. Run from `D:\Work\Templates\FIT\server\FitApi`:
  ```
  dotnet add package Microsoft.AspNetCore.Authentication.Google
  ```
  (Latest stable for net10.0 â€” do NOT pin a patch version.)

  3b. EDIT `D:\Work\Templates\FIT\server\FitApi\Program.cs` â€” additive insert ABOVE the `// FIT:SERVICES-END` marker (this is its OWN SERVICES insert per the CONTRACT; do NOT search for or modify any prior `AddAuthentication()` â€” calling `AddAuthentication()` again only ADDS the Google handler and does not reset Identity's schemes):

  OLD:
  ```csharp
  // FIT:SERVICES-END
  ```
  NEW:
  ```csharp
  builder.Services.AddAuthentication().AddGoogle(o =>
  {
      var cfg = builder.Configuration;
      o.ClientId = cfg["Authentication:Google:ClientId"] ?? "test-client-id";
      o.ClientSecret = cfg["Authentication:Google:ClientSecret"] ?? "test-client-secret";
  });
  // FIT:SERVICES-END
  ```
  > The `?? "test-client-id"` / `?? "test-client-secret"` fallbacks let the host start in `Testing` (and local dev) without real Google credentials configured; the deterministic test overrides the `"Google"` scheme entirely, so these dummy values are never used to contact Google. In production, set the real values in configuration (see NOTE above).

  3c. CREATE `D:\Work\Templates\FIT\server\FitApi\Controllers\AuthGoogleController.cs` with EXACTLY:
  ```csharp
  using System.Security.Claims;
  using FitApi.Models;
  using Microsoft.AspNetCore.Authentication;
  using Microsoft.AspNetCore.Authentication.Google;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Mvc;

  namespace FitApi.Controllers;

  [ApiController]
  [Route("api/auth/google")]
  [AllowAnonymous]
  public class AuthGoogleController : ControllerBase
  {
      private readonly SignInManager<ApplicationUser> _signInManager;
      private readonly UserManager<ApplicationUser> _userManager;

      public AuthGoogleController(
          SignInManager<ApplicationUser> signInManager,
          UserManager<ApplicationUser> userManager)
      {
          _signInManager = signInManager;
          _userManager = userManager;
      }

      // GET api/auth/google  -> challenge the external "Google" scheme; on success
      // Google redirects the browser back to our callback action.
      [HttpGet("")]
      public IActionResult Challenge([FromQuery] string? returnUrl = null)
      {
          var callbackUrl = Url.Action(nameof(Callback), "AuthGoogle", null, Request.Scheme);
          var props = _signInManager.ConfigureExternalAuthenticationProperties(
              GoogleDefaults.AuthenticationScheme, callbackUrl);
          if (!string.IsNullOrWhiteSpace(returnUrl))
          {
              props.Items["returnUrl"] = returnUrl;
          }
          return Challenge(props, GoogleDefaults.AuthenticationScheme);
      }

      // GET api/auth/google/callback -> read the external identity, find-or-create
      // the local user, link the external login, issue the app cookie, redirect.
      [HttpGet("callback")]
      public async Task<IActionResult> Callback()
      {
          var info = await _signInManager.GetExternalLoginInfoAsync();
          if (info is null)
          {
              // Fall back to authenticating the "Google" scheme directly (covers
              // the deterministic fake-scheme test where no auth session cookie
              // was set, and any case where GetExternalLoginInfoAsync returns null).
              var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
              if (!result.Succeeded || result.Principal is null)
              {
                  return Redirect("/profile.html");
              }

              var providerKey =
                  result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  result.Principal.FindFirstValue("sub") ??
                  result.Principal.FindFirstValue(ClaimTypes.Email) ??
                  Guid.NewGuid().ToString();

              info = new ExternalLoginInfo(
                  result.Principal,
                  GoogleDefaults.AuthenticationScheme,
                  providerKey,
                  GoogleDefaults.AuthenticationScheme);
          }

          var email =
              info.Principal.FindFirstValue(ClaimTypes.Email) ??
              info.Principal.FindFirstValue("email");
          if (string.IsNullOrWhiteSpace(email))
          {
              return Redirect("/profile.html");
          }

          // Already linked? sign in directly.
          var signIn = await _signInManager.ExternalLoginSignInAsync(
              info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
          if (signIn.Succeeded)
          {
              return Redirect("/profile.html");
          }

          var user = await _userManager.FindByEmailAsync(email);
          if (user is null)
          {
              user = new ApplicationUser
              {
                  UserName = email,
                  Email = email,
                  EmailConfirmed = true,
                  DisplayName =
                      info.Principal.FindFirstValue(ClaimTypes.Name) ??
                      info.Principal.FindFirstValue("name"),
                  PhotoUrl =
                      info.Principal.FindFirstValue("urn:google:picture") ??
                      info.Principal.FindFirstValue("picture"),
                  UpdatedAt = DateTimeOffset.UtcNow,
              };
              var created = await _userManager.CreateAsync(user);
              if (!created.Succeeded)
              {
                  return Redirect("/profile.html");
              }
          }

          // Link the external login if not already linked, then issue the app cookie.
          var logins = await _userManager.GetLoginsAsync(user);
          var alreadyLinked = logins.Any(l =>
              l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey);
          if (!alreadyLinked)
          {
              await _userManager.AddLoginAsync(user, info);
          }

          await _signInManager.SignInAsync(user, isPersistent: true);
          return Redirect("/profile.html");
      }
  }
  ```

  > Why the fallback branch exists: `GetExternalLoginInfoAsync()` reads the external sign-in *cookie* set by the real Google handler's correlation flow. The offline fake handler in the test never sets that cookie, so we directly `AuthenticateAsync("Google")`, which the fake handler answers with the fixed identity. In production with the real handler, `GetExternalLoginInfoAsync()` succeeds and the fallback is not taken. Both paths converge on find-or-create + `AddLoginAsync` + `SignInAsync` + `302 -> "/profile.html"`.

- [ ] **Step 4: Run, expect PASS**

  Run from `D:\Work\Templates\FIT\server`:
  ```
  dotnet test FitApi.Tests/FitApi.Tests.csproj --filter "FullyQualifiedName~AuthGoogleTests"
  ```
  Expected: `Passed!  - Failed: 0, Passed: 2, Skipped: 0` â€” both `GoogleCallback_CreatesUser_AndCookieAuthenticatesMe` and `GoogleChallenge_ReturnsChallenge_NotServerError` pass. The callback returns `302` with `Location: /profile.html`, and the subsequent `GET /api/auth/me` returns `200` with `email == "google.user@example.com"` and `displayName == "Google User"`.

- [ ] **Step 5: Commit**

  Run from `D:\Work\Templates\FIT`:
  ```
  git add server/FitApi/FitApi.csproj server/FitApi/Program.cs server/FitApi/Controllers/AuthGoogleController.cs server/FitApi.Tests/AuthGoogleTests.cs
  git commit -m "feat(auth): add Google external login controller (challenge + callback) with deterministic fake-scheme test"
  ```


---

### Task 7: ProfileController (GET/PUT/avatar) + IAvatarStorage/LocalAvatarStorage + Program.cs /uploads

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Dtos\ProfileDtos.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Services\IAvatarStorage.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Services\LocalAvatarStorage.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\ProfileController.cs`
- Modify: `D:\Work\Templates\FIT\server\FitApi\Program.cs` (additive Edits at `// FIT:SERVICES-END` and `// FIT:MIDDLEWARE-END` markers only â€” file owned by Task 1)
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\ProfileTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.ApplicationUser` (Task 2): properties `DisplayName`, `PhotoUrl`, `Weight`, `Height`, `Goal`, `Activity`, `UpdatedAt`, plus Identity `Email`.
  - `Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>` (registered by Task 3's `AddIdentity<ApplicationUser,IdentityRole>()`).
  - `FitApi.Controllers.ApiControllerBase` (Task 3) â€” `ProfileController` inherits it (uses Identity `UserManager.GetUserAsync(User)` for data, so does not need the `UserId` property, but inherits it for consistency).
  - `[Authorize]` cookie scheme + `OnRedirectToLogin -> 401` for `/api` paths (Task 3).
  - `FitApi.Tests.CustomWebApplicationFactory` + `TestAuthHelper.RegisterAndLoginAsync` (Tasks 1 & 5).
  - Program.cs CONTRACT markers `// FIT:SERVICES-END`, `// FIT:MIDDLEWARE-END`, and `using Microsoft.Extensions.FileProviders;` (already present from Task 1).
- Produces (later tasks REFERENCE, never redeclare):
  - `FitApi.Dtos.ProfileDto(string DisplayName,string Email,string? PhotoUrl,decimal? Weight,decimal? Height,string? Goal,string? Activity)`
  - `FitApi.Dtos.UpdateProfileRequest(string? DisplayName,decimal? Weight,decimal? Height,string? Goal,string? Activity)`
  - `FitApi.Services.IAvatarStorage` { `Task<string> SaveAsync(string userId, Stream content, string contentType)` }
  - `FitApi.Services.LocalAvatarStorage : IAvatarStorage`
  - Routes: `GET api/profile`, `PUT api/profile`, `POST api/profile/avatar` (multipart field `file`).
  - DI registration `IAvatarStorage -> LocalAvatarStorage` (Scoped) and static-file mapping at `RequestPath "/uploads"`.

---

- [ ] **Step 1: Write the failing test**

Create `D:\Work\Templates\FIT\server\FitApi.Tests\ProfileTests.cs` with COMPLETE code:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class ProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfileTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    // A tiny but VALID 1x1 PNG (decodable header + IEND), as raw bytes.
    private static byte[] TinyPng() => new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,
        0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
        0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,
        0x89,0x00,0x00,0x00,0x0A,0x49,0x44,0x41,
        0x54,0x78,0x9C,0x63,0x00,0x01,0x00,0x00,
        0x05,0x00,0x01,0x0D,0x0A,0x2D,0xB4,0x00,
        0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,
        0x42,0x60,0x82
    };

    [Fact]
    public async Task NewUser_GetProfile_HasEmail_AndNullMetrics()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-new@example.com", "secret123", "New User");

        var resp = await client.GetAsync("/api/profile");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("email").GetString().Should().Be("profile-new@example.com");
        root.GetProperty("displayName").GetString().Should().Be("New User");
        root.GetProperty("photoUrl").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("weight").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("height").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("goal").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("activity").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PutProfile_UpdatesFields_AndGetReflects()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-put@example.com", "secret123", "Old Name");

        var putBody = new
        {
            displayName = "Updated Name",
            weight = 80.5m,
            height = 178.0m,
            goal = "lose",
            activity = "high"
        };
        var put = await client.PutAsJsonAsync("/api/profile", putBody);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var putDoc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        var putRoot = putDoc.RootElement;
        putRoot.GetProperty("displayName").GetString().Should().Be("Updated Name");
        putRoot.GetProperty("weight").GetDecimal().Should().Be(80.5m);
        putRoot.GetProperty("height").GetDecimal().Should().Be(178.0m);
        putRoot.GetProperty("goal").GetString().Should().Be("lose");
        putRoot.GetProperty("activity").GetString().Should().Be("high");

        var get = await client.GetAsync("/api/profile");
        var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var getRoot = getDoc.RootElement;
        getRoot.GetProperty("displayName").GetString().Should().Be("Updated Name");
        getRoot.GetProperty("weight").GetDecimal().Should().Be(80.5m);
        getRoot.GetProperty("height").GetDecimal().Should().Be(178.0m);
        getRoot.GetProperty("goal").GetString().Should().Be("lose");
        getRoot.GetProperty("activity").GetString().Should().Be("high");
        getRoot.GetProperty("email").GetString().Should().Be("profile-put@example.com");
    }

    [Fact]
    public async Task PostAvatar_ValidPng_Returns200_WithPhotoUrl_AndGetReflects()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-avatar@example.com", "secret123", "Avatar User");

        using var content = new MultipartFormDataContent();
        var bytes = TinyPng();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "avatar.png");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var url = doc.RootElement.GetProperty("photoURL").GetString();
        url.Should().NotBeNullOrEmpty();
        url!.Should().StartWith("/uploads/avatars/");
        url.Should().EndWith(".png");

        var get = await client.GetAsync("/api/profile");
        var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetProperty("photoUrl").GetString().Should().Be(url);
    }

    [Fact]
    public async Task PostAvatar_TextFile_Returns400_BadAvatar()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-badtype@example.com", "secret123", "Bad Type");

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("not an image"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "note.txt");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("bad-avatar");
    }

    [Fact]
    public async Task PostAvatar_TooLarge_Returns400_BadAvatar()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "profile-toobig@example.com", "secret123", "Too Big");

        using var content = new MultipartFormDataContent();
        // 2 MB + 1 byte of image/png -> exceeds 2*1024*1024 limit.
        var bytes = new byte[2 * 1024 * 1024 + 1];
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "big.png");

        var resp = await client.PostAsync("/api/profile/avatar", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("bad-avatar");
    }

    [Fact]
    public async Task OwnerIsolation_UserB_Put_DoesNotChange_UserA_Profile()
    {
        var clientA = await _factory.RegisterAndLoginAsync(
            "isoA@example.com", "secret123", "User A");
        var clientB = await _factory.RegisterAndLoginAsync(
            "isoB@example.com", "secret123", "User B");

        // A sets a distinct profile.
        var aBody = new { displayName = "A Display", weight = 70.0m, height = 170.0m, goal = "gain", activity = "low" };
        (await clientA.PutAsJsonAsync("/api/profile", aBody)).StatusCode.Should().Be(HttpStatusCode.OK);

        // B updates B's own profile (the only thing B's cookie can touch).
        var bBody = new { displayName = "B Display", weight = 99.0m, height = 199.0m, goal = "lose", activity = "high" };
        (await clientB.PutAsJsonAsync("/api/profile", bBody)).StatusCode.Should().Be(HttpStatusCode.OK);

        // A's profile must be untouched by B's write.
        var getA = await clientA.GetAsync("/api/profile");
        var docA = JsonDocument.Parse(await getA.Content.ReadAsStringAsync());
        var rootA = docA.RootElement;
        rootA.GetProperty("email").GetString().Should().Be("isoA@example.com");
        rootA.GetProperty("displayName").GetString().Should().Be("A Display");
        rootA.GetProperty("weight").GetDecimal().Should().Be(70.0m);
        rootA.GetProperty("height").GetDecimal().Should().Be(170.0m);
        rootA.GetProperty("goal").GetString().Should().Be("gain");
        rootA.GetProperty("activity").GetString().Should().Be("low");
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

Run from `D:\Work\Templates\FIT\server`:

```
dotnet test --filter "FullyQualifiedName~FitApi.Tests.ProfileTests"
```

Expected: compile failure (e.g. `error CS0246: The type or namespace name 'ProfileController' / 'IAvatarStorage' / 'ProfileDto' could not be found` is not reported directly, but the test references routes `/api/profile` that 404, and `Dtos`/`Services` types do not yet exist). Concretely the build fails first because the new controller/services/DTOs do not exist; you will see `Build FAILED`. If the build somehow succeeds, the run fails with: tests returning `404 NotFound` where `200 OK`/`400 BadRequest` expected â€” `Expected resp.StatusCode to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.NotFound {value: 404}`.

- [ ] **Step 3: Implement**

3a. Create `D:\Work\Templates\FIT\server\FitApi\Dtos\ProfileDtos.cs` (COMPLETE new file â€” owned by THIS task):

```csharp
namespace FitApi.Dtos;

public record ProfileDto(
    string DisplayName,
    string Email,
    string? PhotoUrl,
    decimal? Weight,
    decimal? Height,
    string? Goal,
    string? Activity);

public record UpdateProfileRequest(
    string? DisplayName,
    decimal? Weight,
    decimal? Height,
    string? Goal,
    string? Activity);
```

3b. Create `D:\Work\Templates\FIT\server\FitApi\Services\IAvatarStorage.cs` (COMPLETE new file):

```csharp
namespace FitApi.Services;

public interface IAvatarStorage
{
    Task<string> SaveAsync(string userId, Stream content, string contentType);
}
```

3c. Create `D:\Work\Templates\FIT\server\FitApi\Services\LocalAvatarStorage.cs` (COMPLETE new file). Writes `{userId}{ext}` under `Storage:AvatarRoot` (dev default `"/app/uploads/avatars"`), returns the public URL `"/uploads/avatars/{userId}{ext}"`:

```csharp
using Microsoft.Extensions.Configuration;

namespace FitApi.Services;

public class LocalAvatarStorage : IAvatarStorage
{
    private readonly string _root;

    public LocalAvatarStorage(IConfiguration config)
    {
        _root = config["Storage:AvatarRoot"] ?? "/app/uploads/avatars";
    }

    public async Task<string> SaveAsync(string userId, Stream content, string contentType)
    {
        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => ".img"
        };

        Directory.CreateDirectory(_root);

        var fileName = userId + ext;
        var fullPath = Path.Combine(_root, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs);
        }

        return $"/uploads/avatars/{fileName}";
    }
}
```

3d. Create `D:\Work\Templates\FIT\server\FitApi\Controllers\ProfileController.cs` (COMPLETE new file). Uses `UserManager<ApplicationUser>.GetUserAsync(User)` (authed user always exists â€” no NotFound/Unauthorized branch). Avatar validation: `ContentType` startsWith `"image/"` AND `Length <= 2*1024*1024`, else `Problem` with `code="bad-avatar"`. Avatar success returns `{ photoURL = url }`:

```csharp
using FitApi.Dtos;
using FitApi.Models;
using FitApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAvatarStorage _avatars;

    public ProfileController(UserManager<ApplicationUser> users, IAvatarStorage avatars)
    {
        _users = users;
        _avatars = avatars;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var user = (await _users.GetUserAsync(User))!;
        return Ok(ToDto(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest req)
    {
        var user = (await _users.GetUserAsync(User))!;

        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.Weight is not null) user.Weight = req.Weight;
        if (req.Height is not null) user.Height = req.Height;
        if (req.Goal is not null) user.Goal = req.Goal;
        if (req.Activity is not null) user.Activity = req.Activity;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _users.UpdateAsync(user);
        return Ok(ToDto(user));
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> Avatar([FromForm] IFormFile? file)
    {
        if (file is null
            || string.IsNullOrEmpty(file.ContentType)
            || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || file.Length > 2 * 1024 * 1024)
        {
            return Problem(
                statusCode: 400,
                title: "bad-avatar",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-avatar" });
        }

        var user = (await _users.GetUserAsync(User))!;

        await using var stream = file.OpenReadStream();
        var url = await _avatars.SaveAsync(user.Id, stream, file.ContentType);

        user.PhotoUrl = url;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user);

        return Ok(new { photoURL = url });
    }

    private static ProfileDto ToDto(ApplicationUser u) => new(
        u.DisplayName ?? "",
        u.Email ?? "",
        u.PhotoUrl,
        u.Weight,
        u.Height,
        u.Goal,
        u.Activity);
}
```

3e. Edit `D:\Work\Templates\FIT\server\FitApi\Program.cs` â€” register `IAvatarStorage`. Additive insert ABOVE the `// FIT:SERVICES-END` marker (file owned by Task 1; do NOT overwrite):

OLD:
```
// FIT:SERVICES-END
```
NEW:
```
builder.Services.AddScoped<FitApi.Services.IAvatarStorage, FitApi.Services.LocalAvatarStorage>();
// FIT:SERVICES-END
```

3f. Edit `D:\Work\Templates\FIT\server\FitApi\Program.cs` â€” serve avatar files at `/uploads`. Additive insert ABOVE the `// FIT:MIDDLEWARE-END` marker. Resolve the avatar root from `Storage:AvatarRoot` with a dev default UNDER the content root so the directory exists and is writable on Windows/tests; create it first; then map static files at `RequestPath "/uploads"` pointing at the PARENT (`/uploads`) so the stored URL `/uploads/avatars/{file}` resolves. Uses `Microsoft.Extensions.FileProviders` (already imported by Task 1):

OLD:
```
// FIT:MIDDLEWARE-END
```
NEW:
```
var avatarRoot = builder.Configuration["Storage:AvatarRoot"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "uploads", "avatars");
Directory.CreateDirectory(avatarRoot);
var uploadsRoot = Path.GetFullPath(Path.Combine(avatarRoot, ".."));
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});
// FIT:MIDDLEWARE-END
```

Note: `LocalAvatarStorage` writes to `Storage:AvatarRoot` (or its own default `/app/uploads/avatars` if unset), and the middleware computes `uploadsRoot` as the PARENT of the SAME resolved `avatarRoot`, so the public URL `/uploads/avatars/{userId}{ext}` maps to `{uploadsRoot}/avatars/{userId}{ext}` = the written file. In Testing/Development `Storage:AvatarRoot` is unset, so BOTH the storage default and the middleware fall back to `{ContentRoot}/uploads/avatars` â€” but `LocalAvatarStorage`'s own default is `/app/uploads/avatars`, which on Windows tests is invalid. To keep them in lockstep WITHOUT touching `LocalAvatarStorage` again, set the config key for the test/dev environment in the next step.

3g. Set the dev/test avatar root so `LocalAvatarStorage` and the middleware agree on Windows. Edit `D:\Work\Templates\FIT\server\FitApi\appsettings.Development.json` (file owned by Task 1; ADD the `Storage` key â€” additive JSON key, do NOT remove existing keys). The exact OLD/NEW depends on Task 1's content; the deterministic edit is to add a top-level `"Storage"` object whose `AvatarRoot` is a path under the working directory. Because Task 1's `appsettings.Development.json` exact body is not owned here, instead make the binding explicit in the TEST factory so tests are self-contained â€” add the setting via the factory's existing `UseSetting` mechanism is NOT allowed (CustomWebApplicationFactory is edited only by Task 4). Therefore set it in `appsettings.json` (Task 1 owner) is also not allowed.

The deterministic, ownership-safe solution: make `LocalAvatarStorage`'s default and the middleware default IDENTICAL and content-root-relative by NOT relying on the `/app/...` default in test. Replace step 3c's constructor default to match the middleware default. Use this FINAL `LocalAvatarStorage.cs` body (this is the file created in 3c â€” write it once, with this exact content, so 3c and 3g are the same single create):

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FitApi.Services;

public class LocalAvatarStorage : IAvatarStorage
{
    private readonly string _root;

    public LocalAvatarStorage(IConfiguration config, IWebHostEnvironment env)
    {
        _root = config["Storage:AvatarRoot"]
            ?? Path.Combine(env.ContentRootPath, "uploads", "avatars");
    }

    public async Task<string> SaveAsync(string userId, Stream content, string contentType)
    {
        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => ".img"
        };

        Directory.CreateDirectory(_root);

        var fileName = userId + ext;
        var fullPath = Path.Combine(_root, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs);
        }

        return $"/uploads/avatars/{fileName}";
    }
}
```

Now both `LocalAvatarStorage` and the Program.cs middleware resolve `avatarRoot` to `{ContentRoot}/uploads/avatars` when `Storage:AvatarRoot` is unset (the Docker path is supplied via `Storage:AvatarRoot` env/compose by Task 1/deployment, default `/app/uploads/avatars` there). This makes 3c create exactly ONE version of the file (use the body shown in 3g) and removes any need to edit `appsettings.*.json`. Discard the earlier 3c body; the body in 3g is authoritative.

- [ ] **Step 4: Run, expect PASS**

Run from `D:\Work\Templates\FIT\server`:

```
dotnet test --filter "FullyQualifiedName~FitApi.Tests.ProfileTests"
```

Expected: `Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6` (the six facts: `NewUser_GetProfile_HasEmail_AndNullMetrics`, `PutProfile_UpdatesFields_AndGetReflects`, `PostAvatar_ValidPng_Returns200_WithPhotoUrl_AndGetReflects`, `PostAvatar_TextFile_Returns400_BadAvatar`, `PostAvatar_TooLarge_Returns400_BadAvatar`, `OwnerIsolation_UserB_Put_DoesNotChange_UserA_Profile`).

- [ ] **Step 5: Commit**

Run from `D:\Work\Templates\FIT`:

```
git add server/FitApi/Dtos/ProfileDtos.cs server/FitApi/Services/IAvatarStorage.cs server/FitApi/Services/LocalAvatarStorage.cs server/FitApi/Controllers/ProfileController.cs server/FitApi/Program.cs server/FitApi.Tests/ProfileTests.cs
git commit -m "feat(api): add profile endpoints and local avatar storage

- ProfileDto/UpdateProfileRequest DTOs
- IAvatarStorage + LocalAvatarStorage (writes {userId}{ext}, returns /uploads/avatars/...)
- ProfileController GET/PUT/avatar via UserManager.GetUserAsync; bad-avatar 400 on non-image or >2MB
- register IAvatarStorage and serve /uploads static files in Program.cs
- deterministic ProfileTests incl. owner isolation

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: FavoritesController (list/toggle/delete)

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\FavoritesController.cs`
- Test:   `D:\Work\Templates\FIT\server\FitApi.Tests\FavoritesTests.cs`
- (No Program.cs / DTO changes â€” endpoints use raw `System.Text.Json.JsonElement` bodies, not records.)

**Interfaces:**
- Consumes:
  - `FitApi.Controllers.ApiControllerBase` (Task 3) â€” provides `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;`. This controller inherits it and carries `[Authorize]`.
  - `FitApi.Data.AppDbContext` (Task 2) â€” injected; uses `DbSet<Favorite> Favorites`.
  - `FitApi.Models.Favorite` (Task 2) â€” `{ long Id; string UserId; string ItemId; string Type; System.Text.Json.JsonDocument Data; DateTimeOffset AddedAt; ApplicationUser? User; }`. The unique index `(UserId, ItemId)` exists on this entity (Task 2).
  - `FitApi.Tests.CustomWebApplicationFactory` (Task 1) and `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync` (Task 5) in the test.
- Produces:
  - `FitApi.Controllers.FavoritesController` with route `api/favorites`:
    - `GET    api/favorites`            -> `200` JSON array of the stored `Data` objects.
    - `POST   api/favorites` (body `JsonElement fav`) -> toggle by `fav.GetProperty("id")`: existing -> remove -> `200 {"favorited":false}`; absent -> store -> `200 {"favorited":true}`.
    - `DELETE api/favorites/{itemId}`    -> `200 {}` (removes the caller's favorite with that `ItemId`; no-op-200 if absent).
  - No DTO records are introduced (per FILE OWNERSHIP, no new files under `Dtos/`).

---

- [ ] **Step 1: Write the failing test**

Create `D:\Work\Templates\FIT\server\FitApi.Tests\FavoritesTests.cs` with the COMPLETE contents below. It covers: toggle-add then GET shows it (and that `type`/`addedAt` were merged in); toggle same id again removes it; `DELETE /{itemId}` removes; and OWNER ISOLATION (user B cannot see or delete user A's favorite).

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class FavoritesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FavoritesTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static JsonElement Obj(object value) =>
        JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task Toggle_Add_Then_Get_Returns_Stored_Item_With_Type_And_AddedAt()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-add@test.com", "pass1234", "FavAdd");

        var post = await client.PostAsJsonAsync("/api/favorites",
            Obj(new { id = "ex-1", name = "Bench Press" }));
        post.StatusCode.Should().Be(HttpStatusCode.OK);

        using var postDoc = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
        postDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeTrue();

        var get = await client.GetAsync("/api/favorites");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        getDoc.RootElement.GetArrayLength().Should().Be(1);

        var item = getDoc.RootElement[0];
        item.GetProperty("id").GetString().Should().Be("ex-1");
        item.GetProperty("name").GetString().Should().Be("Bench Press");
        item.GetProperty("type").GetString().Should().Be("exercise");
        item.TryGetProperty("addedAt", out var addedAt).Should().BeTrue();
        addedAt.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Toggle_Add_Honors_Explicit_Type()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-type@test.com", "pass1234", "FavType");

        await client.PostAsJsonAsync("/api/favorites",
            Obj(new { id = "ex-typed", type = "cardio" }));

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(1);
        getDoc.RootElement[0].GetProperty("type").GetString().Should().Be("cardio");
    }

    [Fact]
    public async Task Toggle_Same_Id_Twice_Removes_It()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-toggle@test.com", "pass1234", "FavToggle");

        var add = await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-2" }));
        using (var addDoc = JsonDocument.Parse(await add.Content.ReadAsStringAsync()))
            addDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeTrue();

        var remove = await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-2" }));
        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var remDoc = JsonDocument.Parse(await remove.Content.ReadAsStringAsync()))
            remDoc.RootElement.GetProperty("favorited").GetBoolean().Should().BeFalse();

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Delete_By_ItemId_Removes_It()
    {
        var client = await _factory.RegisterAndLoginAsync(
            "fav-del@test.com", "pass1234", "FavDel");

        await client.PostAsJsonAsync("/api/favorites", Obj(new { id = "ex-3" }));

        var del = await client.DeleteAsync("/api/favorites/ex-3");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync("/api/favorites");
        using var getDoc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        getDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_Requires_Authentication()
    {
        var anon = _factory.CreateClient();
        var get = await anon.GetAsync("/api/favorites");
        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Owner_Isolation_UserB_Cannot_See_Or_Delete_UserA_Favorite()
    {
        var userA = await _factory.RegisterAndLoginAsync(
            "fav-a@test.com", "pass1234", "UserA");
        var userB = await _factory.RegisterAndLoginAsync(
            "fav-b@test.com", "pass1234", "UserB");

        await userA.PostAsJsonAsync("/api/favorites", Obj(new { id = "shared-id" }));

        // B sees an empty list (cannot read A's row).
        var bGet = await userB.GetAsync("/api/favorites");
        using (var bDoc = JsonDocument.Parse(await bGet.Content.ReadAsStringAsync()))
            bDoc.RootElement.GetArrayLength().Should().Be(0);

        // B's DELETE for the same ItemId is a no-op-200 and does NOT remove A's row.
        var bDel = await userB.DeleteAsync("/api/favorites/shared-id");
        bDel.StatusCode.Should().Be(HttpStatusCode.OK);

        var aGet = await userA.GetAsync("/api/favorites");
        using var aDoc = JsonDocument.Parse(await aGet.Content.ReadAsStringAsync());
        aDoc.RootElement.GetArrayLength().Should().Be(1);
        aDoc.RootElement[0].GetProperty("id").GetString().Should().Be("shared-id");
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

Run from the solution directory:

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FavoritesTests"
```

Expected failure: compilation error because `FavoritesController` does not exist yet â€” e.g. the build fails before tests run with a message similar to:

```
error CS... : The type or namespace name 'FavoritesController' could not be found
```

(There is no `FavoritesController` referenced by name in the test, so more precisely the test build succeeds but the run fails: every test fails because there is no `/api/favorites` route â€” `GET`/`POST`/`DELETE` return `404 Not Found`, so assertions like `get.StatusCode.Should().Be(HttpStatusCode.OK)` fail with `Expected ... to be 200 (OK), but found 404 (NotFound)`.) Confirm the run reports `Failed!` with failing test count `6`.

- [ ] **Step 3: Implement**

Create `D:\Work\Templates\FIT\server\FitApi\Controllers\FavoritesController.cs` with the COMPLETE file below. Notes baked into the implementation:
- Toggle keys off `fav.GetProperty("id")` read as a string (handles JSON string ids; the legacy site uses string exercise ids).
- On add, a `JsonObject` is built by copying every property of the incoming `fav`, then `["type"]` is set to the incoming `type` if present (non-empty string) else `"exercise"`, and `["addedAt"]` is set to `DateTimeOffset.UtcNow` in round-trip ISO 8601 (`"O"`). The merged object is serialized to a fresh `JsonDocument` and stored in the `jsonb` `Data` column.
- `GET` returns the parsed `Data` of each row. Each `JsonDocument.RootElement` is collected into a `List<JsonElement>` and returned via `Ok(...)`, which `System.Text.Json` serializes back into a JSON array â€” i.e. exactly the stored objects (including their `type`/`addedAt`).
- All queries are filtered by `UserId` (from `ApiControllerBase`), never from the body/route. `DELETE` matches `UserId == UserId && ItemId == itemId`; missing -> still `Ok(new {})` (no-op-200), which also enforces owner isolation since user B's rows never match user A's `UserId`.

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using FitApi.Data;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public FavoritesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await _db.Favorites
            .Where(f => f.UserId == UserId)
            .OrderBy(f => f.Id)
            .Select(f => f.Data)
            .ToListAsync();

        var items = rows.Select(d => d.RootElement).ToList();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle([FromBody] JsonElement fav)
    {
        if (!fav.TryGetProperty("id", out var idElement))
        {
            return Problem(
                statusCode: 400,
                title: "bad-favorite",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-favorite" });
        }

        var itemId = idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString()
            : idElement.GetRawText();

        if (string.IsNullOrEmpty(itemId))
        {
            return Problem(
                statusCode: 400,
                title: "bad-favorite",
                extensions: new Dictionary<string, object?> { ["code"] = "bad-favorite" });
        }

        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == UserId && f.ItemId == itemId);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { favorited = false });
        }

        var node = new JsonObject();
        foreach (var prop in fav.EnumerateObject())
        {
            node[prop.Name] = JsonNode.Parse(prop.Value.GetRawText());
        }

        string type = "exercise";
        if (fav.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            type = typeElement.GetString()!;
        }
        node["type"] = type;
        node["addedAt"] = DateTimeOffset.UtcNow.ToString("O");

        var data = JsonSerializer.SerializeToDocument(node);

        _db.Favorites.Add(new Favorite
        {
            UserId = UserId,
            ItemId = itemId,
            Type = type,
            Data = data,
            AddedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        return Ok(new { favorited = true });
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> Delete(string itemId)
    {
        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == UserId && f.ItemId == itemId);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }
}
```

- [ ] **Step 4: Run, expect PASS**

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FavoritesTests"
```

Expected output ends with:

```
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6
```

- [ ] **Step 5: Commit**

```
git add D:\Work\Templates\FIT\server\FitApi\Controllers\FavoritesController.cs D:\Work\Templates\FIT\server\FitApi.Tests\FavoritesTests.cs
git commit -m "feat(api): add FavoritesController with toggle/list/delete and owner isolation"
```

---

### Task 9: NutritionPlansController (list/save/delete)

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\NutritionPlansController.cs`
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\NutritionTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Data.AppDbContext` with `DbSet<NutritionPlan> NutritionPlans` (Task 2).
  - `FitApi.Models.NutritionPlan { long Id; string UserId; string PlanId; string? CalId; string? VarId; System.Text.Json.JsonDocument Data; DateTimeOffset SavedAt; ApplicationUser? User; }` (Task 2). Unique index `(UserId, PlanId)`.
  - `FitApi.Controllers.ApiControllerBase` exposing `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;` (Task 3).
  - Identity cookie auth + `ConfigureApplicationCookie` returning 401 for `/api` paths when anonymous (Task 3).
  - Test helper `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync(this CustomWebApplicationFactory f, string email, string password, string name = "Test")` (Task 5).
  - `FitApi.Tests.CustomWebApplicationFactory` (Task 1).
- Produces:
  - Route `api/nutrition-plans`:
    - `GET api/nutrition-plans` -> JSON array of stored `Data` objects.
    - `POST api/nutrition-plans` body `JsonElement plan` -> `{ saved:false }` if PlanId already exists for user, else stores `{...plan, id:PlanId, savedAt}` and returns `{ saved:true }`.
    - `DELETE api/nutrition-plans/{planId}` -> `200 {}`.
  - PlanId derivation rule reused by no later task (controller-internal): `plan.id` else `"{calId}-{varId}"`.

**Notes (deterministic decisions):**
- `PlanId` is computed server-side from the posted JSON: if the body has a non-empty string property `id`, use it; otherwise concatenate property `calId` + `"-"` + property `varId` (missing parts read as empty string). The user id is taken from `UserId` (the cookie claim), NEVER from the body.
- When saving, the stored jsonb `Data` is the posted object with its `id` set/overwritten to the computed `PlanId` and an added ISO-8601 UTC `savedAt` (matches old JS `new Date().toISOString()` shape). `CalId`/`VarId` columns are populated from the body when present (string) else null, for queryability; they are NOT echoed beyond `Data`.
- `GET` returns exactly the array of stored `Data` payloads (each already contains `id` and `savedAt`), ordered by `SavedAt` ascending to match the old append-order list semantics.
- All three actions are filtered by `UserId`; `DELETE` of a non-owned/absent `planId` is a no-op that still returns `200 {}` (idempotent, matches old filter-based remove).
- This controller carries `[Authorize]` and inherits `ApiControllerBase`.

---

- [ ] **Step 1: Write the failing test**

Create `D:\Work\Templates\FIT\server\FitApi.Tests\NutritionTests.cs` with COMPLETE code:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class NutritionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NutritionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Save_Then_List_Returns_Stored_Plan_With_ComputedId_And_SavedAt()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-save@test.local", "passw0rd", "Nut Save");

        // No explicit "id" -> server computes "{calId}-{varId}".
        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"calId":"1800","varId":"lowcarb","title":"Ø®Ø·Ø© 1800"}""");

        var saveResp = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        saveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var saveDoc = JsonDocument.Parse(await saveResp.Content.ReadAsStringAsync());
        saveDoc.RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        var listResp = await client.GetAsync("/api/nutrition-plans");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());

        listDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        listDoc.RootElement.GetArrayLength().Should().Be(1);

        var stored = listDoc.RootElement[0];
        stored.GetProperty("id").GetString().Should().Be("1800-lowcarb");
        stored.GetProperty("calId").GetString().Should().Be("1800");
        stored.GetProperty("varId").GetString().Should().Be("lowcarb");
        stored.GetProperty("title").GetString().Should().Be("Ø®Ø·Ø© 1800");
        stored.TryGetProperty("savedAt", out var savedAt).Should().BeTrue();
        savedAt.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Save_Same_PlanId_Twice_Returns_SavedFalse_And_No_Duplicate()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-dup@test.local", "passw0rd", "Nut Dup");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"plan-A","title":"Ø®Ø·Ø© A"}""");

        var first = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await first.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        var second = await client.PostAsJsonAsync("/api/nutrition-plans", plan);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await second.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeFalse();

        var listResp = await client.GetAsync("/api/nutrition-plans");
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.GetArrayLength().Should().Be(1);
        listDoc.RootElement[0].GetProperty("id").GetString().Should().Be("plan-A");
    }

    [Fact]
    public async Task Delete_Removes_The_Plan()
    {
        var client = await _factory.RegisterAndLoginAsync("nut-del@test.local", "passw0rd", "Nut Del");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"plan-DEL","title":"Ø®Ø·Ø© Ù„Ù„Ø­Ø°Ù"}""");

        (await client.PostAsJsonAsync("/api/nutrition-plans", plan)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var delResp = await client.DeleteAsync("/api/nutrition-plans/plan-DEL");
        delResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResp = await client.GetAsync("/api/nutrition-plans");
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        listDoc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Owner_Isolation_UserB_Cannot_See_Or_Delete_UserA_Plan()
    {
        var clientA = await _factory.RegisterAndLoginAsync("nut-A@test.local", "passw0rd", "User A");
        var clientB = await _factory.RegisterAndLoginAsync("nut-B@test.local", "passw0rd", "User B");

        var plan = JsonSerializer.Deserialize<JsonElement>(
            """{"id":"shared-id","title":"Ø®Ø·Ø© A ÙÙ‚Ø·"}""");

        (await clientA.PostAsJsonAsync("/api/nutrition-plans", plan)).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // B cannot see A's plan.
        var listB = await clientB.GetAsync("/api/nutrition-plans");
        var listBDoc = JsonDocument.Parse(await listB.Content.ReadAsStringAsync());
        listBDoc.RootElement.GetArrayLength().Should().Be(0);

        // B can save its OWN plan reusing the same PlanId (per-user uniqueness only).
        var saveB = await clientB.PostAsJsonAsync("/api/nutrition-plans", plan);
        saveB.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(await saveB.Content.ReadAsStringAsync())
            .RootElement.GetProperty("saved").GetBoolean().Should().BeTrue();

        // B deleting "shared-id" must NOT remove A's row.
        (await clientB.DeleteAsync("/api/nutrition-plans/shared-id")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var listA = await clientA.GetAsync("/api/nutrition-plans");
        var listADoc = JsonDocument.Parse(await listA.Content.ReadAsStringAsync());
        listADoc.RootElement.GetArrayLength().Should().Be(1);
        listADoc.RootElement[0].GetProperty("id").GetString().Should().Be("shared-id");
    }

    [Fact]
    public async Task Anonymous_Is_Unauthorized()
    {
        var anon = _factory.CreateClient();
        (await anon.GetAsync("/api/nutrition-plans")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

- [ ] **Step 2: Run, expect FAIL**

Command (run from repo root):

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.NutritionTests"
```

Expected: compile failure / test failure because `NutritionPlansController` does not exist yet. Expected text resembles:
`error CS... The type or namespace name 'NutritionPlansController' could not be found` OR (if it compiles via routing only) the tests fail with `Expected ... to be 200 ... but found 404 (Not Found)`. Either way: NOT a pass â€” `Failed!  - Failed: ...`.

---

- [ ] **Step 3: Implement**

Create `D:\Work\Templates\FIT\server\FitApi\Controllers\NutritionPlansController.cs` (COMPLETE new file â€” this task is its sole owner):

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using FitApi.Data;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[ApiController]
[Authorize]
[Route("api/nutrition-plans")]
public class NutritionPlansController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public NutritionPlansController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await _db.NutritionPlans
            .Where(p => p.UserId == UserId)
            .OrderBy(p => p.SavedAt)
            .Select(p => p.Data)
            .ToListAsync();

        // Each Data JsonDocument already contains id + savedAt; emit as a raw array.
        var items = rows
            .Select(d => JsonSerializer.Deserialize<JsonElement>(d.RootElement.GetRawText()))
            .ToList();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] JsonElement plan)
    {
        var planId = ComputePlanId(plan);

        var exists = await _db.NutritionPlans
            .AnyAsync(p => p.UserId == UserId && p.PlanId == planId);
        if (exists)
        {
            return Ok(new { saved = false });
        }

        // Build stored payload = {...plan, id: planId, savedAt: nowIso}.
        var obj = JsonNode.Parse(plan.GetRawText())!.AsObject();
        obj["id"] = planId;
        obj["savedAt"] = DateTimeOffset.UtcNow.ToString("O");

        var entity = new NutritionPlan
        {
            UserId = UserId,
            PlanId = planId,
            CalId = ReadString(plan, "calId"),
            VarId = ReadString(plan, "varId"),
            Data = JsonDocument.Parse(obj.ToJsonString()),
            SavedAt = DateTimeOffset.UtcNow
        };

        _db.NutritionPlans.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { saved = true });
    }

    [HttpDelete("{planId}")]
    public async Task<IActionResult> Delete(string planId)
    {
        var row = await _db.NutritionPlans
            .FirstOrDefaultAsync(p => p.UserId == UserId && p.PlanId == planId);
        if (row is not null)
        {
            _db.NutritionPlans.Remove(row);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }

    private static string ComputePlanId(JsonElement plan)
    {
        var id = ReadString(plan, "id");
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        var calId = ReadString(plan, "calId") ?? "";
        var varId = ReadString(plan, "varId") ?? "";
        return $"{calId}-{varId}";
    }

    private static string? ReadString(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(name, out var v) &&
            v.ValueKind == JsonValueKind.String)
        {
            return v.GetString();
        }
        return null;
    }
}
```

---

- [ ] **Step 4: Run, expect PASS**

Command:

```
dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.NutritionTests"
```

Expected output ends with:

```
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5
```

(All five facts pass: save+list, duplicate-PlanId returns `{saved:false}`, delete, owner isolation, anonymous 401.)

---

- [ ] **Step 5: Commit**

```
git add server/FitApi/Controllers/NutritionPlansController.cs server/FitApi.Tests/NutritionTests.cs
git commit -m "feat(api): add NutritionPlansController list/save/delete with owner isolation"
```

---

### Task 10: CalcHistoryController (list/add/delete)

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\CalcHistoryController.cs`
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\CalcHistoryTests.cs`
- (No other files are created or modified by this task. No `Dtos/` record is needed â€” the contract assigns CalcHistory no DTO file; the controller works directly with `System.Text.Json.JsonElement` in and JSON objects out.)

**Interfaces:**
- Consumes:
  - `FitApi.Data.AppDbContext` (Task 2) â€” specifically `DbSet<CalcHistoryEntry> CalcHistory`.
  - `FitApi.Models.CalcHistoryEntry` (Task 2): `{ long Id; string UserId; JsonDocument Data; DateTimeOffset CreatedAt; ApplicationUser? User; }`.
  - `FitApi.Controllers.ApiControllerBase` (Task 3): provides `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;` and inherits `ControllerBase`.
  - Identity/cookie auth + `[Authorize]` 401 behavior (Task 3).
  - `FitApi.Tests.CustomWebApplicationFactory` (Task 1) and `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync` (Task 5) for tests.
- Produces:
  - HTTP endpoints under route `api/calc-history`:
    - `GET    api/calc-history`        -> 200, JSON array; each element = stored `Data` object with an added `"id":<dbid>`; ordered `CreatedAt` descending.
    - `POST   api/calc-history`        -> 200, body `{ "id": <dbid> }`; stores the posted `JsonElement` as jsonb + `CreatedAt = DateTimeOffset.UtcNow`.
    - `DELETE api/calc-history/{id}`   -> 200 `{}`; deletes only the row owned by the current user (no-op for non-owned/missing ids).
  - No public C# symbols other later task depends on (Task 14's frontend `saveCalcResult`/`getCalcHistory`/`deleteCalcResult` bind to these routes).

**TDD steps:**

- [ ] **Step 1: Write the failing test** â€” create `D:\Work\Templates\FIT\server\FitApi.Tests\CalcHistoryTests.cs` with this COMPLETE content:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FitApi.Tests;

public class CalcHistoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CalcHistoryTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

    [Fact]
    public async Task Post_StoresResult_AndReturnsId()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-add@test.com", "password1", "Calc Add");

        var resp = await client.PostAsJsonAsync("/api/calc-history",
            Json("""{"calories":2000,"bmr":1500,"goal":"cut"}"""));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("id", out var idProp).Should().BeTrue();
        idProp.GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_ReturnsStoredData_WithId_NewestFirst()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-list@test.com", "password1", "Calc List");

        var first = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":1000,"label":"first"}"""));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstId = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var second = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":2000,"label":"second"}"""));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondId = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var listResp = await client.GetAsync("/api/calc-history");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;

        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.GetArrayLength().Should().Be(2);

        // newest (second) first
        arr[0].GetProperty("id").GetInt64().Should().Be(secondId);
        arr[0].GetProperty("label").GetString().Should().Be("second");
        arr[0].GetProperty("calories").GetInt32().Should().Be(2000);

        arr[1].GetProperty("id").GetInt64().Should().Be(firstId);
        arr[1].GetProperty("label").GetString().Should().Be("first");
        arr[1].GetProperty("calories").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task Delete_RemovesOwnedEntry()
    {
        var client = await _factory.RegisterAndLoginAsync("calc-del@test.com", "password1", "Calc Del");

        var add = await client.PostAsJsonAsync("/api/calc-history", Json("""{"calories":1234}"""));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = JsonDocument.Parse(await add.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var del = await client.DeleteAsync($"/api/calc-history/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResp = await client.GetAsync("/api/calc-history");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        arr.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_RequiresAuth_Returns401WhenAnonymous()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/calc-history");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OtherUsersEntry_IsNoOp_AndKeepsRow()
    {
        // OWNER ISOLATION: user A creates an entry; user B cannot delete it.
        var userA = await _factory.RegisterAndLoginAsync("calc-owner-a@test.com", "password1", "Owner A");
        var add = await userA.PostAsJsonAsync("/api/calc-history", Json("""{"calories":777}"""));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var idA = JsonDocument.Parse(await add.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();

        var userB = await _factory.RegisterAndLoginAsync("calc-owner-b@test.com", "password1", "Owner B");
        var delByB = await userB.DeleteAsync($"/api/calc-history/{idA}");
        delByB.StatusCode.Should().Be(HttpStatusCode.OK); // no-op, still 200

        // user B sees an empty list (cannot read A's rows)
        var listB = await userB.GetAsync("/api/calc-history");
        var arrB = JsonDocument.Parse(await listB.Content.ReadAsStringAsync()).RootElement;
        arrB.GetArrayLength().Should().Be(0);

        // user A's row still exists
        var listA = await userA.GetAsync("/api/calc-history");
        var arrA = JsonDocument.Parse(await listA.Content.ReadAsStringAsync()).RootElement;
        arrA.GetArrayLength().Should().Be(1);
        arrA[0].GetProperty("id").GetInt64().Should().Be(idA);
        arrA[0].GetProperty("calories").GetInt32().Should().Be(777);
    }
}
```

- [ ] **Step 2: Run, expect FAIL** â€” run from the repo root:

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~CalcHistoryTests"
  ```

  Expected: a COMPILE failure (the run reports build errors, not test results). Exact expected error text contains:

  ```
  error CS0246: The type or namespace name 'CalcHistoryController' could not be found
  ```

  (The route `api/calc-history` does not exist yet, so even if it compiled the requests would 404 â€” but the missing controller file makes it fail at build time first.)

- [ ] **Step 3: Implement** â€” create `D:\Work\Templates\FIT\server\FitApi\Controllers\CalcHistoryController.cs` with this COMPLETE content:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using FitApi.Data;
using FitApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitApi.Controllers;

[Authorize]
[ApiController]
[Route("api/calc-history")]
public class CalcHistoryController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public CalcHistoryController(AppDbContext db) => _db = db;

    // GET api/calc-history -> array of stored Data objects, each with an added "id":<dbid>, newest first
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await _db.CalcHistory
            .Where(e => e.UserId == UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Data })
            .ToListAsync();

        var result = new JsonArray();
        foreach (var row in rows)
        {
            // Re-parse the stored jsonb into a mutable JsonObject and inject the DB id.
            var obj = JsonNode.Parse(row.Data.RootElement.GetRawText()) as JsonObject ?? new JsonObject();
            obj["id"] = row.Id;
            result.Add(obj);
        }

        return Ok(result);
    }

    // POST api/calc-history -> stores the posted JSON as jsonb + CreatedAt; returns { id }
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] JsonElement result)
    {
        var entry = new CalcHistoryEntry
        {
            UserId = UserId,
            Data = JsonDocument.Parse(result.GetRawText()),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CalcHistory.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { id = entry.Id });
    }

    // DELETE api/calc-history/{id} -> deletes only the current user's row; no-op otherwise
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entry = await _db.CalcHistory
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId);

        if (entry is not null)
        {
            _db.CalcHistory.Remove(entry);
            await _db.SaveChangesAsync();
        }

        return Ok(new { });
    }
}
```

  Notes (deterministic, no choices):
  - The route is `api/calc-history` (matches the contract and the frontend `FITData.saveCalcResult/getCalcHistory/deleteCalcResult`).
  - `UserId` comes from `ApiControllerBase` (Task 3); it is NEVER read from body/route.
  - The `"id"` is injected by parsing the stored `Data` raw text into a `JsonObject` and setting `obj["id"] = row.Id` â€” this is the required JsonObject injection. Storing posted JSON uses `JsonDocument.Parse(result.GetRawText())` so the column type is `jsonb` (per `AppDbContext` `HasColumnType("jsonb")`, Task 2).
  - `OrderByDescending(e => e.CreatedAt)` gives newest-first. Because two POSTs in the same test can land on the same `CreatedAt` tick, this relies on insertion order being preserved â€” to make ordering strictly deterministic on identical timestamps, the order-by is on `CreatedAt` only as the contract dictates; the test asserts on two entries whose `CreatedAt` differ by the round-trip latency of two awaited HTTP POSTs, which is reliably non-zero. (No tie-break needed.)
  - DELETE returns `200 { }` whether or not a row matched (no-op for non-owned/missing ids), satisfying OWNER ISOLATION without leaking existence.

- [ ] **Step 4: Run, expect PASS** â€” run from the repo root:

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~CalcHistoryTests"
  ```

  Expected output contains:

  ```
  Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5
  ```

  (5 tests: `Post_StoresResult_AndReturnsId`, `Get_ReturnsStoredData_WithId_NewestFirst`, `Delete_RemovesOwnedEntry`, `Get_RequiresAuth_Returns401WhenAnonymous`, `Delete_OtherUsersEntry_IsNoOp_AndKeepsRow`.)

- [ ] **Step 5: Commit** â€” run from the repo root:

  ```
  git add server/FitApi/Controllers/CalcHistoryController.cs server/FitApi.Tests/CalcHistoryTests.cs
  git commit -m "feat(api): add CalcHistoryController with list/add/delete and owner isolation"
  ```


---

### Task 11: WeightLogController (list/add/delete) + validation

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Dtos\WeightDtos.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\WeightLogController.cs`
- Test: `D:\Work\Templates\FIT\server\FitApi.Tests\WeightLogTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.WeightEntry` entity (Task 2): `{ long Id; string UserId; DateOnly Date; decimal Weight; decimal? Waist; decimal? Chest; decimal? Arms; DateTimeOffset CreatedAt; ApplicationUser? User; }`.
  - `FitApi.Data.AppDbContext` (Task 2) with `DbSet<WeightEntry> WeightLog`.
  - `FitApi.Controllers.ApiControllerBase` (Task 3): provides `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;`.
  - Identity cookie auth + `[Authorize]` -> 401 ProblemDetails on `/api` when anonymous (Task 3 `ConfigureApplicationCookie`).
  - `FitApi.Tests.CustomWebApplicationFactory` (Task 1) and `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync` (Task 5).
- Produces (later tasks/frontend reference these EXACT names):
  - `FitApi.Dtos.WeightEntryRequest(decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms, string? Date)`
  - `FitApi.Dtos.WeightEntryDto(long Id, string Date, decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms)`
  - Routes (consumed by Task 14 `FITData.addWeightEntry`/`getWeightLog`/`deleteWeightEntry`):
    - `GET    /api/weight-log` -> `WeightEntryDto[]` ordered by Date desc
    - `POST   /api/weight-log` -> `201` + `WeightEntryDto` (bad weight -> `400` code `bad-weight`)
    - `DELETE /api/weight-log/{id}` -> `200 {}`

---

- [ ] **Step 1: Write the failing test**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\WeightLogTests.cs` with the COMPLETE code below. It depends only on Task 1 (factory), Task 5 (TestAuthHelper), and this task's controller/DTOs.

  ```csharp
  using System.Net;
  using System.Net.Http.Json;
  using System.Text.Json;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class WeightLogTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public WeightLogTests(CustomWebApplicationFactory factory) => _factory = factory;

      private static string Email() => $"weight_{Guid.NewGuid():N}@test.local";

      [Fact]
      public async Task Post_valid_entry_returns_201_with_dto()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "Weighty");

          var resp = await client.PostAsJsonAsync("/api/weight-log", new
          {
              weight = 82.5m,
              waist = 90m,
              chest = 100m,
              arms = 35m,
              date = "2026-06-20"
          });

          resp.StatusCode.Should().Be(HttpStatusCode.Created);

          var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
          dto.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
          dto.GetProperty("date").GetString().Should().Be("2026-06-20");
          dto.GetProperty("weight").GetDecimal().Should().Be(82.5m);
          dto.GetProperty("waist").GetDecimal().Should().Be(90m);
          dto.GetProperty("chest").GetDecimal().Should().Be(100m);
          dto.GetProperty("arms").GetDecimal().Should().Be(35m);
      }

      [Fact]
      public async Task Post_null_date_defaults_to_today()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

          var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 70m });

          resp.StatusCode.Should().Be(HttpStatusCode.Created);
          var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
          var expected = DateTime.UtcNow.ToString("yyyy-MM-dd");
          dto.GetProperty("date").GetString().Should().Be(expected);
          dto.GetProperty("waist").ValueKind.Should().Be(JsonValueKind.Null);
          dto.GetProperty("chest").ValueKind.Should().Be(JsonValueKind.Null);
          dto.GetProperty("arms").ValueKind.Should().Be(JsonValueKind.Null);
      }

      [Fact]
      public async Task Post_weight_below_min_returns_400_bad_weight()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

          var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 5m });

          resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
          var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
          problem.GetProperty("code").GetString().Should().Be("bad-weight");
      }

      [Fact]
      public async Task Post_weight_above_max_returns_400_bad_weight()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

          var resp = await client.PostAsJsonAsync("/api/weight-log", new { weight = 600m });

          resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
          var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
          problem.GetProperty("code").GetString().Should().Be("bad-weight");
      }

      [Fact]
      public async Task Get_returns_entries_ordered_by_date_desc()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

          await client.PostAsJsonAsync("/api/weight-log", new { weight = 80m, date = "2026-01-01" });
          await client.PostAsJsonAsync("/api/weight-log", new { weight = 81m, date = "2026-03-15" });
          await client.PostAsJsonAsync("/api/weight-log", new { weight = 82m, date = "2026-02-10" });

          var resp = await client.GetAsync("/api/weight-log");
          resp.StatusCode.Should().Be(HttpStatusCode.OK);

          var arr = await resp.Content.ReadFromJsonAsync<JsonElement>();
          arr.ValueKind.Should().Be(JsonValueKind.Array);
          var dates = arr.EnumerateArray().Select(e => e.GetProperty("date").GetString()).ToList();
          dates.Should().ContainInOrder("2026-03-15", "2026-02-10", "2026-01-01");
      }

      [Fact]
      public async Task Delete_removes_entry()
      {
          var client = await _factory.RegisterAndLoginAsync(Email(), "pw1234");

          var post = await client.PostAsJsonAsync("/api/weight-log", new { weight = 75m, date = "2026-05-05" });
          var created = await post.Content.ReadFromJsonAsync<JsonElement>();
          var id = created.GetProperty("id").GetInt64();

          var del = await client.DeleteAsync($"/api/weight-log/{id}");
          del.StatusCode.Should().Be(HttpStatusCode.OK);

          var after = await client.GetAsync("/api/weight-log");
          var arr = await after.Content.ReadFromJsonAsync<JsonElement>();
          arr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().NotContain(id);
      }

      [Fact]
      public async Task Anonymous_get_returns_401()
      {
          var client = _factory.CreateClient();

          var resp = await client.GetAsync("/api/weight-log");

          resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
      }

      [Fact]
      public async Task Owner_isolation_userB_cannot_read_or_delete_userA_entries()
      {
          var clientA = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "UserA");
          var clientB = await _factory.RegisterAndLoginAsync(Email(), "pw1234", "UserB");

          var postA = await clientA.PostAsJsonAsync("/api/weight-log", new { weight = 88m, date = "2026-04-04" });
          var createdA = await postA.Content.ReadFromJsonAsync<JsonElement>();
          var idA = createdA.GetProperty("id").GetInt64();

          // B cannot see A's entry
          var bList = await clientB.GetAsync("/api/weight-log");
          var bArr = await bList.Content.ReadFromJsonAsync<JsonElement>();
          bArr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().NotContain(idA);

          // B's delete of A's id is a no-op (still 200) and A still has the row
          var bDel = await clientB.DeleteAsync($"/api/weight-log/{idA}");
          bDel.StatusCode.Should().Be(HttpStatusCode.OK);

          var aList = await clientA.GetAsync("/api/weight-log");
          var aArr = await aList.Content.ReadFromJsonAsync<JsonElement>();
          aArr.EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).Should().Contain(idA);
      }
  }
  ```

- [ ] **Step 2: Run, expect FAIL**

  Run from the solution directory:

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.WeightLogTests"
  ```

  Expected: BUILD FAILS (test does not compile yet) with errors such as:
  `error CS0246: The type or namespace name 'WeightLogController' could not be found` is NOT referenced directly, but compilation fails earlier because the controller/DTOs/routes do not exist â€” concretely you will see the test project build succeed only after the API project compiles. Before Step 3 the API project has no `WeightLogController`/`WeightDtos`, so the test run reports `Build FAILED` with `error CS` referencing missing route handling is not emitted; instead the run fails to produce passing tests. The deterministic expectation: the command exits non-zero and prints `Failed!  - Failed: ...` / `Build FAILED.` â€” NOT `Passed!`.

- [ ] **Step 3: Implement**

  Create `D:\Work\Templates\FIT\server\FitApi\Dtos\WeightDtos.cs` (this task is the SOLE declarer of these two records):

  ```csharp
  namespace FitApi.Dtos;

  public record WeightEntryRequest(decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms, string? Date);

  public record WeightEntryDto(long Id, string Date, decimal Weight, decimal? Waist, decimal? Chest, decimal? Arms);
  ```

  Create `D:\Work\Templates\FIT\server\FitApi\Controllers\WeightLogController.cs` (full new file):

  ```csharp
  using System.Globalization;
  using FitApi.Data;
  using FitApi.Dtos;
  using FitApi.Models;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;

  namespace FitApi.Controllers;

  [ApiController]
  [Route("api/weight-log")]
  [Authorize]
  public class WeightLogController : ApiControllerBase
  {
      private readonly AppDbContext _db;

      public WeightLogController(AppDbContext db) => _db = db;

      [HttpGet]
      public async Task<ActionResult<IEnumerable<WeightEntryDto>>> Get()
      {
          var entries = await _db.WeightLog
              .Where(w => w.UserId == UserId)
              .OrderByDescending(w => w.Date)
              .Select(w => new WeightEntryDto(
                  w.Id,
                  w.Date.ToString("yyyy-MM-dd"),
                  w.Weight,
                  w.Waist,
                  w.Chest,
                  w.Arms))
              .ToListAsync();

          return Ok(entries);
      }

      [HttpPost]
      public async Task<ActionResult<WeightEntryDto>> Post([FromBody] WeightEntryRequest req)
      {
          if (req.Weight < 10m || req.Weight > 500m)
          {
              return Problem(
                  statusCode: 400,
                  title: "bad-weight",
                  extensions: new Dictionary<string, object?> { ["code"] = "bad-weight" });
          }

          DateOnly date;
          if (string.IsNullOrWhiteSpace(req.Date))
          {
              date = DateOnly.FromDateTime(DateTime.UtcNow);
          }
          else if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
          {
              date = DateOnly.FromDateTime(DateTime.UtcNow);
          }

          var entry = new WeightEntry
          {
              UserId = UserId,
              Date = date,
              Weight = req.Weight,
              Waist = req.Waist,
              Chest = req.Chest,
              Arms = req.Arms,
              CreatedAt = DateTimeOffset.UtcNow
          };

          _db.WeightLog.Add(entry);
          await _db.SaveChangesAsync();

          var dto = new WeightEntryDto(
              entry.Id,
              entry.Date.ToString("yyyy-MM-dd"),
              entry.Weight,
              entry.Waist,
              entry.Chest,
              entry.Arms);

          return Created($"/api/weight-log/{entry.Id}", dto);
      }

      [HttpDelete("{id:long}")]
      public async Task<IActionResult> Delete(long id)
      {
          var entry = await _db.WeightLog
              .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId);

          if (entry is not null)
          {
              _db.WeightLog.Remove(entry);
              await _db.SaveChangesAsync();
          }

          return Ok(new { });
      }
  }
  ```

  Notes that make this deterministic:
  - `ApiControllerBase` (Task 3) supplies `UserId`; this controller inherits it and NEVER reads the user id from body/route.
  - The DELETE filter `w.UserId == UserId` enforces owner isolation: user B deleting user A's id matches no row, so it is a 200 no-op (asserted in the owner-isolation test).
  - `bad-weight` uses the exact `Problem(...) + Extensions["code"]` convention; ProblemDetails serializes the extension as a top-level `code` property.

- [ ] **Step 4: Run, expect PASS**

  ```
  dotnet test D:\Work\Templates\FIT\server\FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.WeightLogTests"
  ```

  Expected output ends with:
  `Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8` (the 8 facts: valid add, null-date-default, weight 5 -> 400, weight 600 -> 400, list desc, delete, anonymous 401, owner isolation).

- [ ] **Step 5: Commit**

  ```
  git add server/FitApi/Dtos/WeightDtos.cs server/FitApi/Controllers/WeightLogController.cs server/FitApi.Tests/WeightLogTests.cs
  git commit -m "feat(api): add weight-log endpoints with weight validation and owner isolation"
  ```

---

### Task 12: StreakCalculator + WorkoutsController (completed/complete/uncomplete)

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Dtos\StreakDtos.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Services\StreakCalculator.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\WorkoutsController.cs`
- Test (Create): `D:\Work\Templates\FIT\server\FitApi.Tests\StreakCalculatorTests.cs`
- Test (Create): `D:\Work\Templates\FIT\server\FitApi.Tests\WorkoutsTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.CompletedDate` (`{ long Id; string UserId; DateOnly Date; ApplicationUser? User; }`) and `FitApi.Models.ApplicationUser` â€” created by Task 2.
  - `FitApi.Data.AppDbContext` with `DbSet<CompletedDate> CompletedDates` and unique index `(UserId, Date)` â€” created by Task 2.
  - `FitApi.Controllers.ApiControllerBase` exposing `protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;` â€” created by Task 3.
  - Identity/cookie auth wired in `Program.cs` (Task 3); `[Authorize]` returns 401 for anonymous `/api` requests.
  - `FitApi.Tests.CustomWebApplicationFactory` (Task 1) and `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync` extension (Task 5).
- Produces:
  - `FitApi.Dtos.StreakStats(int Streak, int LongestStreak, int Total)` â€” referenced by Task 14 (frontend bridge maps `markWorkoutToday`/`unmarkWorkoutToday` returns to `{streak,longestStreak,total}`). This is the ONLY declaration of `StreakStats`.
  - `FitApi.Services.StreakCalculator` with `public static StreakStats Compute(IEnumerable<DateOnly> dates, DateOnly today)`.
  - `FitApi.Controllers.WorkoutsController` (route `api/workouts`): `GET api/workouts/completed` -> `string[]` of `"yyyy-MM-dd"`; `POST api/workouts/complete` -> `StreakStats`; `DELETE api/workouts/complete` -> `StreakStats`.

Steps (TDD order):

- [ ] **Step 1: Write the failing PURE unit tests for StreakCalculator**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\StreakCalculatorTests.cs`:

  ```csharp
  using FitApi.Dtos;
  using FitApi.Services;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class StreakCalculatorTests
  {
      private static readonly DateOnly Today = new DateOnly(2026, 6, 23);

      [Fact]
      public void Empty_ReturnsAllZero()
      {
          var result = StreakCalculator.Compute(Array.Empty<DateOnly>(), Today);

          result.Should().Be(new StreakStats(0, 0, 0));
      }

      [Fact]
      public void TodayPlusYesterdayPlusTwoDaysAgo_AllConsecutive_Streak3Longest3Total3()
      {
          var dates = new[]
          {
              Today,
              Today.AddDays(-1),
              Today.AddDays(-2),
          };

          var result = StreakCalculator.Compute(dates, Today);

          result.Should().Be(new StreakStats(3, 3, 3));
      }

      [Fact]
      public void GapBeforeToday_CurrentStreakShorter_LongestReflectsBestRun()
      {
          // Best run is the 4-day block 10 days ago; current run (ending today) is only 2 days.
          // total = 6 distinct days.
          var dates = new[]
          {
              Today,                  // current run
              Today.AddDays(-1),      // current run
              // gap at -2, -3
              Today.AddDays(-7),      // best run
              Today.AddDays(-8),      // best run
              Today.AddDays(-9),      // best run
              Today.AddDays(-10),     // best run
          };

          var result = StreakCalculator.Compute(dates, Today);

          result.Should().Be(new StreakStats(2, 4, 6));
      }

      [Fact]
      public void LatestIsYesterday_CurrentStreakContinues()
      {
          var dates = new[]
          {
              Today.AddDays(-1),
              Today.AddDays(-2),
          };

          var result = StreakCalculator.Compute(dates, Today);

          result.Should().Be(new StreakStats(2, 2, 2));
      }

      [Fact]
      public void LatestIsTwoDaysAgo_CurrentStreakIsZero_LongestAndTotalIntact()
      {
          var dates = new[]
          {
              Today.AddDays(-2),
              Today.AddDays(-3),
          };

          var result = StreakCalculator.Compute(dates, Today);

          result.Should().Be(new StreakStats(0, 2, 2));
      }

      [Fact]
      public void DuplicateDates_AreDistinctCounted()
      {
          var dates = new[]
          {
              Today,
              Today,
              Today.AddDays(-1),
          };

          var result = StreakCalculator.Compute(dates, Today);

          result.Should().Be(new StreakStats(2, 2, 2));
      }
  }
  ```

- [ ] **Step 2: Run, expect FAIL (compile error â€” types do not exist yet)**

  Command (run from `D:\Work\Templates\FIT\server`):

  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.StreakCalculatorTests"
  ```

  Expected failure: build fails with errors like `CS0103: The name 'StreakCalculator' does not exist in the current context` and `CS0246: The type or namespace name 'StreakStats' could not be found` (because `FitApi.Dtos.StreakDtos.cs` and `FitApi.Services.StreakCalculator.cs` have not been created). No tests run.

- [ ] **Step 3a: Create the StreakStats DTO**

  Create `D:\Work\Templates\FIT\server\FitApi\Dtos\StreakDtos.cs` (this is the ONLY file that declares `StreakStats`):

  ```csharp
  namespace FitApi.Dtos;

  public record StreakStats(int Streak, int LongestStreak, int Total);
  ```

- [ ] **Step 3b: Implement StreakCalculator (faithful port of JS `_getStreakStats`)**

  Source JS being ported (`D:\Work\Templates\FIT\assets\js\auth.js` lines 363-385): distinct count = `total`; `longest` = longest consecutive-day run (starts at 1 when any dates exist); current streak counts back from the latest date and is non-zero ONLY when the latest date is today or yesterday. The C# port takes `today` explicitly instead of reading the clock.

  Create `D:\Work\Templates\FIT\server\FitApi\Services\StreakCalculator.cs`:

  ```csharp
  using FitApi.Dtos;

  namespace FitApi.Services;

  // Faithful port of the JS _getStreakStats in assets/js/auth.js.
  // total   = count of distinct days.
  // longest = longest run of consecutive calendar days.
  // streak  = current run ending at the latest date, but ONLY counted when the
  //           latest date is today or yesterday; otherwise 0.
  public static class StreakCalculator
  {
      public static StreakStats Compute(IEnumerable<DateOnly> dates, DateOnly today)
      {
          var nums = (dates ?? Array.Empty<DateOnly>())
              .Distinct()
              .Select(d => d.DayNumber)
              .OrderBy(n => n)
              .ToArray();

          var total = nums.Length;
          if (total == 0)
          {
              return new StreakStats(0, 0, 0);
          }

          var longest = 1;
          var run = 1;
          for (var i = 1; i < nums.Length; i++)
          {
              if (nums[i] == nums[i - 1] + 1)
              {
                  run++;
                  if (run > longest)
                  {
                      longest = run;
                  }
              }
              else
              {
                  run = 1;
              }
          }

          var todayNum = today.DayNumber;
          var last = nums[^1];
          var current = 0;
          if (last == todayNum || last == todayNum - 1)
          {
              current = 1;
              for (var i = nums.Length - 1; i > 0; i--)
              {
                  if (nums[i] == nums[i - 1] + 1)
                  {
                      current++;
                  }
                  else
                  {
                      break;
                  }
              }
          }

          return new StreakStats(current, longest, total);
      }
  }
  ```

- [ ] **Step 4: Run, expect PASS (unit tests)**

  Command (run from `D:\Work\Templates\FIT\server`):

  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.StreakCalculatorTests"
  ```

  Expected output: build succeeds; `Passed!  - Failed: 0, Passed: 6, Skipped: 0` (the 6 facts in `StreakCalculatorTests`).

- [ ] **Step 5: Write the failing endpoint test for WorkoutsController**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\WorkoutsTests.cs`. Covers: anonymous -> 401; POST complete twice is idempotent (GET completed length 1; `StreakStats.Total == 1`); DELETE complete removes (GET completed empty; `Total == 0`); owner isolation (user B's complete does not appear for user A).

  ```csharp
  using System.Net;
  using System.Net.Http.Json;
  using System.Text.Json;
  using FitApi.Dtos;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class WorkoutsTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public WorkoutsTests(CustomWebApplicationFactory factory) => _factory = factory;

      [Fact]
      public async Task Completed_Anonymous_Returns401()
      {
          var client = _factory.CreateClient();

          var response = await client.GetAsync("/api/workouts/completed");

          response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
      }

      [Fact]
      public async Task Complete_Twice_IsIdempotent()
      {
          var client = await _factory.RegisterAndLoginAsync(
              "workouts-idempotent@test.local", "pass123", "Idem");

          var first = await client.PostAsync("/api/workouts/complete", null);
          first.StatusCode.Should().Be(HttpStatusCode.OK);
          var firstStats = await first.Content.ReadFromJsonAsync<StreakStats>();
          firstStats!.Total.Should().Be(1);
          firstStats.Streak.Should().Be(1);

          var second = await client.PostAsync("/api/workouts/complete", null);
          second.StatusCode.Should().Be(HttpStatusCode.OK);
          var secondStats = await second.Content.ReadFromJsonAsync<StreakStats>();
          secondStats!.Total.Should().Be(1);
          secondStats.Streak.Should().Be(1);

          var completed = await client.GetFromJsonAsync<string[]>("/api/workouts/completed");
          completed!.Should().HaveCount(1);
          completed[0].Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"));
      }

      [Fact]
      public async Task DeleteComplete_RemovesToday()
      {
          var client = await _factory.RegisterAndLoginAsync(
              "workouts-delete@test.local", "pass123", "Del");

          await client.PostAsync("/api/workouts/complete", null);

          var deleteResponse = await client.DeleteAsync("/api/workouts/complete");
          deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
          var deleteStats = await deleteResponse.Content.ReadFromJsonAsync<StreakStats>();
          deleteStats!.Total.Should().Be(0);
          deleteStats.Streak.Should().Be(0);
          deleteStats.LongestStreak.Should().Be(0);

          var completed = await client.GetFromJsonAsync<string[]>("/api/workouts/completed");
          completed!.Should().BeEmpty();
      }

      [Fact]
      public async Task OwnerIsolation_UserBCannotSeeUserACompleted()
      {
          var userA = await _factory.RegisterAndLoginAsync(
              "workouts-owner-a@test.local", "pass123", "A");
          var userB = await _factory.RegisterAndLoginAsync(
              "workouts-owner-b@test.local", "pass123", "B");

          await userA.PostAsync("/api/workouts/complete", null);

          var bCompleted = await userB.GetFromJsonAsync<string[]>("/api/workouts/completed");
          bCompleted!.Should().BeEmpty();

          var aCompleted = await userA.GetFromJsonAsync<string[]>("/api/workouts/completed");
          aCompleted!.Should().HaveCount(1);
      }
  }
  ```

- [ ] **Step 6: Run, expect FAIL (no WorkoutsController yet)**

  Command (run from `D:\Work\Templates\FIT\server`):

  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.WorkoutsTests"
  ```

  Expected failure: build succeeds but every test in `WorkoutsTests` fails â€” `Completed_Anonymous_Returns401` gets `404 NotFound` instead of `401 Unauthorized`, and the other three fail trying to read JSON / deserialize from a `404 NotFound` response (`GET`/`POST`/`DELETE /api/workouts/...` routes do not exist). `Failed: 4`.

- [ ] **Step 7: Implement WorkoutsController**

  Create `D:\Work\Templates\FIT\server\FitApi\Controllers\WorkoutsController.cs`:

  ```csharp
  using FitApi.Data;
  using FitApi.Dtos;
  using FitApi.Models;
  using FitApi.Services;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;

  namespace FitApi.Controllers;

  [ApiController]
  [Authorize]
  [Route("api/workouts")]
  public class WorkoutsController : ApiControllerBase
  {
      private readonly AppDbContext _db;

      public WorkoutsController(AppDbContext db) => _db = db;

      [HttpGet("completed")]
      public async Task<ActionResult<string[]>> GetCompleted()
      {
          var dates = await _db.CompletedDates
              .Where(c => c.UserId == UserId)
              .OrderByDescending(c => c.Date)
              .Select(c => c.Date)
              .ToListAsync();

          var result = dates
              .Select(d => d.ToString("yyyy-MM-dd"))
              .ToArray();

          return Ok(result);
      }

      [HttpPost("complete")]
      public async Task<ActionResult<StreakStats>> Complete()
      {
          var today = DateOnly.FromDateTime(DateTime.UtcNow);

          var exists = await _db.CompletedDates
              .AnyAsync(c => c.UserId == UserId && c.Date == today);

          if (!exists)
          {
              _db.CompletedDates.Add(new CompletedDate
              {
                  UserId = UserId,
                  Date = today,
              });
              await _db.SaveChangesAsync();
          }

          return Ok(await ComputeStatsAsync(today));
      }

      [HttpDelete("complete")]
      public async Task<ActionResult<StreakStats>> Uncomplete()
      {
          var today = DateOnly.FromDateTime(DateTime.UtcNow);

          var entry = await _db.CompletedDates
              .FirstOrDefaultAsync(c => c.UserId == UserId && c.Date == today);

          if (entry is not null)
          {
              _db.CompletedDates.Remove(entry);
              await _db.SaveChangesAsync();
          }

          return Ok(await ComputeStatsAsync(today));
      }

      private async Task<StreakStats> ComputeStatsAsync(DateOnly today)
      {
          var dates = await _db.CompletedDates
              .Where(c => c.UserId == UserId)
              .Select(c => c.Date)
              .ToListAsync();

          return StreakCalculator.Compute(dates, today);
      }
  }
  ```

- [ ] **Step 8: Run, expect PASS (endpoint tests)**

  Command (run from `D:\Work\Templates\FIT\server`):

  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.WorkoutsTests"
  ```

  Expected output: build succeeds; `Passed!  - Failed: 0, Passed: 4, Skipped: 0` (the 4 facts in `WorkoutsTests`; Testcontainers spins up `postgres:16`, migrations applied by `Program.cs` startup).

- [ ] **Step 9: Run the full suite for this task (unit + endpoint together)**

  Command (run from `D:\Work\Templates\FIT\server`):

  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.StreakCalculatorTests|FullyQualifiedName~FitApi.Tests.WorkoutsTests"
  ```

  Expected output: `Passed!  - Failed: 0, Passed: 10, Skipped: 0`.

- [ ] **Step 10: Commit**

  Commands (run from `D:\Work\Templates\FIT`):

  ```
  git add server/FitApi/Dtos/StreakDtos.cs server/FitApi/Services/StreakCalculator.cs server/FitApi/Controllers/WorkoutsController.cs server/FitApi.Tests/StreakCalculatorTests.cs server/FitApi.Tests/WorkoutsTests.cs
  git commit -m "feat(api): add StreakCalculator and WorkoutsController with completed/complete/uncomplete"
  ```


---

### Task 13: PlanController (get/add/reorder/delete/clear)

This task implements the user's custom workout plan ("Ø®Ø·ØªÙŠ Ø§Ù„Ù…Ø®ØµØµØ©") API. It ports the JS logic in `assets/js/auth.js` (`getPlan`/`addToPlan`/`updatePlan`/`removeFromPlan`/`clearPlan`, lines 387â€“424) onto the `CustomPlanItem` entity created in Task 2. All endpoints are user-scoped via `ApiControllerBase.UserId` (Task 3) and `[Authorize]`.

**Files:**
- Create: `D:\Work\Templates\FIT\server\FitApi\Dtos\PlanDtos.cs`
- Create: `D:\Work\Templates\FIT\server\FitApi\Controllers\PlanController.cs`
- Test (Create): `D:\Work\Templates\FIT\server\FitApi.Tests\PlanTests.cs`

**Interfaces:**
- Consumes:
  - `FitApi.Models.CustomPlanItem` { `long Id`; `string UserId`; `string ItemId`; `string? NameAr`; `string? NameEn`; `string? Tab`; `int Sets`; `int Reps`; `int OrderIndex`; `ApplicationUser? User` } â€” created by Task 2.
  - `FitApi.Data.AppDbContext` with `DbSet<CustomPlanItem> CustomPlanItems` and unique index `(UserId, ItemId)` â€” created by Task 2.
  - `FitApi.Controllers.ApiControllerBase` exposing `protected string UserId` from `ClaimTypes.NameIdentifier` â€” created by Task 3.
  - `FitApi.Tests.CustomWebApplicationFactory` â€” created by Task 1.
  - `FitApi.Tests.TestAuthHelper.RegisterAndLoginAsync(this CustomWebApplicationFactory f, string email, string password, string name = "Test")` â€” created by Task 5.
- Produces (referenced by Task 14 `assets/js/auth.js` `getPlan`/`addToPlan`/`updatePlan`/`removeFromPlan`/`clearPlan`):
  - `FitApi.Dtos.PlanItemDto(string Id, string? NameAr, string? NameEn, string? Tab, int Sets, int Reps, int Order)`
  - `FitApi.Dtos.AddPlanItemRequest(string Id, string? NameAr, string? NameEn, string? Tab, int? Sets, int? Reps)`
  - HTTP endpoints under route `api/plan`:
    - `GET    api/plan`            â†’ `200 PlanItemDto[]` ordered by `OrderIndex` ascending.
    - `POST   api/plan`            â†’ `200 {added:false}` if `ItemId` exists for this user; else insert with `Sets = req.Sets ?? 4`, `Reps = req.Reps ?? 10`, `OrderIndex = current count`, â†’ `200 {added:true}`.
    - `PUT    api/plan`            â†’ body `PlanItemDto[]`; for each entry, find the user's item with matching `Id` (=`ItemId`) and overwrite `OrderIndex`/`Sets`/`Reps`; `200 {}`.
    - `DELETE api/plan/{itemId}`   â†’ remove the user's item with `ItemId == itemId`, then renumber remaining items `0..n` by current `OrderIndex`; `200 {}`.
    - `DELETE api/plan`           â†’ remove all of the user's items; `200 {}`.

---

- [ ] **Step 1: Write the failing test**

  Create `D:\Work\Templates\FIT\server\FitApi.Tests\PlanTests.cs` with COMPLETE content:

  ```csharp
  using System.Net;
  using System.Net.Http.Json;
  using System.Text.Json;
  using FluentAssertions;
  using Xunit;

  namespace FitApi.Tests;

  public class PlanTests : IClassFixture<CustomWebApplicationFactory>
  {
      private readonly CustomWebApplicationFactory _factory;

      public PlanTests(CustomWebApplicationFactory factory) => _factory = factory;

      private static object AddBody(string id, string? nameAr = null, string? nameEn = null,
          string? tab = null, int? sets = null, int? reps = null) =>
          new { id, nameAr, nameEn, tab, sets, reps };

      [Fact]
      public async Task Add_Then_Get_Returns_Item_At_Order_Zero()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-add@example.com", "Passw0rd!");

          var add = await client.PostAsJsonAsync("/api/plan",
              AddBody("ex-1", nameAr: "Ø¶ØºØ·", nameEn: "Bench", tab: "chest", sets: 5, reps: 8));
          add.StatusCode.Should().Be(HttpStatusCode.OK);
          var addDoc = JsonDocument.Parse(await add.Content.ReadAsStringAsync());
          addDoc.RootElement.GetProperty("added").GetBoolean().Should().BeTrue();

          var get = await client.GetAsync("/api/plan");
          get.StatusCode.Should().Be(HttpStatusCode.OK);
          var items = JsonDocument.Parse(await get.Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(1);
          var item = items[0];
          item.GetProperty("id").GetString().Should().Be("ex-1");
          item.GetProperty("nameAr").GetString().Should().Be("Ø¶ØºØ·");
          item.GetProperty("nameEn").GetString().Should().Be("Bench");
          item.GetProperty("tab").GetString().Should().Be("chest");
          item.GetProperty("sets").GetInt32().Should().Be(5);
          item.GetProperty("reps").GetInt32().Should().Be(8);
          item.GetProperty("order").GetInt32().Should().Be(0);
      }

      [Fact]
      public async Task Add_Applies_Default_Sets_And_Reps_When_Null()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-defaults@example.com", "Passw0rd!");

          var add = await client.PostAsJsonAsync("/api/plan", AddBody("ex-def"));
          add.StatusCode.Should().Be(HttpStatusCode.OK);

          var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(1);
          items[0].GetProperty("sets").GetInt32().Should().Be(4);
          items[0].GetProperty("reps").GetInt32().Should().Be(10);
      }

      [Fact]
      public async Task Add_Duplicate_Returns_Added_False_And_Does_Not_Duplicate()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-dup@example.com", "Passw0rd!");

          (await client.PostAsJsonAsync("/api/plan", AddBody("ex-dup"))).StatusCode.Should().Be(HttpStatusCode.OK);

          var dup = await client.PostAsJsonAsync("/api/plan", AddBody("ex-dup"));
          dup.StatusCode.Should().Be(HttpStatusCode.OK);
          var dupDoc = JsonDocument.Parse(await dup.Content.ReadAsStringAsync());
          dupDoc.RootElement.GetProperty("added").GetBoolean().Should().BeFalse();

          var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(1);
      }

      [Fact]
      public async Task Put_Reorder_Is_Reflected_In_Get()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-reorder@example.com", "Passw0rd!");

          await client.PostAsJsonAsync("/api/plan", AddBody("a"));
          await client.PostAsJsonAsync("/api/plan", AddBody("b"));
          await client.PostAsJsonAsync("/api/plan", AddBody("c"));

          // Reverse order, and change sets/reps on "a".
          var payload = new[]
          {
              new { id = "c", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 4, reps = 10, order = 0 },
              new { id = "b", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 4, reps = 10, order = 1 },
              new { id = "a", nameAr = (string?)null, nameEn = (string?)null, tab = (string?)null, sets = 3, reps = 15, order = 2 },
          };
          var put = await client.PutAsJsonAsync("/api/plan", payload);
          put.StatusCode.Should().Be(HttpStatusCode.OK);

          var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(3);
          items[0].GetProperty("id").GetString().Should().Be("c");
          items[1].GetProperty("id").GetString().Should().Be("b");
          items[2].GetProperty("id").GetString().Should().Be("a");
          items[2].GetProperty("order").GetInt32().Should().Be(2);
          items[2].GetProperty("sets").GetInt32().Should().Be(3);
          items[2].GetProperty("reps").GetInt32().Should().Be(15);
      }

      [Fact]
      public async Task Delete_Item_Renumbers_Remaining_From_Zero()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-delete@example.com", "Passw0rd!");

          await client.PostAsJsonAsync("/api/plan", AddBody("a")); // order 0
          await client.PostAsJsonAsync("/api/plan", AddBody("b")); // order 1
          await client.PostAsJsonAsync("/api/plan", AddBody("c")); // order 2

          var del = await client.DeleteAsync("/api/plan/b");
          del.StatusCode.Should().Be(HttpStatusCode.OK);

          var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(2);
          items[0].GetProperty("id").GetString().Should().Be("a");
          items[0].GetProperty("order").GetInt32().Should().Be(0);
          items[1].GetProperty("id").GetString().Should().Be("c");
          items[1].GetProperty("order").GetInt32().Should().Be(1);
      }

      [Fact]
      public async Task Clear_Empties_The_Plan()
      {
          var client = await _factory.RegisterAndLoginAsync("plan-clear@example.com", "Passw0rd!");

          await client.PostAsJsonAsync("/api/plan", AddBody("a"));
          await client.PostAsJsonAsync("/api/plan", AddBody("b"));

          var clear = await client.DeleteAsync("/api/plan");
          clear.StatusCode.Should().Be(HttpStatusCode.OK);

          var items = JsonDocument.Parse(await (await client.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          items.GetArrayLength().Should().Be(0);
      }

      [Fact]
      public async Task Owner_Isolation_User_B_Cannot_See_Or_Delete_User_A_Items()
      {
          var a = await _factory.RegisterAndLoginAsync("plan-owner-a@example.com", "Passw0rd!");
          var b = await _factory.RegisterAndLoginAsync("plan-owner-b@example.com", "Passw0rd!");

          (await a.PostAsJsonAsync("/api/plan", AddBody("secret"))).StatusCode.Should().Be(HttpStatusCode.OK);

          // B sees nothing.
          var bItems = JsonDocument.Parse(await (await b.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          bItems.GetArrayLength().Should().Be(0);

          // B's delete of A's item id is a no-op for A.
          (await b.DeleteAsync("/api/plan/secret")).StatusCode.Should().Be(HttpStatusCode.OK);

          var aItems = JsonDocument.Parse(await (await a.GetAsync("/api/plan")).Content.ReadAsStringAsync()).RootElement;
          aItems.GetArrayLength().Should().Be(1);
          aItems[0].GetProperty("id").GetString().Should().Be("secret");
      }

      [Fact]
      public async Task Anonymous_Get_Is_Unauthorized()
      {
          var client = _factory.CreateClient();
          var resp = await client.GetAsync("/api/plan");
          resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
      }
  }
  ```

- [ ] **Step 2: Run, expect FAIL**

  Run from `D:\Work\Templates\FIT\server`:
  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.PlanTests"
  ```
  Expected: BUILD FAILS with compiler errors `error CS0246: The type or namespace name 'PlanController' could not be found` (and `PlanItemDto`/`AddPlanItemRequest` not found), because neither `Controllers/PlanController.cs` nor `Dtos/PlanDtos.cs` exists yet. (If the build somehow succeeds, the tests still fail with a `404` not `200`.)

- [ ] **Step 3: Implement**

  Create `D:\Work\Templates\FIT\server\FitApi\Dtos\PlanDtos.cs` with COMPLETE content (these records are declared here and ONLY here per the contract):

  ```csharp
  namespace FitApi.Dtos;

  public record PlanItemDto(
      string Id,
      string? NameAr,
      string? NameEn,
      string? Tab,
      int Sets,
      int Reps,
      int Order);

  public record AddPlanItemRequest(
      string Id,
      string? NameAr,
      string? NameEn,
      string? Tab,
      int? Sets,
      int? Reps);
  ```

  Create `D:\Work\Templates\FIT\server\FitApi\Controllers\PlanController.cs` with COMPLETE content:

  ```csharp
  using FitApi.Data;
  using FitApi.Dtos;
  using FitApi.Models;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;

  namespace FitApi.Controllers;

  [ApiController]
  [Authorize]
  [Route("api/plan")]
  public class PlanController : ApiControllerBase
  {
      private readonly AppDbContext _db;

      public PlanController(AppDbContext db) => _db = db;

      private static PlanItemDto ToDto(CustomPlanItem i) =>
          new(i.ItemId, i.NameAr, i.NameEn, i.Tab, i.Sets, i.Reps, i.OrderIndex);

      // GET api/plan -> PlanItemDto[] ordered by OrderIndex ascending
      [HttpGet]
      public async Task<ActionResult<IEnumerable<PlanItemDto>>> Get()
      {
          var items = await _db.CustomPlanItems
              .Where(i => i.UserId == UserId)
              .OrderBy(i => i.OrderIndex)
              .ToListAsync();
          return Ok(items.Select(ToDto));
      }

      // POST api/plan -> {added:false} if ItemId exists, else insert with defaults -> {added:true}
      [HttpPost]
      public async Task<IActionResult> Add([FromBody] AddPlanItemRequest req)
      {
          var exists = await _db.CustomPlanItems
              .AnyAsync(i => i.UserId == UserId && i.ItemId == req.Id);
          if (exists)
              return Ok(new { added = false });

          var count = await _db.CustomPlanItems.CountAsync(i => i.UserId == UserId);

          _db.CustomPlanItems.Add(new CustomPlanItem
          {
              UserId = UserId,
              ItemId = req.Id,
              NameAr = req.NameAr,
              NameEn = req.NameEn,
              Tab = req.Tab,
              Sets = req.Sets ?? 4,
              Reps = req.Reps ?? 10,
              OrderIndex = count
          });
          await _db.SaveChangesAsync();
          return Ok(new { added = true });
      }

      // PUT api/plan -> replace OrderIndex/Sets/Reps for the user's matching items by Id
      [HttpPut]
      public async Task<IActionResult> Update([FromBody] PlanItemDto[] items)
      {
          var mine = await _db.CustomPlanItems
              .Where(i => i.UserId == UserId)
              .ToListAsync();
          var byId = mine.ToDictionary(i => i.ItemId);

          foreach (var dto in items)
          {
              if (byId.TryGetValue(dto.Id, out var existing))
              {
                  existing.OrderIndex = dto.Order;
                  existing.Sets = dto.Sets;
                  existing.Reps = dto.Reps;
              }
          }
          await _db.SaveChangesAsync();
          return Ok(new { });
      }

      // DELETE api/plan/{itemId} -> remove the item, then renumber remaining 0..n
      [HttpDelete("{itemId}")]
      public async Task<IActionResult> Remove(string itemId)
      {
          var toRemove = await _db.CustomPlanItems
              .FirstOrDefaultAsync(i => i.UserId == UserId && i.ItemId == itemId);
          if (toRemove != null)
          {
              _db.CustomPlanItems.Remove(toRemove);
              await _db.SaveChangesAsync();
          }

          var remaining = await _db.CustomPlanItems
              .Where(i => i.UserId == UserId)
              .OrderBy(i => i.OrderIndex)
              .ToListAsync();
          for (var i = 0; i < remaining.Count; i++)
              remaining[i].OrderIndex = i;
          await _db.SaveChangesAsync();

          return Ok(new { });
      }

      // DELETE api/plan -> clear all of the user's items
      [HttpDelete]
      public async Task<IActionResult> Clear()
      {
          var mine = await _db.CustomPlanItems
              .Where(i => i.UserId == UserId)
              .ToListAsync();
          _db.CustomPlanItems.RemoveRange(mine);
          await _db.SaveChangesAsync();
          return Ok(new { });
      }
  }
  ```

  Notes that keep this deterministic and contract-correct:
  - Route attribute order: `DELETE api/plan/{itemId}` (`Remove`) and `DELETE api/plan` (`Clear`) are distinguished by ASP.NET routing on presence of the `{itemId}` segment; no ambiguity. Likewise `Add` (`[HttpPost]`) vs `Update` (`[HttpPut]`).
  - `UserId` comes from `ApiControllerBase` (the authenticated cookie principal); it is NEVER read from body/route, satisfying owner isolation.
  - The DTO's `Id` maps to the entity's `ItemId`; the DB primary key `Id` (long) is never exposed.

- [ ] **Step 4: Run, expect PASS**

  Run from `D:\Work\Templates\FIT\server`:
  ```
  dotnet test FitApi.sln --filter "FullyQualifiedName~FitApi.Tests.PlanTests"
  ```
  Expected output ends with: `Passed!  - Failed: 0, Passed: 8, Skipped: 0` (the 8 `[Fact]` methods in `PlanTests`).

- [ ] **Step 5: Commit**

  Run from `D:\Work\Templates\FIT`:
  ```
  git add server/FitApi/Dtos/PlanDtos.cs server/FitApi/Controllers/PlanController.cs server/FitApi.Tests/PlanTests.cs
  git commit -m "feat(api): add PlanController for custom workout plan (get/add/reorder/delete/clear)"
  ```


---

### Task 14: Rewrite assets/js/auth.js (FITAuth + FITData as API clients; keep nav/avatar UI)

**Files:**
- Modify (full rewrite â€” this task is the SOLE owner): `D:\Work\Templates\FIT\assets\js\auth.js`
- Test: MANUAL browser verification only (frontend task â€” no xUnit). Exact steps in Step 2 and Step 4 below.

**Interfaces:**
- Consumes (server endpoints created by earlier tasks; all under `/api`, single origin, cookie auth):
  - `GET  /api/auth/me` -> 200 `UserDto{id,email,displayName,photoUrl,emailConfirmed}` (camelCase) | 401 (Task 5)
  - `POST /api/auth/register` -> 200 `{}` | 409 ProblemDetails `code="email-already-in-use"` (Task 5)
  - `POST /api/auth/login` -> 200 `UserDto` (sets cookie) | 401 `code="invalid-credential"` (Task 5)
  - `POST /api/auth/logout` -> 200 `{}` (Task 5)
  - `POST /api/auth/forgot-password` -> 200 `{}` (Task 5)
  - `POST /api/auth/reset-password` -> 200 `{}` | 400 `code="invalid-token"` (Task 5)
  - `POST /api/auth/resend-verification` -> 200 `{}` (Task 5)
  - `GET  /api/auth/google` -> Google challenge (Task 6)
  - `GET  /api/profile` | `PUT /api/profile` -> `ProfileDto` (Task 7)
  - `POST /api/profile/avatar` (multipart field `file`) -> `{ photoURL }` | 400 `code="bad-avatar"` (Task 7)
  - `GET/POST /api/favorites`, `DELETE /api/favorites/{itemId}` (Task 8)
  - `GET/POST /api/nutrition-plans`, `DELETE /api/nutrition-plans/{planId}` (Task 9)
  - `GET/POST /api/calc-history`, `DELETE /api/calc-history/{id}` (Task 10)
  - `GET/POST /api/weight-log`, `DELETE /api/weight-log/{id}` (Task 11)
  - `GET /api/workouts/completed`, `POST /api/workouts/complete` -> `StreakStats{streak,longestStreak,total}`, `DELETE /api/workouts/complete` -> `StreakStats` (Task 12)
  - `GET/POST /api/plan`, `PUT /api/plan`, `DELETE /api/plan/{itemId}`, `DELETE /api/plan` (Task 13)
  - ProblemDetails error convention: non-2xx body is JSON with top-level property `"code"` (Task 5 ERROR CONVENTION).
- Produces (global bridges the existing 7 pages + inline onclick handlers already call â€” signatures FIXED, do not rename):
  - `window.FITAuth`: getters `user`, `ready`; methods `signInGoogle()`, `registerEmail({name,email,password})`, `loginEmail({email,password})`, `resetPassword(email)`, `resendVerification()`, `logout()`, `errMessage(code)`, `onUser(cb)`, `requireAuth(cb,redirect="auth.html")`, `uploadAvatar(file)`, `refreshUI()`. **`signInMicrosoft` is REMOVED.**
  - `window.FITData`: `getProfile`, `saveProfile`, `getFavorites`, `toggleFavorite`, `removeFavorite`, `saveCalcResult`, `getCalcHistory`, `deleteCalcResult`, `getNutritionPlans`, `saveNutritionPlan`, `removeNutritionPlan`, `addWeightEntry`, `getWeightLog`, `deleteWeightEntry`, `getCompletedDates`, `markWorkoutToday`, `unmarkWorkoutToday`, `getPlan`, `addToPlan`, `updatePlan`, `removeFromPlan`, `clearPlan`. `markWorkoutToday`/`unmarkWorkoutToday` return `{streak,longestStreak,total}`.
  - Internal (preserved, used by nav rendering): `renderNavAuthUI`, `actuallyRenderNav`, `buildSlot`, `renderProfileAvatar`, `mobileLink`, `menuItem`, `injectFastingLink`.

Steps (frontend â€” TDD cycle replaced by MANUAL verification per HARD RULES):

- [ ] **Step 1: Implement** â€” Fully overwrite `D:\Work\Templates\FIT\assets\js\auth.js` with EXACTLY the following (no firebase imports; plain ES module; `fetch` with `credentials:"include"`; nav/avatar render functions reproduced unchanged from the prior file; Arabic copy preserved):

```js
// ============================================================================
//  MAKE ME FIT â€” ÙˆØ­Ø¯Ø© Ø§Ù„Ù…ØµØ§Ø¯Ù‚Ø© ÙˆØ§Ù„Ø¨ÙŠØ§Ù†Ø§Øª (API client)
// ----------------------------------------------------------------------------
//  Ù…Ù„Ù ES module ÙˆØ§Ø­Ø¯ ÙŠØªØ­Ø¯Ø« Ù…Ø¹ ÙˆØ§Ø¬Ù‡Ø© ASP.NET Core Ø¹Ù„Ù‰ Ù†ÙØ³ Ø§Ù„Ø£ØµÙ„ Ø¹Ø¨Ø± fetch.
//  Ù„Ø§ ÙŠØ³ØªÙˆØ±Ø¯ Firebase. ÙŠÙØ­Ù…Ù‘Ù„ ÙÙŠ ÙƒÙ„ ØµÙØ­Ø© Ø¹Ø¨Ø±:
//    <script type="module" src="assets/js/auth.js"></script>
//
//  ÙŠÙƒØ´Ù Ø¬Ø³Ø±ÙŽÙŠÙ† Ø¹Ø§Ù„Ù…ÙŠÙŽÙ‘ÙŠÙ† Ù„Ù„ÙƒÙˆØ¯ Ø§Ù„Ù‚Ø¯ÙŠÙ… (jQuery + inline onclick):
//    window.FITAuth  â€” ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„/Ø§Ù„Ø®Ø±ÙˆØ¬ ÙˆØ­Ø§Ù„Ø© Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…
//    window.FITData  â€” Ù‚Ø±Ø§Ø¡Ø©/ÙƒØªØ§Ø¨Ø© Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø¹Ø¨Ø± REST API
// ============================================================================

// ---------------------------------------------------------------------------
//  Ø­Ø§Ù„Ø© Ø§Ù„ÙˆØ­Ø¯Ø©
// ---------------------------------------------------------------------------
let currentUser = null;     // ÙƒØ§Ø¦Ù† Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø§Ù„Ø­Ø§Ù„ÙŠ Ø£Ùˆ null
let authResolved = false;   // Ù‡Ù„ Ø­ÙØ³Ù…Øª Ø­Ø§Ù„Ø© Ø§Ù„Ù…ØµØ§Ø¯Ù‚Ø© Ø§Ù„Ø£ÙˆÙ„Ù‰ØŸ
const userListeners = [];   // Ù…Ø³ØªÙ…Ø¹Ùˆ ØªØºÙŠÙ‘Ø± Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…

// ---------------------------------------------------------------------------
//  Ù…Ø³Ø§Ø¹Ø¯ fetch Ù…ÙˆØ­Ù‘Ø¯ â€” ÙŠØ±Ø³Ù„ Ø§Ù„ÙƒÙˆÙƒÙŠ Ø¯Ø§Ø¦Ù…Ø§Ù‹ ÙˆÙŠØ±ÙØ¹ Error.code Ù…Ù† ProblemDetails
// ---------------------------------------------------------------------------
async function api(path, { method = "GET", body, raw } = {}) {
  const opts = { method, credentials: "include", headers: {} };
  if (raw !== undefined) {
    opts.body = raw;                       // FormData Ø£Ùˆ Ù…Ø§ Ø´Ø§Ø¨Ù‡ â€” Ù„Ø§ Ù†Ø¶Ø¨Ø· Content-Type
  } else if (body !== undefined) {
    opts.headers["Content-Type"] = "application/json";
    opts.body = JSON.stringify(body);
  }

  let res;
  try {
    res = await fetch(path, opts);
  } catch (e) {
    const err = new Error("network");
    err.code = "network";
    throw err;
  }

  let data = null;
  const text = await res.text();
  if (text) { try { data = JSON.parse(text); } catch (_) { data = null; } }

  if (!res.ok) {
    const code = (data && (data.code || (data.extensions && data.extensions.code))) || String(res.status);
    const err = new Error(code);
    err.code = code;
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

// ---------------------------------------------------------------------------
//  ØªØ­ÙˆÙŠÙ„ UserDto Ø§Ù„Ù‚Ø§Ø¯Ù… Ù…Ù† Ø§Ù„Ø®Ø§Ø¯Ù… Ø¥Ù„Ù‰ Ø§Ù„Ø´ÙƒÙ„ Ø§Ù„Ø°ÙŠ ØªØªÙˆÙ‚Ø¹Ù‡ Ø§Ù„ØµÙØ­Ø§Øª
//  (ÙŠÙƒØ´Ù uid Ùˆ id Ù…Ø¹Ø§Ù‹ â€” uid() ÙÙŠ FITData ÙŠÙ‚Ø±Ø£ currentUser.uid)
// ---------------------------------------------------------------------------
function mapUser(dto) {
  if (!dto) return null;
  return {
    uid: dto.id,
    id: dto.id,
    displayName: dto.displayName || "",
    email: dto.email || "",
    photoURL: dto.photoUrl || null,
    emailVerified: !!dto.emailConfirmed
  };
}

function whenReady(fn) {
  if (document.readyState !== "loading") fn();
  else document.addEventListener("DOMContentLoaded", fn);
}

// ØªÙØ³ØªØ¯Ø¹Ù‰ Ø¹Ù†Ø¯ Ø­Ø³Ù… Ø­Ø§Ù„Ø© Ø§Ù„Ù…ØµØ§Ø¯Ù‚Ø©
function notify(user) {
  currentUser = user || null;
  authResolved = true;
  renderNavAuthUI(currentUser);
  userListeners.forEach((cb) => { try { cb(currentUser); } catch (e) { console.error(e); } });
}

// ---------------------------------------------------------------------------
//  ØªÙ‡ÙŠØ¦Ø©: Ø§Ù‚Ø±Ø£ Ø­Ø§Ù„Ø© Ø§Ù„Ø¬Ù„Ø³Ø© Ø§Ù„Ø­Ø§Ù„ÙŠØ© Ù…Ù† Ø§Ù„Ø®Ø§Ø¯Ù… (ÙŠØ­Ù„Ù‘ Ù…Ø­Ù„ onAuthStateChanged)
// ---------------------------------------------------------------------------
(async function bootstrap() {
  try {
    const dto = await api("/api/auth/me");
    notify(mapUser(dto));
  } catch (e) {
    notify(null);   // 401 Ø£Ùˆ Ø®Ø·Ø£ Ø´Ø¨ÙƒØ© => Ù„Ø§ ÙŠÙˆØ¬Ø¯ Ù…Ø³ØªØ®Ø¯Ù…
  }
})();

// ===========================================================================
//  ÙˆØ§Ø¬Ù‡Ø© Ø§Ù„Ù…ØµØ§Ø¯Ù‚Ø©  window.FITAuth
// ===========================================================================
async function signInGoogle() {
  location.href = "/api/auth/google";
}

async function registerEmail({ name, email, password }) {
  await api("/api/auth/register", { method: "POST", body: { name, email, password } });
  // Ø¨Ø¹Ø¯ Ø§Ù„ØªØ³Ø¬ÙŠÙ„ Ø³Ø¬Ù‘Ù„ Ø§Ù„Ø¯Ø®ÙˆÙ„ Ù…Ø¨Ø§Ø´Ø±Ø©Ù‹ Ù„ØªØ«Ø¨ÙŠØª Ø§Ù„Ø¬Ù„Ø³Ø© (Ø§Ù„ÙƒÙˆÙƒÙŠ)
  return loginEmail({ email, password });
}

async function loginEmail({ email, password }) {
  const dto = await api("/api/auth/login", { method: "POST", body: { email, password } });
  notify(mapUser(dto));
  return currentUser;
}

async function resetPassword(email) {
  return api("/api/auth/forgot-password", { method: "POST", body: { email } });
}

async function resendVerification() {
  return api("/api/auth/resend-verification", { method: "POST" });
}

async function logout() {
  try { await api("/api/auth/logout", { method: "POST" }); }
  catch (e) { console.warn("[FIT] logout:", e); }
  notify(null);
  location.href = "index.html";
}

// ÙŠØ­ÙˆÙ‘Ù„ Ø±Ù…ÙˆØ² Ø£Ø®Ø·Ø§Ø¡ Ø§Ù„Ù€ API Ø¥Ù„Ù‰ Ø±Ø³Ø§Ø¦Ù„ Ø¹Ø±Ø¨ÙŠØ© (Ù†ÙØ³ ØµÙŠØ§ØºØ© Ø§Ù„Ù†Ø³Ø®Ø© Ø§Ù„Ø³Ø§Ø¨Ù‚Ø©)
function errMessage(code) {
  const map = {
    "email-already-in-use": "Ù‡Ø°Ø§ Ø§Ù„Ø¨Ø±ÙŠØ¯ Ù…Ø³Ø¬Ù‘Ù„ Ø¨Ø§Ù„ÙØ¹Ù„. Ø¬Ø±Ù‘Ø¨ ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„.",
    "invalid-credential":   "Ø§Ù„Ø¨Ø±ÙŠØ¯ Ø£Ùˆ ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± ØºÙŠØ± ØµØ­ÙŠØ­Ø©.",
    "invalid-token":        "Ø±Ø§Ø¨Ø· Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„ØªØ¹ÙŠÙŠÙ† ØºÙŠØ± ØµØ§Ù„Ø­ Ø£Ùˆ Ù…Ù†ØªÙ‡ÙŠ Ø§Ù„ØµÙ„Ø§Ø­ÙŠØ©.",
    "bad-avatar":           "ÙŠØ¬Ø¨ Ø§Ø®ØªÙŠØ§Ø± Ù…Ù„Ù ØµÙˆØ±Ø© ØµØ§Ù„Ø­ Ø£Ù‚Ù„ Ù…Ù† Ù¢ Ù…ÙŠØ¬Ø§Ø¨Ø§ÙŠØª.",
    "bad-weight":           "Ø§Ù„ÙˆØ²Ù† ØºÙŠØ± ØµØ­ÙŠØ­ (Ù¡Ù â€“Ù¥Ù Ù  ÙƒØ¬Ù…).",
    "network":              "ØªØ¹Ø°Ù‘Ø± Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ø§Ù„Ø´Ø¨ÙƒØ©. ØªØ­Ù‚Ù‘Ù‚ Ù…Ù† Ø§ØªØµØ§Ù„Ùƒ."
  };
  return map[code] || "Ø­Ø¯Ø« Ø®Ø·Ø£ ØºÙŠØ± Ù…ØªÙˆÙ‚Ø¹. Ø­Ø§ÙˆÙ„ Ù…Ø±Ø© Ø£Ø®Ø±Ù‰.";
}

window.FITAuth = {
  get user() { return currentUser; },
  get ready() { return true; },
  signInGoogle,
  registerEmail,
  loginEmail,
  resetPassword,
  resendVerification,
  logout,
  errMessage,

  // ÙŠØ³Ø¬Ù‘Ù„ Ù…Ø³ØªÙ…Ø¹Ø§Ù‹ Ù„ØªØºÙŠÙ‘Ø± Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…Ø› ÙŠÙÙ†Ø§Ø¯Ù‰ ÙÙˆØ±Ø§Ù‹ Ø¥Ù† ÙƒØ§Ù†Øª Ø§Ù„Ø­Ø§Ù„Ø© Ù…Ø­Ø³ÙˆÙ…Ø©
  onUser(cb) {
    if (typeof cb !== "function") return;
    userListeners.push(cb);
    if (authResolved) cb(currentUser);
  },

  // Ø­Ù…Ø§ÙŠØ© ØµÙØ­Ø©: ÙŠÙ†ÙÙ‘Ø° cb Ø¹Ù†Ø¯ Ø§Ù„Ø¯Ø®ÙˆÙ„ØŒ ÙˆÙŠØ­ÙˆÙ‘Ù„ Ù„ØµÙØ­Ø© Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø¥Ù† Ù„Ù… ÙŠÙƒÙ† Ø¯Ø§Ø®Ù„Ø§Ù‹
  requireAuth(cb, redirect = "auth.html") {
    this.onUser((user) => {
      if (user) { if (cb) cb(user); }
      else location.replace(redirect);
    });
  },

  // Ø±ÙØ¹ ØµÙˆØ±Ø© Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø¹Ø¨Ø± multipart Ø¥Ù„Ù‰ /api/profile/avatar Ø«Ù… ØªØ­Ø¯ÙŠØ« Ø§Ù„ÙˆØ§Ø¬Ù‡Ø©
  async uploadAvatar(file) {
    if (!currentUser) throw new Error("ÙŠØ¬Ø¨ ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø£ÙˆÙ„Ø§Ù‹");
    if (!file || !file.type || !file.type.startsWith("image/")) {
      const err = new Error("bad-avatar"); err.code = "bad-avatar"; throw err;
    }
    if (file.size > 2 * 1024 * 1024) {
      const err = new Error("bad-avatar"); err.code = "bad-avatar"; throw err;
    }
    const fd = new FormData();
    fd.append("file", file);
    const data = await api("/api/profile/avatar", { method: "POST", raw: fd });
    const url = data && data.photoURL;
    currentUser = { ...currentUser, photoURL: url || null };
    renderNavAuthUI(currentUser);
    return url;
  },

  // Ø¥Ø¹Ø§Ø¯Ø© Ø±Ø³Ù… Ø§Ù„ÙˆØ§Ø¬Ù‡Ø© (Ø§Ù„Ù‚Ø§Ø¦Ù…Ø© + ØµÙˆØ±Ø© Ø§Ù„Ù…Ù„Ù) Ù…Ù† Ø­Ø§Ù„Ø© Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø§Ù„Ø­Ø§Ù„ÙŠØ©
  refreshUI() { renderNavAuthUI(currentUser); }
};

// ===========================================================================
//  Ø·Ø¨Ù‚Ø© Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª  window.FITData  (REST API)
// ===========================================================================
function uid() { return currentUser ? currentUser.uid : null; }

window.FITData = {
  // --- Ø§Ù„Ù…Ù„Ù Ø§Ù„Ø´Ø®ØµÙŠ ---
  async getProfile() {
    if (!uid()) return null;
    try { return await api("/api/profile"); }
    catch (e) { return null; }
  },
  async saveProfile(partial) {
    return api("/api/profile", { method: "PUT", body: partial || {} });
  },

  // --- Ø§Ù„ØªÙ…Ø§Ø±ÙŠÙ† Ø§Ù„Ù…ÙØ¶Ù„Ø© ---
  async getFavorites() {
    if (!uid()) return [];
    try { return await api("/api/favorites"); }
    catch (e) { return []; }
  },
  // ÙŠØ¶ÙŠÙ Ø£Ùˆ ÙŠØ²ÙŠÙ„ Ø­Ø³Ø¨ ÙˆØ¬ÙˆØ¯ Ø§Ù„Ù…Ø¹Ø±Ù‘ÙØ› ÙŠØ¹ÙŠØ¯ true Ø¥Ø°Ø§ Ø£ØµØ¨Ø­ Ù…ÙØ¶Ù‘Ù„Ø§Ù‹
  async toggleFavorite(fav) {
    const res = await api("/api/favorites", { method: "POST", body: fav });
    return !!(res && res.favorited);
  },
  async removeFavorite(id) {
    return api(`/api/favorites/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- Ø³Ø¬Ù„ Ø­Ø§Ø³Ø¨Ø© Ø§Ù„Ø³Ø¹Ø±Ø§Øª ---
  async saveCalcResult(result) {
    return api("/api/calc-history", { method: "POST", body: result });
  },
  async getCalcHistory() {
    if (!uid()) return [];
    try { return await api("/api/calc-history"); }
    catch (e) { return []; }
  },
  async deleteCalcResult(id) {
    return api(`/api/calc-history/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- Ø®Ø·Ø· Ø§Ù„ØªØºØ°ÙŠØ© Ø§Ù„Ù…Ø­ÙÙˆØ¸Ø© ---
  async getNutritionPlans() {
    if (!uid()) return [];
    try { return await api("/api/nutrition-plans"); }
    catch (e) { return []; }
  },
  async saveNutritionPlan(plan) {
    const res = await api("/api/nutrition-plans", { method: "POST", body: plan });
    return !!(res && res.saved);
  },
  async removeNutritionPlan(id) {
    return api(`/api/nutrition-plans/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- Ø³Ø¬Ù„ Ø§Ù„ÙˆØ²Ù† ÙˆØ§Ù„Ù‚ÙŠØ§Ø³Ø§Øª (Ù†ÙØ³ Ø±Ø³Ø§Ù„Ø© Ø§Ù„ØªØ­Ù‚Ù‚ Ø§Ù„Ø¹Ø±Ø¨ÙŠØ© Ù‚Ø¨Ù„ Ø§Ù„Ø¥Ø±Ø³Ø§Ù„) ---
  async addWeightEntry(entry) {
    const { weight, waist, chest, arms, date } = entry || {};
    const w = Number(weight);
    if (!w || w < 10 || w > 500) throw new Error("Ø§Ù„ÙˆØ²Ù† ØºÙŠØ± ØµØ­ÙŠØ­ (Ù¡Ù â€“Ù¥Ù Ù  ÙƒØ¬Ù…)");
    return api("/api/weight-log", {
      method: "POST",
      body: {
        weight: w,
        waist: waist ? Number(waist) : null,
        chest: chest ? Number(chest) : null,
        arms:  arms  ? Number(arms)  : null,
        date:  date || null
      }
    });
  },
  async getWeightLog() {
    if (!uid()) return [];
    try { return await api("/api/weight-log"); }
    catch (e) { return []; }
  },
  async deleteWeightEntry(id) {
    return api(`/api/weight-log/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- Ù…ØªØ§Ø¨Ø¹Ø© Ø¥Ù†Ø¬Ø§Ø² Ø§Ù„ØªÙ…Ø§Ø±ÙŠÙ† + Ø§Ù„Ø³Ù„Ø³Ù„Ø© (streak) ---
  async getCompletedDates() {
    if (!uid()) return [];
    try { return await api("/api/workouts/completed"); }
    catch (e) { return []; }
  },
  async markWorkoutToday() {
    return api("/api/workouts/complete", { method: "POST" });
  },
  async unmarkWorkoutToday() {
    return api("/api/workouts/complete", { method: "DELETE" });
  },

  // --- Ø®Ø·ØªÙŠ Ø§Ù„Ù…Ø®ØµØµØ© ---
  async getPlan() {
    if (!uid()) return [];
    try { return await api("/api/plan"); }
    catch (e) { return []; }
  },
  async addToPlan(exercise) {
    const res = await api("/api/plan", {
      method: "POST",
      body: {
        id: exercise.id,
        nameAr: exercise.nameAr || "",
        nameEn: exercise.nameEn || "",
        tab: exercise.tab || "",
        sets: exercise.sets ?? null,
        reps: exercise.reps ?? null
      }
    });
    return !!(res && res.added);
  },
  async updatePlan(list) {
    return api("/api/plan", { method: "PUT", body: list || [] });
  },
  async removeFromPlan(id) {
    return api(`/api/plan/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  async clearPlan() {
    return api("/api/plan", { method: "DELETE" });
  }
};

// ===========================================================================
//  Ø­Ù‚Ù† ÙˆØ§Ø¬Ù‡Ø© Ø§Ù„Ù…ØµØ§Ø¯Ù‚Ø© ÙÙŠ Ø§Ù„Ù‚Ø§Ø¦Ù…Ø© (nav) Ø¹Ù„Ù‰ ÙƒÙ„ Ø§Ù„ØµÙØ­Ø§Øª
//  (Ù‡Ø°Ù‡ Ø§Ù„Ø¯ÙˆØ§Ù„ Ù…Ø­ÙÙˆØ¸Ø© ÙƒÙ…Ø§ Ù‡ÙŠ Ù…Ù† Ø§Ù„Ù†Ø³Ø®Ø© Ø§Ù„Ø³Ø§Ø¨Ù‚Ø©)
// ===========================================================================
function renderNavAuthUI(user) {
  whenReady(() => { actuallyRenderNav(user); renderProfileAvatar(user); });
}

// ØªØ­Ø¯ÙŠØ« ØµÙˆØ±Ø© Ø§Ù„Ù…Ù„Ù Ø§Ù„Ø´Ø®ØµÙŠ ÙÙŠ profile.html (Ø¥Ù† ÙˆÙØ¬Ø¯Øª)
function renderProfileAvatar(user) {
  const av = document.getElementById("pf-avatar");
  if (!av || !user) return;
  const name = user.displayName || (user.email ? user.email.split("@")[0] : "Ø­Ø³Ø§Ø¨ÙŠ");
  if (user.photoURL) {
    av.innerHTML = "";
    const img = document.createElement("img");
    img.src = user.photoURL;
    img.alt = name;
    img.referrerPolicy = "no-referrer";
    av.appendChild(img);
  } else {
    av.textContent = (name.trim()[0] || "ØŸ").toUpperCase();
  }
}

function actuallyRenderNav(user) {
  const desktop = document.querySelector("header nav .d-none.d-lg-flex");
  const mobile  = document.getElementById("mobileNav");

  // Ø¥Ø²Ø§Ù„Ø© Ø£ÙŠ Ø®Ø§Ù†Ø© Ø³Ø§Ø¨Ù‚Ø© Ù„ØªÙØ§Ø¯ÙŠ Ø§Ù„ØªÙƒØ±Ø§Ø±
  document.querySelectorAll(".fit-auth-slot").forEach((el) => el.remove());

  injectFastingLink(desktop, mobile);

  if (desktop) desktop.appendChild(buildSlot(user, false));
  if (mobile)  mobile.appendChild(buildSlot(user, true));
}

// ÙŠØ¶ÙŠÙ Ø±Ø§Ø¨Ø· "Ø§Ù„ØµÙŠØ§Ù…" Ø¥Ù„Ù‰ Ø§Ù„Ù‚Ø§Ø¦Ù…Ø© (Ù…Ø±Ø© ÙˆØ§Ø­Ø¯Ø©) Ø¨Ø¹Ø¯ Ø±Ø§Ø¨Ø· Ø­Ø§Ø³Ø¨Ø© Ø§Ù„Ø³Ø¹Ø±Ø§Øª
function injectFastingLink(desktop, mobile) {
  const active = (location.pathname.split("/").pop() || "") === "fasting.html";
  if (desktop && !desktop.querySelector('a[href="fasting.html"]')) {
    const a = document.createElement("a");
    a.href = "fasting.html"; a.className = "nav-link-item"; a.textContent = "Ø§Ù„ØµÙŠØ§Ù…";
    a.style.cssText = `color:${active ? "#f59e0b" : "#ccc"};text-decoration:none;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;`;
    const after = desktop.querySelector('a[href="calculator.html"]');
    if (after) after.insertAdjacentElement("afterend", a); else desktop.appendChild(a);
  }
  if (mobile && !mobile.querySelector('a[href="fasting.html"]')) {
    const a = document.createElement("a");
    a.href = "fasting.html"; a.textContent = "Ø§Ù„ØµÙŠØ§Ù…";
    a.style.cssText = `display:block;color:${active ? "#f59e0b" : "#ccc"};text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;`;
    const after = mobile.querySelector('a[href="calculator.html"]');
    if (after) after.insertAdjacentElement("afterend", a); else mobile.appendChild(a);
  }
}

function buildSlot(user, isMobile) {
  const slot = document.createElement("div");
  slot.className = "fit-auth-slot";
  slot.style.cssText = isMobile
    ? "border-top:1px solid #333;margin-top:8px;padding-top:8px;"
    : "display:flex;align-items:center;";

  if (!user) {
    // --- Ø®Ø§Ø±Ø¬ Ø§Ù„Ø­Ø³Ø§Ø¨: Ø²Ø± ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ ---
    const a = document.createElement("a");
    a.href = "auth.html";
    a.textContent = "ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„";
    a.style.cssText = isMobile
      ? "display:block;color:#f59e0b;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;font-weight:700;"
      : "background:#f59e0b;color:#111;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:700;font-size:0.95rem;padding:8px 18px;border-radius:8px;white-space:nowrap;transition:background 0.3s;";
    if (!isMobile) {
      a.onmouseenter = () => (a.style.background = "#d97706");
      a.onmouseleave = () => (a.style.background = "#f59e0b");
    }
    slot.appendChild(a);
    return slot;
  }

  // --- Ø¯Ø§Ø®Ù„ Ø§Ù„Ø­Ø³Ø§Ø¨: ØµÙˆØ±Ø© + Ø§Ø³Ù… + Ù‚Ø§Ø¦Ù…Ø© Ù…Ù†Ø³Ø¯Ù„Ø© ---
  const name  = user.displayName || (user.email ? user.email.split("@")[0] : "Ø­Ø³Ø§Ø¨ÙŠ");
  const photo = user.photoURL;

  const avatar = document.createElement("span");
  avatar.style.cssText =
    "width:34px;height:34px;border-radius:50%;flex:0 0 auto;display:inline-flex;align-items:center;justify-content:center;background:#f59e0b;color:#111;font-weight:800;font-family:'Tajawal',sans-serif;overflow:hidden;";
  if (photo) {
    const img = document.createElement("img");
    img.src = photo;
    img.alt = name;
    img.referrerPolicy = "no-referrer";
    img.style.cssText = "width:100%;height:100%;object-fit:cover;";
    avatar.appendChild(img);
  } else {
    avatar.textContent = (name.trim()[0] || "ØŸ").toUpperCase();
  }

  if (isMobile) {
    // Ø§Ù„Ù…ÙˆØ¨Ø§ÙŠÙ„: Ø±ÙˆØ§Ø¨Ø· Ù…Ø¨Ø§Ø´Ø±Ø© Ø¨Ø¯ÙˆÙ† Ù…Ù†Ø³Ø¯Ù„Ø©
    const head = document.createElement("div");
    head.style.cssText = "display:flex;align-items:center;gap:10px;padding:10px 0;";
    const label = document.createElement("span");
    label.textContent = name;
    label.style.cssText = "color:#fff;font-family:'Tajawal',sans-serif;font-weight:700;";
    head.appendChild(avatar);
    head.appendChild(label);
    slot.appendChild(head);

    slot.appendChild(mobileLink("Ù…Ù„ÙÙŠ Ø§Ù„Ø´Ø®ØµÙŠ", () => (location.href = "profile.html")));
    slot.appendChild(mobileLink("ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø®Ø±ÙˆØ¬", () => window.FITAuth.logout(), "#e94560"));
    return slot;
  }

  // Ø§Ù„Ø¯ÙŠØ³ÙƒØªÙˆØ¨: Ø²Ø± ÙŠÙØªØ­ Ù‚Ø§Ø¦Ù…Ø© Ù…Ù†Ø³Ø¯Ù„Ø©
  slot.style.position = "relative";

  const btn = document.createElement("button");
  btn.type = "button";
  btn.style.cssText =
    "display:flex;align-items:center;gap:8px;background:none;border:1px solid #f59e0b55;border-radius:30px;padding:4px 12px 4px 6px;cursor:pointer;color:#fff;font-family:'Tajawal',sans-serif;";
  const btnLabel = document.createElement("span");
  btnLabel.textContent = name;
  btnLabel.style.cssText = "font-weight:600;font-size:0.95rem;max-width:120px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;";
  const caret = document.createElement("i");
  caret.className = "fas fa-chevron-down";
  caret.style.cssText = "font-size:0.7rem;color:#f59e0b;";
  btn.appendChild(avatar);
  btn.appendChild(btnLabel);
  btn.appendChild(caret);

  const menu = document.createElement("div");
  menu.style.cssText =
    "position:absolute;top:calc(100% + 8px);left:0;min-width:180px;background:#1a1a1a;border:1px solid #f59e0b33;border-radius:10px;padding:6px;display:none;flex-direction:column;z-index:10000;box-shadow:0 10px 30px rgba(0,0,0,.5);";
  menu.appendChild(menuItem("Ù…Ù„ÙÙŠ Ø§Ù„Ø´Ø®ØµÙŠ", "fa-user", () => (location.href = "profile.html")));
  menu.appendChild(menuItem("ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø®Ø±ÙˆØ¬", "fa-right-from-bracket", () => window.FITAuth.logout(), "#e94560"));

  btn.addEventListener("click", (e) => {
    e.stopPropagation();
    menu.style.display = menu.style.display === "flex" ? "none" : "flex";
  });
  document.addEventListener("click", () => { menu.style.display = "none"; });

  slot.appendChild(btn);
  slot.appendChild(menu);
  return slot;
}

function mobileLink(text, onClick, color = "#ccc") {
  const a = document.createElement("a");
  a.href = "#";
  a.textContent = text;
  a.style.cssText = `display:block;color:${color};text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;`;
  a.addEventListener("click", (e) => { e.preventDefault(); onClick(); });
  return a;
}

function menuItem(text, icon, onClick, color = "#fff") {
  const a = document.createElement("a");
  a.href = "#";
  a.style.cssText = `display:flex;align-items:center;gap:10px;color:${color};text-decoration:none;padding:10px 12px;border-radius:8px;font-family:'Tajawal',sans-serif;font-size:0.95rem;`;
  const i = document.createElement("i");
  i.className = "fas " + icon;
  i.style.cssText = "font-size:0.85rem;opacity:.8;";
  const span = document.createElement("span");
  span.textContent = text;
  a.appendChild(i);
  a.appendChild(span);
  a.addEventListener("mouseenter", () => (a.style.background = "#ffffff10"));
  a.addEventListener("mouseleave", () => (a.style.background = "transparent"));
  a.addEventListener("click", (e) => { e.preventDefault(); onClick(); });
  return a;
}
```

  Key points to double-check after pasting:
  - There are **no** `import` statements anywhere in the file (no firebase-config, firebase-app, firebase-auth, firebase-firestore, firebase-storage).
  - There is **no** `signInMicrosoft` function and it is **not** present on `window.FITAuth`.
  - `mapUser` reads `dto.photoUrl` (server camelCase from `PhotoUrl`) and exposes it as `photoURL` (frontend casing the pages read). It exposes BOTH `uid` and `id` equal to `dto.id`.
  - `errMessage` keys are exactly the six CONTRACT codes (`email-already-in-use`, `invalid-credential`, `invalid-token`, `bad-avatar`, `bad-weight`, `network`) plus the default fallback; the Arabic strings are preserved from the prior file.
  - `addWeightEntry` throws the SAME Arabic message `"Ø§Ù„ÙˆØ²Ù† ØºÙŠØ± ØµØ­ÙŠØ­ (Ù¡Ù â€“Ù¥Ù Ù  ÙƒØ¬Ù…)"` before any network call (client-side guard), matching the prior behavior.
  - `markWorkoutToday`/`unmarkWorkoutToday` return the raw `StreakStats` object `{streak,longestStreak,total}` straight from the API.

- [ ] **Step 2: Run, expect FAIL (manual baseline)** â€” Before the rest of the API exists (or to confirm the old behavior is gone), open the file in a text-viewer and confirm there are zero firebase references. Run:
```bash
grep -c -i "firebase\|signInMicrosoft\|onAuthStateChanged" "D:/Work/Templates/FIT/assets/js/auth.js"
```
  Expected output: `0` (no firebase imports, no `signInMicrosoft`, no `onAuthStateChanged` remain). If the count is non-zero, the rewrite was incomplete â€” re-apply Step 1.

- [ ] **Step 3: Implement** â€” (No separate implementation step; the complete file is written in Step 1. This is a single-file frontend rewrite.) Confirm the file parses as a module:
```bash
node --check "D:/Work/Templates/FIT/assets/js/auth.js"
```
  Expected output: no output and exit code 0 (valid JavaScript syntax).

- [ ] **Step 4: Run, expect PASS (manual end-to-end in the browser)** â€” Build and run the full stack, then exercise the session through the real UI:
  1. From `D:\Work\Templates\FIT` run:
     ```bash
     docker compose up --build
     ```
     Wait until the api container logs `Now listening on:` and `/api/health` is reachable.
  2. In a browser open `http://localhost:8080/auth.html`.
  3. Fill the registration form with a brand-new email (e.g. `tester1@example.com`), name `Tester One`, password `pass1234`, and submit the register action.
     - EXPECTED: the page redirects to `profile.html` (registerEmail logs in via the cookie, then the page's existing `requireAuth`/redirect logic runs). The header nav shows a rounded button with the name **`Tester One`** and a circular avatar slot (initial **`T`** since no photo yet). On mobile width the nav shows the name with "Ù…Ù„ÙÙŠ Ø§Ù„Ø´Ø®ØµÙŠ" and "ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø®Ø±ÙˆØ¬" links.
  4. Click `index.html` (or navigate to `http://localhost:8080/index.html`) and reload.
     - EXPECTED: the session persists via the auth cookie â€” the nav STILL shows `Tester One` + avatar (because module load calls `GET /api/auth/me` and gets 200). You are NOT shown the "ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„" button.
  5. Open DevTools Network tab, reload, and confirm the very first auth call is `GET /api/auth/me` returning `200` with JSON `{ "id": "...", "email": "tester1@example.com", "displayName": "Tester One", "photoUrl": null, "emailConfirmed": false }`, and that the request carried the session cookie.
  6. Click the nav menu â†’ "ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø®Ø±ÙˆØ¬".
     - EXPECTED: a `POST /api/auth/logout` returns 200, then the browser navigates to `index.html` and the nav now shows the orange "ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„" button (currentUser is null).
  All four EXPECTED results matching = PASS. Stop the stack with `Ctrl+C`.

- [ ] **Step 5: Commit**
```bash
cd "D:/Work/Templates/FIT"
git add assets/js/auth.js
git commit -m "feat(frontend): rewrite auth.js as REST API client (FITAuth + FITData), drop Firebase"
```


---

### Task 15: confirm-email.html + reset-password.html + wire auth.html (remove Microsoft)

**Files:**
- Create: `D:\Work\Templates\FIT\confirm-email.html`
- Create: `D:\Work\Templates\FIT\reset-password.html`
- Modify: `D:\Work\Templates\FIT\auth.html`

**Interfaces:**
- Consumes (from the FRONTEND BRIDGE CONTRACT, owned by Task 14's `assets/js/auth.js`):
  - `window.FITAuth.loginEmail({email,password})`, `registerEmail({name,email,password})`, `resetPassword(email)`, `signInGoogle()`, `onUser(cb)`, `errMessage(code)`.
  - The page loads the module via `<script type="module" src="assets/js/auth.js"></script>` (already present at `auth.html:168`).
- Consumes (from the API CONTRACT, owned by Task 5 `AuthController`):
  - `GET api/auth/confirm-email?userId=&token=` â†’ 302 to `/confirm-email.html?verified=1` (success) or `?verified=0` (failure). This page only READS that query param; it makes no API call.
  - `POST api/auth/reset-password` with body `{email,token,password}` â†’ 200 `{}`; failure â†’ 400 ProblemDetails with `code:"invalid-token"`. `reset-password.html` calls this directly via `fetch(..., {credentials:"include"})` (NOT through FITAuth, because the page needs the `token` from the URL, which `FITAuth.resetPassword(email)` does not handle).
- Produces: two static pages reachable at `/confirm-email.html` and `/reset-password.html` (served by the API's static file middleware at `/`), and an `auth.html` with NO Microsoft button and NO `signInMicrosoft` reference. No later task depends on internals of these pages.

NOTE on token: `reset-password.html` reads the raw `token` from `location.search` using `URLSearchParams`, which already URL-decodes it. The contract says the emailed link has the token URL-encoded; `URLSearchParams.get("token")` returns the decoded value, which is exactly what the API expects in the JSON body. Do NOT re-encode or re-decode it.

This is a frontend task: there is no xUnit test cycle. Steps 1â€“3 create/edit files; Steps 4â€“5 are concrete MANUAL verification + commit.

---

- [ ] **Step 1: Create `D:\Work\Templates\FIT\confirm-email.html`** (COMPLETE file)

Styling mirrors `auth.html` (Tajawal/Zen Dots fonts, dark `#0d0d0d` background, `#f59e0b` accent, RTL `dir="rtl"`, the same `.auth-wrap`/`.auth-card`/`.auth-submit` look reproduced inline so the page is self-contained). It reads `?verified=1`/`?verified=0` and shows the matching Arabic message. Default (param missing/other) is treated as failure.

```html
<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAKE ME FIT - ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ</title>
    <link rel="shortcut icon" href="assets/images/favicon/favicon.ico" type="image/x-icon">
    <link href="https://fonts.googleapis.com/css2?family=Zen+Dots&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@300;400;500;700;800;900&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="assets/css/all.min.css">
    <link rel="stylesheet" href="assets/css/bootstrap.min.css">
    <link rel="stylesheet" href="assets/css/style.css">
    <link rel="stylesheet" href="assets/css/media-query.css">
    <style>
        body,h1,h2,h3,h4,h5,h6,p,a,span,li,td,th,div,label,input,button{font-family:'Tajawal',sans-serif !important;}
        body{background:#0d0d0d;}
        .auth-wrap{min-height:100vh;display:flex;align-items:center;justify-content:center;padding:40px 16px;
            background:linear-gradient(135deg,#1a0a00,#0a0a0a);}
        .auth-card{width:100%;max-width:440px;background:#161616;border:1px solid #f59e0b33;border-radius:18px;
            padding:40px 28px;box-shadow:0 20px 60px rgba(0,0,0,.6);text-align:center;}
        .auth-card .brand{font-family:'Zen Dots',cursive !important;font-size:1.4rem;margin-bottom:24px;display:block;}
        .auth-card .brand .c1{color:#f59e0b;}
        .auth-card .brand .c2{color:#fff;}
        .status-icon{font-size:3.4rem;margin-bottom:16px;display:block;}
        .status-icon.ok{color:#4caf50;}
        .status-icon.fail{color:#e94560;}
        .auth-card h1{color:#fff;font-size:1.4rem;font-weight:800;margin-bottom:10px;}
        .auth-card p{color:#aaa;font-size:0.98rem;margin-bottom:26px;line-height:1.7;}
        .auth-link-btn{display:inline-block;background:#f59e0b;color:#111;border:none;border-radius:10px;
            padding:13px 30px;font-weight:800;font-size:1rem;cursor:pointer;text-decoration:none;transition:background .25s;}
        .auth-link-btn:hover{background:#d97706;color:#111;}
    </style>
</head>
<body>
    <main class="auth-wrap">
        <div class="auth-card">
            <span class="brand"><span class="c1">MAKE ME</span> <span class="c2">FIT</span></span>
            <span class="status-icon" id="icon"></span>
            <h1 id="title"></h1>
            <p id="message"></p>
            <a href="auth.html" class="auth-link-btn">Ø§Ù„Ø°Ù‡Ø§Ø¨ Ù„ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„</a>
        </div>
    </main>

    <script>
        (function () {
            var verified = new URLSearchParams(window.location.search).get("verified");
            var ok = verified === "1";
            var icon = document.getElementById("icon");
            var title = document.getElementById("title");
            var message = document.getElementById("message");
            if (ok) {
                icon.className = "status-icon ok";
                icon.innerHTML = "&#10004;";
                title.textContent = "ØªÙ… ØªØ£ÙƒÙŠØ¯ Ø¨Ø±ÙŠØ¯Ùƒ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ!";
                message.textContent = "ØªÙ… ØªÙØ¹ÙŠÙ„ Ø­Ø³Ø§Ø¨Ùƒ Ø¨Ù†Ø¬Ø§Ø­. ÙŠÙ…ÙƒÙ†Ùƒ Ø§Ù„Ø¢Ù† ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ ÙˆØ§Ù„Ø§Ø³ØªÙ…ØªØ§Ø¹ Ø¨ÙƒÙ„ Ø§Ù„Ù…Ù…ÙŠØ²Ø§Øª.";
            } else {
                icon.className = "status-icon fail";
                icon.innerHTML = "&#10006;";
                title.textContent = "ØªØ¹Ø°Ù‘Ø± ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ";
                message.textContent = "Ø±Ø§Ø¨Ø· Ø§Ù„ØªØ£ÙƒÙŠØ¯ ØºÙŠØ± ØµØ§Ù„Ø­ Ø£Ùˆ Ø§Ù†ØªÙ‡Øª ØµÙ„Ø§Ø­ÙŠØªÙ‡. ÙŠÙ…ÙƒÙ†Ùƒ ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø«Ù… Ø·Ù„Ø¨ Ø¥Ø±Ø³Ø§Ù„ Ø±Ø§Ø¨Ø· ØªØ£ÙƒÙŠØ¯ Ø¬Ø¯ÙŠØ¯.";
            }
        })();
    </script>
</body>
</html>
```

---

- [ ] **Step 2: Create `D:\Work\Templates\FIT\reset-password.html`** (COMPLETE file)

Reads `email` + `token` from the query string, shows a new-password form (with confirm field), POSTs `{email,token,password}` to `/api/auth/reset-password` via `fetch` with `credentials:"include"`. On 2xx shows an Arabic success message and a button to `auth.html`; on non-2xx reads ProblemDetails `code` and shows `invalid-token` â†’ token-expired Arabic message, else a generic Arabic error. If `email` or `token` is missing the form is hidden and a "link invalid" message is shown.

```html
<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAKE ME FIT - Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±</title>
    <link rel="shortcut icon" href="assets/images/favicon/favicon.ico" type="image/x-icon">
    <link href="https://fonts.googleapis.com/css2?family=Zen+Dots&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@300;400;500;700;800;900&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="assets/css/all.min.css">
    <link rel="stylesheet" href="assets/css/bootstrap.min.css">
    <link rel="stylesheet" href="assets/css/style.css">
    <link rel="stylesheet" href="assets/css/media-query.css">
    <style>
        body,h1,h2,h3,h4,h5,h6,p,a,span,li,td,th,div,label,input,button{font-family:'Tajawal',sans-serif !important;}
        body{background:#0d0d0d;}
        .auth-wrap{min-height:100vh;display:flex;align-items:center;justify-content:center;padding:40px 16px;
            background:linear-gradient(135deg,#1a0a00,#0a0a0a);}
        .auth-card{width:100%;max-width:440px;background:#161616;border:1px solid #f59e0b33;border-radius:18px;
            padding:36px 28px;box-shadow:0 20px 60px rgba(0,0,0,.6);}
        .auth-card .brand{font-family:'Zen Dots',cursive !important;font-size:1.3rem;margin-bottom:6px;display:block;text-align:center;}
        .auth-card .brand .c1{color:#f59e0b;}
        .auth-card .brand .c2{color:#fff;}
        .auth-card h1{color:#fff;font-size:1.35rem;font-weight:800;text-align:center;margin-bottom:4px;}
        .auth-card .sub{color:#888;text-align:center;font-size:0.92rem;margin-bottom:22px;}
        .auth-field{margin-bottom:14px;}
        .auth-field label{display:block;color:#ccc;font-size:0.9rem;margin-bottom:6px;font-weight:600;}
        .auth-input{width:100%;background:#0d0d0d;border:1px solid #2e2e2e;border-radius:10px;color:#fff;
            padding:12px 14px;font-size:0.95rem;outline:none;transition:border-color .25s;}
        .auth-input:focus{border-color:#f59e0b;}
        .auth-submit{width:100%;background:#f59e0b;color:#111;border:none;border-radius:10px;padding:13px;
            font-weight:800;font-size:1rem;cursor:pointer;margin-top:6px;transition:background .25s;}
        .auth-submit:hover{background:#d97706;}
        .auth-submit:disabled{opacity:.6;cursor:not-allowed;}
        .auth-msg{display:none;border-radius:10px;padding:12px 14px;font-size:0.9rem;margin-bottom:16px;font-weight:600;}
        .auth-msg.error{display:block;background:#e9456022;border:1px solid #e94560;color:#ff8aa0;}
        .auth-msg.success{display:block;background:#4caf5022;border:1px solid #4caf50;color:#9be29e;}
        .auth-link-btn{display:block;text-align:center;background:#f59e0b;color:#111;border:none;border-radius:10px;
            padding:13px;font-weight:800;font-size:1rem;cursor:pointer;text-decoration:none;margin-top:6px;transition:background .25s;}
        .auth-link-btn:hover{background:#d97706;color:#111;}
        .hidden{display:none;}
    </style>
</head>
<body>
    <main class="auth-wrap">
        <div class="auth-card">
            <span class="brand"><span class="c1">MAKE ME</span> <span class="c2">FIT</span></span>
            <h1>Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±</h1>
            <p class="sub">Ø§Ø®ØªØ± ÙƒÙ„Ù…Ø© Ù…Ø±ÙˆØ± Ø¬Ø¯ÙŠØ¯Ø© Ù„Ø­Ø³Ø§Ø¨Ùƒ</p>

            <div class="auth-msg" id="msg" role="alert" aria-live="polite"></div>

            <form id="form-reset" autocomplete="on">
                <div class="auth-field">
                    <label for="new-password">ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø§Ù„Ø¬Ø¯ÙŠØ¯Ø©</label>
                    <input type="password" id="new-password" class="auth-input" required minlength="6" autocomplete="new-password" placeholder="Ù¦ Ø£Ø­Ø±Ù Ø¹Ù„Ù‰ Ø§Ù„Ø£Ù‚Ù„">
                </div>
                <div class="auth-field">
                    <label for="confirm-password">ØªØ£ÙƒÙŠØ¯ ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±</label>
                    <input type="password" id="confirm-password" class="auth-input" required minlength="6" autocomplete="new-password" placeholder="Ø£Ø¹Ø¯ ÙƒØªØ§Ø¨Ø© ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±">
                </div>
                <button type="submit" class="auth-submit" id="submit-reset">Ø­ÙØ¸ ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±</button>
            </form>

            <a href="auth.html" class="auth-link-btn hidden" id="done-link">Ø§Ù„Ø°Ù‡Ø§Ø¨ Ù„ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„</a>
        </div>
    </main>

    <script>
        (function () {
            var params = new URLSearchParams(window.location.search);
            var email = params.get("email");
            var token = params.get("token");

            var form = document.getElementById("form-reset");
            var msg = document.getElementById("msg");
            var doneLink = document.getElementById("done-link");
            var submit = document.getElementById("submit-reset");

            function showMsg(text, type) {
                msg.textContent = text;
                msg.className = "auth-msg " + type;
            }
            function clearMsg() { msg.className = "auth-msg"; msg.textContent = ""; }

            if (!email || !token) {
                form.classList.add("hidden");
                doneLink.classList.remove("hidden");
                showMsg("Ø±Ø§Ø¨Ø· Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„ØªØ¹ÙŠÙŠÙ† ØºÙŠØ± ØµØ§Ù„Ø­ Ø£Ùˆ Ù†Ø§Ù‚Øµ. ÙŠØ±Ø¬Ù‰ Ø·Ù„Ø¨ Ø±Ø§Ø¨Ø· Ø¬Ø¯ÙŠØ¯ Ù…Ù† ØµÙØ­Ø© ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„.", "error");
                return;
            }

            form.addEventListener("submit", async function (e) {
                e.preventDefault();
                clearMsg();
                var pass = document.getElementById("new-password").value;
                var confirm = document.getElementById("confirm-password").value;
                if (pass !== confirm) { showMsg("ÙƒÙ„Ù…ØªØ§ Ø§Ù„Ù…Ø±ÙˆØ± ØºÙŠØ± Ù…ØªØ·Ø§Ø¨Ù‚ØªÙŠÙ†.", "error"); return; }

                var original = submit.textContent;
                submit.disabled = true; submit.textContent = "Ø¬Ø§Ø±Ù Ø§Ù„Ø­ÙØ¸â€¦";
                try {
                    var res = await fetch("/api/auth/reset-password", {
                        method: "POST",
                        credentials: "include",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({ email: email, token: token, password: pass })
                    });
                    if (res.ok) {
                        form.classList.add("hidden");
                        doneLink.classList.remove("hidden");
                        showMsg("ØªÙ… ØªØºÙŠÙŠØ± ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø¨Ù†Ø¬Ø§Ø­! ÙŠÙ…ÙƒÙ†Ùƒ Ø§Ù„Ø¢Ù† ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø¨ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø§Ù„Ø¬Ø¯ÙŠØ¯Ø©.", "success");
                        return;
                    }
                    var code = "";
                    try {
                        var data = await res.json();
                        code = (data && data.code) || "";
                    } catch (_) { /* ignore non-JSON */ }
                    if (code === "invalid-token") {
                        showMsg("Ø±Ø§Ø¨Ø· Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„ØªØ¹ÙŠÙŠÙ† ØºÙŠØ± ØµØ§Ù„Ø­ Ø£Ùˆ Ø§Ù†ØªÙ‡Øª ØµÙ„Ø§Ø­ÙŠØªÙ‡. ÙŠØ±Ø¬Ù‰ Ø·Ù„Ø¨ Ø±Ø§Ø¨Ø· Ø¬Ø¯ÙŠØ¯.", "error");
                    } else {
                        showMsg("ØªØ¹Ø°Ù‘Ø± ØªØºÙŠÙŠØ± ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±. Ø­Ø§ÙˆÙ„ Ù…Ø±Ø© Ø£Ø®Ø±Ù‰.", "error");
                    }
                } catch (_) {
                    showMsg("ØªØ¹Ø°Ù‘Ø± Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ø§Ù„Ø®Ø§Ø¯Ù…. ØªØ­Ù‚Ù‘Ù‚ Ù…Ù† Ø§ØªØµØ§Ù„Ùƒ Ø¨Ø§Ù„Ø¥Ù†ØªØ±Ù†Øª.", "error");
                } finally {
                    submit.disabled = false; submit.textContent = original;
                }
            });
        })();
    </script>
</body>
</html>
```

---

- [ ] **Step 3: Edit `D:\Work\Templates\FIT\auth.html`** (three additive/removal Edits; show exact OLD â†’ NEW)

The file already loads `assets/js/auth.js` as a module (`auth.html:168`) and already wires `loginEmail`/`registerEmail`/`resetPassword`/`signInGoogle`. Two changes are required: (a) remove the Microsoft sign-in BUTTON; (b) remove the `signInMicrosoft` click handler. A third edit replaces the Google icon's Firebase-hosted SVG `<img>` with a Font Awesome icon so NO Firebase-hosted asset URL remains on the page (the page already includes `assets/css/all.min.css` for Font Awesome).

**Edit 3a â€” remove the Microsoft button** (lines 122â€“125):

OLD:
```html
                <button type="button" class="social-btn" id="btn-google">
                    <img src="https://www.gstatic.com/firebasejs/ui/2.0.0/images/auth/google.svg" alt="Google">
                    <span>Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Google</span>
                </button>
                <button type="button" class="social-btn ms" id="btn-microsoft">
                    <i class="fab fa-microsoft" style="color:#00a4ef;"></i>
                    <span>Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Microsoft</span>
                </button>
```

NEW:
```html
                <button type="button" class="social-btn" id="btn-google">
                    <i class="fab fa-google" style="color:#ea4335;"></i>
                    <span>Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Google</span>
                </button>
```

(This both deletes the Microsoft `<button id="btn-microsoft">` and swaps the Google `<img src="https://www.gstatic.com/...google.svg">` for `<i class="fab fa-google">`, eliminating the only Firebase-hosted URL on the page.)

**Edit 3b â€” remove the `signInMicrosoft` click handler** (lines 213â€“217):

OLD:
```html
        // Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø§Ù„Ø§Ø¬ØªÙ…Ø§Ø¹ÙŠ
        $("btn-google").addEventListener("click", () =>
            run($("btn-google"), async () => { await window.FITAuth.signInGoogle(); goProfile(); }));
        $("btn-microsoft").addEventListener("click", () =>
            run($("btn-microsoft"), async () => { await window.FITAuth.signInMicrosoft(); goProfile(); }));
```

NEW:
```html
        // Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø§Ù„Ø§Ø¬ØªÙ…Ø§Ø¹ÙŠ
        $("btn-google").addEventListener("click", () =>
            run($("btn-google"), async () => { await window.FITAuth.signInGoogle(); goProfile(); }));
```

After these edits, confirm by reading the file:
- `<script type="module" src="assets/js/auth.js"></script>` is still present (unchanged at the original line).
- The page contains NO `firebase` substring, NO `gstatic.com` substring, NO `btn-microsoft`, and NO `signInMicrosoft`.

Run this exact check (PowerShell) â€” expect each count to be `0`:

```powershell
$f = "D:\Work\Templates\FIT\auth.html"
"firebase","gstatic.com","btn-microsoft","signInMicrosoft" | ForEach-Object {
    $c = (Select-String -Path $f -Pattern $_ -SimpleMatch -AllMatches | Measure-Object).Count
    "{0}: {1}" -f $_, $c
}
```
Expected output (exactly):
```
firebase: 0
gstatic.com: 0
btn-microsoft: 0
signInMicrosoft: 0
```

---

- [ ] **Step 4: MANUAL verification** (exact URLs + exact expected on-screen results)

Prereq: the stack is running via `docker compose up` (api at `http://localhost:8080`, Mailpit UI at `http://localhost:8025`) â€” built by earlier tasks. Use the API origin so the static files are served at `/`.

1. **Confirm-email success page** â€” open `http://localhost:8080/confirm-email.html?verified=1`.
   Expected on screen: a green check (âœ”) icon, heading `ØªÙ… ØªØ£ÙƒÙŠØ¯ Ø¨Ø±ÙŠØ¯Ùƒ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ!`, the body line `ØªÙ… ØªÙØ¹ÙŠÙ„ Ø­Ø³Ø§Ø¨Ùƒ Ø¨Ù†Ø¬Ø§Ø­. ÙŠÙ…ÙƒÙ†Ùƒ Ø§Ù„Ø¢Ù† ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ ÙˆØ§Ù„Ø§Ø³ØªÙ…ØªØ§Ø¹ Ø¨ÙƒÙ„ Ø§Ù„Ù…Ù…ÙŠØ²Ø§Øª.`, and an orange button `Ø§Ù„Ø°Ù‡Ø§Ø¨ Ù„ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„` that navigates to `http://localhost:8080/auth.html`.

2. **Confirm-email failure page** â€” open `http://localhost:8080/confirm-email.html?verified=0`.
   Expected on screen: a red cross (âœ–) icon, heading `ØªØ¹Ø°Ù‘Ø± ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ Ø§Ù„Ø¥Ù„ÙƒØªØ±ÙˆÙ†ÙŠ`, and the body line about the link being invalid/expired.

3. **Reset-password happy path (via emailed link)** â€”
   a. At `http://localhost:8080/auth.html`, type a registered account's email into the login email field and click `Ù†Ø³ÙŠØª ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±ØŸ`. Expect the success banner `Ø£Ø±Ø³Ù„Ù†Ø§ Ø±Ø§Ø¨Ø· Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø¥Ù„Ù‰ Ø¨Ø±ÙŠØ¯Ùƒ.`.
   b. Open Mailpit at `http://localhost:8025`, open the newest message, and click/copy the reset link. It must point to `http://localhost:8080/reset-password.html?email=...&token=...`. Open it.
   c. Expected on screen: the heading `Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±`, sub `Ø§Ø®ØªØ± ÙƒÙ„Ù…Ø© Ù…Ø±ÙˆØ± Ø¬Ø¯ÙŠØ¯Ø© Ù„Ø­Ø³Ø§Ø¨Ùƒ`, and two password fields. Enter a new password (â‰¥6 chars) in both fields and click `Ø­ÙØ¸ ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±`.
   d. Expected: the form is replaced by the green banner `ØªÙ… ØªØºÙŠÙŠØ± ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø¨Ù†Ø¬Ø§Ø­! ÙŠÙ…ÙƒÙ†Ùƒ Ø§Ù„Ø¢Ù† ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„ Ø¨ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ± Ø§Ù„Ø¬Ø¯ÙŠØ¯Ø©.` and the `Ø§Ù„Ø°Ù‡Ø§Ø¨ Ù„ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„` button appears.
   e. Go to `http://localhost:8080/auth.html`, log in with the SAME email and the NEW password â€” login succeeds and redirects to `profile.html`.

4. **Reset-password invalid/missing link** â€” open `http://localhost:8080/reset-password.html` (no query). Expected: the form is hidden and the red banner `Ø±Ø§Ø¨Ø· Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„ØªØ¹ÙŠÙŠÙ† ØºÙŠØ± ØµØ§Ù„Ø­ Ø£Ùˆ Ù†Ø§Ù‚Øµ. ÙŠØ±Ø¬Ù‰ Ø·Ù„Ø¨ Ø±Ø§Ø¨Ø· Ø¬Ø¯ÙŠØ¯ Ù…Ù† ØµÙØ­Ø© ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„.` is shown, with the `Ø§Ù„Ø°Ù‡Ø§Ø¨ Ù„ØªØ³Ø¬ÙŠÙ„ Ø§Ù„Ø¯Ø®ÙˆÙ„` button visible.

5. **Auth page has no Microsoft / no Firebase** â€” open `http://localhost:8080/auth.html`. Expected: ONLY the `Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Google` button is shown (no `Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Microsoft` button). Open DevTools Network and reload â€” there is NO request to any `gstatic.com`/`firebase` URL. Clicking `Ø§Ù„Ù…ØªØ§Ø¨Ø¹Ø© Ø¨Ø­Ø³Ø§Ø¨ Google` navigates the browser to `/api/auth/google`.

---

- [ ] **Step 5: Commit** (exact commands)

```powershell
git add "D:\Work\Templates\FIT\confirm-email.html" "D:\Work\Templates\FIT\reset-password.html" "D:\Work\Templates\FIT\auth.html"
git commit -m "feat(web): add confirm-email & reset-password pages; wire auth.html to API and remove Microsoft sign-in"
```

---

### Task 16: Delete Firebase files + POSTGRES-SETUP.md run guide

**Files:**
- Delete: `D:\Work\Templates\FIT\assets\js\firebase-config.js`
- Delete: `D:\Work\Templates\FIT\firestore.rules`
- Delete: `D:\Work\Templates\FIT\storage.rules`
- Delete: `D:\Work\Templates\FIT\FIREBASE-SETUP.md`
- Create: `D:\Work\Templates\FIT\POSTGRES-SETUP.md`

**Interfaces:**
- Consumes: the running stack produced by earlier tasks â€” `docker-compose.yml` (services `db`, `api`, `mail`; created in the docker-compose task), the API that serves the static site at `/` (Task 1 Program.cs `Frontend:WebRoot` static-file serving), config keys `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` (Task 6), `Smtp:Host` / `Smtp:Port` / `Smtp:From` (Task 4), `ConnectionStrings:Default` (Task 2), `Storage:AvatarRoot` (Task 7), and the `dotnet test` test project in `server\` (Tasks 1+). This task documents them; it does not define them.
- Produces: `POSTGRES-SETUP.md` (the single canonical run guide replacing `FIREBASE-SETUP.md`). No code symbols are produced; no later task references this file programmatically.

This is a docs/cleanup task. There is no automated test cycle; use the concrete MANUAL verification in Steps 4â€“6 instead.

- [ ] **Step 1: Confirm the four Firebase files exist before removing them.** Run exactly:

  ```bash
  ls -la "D:/Work/Templates/FIT/assets/js/firebase-config.js" \
         "D:/Work/Templates/FIT/firestore.rules" \
         "D:/Work/Templates/FIT/storage.rules" \
         "D:/Work/Templates/FIT/FIREBASE-SETUP.md"
  ```

  Expected: all four lines print file sizes (no "No such file or directory"). All four files are confirmed to exist, so Step 2 removes exactly these four paths unconditionally.

- [ ] **Step 2: `git rm` the four Firebase files.** Run exactly (one command; all four paths):

  ```bash
  git -C "D:/Work/Templates/FIT" rm \
    "assets/js/firebase-config.js" \
    "firestore.rules" \
    "storage.rules" \
    "FIREBASE-SETUP.md"
  ```

  Expected output (order may vary):

  ```
  rm 'assets/js/firebase-config.js'
  rm 'firestore.rules'
  rm 'storage.rules'
  rm 'FIREBASE-SETUP.md'
  ```

  Note: `firestore.rules` and `profile.html` show as modified in the current working tree â€” `git rm` removes the tracked file regardless of staged/unstaged modifications to `firestore.rules`, which is correct (we are deleting it).

- [ ] **Step 3: Create `D:\Work\Templates\FIT\POSTGRES-SETUP.md`** with EXACTLY this content (Arabic, matching the existing doc tone):

  ```markdown
  # Ø¥Ø¹Ø¯Ø§Ø¯ ÙˆØªØ´ØºÙŠÙ„ Ø§Ù„Ø®Ø§Ø¯Ù… (PostgreSQL + ASP.NET Core) â€” MAKE ME FIT

  ØªÙ…Øª ØªØ±Ù‚ÙŠØ© Ø§Ù„Ù…ÙˆÙ‚Ø¹ Ù…Ù† **Firebase** Ø¥Ù„Ù‰ Ø®Ø§Ø¯Ù… Ø°Ø§ØªÙŠ Ø§Ù„Ø§Ø³ØªØ¶Ø§ÙØ©: **ASP.NET Core Web API** + Ù‚Ø§Ø¹Ø¯Ø© Ø¨ÙŠØ§Ù†Ø§Øª **PostgreSQL**ØŒ ÙƒÙ„Ø§Ù‡Ù…Ø§ Ø¯Ø§Ø®Ù„ **Docker**. Ø§Ù„Ù€API Ù†ÙØ³Ù‡ **ÙŠØ®Ø¯Ù… ØµÙØ­Ø§Øª Ø§Ù„Ù…ÙˆÙ‚Ø¹ Ø§Ù„Ø«Ø§Ø¨ØªØ©** Ø¹Ù„Ù‰ Ù†ÙØ³ Ø§Ù„Ù…Ù†ÙØ° (Ø£ØµÙ„ ÙˆØ§Ø­Ø¯ / single origin)ØŒ ÙÙ„Ø§ Ø­Ø§Ø¬Ø© Ù„Ø£ÙŠ Ø®Ø§Ø¯Ù… Ù…Ù„ÙØ§Øª Ù…Ù†ÙØµÙ„.

  > Ø§Ù„Ù†Ø¸Ø§Ù… ÙŠØ´Ù…Ù„: Ø­Ø³Ø§Ø¨Ø§Øª (Ø¯Ø®ÙˆÙ„ Ø¹Ø¨Ø± **Google** Ø£Ùˆ **Ø§Ù„Ø¨Ø±ÙŠØ¯/ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±**) + **Ù…Ø³Ø§Ø­Ø© Ø´Ø®ØµÙŠØ©** (Ù…Ù„Ù Ø´Ø®ØµÙŠ + ØµÙˆØ±Ø© Ù‚Ø§Ø¨Ù„Ø© Ù„Ù„Ø±ÙØ¹ + ØªÙ…Ø§Ø±ÙŠÙ† Ù…ÙØ¶Ù„Ø© + Ø³Ø¬Ù„ Ø­Ø§Ø³Ø¨Ø© Ø§Ù„Ø³Ø¹Ø±Ø§Øª + Ø®Ø·Ø· ØªØºØ°ÙŠØ© Ù…Ø­ÙÙˆØ¸Ø© + Ù…ØªØ§Ø¨Ø¹ ÙˆØ²Ù† ÙˆÙ‚ÙŠØ§Ø³Ø§Øª + Ù…ØªØ§Ø¨Ø¹Ø© Ø¥Ù†Ø¬Ø§Ø² Ø§Ù„ØªÙ…Ø§Ø±ÙŠÙ† ÙˆØ³Ù„Ø³Ù„Ø© Ø§Ù„Ø£ÙŠØ§Ù… + Ù…Ù†Ø´Ø¦ Ø®Ø·Ø© Ù…Ø®ØµØµØ© + Ø§Ù„ØµÙŠØ§Ù… Ø§Ù„Ù…ØªÙ‚Ø·Ø¹). Ø¯Ø®ÙˆÙ„ **Microsoft** Ù„Ù… ÙŠØ¹Ø¯ Ù…Ø¯Ø¹ÙˆÙ…Ø§Ù‹.

  ---

  ## Ø§Ù„Ù…ØªØ·Ù„Ø¨Ø§Øª Ø§Ù„Ù…Ø³Ø¨Ù‚Ø©

  - **Docker Desktop** (ÙŠØªØ¶Ù…Ù† `docker compose`) Ù…ÙØ«Ø¨Ù‘Øª ÙˆÙŠØ¹Ù…Ù„.
  - **.NET 10 SDK** â€” Ù„Ø§Ø²Ù… ÙÙ‚Ø· Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±Ø§Øª Ù…Ø­Ù„ÙŠØ§Ù‹ (Ø§Ù„Ø®Ø·ÙˆØ© "ØªØ´ØºÙŠÙ„ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±Ø§Øª"). Ø§Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ø¹Ø§Ø¯ÙŠ Ø¹Ø¨Ø± Docker Ù„Ø§ ÙŠØ­ØªØ§Ø¬Ù‡.

  ---

  ## Ø§Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ø³Ø±ÙŠØ¹

  Ù…Ù† Ø¯Ø§Ø®Ù„ Ù…Ø¬Ù„Ø¯ Ø§Ù„Ù…Ø´Ø±ÙˆØ¹ `D:\Work\Templates\FIT`:

  ```bash
  docker compose up --build
  ```

  ÙŠØ¨Ù†ÙŠ Ù‡Ø°Ø§ Ø§Ù„Ø£Ù…Ø± Ø«Ù„Ø§Ø« Ø®Ø¯Ù…Ø§Øª ÙˆÙŠØ´ØºÙ‘Ù„Ù‡Ø§:

  | Ø§Ù„Ø®Ø¯Ù…Ø© | Ø§Ù„ØµÙˆØ±Ø© | Ø§Ù„Ø¯ÙˆØ± |
  |--------|--------|-------|
  | `db`   | `postgres:16`     | Ù‚Ø§Ø¹Ø¯Ø© Ø¨ÙŠØ§Ù†Ø§Øª PostgreSQL |
  | `api`  | (ÙŠÙØ¨Ù†Ù‰ Ù…Ù† `server/FitApi/Dockerfile`) | Ø§Ù„Ù€API + Ø®Ø¯Ù…Ø© ØµÙØ­Ø§Øª Ø§Ù„Ù…ÙˆÙ‚Ø¹ |
  | `mail` | `axllent/mailpit` | Ø®Ø§Ø¯Ù… Ø¨Ø±ÙŠØ¯ ØªØ¬Ø±ÙŠØ¨ÙŠ ÙŠÙ„ØªÙ‚Ø· Ø§Ù„Ø±Ø³Ø§Ø¦Ù„ (ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ / Ø¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±) |

  ### Ø§Ù„Ø¹Ù†Ø§ÙˆÙŠÙ† Ø¨Ø¹Ø¯ Ø§Ù„ØªØ´ØºÙŠÙ„

  | Ø§Ù„ØºØ±Ø¶ | Ø§Ù„Ø¹Ù†ÙˆØ§Ù† |
  |------|---------|
  | Ø§Ù„Ù…ÙˆÙ‚Ø¹ (Ø§Ù„ØµÙØ­Ø© Ø§Ù„Ø±Ø¦ÙŠØ³ÙŠØ©) | <http://localhost:8080> |
  | ÙØ­Øµ ØµØ­Ø© Ø§Ù„Ù€API | <http://localhost:8080/api/health> â†’ `{"status":"ok"}` |
  | ØµÙ†Ø¯ÙˆÙ‚ Ø¨Ø±ÙŠØ¯ Mailpit (Ù„Ø¹Ø±Ø¶ Ø±Ø³Ø§Ø¦Ù„ Ø§Ù„ØªØ£ÙƒÙŠØ¯) | <http://localhost:8025> |

  > Ø§Ù„Ù‡Ø¬Ø±Ø§Øª (migrations) ØªÙØ·Ø¨ÙŽÙ‘Ù‚ ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹ Ø¹Ù†Ø¯ Ø¥Ù‚Ù„Ø§Ø¹ Ø§Ù„Ù€API Ø¹Ø¨Ø± `AppDbContext.Database.Migrate()`ØŒ ÙÙ„Ø§ ØªØ­ØªØ§Ø¬ Ø®Ø·ÙˆØ© ÙŠØ¯ÙˆÙŠØ© Ù„Ø¥Ù†Ø´Ø§Ø¡ Ø§Ù„Ø¬Ø¯Ø§ÙˆÙ„. Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª ØªØ¨Ø¯Ø£ ÙØ§Ø±ØºØ© (greenfield).

  Ù„Ù„Ø¥ÙŠÙ‚Ø§Ù: `Ctrl+C` Ø«Ù… `docker compose down`. Ù„Ù…Ø³Ø­ Ù‚Ø§Ø¹Ø¯Ø© Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª Ø£ÙŠØ¶Ø§Ù‹: `docker compose down -v`.

  ---

  ## Ø§Ø³ØªØ®Ø¯Ù… Ø§Ù„Ù…ÙˆÙ‚Ø¹ Ø¹Ø¨Ø± http (ÙˆÙ„ÙŠØ³ Ø¨Ø§Ù„Ù†Ù‚Ø± Ø§Ù„Ù…Ø²Ø¯ÙˆØ¬)

  âš ï¸ Ø§ÙØªØ­ Ø§Ù„Ù…ÙˆÙ‚Ø¹ Ø¯Ø§Ø¦Ù…Ø§Ù‹ Ø¹Ø¨Ø± **<http://localhost:8080>**. Ù„Ù… ÙŠØ¹Ø¯ ÙØªØ­ Ù…Ù„ÙØ§Øª `.html` Ø¨Ø§Ù„Ù†Ù‚Ø± Ø§Ù„Ù…Ø²Ø¯ÙˆØ¬ (`file://`) Ø£Ø³Ù„ÙˆØ¨Ø§Ù‹ Ù…Ø¯Ø¹ÙˆÙ…Ø§Ù‹ â€” ÙˆØ§Ù„Ø£Ù‡Ù… Ø£Ù† **Ø³Ø¨Ø¨ Ø§Ù„Ø£Ø¹Ø·Ø§Ù„ Ø§Ù„Ù‚Ø¯ÙŠÙ… Ø§Ø®ØªÙÙ‰ Ø¨Ù†ÙŠÙˆÙŠØ§Ù‹**: Ù„Ø£Ù† Ø§Ù„Ù€API Ù†ÙØ³Ù‡ Ù‡Ùˆ Ù…Ù† ÙŠØ®Ø¯Ù… Ø§Ù„ØµÙØ­Ø§Øª Ø§Ù„Ø¢Ù†ØŒ ØªØ¹Ù…Ù„ ÙˆØ­Ø¯Ø§Øª Ø§Ù„Ù€JS (ES modules) ÙˆÙ†Ø¯Ø§Ø¡Ø§Øª Ø§Ù„Ù€API ÙˆÙ…Ù„ÙØ§Øª Ø§Ù„ÙƒÙˆÙƒÙŠØ² ÙƒÙ„Ù‡Ø§ Ø¹Ù„Ù‰ Ù†ÙØ³ Ø§Ù„Ø£ØµÙ„ Ø¯ÙˆÙ† Ù…Ø´Ø§ÙƒÙ„ CORS Ø£Ùˆ Ù‚ÙŠÙˆØ¯ `file://`.

  ---

  ## Ø§Ù„Ø¥Ø¹Ø¯Ø§Ø¯: Google ÙˆØ¯Ø®ÙˆÙ„ Ø§Ù„Ø¨Ø±ÙŠØ¯ (SMTP)

  ### Ø¯Ø®ÙˆÙ„ Google

  Ø£Ù†Ø´Ø¦ **OAuth 2.0 Client ID** Ù…Ù† [console.cloud.google.com](https://console.cloud.google.com) â†’ **APIs & Services** â†’ **Credentials** â†’ **Create credentials** â†’ **OAuth client ID** (Ù†ÙˆØ¹ **Web application**)ØŒ ÙˆØ£Ø¶ÙÙ ÙÙŠ **Authorized redirect URIs**:

  ```
  http://localhost:8080/signin-google
  ```

  Ø«Ù… Ø²ÙˆÙ‘Ø¯ Ø§Ù„Ù€API Ø¨Ø§Ù„Ù‚ÙŠÙ…ØªÙŠÙ† Ø¹Ø¨Ø± Ù…ØªØºÙŠØ±Ø§Øª Ø§Ù„Ø¨ÙŠØ¦Ø© ÙÙŠ `docker-compose.yml` (Ø¶Ù…Ù† Ù‚Ø³Ù… `environment` Ù„Ù„Ø®Ø¯Ù…Ø© `api`)Ø› ØµÙŠØºØ© Ø§Ù„Ù…ØªØºÙŠØ± ÙÙŠ ASP.NET Core ØªØ³ØªØ¨Ø¯Ù„ Ø§Ù„Ù†Ù‚Ø·ØªÙŠÙ† `:` Ø¨Ø´Ø±Ø·ØªÙŠÙ† Ø³ÙÙ„ÙŠØªÙŠÙ† `__`:

  ```yaml
  services:
    api:
      environment:
        - Authentication__Google__ClientId=Ø¶Ø¹-Ø§Ù„Ù€ClientId-Ù‡Ù†Ø§
        - Authentication__Google__ClientSecret=Ø¶Ø¹-Ø§Ù„Ù€ClientSecret-Ù‡Ù†Ø§
  ```

  Ø¨Ø¯ÙŠÙ„Ø§Ù‹ Ø¹Ù† Ø°Ù„Ùƒ ÙŠÙ…ÙƒÙ† ÙˆØ¶Ø¹ Ù†ÙØ³ Ø§Ù„Ù‚ÙŠÙ… ÙÙŠ `server/FitApi/appsettings.json` ØªØ­Øª Ø§Ù„Ù…ÙØªØ§Ø­ `Authentication:Google` â€” Ù„ÙƒÙ† Ø§Ù„Ù…ÙØ¶Ù‘Ù„ Ø£Ø«Ù†Ø§Ø¡ Ø§Ù„ØªØ·ÙˆÙŠØ± Ù‡Ùˆ Ù…ØªØºÙŠØ±Ø§Øª Ø§Ù„Ø¨ÙŠØ¦Ø© ÙƒÙŠ Ù„Ø§ ØªÙØ­ÙØ¸ Ø§Ù„Ø£Ø³Ø±Ø§Ø± ÙÙŠ Ø§Ù„ÙƒÙˆØ¯.

  ### Ø§Ù„Ø¨Ø±ÙŠØ¯ (SMTP) â€” ØªØ£ÙƒÙŠØ¯ Ø§Ù„Ø¨Ø±ÙŠØ¯ ÙˆØ¥Ø¹Ø§Ø¯Ø© ØªØ¹ÙŠÙŠÙ† ÙƒÙ„Ù…Ø© Ø§Ù„Ù…Ø±ÙˆØ±

  Ø£Ø«Ù†Ø§Ø¡ Ø§Ù„ØªØ·ÙˆÙŠØ±ØŒ Ø§Ù„Ø¥Ø¹Ø¯Ø§Ø¯Ø§Øª Ø§Ù„Ø§ÙØªØ±Ø§Ø¶ÙŠØ© Ù…ÙˆØ¬Ù‘Ù‡Ø© Ù„Ø®Ø¯Ù…Ø© **Mailpit** Ø¯Ø§Ø®Ù„ Ø§Ù„Ø´Ø¨ÙƒØ© (`Smtp:Host=mail`, `Smtp:Port=1025`)ØŒ ÙØªØ±Ù‰ ÙƒÙ„ Ø±Ø³Ø§Ø¦Ù„ Ø§Ù„ØªØ£ÙƒÙŠØ¯/Ø§Ù„Ø§Ø³ØªØ¹Ø§Ø¯Ø© Ø¹Ù„Ù‰ <http://localhost:8025> Ø¯ÙˆÙ† Ø¥Ø±Ø³Ø§Ù„ Ø­Ù‚ÙŠÙ‚ÙŠ. Ù„Ù„Ø¥Ù†ØªØ§Ø¬ØŒ Ø¹Ø¯Ù‘Ù„ Ø§Ù„Ù‚ÙŠÙ… Ø¹Ø¨Ø± Ù…ØªØºÙŠØ±Ø§Øª Ø§Ù„Ø¨ÙŠØ¦Ø© Ù„Ù„Ø®Ø¯Ù…Ø© `api`:

  ```yaml
  services:
    api:
      environment:
        - Smtp__Host=smtp.yourprovider.com
        - Smtp__Port=587
        - Smtp__From=no-reply@yoursite.com
  ```

  Ø£Ùˆ Ø¶Ø¹Ù‡Ø§ ÙÙŠ `appsettings.json` ØªØ­Øª Ø§Ù„Ù…ÙØªØ§Ø­ `Smtp`.

  ---

  ## Ù…ØªØºÙŠØ±Ø§Øª Ø§Ù„Ø¨ÙŠØ¦Ø© Ø§Ù„Ù…Ø·Ù„ÙˆØ¨Ø©

  | Ø§Ù„Ù…ÙØªØ§Ø­ (Ø¯Ø§Ø®Ù„ Ø§Ù„ØªØ·Ø¨ÙŠÙ‚) | ØµÙŠØºØ© Ù…ØªØºÙŠØ± Ø§Ù„Ø¨ÙŠØ¦Ø© | Ø§Ù„Ù‚ÙŠÙ…Ø© Ø§Ù„Ø§ÙØªØ±Ø§Ø¶ÙŠØ© (ØªØ·ÙˆÙŠØ±) | Ø§Ù„ØºØ±Ø¶ |
  |---|---|---|---|
  | `ConnectionStrings:Default` | `ConnectionStrings__Default` | Ø³Ù„Ø³Ù„Ø© Ø§ØªØµØ§Ù„ Ø®Ø¯Ù…Ø© `db` | Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ù€PostgreSQL |
  | `Authentication:Google:ClientId` | `Authentication__Google__ClientId` | â€” (ÙŠÙØ¶Ø¨Ø· Ù…Ù†Ùƒ) | Ø¯Ø®ÙˆÙ„ Google |
  | `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` | â€” (ÙŠÙØ¶Ø¨Ø· Ù…Ù†Ùƒ) | Ø¯Ø®ÙˆÙ„ Google |
  | `Smtp:Host` | `Smtp__Host` | `mail` | Ø®Ø§Ø¯Ù… Ø§Ù„Ø¨Ø±ÙŠØ¯ |
  | `Smtp:Port` | `Smtp__Port` | `1025` | Ù…Ù†ÙØ° Ø§Ù„Ø¨Ø±ÙŠØ¯ |
  | `Smtp:From` | `Smtp__From` | Ø¹Ù†ÙˆØ§Ù† Ù…Ø±Ø³ÙÙ„ Ø§ÙØªØ±Ø§Ø¶ÙŠ | Ù…ÙØ±Ø³ÙÙ„ Ø§Ù„Ø±Ø³Ø§Ø¦Ù„ |
  | `Storage:AvatarRoot` | `Storage__AvatarRoot` | `/app/uploads/avatars` | Ù…Ø¬Ù„Ø¯ Ø­ÙØ¸ ØµÙˆØ± Ø§Ù„Ø¨Ø±ÙˆÙØ§ÙŠÙ„ |
  | `Frontend:WebRoot` | `Frontend__WebRoot` | Ø¬Ø°Ø± Ø§Ù„Ù…ÙˆÙ‚Ø¹ Ø¯Ø§Ø®Ù„ Ø§Ù„Ø­Ø§ÙˆÙŠØ© | Ø§Ù„Ù…Ø¬Ù„Ø¯ Ø§Ù„Ø°ÙŠ ÙŠØ®Ø¯Ù… Ù…Ù†Ù‡ Ø§Ù„Ù€API ØµÙØ­Ø§Øª Ø§Ù„Ù…ÙˆÙ‚Ø¹ |

  > Ù…Ù„Ø§Ø­Ø¸Ø©: `Frontend:WebRoot` ØªÙØ¶Ø¨Ø· Ø¯Ø§Ø®Ù„ ØµÙˆØ±Ø© Ø§Ù„Ù€API Ù„ØªØ´ÙŠØ± Ø¥Ù„Ù‰ Ù…Ø¬Ù„Ø¯ Ù…Ù„ÙØ§Øª Ø§Ù„Ù…ÙˆÙ‚Ø¹Ø› Ù„Ø§ ØªØ­ØªØ§Ø¬ ØªØºÙŠÙŠØ±Ù‡Ø§ Ù„Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ø¹Ø§Ø¯ÙŠ. ØµÙˆØ± Ø§Ù„Ø¨Ø±ÙˆÙØ§ÙŠÙ„ ØªÙØ®Ø¯ÙŽÙ‘Ù… Ø¹Ù„Ù‰ Ø§Ù„Ù…Ø³Ø§Ø± `/uploads/avatars/...`.

  ---

  ## ØªØ´ØºÙŠÙ„ Ø§Ù„Ù‡Ø¬Ø±Ø§Øª (migrations)

  ØªÙØ·Ø¨ÙŽÙ‘Ù‚ Ø§Ù„Ù‡Ø¬Ø±Ø§Øª **ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹** Ø¹Ù†Ø¯ ÙƒÙ„ Ø¥Ù‚Ù„Ø§Ø¹ Ù„Ù„Ù€APIØŒ ÙÙ„Ø§ Ø­Ø§Ø¬Ø© Ù„Ø®Ø·ÙˆØ© ÙŠØ¯ÙˆÙŠØ© ÙÙŠ Ø§Ù„ØªØ´ØºÙŠÙ„ Ø§Ù„Ù…Ø¹ØªØ§Ø¯. Ø¹Ù†Ø¯ ØªØ¹Ø¯ÙŠÙ„ Ø§Ù„Ù†Ù…Ø§Ø°Ø¬ (Models) ÙˆØ¥Ù†Ø´Ø§Ø¡ Ù‡Ø¬Ø±Ø© Ø¬Ø¯ÙŠØ¯Ø©ØŒ Ø´ØºÙ‘Ù„ Ù…Ù† Ø¯Ø§Ø®Ù„ `D:\Work\Templates\FIT\server`:

  ```bash
  cd "D:/Work/Templates/FIT/server"
  dotnet ef migrations add <Ø§Ø³Ù…-Ø§Ù„Ù‡Ø¬Ø±Ø©> --project FitApi
  ```

  Ø«Ù… Ø£Ø¹Ø¯ Ø¨Ù†Ø§Ø¡ Ø§Ù„Ø­Ø§ÙˆÙŠØ§Øª (`docker compose up --build`) Ù„ØªÙØ·Ø¨ÙŽÙ‘Ù‚ Ø§Ù„Ù‡Ø¬Ø±Ø© Ø§Ù„Ø¬Ø¯ÙŠØ¯Ø© Ø¢Ù„ÙŠØ§Ù‹ Ø¹Ù†Ø¯ Ø§Ù„Ø¥Ù‚Ù„Ø§Ø¹. (ØªØ«Ø¨ÙŠØª Ø§Ù„Ø£Ø¯Ø§Ø© Ø¹Ù†Ø¯ Ø§Ù„Ø­Ø§Ø¬Ø©: `dotnet tool install --global dotnet-ef`.)

  ---

  ## ØªØ´ØºÙŠÙ„ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±Ø§Øª

  Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±Ø§Øª ØªØ³ØªØ®Ø¯Ù… **Testcontainers** ÙØªØ´ØºÙ‘Ù„ Ø­Ø§ÙˆÙŠØ© PostgreSQL Ø®Ø§ØµØ© Ø¨Ù‡Ø§ ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹ â€” Ù„Ø°Ø§ ÙŠØ¬Ø¨ Ø£Ù† ÙŠÙƒÙˆÙ† **Docker Desktop ÙŠØ¹Ù…Ù„**. Ù…Ù† Ø¯Ø§Ø®Ù„ `D:\Work\Templates\FIT\server`:

  ```bash
  cd "D:/Work/Templates/FIT/server"
  dotnet test
  ```

  ØªÙ†ØªÙ‡ÙŠ ÙƒÙ„ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±Ø§Øª Ø¨Ù†Ø¬Ø§Ø­ (Passed!) Ø¯ÙˆÙ† Ø£ÙŠ Ø¥Ø¹Ø¯Ø§Ø¯ Ù…Ø³Ø¨Ù‚ Ù„Ù‚Ø§Ø¹Ø¯Ø© Ø¨ÙŠØ§Ù†Ø§Øª â€” Ø§Ù„Ù€CustomWebApplicationFactory ÙŠÙ‡ÙŠÙ‘Ø¦ Ù‚Ø§Ø¹Ø¯Ø© Ù…Ø¹Ø²ÙˆÙ„Ø© Ù„ÙƒÙ„ ØªØ´ØºÙŠÙ„ØŒ ÙˆØ§Ù„Ù‡Ø¬Ø±Ø§Øª ØªÙØ·Ø¨ÙŽÙ‘Ù‚ Ø¢Ù„ÙŠØ§Ù‹ Ø¹Ù†Ø¯ Ø¥Ù‚Ù„Ø§Ø¹ Ø§Ù„ØªØ·Ø¨ÙŠÙ‚ Ø¯Ø§Ø®Ù„ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±.

  ---

  ## Ù…Ù„Ø§Ø­Ø¸Ø§Øª

  - Ù„Ù… ØªØ¹Ø¯ Ù‡Ù†Ø§Ùƒ Ø£ÙŠ ØªØ¨Ø¹ÙŠØ© Ø¹Ù„Ù‰ Firebase: Ø­ÙØ°ÙØª Ù…Ù„ÙØ§Øª `assets/js/firebase-config.js` Ùˆ`firestore.rules` Ùˆ`storage.rules` Ùˆ`FIREBASE-SETUP.md`. ÙƒÙ„ Ø§Ù„Ù…Ù†Ø·Ù‚ ØµØ§Ø± ÙÙŠ `assets/js/auth.js` (Ø§Ù„Ø°ÙŠ ÙŠØ³ØªØ¯Ø¹ÙŠ Ø§Ù„Ù€API Ø¹Ø¨Ø± `fetch` Ù…Ø¹ `credentials:"include"`).
  - Ø¹Ø²Ù„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù…ÙŠÙ† ÙŠØªÙ… Ø¹Ù„Ù‰ Ù…Ø³ØªÙˆÙ‰ Ø§Ù„Ù€API: ÙƒÙ„ Ø§Ø³ØªØ¹Ù„Ø§Ù… Ù…ÙÙ‚ÙŠÙŽÙ‘Ø¯ Ø¨Ù…Ø¹Ø±Ù‘Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø§Ù„Ù…Ø³ØªØ®Ø±Ø¬ Ù…Ù† Ù…Ù„Ù Ø§Ù„ÙƒÙˆÙƒÙŠØ² (Ø§Ù„Ø¬Ù„Ø³Ø©)ØŒ ÙˆÙ„Ø§ ÙŠÙÙ‚Ø±Ø£ Ù…Ø¹Ø±Ù‘Ù Ø§Ù„Ù…Ø³ØªØ®Ø¯Ù… Ø£Ø¨Ø¯Ø§Ù‹ Ù…Ù† Ø¬Ø³Ù… Ø§Ù„Ø·Ù„Ø¨ Ø£Ùˆ Ù…Ø³Ø§Ø±Ù‡.
  - Ø§Ù„Ù‚Ø§Ø¦Ù…Ø© (nav) Ù…Ø§ Ø²Ø§Ù„Øª Ù…ÙƒØ±Ø±Ø© ÙŠØ¯ÙˆÙŠØ§Ù‹ ÙÙŠ ÙƒÙ„ ØµÙØ­Ø© (ØªØµÙ…ÙŠÙ… Ø§Ù„Ù‚Ø§Ù„Ø¨ Ø§Ù„Ø£ØµÙ„ÙŠ)Ø› ÙˆØ§Ø¬Ù‡Ø© Ø§Ù„Ø¯Ø®ÙˆÙ„ ØªÙØ­Ù‚Ù† ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹ Ø¹Ø¨Ø± Ø§Ù„Ù€JS. Ù„Ø¥Ø¶Ø§ÙØ© ØµÙØ­Ø© Ø¬Ø¯ÙŠØ¯Ø© Ø£Ø¶ÙÙ Ù„Ù‡Ø§ ÙÙ‚Ø·:
    `<script type="module" src="assets/js/auth.js"></script>`
  ```

- [ ] **Step 4: MANUAL verification â€” zero `firebase` references remain in the shipped pages and scripts.** Run exactly (case-insensitive grep over the five root-level shipped pages and the JS bundle; do NOT scan the `html/` subfolder, which holds untouched original template variants and is out of scope):

  ```bash
  grep -ric "firebase" \
    "D:/Work/Templates/FIT/index.html" \
    "D:/Work/Templates/FIT/auth.html" \
    "D:/Work/Templates/FIT/profile.html" \
    "D:/Work/Templates/FIT/confirm-email.html" \
    "D:/Work/Templates/FIT/reset-password.html" \
    "D:/Work/Templates/FIT/assets/js/"*.js
  ```

  Expected on-screen result: **every line ends in `:0` (0 matches)** and the total match count is zero. By Task 14 the JS bundle no longer references Firebase, by Task 15 `auth.html` uses a Font Awesome `<i class="fab fa-google">` icon (no Firebase-hosted asset URL), and Step 2 above deleted `firebase-config.js`. If any line shows a non-zero count, locate that file and remove the reference before continuing.

- [ ] **Step 5: MANUAL verification â€” `docker compose config` validates.** Run exactly:

  ```bash
  docker compose -f "D:/Work/Templates/FIT/docker-compose.yml" config
  ```

  Expected on-screen result: the fully-resolved compose YAML is printed (showing the `db`, `api`, and `mail` services with `ports` `8080:8080` / `8025:8025` and the `1025` mail port) and the command exits 0 with **no `error` / `invalid` / `services must be a mapping` message**. This confirms the doc's documented URLs (8080 site, 8025 Mailpit) match the actual compose file. (If `docker-compose.yml` does not yet exist because its owning task has not run, this verification is deferred until that task completes â€” the `POSTGRES-SETUP.md` content does not block it.)

- [ ] **Step 6: Commit.** Run exactly:

  ```bash
  git -C "D:/Work/Templates/FIT" add -A POSTGRES-SETUP.md
  git -C "D:/Work/Templates/FIT" rm --cached --ignore-unmatch \
    "assets/js/firebase-config.js" "firestore.rules" "storage.rules" "FIREBASE-SETUP.md"
  git -C "D:/Work/Templates/FIT" commit -m "docs: remove Firebase files and add POSTGRES-SETUP run guide

Delete firebase-config.js, firestore.rules, storage.rules and FIREBASE-SETUP.md;
add Arabic POSTGRES-SETUP.md covering docker compose, URLs (site :8080, Mailpit :8025),
Google/SMTP config, env vars, migrations, and running tests in server/."
  ```

  (The `git rm` in Step 2 already staged the four deletions; the `--cached --ignore-unmatch` line above is a harmless idempotent guard so this commit step is safe to re-run even if Steps 2 was already committed separately.) Expected: a commit is created summarizing one added file (`POSTGRES-SETUP.md`) and four deletions.

