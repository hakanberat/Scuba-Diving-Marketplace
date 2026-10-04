param([string]$BaseUrl='http://localhost:5261')
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
Add-Type -AssemblyName System.Net.Http
$centerId=0; $activityId=0; $uploadedUrl=$null
try {
 $owner=Register 'photos'; $stranger=Register 'stranger'
 Sql "UPDATE AppUsers SET Role='Admin' WHERE Email='$prefix-photos@example.invalid';" | Out-Null
 $owner=Login 'photos'
 $response=Post '/CenterProfile/Create' '/CenterProfile/Create' $owner @{Name="Photo QA $run";Country='Cyprus';City='Famagusta'}
 Assert ($response.StatusCode -eq 200) 'Create photo test center'
 $centerId=[int](Sql "SELECT Id FROM DiveCenters WHERE Name='Photo QA $run'")
 $response=Post '/ManageActivities/Create' '/ManageActivities/Create' $owner @{DiveCenterId=$centerId;ActivityCategoryId=(Sql "SELECT TOP(1) Id FROM ActivityCategories WHERE IsActive=1 ORDER BY Id");Name="Photo QA Activity $run";Price='20';CurrencyCode='EUR';IsActive='true'}
 Assert ($response.StatusCode -eq 200) 'Create photo test activity'
 $activityId=[int](Sql "SELECT Id FROM Activities WHERE Name='Photo QA Activity $run'")
 # Make the unrelated account a center operator with no membership of this center.
 Sql "UPDATE AppUsers SET Role='DiveCenter' WHERE Email='$prefix-stranger@example.invalid';" | Out-Null
 $stranger=Login 'stranger'
 $denied=Request "/Photos/Center/$centerId" $stranger
 Assert ($denied.StatusCode -eq 403) 'Foreign center photo manager forbidden'
 $denied=Request "/Photos/Activity/$activityId" $stranger
 Assert ($denied.StatusCode -eq 403) 'Foreign activity photo manager forbidden'
 $invalid=Post "/Photos/Center/$centerId" '/Photos/Add' $owner @{'Input.Target'='Center';'Input.ResourceId'=$centerId;'Input.ImageUrl'='javascript:alert(1)'}
 Assert ($invalid.StatusCode -eq 200 -and $invalid.Content -match 'Use an http or https') 'Unsafe image URL rejected'
 $empty=Post "/Photos/Center/$centerId" '/Photos/Add' $owner @{'Input.Target'='Center';'Input.ResourceId'=$centerId}
 Assert ($empty.Content -match 'Choose a photo file') 'Photo required validation'
 $added=Post "/Photos/Center/$centerId" '/Photos/Add' $owner @{'Input.Target'='Center';'Input.ResourceId'=$centerId;'Input.ImageUrl'='https://example.invalid/center.jpg';'Input.Caption'='<script>bad</script>'}
 Assert ($added.StatusCode -eq 200 -and $added.Content -match 'Photo added') 'Center gallery URL photo saved'
 $urlPhoto=[int](Sql "SELECT TOP(1) Id FROM Photos WHERE DiveCenterId=$centerId ORDER BY Id")
 $public=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $page=Request "/DiveCenters/Details/$centerId" $public
 Assert ($page.StatusCode -eq 200 -and $page.Content -match 'example.invalid/center.jpg' -and $page.Content -match '&lt;script&gt;') 'Public center gallery renders and encodes caption'
 $page=Request '/DiveCenters' $public
 Assert ($page.Content -match 'example.invalid/center.jpg') 'Center cover shown on discovery card'
 function Upload($bytes,$session,$target,$id,$count=1,$invalidLast=$false) {
  $page=Request "/Photos/$target/$id" $session
  $token=[regex]::Match($page.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
  $handler=New-Object System.Net.Http.HttpClientHandler; $handler.CookieContainer=$session.Cookies; $handler.AllowAutoRedirect=$false
  $client=New-Object System.Net.Http.HttpClient($handler)
  $form=New-Object System.Net.Http.MultipartFormDataContent
  foreach($entry in @{'Input.Target'=$target;'Input.ResourceId'="$id";'Input.Caption'='Uploaded PNG';'__RequestVerificationToken'=$token}.GetEnumerator()) {
   $value=New-Object System.Net.Http.StringContent([string]$entry.Value); $form.Add($value,[string]$entry.Key)
  }
  for($n=0;$n -lt $count;$n++) {
   $contentBytes=if($invalidLast -and $n -eq $count-1){[Text.Encoding]::UTF8.GetBytes('<html>not image</html>')}else{$bytes}
   $photo=New-Object System.Net.Http.ByteArrayContent -ArgumentList @(,$contentBytes)
   $photo.Headers.ContentType=[System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('image/png')
   $field=if($count -eq 1){'Input.File'}else{'Input.Files'}
   $form.Add($photo,$field,'../../untrusted.html')
  }
  try { $result=$client.PostAsync("$BaseUrl/Photos/Add",$form).GetAwaiter().GetResult(); return [pscustomobject]@{StatusCode=[int]$result.StatusCode;Content=$result.Content.ReadAsStringAsync().GetAwaiter().GetResult()} }
  finally { $form.Dispose();$client.Dispose() }
 }
 $png=[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a5C8AAAAASUVORK5CYII=')
 $response=Upload $png $owner 'Center' $centerId 2
 Assert ($response.StatusCode -eq 302 -and (Sql "SELECT COUNT(*) FROM Photos WHERE DiveCenterId=$centerId AND ImageUrl LIKE '/uploads/photos/%'") -eq '2') 'Two files selected together upload to school gallery'
 $urls=Sql "SELECT ImageUrl FROM Photos WHERE DiveCenterId=$centerId AND ImageUrl LIKE '/uploads/photos/%'"
 $page=Request "/DiveCenters/Details/$centerId" $public
 foreach($url in ($urls -split "`n")) { Assert ($page.Content -match [regex]::Escape($url)) 'Customer sees every school batch photo' }
 $before=@(Get-ChildItem wwwroot/uploads/photos -File).Count
 $response=Upload $png $owner 'Center' $centerId 2 $true
 Assert ($response.Content -match 'Only JPEG' -and (Sql "SELECT COUNT(*) FROM Photos WHERE DiveCenterId=$centerId") -eq '3' -and @(Get-ChildItem wwwroot/uploads/photos -File).Count -eq $before) 'Invalid second photo rolls back complete batch and files'
 $response=Upload $png $owner 'Activity' $activityId 2
 Assert ($response.StatusCode -eq 302 -and (Sql "SELECT COUNT(*) FROM Photos WHERE ActivityId=$activityId") -eq '2') 'Product batch uploads multiple photos'
 $urls=Sql "SELECT ImageUrl FROM Photos WHERE ActivityId=$activityId"
 $page=Request "/Activities/Details/$activityId" $public
 foreach($url in ($urls -split "`n")) { Assert ($page.Content -match [regex]::Escape($url)) 'Customer sees every product batch photo' }
 foreach($id in ((Sql "SELECT Id FROM Photos WHERE ActivityId=$activityId") -split "`n")) { $null=Post "/Photos/Activity/$activityId" '/Photos/Delete' $owner @{target='Activity';id=$activityId;photoId=$id} }
 $response=Upload $png $owner 'Activity' $activityId
 Assert ($response.StatusCode -eq 302 -and (Sql "SELECT COUNT(*) FROM Photos WHERE ActivityId=$activityId AND ImageUrl LIKE '/uploads/photos/%'") -eq '1') 'Actual multipart PNG upload succeeds'
 $uploadedUrl=Sql "SELECT TOP(1) ImageUrl FROM Photos WHERE ActivityId=$activityId ORDER BY Id"
 Assert ($uploadedUrl -match '^/uploads/photos/[a-f0-9]{32}\.png$') 'Upload filename generated independently of posted filename'
 $response=Request $uploadedUrl $public
 Assert ($response.StatusCode -eq 200 -and $response.Headers['Content-Type'] -match 'image/png') 'Runtime uploaded image served immediately'
 $page=Request "/Activities/Details/$activityId" $public
 Assert ($page.StatusCode -eq 200 -and $page.Content -match [regex]::Escape($uploadedUrl)) 'Public activity photo gallery displayed'
 $page=Request '/Activities' $public
 Assert ($page.Content -match [regex]::Escape($uploadedUrl)) 'Uploaded cover shown on product card'
 $response=Upload ([Text.Encoding]::UTF8.GetBytes('<html>not an image</html>')) $owner 'Activity' $activityId
 Assert ($response.Content -match 'Only JPEG, PNG and WebP') 'Non-image upload rejected despite posted image MIME'
 $oversize=New-Object byte[] (5*1024*1024+1); [Array]::Copy($png,$oversize,$png.Length)
 $response=Upload $oversize $owner 'Activity' $activityId
 Assert ($response.Content -match 'up to 5 MB') 'Oversized image rejected'
 $added=Post "/Photos/Activity/$activityId" '/Photos/Add' $owner @{'Input.Target'='Activity';'Input.ResourceId'=$activityId;'Input.ImageUrl'='https://example.invalid/activity-cover.jpg';'Input.Caption'='Second image'}
 Assert ($added.StatusCode -eq 200 -and $added.Content -match 'Photo added') 'Multiple product photos supported'
 $second=[int](Sql "SELECT TOP(1) Id FROM Photos WHERE ActivityId=$activityId ORDER BY Id DESC")
 $response=Post "/Photos/Activity/$activityId" '/Photos/SetCover' $owner @{target='Activity';id=$activityId;photoId=$second}
 Assert ($response.StatusCode -eq 200 -and (Sql "SELECT CAST(IsCover AS int) FROM Photos WHERE Id=$second") -eq '1') 'Cover selection persists'
 $page=Request '/Activities' $public
 Assert ($page.Content -match 'activity-cover.jpg') 'Selected product cover used by marketplace'
 $forbidden=Post '/Account/Login' '/Photos/SetCover' $stranger @{target='Activity';id=$activityId;photoId=$second}
 Assert ($forbidden.StatusCode -eq 403) 'Foreign cover POST forbidden'
 $forbidden=Post '/Account/Login' '/Photos/Delete' $stranger @{target='Activity';id=$activityId;photoId=$second}
 Assert ($forbidden.StatusCode -eq 403) 'Foreign photo deletion forbidden'
 $forbidden=Post '/Account/Login' '/Photos/Add' $stranger @{'Input.Target'='Activity';'Input.ResourceId'=$activityId;'Input.ImageUrl'='https://example.invalid/forged.jpg'}
 Assert ($forbidden.StatusCode -eq 403) 'Foreign photo upload POST forbidden'
 $bad=Request '/Photos/Delete' $owner 'POST' @{target='Activity';id=$activityId;photoId=$second}
 Assert ($bad.StatusCode -eq 400) 'Photo mutations require antiforgery token'
 $bad=Request '/Photos/Delete?target=Activity&id=1&photoId=1' $owner
 Assert ($bad.StatusCode -in 404,405) 'GET cannot delete photos'
 $uploadedId=[int](Sql "SELECT Id FROM Photos WHERE ActivityId=$activityId AND ImageUrl='$uploadedUrl'")
 $response=Post "/Photos/Activity/$activityId" '/Photos/Delete' $owner @{target='Activity';id=$activityId;photoId=$uploadedId}
 Assert ($response.StatusCode -eq 200 -and (Sql "SELECT COUNT(*) FROM Photos WHERE Id=$uploadedId") -eq '0') 'Owner deletes uploaded photo metadata'
 $response=Request $uploadedUrl $public
 Assert ($response.StatusCode -eq 404) 'Deleted uploaded file removed from disk'
 # Exercise the gallery limit without affecting real records.
 Sql "DECLARE @i int=1; WHILE @i<20 BEGIN INSERT Photos(ActivityId,ImageUrl) VALUES($activityId,'https://example.invalid/limit.jpg'); SET @i=@i+1; END;" | Out-Null
 $response=Post "/Photos/Activity/$activityId" '/Photos/Add' $owner @{'Input.Target'='Activity';'Input.ResourceId'=$activityId;'Input.ImageUrl'='https://example.invalid/over-limit.jpg'}
 Assert ($response.Content -match 'up to 20 photos' -and (Sql "SELECT COUNT(*) FROM Photos WHERE ActivityId=$activityId") -eq '20') 'Maximum 20 photos enforced'
 Write-Host "Completed $script:passed photo checks. Temporary test fixtures are removed."
}
finally {
 $remainingFiles=Sql "SELECT ImageUrl FROM Photos WHERE (ActivityId=$activityId OR DiveCenterId=$centerId) AND ImageUrl LIKE '/uploads/photos/%'"
 foreach($url in ($remainingFiles -split "`n")) { if($url -match '^/uploads/photos/[a-f0-9]{32}\.png$') { $path=Join-Path (Get-Location) ('wwwroot'+$url.Replace('/','\')); if(Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } } }
 Sql "DELETE FROM Photos WHERE DiveCenterId=$centerId OR ActivityId=$activityId; DELETE FROM Activities WHERE Id=$activityId AND Name='Photo QA Activity $run'; DELETE FROM DiveCenterUsers WHERE DiveCenterId=$centerId; DELETE FROM DiveCenters WHERE Id=$centerId AND Name='Photo QA $run'; DELETE FROM AppUsers WHERE Email LIKE '$prefix-%' AND NOT EXISTS(SELECT 1 FROM Reservations WHERE CustomerUserId=AppUsers.Id);" | Out-Null
 if($uploadedUrl -and $uploadedUrl -match '^/uploads/photos/[a-f0-9]{32}\.png$') { $file=Join-Path (Get-Location) ('wwwroot'+$uploadedUrl.Replace('/','\')); if(Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file } }
}