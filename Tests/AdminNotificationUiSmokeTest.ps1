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
$applicationId=0; $centerId=0
try {
 $admin=Register 'admin'; $owner=Register 'owner'; $customer=Register 'customer'
 Sql "UPDATE AppUsers SET Role=CASE WHEN Email='$prefix-admin@example.invalid' THEN 'Admin' ELSE 'DiveCenter' END WHERE Email IN('$prefix-admin@example.invalid','$prefix-owner@example.invalid')" | Out-Null
 $admin=Login 'admin'; $owner=Login 'owner'
 $adminId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-admin@example.invalid'")
 $ownerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-owner@example.invalid'")
 # Use only an existing QA audit reservation; never inspect real customer contact data.
 $reservationId=[int](Sql "SELECT TOP(1) r.Id FROM Reservations r JOIN AppUsers u ON u.Id=r.CustomerUserId WHERE u.Email LIKE 'qa-%@example.invalid' AND r.ContactByEmail=1 ORDER BY r.Id DESC")
 $centerId=[int](Sql "SELECT DiveCenterId FROM Reservations WHERE Id=$reservationId")
 Sql "INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) VALUES($centerId,$ownerId,1)" | Out-Null
 $r=Request '/Reservations' $owner
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Customer / contact preference' -and $r.Content -match 'Contact details') 'School reservation list exposes contact preference column'
 $r=Request "/Reservations/Details/$reservationId" $owner
 Assert ($r.Content -match 'Customer communication' -and $r.Content -match 'preferred' -and $r.Content -match 'mailto:') 'School reservation details highlight preferred contact methods and contact links'
 $r=Request '/Dashboard' $owner
 Assert ($r.Content -match 'Customer / contact preference') 'Owner dashboard includes communication preference'
 $r=Request "/Reservations/Details/$reservationId" $customer
 Assert ($r.StatusCode -eq 404) 'Unrelated customer cannot access QA customer contact details'
 $r=Request '/Notifications/Indicator' $admin
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'data-unread-count="0"') 'Authenticated partial initially reports zero unread'
 $applicant=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $r=Post '/Account/RegisterOwner' '/Account/RegisterOwner' $applicant @{FirstName='QA';LastName='applicant';Email="$prefix-applicant@example.invalid";Password=$password;ConfirmPassword=$password;CenterName="Admin alert QA $run";Country='Cyprus';City='Famagusta'}
 Assert ($r.StatusCode -eq 200) 'Owner application generates an admin notification'
 $applicationId=[int](Sql "SELECT Id FROM DiveCenterApplications WHERE Name='Admin alert QA $run'")
 $r=Request '/Notifications/Indicator' $admin
 Assert ($r.Content -match 'data-unread-count="1"' -and $r.Content -match 'text-bg-danger' -and $r.Content -match 'has-unread') 'New notification indicator renders red with unread count'
 Assert ($r.Content -match 'data-pending-applications="1"') 'Live indicator carries eligible pending application count'
 $r=Request '/Admin' $admin
 Assert ($r.Content -match 'Show owner applications' -and $r.Content -match 'alert alert-danger' -and $r.Content -match 'waiting for your review') 'Administration displays pending owner application as a red alert'
 $r=Request '/Notifications' $admin
 Assert ($r.Content -match 'notification-unread' -and $r.Content -match 'text-bg-danger') 'Unread inbox items are red'
 $r=Request '/Notifications/Indicator' $customer
 Assert ($r.Content -match 'data-unread-count="0"' -and $r.Content -match 'data-pending-applications="0"') 'Customer indicator cannot see admin notification counts'
 $anonymous=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $r=Request '/Notifications/Indicator' $anonymous
 Assert ($r.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/Login') 'Anonymous partial access redirects to login'
 foreach($route in '/Admin','/Admin/Users','/Admin/Centers','/Admin/Categories','/Admin/Applications','/Admin/AssignCenterUser','/ManageActivities','/Reservations') {
  $r=Request $route $admin; Assert ($r.StatusCode -eq 200) "Admin panel route $route"
 }
 $r=Request '/Admin' $owner
 Assert ($r.StatusCode -eq 403) 'School owner cannot access administration'
 $r=Post '/Notifications' '/Notifications/MarkAllRead' $admin @{}
 $r=Request '/Notifications/Indicator' $admin
 Assert ($r.Content -match 'data-unread-count="0"' -and $r.Content -notmatch 'has-unread') 'Reading notifications clears red unread indicator'
 Assert ($r.Content -match 'data-pending-applications="1"') 'Reading notification does not hide an application awaiting review'
 $r=Post "/Admin/Application/$applicationId" '/Admin/ReviewApplication' $admin @{id=$applicationId;approve='false';notes='QA alert test complete'}
 $r=Request '/Notifications/Indicator' $admin
 Assert ($r.Content -match 'data-pending-applications="0"') 'Reviewed owner application clears pending count'
 Write-Host "Completed $script:passed UI and admin checks. Temporary memberships/accounts are removed; application audit is retained inactive."
}
finally {
 Sql "DELETE FROM Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') OR EventKey='ApplicationSubmitted:$applicationId'; DELETE FROM DiveCenterUsers WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'); UPDATE AppUsers SET IsActive=0 WHERE Email LIKE '$prefix-%'; DELETE FROM AppUsers WHERE Email LIKE '$prefix-%' AND NOT EXISTS(SELECT 1 FROM DiveCenterApplications WHERE AppUserId=AppUsers.Id OR ReviewedByUserId=AppUsers.Id) AND NOT EXISTS(SELECT 1 FROM Reservations WHERE CustomerUserId=AppUsers.Id);" | Out-Null
}