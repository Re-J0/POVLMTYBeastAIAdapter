@echo off
setlocal

echo [START] POV LMTY BeastAI Adapter v0.3.1 build/install script started.

cd /d "%~dp0"
set "VALHEIM=C:\Program Files (x86)\Steam\steamapps\common\Valheim"
set "GALEPROFILE=%APPDATA%\com.kesomannen.gale\valheim\profiles\default"
set "GALECORE=%GALEPROFILE%\BepInEx\core"
set "DEST=%GALEPROFILE%\BepInEx\plugins\POVLMTYBeastAIAdapter"

echo [INFO] Source dir: "%CD%"
echo [INFO] Valheim dir: "%VALHEIM%"
echo [INFO] Gale profile: "%GALEPROFILE%"

if not exist "%VALHEIM%\valheim_Data\Managed\assembly_valheim.dll" goto :missing_valheim
if not exist "%GALECORE%\BepInEx.dll" goto :missing_bepinex
if not exist "%GALECORE%\0Harmony.dll" goto :missing_harmony

echo [INFO] Building POVLMTYBeastAIAdapter...
dotnet build "POVLMTYBeastAIAdapter.csproj" -c Release -p:ValheimDir="%VALHEIM%" -p:BepInExCoreDir="%GALECORE%"
if errorlevel 1 goto :build_failed

if not exist "bin\Release\netstandard2.1\POVLMTYBeastAIAdapter.dll" goto :dll_missing

if not exist "%DEST%" mkdir "%DEST%"
copy /y "bin\Release\netstandard2.1\POVLMTYBeastAIAdapter.dll" "%DEST%\POVLMTYBeastAIAdapter.dll" >nul
if errorlevel 1 goto :copy_failed

echo.
echo ===============================================================
echo Build + install succeeded.
echo Installed to:
echo "%DEST%\POVLMTYBeastAIAdapter.dll"
echo ===============================================================
echo.
echo IMPORTANT:
echo   Disable/delete the OLD plugin folder:
echo   "%GALEPROFILE%\BepInEx\plugins\POVLMTYDeerAdapter"
echo.
echo TEST SETUP:
echo   PathOfValheiman        = ON
echo   LetMeTameYou           = ON
echo   POVLMTYBeastAIAdapter  = ON
echo   POVLMTYDeerAdapter     = OFF/REMOVED
echo   POVDeerBreedingFix     = OFF
echo   BetterTames            = ON or OFF ^(either is supported; keep your current setup^)
echo.
echo Generic auto-repair + adaptive taming:
echo   No prefab-name allowlist.
echo   Every LMTY AddTame target is inspected once during prefab setup.
echo   Only the confirmed POV incompatible shape is auto-repaired:
echo     MonsterAI + Character + no Humanoid + no AnimalAI + POV AI evidence
echo   Normal LMTY creatures pass through untouched.
echo   Unknown custom-AI shapes are logged but not modified.
echo.
echo NOTE:
echo   v0.3.1 intentionally leaves special egg-config auto-repair to original LMTY.
echo   Standard mini-offspring and explicit offspringPrefab are supported.
echo.
echo For v0.3.1, only one fresh untamed Deer/Hare/Seal test is needed.
echo Send the new LogOutput.log after that focused test.
echo.
pause
exit /b 0

:missing_valheim
echo [ERROR] assembly_valheim.dll not found.
goto :fail
:missing_bepinex
echo [ERROR] BepInEx.dll not found.
goto :fail
:missing_harmony
echo [ERROR] 0Harmony.dll not found.
goto :fail
:build_failed
echo [ERROR] Build failed. Send the full console output.
goto :fail
:dll_missing
echo [ERROR] Build succeeded but DLL was not found.
goto :fail
:copy_failed
echo [ERROR] Could not copy DLL to Gale profile.
goto :fail
:fail
pause
exit /b 1
