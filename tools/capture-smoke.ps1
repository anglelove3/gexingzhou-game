param([string]$GodotExe)
. "$PSScriptRoot/Common.ps1"
try {
    $context=Get-ToolContext $GodotExe
    foreach($size in @('1280x720','1920x1080')) {
        $outputDir=Join-Path $script:ProjectRoot 'test-output/captures'
        [IO.Directory]::CreateDirectory($outputDir)|Out-Null
        $stdout=Join-Path $outputDir ($size+'.stdout.log');$stderr=Join-Path $outputDir ($size+'.stderr.log')
        $arguments=@('--borderless','--position','-10000,-10000','--resolution',$size,'--path',('"'+$script:ProjectRoot+'"'),'res://tests/integration/Smoke.tscn','--','--suite=Capture',"--capture-size=$size")
        $process=Start-Process -FilePath $context.GodotExe -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $retainedHandle=$process.Handle
        if(!$process.WaitForExit(55000)){Stop-Process -Id $process.Id;throw "CAPTURE_TIMEOUT $size own_process=$($process.Id)"}
        $process.WaitForExit();$body=(Get-Content -LiteralPath $stdout -Raw -Encoding UTF8)+(Get-Content -LiteralPath $stderr -Raw -Encoding UTF8)
        Write-Output $body
        if($process.ExitCode -ne 0 -or $body -match 'ERROR:|GODOT_CHECKS_FAIL|Fatal error' -or $body -notmatch 'GODOT_CHECKS_PASS Capture'){throw "CAPTURE_FAILED $size exit=$($process.ExitCode)"}
        foreach($name in @('menu','phone','soup','memory','memory-font-32','art-community_gate','art-convenience_street','art-soup_shop','art-walk-0','art-walk-1','art-walk-2','art-walk-3','observation-quiet','observation-answered','observation-unanswered','return-0-font-32','return-1-font-32','return-2-font-32')) {
            $path=Join-Path $outputDir "$size/$name.png"
            if(!(Test-Path -LiteralPath $path) -or $body -notmatch "CAPTURE $name $size "){throw "CAPTURE_SIZE_OR_FILE_MISMATCH $name $size"}
        }
    }
    Write-Output 'CAPTURE_PASS 36 required real-rendered images; visual inspection still required'
    exit 0
} catch {Write-Output "CAPTURE_FAIL $_";exit 1}
