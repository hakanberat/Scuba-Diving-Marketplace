param([string]$BaseUrl='http://localhost:5265')
$ErrorActionPreference='Stop'
$run=[Guid]::NewGuid().ToString('N').Substring(0,12)
$prefix="qa-$run"
$password='Qa!'+[Guid]::NewGuid().ToString('N')
$script:passed=0
function Assert($condition,$message) { if(!$condition) { throw "FAILED: $message" }; $script:passed++; Write-Host "PASS: $message" }
function Sql($query) {
 $result=& sqlcmd -S localhost\SQLEXPRESS -d DiveMarketplaceDb -E -C -I -b -h -1 -W -Q "SET NOCOUNT ON; $query"
 if($LASTEXITCODE -ne 0) { throw 'SQL test query failed.' }
 return (($result | Where-Object { $_.Trim() }) -join "`n").Trim()
}
function Request($path,$session,$method='GET',$body=$null) {
 try { return Invoke-WebRequest ($BaseUrl+$path) -WebSession $session -Method $method -Body $body -UseBasicParsing }
 catch {
  if($_.Exception.Response) { return [pscustomobject]@{StatusCode=[int]$_.Exception.Response.StatusCode;Content='';BaseResponse=$_.Exception.Response} }
  throw
 }
}
function Post($formPath,$postPath,$session,$body) {
 if($postPath -eq '/Account/RegisterOwner' -and !$body.ContainsKey('MapLocation')) { $body['MapLocation']='https://maps.google.com/?q=Famagusta' }
 if($postPath -eq '/Reservations/Create') {
  foreach($entry in @{ContactEmail='qa-contact@example.invalid';ContactPhone='+905551112233';ContactByEmail='true'}.GetEnumerator()) {
   if(!$body.ContainsKey($entry.Key)) { $body[$entry.Key]=$entry.Value }
  }
 }
 $page=Request $formPath $session
 if($page.StatusCode -ne 200) { throw "Cannot read form $formPath ($($page.StatusCode))" }
 $token=[regex]::Match($page.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
 if(!$token) { throw "No antiforgery token in $formPath" }
 $body['__RequestVerificationToken']=[System.Net.WebUtility]::HtmlDecode($token)
 return Request $postPath $session 'POST' $body
}
function Register($suffix) {
 $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $email="$prefix-$suffix@example.invalid"
 $response=Post '/Account/Register' '/Account/Register' $session @{FirstName='QA';LastName=$suffix;Email=$email;Password=$password;ConfirmPassword=$password;Role='Admin'}
 Assert ($response.StatusCode -eq 200 -and $response.Content -match 'Logout') "Register $suffix with hashed password and signed-in session"
 return $session
}
function Login($suffix) {
 $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $response=Post '/Account/Login' '/Account/Login' $session @{Email="$prefix-$suffix@example.invalid";Password=$password}
 Assert ($response.StatusCode -eq 200 -and $response.Content -match 'Logout') "Login $suffix"
 return $session
}
$centerId=0; $otherId=0
try {
 $customer=Register 'customer'; $admin=Register 'admin'; $owner=Register 'owner'
 Sql "UPDATE AppUsers SET Role=CASE WHEN Email='$prefix-admin@example.invalid' THEN 'Admin' ELSE 'DiveCenter' END WHERE Email IN('$prefix-admin@example.invalid','$prefix-owner@example.invalid')" | Out-Null
 $admin=Login 'admin'; $owner=Login 'owner'
 $ownerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-owner@example.invalid'")
 $customerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid'")
 $centerId=[int](Sql "INSERT DiveCenters(Name,IsActive,CreatedAt) OUTPUT INSERTED.Id VALUES('Revenue QA $run',1,SYSUTCDATETIME())")
 $otherId=[int](Sql "INSERT DiveCenters(Name,IsActive,CreatedAt) OUTPUT INSERTED.Id VALUES('Other revenue QA $run',1,SYSUTCDATETIME())")
 Sql "INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) VALUES($centerId,$ownerId,1); INSERT Activities(DiveCenterId,ActivityCategoryId,Name,Price,CurrencyCode) SELECT Id,(SELECT TOP(1) Id FROM ActivityCategories),'Revenue QA',1,'EUR' FROM DiveCenters WHERE Id IN($centerId,$otherId); INSERT Reservations(ReservationNumber,ActivityId,DiveCenterId,CustomerUserId,ReservationDate,Participants,UnitPrice,CurrencyCode,TotalPrice,Status) SELECT '$prefix-'+v.Code,a.Id,a.DiveCenterId,$customerId,CAST(GETDATE() AS date),1,v.Amount,v.Currency,v.Amount,v.Status FROM Activities a CROSS JOIN (VALUES('eur',CAST(123.45 AS decimal(18,2)),'EUR','Confirmed'),('usd',77.25,'USD','Confirmed'),('pending',45.50,'EUR','Pending'),('cancelled',999.00,'EUR','Cancelled'),('rejected',888.00,'EUR','Rejected')) v(Code,Amount,Currency,Status) WHERE a.DiveCenterId=$centerId; INSERT Reservations(ReservationNumber,ActivityId,DiveCenterId,CustomerUserId,ReservationDate,Participants,UnitPrice,CurrencyCode,TotalPrice,Status) SELECT '$prefix-other',Id,DiveCenterId,$customerId,CAST(GETDATE() AS date),1,444.44,'EUR',444.44,'Confirmed' FROM Activities WHERE DiveCenterId=$otherId;" | Out-Null
 $r=Request '/Dashboard' $owner
 $section=[regex]::Match($r.Content,'(?s)<section class="mb-4" aria-labelledby="reservation-revenue-heading">.*?</section>').Value
 Assert ($r.StatusCode -eq 200 -and $section -match 'Reservation revenue') 'Owner dashboard shows revenue'
 Assert ($section -match '123[.,]45 EUR' -and $section -match '77[.,]25 USD' -and $section -match '45[.,]50 EUR') 'Currencies and pending amounts remain separate'
 Assert ($section -notmatch '999[.,]00|888[.,]00') 'Cancelled and rejected excluded'
 Assert ($section -notmatch "Other revenue QA $run|444[.,]44") 'Unrelated school revenue is private'
 Assert ($r.Content -match 'btn btn-outline-primary btn-sm"[^>]*href="/Account/Profile"') 'Name appears as profile button'
 $nav=[regex]::Match($r.Content,'(?s)<nav class="management-navigation.*?</nav>').Value
 Assert ($nav -notmatch 'Add activity') 'Owner management navigation omits duplicate activity shortcut'
 $r=Request '/Admin' $admin
 $section=[regex]::Match($r.Content,'(?s)<section class="mb-4" aria-labelledby="reservation-revenue-heading">.*?</section>').Value
 Assert ($r.StatusCode -eq 200 -and $section -match "Revenue QA $run" -and $section -match "Other revenue QA $run" -and $section -match '444[.,]44 EUR') 'Admin sees every school revenue'
 $nav=[regex]::Match($r.Content,'(?s)<nav class="management-navigation.*?</nav>').Value
 Assert ($nav -notmatch 'Add activity') 'Admin activity shortcut removed'
 Assert ($section -match 'Payments are not tracked yet') 'Booking values distinguished from collected payments'
 Sql "UPDATE Reservations SET Status='Cancelled' WHERE ReservationNumber='$prefix-eur'" | Out-Null
 $r=Request '/Dashboard' $owner
 $section=[regex]::Match($r.Content,'(?s)<section class="mb-4" aria-labelledby="reservation-revenue-heading">.*?</section>').Value
 Assert ($section -notmatch '123[.,]45 EUR' -and $section -match '0[.,]00 EUR') 'Cancelling confirmed booking updates revenue'
 $r=Request '/Admin' $customer
 Assert ($r.StatusCode -eq 403) 'Customer cannot access admin revenue'
 Write-Host "Completed $script:passed revenue checks."
}
finally {
 Sql "DELETE Reservations WHERE ReservationNumber LIKE '$prefix-%'; DELETE Activities WHERE DiveCenterId IN($centerId,$otherId); DELETE DiveCenterUsers WHERE DiveCenterId IN($centerId,$otherId); DELETE DiveCenters WHERE Id IN($centerId,$otherId); DELETE Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'); DELETE AppUsers WHERE Email LIKE '$prefix-%';" | Out-Null
}
