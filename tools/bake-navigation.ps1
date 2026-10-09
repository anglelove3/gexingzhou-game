param([string]$GodotExe,[switch]$Check,[string]$ExpectedFile='content/vs01/navigation.json')
. "$PSScriptRoot/Common.ps1"
try {
    $context=Get-ToolContext $GodotExe
    $expected=[IO.Path]::GetFullPath((Join-Path $script:ProjectRoot $ExpectedFile))
    $canonical=[IO.Path]::GetFullPath((Join-Path $script:ProjectRoot 'content/vs01/navigation.json'))
    $tests=[IO.Path]::GetFullPath((Join-Path $script:ProjectRoot 'test-output'))+[IO.Path]::DirectorySeparatorChar
    if($expected -ne $canonical -and !$expected.StartsWith($tests,[StringComparison]::OrdinalIgnoreCase)){throw 'EXPECTED_FILE_OUTSIDE_PROJECT'}
    if($Check -and !(Test-Path -LiteralPath $expected -PathType Leaf)){throw 'EXPECTED_FILE_MISSING'}
    Push-Location $script:ProjectRoot
    try {
        Invoke-Checked $context.DotnetExe @('build','GeXingzhou.csproj','--no-restore') 'navigation-build'
        Invoke-Checked $context.GodotExe @('--headless','--editor','--path',$script:ProjectRoot,'--import') 'navigation-import'
        $arguments=@('--headless','--path',$script:ProjectRoot,'res://tests/integration/NavigationBake.tscn','--','--suite=NavigationBake')
        if($Check){$arguments+="--navigation-expected=$expected"}
        Invoke-Checked $context.GodotExe $arguments 'navigation-export'
    } finally {Pop-Location}
    exit 0
} catch {Write-Output "NAVIGATION_CHECK_FAIL $_";exit 1}
