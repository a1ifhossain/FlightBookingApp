param([string]$BaseUrl = "http://localhost:5080")
$ErrorActionPreference = "Stop"
$date = (Get-Date).AddDays(7).ToString('yyyy-MM-dd')
$health = Invoke-RestMethod "$BaseUrl/health"
if ($health.status -ne 'Healthy') { throw 'Health check failed' }
$offers = Invoke-RestMethod "$BaseUrl/api/flights/search?origin=dac&destination=cxb&departureDate=$date"
if ($offers.Count -ne 2 -or $offers[0].price -ne 5800 -or $offers[1].price -ne 6500) { throw 'Search sorting failed' }
$empty = Invoke-RestMethod "$BaseUrl/api/flights/search?origin=CXB&destination=DAC&departureDate=$date"
if ($empty.Count -ne 0) { throw 'Empty search failed' }
foreach ($query in @("origin=DA&destination=CXB&departureDate=$date", "origin=DAC&destination=DAC&departureDate=$date", 'origin=DAC&destination=CXB', 'origin=DAC&destination=CXB&departureDate=2000-01-01', 'origin=DAC&destination=CXB&departureDate=invalid')) {
    $status = 0
    try { Invoke-RestMethod "$BaseUrl/api/flights/search?$query" | Out-Null }
    catch { if ($null -eq $_.Exception.Response) { throw }; $status = [int]$_.Exception.Response.StatusCode }
    if ($status -ne 400) { throw "Expected 400 for $query, got $status" }
}
Write-Host 'PASS: health, route filtering, sorting, lowercase input, empty results and five invalid requests.'
