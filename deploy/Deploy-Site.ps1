<#
.SYNOPSIS
    Deploys the FIT API (which also serves the MAKE ME FIT frontend) to IIS,
    with backup, offline window, mirrored copy, health check and automatic
    rollback.

.DESCRIPTION
    Standalone by design: this script IS the deployment logic. The GitHub Actions
    workflow builds, tests and publishes, then calls this. That means a deploy -
    or more importantly a ROLLBACK - can always be performed by hand on the
    server, with no dependency on GitHub being reachable.

    Sequence:
        [1] publish    - dotnet publish + stage the frontend   (only with -Publish)
        [2] pre-flight - verify staging really contains a usable app
            vvv only from here do we touch production vvv
        [3] backup     - sites\Web -> backups\Web\<timestamp>   (last 5 kept)
        [4] offline    - drop app_offline.htm, stop the App Pool
        [5] copy       - robocopy /MIR staging -> sites, with exclusions
        [6] online     - start the App Pool, remove app_offline.htm
        [7] health     - GET /api/health, N attempts
        [8] rollback   - on health failure, restore [3] and exit non-zero

    Steps 1-3 are safe: the live site keeps serving throughout. Actual downtime is
    steps 4-6 only.

    WHY THE APP POOL MUST STOP HERE
    An earlier version of this site was pure static HTML and needed no offline
    window at all. It is now an ASP.NET Core application: the worker process holds
    FitApi.dll and its dependencies locked, so they cannot be overwritten while it
    runs. The offline window is mandatory now, not optional.

    THE DATABASE
    Unlike the Healixa pipeline - where the database is strictly manual and the
    pipeline never touches it - this application applies its OWN EF Core migrations
    at startup (AppDbContext.Database.Migrate() in Program.cs). That is the app's
    design, not a choice made here, but it has a consequence worth stating plainly:
    a bad migration ships with a bad deploy, and rolling the FILES back does NOT
    roll the schema back. If a deploy fails after the app has booted once, check
    the database state before re-deploying.

.PARAMETER Publish
    Build and publish from source into staging before deploying. Used for manual
    deploys. The workflow publishes in its own step and omits this.

.PARAMETER DryRun
    Runs robocopy in /L (list-only) mode. Nothing is copied or deleted, the App
    Pool is not stopped and no backup is taken.

.PARAMETER Rollback
    Skips build/staging entirely and restores the most recent backup (or the one
    named by -BackupName).

.EXAMPLE
    .\Deploy-Site.ps1 -Publish
    Full manual deploy from the current working tree.

.EXAMPLE
    .\Deploy-Site.ps1 -DryRun
    Show what would change, touching nothing.

.EXAMPLE
    .\Deploy-Site.ps1 -Rollback
    Restore the most recent backup immediately.
