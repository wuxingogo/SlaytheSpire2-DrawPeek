@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 goto location_error
if not exist "ModUploader.exe" goto missing_uploader
if not exist "DrawPeekWorkshop\workshop.json" goto missing_workspace
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
:finish
popd
pause
exit /b %upload_result%
:location_error
echo ERROR: Could not open the uploader directory.
pause
exit /b 1
