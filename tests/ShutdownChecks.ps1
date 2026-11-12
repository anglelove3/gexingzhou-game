param([string]$GodotExe,[switch]$Rendered)
. "$PSScriptRoot/../tools/Common.ps1"
try {
    $context=Get-ToolContext $GodotExe
    $output=Join-Path $script:ProjectRoot ('test-output/shutdown-'+[Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($output)|Out-Null
    foreach($mode in @('menu','window-menu','window','pause','save-failure','effect','muted')) {
        $stdout=Join-Path $output ($mode+'.stdout.log');$stderr=Join-Path $output ($mode+'.stderr.log')
        $arguments=@('--verbose','--path',$script:ProjectRoot,'--script','res://tests/runtime/ShutdownSmoke.gd','--log-file',(Join-Path $output ($mode+'.engine.log')))
        if(!$Rendered){$arguments=@('--headless')+$arguments}
        $arguments+=@('--','--suite=Shutdown',"--shutdown-mode=$mode")
        $quoted=@($arguments|ForEach-Object {'"'+$_.Replace('"','\"')+'"'})
        $process=Start-Process -FilePath $context.GodotExe -ArgumentList $quoted -WorkingDirectory $script:ProjectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $null=$process.Handle # Retain the exit code even if a short process ends before WaitForExit.
        if(!$process.WaitForExit(45000)){$process.Kill();throw "SHUTDOWN_TIMEOUT $mode"}
        $process.WaitForExit();$process.Refresh()
        $body=[IO.File]::ReadAllText($stdout)+[IO.File]::ReadAllText($stderr)
        if($process.ExitCode -ne 0 -or $body -notmatch "SHUTDOWN_CHECK_PASS $mode" -or $body -match '(?im)^\s*(ERROR:|WARNING:|Leaked instance:|SCRIPT ERROR:)') {
            Write-Output $body;throw "SHUTDOWN_FAILED $mode exit=$($process.ExitCode)"
        }
        Write-Output "SHUTDOWN_PASS $mode"
    }
    Write-Output "SHUTDOWN_CHECKS_PASS cases=7 rendered=$Rendered logs=$output"
    exit 0
} catch {Write-Output "SHUTDOWN_CHECKS_FAIL $_";exit 1}
