@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
set "EDITOR_DIR=%SCRIPT_DIR%..\stage-editor"

if not exist "%EDITOR_DIR%\node_modules" (
    echo [stage-editor] node_modules not found, running npm install...
    pushd "%EDITOR_DIR%"
    call npm install
    if %ERRORLEVEL% neq 0 (
        echo [stage-editor] ERROR: npm install failed
        popd
        pause
        exit /b 1
    )
    popd
)

echo [stage-editor] Starting dev server at http://localhost:3000
pushd "%EDITOR_DIR%"
call npm run dev
popd

endlocal
