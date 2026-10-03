param([string]$GodotExe,[string]$TemplateDirectory)
. "$PSScriptRoot/Common.ps1"
try {
    if(!$TemplateDirectory) {
        foreach($name in @('4.7.2.stable.mono','4.7.2.stable')) {
            $candidate=Join-Path $env:APPDATA "Godot/export_templates/$name"
            if(Test-Path -LiteralPath (Join-Path $candidate 'windows_release_x86_64.exe')){$TemplateDirectory=$candidate;break}
        }
    }
    if(!$TemplateDirectory -or !(Test-Path -LiteralPath (Join-Path $TemplateDirectory 'windows_release_x86_64.exe'))){throw 'TEMPLATES_MISSING: Godot 4.7.2 .NET Windows x86_64 templates required; editor delivery is not an EXE'}
    $context=Get-ToolContext $GodotExe
    & "$PSScriptRoot/verify.ps1" -GodotExe $context.GodotExe
    if($LASTEXITCODE -ne 0){throw 'VERIFICATION_REQUIRED'}
    $resources=@(Get-PublicResources)
    $output=Join-Path $script:ProjectRoot ('builds/windows-vs01-'+[Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($output)|Out-Null
    # Use a per-run preset outside the root only for the explicit custom template.
    # Godot resolves the standard preset and installed version; custom paths must be configured there by the developer.
    Invoke-Checked $context.GodotExe @('--headless','--path',$script:ProjectRoot,'--export-release','Windows Desktop', (Join-Path $output 'GeXingzhou.exe')) 'export'
    if(!(Test-Path -LiteralPath (Join-Path $output 'GeXingzhou.exe'))){throw 'EXE_NOT_CREATED'}
    Copy-Item -LiteralPath (Join-Path $script:ProjectRoot 'README.md') -Destination $output
    Copy-Item -LiteralPath (Join-Path $script:ProjectRoot 'assets/manifest.json') -Destination $output
    Write-Output "EXPORT_CREATED_NOT_YET_PLAYTESTED $output"
    exit 0
} catch {Write-Output "EXPORT_FAIL $_";exit 1}
