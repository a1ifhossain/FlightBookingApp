param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
$date = (Get-Date).AddDays(7).ToString('yyyy-MM-dd')
$payload = @{ offerId='MOCK-002'; origin='DAC'; destination='CXB'; departureDate=$date; passengerName='Demo Passenger'; contactEmail='demo@example.com'; price=1 }
$response = Invoke-WebRequest "$BaseUrl/api/bookings" -Method Post -ContentType 'application/json' -Body ($payload | ConvertTo-Json) -TimeoutSec 10
if ([int]$response.StatusCode -ne 201 -or !$response.Headers.Location) { throw 'Expected 201 and Location' }
$booking = $response.Content | ConvertFrom-Json
if ($booking.flight.price -ne 5800 -or $booking.status -ne 'Pending') { throw 'Server price/status failed' }
$saved = Invoke-RestMethod "$BaseUrl/api/bookings/$($booking.reference)" -TimeoutSec 10
if ($saved.reference -ne $booking.reference -or $saved.passengerName -ne 'Demo Passenger') { throw 'Read failed' }
function Assert-Status($url, $method, $body, $expected) {
    $status = 0
    try { Invoke-WebRequest $url -Method $method -ContentType 'application/json' -Body $body -TimeoutSec 10 | Out-Null }
    catch { if ($null -eq $_.Exception.Response) { throw }; $status = [int]$_.Exception.Response.StatusCode }
    if ($status -ne $expected) { throw "Expected $expected, got $status" }
}
Assert-Status "$BaseUrl/api/bookings/unknown" 'Get' $null 404
$payload.contactEmail='invalid'
Assert-Status "$BaseUrl/api/bookings" 'Post' ($payload | ConvertTo-Json) 400
$payload.contactEmail='demo@example.com'; $payload.passengerName='  '
Assert-Status "$BaseUrl/api/bookings" 'Post' ($payload | ConvertTo-Json) 400
$payload.passengerName='Demo Passenger'; $payload.destination='CGP'
Assert-Status "$BaseUrl/api/bookings" 'Post' ($payload | ConvertTo-Json) 409
$payload.destination='CXB'; $payload.departureDate='2000-01-01'
Assert-Status "$BaseUrl/api/bookings" 'Post' ($payload | ConvertTo-Json) 400
Write-Host 'PASS: 201/Location, stored booking GET, server-controlled fare, 404, validation and offer mismatch.'
