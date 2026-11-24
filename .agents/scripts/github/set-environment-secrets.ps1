<#
.SYNOPSIS
    Automates creating all required GitHub Environment Secrets for MelloSilveiraTools.WebApi.Host using the GitHub CLI (gh).
.PARAMETER Environment
    Target GitHub repository environment name (e.g., Development, Docker, Staging, Production).
.PARAMETER Repo
    Optional GitHub repository in 'owner/name' format. Defaults to the current repository detected by gh.
.PARAMETER SecretsFile
    Optional path to a JSON file matching User Secrets schema. Defaults to the local dotnet User Secrets file.
.EXAMPLE
    .\scripts\github\set-environment-secrets.ps1 -Environment Production
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Repo = "",

    [Parameter(Mandatory = $false)]
    [string]$SecretsFile = (Join-Path $env:APPDATA "Microsoft\UserSecrets\mellosilveiratools-webapi-host-quickstart\secrets.json")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI ('gh') was not found on PATH. Install it from https://cli.github.com/ and run 'gh auth login'."
}

$secretsData = $null
if (Test-Path $SecretsFile) {
    Write-Host "Loading secret values from '$SecretsFile'..." -ForegroundColor Cyan
    $secretsData = Get-Content -Path $SecretsFile -Raw | ConvertFrom-Json
} else {
    Write-Host "Secrets file '$SecretsFile' not found. You will be prompted interactively for each secret." -ForegroundColor Yellow
}

function Get-SecretValue {
    param(
        [string]$SecretName,
        [string]$JsonValue
    )

    if (-not [string]::IsNullOrWhiteSpace($JsonValue)) {
        return $JsonValue
    }

    $secure = Read-Host "Enter value for secret '$SecretName'" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    } finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

$secrets = [ordered]@{
    "DATABASE__CONNECTIONSTRING" = Get-SecretValue "DATABASE__CONNECTIONSTRING" ($secretsData.Database.ConnectionString)
    "JWT__ENCRYPTIONKEY"         = Get-SecretValue "JWT__ENCRYPTIONKEY" ($secretsData.Jwt.EncryptionKey)
    "JWT__SIGNINGKEY"            = Get-SecretValue "JWT__SIGNINGKEY" ($secretsData.Jwt.SigningKey)
}

if ([System.Text.Encoding]::UTF8.GetByteCount($secrets["JWT__ENCRYPTIONKEY"]) -ne 32) {
    throw "JWT__ENCRYPTIONKEY must be exactly 32 UTF-8 bytes for AES-256 KW."
}

$repoArgs = @()
if (-not [string]::IsNullOrWhiteSpace($Repo)) {
    $repoArgs = @("--repo", $Repo)
}

foreach ($entry in $secrets.GetEnumerator()) {
    Write-Host "Setting secret '$($entry.Key)' in environment '$Environment'..." -ForegroundColor Green
    & gh secret set $entry.Key --env $Environment --body $entry.Value @repoArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set secret '$($entry.Key)' in environment '$Environment'."
    }
}

Write-Host "All secrets successfully configured for environment '$Environment'." -ForegroundColor Cyan
