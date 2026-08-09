@echo off
echo Building Nime Vault...

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
    exit /b 1
)
