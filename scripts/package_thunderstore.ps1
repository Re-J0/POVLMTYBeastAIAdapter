param(
    [string]$Configuration = "Release",
    [string]$DllPath = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Version = "0.3.2"

if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $RepoRoot "bin/$Configuration/netstandard2.1/POVLMTYBeastAIAdapter.dll"
}

if (-not (Test-Path $DllPath)) {
    throw "DLL not found: $DllPath`nBuild the project first, or pass -DllPath <path>."
}

$Stage = Join-Path $RepoRoot "artifacts/thunderstore"
$ZipPath = Join-Path $RepoRoot "artifacts/POVLMTYBeastAIAdapter-$Version-Thunderstore.zip"

if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $Stage "plugins/POVLMTYBeastAIAdapter") -Force | Out-Null

Copy-Item (Join-Path $RepoRoot "distribution/thunderstore/manifest.json") $Stage
Copy-Item (Join-Path $RepoRoot "distribution/thunderstore/README.md") $Stage
Copy-Item (Join-Path $RepoRoot "distribution/thunderstore/CHANGELOG.md") $Stage
Copy-Item (Join-Path $RepoRoot "distribution/thunderstore/icon.png") $Stage
Copy-Item (Join-Path $RepoRoot "LICENSE") $Stage
Copy-Item $DllPath (Join-Path $Stage "plugins/POVLMTYBeastAIAdapter/POVLMTYBeastAIAdapter.dll")

if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path (Join-Path $Stage "*") -DestinationPath $ZipPath -CompressionLevel Optimal

Write-Host "Created: $ZipPath"
Write-Host "Verify ZIP root contains manifest.json, README.md, icon.png, CHANGELOG.md, LICENSE, and plugins/."
