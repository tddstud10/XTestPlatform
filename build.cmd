@echo off
cls

.paket\paket.bootstrapper.exe 4.8.7
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

packages\Build\FAKE\tools\FAKE.exe build.fsx %*
