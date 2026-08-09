@echo off
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
    echo Build succeeded. Output: %~dp0exe\NimeVault.exe
) else (
    echo.
  echo Build failed.
  echo See the output above for details.
  pause
  exit /b 1
)
