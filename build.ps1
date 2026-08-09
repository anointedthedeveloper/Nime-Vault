dotnet publish NimeVault.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output ./exe `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded. Output: $PSScriptRoot\exe\NimeVault.exe" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed." -ForegroundColor Red
    exit 1
}
