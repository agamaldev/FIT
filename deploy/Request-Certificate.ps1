<#
.SYNOPSIS
    Requests (or re-requests) the Let's Encrypt certificate for mmfit.space and
    installs it on the MMFit.Web IIS site.

.DESCRIPTION
    Uses win-acme, which is already installed on this server at C:\Healixa\win-acme
    and already owns a scheduled task ("win-acme renew ...") that renews EVERY
    registered renewal, including this one. So this script is run ONCE; renewal is
    automatic from then on.

    Validation is HTTP-01 via the filesystem plugin: win-acme writes a token to
    <webroot>\.well-known\acme-challenge\ and Let's Encrypt fetches it over port 80.
    Two things make that work on a static site, and both must stay in place:

      1. .well-known\acme-challenge\web.config maps extensionless files to
         text/plain. Without it IIS returns 404.3 for the token and issuance fails.
      2. Deploy-Site.ps1 excludes .well-known from robocopy /MIR, so neither the
         folder nor that web.config is deleted by a deploy.

    Run Test-AcmeChallenge.ps1 first if you want to confirm the challenge path is
    reachable before spending Let's Encrypt rate limit.

.PARAMETER Staging
    Use the Let's Encrypt STAGING environment. Issues an untrusted certificate but
    does not consume the production rate limit (5 duplicate certs per week). Use
    this when debugging validation problems.
#>
[CmdletBinding()]
param(
    [string]$Wacs        = 'C:\Healixa\win-acme\wacs.exe',
    [string]$WebRoot     = 'C:\MMFit\sites\Web',
    [string[]]$HostNames = @('mmfit.space', 'www.mmfit.space'),
    [int]$SiteId         = 4,
    [string]$EmailAddress = 'a.elgamal726@gmail.com',
    [switch]$Staging
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Wacs))    { throw "win-acme not found at $Wacs" }
if (-not (Test-Path $WebRoot)) { throw "Web root not found at $WebRoot" }

$challengeDir = Join-Path $WebRoot '.well-known\acme-challenge'
if (-not (Test-Path (Join-Path $challengeDir 'web.config'))) {
    throw "Missing $challengeDir\web.config - extensionless challenge tokens will 404. Create it before requesting a certificate."
}

$args = @(
    '--target', 'manual',
    '--host', ($HostNames -join ','),
    '--commonname', $HostNames[0],
    '--validation', 'filesystem',
    '--webroot', $WebRoot,
    '--store', 'certificatestore',
    '--certificatestore', 'WebHosting',
    '--installation', 'iis',
    '--installationsiteid', $SiteId,
    '--friendlyname', "[Manual] $($HostNames[0])",
    '--emailaddress', $EmailAddress,
    '--accepttos'
)
if ($Staging) { $args += @('--baseuri', 'https://acme-staging-v02.api.letsencrypt.org/') }

Write-Host "=== Requesting certificate for $($HostNames -join ', ') ===" -ForegroundColor Cyan
Write-Host "    webroot : $WebRoot"
Write-Host "    IIS site: $SiteId"
if ($Staging) { Write-Host '    MODE    : STAGING (untrusted cert, no rate limit)' -ForegroundColor Yellow }
Write-Host ''

& $Wacs @args
$code = $LASTEXITCODE

if ($code -ne 0) { throw "win-acme exited with code $code" }

Write-Host "`n=== Result ===" -ForegroundColor Cyan
Import-Module WebAdministration
Get-ChildItem Cert:\LocalMachine\WebHosting |
    Where-Object { $_.Subject -like "*$($HostNames[0])*" } |
    Select-Object Subject, NotAfter, Thumbprint | Format-Table -AutoSize
Get-WebBinding -Name (Get-Website | Where-Object { $_.ID -eq $SiteId }).Name |
    Select-Object protocol, bindingInformation | Format-Table -AutoSize
