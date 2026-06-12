@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
set "EDITOR_DIR=%SCRIPT_DIR%stage_editor"
set "PROJECT_ROOT=%SCRIPT_DIR%.."
set "GEN_DIR=%SCRIPT_DIR%stage_generator"
set "GEN_DLL=%GEN_DIR%\bin\publish\StageGenerator.Cli.dll"
set "GEN_PROJ=%GEN_DIR%\StageGenerator.Cli.csproj"

:: Publish stage_generator if DLL is missing or source is newer
powershell -NoProfile -Command ^
  "if (!(Test-Path '%GEN_DLL%') -or ((Get-Item '%GEN_DIR%\Program.cs').LastWriteTime -gt (Get-Item '%GEN_DLL%').LastWriteTime) -or ((Get-Item '%GEN_PROJ%').LastWriteTime -gt (Get-Item '%GEN_DLL%').LastWriteTime)) { exit 1 } else { exit 0 }"
if %ERRORLEVEL% neq 0 (
    echo [stage_generator] Publishing Release build...
    dotnet publish "%GEN_PROJ%" -c Release -o "%GEN_DIR%\bin\publish" --nologo -v quiet
    if %ERRORLEVEL% neq 0 (
        echo [stage_generator] ERROR: publish failed
        pause
        exit /b 1
    )
    echo [stage_generator] Publish complete.
)

if not exist "%EDITOR_DIR%\node_modules" (
    echo [stage_editor] node_modules not found, running npm install...
    pushd "%EDITOR_DIR%"
    call npm install
    if %ERRORLEVEL% neq 0 (
        echo [stage_editor] ERROR: npm install failed
        popd
        pause
        exit /b 1
    )
    popd
)

echo [stage_editor] Starting dev server at http://localhost:3000
pushd "%EDITOR_DIR%"
call npm run dev
popd

endlocal
