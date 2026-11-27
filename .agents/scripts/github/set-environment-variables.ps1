<#
.SYNOPSIS
    Automates creating all non-secret GitHub Environment Variables for MelloSilveiraTools.WebApi.Host from appsettings*.json using the GitHub CLI (gh).
.PARAMETER Environment
    Target GitHub repository environment name (e.g., Development, Docker, Staging, Production).
.PARAMETER Repo
    Optional GitHub repository in 'owner/name' format. Defaults to the current repository detected by gh.
.PARAMETER AppSettingsFile
    Path to the source appsettings JSON file. Defaults to src/MelloSilveiraTools.WebApi.Host/appsettings.Docker.json.
.EXAMPLE
    .\scripts\github\set-environment-variables.ps1 -Environment Docker
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Repo = "",

    [Parameter(Mandatory = $false)]
    [string]$AppSettingsFile = "src/MelloSilveiraTools.WebApi.Host/appsettings.Docker.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI ('gh') was not found on PATH. Install it from https://cli.github.com/ and run 'gh auth login'."
}

if (-not (Test-Path $AppSettingsFile)) {
    throw "AppSettings file '$AppSettingsFile' was not found."
}

$secretKeys = @(
    "DATABASE__CONNECTIONSTRING",
    "JWT__ENCRYPTIONKEY",
    "JWT__SIGNINGKEY"
)

$variables = [ordered]@{}

function Add-FlattenedProperties {
    param(
        [Parameter(Mandatory = $true)] $Node,
        [string]$Prefix = ""
    )

    foreach ($prop in $Node.PSObject.Properties) {
        $key = if ([string]::IsNullOrEmpty($Prefix)) { $prop.Name } else { "${Prefix}__$($prop.Name)" }
        $upperKey = $key.ToUpperInvariant()

        if ($upperKey.StartsWith("LOGGING__") -or $upperKey -eq "LOGGING") {
            continue
        }

        if ($secretKeys -contains $upperKey) {
            continue
        }

        if ($null -eq $prop.Value) {
            continue
        }

        if ($prop.Value -is [System.Management.Automation.PSCustomObject]) {
            Add-FlattenedProperties -Node $prop.Value -Prefix $key
        } else {
            $variables[$upperKey] = [string]$prop.Value
        }
    }
}

$json = Get-Content -Path $AppSettingsFile -Raw | ConvertFrom-Json
Add-FlattenedProperties -Node $json

$repoArgs = @()
if (-not [string]::IsNullOrWhiteSpace($Repo)) {
    $repoArgs = @("--repo", $Repo)
}

foreach ($entry in $variables.GetEnumerator()) {
    Write-Host "Setting variable '$($entry.Key)' = '$($entry.Value)' in environment '$Environment'..." -ForegroundColor Green
    & gh variable set $entry.Key --env $Environment --body $entry.Value @repoArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set variable '$($entry.Key)' in environment '$Environment'."
    }
}

Write-Host "All $($variables.Count) environment variables successfully configured for environment '$Environment'." -ForegroundColor Cyan
