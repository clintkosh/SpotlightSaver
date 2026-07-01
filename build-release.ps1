$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot

dotnet publish .\src\SpotlightSaver\SpotlightSaver.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish

Write-Host ""
Write-Host "Built executable:" -ForegroundColor Green
Write-Host "$PSScriptRoot\publish\SpotlightSaver.exe"

Pop-Location
