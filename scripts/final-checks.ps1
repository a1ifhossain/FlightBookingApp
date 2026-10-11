param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$password = ((Get-Content '.env' | Where-Object { $_ -match '^DEMO_PASSWORD=' }) -split '=',2)[1]
function JsonRequest($path,$body,$token='',$method='POST') {
    $headers=@{}; if ($token) { $headers.Authorization="Bearer $token" }
    Invoke-RestMethod "$BaseUrl/api$path" -Method $method -Headers $headers -ContentType 'application/json' -Body ($body|ConvertTo-Json -Depth 8) -TimeoutSec 15
}
$admin=(JsonRequest '/auth/login?useCookies=false' @{email='admin@example.com';password=$password}).accessToken
$email='qa-'+[guid]::NewGuid().ToString('N')+'@example.com'
JsonRequest '/auth/register' @{email=$email;password=$password} | Out-Null
$login=(JsonRequest '/auth/login?useCookies=false' @{email=$email;password=$password}).accessToken
$me=Invoke-RestMethod "$BaseUrl/api/account/me" -Headers @{Authorization="Bearer $login"}
JsonRequest "/admin/users/$($me.id)/role" @{role='Agent'} $admin 'PUT' | Out-Null
$revoked=Invoke-WebRequest "$BaseUrl/api/account/me" -Headers @{Authorization="Bearer $login"} -SkipHttpErrorCheck
if ([int]$revoked.StatusCode -ne 401) { throw 'Role update did not invalidate old token.' }
$login=(JsonRequest '/auth/login?useCookies=false' @{email=$email;password=$password}).accessToken
$me=Invoke-RestMethod "$BaseUrl/api/account/me" -Headers @{Authorization="Bearer $login"}
if ($me.roles -notcontains 'Agent') { throw 'Updated role not visible after login.' }

$date=(Get-Date).AddMonths(1).ToString('yyyy-MM')+'-20'
$payload=@{offerId='REST-001';supplier='REST';origin='DAC';destination='CXB';departureDate=$date;contactEmail=$email;passengers=@(@{fullName='Concurrency Demo'})}|ConvertTo-Json -Depth 8
$key=[guid]::NewGuid().ToString()
$client=[System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Authorization=[System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$login)
$client.DefaultRequestHeaders.Add('Idempotency-Key',$key)
$content1=[System.Net.Http.StringContent]::new($payload,[Text.Encoding]::UTF8,'application/json')
$content2=[System.Net.Http.StringContent]::new($payload,[Text.Encoding]::UTF8,'application/json')
$pending1=$client.PostAsync("$BaseUrl/api/bookings",$content1)
$pending2=$client.PostAsync("$BaseUrl/api/bookings",$content2)
$response1=$pending1.GetAwaiter().GetResult();$response2=$pending2.GetAwaiter().GetResult()
$first=($response1.Content.ReadAsStringAsync().GetAwaiter().GetResult())|ConvertFrom-Json
$second=($response2.Content.ReadAsStringAsync().GetAwaiter().GetResult())|ConvertFrom-Json
if ([int]$response1.StatusCode -notin @(200,201) -or [int]$response2.StatusCode -notin @(200,201) -or $first.reference -ne $second.reference) { throw 'Concurrent idempotent create failed.' }
$client.DefaultRequestHeaders.Remove('Idempotency-Key')|Out-Null
$update1=@{contactEmail='one@example.com';version=$first.version}|ConvertTo-Json
$update2=@{contactEmail='two@example.com';version=$first.version}|ConvertTo-Json
$pending1=$client.PutAsync("$BaseUrl/api/bookings/$($first.reference)/contact",[System.Net.Http.StringContent]::new($update1,[Text.Encoding]::UTF8,'application/json'))
$pending2=$client.PutAsync("$BaseUrl/api/bookings/$($first.reference)/contact",[System.Net.Http.StringContent]::new($update2,[Text.Encoding]::UTF8,'application/json'))
$statuses=@([int]$pending1.GetAwaiter().GetResult().StatusCode,[int]$pending2.GetAwaiter().GetResult().StatusCode)|Sort-Object
if ($statuses[0] -ne 200 -or $statuses[1] -ne 409) { throw "Concurrent version updates failed: $statuses" }
New-Item -ItemType Directory -Force '.local'|Out-Null
@{reference=$first.reference;email=$email}|ConvertTo-Json|Set-Content '.local/persistence-check.json'
$response1.Dispose();$response2.Dispose();$client.Dispose();$content1.Dispose();$content2.Dispose()
Write-Host 'PASS: registration, role assignment, old-session revocation, simultaneous idempotent creates and competing updates.'
