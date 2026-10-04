param([string]$BaseUrl='http://localhost:5263')
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
$applicationId=0; $centerId=0; $activityId=0; $reservationId=0
try {
 $admin=Register 'admin'; $customer=Register 'customer'; $stranger=Register 'stranger'
 Sql "UPDATE AppUsers SET Role='Admin' WHERE Email='$prefix-admin@example.invalid'" | Out-Null
 $admin=Login 'admin'
 $adminId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-admin@example.invalid'")
 $owner=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $form=@{FirstName='QA';LastName='owner';Email="$prefix-owner@example.invalid";Password=$password;ConfirmPassword=$password;CenterName="Application QA $run";Country='Cyprus';City='Famagusta';Phone='123';Role='Admin'}
 $r=Post '/Account/RegisterOwner' '/Account/RegisterOwner' $owner $form
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'awaiting admin review') 'Separate owner form submits a pending application'
 $applicationId=[int](Sql "SELECT Id FROM DiveCenterApplications WHERE Name='Application QA $run'")
 $ownerId=[int](Sql "SELECT AppUserId FROM DiveCenterApplications WHERE Id=$applicationId")
 Assert ((Sql "SELECT Role FROM AppUsers WHERE Id=$ownerId") -eq 'DiveCenter') 'Owner role fixed server-side despite role injection'
 Assert ((Sql "SELECT COUNT(*) FROM DiveCenterUsers WHERE AppUserId=$ownerId") -eq '0') 'Pending owner has no center management mapping'
 Assert ((Sql "SELECT COUNT(*) FROM DiveCenters WHERE Name='Application QA $run'") -eq '0') 'Pending school is not published'
 $r=Request '/Dashboard' $owner
 Assert ($r.BaseResponse.ResponseUri.AbsolutePath -eq '/Account/ApplicationStatus') 'Pending owner dashboard redirects to application status'
 $r=Request '/CenterProfile/Create' $customer
 Assert ($r.StatusCode -eq 403) 'Customer cannot bypass owner approval through center creation'
 $r=Post '/Account/Login' '/CenterProfile/Create' $owner @{Name='Bypass test'}
 Assert ($r.StatusCode -eq 403) 'Pending owner cannot publish directly'
 $r=Request '/Notifications' $admin
 Assert ($r.Content -match 'New dive center application' -and $r.Content -match "Application QA $run") 'Admin receives owner application notification'
 $r=Request '/Admin/Applications' $admin
 Assert ($r.StatusCode -eq 200 -and $r.Content -match "Application QA $run") 'Admin application queue shows new school'
 $r=Request "/Admin/Application/$applicationId" $customer
 Assert ($r.StatusCode -eq 403) 'Customer cannot review school applications'
 $r=Post "/Admin/Application/$applicationId" '/Admin/ReviewApplication' $admin @{id=$applicationId;approve='true';notes='QA approval'}
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Application approved') 'Admin approves owner application'
 $centerId=[int](Sql "SELECT DiveCenterId FROM DiveCenterApplications WHERE Id=$applicationId")
 Assert ((Sql "SELECT COUNT(*) FROM DiveCenterUsers WHERE DiveCenterId=$centerId AND AppUserId=$ownerId AND IsOwner=1") -eq '1') 'Approval creates school and owner membership atomically'
 $r=Post "/Admin/Application/$applicationId" '/Admin/ReviewApplication' $admin @{id=$applicationId;approve='true'}
 Assert ($r.StatusCode -eq 400 -and (Sql "SELECT COUNT(*) FROM DiveCenters WHERE Name='Application QA $run'") -eq '1') 'Repeated approval rejected without duplicate school'
 $r=Request '/Notifications' $owner
 Assert ($r.Content -match 'Your dive center application was approved') 'Owner receives approval notification'
 $r=Request '/Dashboard' $owner
 Assert ($r.StatusCode -eq 200 -and $r.BaseResponse.ResponseUri.AbsolutePath -eq '/Dashboard') 'Approved owner dashboard enabled without another login'
 $r=Post '/ManageActivities/Create' '/ManageActivities/Create' $owner @{DiveCenterId=$centerId;ActivityCategoryId=(Sql "SELECT TOP(1) Id FROM ActivityCategories WHERE IsActive=1 ORDER BY Id");Name="Notification QA Activity $run";Price='30';CurrencyCode='EUR';MaxParticipants=4;IsActive='true'}
 Assert ($r.StatusCode -eq 200) 'Approved owner publishes own activity'
 $activityId=[int](Sql "SELECT Id FROM Activities WHERE Name='Notification QA Activity $run'")
 $r=Post "/Reservations/Create?activityId=$activityId" '/Reservations/Create' $customer @{ActivityId=$activityId;ReservationDate=(Get-Date).AddDays(4).ToString('yyyy-MM-dd');Participants=2;CustomerNotes='Notification workflow QA'}
 Assert ($r.StatusCode -eq 200 -and $r.Content -match 'Reservation submitted') 'Customer reserves approved school activity'
 $reservationId=[int](Sql "SELECT TOP(1) Id FROM Reservations WHERE ActivityId=$activityId ORDER BY Id DESC")
 Assert ((Sql "SELECT COUNT(*) FROM Notifications WHERE AppUserId=$ownerId AND EventKey='ReservationCreated:$reservationId' AND IsRead=0") -eq '1') 'School owner receives one unread reservation notification'
 $r=Request '/Notifications' $owner
 Assert ($r.Content -match 'New reservation request' -and $r.Content -match "Notification QA Activity $run") 'Owner notification contains activity and reservation detail link'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $owner @{id=$reservationId;status='Confirmed';notes='QA confirmation'}
 Assert ($r.StatusCode -eq 200) 'Owner confirms reservation'
 $customerId=[int](Sql "SELECT Id FROM AppUsers WHERE Email='$prefix-customer@example.invalid'")
 Assert ((Sql "SELECT COUNT(*) FROM Notifications WHERE AppUserId=$customerId AND EventKey='ReservationStatus:${reservationId}:Confirmed'") -eq '1') 'Confirmation sends customer notification'
 $r=Request '/Notifications' $customer
 Assert ($r.Content -match 'Your reservation is now confirmed') 'Customer sees reservation confirmation'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $owner @{id=$reservationId;status='Confirmed'}
 Assert ($r.StatusCode -eq 400 -and (Sql "SELECT COUNT(*) FROM Notifications WHERE AppUserId=$customerId AND EventKey='ReservationStatus:${reservationId}:Confirmed'") -eq '1') 'Repeated confirmation creates no duplicate notification'
 $notificationId=[int](Sql "SELECT TOP(1) Id FROM Notifications WHERE AppUserId=$customerId ORDER BY Id DESC")
 $r=Post '/Notifications' '/Notifications/MarkRead' $stranger @{id=$notificationId}
 Assert ($r.StatusCode -eq 404) 'Unrelated user cannot mark another user notification'
 $r=Request '/Notifications' $stranger
 Assert ($r.Content -notmatch 'Your reservation is now confirmed' -and $r.Content -notmatch 'New dive center application') 'Notification inbox is scoped to the logged-in user'
 $r=Post '/Notifications' '/Notifications/MarkRead' $customer @{id=$notificationId}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT CAST(IsRead AS int) FROM Notifications WHERE Id=$notificationId") -eq '1') 'Customer marks own notification read'
 $r=Request '/Notifications/MarkAllRead' $customer 'POST' @{}
 Assert ($r.StatusCode -eq 400) 'Notification changes require antiforgery token'
 $rejected=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $form.Email="$prefix-rejected@example.invalid"; $form.CenterName="Rejected QA $run"
 $r=Post '/Account/RegisterOwner' '/Account/RegisterOwner' $rejected $form
 $rejectedId=[int](Sql "SELECT Id FROM DiveCenterApplications WHERE Name='Rejected QA $run'")
 $r=Post "/Admin/Application/$rejectedId" '/Admin/ReviewApplication' $admin @{id=$rejectedId;approve='false';notes='QA rejection'}
 Assert ($r.StatusCode -eq 200) 'Admin rejects a school application'
 $r=Request '/Account/ApplicationStatus' $rejected
 Assert ($r.Content -match 'Rejected' -and $r.Content -match 'QA rejection') 'Rejected owner can see review notes'
 Assert ((Sql "SELECT COUNT(*) FROM DiveCenters WHERE Name='Rejected QA $run'") -eq '0') 'Rejected school never published'
 $r=Request '/Notifications' $rejected
 Assert ($r.Content -match 'application was rejected') 'Rejected owner receives review notification'
 $r=Post '/Notifications' '/Notifications/MarkAllRead' $admin @{}
 Assert ($r.StatusCode -eq 200 -and (Sql "SELECT COUNT(*) FROM Notifications WHERE AppUserId=$adminId AND IsRead=0") -eq '0') 'Mark all read is scoped to the admin inbox'
 $r=Post "/Reservations/Details/$reservationId" '/Reservations/ChangeStatus' $customer @{id=$reservationId;status='Cancelled'}
 Assert ($r.StatusCode -eq 200) 'Customer cancellation preserves workflow'
 Write-Host "Completed $script:passed application/notification checks. QA reservation audit records remain; QA catalog is deactivated."
}
finally {
 # Preserve reservation history and its foreign-key parents rather than deleting reservations.
 Sql "UPDATE Reservations SET Status='Cancelled',CancelledAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME() WHERE ActivityId=$activityId AND Status IN('Pending','Confirmed'); UPDATE Activities SET IsActive=0 WHERE Id=$activityId; UPDATE DiveCenters SET IsActive=0 WHERE Id=$centerId; UPDATE AppUsers SET IsActive=0 WHERE Email LIKE '$prefix-%'; DELETE FROM Notifications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%') OR EventKey IN(SELECT 'ApplicationSubmitted:'+CAST(Id AS nvarchar(20)) FROM DiveCenterApplications WHERE AppUserId IN(SELECT Id FROM AppUsers WHERE Email LIKE '$prefix-%'));" | Out-Null
}