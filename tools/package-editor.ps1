param([string]$Destination)
. "$PSScriptRoot/Common.ps1"
try {
    $resources=@(Get-PublicResources)
    if(!$Destination){$Destination=Join-Path $script:ProjectRoot ('builds/editor-vs01-'+[Guid]::NewGuid().ToString('N'))}
    if(Test-Path -LiteralPath $Destination){throw 'DESTINATION_EXISTS: choose a fresh path; existing packages are not overwritten'}
    $destinationPath=[IO.Path]::GetFullPath($Destination)
    $buildsPath=[IO.Path]::GetFullPath((Join-Path $script:ProjectRoot 'builds'))+[IO.Path]::DirectorySeparatorChar
    if(!$destinationPath.StartsWith($buildsPath,[StringComparison]::OrdinalIgnoreCase)){throw 'DESTINATION_OUTSIDE_BUILDS'}
    $files=$resources+@('project.godot','GeXingzhou.csproj','global.json','export_presets.cfg','README.md','.gitignore','code/Domain/GeXingzhou.Domain.csproj')
    foreach($folder in @('code/Domain','tests','tools','docs/development','.vscode')) {
        foreach($item in Get-ChildItem -LiteralPath (Join-Path $script:ProjectRoot $folder) -File -Recurse -Force) {
            $relative=$item.FullName.Substring($script:ProjectRoot.Length+1).Replace('\','/')
            if($relative -match '(^|/)(bin|obj)(/|$)|environment\.local\.json$'){continue}
            $files+=$relative
        }
    }
    # Test harness is intentional in the editor source bundle, never in the public runtime resources.
    foreach($item in Get-ChildItem -LiteralPath (Join-Path $script:ProjectRoot 'scripts/Testing') -File){$files+=$item.FullName.Substring($script:ProjectRoot.Length+1).Replace('\','/')}
    foreach($relative in $files|Select-Object -Unique) {
        $source=Join-Path $script:ProjectRoot $relative;$target=Join-Path $destinationPath $relative
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target))|Out-Null
        Copy-Item -LiteralPath $source -Destination $target
        if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw "PACKAGE_HASH_MISMATCH $relative"}
    }
    Write-Output "EDITOR_SOURCE_PACKAGE_NOT_STANDALONE $destinationPath"
    exit 0
} catch {Write-Output "PACKAGE_FAIL $_";exit 1}
