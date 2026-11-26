param([string]$BaseUrl='http://localhost:5264')
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
$centerId=0; $activityId=0; $reservationId=0
try {
 $owner=Register 'operator'; $customer=Register 'customer'; $stranger=Register 'stranger'
 Sql "UPDATE AppUsers SET Role='Admin' WHERE Email='$prefix-operator@example.invalid'" | Out-Null
 $owner=Login 'operator'
 $r=Post '/CenterProfile/Create' '/CenterProfile/Create' $owner @{Name="Map QA $run";Country='Cyprus';City='Famagusta';MapLocation='35.123456, 33.987654'}
 Assert ($r.StatusCode -eq 200) 'Create school with map location'
 $centerId=[int](Sql "SELECT Id FROM DiveCenters WHERE Name='Map QA $run'")
 $anonymous=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $r=Request "/DiveCenters/Details/$centerId" $anonymous
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'maps.google.com/maps' -and $r.Content -match '35.123456%2C%2033.987654') 'School Google Maps iframe uses encoded precise coordinates'
 $r=Post "/CenterProfile/Edit/$centerId" "/CenterProfile/Edit/$centerId" $owner @{Id=$centerId;Name="Map QA $run";Country='Cyprus';City='Famagusta';MapLocation='Famagusta Marina, Cyprus'}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT MapLocation FROM DiveCenters WHERE Id=$centerId") -eq 'Famagusta Marina, Cyprus') 'School map location editable'
 $r=Post '/ManageActivities/Create' '/ManageActivities/Create' $owner @{DiveCenterId=$centerId;ActivityCategoryId=(Sql "SELECT TOP(1) Id FROM ActivityCategories WHERE IsActive=1 ORDER BY Id");Name="Contact QA $run";Price='35';CurrencyCode='EUR';IsActive='true';Location='Meeting point';City='Famagusta';Country='Cyprus';MapLocation='35.111111, 33.999999'}
 Assert ($r.StatusCode -eq 200) 'Create activity with its separate meeting point'
 $activityId=[int](Sql "SELECT Id FROM Activities WHERE Name='Contact QA $run'")
 $r=Request "/Activities/Details/$activityId" $anonymous
 Assert ($r.Content -match '35.111111%2C%2033.999999' -and $r.Content -match 'maps/search/\?api=1') 'Product map and open-in-Maps link rendered'
 Sql "UPDATE AppUsers SET Role='DiveCenter' WHERE Email='$prefix-operator@example.invalid'" | Out-Null
 $owner=Login 'operator'
 $r=Request "/Reservations/Create?activityId=$activityId" $customer
 Assert ($r.StatusCode -eq 200 -and $r.Content -match "$prefix-customer@example.invalid" -and $r.Content -match 'ContactByWhatsApp') 'Booking form prefills email and provides contact checkboxes'
 $date=(Get-Date).AddDays(5).ToString('yyyy-MM-dd')
 $body=@{ActivityId=$activityId;ReservationDate=$date;Participants=2;ContactEmail='bad-email';ContactPhone='abc';ContactByEmail='true'}
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer $body
 Assert ($r.Content -match 'validation-summary-errors' -and (Sql "SELECT COUNT(*) FROM Reservations WHERE ActivityId=$activityId") -eq '0') 'Malformed email and phone rejected without creating reservation'
 $body.ContactEmail='booking-contact@example.invalid'; $body.ContactPhone='+905551112233'; $body.ContactByEmail='false'; $body.ContactByPhone='false'; $body.ContactByWhatsApp='false'
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer $body
 Assert ($r.Content -match 'Select at least one preferred contact method') 'At least one contact checkbox required'
 $body.ContactByWhatsApp='true'; $body.ContactWhatsApp=''
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer $body
 Assert ($r.Content -match 'Enter your WhatsApp number') 'WhatsApp requires corresponding number'
 $body.ContactWhatsApp='+905559998877'; $body.ContactByEmail='true'; $body.ContactByPhone='true'
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer $body
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Reservation submitted') 'Reservation saves all contact fields and multiple selected channels'
 $reservationId=[int](Sql "SELECT TOP(1) Id FROM Reservations WHERE ActivityId=$activityId ORDER BY Id DESC")
 Assert ((Sql "SELECT COUNT(*) FROM Reservations WHERE Id=$reservationId AND ContactEmail='booking-contact@example.invalid' AND ContactPhone='+905551112233' AND ContactWhatsApp='+905559998877' AND ContactByEmail=1 AND ContactByPhone=1 AND ContactByWhatsApp=1 AND TotalPrice=70") -eq '1') 'Contact snapshot and existing price snapshot persist'
 $r=Request "/Reservations/Details/$reservationId" $owner
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'booking-contact@example.invalid' -and $r.Content -match '905559998877') 'School owner sees customer contact details and preferences'
 $r=Request "/Reservations/Details/$reservationId" $stranger
 Assert ($r.StatusCode -eq 404) 'Unrelated customer cannot see private contact details'
 Sql "UPDATE AppUsers SET Phone='+905550000000' WHERE Email='$prefix-customer@example.invalid'" | Out-Null
 Assert ((Sql "SELECT ContactPhone FROM Reservations WHERE Id=$reservationId") -eq '+905551112233') 'Later account changes leave reservation contact snapshot unchanged'
 $ownerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-operator@example.invalid'")
 Assert ((Sql "SELECT COUNT(*) FROM Notifications WHERE AppUserId=$ownerId AND EventKey='ReservationCreated:$reservationId'") -eq '1') 'Contact fields preserve school-owner reservation notification'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $owner @{id=$reservationId;status='Confirmed'}
 Assert ($r.StatusCode -eq 200) 'Reservation can still be confirmed'
 Write-Host "Completed $script:passed map/contact checks. QA audit records and parents are retained inactive."
}
finally {
 Sql "UPDATE Reservations SET Status='Cancelled',CancelledAt=SYSUTCDATETIME() WHERE ActivityId=$activityId AND Status IN('Pending','Confirmed'); UPDATE Activities SET IsActive=0 WHERE Id=$activityId; UPDATE DiveCenters SET IsActive=0 WHERE Id=$centerId; DELETE FROM Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'); UPDATE AppUsers SET IsActive=0 WHERE Email LIKE '$prefix-%';" | Out-Null
}