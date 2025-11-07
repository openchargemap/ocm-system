@echo off
REM OCM Import Worker - Docker Build Script (Windows)
REM This script builds the Docker image from the repository root

setlocal enabledelayedexpansion

REM Configuration
set IMAGE_NAME=ocm-import-worker
set IMAGE_TAG=%1
if "%IMAGE_TAG%"=="" set IMAGE_TAG=latest
set BUILD_CONFIG=%BUILD_CONFIGURATION%
if "%BUILD_CONFIG%"=="" set BUILD_CONFIG=Release
set DOCKERFILE_PATH=Import\OCM.Import.Worker\Dockerfile

REM Get script directory and navigate to repo root
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%..\.."
set REPO_ROOT=%CD%

echo ========================================
echo OCM Import Worker - Docker Build
echo ========================================
echo.
echo Repository Root: %REPO_ROOT%
echo Image Name: %IMAGE_NAME%:%IMAGE_TAG%
echo Build Configuration: %BUILD_CONFIG%
echo Dockerfile: %DOCKERFILE_PATH%
echo.

REM Check if Dockerfile exists
if not exist "%DOCKERFILE_PATH%" (
    echo Error: Dockerfile not found at %DOCKERFILE_PATH%
    exit /b 1
)

REM Build the image
echo Building Docker image...
docker build ^
    -f "%DOCKERFILE_PATH%" ^
    --build-arg BUILD_CONFIGURATION=%BUILD_CONFIG% ^
    -t %IMAGE_NAME%:%IMAGE_TAG% ^
    .

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================
    echo Build completed successfully!
    echo ========================================
    echo.
    echo Image: %IMAGE_NAME%:%IMAGE_TAG%
    echo.
    echo To run the container:
    echo   docker run -d --name ocm-import-worker %IMAGE_NAME%:%IMAGE_TAG%
    echo.
    echo To view logs:
    echo   docker logs -f ocm-import-worker
    echo.
    echo To use Docker Compose:
    echo   cd Import\OCM.Import.Worker ^&^& docker-compose up -d
    echo.
) else (
    echo Build failed!
    exit /b 1
)

endlocal