#>
[CmdletBinding()]
param(
    [string]$Root = 'C:\MMFit',
    [string]$SourcePath,
    [switch]$Publish,
    # The app runs EF migrations and builds its Identity/EF model on first request,
    # so the first hit after a restart is much slower than steady state. Do not
    # shrink this to "make deploys feel faster" - a short window rolls back a
    # perfectly healthy release.
    [int]$HealthAttempts = 24,
    [int]$HealthDelaySeconds = 5,
    [int]$HealthTimeoutSeconds = 30,
    [int]$KeepBackups = 5,
    [switch]$DryRun,
    [switch]$Rollback,
    [string]$BackupName,
    [switch]$SkipHealthCheck
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ---------------------------------------------------------------- configuration
$Staging    = Join-Path $Root 'staging\Web'
$SitePath   = Join-Path $Root 'sites\Web'
$BackupRoot = Join-Path $Root 'backups\Web'
$AppPool    = 'MMFitWeb'
$StopTask   = 'MMFit-StopPool-Web'
$StartTask  = 'MMFit-StartPool-Web'
$Offline    = Join-Path $SitePath 'app_offline.htm'

# Directories that live ONLY on the server and must survive robocopy /MIR:
#   uploads    - user-uploaded profile avatars (Storage:AvatarRoot defaults to
#                <ContentRoot>\uploads\avatars). Deleting these is unrecoverable.
#   logs       - ASP.NET Core Module stdout logs, written live by the worker.
#   .well-known- Let's Encrypt HTTP-01 challenge root. Deleting it does not break
#                the site today, it breaks certificate RENEWAL ~60 days later.
$ExcludeDirs = @('uploads', 'logs', '.well-known')

#   appsettings.Production.json - connection string, Google OAuth secret, SMTP
#                                 password. Lives only on the server, never in git.
#   app_offline.htm             - managed by this script itself.
$ExcludeFiles = @('appsettings.Production.json', 'app_offline.htm')

# Files that prove the publish actually produced a usable app rather than an
# empty or half-copied folder.
$Preflight = @('FitApi.dll', 'web.config', 'wwwroot\index.html', 'wwwroot\assets\css\style.css')

# The API's own health endpoint. 127.0.0.1 with an explicit Host header is
# required: the site is bound by host header, so a plain GET to localhost would
# hit Healixa's catch-all binding instead.
$HealthUrl    = 'http://127.0.0.1/api/health'
$HealthHost   = 'mmfit.space'
$HealthMarker = '"status":"ok"'

# ---------------------------------------------------------------- helpers
function Write-Step { param([string]$m) Write-Host "`n=== $m ===" -ForegroundColor Cyan }
function Write-Ok   { param([string]$m) Write-Host "    $m" -ForegroundColor Green }
function Write-Info { param([string]$m) Write-Host "    $m" -ForegroundColor Gray }
function Write-Warn { param([string]$m) Write-Host "    $m" -ForegroundColor Yellow }

function Get-DotnetPath {
    <#  Resolve dotnet explicitly rather than trusting PATH.

        A Windows service does not inherit an interactive shell's PATH, and this
        script runs both from a service (the GitHub Actions runner) and by hand.
        Observed on this box 2026-07-29: `dotnet` resolved fine for an interactive
        admin but not in the non-interactive shell, failing the publish with
        "The term 'dotnet' is not recognized". #>
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in @("$env:ProgramFiles\dotnet\dotnet.exe", "${env:ProgramFiles(x86)}\dotnet\dotnet.exe")) {
        if (Test-Path $p) { return $p }
    }
    throw 'dotnet was not found on PATH or in the standard install locations.'
}

