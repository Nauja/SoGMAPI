@echo off
setlocal enabledelayedexpansion

SET installerDir="%~dp0"

REM make sure we're not running within a zip folder
echo %installerDir% | findstr /C:"%TEMP%" 1>nul
if %ERRORLEVEL% EQU 0 (
    echo Oops! It looks like you're running the installer from inside a zip file. Make sure you unzip the download first.
    echo.
    pause
    exit
)

REM make sure an antivirus hasn't deleted the installer DLL
if not exist %installerDir%"internal\windows\SoGMAPI.Installer.dll" (
    echo Oops! SoGMAPI is missing one of its files. Your antivirus might have deleted it.
    echo Missing file: %installerDir%internal\windows\SoGMAPI.Installer.dll
    echo.
    pause
    exit
)
if not exist %installerDir%"internal\windows\SoGMAPI.Installer.exe" (
    echo Oops! SoGMAPI is missing one of its files. Your antivirus might have deleted it.
    echo Missing file: %installerDir%internal\windows\SoGMAPI.Installer.exe
    echo.
    pause
    exit
)

REM start installer
internal\windows\SoGMAPI.Installer.exe

REM keep window open if it failed
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Oops! The SoGMAPI installer seems to have failed. The error details may be shown above.
    echo.
    pause
    exit
)
