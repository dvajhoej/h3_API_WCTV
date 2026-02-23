@echo off
setlocal

echo.
echo  [1/2] Starting WCTV.Api ...
start "WCTV API" cmd /k "cd /d "%~dp0WCTV.Api" && dotnet run"

echo  [2/2] Waiting for API on localhost:5000
set TRIES=0

:poll
set /a TRIES+=1
if %TRIES% gtr 45 (
    echo.
    echo  ERROR: API did not respond after 90 seconds.
    echo  Check the "WCTV API" window for build or runtime errors.
    exit /b 1
)
curl -sf http://localhost:5000/api/kpi -o nul 2>nul
if not errorlevel 1 goto ready
<nul set /p ".=."
timeout /t 2 /nobreak >nul
goto poll

:ready
echo  ready!
echo  [3/3] Starting wctv-dashboard ...
start "WCTV Dashboard" cmd /k "cd /d "%~dp0wctv-dashboard" && npm run dev"

echo.
echo   API:       http://localhost:5000
echo   Dashboard: http://localhost:5173
echo   Swagger:   http://localhost:5000/swagger
echo.
echo   Both services are running in separate windows.
echo.
