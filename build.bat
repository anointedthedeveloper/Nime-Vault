@echo off
echo Closing running instances of Nime Vault...
taskkill /f /im NimeVault.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo Building Nime Vault...

if exist "%~dp0exe" (
    echo Clearing old exe output...
    rmdir /s /q "%~dp0exe"
)
mkdir "%~dp0exe"

dotnet publish NimeVault.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output ./exe ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false

if %ERRORLEVEL% == 0 (
    echo.
    echo Copying cover images...
    if not exist "%~dp0exe\covers" mkdir "%~dp0exe\covers"
    copy /y "%~dp0covers\*.png" "%~dp0exe\covers\" >nul
    echo Build succeeded. Output: %~dp0exe\NimeVault.exe
) else (
    echo.
  echo Build failed.
  echo See the output above for details.
  pause
  exit /b 1
)
