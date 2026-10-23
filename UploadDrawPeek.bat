@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 goto location_error
if not exist "ModUploader.exe" goto missing_uploader
if not exist "DrawPeekWorkshop\workshop.json" goto missing_workspace
set "DRAWPEEK_WORKSHOP_CONFIG=%~dp0DrawPeekWorkshop\workshop.json"
powershell.exe -NoProfile -Command "try { $ErrorActionPreference = 'Stop'; $config = Get-Content -LiteralPath $env:DRAWPEEK_WORKSHOP_CONFIG -Raw -Encoding UTF8 | ConvertFrom-Json -ErrorAction Stop; $bytes = [Text.Encoding]::UTF8.GetByteCount([string]$config.description); Write-Host ('Workshop description: ' + $bytes + ' UTF-8 bytes.'); if ($bytes -ge 8000) { throw 'Workshop description must be below 8000 UTF-8 bytes. Shorten it before uploading.' } } catch { Write-Host ('ERROR: ' + $_.Exception.Message); exit 1 }"
if errorlevel 1 goto invalid_config
"%~dp0ModUploader.exe" upload -w "%~dp0DrawPeekWorkshop"
set "upload_result=%errorlevel%"
echo.
echo ModUploader finished with exit code %upload_result%.
goto finish
:missing_uploader
echo ERROR: ModUploader.exe was not found beside this BAT.
set "upload_result=1"
goto finish
:missing_workspace
echo ERROR: DrawPeekWorkshop\workshop.json was not found.
set "upload_result=1"
goto finish
:invalid_config
echo Upload cancelled because the workshop configuration check failed.
set "upload_result=1"
:finish
popd
pause
exit /b %upload_result%
:location_error
echo ERROR: Could not open the uploader directory.
pause
exit /b 1
