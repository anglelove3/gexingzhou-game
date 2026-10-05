param([string]$Destination)
. "$PSScriptRoot/Common.ps1"
try {
    $resources=@(Get-PublicResources)
    if(!$Destination){$Destination=Join-Path $script:ProjectRoot ('builds/editor-vs01-'+[Guid]::NewGuid().ToString('N'))}
    if(Test-Path -LiteralPath $Destination){throw 'DESTINATION_EXISTS: choose a fresh path; existing packages are not overwritten'}
    $destinationPath=[IO.Path]::GetFullPath($Destination)
    $buildsPath=[IO.Path]::GetFullPath((Join-Path $script:ProjectRoot 'builds'))+[IO.Path]::DirectorySeparatorChar
    if(!$destinationPath.StartsWith($buildsPath,[StringComparison]::OrdinalIgnoreCase)){throw 'DESTINATION_OUTSIDE_BUILDS'}
    [IO.Directory]::CreateDirectory($buildsPath)|Out-Null
    $ignoreFile=Join-Path $buildsPath '.gdignore'
    if(!(Test-Path -LiteralPath $ignoreFile)){[IO.File]::WriteAllText($ignoreFile,'')}
    $files=$resources+@('project.godot','GeXingzhou.csproj','global.json','export_presets.cfg','README.md','.gitignore','code/Domain/GeXingzhou.Domain.csproj')
    $files+=@('00_阅读与协作说明.md','docs/superpowers/specs/2026-10-04-Godot可编辑场景与坐下互动设计.md','docs/superpowers/plans/2026-10-04-VS01可编辑场景与表现润色实施计划.md','docs/project/2026-10-04_场景润色资源记录.json')
    $experienceDocs=@('docs/reviews/2026-10-05_首段润色验收记录.md','docs/superpowers/specs/2026-10-05-首段玩家体验润色-design.md','docs/superpowers/plans/2026-10-05-首段润色-实施入口.md','docs/superpowers/plans/2026-10-05-润色-P1-引导与对白.md','docs/superpowers/plans/2026-10-05-润色-P2-汤店动作.md','docs/superpowers/plans/2026-10-05-润色-P3-桌面硬币.md','docs/superpowers/plans/2026-10-05-润色-P4-音频氛围.md','docs/superpowers/plans/2026-10-05-润色-P5-暂停收尾.md')
    $files+=$experienceDocs
    foreach($folder in @('code/Domain','tests','tools','docs/development','.vscode')) {
        foreach($item in Get-ChildItem -LiteralPath (Join-Path $script:ProjectRoot $folder) -File -Recurse -Force) {
            $relative=$item.FullName.Substring($script:ProjectRoot.Length+1).Replace('\','/')
            if($relative -match '(^|/)(bin|obj)(/|$)|environment\.local\.json$'){continue}
            $files+=$relative
        }
    }
    # Test harness is intentional in the editor source bundle, never in the public runtime resources.
    foreach($item in Get-ChildItem -LiteralPath (Join-Path $script:ProjectRoot 'scripts/Testing') -File){$files+=$item.FullName.Substring($script:ProjectRoot.Length+1).Replace('\','/')}
    $explicit=@('project.godot','GeXingzhou.csproj','global.json','export_presets.cfg','README.md','.gitignore','00_阅读与协作说明.md','docs/superpowers/specs/2026-10-04-Godot可编辑场景与坐下互动设计.md','docs/superpowers/plans/2026-10-04-VS01可编辑场景与表现润色实施计划.md','docs/project/2026-10-04_场景润色资源记录.json')
    $explicit+=$experienceDocs+@('tools/audio/generate-vs01-audio.mjs','tools/audio/check-vs01-audio.mjs')
    foreach($relative in $files|Select-Object -Unique) {
        if($relative -notin $resources -and $relative -notin $explicit -and
           $relative -notmatch '^(code/Domain/.*\.(cs|csproj|uid)|tests/.*\.(cs|csproj|json|tscn|uid|ps1)|tools/[^/]+\.ps1|docs/development/[^/]+\.md|\.vscode/[^/]+\.json|scripts/Testing/[^/]+\.(cs|uid))$') {throw "PRIVATE_SOURCE_REJECTED $relative"}
        $sourceInfo=Get-Item -LiteralPath (Join-Path $script:ProjectRoot $relative)
        if(($sourceInfo.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "PRIVATE_SOURCE_REJECTED link $relative"}
    }
    foreach($relative in $files|Select-Object -Unique) {
        $source=Join-Path $script:ProjectRoot $relative;$target=Join-Path $destinationPath $relative
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target))|Out-Null
        Copy-Item -LiteralPath $source -Destination $target
        if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "PACKAGE_HASH_MISMATCH $relative"}
    }
    Write-Output "EDITOR_SOURCE_PACKAGE_NOT_STANDALONE $destinationPath"
    exit 0
} catch {Write-Output "PACKAGE_FAIL $_";exit 1}
