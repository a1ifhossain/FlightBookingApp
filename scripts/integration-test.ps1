param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$values = @{}
foreach ($line in Get-Content '.env') { if ($line -match '^([^#=]+)=(.*)$') { $values[$matches[1]] = $matches[2] } }
$checks = 0
function Request($path, $method = 'GET', $body = $null, $token = '', $extra = @{}, $expected = 200) {
    $headers = @{}; foreach ($key in $extra.Keys) { $headers[$key] = $extra[$key] }
    if ($token) { $headers.Authorization = "Bearer $token" }
    $parameters = @{ Uri="$BaseUrl/api$path"; Method=$method; Headers=$headers; TimeoutSec=20; SkipHttpErrorCheck=$true }
    if ($null -ne $body) { $parameters.Body = ($body | ConvertTo-Json -Depth 10); $parameters.ContentType='application/json' }
    $response = Invoke-WebRequest @parameters
    if ($response.Content -is [byte[]]) { $response = [pscustomobject]@{ StatusCode = $response.StatusCode; Content = [Text.Encoding]::UTF8.GetString($response.Content) } }
    if ([int]$response.StatusCode -ne $expected) { throw "$method $path expected $expected, got $($response.StatusCode): $($response.Content)" }
    $script:checks++
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}
function Login($email) { (Request '/auth/login?useCookies=false' 'POST' @{email=$email;password=$values['DEMO_PASSWORD']}).accessToken }
$customer = Login 'customer@example.com'; $agent = Login 'agent@example.com'; $admin = Login 'admin@example.com'
Request '/account/me' 'GET' $null $customer | Out-Null
Request '/bookings' 'GET' $null '' @{} 401 | Out-Null
Request '/admin/users' 'GET' $null $customer @{} 403 | Out-Null
Request '/admin/users' 'GET' $null $admin | Out-Null
$date = (Get-Date).AddMonths(1).ToString('yyyy-MM') + '-20'
$search = Request "/flights/search?origin=DAC&destination=CXB&departureDate=$date"
if ($search.offers.Count -ne 2 -or $search.offers[0].price -ne 5800) { throw 'REST/SOAP search mapping failed' }
foreach ($supplier in @('REST','SOAP')) {
    $offer = $search.offers | Where-Object supplier -eq $supplier
    $key = [guid]::NewGuid().ToString()
    $body = @{offerId=$offer.id;supplier=$supplier;origin='DAC';destination='CXB';departureDate=$date;contactEmail='demo@example.com';passengers=@(@{fullName='Demo One'},@{fullName='Demo Two'});price=1}
    $booking = Request '/bookings' 'POST' $body $customer @{'Idempotency-Key'=$key} 201
    if ($booking.totalPrice -ne ($offer.price * 2) -or $booking.passengers.Count -ne 2) { throw 'Server fare or passenger relationship failed' }
    $replay = Request '/bookings' 'POST' $body $customer @{'Idempotency-Key'=$key}
    if ($replay.reference -ne $booking.reference) { throw 'Idempotency replay failed' }
    $body.contactEmail='changed@example.com'
    Request '/bookings' 'POST' $body $customer @{'Idempotency-Key'=$key} 409 | Out-Null
    Request "/bookings/$($booking.reference)" 'GET' $null $agent @{} 404 | Out-Null
    Request "/bookings/$($booking.reference)" 'GET' $null $admin | Out-Null
    Request "/bookings/$($booking.reference)/ticket" 'POST' @{version=$booking.version} $customer @{} 409 | Out-Null
    $stale = $booking.version
    $booking = Request "/bookings/$($booking.reference)/contact" 'PUT' @{contactEmail='changed@example.com';version=$booking.version} $customer
    Request "/bookings/$($booking.reference)/confirm" 'POST' @{version=$stale} $customer @{} 409 | Out-Null
    foreach ($action in @('confirm','ticket','cancel','refund')) {
        $booking = Request "/bookings/$($booking.reference)/$action" 'POST' @{version=$booking.version} $customer
        if ($action -eq 'ticket' -and ($booking.tickets.Count -ne 2 -or !$booking.paymentReference)) { throw 'Ticket relationships/payment failed' }
    }
    if ($booking.status -ne 'Refunded' -or $booking.events.Count -ne 6) { throw 'Lifecycle/history failed' }
    Request "/bookings/$($booking.reference)/confirm" 'POST' @{version=$booking.version} $customer @{} 409 | Out-Null
}
$body.supplier='REST';$body.offerId='REST-001';$body.contactEmail='invalid'
Request '/bookings' 'POST' $body $customer @{'Idempotency-Key'=[guid]::NewGuid().ToString()} 400 | Out-Null
$body.contactEmail='demo@example.com';$body.offerId='SOAP-001'
Request '/bookings' 'POST' $body $customer @{'Idempotency-Key'=[guid]::NewGuid().ToString()} 409 | Out-Null
$body.offerId='REST-001'
$agentBooking = Request '/bookings' 'POST' $body $agent @{'Idempotency-Key'=[guid]::NewGuid().ToString()} 201
Request "/bookings/$($agentBooking.reference)" 'DELETE' $null $agent @{'If-Match'=('"' + $agentBooking.version + '"')} 204 | Out-Null
$cancelled = Request "/bookings/$($agentBooking.reference)" 'GET' $null $agent
if ($cancelled.status -ne 'Cancelled') { throw 'DELETE cancellation failed' }
$failureDate = (Get-Date).AddMonths(1).ToString('yyyy-MM') + '-13'
$partial = Request "/flights/search?origin=DAC&destination=CXB&departureDate=$failureDate"
if ($partial.offers.Count -ne 1 -or $partial.warnings.Count -ne 1) { throw 'REST failure isolation failed' }
$failureDate = (Get-Date).AddMonths(1).ToString('yyyy-MM') + '-14'
$partial = Request "/flights/search?origin=DAC&destination=CXB&departureDate=$failureDate"
if ($partial.offers.Count -ne 1 -or $partial.warnings.Count -ne 1) { throw 'SOAP failure isolation failed' }
Request '/admin/reports' 'GET' $null $admin | Out-Null
Request '/admin/sql-summary' 'GET' $null $admin | Out-Null
Request '/bookings?page=1&pageSize=2' 'GET' $null $customer | Out-Null
Write-Host "PASS: $checks HTTP checks covering auth/roles/ownership, REST + SOAP lifecycle, idempotency, validation, concurrency, failure isolation and reporting."
