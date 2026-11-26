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
try {
 $customer=Register 'customer'; $admin=Register 'admin'; $owner=Register 'owner'
 Sql "UPDATE AppUsers SET Role=CASE WHEN Email='$prefix-admin@example.invalid' THEN 'Admin' ELSE 'DiveCenter' END WHERE Email IN('$prefix-admin@example.invalid','$prefix-owner@example.invalid')" | Out-Null
 $admin=Login 'admin'; $owner=Login 'owner'
 foreach($session in @($customer,$owner,$admin)) {
  $r=Request '/Account/Profile' $session
  Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Save profile') 'Every role has a personal profile'
 }
 $r=Post '/Account/Profile' '/Account/Profile' $customer @{FirstName='Updated';LastName='Customer';Email="$prefix-customer@example.invalid";Phone='+905551234567';Role='Admin';Id='1'}
 Assert ($r.Content -match 'Your profile has been updated' -and $r.Content -match 'Updated') 'Customer can update personal information'
 Assert ((Sql "SELECT Role FROM AppUsers WHERE Email='$prefix-customer@example.invalid'") -eq 'Customer') 'Posted role and id cannot change privileges'
 $r=Post '/Account/Profile' '/Account/Profile' $customer @{FirstName='Updated';LastName='Customer';Email="$prefix-admin@example.invalid"}
 Assert ($r.Content -match 'already in use') 'Duplicate profile email rejected'
 $r=Request '/Admin/Owners' $admin
 Assert ($r.StatusCode -eq 200 -and $r.Content -match "$prefix-owner@example.invalid" -and $r.Content -notmatch "$prefix-customer@example.invalid") 'Admin school owner list filters by role'
 foreach($route in '/Admin/Users','/ManageActivities','/CenterProfile','/Reservations') {
  $r=Request $route $admin
  Assert ($r.StatusCode -eq 200 -and $r.Content -match 'aria-label="Management"' -and $r.Content -match 'School owners') "Persistent admin management navigation on $route"
 }
 $r=Request '/CenterProfile' $owner
 Assert ($r.Content -match 'aria-label="Management"' -and $r.Content -match 'Manage instructors') 'Persistent owner navigation'
 $customerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid'")
 Sql "INSERT Notifications(AppUserId,EventKey,Title,Message,LinkUrl,IsRead) VALUES($customerId,'$prefix-old','Old QA','Old','/Account/Profile',1),($customerId,'$prefix-new','Fresh QA','New','/Account/Profile',0)" | Out-Null
 $r=Request '/Notifications' $customer
 Assert ($r.Content -match 'Fresh QA' -and $r.Content -notmatch 'Old QA' -and $r.Content -match 'notification-list') 'Unread inbox hides old read items and supports live refresh'
 $r=Request '/Notifications/List' $customer
 Assert ($r.Content -match 'Fresh QA' -and $r.Content -notmatch 'Old QA') 'Live notification partial shows latest unread'
 $r=Request '/Notifications?history=true' $customer
 Assert ($r.Content -match 'Old QA' -and $r.Content -match 'Fresh QA') 'History preserves past notifications'
 $r=Request '/Admin/Owners' $customer
 Assert ($r.StatusCode -eq 403) 'Customer cannot read owner management'
 $anon=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $r=Post '/Account/RegisterOwner' '/Account/RegisterOwner' $anon @{FirstName='QA';LastName='map';Email="$prefix-map@example.invalid";Password=$password;ConfirmPassword=$password;CenterName='QA map';Country='Cyprus';City='Famagusta';Website='www.example.com';MapLocation='https://maps.app.goo.gl/test'}
 Assert ($r.Content -match 'application' -and (Sql "SELECT COUNT(*) FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId WHERE u.Email='$prefix-map@example.invalid' AND Website='https://www.example.com' AND MapLocation='https://maps.app.goo.gl/test'") -eq '1') 'Owner registration accepts domain website and stores map link'
 $applicationId=[int](Sql "SELECT a.Id FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId WHERE u.Email='$prefix-map@example.invalid'")
 $r=Post "/Admin/Application/$applicationId" '/Admin/ReviewApplication' $admin @{id=$applicationId;approve='true'}
 Assert ((Sql "SELECT COUNT(*) FROM DiveCenters d JOIN DiveCenterApplications a ON a.DiveCenterId=d.Id WHERE a.Id=$applicationId AND d.MapLocation='https://maps.app.goo.gl/test'") -eq '1') 'Approval transfers map location to school profile'
 $invalid=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $r=Post '/Account/RegisterOwner' '/Account/RegisterOwner' $invalid @{FirstName='QA';LastName='invalid';Email="$prefix-invalid@example.invalid";Password=$password;ConfirmPassword=$password;CenterName='Invalid';Country='Cyprus';City='Famagusta';MapLocation='Cyprus';Website='javascript:alert(1)'}
 Assert ((Sql "SELECT COUNT(*) FROM AppUsers WHERE Email='$prefix-invalid@example.invalid'") -eq '0') 'Unsafe website scheme rejected before account creation'
 $r=Post '/CenterProfile/Create' '/CenterProfile/Create' $admin @{Name='QA Missing map'}
 Assert ($r.Content -match 'Enter the school Google Maps link') 'School creation rejects missing location'
 $r=Request '/CenterProfile/Create' $admin
 Assert ($r.Content -match 'Google Maps share link') 'School creation asks for Google Maps link'
 Write-Host "Completed $script:passed profile, navigation and notification checks."
}
finally {
 Sql "DELETE Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') OR EventKey IN(SELECT 'ApplicationSubmitted:'+CAST(a.Id AS varchar) FROM DiveCenterApplications a JOIN AppUsers u ON u.Id=a.AppUserId WHERE u.Email LIKE '$prefix-%'); DELETE DiveCenterUsers WHERE DiveCenterId IN(SELECT DiveCenterId FROM DiveCenterApplications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%')); DECLARE @centers TABLE(Id int); INSERT @centers SELECT DiveCenterId FROM DiveCenterApplications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') AND DiveCenterId IS NOT NULL; DELETE DiveCenterApplications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'); DELETE DiveCenters WHERE Id IN(SELECT Id FROM @centers); DELETE AppUsers WHERE Email LIKE '$prefix-%';" | Out-Null
}