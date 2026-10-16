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

$Stage = Join-Path $RepoRoot "artifacts/nexus"
$ZipPath = Join-Path $RepoRoot "artifacts/POVLMTYBeastAIAdapter-$Version-Nexus.zip"

if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $Stage "BepInEx/plugins/POVLMTYBeastAIAdapter") -Force | Out-Null

Copy-Item $DllPath (Join-Path $Stage "BepInEx/plugins/POVLMTYBeastAIAdapter/POVLMTYBeastAIAdapter.dll")
Copy-Item (Join-Path $RepoRoot "README.md") (Join-Path $Stage "README.md")
Copy-Item (Join-Path $RepoRoot "LICENSE") (Join-Path $Stage "LICENSE")

if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path (Join-Path $Stage "*") -DestinationPath $ZipPath -CompressionLevel Optimal

Write-Host "Created: $ZipPath"
