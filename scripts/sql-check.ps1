param([switch]$DockerSql, [string]$SqlServer = 'localhost')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if ($DockerSql) {
    docker compose cp sql/learning.sql sql:/tmp/learning.sql
    if ($LASTEXITCODE -ne 0) { throw 'SQL file copy failed.' }
    docker compose exec -T sql sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d FlightBooking -i /tmp/learning.sql -b'
} else {
    $sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE'
    if (!(Test-Path $sqlcmd)) { $sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source }
    & $sqlcmd -S $SqlServer -E -C -d FlightBooking -i sql/learning.sql -b -W -w 160
}
if ($LASTEXITCODE -ne 0) { throw 'SQL checks failed.' }
