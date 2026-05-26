@echo off
title VR Rhythm Game Beatmap Editor Launcher
chcp 65001 > nul

echo ===================================================
echo  VR Rhythm Game - Standalone Beatmap Editor
echo ===================================================
echo.

cd BeatmapEditorApp

if not exist node_modules (
    echo [BeatmapEditor] First run detected. Installing Electron dependencies...
    echo (This may take 20-30 seconds for the first time. Please wait...)
    echo.
    cmd /c "npm install"
    if errorlevel 1 (
        echo.
        echo [ERROR] Failed to install Electron. Please check Node.js installation and internet connection.
        pause
        exit /b 1
    )
)

echo.
echo [BeatmapEditor] Starting Standalone Program...
echo (Closing this console window will also close the Editor.)
echo.

cmd /c "npm start"
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start Editor. (Exit Code: %errorlevel%)
    pause
)
