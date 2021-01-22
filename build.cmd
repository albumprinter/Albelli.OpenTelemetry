@echo off
cls
pushd "%~dp0"
call paket restore
if errorlevel 1 (
  exit /b %errorlevel%
)

"packages\build\FAKE\tools\Fake.exe" build.fsx %* "parallel-jobs=1"
popd