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
try {
    $exploration=@('content/vs01/navigation.json','tools/bake-navigation.ps1','tests/integration/NavigationBake.tscn','scripts/Testing/NavigationBake.cs','assets/art/vs01-v4/player-soup.png','assets/art/vs01-v4/player-soup-phone.png','assets/animations/player-soup-v4.tres','docs/project/2026-10-09_探索资源记录.json','docs/superpowers/specs/2026-10-09-二维探索汤店样板-design.md','docs/superpowers/plans/2026-10-09-二维探索汤店样板-实施计划.md','docs/reviews/2026-10-09_二维探索汤店样板验收记录.md')
    foreach($path in $exploration){
        $target=Join-Path $destination $path;$source=Join-Path $projectRoot $path
        if(!(Test-Path -LiteralPath $target) -or !(Test-Path -LiteralPath $source) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "EXPLORATION_PACKAGE_MISMATCH $path"}
    }
    foreach($path in @('docs/reviews/2026-10-05_首段试玩评估与待确认润色建议.md','references','test-output','游戏人物图片','tools/environment.local.json')){if(Test-Path -LiteralPath (Join-Path $destination $path)){throw "EXPLORATION_PRIVATE_PACKAGE $path"}}
    if($preset -notmatch 'assets/art/vs01-v4/\*\.png'){throw 'DYNAMIC_EXPLORATION_DEPENDENCY_MISSING'}
    Write-Output 'PASS ExplorationDependencyWhitelist SourceBundleHashesPrivateExclusions'
} catch {$failures++;Write-Output "FAIL ExplorationDependencyWhitelist $_"}
try {
    $original=Join-Path $projectRoot 'content/vs01/navigation.json';$hash=(Get-FileHash -LiteralPath $original).Hash
    $journal=@('scenes/ui/Journal.tscn','scripts/JournalController.cs','code/Domain/JournalProjection.cs','docs/superpowers/specs/2026-10-10-今日记事任务日志-design.md','docs/superpowers/plans/2026-10-10-今日记事任务日志-实施计划.md','docs/development/12_今日记事验收与接续.md')
    foreach($path in $journal){
        $target=Join-Path $destination $path;$source=Join-Path $projectRoot $path
        if(!(Test-Path -LiteralPath $source)){throw "JOURNAL_SOURCE_MISSING $path"}
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "JOURNAL_PACKAGE_MISMATCH $path"}
    }
    foreach($path in @('docs/reviews/2026-10-05_首段试玩评估与待确认润色建议.md','references','test-output','游戏人物图片','tools/environment.local.json')){if(Test-Path -LiteralPath (Join-Path $destination $path)){throw "JOURNAL_PRIVATE_PACKAGE $path"}}
    Write-Output 'PASS JournalDependencyWhitelist SourceBundleHashesPrivateExclusions'
} catch {$failures++;Write-Output "FAIL JournalDependencyWhitelist $_"}
try {
    $original=Join-Path $projectRoot 'content/vs01/navigation.json';$hash=(Get-FileHash -LiteralPath $original).Hash
    $copyRelative='test-output/navigation-mismatch-copy.json';$copy=Join-Path $projectRoot $copyRelative
    Copy-Item -LiteralPath $original -Destination $copy
    $before=Get-Content -LiteralPath $copy -Raw
    $fixture=$before|ConvertFrom-Json
    $fixture.profiles[0].ground[0].x+=1
    $after=$fixture|ConvertTo-Json -Depth 30 -Compress
    # Runtime test artifact only; the canonical project file is never rewritten.
    [IO.File]::WriteAllText($copy,$after,[Text.UTF8Encoding]::new($false))
    $context=Get-ToolContext
    $output=& $shellExe -NoProfile -File "$projectRoot/tools/bake-navigation.ps1" -Check -GodotExe $context.GodotExe -ExpectedFile $copyRelative 2>&1
    if($LASTEXITCODE -eq 0 -or ($output -join "`n") -notmatch 'NAVIGATION_EXPORT_FAIL'){throw "NAVIGATION_MISMATCH_NOT_REJECTED $output"}
    if((Get-FileHash -LiteralPath $original).Hash -ne $hash){throw 'CANONICAL_NAVIGATION_CHANGED'}
    Write-Output 'PASS NavigationBakeMismatchRejected OriginalHashUnchanged'
} catch {$failures++;Write-Output "FAIL NavigationBakeMismatchRejected $_"}
try {
    $community=@('content/vs01/compat/navigation-v2.json','assets/art/vs01-v5-community/community-background.png','assets/art/vs01-v5-community/bench.png','assets/art/vs01-v5-community/planter.png','assets/art/vs01-v5-community/bollard.png','assets/art/vs01-v5-community/old-sign.png','assets/art/vs01-v5-community/notice-board.png','assets/art/vs01-v5-community/community-rest.tres','assets/art/vs01-v5-community/community-rest-reduced.tres','docs/superpowers/specs/2026-10-10-安置小区四向探索-design.md','docs/superpowers/plans/2026-10-10-安置小区四向探索-实施计划.md','docs/project/2026-10-10_小区探索资源记录.json','docs/development/13_小区四向探索验收与接续.md')
    foreach($path in $community){
        $source=Join-Path $projectRoot $path;$target=Join-Path $destination $path
        if(!(Test-Path -LiteralPath $source) -or !(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "COMMUNITY_PACKAGE_MISMATCH $path"}
    }
    if((Get-FileHash -LiteralPath (Join-Path $destination 'content/vs01/compat/navigation-v2.json')).Hash -ne '517BBC24C6CDC04F6186A7A699766DC5FA7D640417E12E4952CC421C3154E280'){throw 'FROZEN_PROTOCOL_HASH_CHANGED'}
    $record=Get-Content -LiteralPath (Join-Path $destination 'docs/project/2026-10-10_小区探索资源记录.json') -Raw
    if($record -match '(?i)Users[/\\]|Desktop|references[/\\]|\.docx'){throw 'PRIVATE_ART_REFERENCE_PATH'}
    Write-Output 'PASS CommunityDependencyWhitelist FrozenProtocol Art Documents Hashes'
} catch {$failures++;Write-Output "FAIL CommunityDependencyWhitelist $_"}
try {
    $polish=@('assets/animations/player-depth-polish.tres','assets/art/vs01-v6-polish/menu-autumn.png','assets/art/vs01-v6-polish/living-details.png','assets/art/vs01-v6-polish/player-walk-source.png','assets/art/vs01-v6-polish/player-walk.png','assets/ui/panel-polish.tres','assets/ui/slider-knob.svg','assets/audio/vs02/piano-quiet-theme.wav','assets/audio/vs02/ambience-outdoor-soft.wav','assets/audio/vs02/ambience-indoor-soft.wav','assets/audio/vs02/manifest.json','scenes/ui/MenuAtmosphere.tscn','scripts/MenuAtmosphere.cs','scripts/AmbientResidentController.cs','tools/audio/generate-vs02-audio.mjs','tools/audio/check-vs02-audio.mjs','tools/build-walk-atlas.mjs','docs/project/2026-10-11_试玩润色资源记录.json','docs/reviews/2026-10-11_首段试玩反馈与诊断记录.md','docs/development/14_试玩反馈润色验收与接续.md','licenses/AI_ART_POLISH_20261011.md','licenses/AUDIO_VS02.md')
    foreach($path in $polish){
        $source=Join-Path $projectRoot $path;$target=Join-Path $destination $path
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "FEEDBACK_POLISH_PACKAGE_MISMATCH $path"}
    }
    foreach($wav in @('piano-quiet-theme','ambience-outdoor-soft','ambience-indoor-soft')){
        $import=Get-Content -LiteralPath (Join-Path $destination "assets/audio/vs02/$wav.wav.import") -Raw
        if($import -notmatch '(?m)^compress/mode=0\s*$'){throw "POLISH_PCM_IMPORT_CHANGED $wav"}
    }
    $record=Get-Content -LiteralPath (Join-Path $destination 'docs/project/2026-10-11_试玩润色资源记录.json') -Raw | ConvertFrom-Json
    foreach($asset in $record.assets){if((Get-FileHash -LiteralPath (Join-Path $destination $asset.file)).Hash -ne $asset.sha256){throw "POLISH_MANIFEST_HASH_MISMATCH $($asset.file)"}}
    if($preset -notmatch 'assets/art/vs01-v6-polish/\*\.png' -or $preset -notmatch 'assets/audio/vs02/\*\.wav'){throw 'FEEDBACK_EXPORT_DEPENDENCY_MISSING'}
    Write-Output 'PASS FeedbackPolishDependencyWhitelist ManifestHashes PCMImports SourceTools Documents'
} catch {$failures++;Write-Output "FAIL FeedbackPolishDependencyWhitelist $_"}
try {
    $streetFiles=@('content/vs01/compat/navigation-v3.json','docs/project/2026-10-14_便利店街探索资源记录.json','licenses/AI_ART_STREET_20261014.md','docs/superpowers/specs/2026-10-14-便利店街环形探索-design.md','docs/superpowers/plans/2026-10-14-便利店街环形探索-实施计划.md','docs/development/15_便利店街环形探索验收与接续.md')
    foreach($path in $streetFiles){
        $source=Join-Path $projectRoot $path;$target=Join-Path $destination $path
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "STREET_PACKAGE_MISMATCH $path"}
    }
    if((Get-FileHash -LiteralPath (Join-Path $destination 'content/vs01/compat/navigation-v3.json')).Hash -ne 'F918EE94AC3A74320FF2EEA567AC86E3526E905E7B581E5EBD84CE19F4F5A18F'){throw 'FROZEN_V3_PROTOCOL_HASH_CHANGED'}
    $streetRecord=Get-Content -LiteralPath (Join-Path $destination 'docs/project/2026-10-14_便利店街探索资源记录.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if(@($streetRecord.assets).Count -ne 8){throw 'STREET_ASSET_COUNT_CHANGED'}
    foreach($asset in $streetRecord.assets){
        $source=Join-Path $projectRoot $asset.path;$target=Join-Path $destination $asset.path
        if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne $asset.sha256 -or (Get-FileHash -LiteralPath $target).Hash -ne $asset.sha256){throw "STREET_ASSET_HASH_MISMATCH $($asset.path)"}
    }
    if($preset -notmatch 'assets/art/vs01-v7-street/\*\.png'){throw 'STREET_EXPORT_DEPENDENCY_MISSING'}
    Write-Output 'PASS StreetCompatibilityDependencyWhitelist FrozenV3 EightArtHashes Documents'
} catch {$failures++;Write-Output "FAIL StreetCompatibilityDependencyWhitelist $_"}
  try {
      $refinementFiles=@('docs/project/2026-10-15_场景精修资源记录.json','docs/development/16_现有场景精修验收与接续.md','licenses/AI_ART_REFINEMENT_20261015.md','assets/art/vs01-v7-street/street-background-aligned.png','assets/art/vs01-v7-street/street-registration.gdshader')
      foreach($path in $refinementFiles){
          $source=Join-Path $projectRoot $path;$target=Join-Path $destination $path
          if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "REFINEMENT_PACKAGE_MISMATCH $path"}
      }
      $refinementRecord=Get-Content -LiteralPath (Join-Path $destination 'docs/project/2026-10-15_场景精修资源记录.json') -Raw -Encoding UTF8 | ConvertFrom-Json
      foreach($asset in @($refinementRecord.assets)+@($refinementRecord.native_registration)){
          if((Get-FileHash -LiteralPath (Join-Path $destination $asset.path)).Hash -ne $asset.sha256){throw "REFINEMENT_HASH_MISMATCH $($asset.path)"}
      }
      if($preset -notmatch 'assets/art/vs01-v7-street/\*\.gdshader'){throw 'REGISTRATION_EXPORT_DEPENDENCY_MISSING'}
      Write-Output 'PASS RefinementDependencyWhitelist ArtRegistrationHashes Documents'
  } catch {$failures++;Write-Output "FAIL RefinementDependencyWhitelist $_"}
  foreach($probe in @(@{Name='CaptureSizesEmpty';Literal='@()'},@{Name='CaptureSizesPath';Literal="@('../escape')"})) {
    # Continue keeping the default capture batches and private exclusions unchanged.
    $command="& '$projectRoot/tools/capture-smoke.ps1' -Sizes $($probe.Literal) -GodotExe '$projectRoot/test-output/no-godot.exe'"
    $output=& $shellExe -NoProfile -Command $command 2>&1
    if($LASTEXITCODE -eq 0 -or ($output -join "`n") -notmatch 'INVALID_CAPTURE_SIZES'){$failures++;Write-Output "FAIL $($probe.Name) $output"}else{Write-Output "PASS $($probe.Name)"}
}
try{
    $bcFiles=@('assets/audio/vs03/ambience-street-soft.wav','assets/audio/vs03/manifest.json','assets/audio/vs03/ambience-street-soft.wav.import','licenses/AUDIO_VS03.md','tools/audio/generate-vs03-audio.mjs','tools/audio/check-vs03-audio.mjs','docs/project/2026-10-16_街区生活与叙事资源记录.json','docs/development/17_街区生活与叙事润色验收与接续.md')
    foreach($path in $bcFiles){$source=Join-Path $projectRoot $path;$target=Join-Path $destination $path;if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "BC_PACKAGE_MISMATCH $path"}}
    $bc=Get-Content -LiteralPath (Join-Path $destination 'docs/project/2026-10-16_街区生活与叙事资源记录.json') -Raw -Encoding UTF8|ConvertFrom-Json
    if((Get-FileHash -LiteralPath (Join-Path $destination $bc.audio.path)).Hash -ne $bc.audio.sha256){throw 'STREET_AUDIO_HASH_MISMATCH'}
    if($preset -notmatch 'assets/audio/vs03/\*\.wav' -or $preset -notmatch 'assets/audio/vs03/manifest.json'){throw 'STREET_AUDIO_EXPORT_MISSING'}
    Write-Output 'PASS StreetLifeNarrativeDependencyWhitelist AudioHashes SourceScripts Documents'
}catch{$failures++;Write-Output "FAIL StreetLifeNarrativeDependencyWhitelist $_"}
if($args -contains '--check-capture-records'){
    try {
        foreach($size in @('1280x720','1920x1080','1440x1080')){foreach($batch in @('CapturePhoneChat','CaptureJournal','CaptureDepthSeat')){
            $record=Get-Content -LiteralPath (Join-Path $projectRoot "test-output/captures/$size-$batch.stdout.log") -Raw
            $count=if($batch -eq 'CapturePhoneChat'){16}else{18}
            if($record -notmatch ("GODOT_CHECKS_PASS "+$batch) -or ([regex]::Matches($record,"(?m)^CAPTURE .+ $size ")).Count -ne $count){throw "DEFAULT_CAPTURE_MATRIX_CHANGED $size $batch"}
        }
        }
        Write-Output 'PASS DefaultCaptureSizesUnchanged Phone48 Journal54 DepthSeat54 ActualRecords'
    }catch{$failures++;Write-Output "FAIL DefaultCaptureSizesUnchanged $_"}
}
Write-Output "TOOLS_FAILURES $failures"
if($failures -gt 0){exit 1}else{exit 0}
