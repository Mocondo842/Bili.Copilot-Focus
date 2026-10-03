@echo off
rem Bili.Copilot-Focus sideload installer launcher (ASCII only on purpose:
rem cmd.exe codepage issues). Double-click this file; it keeps the window open.
setlocal
title Bili.Copilot-Focus Installer
cd /d "%~dp0"

set "PS1=%~dp0Install-Focus.ps1"
if not exist "%PS1%" (
    echo [ERROR] Install-Focus.ps1 was not found next to this file.
    echo         Extract the whole zip first, then run this file again.
    echo.
    pause
    exit /b 1
)

echo ===============================================
echo  Bili.Copilot-Focus installer
echo  A UAC prompt may appear - click Yes there.
echo ===============================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%" -FromCmd
set "RC=%ERRORLEVEL%"

if "%RC%"=="2" (
    echo.
    echo Administrator rights were requested.
    echo Finish the installation in the NEW elevated window.
    echo If no UAC prompt appeared, right-click Install-Focus.ps1
    echo and choose "Run as administrator".
    echo.
    pause
    exit /b 0
)

if not "%RC%"=="0" (
    echo.
    echo Installer exited with code %RC%.
    echo Please copy the text above and send it to the author.
    echo.
    pause
    exit /b %RC%
)

echo.
pause
endlocal
exit /b 0
