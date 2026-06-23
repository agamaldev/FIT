@echo off
REM ============================================================
REM  MAKE ME FIT - launcher
REM  Double-click THIS file (instead of the .html) to run the
REM  site on a local web server so the YouTube popup player
REM  works.
REM ============================================================
cd /d "%~dp0"

REM Start the local web server in its own minimized window
start "MAKE ME FIT server" /min python -m http.server 8000

REM Give the server a moment, then open the site in the browser
timeout /t 2 /nobreak >nul
start "" http://localhost:8000/index.html

echo.
echo   MAKE ME FIT is now running at:  http://localhost:8000
echo.
echo   The video popups will work while this server is running.
echo   To STOP the site, close the minimized "MAKE ME FIT server" window.
echo.
pause
