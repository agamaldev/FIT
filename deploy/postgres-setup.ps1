<#
.SYNOPSIS
    One-time PostgreSQL install + database/role creation for the FIT API.

.DESCRIPTION
    Installs PostgreSQL 16 as a Windows service and creates the application
    database and a NON-SUPERUSER role that owns it. The API connects as that
    role, never as `postgres`.

    Passwords are generated here, written once to a file readable only by
    Administrators, and never printed to the console or committed. The
    installer receives them through an option file rather than the command
    line, so they do not appear in the process list.

    The API applies its own EF Core migrations at startup
    (AppDbContext.Database.Migrate() in Program.cs), so no schema step is
    needed here - only the empty database and its owner.

    NOTE this differs from the Healixa deployment, where the database is
    strictly manual and the pipeline never touches it. Here the application
    itself migrates on boot; that is this app's design, not a choice made by
    the pipeline. It does mean a bad migration ships with a bad deploy, so the
    rollback path matters more, not less.

.PARAMETER Installer
    Path to the EnterpriseDB PostgreSQL Windows installer.
#>
[CmdletBinding()]
param(
    [string]$Installer  = 'C:\MMFit\installers\postgresql-x64.exe',
    [string]$Prefix     = 'C:\Program Files\PostgreSQL\16',
    [string]$DataDir    = 'C:\PostgreSQL\16\data',
    [int]$Port          = 5432,
    [string]$DbName     = 'fit',
    [string]$DbUser     = 'fit',
    [string]$SecretFile = 'C:\MMFit\secrets\postgres-credentials.txt'
)

$ErrorActionPreference = 'Stop'

function New-Password {
    # 24 random bytes -> base64, stripped of characters that need escaping in a
    # libpq connection string or a shell.
    $b = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    ([Convert]::ToBase64String($b) -replace '[+/=]', '') + 'aA1'
}

Write-Host '=== PostgreSQL setup ===' -ForegroundColor Cyan

$svc = Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue
if ($svc) {
    Write-Host "  PostgreSQL service already present: $($svc.Name) [$($svc.Status)]" -ForegroundColor Yellow
} else {
    if (-not (Test-Path $Installer)) { throw "Installer not found: $Installer" }

    $superPw = New-Password

    # Option file keeps the password out of the process command line.
    $optFile = Join-Path $env:TEMP 'pg-opts.ini'
    @(
        'mode=unattended'
        'unattendedmodeui=none'
        "prefix=$Prefix"
        "datadir=$DataDir"
        "serverport=$Port"
        "superpassword=$superPw"
        # pgAdmin bundles an entire private Python runtime (boto3/botocore alone is
        # thousands of tiny files) and pushed a first install on this box past 25
        # minutes. It is a GUI admin tool - useless on a headless server, where
        # psql does the job. Stack Builder is an installer for optional add-ons and
        # is equally unwanted.
        'disable-components=pgAdmin,stackbuilder'
    ) | Set-Content -Path $optFile -Encoding ASCII

    try {
        Write-Host '  installing (this takes a few minutes)...' -ForegroundColor Gray
        $p = Start-Process -FilePath $Installer -ArgumentList "--optionfile `"$optFile`"" -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "PostgreSQL installer exited with $($p.ExitCode)" }
    } finally {
        Remove-Item $optFile -Force -ErrorAction SilentlyContinue
    }
    Write-Host '  installed' -ForegroundColor Green

    $script:SuperPassword = $superPw
}

$svc = Get-Service -Name 'postgresql*' -ErrorAction Stop
if ($svc.Status -ne 'Running') { Start-Service $svc.Name }
Write-Host "  service $($svc.Name) is $((Get-Service $svc.Name).Status)" -ForegroundColor Gray

# ------------------------------------------------------------------ database + role
$psql = Join-Path $Prefix 'bin\psql.exe'
if (-not (Test-Path $psql)) { throw "psql not found at $psql" }

if (-not $script:SuperPassword) {
    throw "PostgreSQL was already installed, so the superuser password is unknown to this script. Create the database and role by hand, or pass the password in."
}

$appPw = New-Password
$env:PGPASSWORD = $script:SuperPassword

# Role first, then a database it owns. Created idempotently so a re-run is safe.
$sql = @"
DO `$`$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$DbUser') THEN
      CREATE ROLE $DbUser LOGIN PASSWORD '$appPw';
   ELSE
      ALTER ROLE $DbUser LOGIN PASSWORD '$appPw';
   END IF;
END
`$`$;
"@

$sql | & $psql -U postgres -h 127.0.0.1 -p $Port -d postgres -v ON_ERROR_STOP=1 -f -
if ($LASTEXITCODE -ne 0) { throw "Failed to create role $DbUser" }
Write-Host "  role $DbUser ready" -ForegroundColor Green

$exists = & $psql -U postgres -h 127.0.0.1 -p $Port -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName'"
if ($exists -ne '1') {
    & $psql -U postgres -h 127.0.0.1 -p $Port -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $DbName OWNER $DbUser ENCODING 'UTF8'"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create database $DbName" }
    Write-Host "  database $DbName created (owner $DbUser)" -ForegroundColor Green
} else {
    Write-Host "  database $DbName already exists" -ForegroundColor Yellow
}

$env:PGPASSWORD = $null

# ------------------------------------------------------------------ record secrets
$secretDir = Split-Path -Parent $SecretFile
if (-not (Test-Path $secretDir)) { New-Item -ItemType Directory -Path $secretDir -Force | Out-Null }

# Administrators + SYSTEM only. Inheritance off so it does not pick up looser
# permissions from the parent.
$acl = New-Object Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true, $false)
foreach ($id in @('BUILTIN\Administrators', 'NT AUTHORITY\SYSTEM')) {
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule(
        $id, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
}
Set-Acl -Path $secretDir -AclObject $acl

@(
    '# PostgreSQL credentials for mmfit.space - generated by deploy/postgres-setup.ps1'
    '# Readable by Administrators only. NOT in git. Do not paste into chat or tickets.'
    ''
    "postgres superuser : $script:SuperPassword"
    "app role           : $DbUser"
    "app password       : $appPw"
    "database           : $DbName"
    "port               : $Port"
    ''
    'Connection string for appsettings.Production.json:'
    "Host=127.0.0.1;Port=$Port;Database=$DbName;Username=$DbUser;Password=$appPw"
) | Set-Content -Path $SecretFile -Encoding UTF8

Write-Host "`n  credentials written to $SecretFile (Administrators only)" -ForegroundColor Green
Write-Host '  the connection string for appsettings.Production.json is in that file' -ForegroundColor Gray
Write-Host "`n=== Done ===" -ForegroundColor Cyan
