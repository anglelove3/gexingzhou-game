$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$shellExe=(Get-Process -Id $PID).Path
$failures=0
function Check-Rejection($Name,$Script,$Arguments,$ExpectedText) {
    $output=& $shellExe -NoProfile -ExecutionPolicy Bypass -File $Script @Arguments 2>&1
    if($LASTEXITCODE -eq 0 -or ($output -join "`n") -notmatch $ExpectedText) { $script:failures++; Write-Output "FAIL $Name expected rejection: $output" } else {Write-Output "PASS $Name"}
}
Check-Rejection 'UnknownSuite' "$projectRoot/tools/verify.ps1" @('-Suite','does_not_exist') 'UNKNOWN_SUITE'
Check-Rejection 'MissingGodot' "$projectRoot/tools/verify.ps1" @('-GodotExe',"$projectRoot/test-output/no-godot.exe") 'GODOT_NOT_FOUND'
Check-Rejection 'MissingTemplate' "$projectRoot/tools/export-windows.ps1" @('-TemplateDirectory',"$projectRoot/test-output/no-template") 'TEMPLATES_MISSING'
. "$projectRoot/tools/Common.ps1"
foreach($path in @('references/story.docx','assets/photo.docx','游戏人物图片/a.zip','tests/integration/Smoke.tscn','scripts/Testing/SmokeHarness.cs','test-output/events.jsonl','tools/environment.local.json')) {
    try { Assert-PublicResourcePaths @($path);$failures++;Write-Output "FAIL PrivateResource $path accepted" } catch {Write-Output "PASS PrivateResource $path"}
}
try {Assert-PublicResourcePaths @('scenes/Main.tscn','assets/theme.tres','content/vs01/dialogues.json');Write-Output 'PASS AllowedResource'} catch {$failures++;Write-Output "FAIL AllowedResource $_"}
Write-Output "TOOLS_FAILURES $failures"
if($failures -gt 0){exit 1}else{exit 0}
