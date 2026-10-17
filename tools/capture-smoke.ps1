param([string]$GodotExe,[ValidateSet('All','CaptureArt','CaptureExperience','CaptureSoupSeat','CaptureCoins','CapturePause','CaptureEnd','CaptureExploration','CaptureDepthSeat','CapturePhoneChat','CaptureJournal','CaptureCommunity','CaptureFeedback','CaptureRefinement','CaptureStreet')][string]$Batch='All',[AllowEmptyCollection()][string[]]$Sizes=@('1280x720','1920x1080','1440x1080'))
. "$PSScriptRoot/Common.ps1"
try {
    if(!$Sizes -or $Sizes.Count -eq 0 -or @($Sizes|Where-Object {$_ -notin @('1280x720','1920x1080','1440x1080','1440x900','1024x768')}).Count -gt 0){throw 'INVALID_CAPTURE_SIZES: select nonempty supported dimensions'}
    $context=Get-ToolContext $GodotExe
    Add-Type -AssemblyName System.Drawing
    $required=@('menu','phone','soup','memory','memory-font-32','art-community_gate','art-convenience_street','art-soup_shop','observation-quiet','observation-answered','observation-unanswered','return-0-font-32','return-1-font-32','return-2-font-32','polish-dialogue-20','polish-dialogue-24','polish-dialogue-32','polish-phone-20','polish-phone-24','polish-phone-32','rest-seated','rest-options','rest-smoke','rest-risen')
    $walkFrames=@();foreach($direction in @('front','back','side')){foreach($frame in 0..7){$walkFrames+='art-walk-'+$direction+'-'+$frame}}
    $required+=$walkFrames
    $required+=@('guidance-first','guidance-cannon','guidance-exit')
    $required+=@('soup-seated','soup-eat','soup-chopsticks','soup-phone','soup-risen','candy-handover')
    $required+=@('soup-seated-reduced','soup-eat-reduced','soup-chopsticks-reduced','soup-phone-reduced','soup-risen-reduced')
    foreach($font in @(20,24,32)){foreach($kind in @('spoken','thought','narration')){$required+='dialogue-'+$kind+'-'+$font}}
    foreach($font in @(20,24,32)){foreach($kind in @('table','partial','complete','help')){$required+='memory-'+$kind+'-'+$font}}
    foreach($font in @(20,24,32)){foreach($kind in @('pause','pause-controls','pause-settings','pause-save-failed')){$required+=$kind+'-'+$font}}
    foreach($font in @(20,24,32)){foreach($kind in @('end','end-save-failed')){$required+=$kind+'-'+$font}}
    $exploration=@('depth-front','depth-back','depth-side','depth-table-occlusion','observation-sign','observation-menu','observation-note','observation-hover','compact-short-20','compact-short-24','compact-short-32','compact-long-32')
    $depthSeat=@();foreach($suffix in @('','-reduced')){foreach($action in @('sit','stand')){foreach($frame in @('start','mid','end')){$depthSeat+='depth-'+$action+'-'+$frame+$suffix}};foreach($action in @('eat','chopsticks','phone')){$depthSeat+='depth-'+$action+$suffix}}
    $required+=$exploration+$depthSeat
    $phoneChat=@();foreach($font in @(20,24,32)){foreach($state in @('empty','message','long','call','answered')){$phoneChat+='chat-'+$state+'-'+$font}}
    $phoneChat+='chat-missed'
    $required+=$phoneChat
    $journal=@();foreach($font in @(20,24,32)){foreach($state in @('current','history','discoveries','empty-history','empty-discoveries','long')){$journal+='journal-'+$state+'-'+$font}}
    $required+=$journal
    $community=@();foreach($font in @(20,24,32)){foreach($state in @('entry','explore','observation','seated','smoke','stand')){$community+='community-'+$state+'-'+$font}}
    $required+=$community
    $feedback=@('feedback-menu','notice-player-wall-depth','ambient-community','street-exit-ready','street-return-community')
    foreach($font in @(20,24,32)){foreach($state in @('entry','pause','settings','rest','thought','thought-end')){$feedback+='feedback-'+$state+'-'+$font}}
    $required+=$feedback
    $refinement=@();foreach($font in @(20,24,32)){foreach($state in @('community','street','goal-expanded','notice','north','curb','east','wall','soup')){$refinement+='refinement-'+$state+'-'+$font}}
    $required+=$refinement
    if($Batch -eq 'CaptureRefinement'){$required=$refinement}
    $street=@();foreach($font in @(20,24,32)){
        foreach($state in @('entry','main','north','south','east','journal-top','journal-bottom','residents-front','residents-back','soup-locked','hey-before','hey-after','soup-ready')){$street+='street-'+$state+'-'+$font}
        foreach($id in @('doorplate','newsstand','bike_shed','drain','wall_paint','laundry_rack','stool')){$street+='street-observe-'+$id+'-'+$font}
        foreach($state in @('end','end-save-failed')){$street+=$state+'-'+$font}
    }
    if($Batch -eq 'CaptureStreet'){$required=$street}
    if($Batch -eq 'All'){$required+=@($street|Where-Object {$_ -notmatch '^end-'})}
    if($Batch -eq 'CaptureArt'){$required=$walkFrames+@('art-community_gate','art-convenience_street','art-soup_shop')}
    if($Batch -eq 'CaptureCommunity'){$required=$community}
    if($Batch -eq 'CaptureFeedback'){
        $required=$feedback
    }
    if($Batch -eq 'CaptureJournal'){$required=$journal}
    if($Batch -eq 'CapturePhoneChat'){$required=$phoneChat}
    if($Batch -eq 'CaptureExploration'){$required=$exploration}
    if($Batch -eq 'CaptureDepthSeat'){$required=$depthSeat}
    if($Batch -eq 'CaptureEnd'){$required=@($required|Where-Object {$_ -match '^end-'})}
    if($Batch -eq 'CapturePause'){$required=@($required|Where-Object {$_ -match '^pause-'})}
    if($Batch -eq 'CaptureCoins'){$required=@($required|Where-Object {$_ -match '^memory-(table|partial|complete|help)-'})}
    if($Batch -eq 'CaptureExperience'){$required=@($required|Where-Object {$_ -match '^guidance-|^dialogue-'})}
    if($Batch -eq 'CaptureSoupSeat'){$required=@($required|Where-Object {$_ -match '^soup-.+|^candy-handover$'})}
    $batches=if($Batch -eq 'All'){@('Capture','CaptureNarrative','CapturePolish','CaptureExperience','CaptureSoupSeat','CaptureCoins','CapturePause','CaptureEnd','CaptureExploration','CaptureDepthSeat','CapturePhoneChat','CaptureJournal','CaptureCommunity','CaptureFeedback','CaptureRefinement','CaptureStreet')}else{@($Batch)}
    $supplemental=if($batches -contains 'CaptureCommunity'){@('community-sign-back','community-sign-front')}else{@()}
    foreach($size in $Sizes) {
        $outputDir=Join-Path $script:ProjectRoot 'test-output/captures'
        [IO.Directory]::CreateDirectory($outputDir)|Out-Null
        $combined=''
        foreach($captureBatch in $batches) {
            $stdout=Join-Path $outputDir ($size+'-'+$captureBatch+'.stdout.log');$stderr=Join-Path $outputDir ($size+'-'+$captureBatch+'.stderr.log')
            $arguments=@('--borderless','--position','-10000,-10000','--resolution',$size,'--path',('"'+$script:ProjectRoot+'"'),'res://tests/integration/Smoke.tscn','--',("--suite="+$captureBatch),"--capture-size=$size")
            $process=Start-Process -FilePath $context.GodotExe -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            $retainedHandle=$process.Handle
            $deadline=[Diagnostics.Stopwatch]::StartNew()
            while(!$process.WaitForExit(30000)){
                if($deadline.Elapsed.TotalSeconds -ge 120){Stop-Process -Id $process.Id;throw "CAPTURE_TIMEOUT $size $captureBatch own_process=$($process.Id)"}
                Write-Output "CAPTURE_RUNNING $size $captureBatch elapsed=$([int]$deadline.Elapsed.TotalSeconds)s"
            }
            $process.WaitForExit();$body=(Get-Content -LiteralPath $stdout -Raw -Encoding UTF8)+(Get-Content -LiteralPath $stderr -Raw -Encoding UTF8)
            Write-Output $body
            if($process.ExitCode -ne 0 -or $body -match 'ERROR:|GODOT_CHECKS_FAIL|Fatal error' -or $body -notmatch ("GODOT_CHECKS_PASS "+$captureBatch)){throw "CAPTURE_FAILED $size $captureBatch exit=$($process.ExitCode)"}
            $combined+=$body
        }
        $dimensions=$size.Split('x')
        foreach($name in ($required+$supplemental)) {
            $path=Join-Path $outputDir "$size/$name.png"
            if(!(Test-Path -LiteralPath $path) -or $combined -notmatch "CAPTURE $name $size "){throw "CAPTURE_SIZE_OR_FILE_MISMATCH $name $size"}
            $image=[Drawing.Bitmap]::new($path)
            try {if($image.Width -ne [int]$dimensions[0] -or $image.Height -ne [int]$dimensions[1]){throw "CAPTURE_PIXEL_SIZE_MISMATCH $name $size"}} finally {$image.Dispose()}
        }
    }
    Write-Output "CAPTURE_PASS $($required.Count*$Sizes.Count) required real-rendered images; visual inspection still required"
    if($supplemental.Count){Write-Output "CAPTURE_SUPPLEMENTAL_PASS $($supplemental.Count*$Sizes.Count) sign front/back render checks"}
    exit 0
} catch {Write-Output "CAPTURE_FAIL $_";exit 1}
