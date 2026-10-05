param([string]$GodotExe,[ValidateSet('All','CaptureExperience','CaptureSoupSeat','CaptureCoins')][string]$Batch='All')
. "$PSScriptRoot/Common.ps1"
try {
    $context=Get-ToolContext $GodotExe
    Add-Type -AssemblyName System.Drawing
    $required=@('menu','phone','soup','memory','memory-font-32','art-community_gate','art-convenience_street','art-soup_shop','art-walk-0','art-walk-1','art-walk-2','art-walk-3','observation-quiet','observation-answered','observation-unanswered','return-0-font-32','return-1-font-32','return-2-font-32','polish-dialogue-20','polish-dialogue-24','polish-dialogue-32','polish-phone-20','polish-phone-24','polish-phone-32','rest-seated','rest-options','rest-smoke','rest-risen')
    $required+=@('guidance-first','guidance-cannon','guidance-exit')
    $required+=@('soup-seated','soup-eat','soup-chopsticks','soup-phone','soup-risen','candy-handover')
    $required+=@('soup-seated-reduced','soup-eat-reduced','soup-chopsticks-reduced','soup-phone-reduced','soup-risen-reduced')
    foreach($font in @(20,24,32)){foreach($kind in @('spoken','thought','narration')){$required+='dialogue-'+$kind+'-'+$font}}
    foreach($font in @(20,24,32)){foreach($kind in @('table','partial','complete','help')){$required+='memory-'+$kind+'-'+$font}}
    if($Batch -eq 'CaptureCoins'){$required=@($required|Where-Object {$_ -match '^memory-(table|partial|complete|help)-'})}
    if($Batch -eq 'CaptureExperience'){$required=@($required|Where-Object {$_ -match '^guidance-|^dialogue-'})}
    if($Batch -eq 'CaptureSoupSeat'){$required=@($required|Where-Object {$_ -match '^soup-.+|^candy-handover$'})}
    $batches=if($Batch -eq 'All'){@('Capture','CaptureNarrative','CapturePolish','CaptureExperience','CaptureSoupSeat')}else{@($Batch)}
    foreach($size in @('1280x720','1920x1080','1440x1080')) {
        $outputDir=Join-Path $script:ProjectRoot 'test-output/captures'
        [IO.Directory]::CreateDirectory($outputDir)|Out-Null
        $combined=''
        foreach($captureBatch in $batches) {
            $stdout=Join-Path $outputDir ($size+'-'+$captureBatch+'.stdout.log');$stderr=Join-Path $outputDir ($size+'-'+$captureBatch+'.stderr.log')
            $arguments=@('--borderless','--position','-10000,-10000','--resolution',$size,'--path',('"'+$script:ProjectRoot+'"'),'res://tests/integration/Smoke.tscn','--',("--suite="+$captureBatch),"--capture-size=$size")
            $process=Start-Process -FilePath $context.GodotExe -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            $retainedHandle=$process.Handle
            if(!$process.WaitForExit(55000)){Stop-Process -Id $process.Id;throw "CAPTURE_TIMEOUT $size $captureBatch own_process=$($process.Id)"}
            $process.WaitForExit();$body=(Get-Content -LiteralPath $stdout -Raw -Encoding UTF8)+(Get-Content -LiteralPath $stderr -Raw -Encoding UTF8)
            Write-Output $body
            if($process.ExitCode -ne 0 -or $body -match 'ERROR:|GODOT_CHECKS_FAIL|Fatal error' -or $body -notmatch ("GODOT_CHECKS_PASS "+$captureBatch)){throw "CAPTURE_FAILED $size $captureBatch exit=$($process.ExitCode)"}
            $combined+=$body
        }
        $dimensions=$size.Split('x')
        foreach($name in $required) {
            $path=Join-Path $outputDir "$size/$name.png"
            if(!(Test-Path -LiteralPath $path) -or $combined -notmatch "CAPTURE $name $size "){throw "CAPTURE_SIZE_OR_FILE_MISMATCH $name $size"}
            $image=[Drawing.Bitmap]::new($path)
            try {if($image.Width -ne [int]$dimensions[0] -or $image.Height -ne [int]$dimensions[1]){throw "CAPTURE_PIXEL_SIZE_MISMATCH $name $size"}} finally {$image.Dispose()}
        }
    }
    Write-Output "CAPTURE_PASS $($required.Count*3) required real-rendered images; visual inspection still required"
    exit 0
} catch {Write-Output "CAPTURE_FAIL $_";exit 1}
