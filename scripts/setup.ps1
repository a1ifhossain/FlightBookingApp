$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (!(Test-Path -LiteralPath '.env')) {
    $sqlPassword = 'Sql!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(18)) + 'a1'
    $demoPassword = 'Demo!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(12)) + 'a1'
    "MSSQL_SA_PASSWORD=$sqlPassword`nDEMO_PASSWORD=$demoPassword" | Set-Content -LiteralPath '.env'
    Write-Host 'Created local .env with random SQL and demo passwords. Keep this file out of Git.'
} else { Write-Host 'Existing .env preserved.' }
