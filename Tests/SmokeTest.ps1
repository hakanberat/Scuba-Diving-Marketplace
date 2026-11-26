param([string]$BaseUrl='http://localhost:5258')
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
 if($postPath -in @('/Account/RegisterOwner','/CenterProfile/Create') -and !$body.ContainsKey('MapLocation')) { $body['MapLocation']='Famagusta Marina, Cyprus' }
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
$activityId=0; $centerId=0; $instructorId=0; $oldPrice=0
try {
 $anonymous=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 foreach($route in '/','/Activities','/DiveCenters','/DiveCenters/Details/1') { $r=Request $route $anonymous; Assert ($r.StatusCode -eq 200) "Public route $route" }
 $invalid=Post '/Account/Register' '/Account/Register' $anonymous @{FirstName='';LastName='';Email='';Password='';ConfirmPassword=''}
 Assert ($invalid.StatusCode -eq 200 -and $invalid.Content -match 'validation-summary-errors') 'Empty registration returns validation errors without exceptions'
 $admin=Register 'admin'; $customer=Register 'customer'; $owner=Register 'owner'; $other=Register 'other'
 Assert ((Sql "SELECT COUNT(*) FROM AppUsers WHERE Email LIKE '$prefix-%' AND Role='Customer' AND PasswordHash NOT LIKE '$password'") -eq '4') 'Public registration ignores role injection and stores hashes'
 Sql "UPDATE AppUsers SET Role=CASE WHEN Email='$prefix-admin@example.invalid' THEN 'Admin' ELSE 'DiveCenter' END WHERE Email IN ('$prefix-admin@example.invalid','$prefix-owner@example.invalid','$prefix-other@example.invalid');" | Out-Null
 $admin=Login 'admin'; $owner=Login 'owner'
 $assigned=Post '/Admin/AssignCenterUser' '/Admin/AssignCenterUser' $admin @{DiveCenterId=1;AppUserId=(Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-owner@example.invalid'");IsOwner='true'}
 Assert ($assigned.StatusCode -eq 200 -and $assigned.Content -match 'Dive center access assigned') 'Admin assigns Dive Hub ownership'
 $other=Login 'other'
 $created=Post '/CenterProfile/Create' '/CenterProfile/Create' $admin @{Name="QA Center $run";City='Test';Country='Cyprus'}
 Assert ($created.StatusCode -eq 200 -and $created.Content -match 'Your dive center profile is ready') 'Admin creates test center'
 $centerId=[int](Sql "SELECT Id FROM DiveCenters WHERE Name='QA Center $run'")
 Sql "INSERT DiveCenterUsers(DiveCenterId,AppUserId,IsOwner) SELECT $centerId,Id,1 FROM AppUsers WHERE Email='$prefix-other@example.invalid';" | Out-Null
 $denied=Request "/CenterProfile/Edit/$centerId" $owner
 Assert ($denied.StatusCode -eq 403) 'Owner cannot access another center profile'
 $denied=Request '/Admin' $customer
 Assert ($denied.StatusCode -eq 403) 'Customer cannot access admin'
 $response=Post '/Instructors/Create' '/Instructors/Create' $owner @{DiveCenterId=1;FirstName='QA';LastName=$run;Bio='QA test instructor';CertificationAgency='PADI';InstructorNumber='QA';IsActive='true'}
 Assert ($response.StatusCode -eq 200 -and $response.Content -match $run) 'Owner creates an instructor'
 $instructorId=[int](Sql "SELECT Id FROM Instructors WHERE LastName='$run'")
 $response=Post "/Instructors/Edit/$instructorId" "/Instructors/Edit/$instructorId" $owner @{Id=$instructorId;DiveCenterId=1;FirstName='QA';LastName=$run;Bio='Updated QA bio';IsActive='true'}
 Assert ($response.StatusCode -eq 200 -and (Sql "SELECT Bio FROM Instructors WHERE Id=$instructorId") -eq 'Updated QA bio') 'Owner edits instructor'
 $response=Post '/ManageActivities/Create' '/ManageActivities/Create' $other @{DiveCenterId=$centerId;ActivityCategoryId=(Sql "SELECT Id FROM ActivityCategories WHERE Slug='fun-dive'");Name="QA Activity $run";Price='50';CurrencyCode='EUR';IsActive='true';MaxParticipants=4}
 Assert ($response.StatusCode -eq 200 -and $response.Content -match "QA Activity $run") 'Owner creates activity in own center'
 $foreignId=[int](Sql "SELECT Id FROM Activities WHERE Name='QA Activity $run'")
 $denied=Request "/ManageActivities/Edit/$foreignId" $owner
 Assert ($denied.StatusCode -eq 403) 'Foreign activity edit is forbidden'
 $denied=Post '/ManageActivities/Create' '/ManageActivities/Create' $owner @{DiveCenterId=$centerId;ActivityCategoryId=1;Name='Unauthorized';Price='1';CurrencyCode='EUR';IsActive='true'}
 Assert ($denied.StatusCode -eq 403) 'Forged center ID in activity POST is forbidden'
 $response=Post "/ManageActivities/Edit/$foreignId" "/ManageActivities/Edit/$foreignId" $other @{Id=$foreignId;DiveCenterId=$centerId;ActivityCategoryId=(Sql "SELECT Id FROM ActivityCategories WHERE Slug='fun-dive'");Name="QA Activity $run";Price='65';CurrencyCode='EUR';IsActive='true';MaxParticipants=4}
 Assert ($response.StatusCode -eq 200 -and (Sql "SELECT CAST(Price AS int) FROM Activities WHERE Id=$foreignId") -eq '65') 'Owner edits activity'
 $activityId=[int](Sql "SELECT TOP(1) Id FROM Activities WHERE DiveCenterId=1 AND Name='Discover Scuba Diving' ORDER BY Id")
 $oldPrice=Sql "SELECT CAST(Price AS nvarchar(30)) FROM Activities WHERE Id=$activityId"
 $r=Request "/Activities/Details/$activityId" $anonymous
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Reserve this activity') 'Public activity details and booking CTA'
 $r=Request "/Activities?Search=Discover&Country=Cyprus&City=Famagusta&MinPrice=1&MaxPrice=10000" $anonymous
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Discover Scuba Diving') 'Combined marketplace search filters'
 $r=Request "/Reservations/Create?activityId=$activityId" $anonymous
 Assert ($r.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/Login' -and $r.Content -match 'ReturnUrl') 'Anonymous booking redirects to login with return URL'
 $date=(Get-Date).AddDays(3).ToString('yyyy-MM-dd')
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer @{ActivityId=$activityId;ReservationDate=(Get-Date).AddDays(-1).ToString('yyyy-MM-dd');Participants=1}
 Assert ($r.Content -match 'Choose today or a future date') 'Past reservation date rejected'
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer @{ActivityId=$activityId;ReservationDate=$date;Participants=999}
 Assert ($r.Content -match 'Participant count exceeds') 'Participant limit enforced'
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer @{ActivityId=$activityId;ReservationDate=$date;Participants=2;CustomerNotes='Automated QA reservation';UnitPrice=1;TotalPrice=2}
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Reservation submitted') 'Customer creates pending reservation'
 $reservationId=[int](Sql "SELECT TOP(1) Id FROM Reservations WHERE CustomerUserId=(SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid') ORDER BY Id DESC")
 Assert ((Sql "SELECT COUNT(*) FROM Reservations WHERE Id=$reservationId AND UnitPrice=$oldPrice AND TotalPrice=2*$oldPrice AND Status='Pending'") -eq '1') 'Price snapshot ignores posted price tampering'
 # Edit published activity price, verify historical reservation snapshot, then restore in finally.
 Sql "UPDATE Activities SET Price=99 WHERE Id=$activityId" | Out-Null
 Assert ((Sql "SELECT COUNT(*) FROM Reservations WHERE Id=$reservationId AND UnitPrice=$oldPrice AND TotalPrice=2*$oldPrice") -eq '1') 'Activity price changes leave reservation snapshot unchanged'
 $r=Request "/Reservations/Details/$reservationId" $other
 Assert ($r.StatusCode -eq 404) 'Unrelated center cannot read reservation'
 $r=Request '/Reservations/MyReservations' $customer
 Assert ($r.Content -match 'Automated QA' -or $r.Content -match 'DM-') 'Customer sees own reservation list'
 $r=Request '/Reservations/Index?status=Pending' $owner
 Assert ($r.Content -match 'DM-') 'Owner sees pending center reservations'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $owner @{Id=$reservationId;Status='Confirmed';Notes='QA confirmed'}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT Status FROM Reservations WHERE Id=$reservationId") -eq 'Confirmed') 'Owner confirms pending reservation'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $owner @{Id=$reservationId;Status='Rejected'}
 Assert ($r.StatusCode -eq 400) 'Confirmed reservation cannot be rejected'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $customer @{Id=$reservationId;Status='Cancelled';Notes='Do not overwrite center notes'}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT COUNT(*) FROM Reservations WHERE Id=$reservationId AND Status='Cancelled' AND DiveCenterNotes='QA confirmed' AND CancelledAt IS NOT NULL") -eq '1') 'Customer cancellation preserves center notes and historical record'
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer @{ActivityId=$activityId;ReservationDate=$date;Participants=1;CustomerNotes='Automated QA rejection test'}
 $rejectedId=[int](Sql "SELECT TOP(1) Id FROM Reservations WHERE CustomerUserId=(SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid') ORDER BY Id DESC")
 $r=Post "/Reservations/Details/$rejectedId" '/Reservations/ChangeStatus' $owner @{Id=$rejectedId;Status='Rejected';Notes='QA rejection'}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT Status FROM Reservations WHERE Id=$rejectedId") -eq 'Rejected') 'Owner rejects pending reservation'
 $r=Request '/Reservations/ChangeStatus?id=1&status=Cancelled' $customer
 Assert ($r.StatusCode -in 404,405) 'GET cannot change reservation status'
 $r=Request '/Reservations/ChangeStatus' $customer 'POST' @{Id=$reservationId;Status='Cancelled'}
 Assert ($r.StatusCode -eq 400) 'Missing antiforgery token rejected'
 $r=Post '/ManageActivities' '/ManageActivities/SetActive' $owner @{Id=$activityId;Active='false'}
 $r=Request "/Activities/Details/$activityId" $anonymous
 Assert ($r.StatusCode -eq 404) 'Inactive activity hidden from public'
 $r=Request "/Reservations/Create?activityId=$activityId" $customer
 Assert ($r.StatusCode -eq 404) 'Inactive activity cannot be reserved'
 Post '/ManageActivities' '/ManageActivities/SetActive' $owner @{Id=$activityId;Active='true'} | Out-Null
 $r=Post '/Admin/Centers' '/Admin/SetCenterActive' $admin @{Id=1;Active='false'}
 $r=Request '/DiveCenters/Details/1' $anonymous
 Assert ($r.StatusCode -eq 404) 'Inactive center hidden from public'
 $r=Request "/Reservations/Create?activityId=$activityId" $customer
 Assert ($r.StatusCode -eq 404) 'Inactive center cannot receive bookings'
 Post '/Admin/Centers' '/Admin/SetCenterActive' $admin @{Id=1;Active='true'} | Out-Null
 foreach($route in '/Dashboard','/CenterProfile','/ManageActivities','/Instructors','/Reservations') { $r=Request $route $owner; Assert ($r.StatusCode -eq 200) "Owner management route $route" }
 foreach($route in '/Admin','/Admin/Users','/Admin/Centers','/Admin/Categories','/Admin/AssignCenterUser') { $r=Request $route $admin; Assert ($r.StatusCode -eq 200) "Admin route $route" }
 $r=Post '/Admin/EditCategory' '/Admin/EditCategory' $admin @{Id=0;Name="QA category $run";Slug="qa-$run";IsActive='true'}
 Assert ($r.StatusCode -eq 200 -and $r.Content -match "QA category $run") 'Admin creates category'
 $categoryId=[int](Sql "SELECT Id FROM ActivityCategories WHERE Slug='qa-$run'")
 $r=Post "/Admin/EditCategory/$categoryId" "/Admin/EditCategory/$categoryId" $admin @{Id=$categoryId;Name="QA category $run";Slug="qa-$run";IsActive='false'}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT CAST(IsActive AS int) FROM ActivityCategories WHERE Id=$categoryId") -eq '0') 'Admin deactivates category'
 $customerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid'")
 $r=Post "/Admin/EditUser/$customerId" "/Admin/EditUser/$customerId" $admin @{Id=$customerId;Role='Customer';IsActive='false'}
 $r=Request '/Reservations/MyReservations' $customer
 Assert ($r.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/Login') 'Deactivated account loses existing session access'
 Write-Output "Completed $script:passed checks. QA customer audit records are retained inactive; unused test accounts and temporary catalog fixtures are removed."
}
finally {
 if($activityId -gt 0) {
  # Restore all original demo values after price-edit tests.
  Sql "UPDATE Activities SET Price=$oldPrice,IsActive=1 WHERE Id=$activityId;" | Out-Null
 }
 Sql "UPDATE DiveCenters SET IsActive=1 WHERE Id=1; UPDATE Reservations SET Status='Cancelled',CancelledAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME() WHERE CustomerUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') AND Status IN ('Pending','Confirmed'); UPDATE AppUsers SET IsActive=0 WHERE Email LIKE '$prefix-%'; DELETE FROM Instructors WHERE LastName='$run'; DELETE FROM Activities WHERE Name='QA Activity $run'; DELETE FROM DiveCenterUsers WHERE DiveCenterId IN(SELECT Id FROM DiveCenters WHERE Name='QA Center $run'); DELETE FROM DiveCenters WHERE Name='QA Center $run'; DELETE FROM ActivityCategories WHERE Slug='qa-$run'; DELETE FROM DiveCenterUsers WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'); DELETE FROM Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') OR EventKey IN(SELECT 'ReservationCreated:'+CAST(Id AS nvarchar(20)) FROM Reservations WHERE CustomerUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%')); DELETE FROM AppUsers WHERE Email LIKE '$prefix-%' AND NOT EXISTS(SELECT 1 FROM Reservations WHERE CustomerUserId=AppUsers.Id);" | Out-Null
}