function Test-IsAdministrator {
    ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-PoolState {
    param([string]$Name)
    $out = & "$env:SystemRoot\System32\inetsrv\appcmd.exe" list apppool $Name 2>$null
    if ($out -match 'state:(\w+)') { return $Matches[1] }
    return 'Unknown'
}

function Set-PoolState {
    <#  Drives the App Pool through SYSTEM scheduled tasks so a NON-ADMIN runner
        can do it. schtasks /run returns immediately, so poll for the state to
        actually settle rather than trusting the exit code. #>
    param(
        [ValidateSet('Started', 'Stopped')][string]$Desired,
        [int]$TimeoutSeconds = 120
    )

    $taskName = if ($Desired -eq 'Stopped') { $StopTask } else { $StartTask }

    if ((Get-PoolState -Name $AppPool) -eq $Desired) {
        Write-Info "App Pool '$AppPool' already $Desired"
        return
    }

    if ($null -ne (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue)) {
        Write-Info "triggering scheduled task '$taskName'"
        & schtasks /run /tn $taskName | Out-Null
    }
    elseif (Test-IsAdministrator) {
        Write-Warn "task '$taskName' not found; falling back to direct control (running as admin)"
        $verb = if ($Desired -eq 'Started') { 'start' } else { 'stop' }
        & "$env:SystemRoot\System32\inetsrv\appcmd.exe" $verb apppool $AppPool | Out-Null
    }
    else {
        throw "Cannot set App Pool '$AppPool' to $Desired - scheduled task '$taskName' is missing and this account is not an administrator."
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 700
        if ((Get-PoolState -Name $AppPool) -eq $Desired) { Write-Ok "App Pool '$AppPool' is $Desired"; return }
    }

    # A pool wedged in 'Stopping' will never settle on its own: the worker is not
    # responding to the shutdown signal. Killing it is safe here because
    # app_offline.htm is already in place, so it is serving nothing, and leaving it
    # wedged would block the deploy AND the rollback.
    $state = Get-PoolState -Name $AppPool
    if ($Desired -eq 'Stopped' -and $state -eq 'Stopping') {
        Write-Warn "App Pool '$AppPool' wedged in 'Stopping' after ${TimeoutSeconds}s - terminating its worker"
        Get-CimInstance Win32_Process -Filter "Name='w3wp.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match [regex]::Escape($AppPool) } |
            ForEach-Object { Write-Info "killing w3wp PID $($_.ProcessId)"; Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 5
        if ((Get-PoolState -Name $AppPool) -eq 'Stopped') { Write-Ok "App Pool '$AppPool' is Stopped"; return }
    }

    throw "Timed out after ${TimeoutSeconds}s waiting for App Pool '$AppPool' to reach '$Desired' (currently '$state')."
}

function Get-RobocopyArgs {
    <#  Exclusions are emitted TWICE: once as a full destination path and once as
        a bare name.

        A full path alone is NOT enough. With only '/XD C:\...\sites\Web\uploads',
        robocopy excludes 'uploads' itself but still descends into it and marks the
        CHILD 'uploads\avatars' as an extra to delete, because that child's path
        does not match the exclusion string. That would delete every user's
        uploaded avatar.

        Bare names match at ANY depth, which is exactly what is wanted: anything
        named uploads / logs / .well-known must survive wherever it sits. The full
        path is kept as a second, explicit guard that documents intent. #>
    param([string]$Source, [string]$Destination, [switch]$ListOnly)

    $a = @($Source, $Destination, '/MIR', '/R:3', '/W:2', '/NP', '/NDL', '/NJH')

    if ($ExcludeDirs.Count -gt 0) {
        $a += '/XD'
        foreach ($d in $ExcludeDirs) { $a += (Join-Path $Destination $d); $a += (Split-Path $d -Leaf) }
    }
    if ($ExcludeFiles.Count -gt 0) {
        $a += '/XF'
        foreach ($f in $ExcludeFiles) { $a += (Join-Path $Destination $f); $a += (Split-Path $f -Leaf) }
    }
    if ($ListOnly) { $a += '/L' }
    return $a
}

function Invoke-Robocopy {
    <#  Robocopy's exit code is a bit field, not a status:
            0 nothing to do  1 copied  2 extras  4 mismatches  8 FAILURES  16 fatal
        Anything < 8 is success. Treating it as a normal exit code would fail every
        deploy that actually copied something. #>
    param([string[]]$Arguments, [string]$Label = 'copy')
    Write-Info "robocopy $($Arguments -join ' ')"
    $out = & robocopy.exe @Arguments
    $code = $LASTEXITCODE
    $out | Select-Object -Last 10 | ForEach-Object { Write-Host "      $_" -ForegroundColor DarkGray }
    if ($code -ge 8) { throw "$Label failed - robocopy exit code $code" }
    Write-Ok "$Label ok (robocopy exit $code)"
}

function Test-SiteHealth {
    param([int]$Attempts, [int]$DelaySeconds, [int]$TimeoutSeconds)
    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            $r = Invoke-WebRequest -Uri $HealthUrl -Headers @{ Host = $HealthHost } `
                                   -UseBasicParsing -TimeoutSec $TimeoutSeconds
            if ($r.StatusCode -eq 200 -and ($r.Content -replace '\s', '') -match [regex]::Escape($HealthMarker)) {
                Write-Ok "health ok on attempt $i (HTTP $($r.StatusCode))"
                return $true
            }
            Write-Warn "attempt $i - HTTP $($r.StatusCode) but body was: $($r.Content)"
        } catch {
            Write-Warn "attempt $i - $($_.Exception.Message)"
        }
        if ($i -lt $Attempts) { Start-Sleep -Seconds $DelaySeconds }
    }
    return $false
}

function Restore-Backup {
    param([string]$From)
    if (-not (Test-Path $From)) { throw "Backup folder not found: $From" }
    Write-Step "ROLLBACK - restoring $From"

    New-Item -ItemType File -Path $Offline -Force `
             -Value '<html><body><h1>MAKE ME FIT - restoring previous release</h1></body></html>' | Out-Null
    Set-PoolState -Desired 'Stopped'

    Invoke-Robocopy -Label 'rollback' -Arguments (Get-RobocopyArgs -Source $From -Destination $SitePath)

    Set-PoolState -Desired 'Started'
    Remove-Item $Offline -Force -ErrorAction SilentlyContinue
    Write-Ok 'rollback copy complete'
}

# ================================================================== ROLLBACK MODE
if ($Rollback) {
    if (-not (Test-Path $BackupRoot)) { throw "No backup root at $BackupRoot" }
    $target = if ($BackupName) { Join-Path $BackupRoot $BackupName }
              else {
                  $newest = Get-ChildItem $BackupRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
                  if (-not $newest) { throw "No backups found under $BackupRoot" }
                  $newest.FullName
              }

    Restore-Backup -From $target

    if (-not $SkipHealthCheck) {
        if (-not (Test-SiteHealth -Attempts $HealthAttempts -DelaySeconds $HealthDelaySeconds -TimeoutSeconds $HealthTimeoutSeconds)) {
            throw "Site is STILL unhealthy after restoring $target - manual intervention required"
        }
    }
    Write-Host "`nRollback complete: $target" -ForegroundColor Green
    exit 0
}

# ================================================================== [1] PUBLISH
if (-not $SourcePath) { $SourcePath = Split-Path -Parent $PSScriptRoot }

if ($Publish) {
    if (-not (Test-Path $SourcePath)) { throw "SourcePath not found: $SourcePath" }
    Write-Step "[1] Publish from $SourcePath"

    # Clean staging first: stale files from a previous publish would otherwise be
    # mirrored into production by /MIR.
    if (Test-Path $Staging) { Remove-Item "$Staging\*" -Recurse -Force } else { New-Item -ItemType Directory -Path $Staging -Force | Out-Null }

    $proj = Join-Path $SourcePath 'server\FitApi\FitApi.csproj'
    if (-not (Test-Path $proj)) { throw "Project not found: $proj" }

    $dotnet = Get-DotnetPath
    Write-Info "using $dotnet"
    & $dotnet publish $proj -c Release -o $Staging --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with $LASTEXITCODE" }
    Write-Ok 'API published'

    # `dotnet publish` generates web.config with stdoutLogEnabled="false". When the
    # app then fails to start, ANCM returns a bare HTTP 500.30 and writes NOTHING
    # anywhere - no stdout file, and on this box not even an Application event log
    # entry. Diagnosing the first failed deploy here meant running the published
    # DLL by hand to see the exception at all.
    #
    # Turning the log on costs one small file per worker start and turns that blind
    # failure into a readable stack trace in .\logs\. Patched here rather than in
    # the csproj because publish regenerates this file every time, so the setting
    # has to be re-applied on every deploy regardless of where it is declared.
    $wc = Join-Path $Staging 'web.config'
    if (Test-Path $wc) {
        [xml]$x = Get-Content $wc
        $node = $x.SelectSingleNode('//aspNetCore')
        if ($node) {
            $node.SetAttribute('stdoutLogEnabled', 'true')
            $x.Save($wc)
            Write-Ok 'web.config: stdout logging enabled'
        }
    }

    # The frontend is served by the API from <ContentRoot>\wwwroot (Program.cs
    # falls back to wwwroot when Frontend:WebRoot is unset). This mirrors what the
    # Dockerfile does - EXCEPT the Dockerfile also copies html\, the 160 MB
    # purchased Gymort template. No page references it, so it is deliberately left
    # out here. Source\ (53 MB of PDFs) is likewise not referenced and not shipped.
    $wwwroot = Join-Path $Staging 'wwwroot'
    New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null

    $pages = Get-ChildItem -Path $SourcePath -Filter '*.html' -File
    if (-not $pages) { throw "No .html pages found in $SourcePath" }
    $pages | Copy-Item -Destination $wwwroot -Force
    Write-Ok "staged $($pages.Count) page(s)"

    $assetsSrc = Join-Path $SourcePath 'assets'
    if (-not (Test-Path $assetsSrc)) { throw "assets\ not found in $SourcePath" }
    Invoke-Robocopy -Label 'stage assets' -Arguments @(
        $assetsSrc, (Join-Path $wwwroot 'assets'), '/MIR', '/NFL', '/NDL', '/NJH', '/NP', '/R:2', '/W:2'
    )
}

# ================================================================== [2] PRE-FLIGHT
Write-Step '[2] Pre-flight'
if (-not (Test-Path $Staging)) { throw "Staging folder does not exist: $Staging. Run with -Publish first." }

$missing = @()
foreach ($f in $Preflight) {
    if (Test-Path (Join-Path $Staging $f)) { Write-Ok "present  $f" } else { Write-Warn "MISSING  $f"; $missing += $f }
}
if ($missing.Count -gt 0) { throw "Pre-flight failed - staging is not a usable app. Missing: $($missing -join ', ')" }

$n  = (Get-ChildItem $Staging -Recurse -File | Measure-Object).Count
$mb = [math]::Round(((Get-ChildItem $Staging -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Info "staging holds $n files, $mb MB"

# ================================================================== DRY RUN
if ($DryRun) {
    Write-Step '[3-5] DRY RUN - listing what a real deploy would change'
    Write-Info 'no backup taken, App Pool not stopped, nothing copied or deleted'
    & robocopy.exe @(Get-RobocopyArgs -Source $Staging -Destination $SitePath -ListOnly)
    Write-Host "`nDry run complete (robocopy exit $LASTEXITCODE). Nothing was changed." -ForegroundColor Yellow
    exit 0
}

# ================================================================== [3] BACKUP
Write-Step '[3] Backup live site'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = Join-Path $BackupRoot $stamp

$liveFiles = if (Test-Path $SitePath) { (Get-ChildItem $SitePath -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count } else { 0 }
if ($liveFiles -eq 0) {
    Write-Warn 'live site is empty - first deploy, no backup to take'
    $backupPath = $null
} else {
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    Invoke-Robocopy -Label 'backup' -Arguments @(
        $SitePath, $backupPath, '/MIR', '/R:2', '/W:2', '/NP', '/NDL', '/NJH', '/NFL'
    )
    Write-Ok "backup -> $backupPath"

    Get-ChildItem $BackupRoot -Directory | Sort-Object Name -Descending | Select-Object -Skip $KeepBackups |
        ForEach-Object { Write-Info "pruning old backup $($_.Name)"; Remove-Item $_.FullName -Recurse -Force }
}

# ============================================================ [4]-[6] THE SWAP
try {
    Write-Step '[4] Going offline'
    New-Item -ItemType File -Path $Offline -Force `
             -Value '<html><body><h1>MAKE ME FIT - deploying</h1></body></html>' | Out-Null
    Write-Ok 'app_offline.htm in place'
    Set-PoolState -Desired 'Stopped'

    Write-Step '[5] Mirror staging -> live site'
    Invoke-Robocopy -Label 'deploy' -Arguments (Get-RobocopyArgs -Source $Staging -Destination $SitePath)

    Write-Step '[6] Coming back online'
    Set-PoolState -Desired 'Started'
    Remove-Item $Offline -Force -ErrorAction SilentlyContinue
    Write-Ok 'app_offline.htm removed'
}
catch {
    # Never leave the site offline because the swap threw. Put it back up on the
    # previous release if we can, then rethrow.
    Write-Warn "deploy step failed: $($_.Exception.Message)"
    if ($backupPath) { try { Restore-Backup -From $backupPath } catch { Write-Warn "rollback also failed: $($_.Exception.Message)" } }
    else { Set-PoolState -Desired 'Started'; Remove-Item $Offline -Force -ErrorAction SilentlyContinue }
    throw
}

# ================================================================== [7] HEALTH
if ($SkipHealthCheck) {
    Write-Step '[7] Health check SKIPPED by request'
    Write-Host "`nDeploy complete (health check skipped)." -ForegroundColor Green
    exit 0
}

Write-Step '[7] Health check'
if (Test-SiteHealth -Attempts $HealthAttempts -DelaySeconds $HealthDelaySeconds -TimeoutSeconds $HealthTimeoutSeconds) {
    Write-Host "`nDeploy complete and healthy." -ForegroundColor Green
    if ($backupPath) { Write-Info "rollback if needed:  .\Deploy-Site.ps1 -Rollback -BackupName $stamp" }
    exit 0
}

# ================================================================== [8] ROLLBACK
Write-Step '[8] Health check FAILED'
if (-not $backupPath) {
    throw "Site is unhealthy and there is no backup to roll back to (this was the first deploy). Left as-is for inspection at $SitePath"
}

Restore-Backup -From $backupPath

if (Test-SiteHealth -Attempts $HealthAttempts -DelaySeconds $HealthDelaySeconds -TimeoutSeconds $HealthTimeoutSeconds) {
    throw "Deploy failed its health check and was ROLLED BACK to $stamp. The site is healthy again on the previous release. NOTE: any EF migration this release applied is still applied - check the database."
}
throw "Deploy failed its health check AND the rollback to $stamp is also unhealthy - manual intervention required."
