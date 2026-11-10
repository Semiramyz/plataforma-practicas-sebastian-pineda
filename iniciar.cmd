@echo off
rem Doble clic para levantar la plataforma en Windows. Pasa los argumentos a iniciar.ps1
rem (por ejemplo: iniciar.cmd -Puerto 8080). -ExecutionPolicy Bypass evita el bloqueo de scripts
rem de PowerShell solo para esta ejecucion; no cambia la configuracion del equipo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0iniciar.ps1" %*
if errorlevel 1 pause
