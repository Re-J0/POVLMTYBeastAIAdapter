param(
    [string]$ValheimDir = "C:\Program Files (x86)\Steam\steamapps\common\Valheim",
    [string]$BepInExCoreDir = "$env:APPDATA\com.kesomannen.gale\valheim\profiles\default\BepInEx\core"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $RepoRoot
try {
    dotnet build -c Release -p:ValheimDir="$ValheimDir" -p:BepInExCoreDir="$BepInExCoreDir"
    & (Join-Path $PSScriptRoot "package_thunderstore.ps1")
    & (Join-Path $PSScriptRoot "package_nexus.ps1")
} finally {
    Pop-Location
}
