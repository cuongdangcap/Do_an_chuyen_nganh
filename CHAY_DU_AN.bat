@echo off
setlocal
cd /d "%~dp0"
title CMC Admissions - Chay du an

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\launch_local_runtime.ps1"

if errorlevel 1 (
  echo.
  echo [LOI] Du an chua khoi dong thanh cong. Xem thong bao phia tren.
  pause
)

endlocal
