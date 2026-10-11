param([switch]$DockerSql, [string]$SqlServer = 'localhost')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (!(Test-Path '.env')) { & "$PSScriptRoot/setup.ps1" }
$values = @{}
foreach ($line in Get-Content '.env') { if ($line -match '^([^#=]+)=(.*)$') { $values[$matches[1]] = $matches[2] } }
$env:ConnectionStrings__SqlServer = if ($DockerSql) { "Server=127.0.0.1,14333;Database=FlightBooking;User Id=sa;Password=$($values['MSSQL_SA_PASSWORD']);TrustServerCertificate=True" } else { "Server=$SqlServer;Database=FlightBooking;Integrated Security=True;TrustServerCertificate=True" }
$env:Database__Initialize = 'true'
$env:Seed__Password = $values['DEMO_PASSWORD']
$env:Supplier__Url = 'http://127.0.0.1:5090'
$env:Logging__LogLevel__Microsoft = 'Warning'
dotnet run --no-launch-profile --project src/FlightBooking.Api --urls http://127.0.0.1:5080
