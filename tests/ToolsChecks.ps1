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
$privateProbeDir=Join-Path $projectRoot ('tools/source-guard-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($privateProbeDir)|Out-Null
try {
    foreach($suffix in @('log','docx','zip')) {
        $privateProbe=Join-Path $privateProbeDir ('private-probe.'+$suffix)
        Copy-Item -LiteralPath "$PSScriptRoot/fixtures/CompileProbe.cs" -Destination $privateProbe
        try {Check-Rejection ('PrivateSourceExtra-'+$suffix) "$projectRoot/tools/package-editor.ps1" @('-Destination',(Join-Path $projectRoot ('builds/source-guard-'+[Guid]::NewGuid().ToString('N')))) 'PRIVATE_SOURCE_REJECTED'}
        finally {Remove-Item -LiteralPath $privateProbe}
    }
} finally {Remove-Item -LiteralPath $privateProbeDir}
$probeDir=Join-Path $projectRoot ('builds/compile-check-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($probeDir)|Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/fixtures/CompileProbe.cs" -Destination $probeDir
$items=(& dotnet msbuild "$projectRoot/GeXingzhou.csproj" -getItem:Compile | Out-String)|ConvertFrom-Json
if($items.Items.Compile.Identity -match '^builds[/\\]'){$failures++;Write-Output 'FAIL PackagePollutesRootCompile'}else{Write-Output 'PASS PackageExcludedFromRootCompile'}
foreach($path in @('references/story.docx','assets/photo.docx','游戏人物图片/a.zip','tests/integration/Smoke.tscn','scripts/Testing/SmokeHarness.cs','test-output/events.jsonl','tools/environment.local.json')) {
    try { Assert-PublicResourcePaths @($path);$failures++;Write-Output "FAIL PrivateResource $path accepted" } catch {Write-Output "PASS PrivateResource $path"}
}
try {Assert-PublicResourcePaths @('scenes/Main.tscn','assets/theme.tres','content/vs01/dialogues.json');Write-Output 'PASS AllowedResource'} catch {$failures++;Write-Output "FAIL AllowedResource $_"}
try {
    $required=@('assets/art/vs01-v2/phone-frame.png','assets/art/vs01-v2/dialogue-frame.png','assets/art/vs01-v2/player-rest.png','assets/art/vs01-v2/bench-foreground.png','assets/animations/player-rest-v2.tres','assets/animations/player-rest-reduced-v2.tres','assets/ui/vs01-v2/phone.tres','assets/ui/vs01-v2/dialogue.tres','scenes/world/Bench.tscn','scenes/ui/Choices.tscn','scenes/ui/RestOptions.tscn')
    Assert-PublicResourcePaths $required
    $public=@(Get-PublicResources);foreach($path in $required){if($path -notin $public){throw "MISSING_POLISH_RESOURCE $path"}}
    $preset=Get-Content -LiteralPath "$projectRoot/export_presets.cfg" -Raw
    if($preset -notmatch 'assets/art/vs01-v2/\*\.png' -or $preset -notmatch 'assets/animations/\*\.tres'){throw 'DYNAMIC_POLISH_DEPENDENCY_MISSING'}
    $teaching=@('00_阅读与协作说明.md','docs/development/07_Godot与VSCode联合编辑.md','docs/development/08_场景润色验收记录.md','docs/project/2026-10-04_场景润色资源记录.json','docs/superpowers/specs/2026-10-04-Godot可编辑场景与坐下互动设计.md','docs/superpowers/plans/2026-10-04-VS01可编辑场景与表现润色实施计划.md')
    foreach($path in $teaching){if(!(Test-Path -LiteralPath (Join-Path $projectRoot $path))){throw "MISSING_POLISH_TEACHING $path"}}
    $destination=Join-Path $projectRoot ('builds/tools-check-'+[Guid]::NewGuid().ToString('N'))
    $package=& $shellExe -NoProfile -ExecutionPolicy Bypass -File "$projectRoot/tools/package-editor.ps1" -Destination $destination
    if($LASTEXITCODE -ne 0){throw "POLISH_PACKAGE_FAILED $package"}
    foreach($path in $required+$teaching){
        $target=Join-Path $destination $path;$source=Join-Path $projectRoot $path
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "POLISH_PACKAGE_MISMATCH $path"}
    }
    foreach($path in @('tools/audio/generate-vs01-audio.mjs','tools/audio/check-vs01-audio.mjs','docs/reviews/2026-10-05_首段润色验收记录.md','docs/superpowers/specs/2026-10-05-首段玩家体验润色-design.md','scenes/ui/Pause.tscn','scenes/ui/SliceEnd.tscn','assets/audio/vs01/manifest.json')){
        $target=Join-Path $destination $path;$source=Join-Path $projectRoot $path
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "EXPERIENCE_PACKAGE_MISMATCH $path"}
    }
    if(Get-ChildItem -LiteralPath $destination -File -Recurse -Force | Where-Object {$_.FullName -match 'references|test-output|environment\.local\.json|\.docx$|\.zip$'}){throw 'PRIVATE_SOURCE_BUNDLE_CONTENT'}
    Write-Output 'PASS PolishDependencyWhitelist SourceBundleTeachingAndHashes'
} catch {$failures++;Write-Output "FAIL PolishDependencyWhitelist $_"}
Write-Output "TOOLS_FAILURES $failures"
if($failures -gt 0){exit 1}else{exit 0}
