[CmdletBinding()]
param(
    [string]$ZipPath,
    [switch]$Rehearsal
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$release = 'vs01-opponent-labels-20261124'
$archiveName = "gexingzhou-demo-$release-win64"
$expectedHash = 'C57EBF1F8AC61C747189926A0137E9994BF7C4D6545006FE0549D78066A00140'
if (!$ZipPath) {
    $ZipPath = Join-Path $projectRoot "builds/$archiveName.zip"
}
if (!(Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
    throw "WIN64_ZIP_MISSING $ZipPath"
}
if ((Get-FileHash -LiteralPath $ZipPath).Hash -ne $expectedHash) {
    throw "ZIP_SHA256_MISMATCH $ZipPath"
}

$runId = [Guid]::NewGuid().ToString('N')
$purpose = if ($Rehearsal) { 'rehearsal' } else { 'session' }
$runDirectory = Join-Path $projectRoot "builds/wave4-playtest-$purpose-$runId"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path -LiteralPath $ZipPath).Path, $runDirectory)
$gameDirectory = Join-Path $runDirectory $archiveName
$executable = Join-Path $gameDirectory 'gexingzhou-demo.exe'
$sessionDirectory = Join-Path $gameDirectory 'test-output/wave4-playtest'
$saveDirectory = Join-Path $sessionDirectory 'combat-v1'
[IO.Directory]::CreateDirectory($saveDirectory) | Out-Null

# Synthetic ordinary progress after the first three waves of this release.
$progress = [ordered]@{
    SchemaVersion = 1
    ContentVersion = 'combat-0.1'
    RunId = $runId
    Experience = 110
    Owned = @('wraps', 'jacket', 'trainers', 'gloves', 'canvas', 'stick')
    Equipment = [ordered]@{ weapon = 'wraps'; coat = 'jacket'; shoes = 'trainers' }
    Completed = @('a1', 'a2', 'b1')
    LastWave = 'b2'
    CompletedRun = $false
}
$saveFile = Join-Path $saveDirectory 'save.json'
[IO.File]::WriteAllText($saveFile, ($progress | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))

$launcher = Join-Path $gameDirectory 'playtest.cmd'
$batchLines = @(
    '@echo off'
    'setlocal'
    'set "APPDATA=%~dp0test-output\wave4-playtest\roaming"'
    'set "LOCALAPPDATA=%~dp0test-output\wave4-playtest\local"'
    'start "" /D "%~dp0." "%~dp0gexingzhou-demo.exe" --log-file "%~dp0test-output\wave4-playtest\engine.log" -- --suite=Wave4Playtest --test-save-root=res://test-output/wave4-playtest'
)
[IO.File]::WriteAllLines($launcher, $batchLines, [Text.Encoding]::ASCII)

[pscustomobject]@{
    Release = $release
    Purpose = $purpose
    RunDirectory = $runDirectory
    GameDirectory = $gameDirectory
    Executable = $executable
    Launcher = $launcher
    SaveFile = $saveFile
    ExportDirectory = Join-Path $saveDirectory 'exports'
}
