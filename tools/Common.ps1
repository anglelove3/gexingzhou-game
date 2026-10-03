$ErrorActionPreference='Stop'
$script:ProjectRoot=Split-Path -Parent $PSScriptRoot
function Assert-PublicResourcePaths([string[]]$Paths) {
    foreach($path in $Paths) {
        $clean=$path.Replace('\','/')
        if($clean -match '(^|/)(references|tests|Testing|test-output|bin|obj|\.git|\.superpowers)(/|$)' -or
           $clean -match '\.(docx|zip|log)$|environment\.local\.json$|(^|/)\.\.' -or
           $clean -notmatch '^(scenes|scripts|content|assets|licenses)/') {throw "PRIVATE_RESOURCE_REJECTED $path"}
    }
}
function Get-PublicResources {
    $items=@()
    foreach($folder in @('scenes','scripts','content','assets','licenses')) {
        $location=Join-Path $script:ProjectRoot $folder
        if(!(Test-Path -LiteralPath $location)){continue}
        foreach($item in Get-ChildItem -LiteralPath $location -Recurse -File -Force) {
            if($item.FullName -match '\\(Testing|bin|obj)\\'){continue}
            if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'RESOURCE_LINK_REJECTED'}
            $items+=$item.FullName.Substring($script:ProjectRoot.Length+1).Replace('\','/')
        }
    }
    Assert-PublicResourcePaths $items
    return $items
}
function Get-ToolContext([string]$GodotExe) {
    $config=@{}
    $configFile=Join-Path $script:ProjectRoot 'tools/environment.local.json'
    if(Test-Path -LiteralPath $configFile){$config=Get-Content -LiteralPath $configFile -Raw -Encoding UTF8 | ConvertFrom-Json}
    if(!$GodotExe){$GodotExe=$config.GodotExe}
    if(!$GodotExe){$command=Get-Command godot -ErrorAction SilentlyContinue;if($command){$GodotExe=$command.Source}}
    if(!$GodotExe -or !(Test-Path -LiteralPath $GodotExe -PathType Leaf)){throw 'GODOT_NOT_FOUND: supply -GodotExe or tools/environment.local.json'}
    $dotnetExe=$config.DotnetExe
    if(!$dotnetExe){$command=Get-Command dotnet -ErrorAction SilentlyContinue;if($command){$dotnetExe=$command.Source}}
    if(!$dotnetExe -or !(Test-Path -LiteralPath $dotnetExe -PathType Leaf)){throw 'DOTNET_NOT_FOUND'}
    return @{GodotExe=$GodotExe;DotnetExe=$dotnetExe;NugetSource=$config.NugetSource}
}
function Invoke-Checked([string]$Executable,[string[]]$Arguments,[string]$LogName) {
    $directory=Join-Path $script:ProjectRoot 'test-output/verification'
    [IO.Directory]::CreateDirectory($directory)|Out-Null
    $oldPreference=$ErrorActionPreference;$ErrorActionPreference='Continue'
    $output=& $Executable @Arguments 2>&1
    $code=$LASTEXITCODE;$ErrorActionPreference=$oldPreference
    $body=($output|ForEach-Object {"$_"}) -join "`n"
    [IO.File]::WriteAllText((Join-Path $directory ($LogName+'.log')),$body,[Text.UTF8Encoding]::new($false))
    Write-Output $body
    if($code -ne 0 -or $body -match '(?im)^\s*(ERROR:|GODOT_CHECKS_FAIL|Fatal error|Unhandled exception)'){throw "COMMAND_FAILED $LogName exit=$code"}
}
