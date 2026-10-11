$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
& "$PSScriptRoot/setup.ps1"
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw 'Compose failed. Check Docker Desktop is running with Linux containers.' }
Write-Host 'Open http://localhost:8088. Check readiness at http://localhost:5080/health/ready.'
