param (
    [Parameter(Mandatory=$true)]
    [string]$PdfPath
)

# Replace backslashes with forward slashes for WSL
$wslPath = $PdfPath -replace '\\', '/'

# Convert drive letter (e.g. D:/ to /mnt/d/)
if ($wslPath -match '^([A-Za-z]):/(.*)') {
    $drive = $matches[1].ToLower()
    $rest = $matches[2]
    $wslPath = "/mnt/$drive/$rest"
}

# The known valid docling venv path in the Ubuntu distribution
$doclingCmd = "/home/pedro/docling-project/.venv/bin/docling"

Write-Host "Invoking docling inside Ubuntu WSL for: $wslPath"
# Execute docling, generate markdown in the same directory
wsl -d Ubuntu -e bash -c "$doclingCmd `"$wslPath`""

$mdPath = $PdfPath -replace '\.pdf$', '.md'
if (Test-Path $mdPath) {
    Write-Host "Success: Markdown generated at $mdPath"
    exit 0
} else {
    Write-Error "Failed to generate markdown."
    exit 1
}
