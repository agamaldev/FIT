<#
.SYNOPSIS
    One-time setup of the self-hosted GitHub Actions runner for agamaldev/FIT.

.DESCRIPTION
    Creates a dedicated NON-ADMINISTRATIVE local account for the runner service
    and grants it only the file permissions the deploy actually needs:

        C:\MMFit\_work      full   - the runner's own workspace (checkout, temp)
        C:\MMFit\staging    modify - deploy stages the payload here
        C:\MMFit\sites\Web  modify - the live site, target of robocopy /MIR
        C:\MMFit\backups    modify - pre-deploy backups
        C:\MMFit\actions-runner  read+execute

    It gets nothing else. It is not an administrator, it cannot touch the Healixa
    deployment, and it cannot reconfigure IIS.

    This is a SIMPLER security model than Healixa's runner, which additionally
    needs pre-registered scheduled tasks to start/stop its App Pool as SYSTEM.
    This site is static, so the deploy never touches an App Pool at all - plain
    file writes are the whole job.

    The account password is generated here, handed straight to the service
    registration, and never written to disk or printed. Windows stores it as an
    LSA secret for the service. If it is ever needed again, reset it rather than
    trying to recover it.

.PARAMETER Repo
    owner/name of the repository the runner serves.
#>
[CmdletBinding()]
param(
    [string]$Repo        = 'agamaldev/FIT',
    [string]$RunnerDir   = 'C:\MMFit\actions-runner',
    [string]$WorkDir     = 'C:\MMFit\_work',
    [string]$RunnerName  = 'mmfit-vps',
    [string]$Labels      = 'self-hosted,windows,mmfit-prod',
    [string]$AccountName = 'MMFitRunner',
    [string]$Gh          = 'C:\Program Files\GitHub CLI\gh.exe'
)

$ErrorActionPreference = 'Stop'

Write-Host '=== GitHub Actions runner setup ===' -ForegroundColor Cyan

# ------------------------------------------------------------------ account
$acct = Get-LocalUser -Name $AccountName -ErrorAction SilentlyContinue
if (-not $acct) {
    # 24 random bytes -> base64. Never printed, never persisted.
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $plain = [Convert]::ToBase64String($bytes) + '!aA1'
    $pw = ConvertTo-SecureString $plain -AsPlainText -Force

    New-LocalUser -Name $AccountName `
                  -Password $pw `
                  -Description 'Non-admin Actions runner acct (mmfit.space)' `
                  -PasswordNeverExpires `
                  -UserMayNotChangePassword | Out-Null
    Write-Host "  created local account $AccountName (non-admin)" -ForegroundColor Green
} else {
    throw "Account $AccountName already exists. Delete it first (Remove-LocalUser $AccountName) or pass -AccountName."
}

# Deliberately NOT added to Administrators. Stated explicitly because it is the
# single most important property of this setup.
Write-Host '  not a member of Administrators (by design)' -ForegroundColor Gray

# ------------------------------------------------------------------ file ACLs
function Grant-Path {
    param([string]$Path, [string]$Rights)
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path -Force | Out-Null }
    $acl  = Get-Acl $Path
    $rule = New-Object Security.AccessControl.FileSystemAccessRule(
        "$env:COMPUTERNAME\$AccountName", $Rights,
        'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($rule)
    Set-Acl -Path $Path -AclObject $acl
    Write-Host "  $Rights on $Path" -ForegroundColor Gray
}

Grant-Path -Path $WorkDir             -Rights 'FullControl'
Grant-Path -Path 'C:\MMFit\staging'   -Rights 'Modify'
Grant-Path -Path 'C:\MMFit\sites\Web' -Rights 'Modify'
Grant-Path -Path 'C:\MMFit\backups'   -Rights 'Modify'
Grant-Path -Path $RunnerDir           -Rights 'ReadAndExecute'

# The runner writes its own _diag logs and .runner/.credentials into RunnerDir.
Grant-Path -Path (Join-Path $RunnerDir '_diag') -Rights 'Modify'

# ------------------------------------------------------------------ registration token
# Short-lived (~1 hour) and single-use. Fetched at run time rather than stored.
Write-Host "  requesting registration token for $Repo" -ForegroundColor Gray
$token = (& $Gh api -X POST "repos/$Repo/actions/runners/registration-token" --jq '.token')
if ($LASTEXITCODE -ne 0 -or -not $token) { throw 'Failed to obtain a runner registration token' }

# ------------------------------------------------------------------ configure
Push-Location $RunnerDir
try {
    & .\config.cmd --unattended `
        --url "https://github.com/$Repo" `
        --token $token `
        --name $RunnerName `
        --labels $Labels `
        --work $WorkDir `
        --runasservice `
        --windowslogonaccount "$env:COMPUTERNAME\$AccountName" `
        --windowslogonpassword $plain `
        --replace
    if ($LASTEXITCODE -ne 0) { throw "config.cmd failed with $LASTEXITCODE" }
} finally {
    Pop-Location
}

# Recorded so the App Pool control grant can actually be VERIFIED (by running a
# scheduled task as this account) rather than merely assumed, and so the password
# is recoverable for support without resetting the account. Same
# Administrators-only file as the database credentials.
$secretFile = 'C:\MMFit\secrets\runner-credentials.txt'
$secretDir  = Split-Path -Parent $secretFile
if (-not (Test-Path $secretDir)) { New-Item -ItemType Directory -Path $secretDir -Force | Out-Null }
$acl = New-Object Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true, $false)
foreach ($id in @('BUILTIN\Administrators', 'NT AUTHORITY\SYSTEM')) {
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule(
        $id, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
}
Set-Acl -Path $secretDir -AclObject $acl
@(
    '# GitHub Actions runner service account for mmfit.space'
    '# Administrators only. NOT in git. Do not paste into chat or tickets.'
    ''
    "account  : $env:COMPUTERNAME\$AccountName"
    "password : $plain"
) | Set-Content -Path $secretFile -Encoding UTF8
Write-Host "  credentials recorded in $secretFile (Administrators only)" -ForegroundColor Gray

Remove-Variable plain -ErrorAction SilentlyContinue
[GC]::Collect()

Write-Host "`n=== Result ===" -ForegroundColor Cyan
Get-CimInstance Win32_Service |
    Where-Object { $_.Name -like 'actions.runner*FIT*' } |
    Select-Object Name, StartName, State | Format-List
