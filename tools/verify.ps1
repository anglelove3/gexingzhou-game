param([string]$Suite,[string]$GodotExe)
. "$PSScriptRoot/Common.ps1"
$known=@('Movement','Invitation','Hey','Soup','Memory','Slice','Resume','ResumeSeed','ResumeRead','Accessibility','Recovery','Narrative','Art','EditableWorld','EditableUi','ResponsiveUi','PolishedUi','Rest','ExperienceDialogue','ExperienceGuidance','ExperienceSoupSeat','ExperienceCoins','ExperienceAudio','ExperiencePause','ExperienceEnd','DepthMovement')
try {
    $known+=@('Community','CommunityUpgrade','CommunityRest')
    $known+='SaveUpgrade'
    $known+='DepthSeat'
    $known+='ExplorationUi'
    $known+='FinalBoundaries'
    $known+='PhoneChat'
    $known+='Journal'
    if($Suite -and $Suite -notin $known){throw "UNKNOWN_SUITE $Suite"}
    $context=Get-ToolContext $GodotExe
    Push-Location $script:ProjectRoot
    try {
        $version=& $context.GodotExe --version
        if($LASTEXITCODE -ne 0 -or "$version" -notmatch '^4\.7\.2\.stable\.mono'){throw "GODOT_VERSION_MISMATCH $version"}
        $restore=@('restore','GeXingzhou.csproj')
        if($context.NugetSource){$restore+=@('--source',$context.NugetSource)}
        Invoke-Checked $context.DotnetExe $restore 'restore'
        Invoke-Checked $context.DotnetExe @('run','--project','tests/DomainChecks') 'domain'
        Invoke-Checked $context.DotnetExe @('build','GeXingzhou.csproj','--no-restore') 'build'
        Invoke-Checked $context.GodotExe @('--headless','--editor','--path',$script:ProjectRoot,'--import') 'import'
        if($Suite){$suites=@($Suite)}else{$suites=@('Movement','Invitation','Hey','Soup','Memory','Slice','ResumeSeed','ResumeRead','Accessibility','Recovery','Narrative','Art','EditableWorld','EditableUi','ResponsiveUi','PolishedUi','Rest','ExperienceDialogue','ExperienceGuidance','ExperienceSoupSeat','ExperienceCoins','ExperienceAudio','ExperiencePause','ExperienceEnd','DepthMovement')}
        $resumeRoot='res://test-output/verify-resume-'+[Guid]::NewGuid().ToString('N')
        if(!$Suite){$suites+='SaveUpgrade'}
        if(!$Suite){$suites+='DepthSeat'}
        if(!$Suite){$suites+='ExplorationUi'}
        if(!$Suite){$suites+='FinalBoundaries'}
        if(!$Suite){$suites+='PhoneChat'}
        if(!$Suite){$suites+='Journal'}
        if(!$Suite){$suites+=@('Community','CommunityUpgrade','CommunityRest')}
        foreach($name in $suites) {
            $arguments=@('--headless','--fixed-fps','60','--path',$script:ProjectRoot,'res://tests/integration/Smoke.tscn','--',"--suite=$name")
            if($name -in @('ResumeSeed','ResumeRead')){$arguments+="--test-save-root=$resumeRoot"}
            Invoke-Checked $context.GodotExe $arguments ('suite-'+$name)
        }
        $resources=@(Get-PublicResources)
        Write-Output "VERIFY_PASS suites=$($suites.Count) public_resources=$($resources.Count)"
    } finally {Pop-Location}
    exit 0
} catch {Write-Output "VERIFY_FAIL $_";exit 1}
