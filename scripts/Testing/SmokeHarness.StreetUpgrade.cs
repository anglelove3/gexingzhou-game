using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task StreetUpgradeChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();
        Require(session.SaveDirectory.Replace('\\','/').EndsWith("/v4",StringComparison.Ordinal),"Runtime save root is not v4");
        Require(session.Snapshot.SchemaVersion==4&&session.Snapshot.ContentVersion=="vs01-0.4","Runtime new game is not v4");
        var frozen=session.GetType().GetProperty("FrozenV3Navigation")?.GetValue(session) as NavigationCatalog;
        Require(frozen!=null,"Frozen v3 navigation is not exposed");
        Require(frozen!.Profiles["convenience_street"].Mode==WorldMode.Horizontal,"Frozen v3 street changed");
        Require(frozen.Profiles["convenience_street"].Anchors["safe"]==new Position2(320,280),"Frozen v3 street coordinates changed");
        var v3Directory=session.GetType().GetProperty("V3Directory")?.GetValue(session) as string;
        Require(v3Directory!=null&&v3Directory.Replace('\\','/').EndsWith("/v3",StringComparison.Ordinal),"V3 read-only directory is not exposed");

        var fixture=UpgradeFixture();var fixtureFrozen=fixture.GetType().GetProperty("FrozenV3Navigation")?.GetValue(fixture) as NavigationCatalog;
        var fixtureV3=fixture.GetType().GetProperty("V3Directory")?.GetValue(fixture) as string;
        Require(fixtureFrozen!=null&&fixtureV3!=null,"V3 fixture boundary missing");
        var source=SaveV3Codec.CreateNew(new(),fixtureFrozen!) with{SceneId="convenience_street",PlayerPosition=new(600,280)};
        System.IO.Directory.CreateDirectory(fixtureV3!);var sourcePath=System.IO.Path.Combine(fixtureV3!,"save.json");
        System.IO.File.WriteAllText(sourcePath,SnapshotJson(source));var sourceBytes=System.IO.File.ReadAllBytes(sourcePath);
        var runtime=fixture.Snapshot;var flow=fixture.Flow;
        Require(fixture.ProbeResume(false).Source==VersionedResumeSource.V3,"Runtime did not select v3 source");
        var upgraded=fixture.TryUpgradePrevious(false);
        Require(upgraded.Status==LoadStatus.Loaded&&upgraded.Snapshot!.SchemaVersion==4&&upgraded.Snapshot.PlayerPosition==new Position2(600,520),"V3 street did not map into v4 main street");
        Require(fixture.Saves.Load().Status==LoadStatus.Loaded,"Confirmed v3 upgrade did not write v4");
        Require(System.IO.File.ReadAllBytes(sourcePath).SequenceEqual(sourceBytes)&&ReferenceEquals(fixture.Snapshot,runtime)&&fixture.Flow==flow,"V3 upgrade modified source or restored runtime before continue");
        fixture.Free();await Frames();
        await StreetV3MenuChecks(session);
        await StreetV3CandidateFailureChecks();
    }

    // Missing v3 routing must never hide an old progress slot or write it before confirmation.
    private async Task StreetV3MenuChecks(GameSession session)
    {
        foreach(var manual in new[]{false,true})foreach(var backup in new[]{false,true})
        {
            foreach(var directory in new[]{session.SaveDirectory,session.V3Directory})
            {
                Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Street menu fixture outside test-output");
                if(System.IO.Directory.Exists(directory))foreach(var name in new[]{"save.json","save.bak.json","manual.json","manual.bak.json"})System.IO.File.Delete(System.IO.Path.Combine(directory,name));
            }
            session.NewGame();
            var sourcePath=WriteStreetV3(session,manual,backup);
            var original=CommunityFileState(session.V3Directory);var current=CommunityFileState(session.SaveDirectory);var snapshot=session.Snapshot;
            var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(menu);await Frames(2);
            var button=menu.GetNode<Button>("MenuScroll/Menu/"+(backup?(manual?"ManualRecoveryButton":"RecoveryButton"):(manual?"LegacyManualUpgradeButton":"LegacyAutoUpgradeButton")));
            Require(button.Visible&&!button.Disabled,"V3 menu upgrade unavailable manual="+manual+" backup="+backup);
            Require(button.Text.Contains(backup?"备份":"v3"),"V3 menu source label missing");
            button.EmitSignal(Button.SignalName.Pressed);await Frames();
            var choices=menu.GetNode<ChoiceController>("Choices");
            Require(choices.IsOpen&&choices.GetNode<Label>("Panel/Content/Title").Text.Contains("v3")&&choices.GetNode<Label>("Panel/Content/Title").Text.Contains("v4"),"V3 upgrade did not explain source and target versions");
            Require(CommunityFileState(session.SaveDirectory)==current&&ReferenceEquals(snapshot,session.Snapshot),"V3 menu wrote or restored before confirmation");
            choices.HandleKey(Key.Escape);await Frames();
            Require(CommunityFileState(session.V3Directory)==original&&CommunityFileState(session.SaveDirectory)==current,"Cancel changed v3/v4 files");
            button.EmitSignal(Button.SignalName.Pressed);await Frames();GetTree().CurrentScene=null;choices.HandleKey(Key.E);await Frames(8);
            var resumed=GetTree().CurrentScene as MainView;
            Require(resumed!=null&&resumed.World.SceneId=="convenience_street"&&resumed.World.Player.Position==new Vector2(600,520),"Confirmed v3 menu did not restore mapped v4 street");
            Require((manual?session.ManualSaves:session.Saves).Load().Snapshot?.SchemaVersion==4,"V3 menu did not save v4 target slot");
            Require((manual?session.Saves:session.ManualSaves).Load().Status==LoadStatus.NotFound,"V3 menu crossed manual/auto slots");
            Require(System.IO.File.Exists(sourcePath)&&CommunityFileState(session.V3Directory)==original,"V3 menu changed old files");
            GetTree().CurrentScene=null;resumed!.Free();menu.Free();await Frames(2);
        }
        GD.Print("STREET_V3_MENU_PASS Auto Manual Main Backup Confirm Cancel SourceHashes");
    }

    private async Task StreetV3CandidateFailureChecks()
    {
        var fixture=UpgradeFixture();WriteStreetV3(fixture,false,false);fixture.Flow=FlowState.Phone;
        var source=CommunityFileState(fixture.V3Directory);var current=CommunityFileState(fixture.SaveDirectory);var snapshot=fixture.Snapshot;
        var resources=(Dictionary<string,PackedScene>)DiagnosticField(fixture,"sceneResources")!;
        const string path="res://scenes/world/ConvenienceStreet.tscn";var original=resources[path];
        var candidate=original.Instantiate<WorldView>();candidate.GetNode<Node2D>("Navigation/GroundBoundary").Position=new(1,0);
        using var broken=new PackedScene();Require(broken.Pack(candidate)==Error.Ok,"Invalid street fixture pack failed");candidate.Free();resources[path]=broken;
        try{
            Require(fixture.TryUpgradePrevious(false).Status==LoadStatus.Corrupt,"Invalid street candidate was upgraded");
            Require(CommunityFileState(fixture.V3Directory)==source&&CommunityFileState(fixture.SaveDirectory)==current&&ReferenceEquals(fixture.Snapshot,snapshot)&&fixture.Flow==FlowState.Phone,"Failed v3 street candidate changed files/runtime");
        }finally{resources[path]=original;fixture.Free();}
        await Frames();GD.Print("STREET_V3_CANDIDATE_PASS InvalidNavigationIsAtomic");
    }

    private static string WriteStreetV3(GameSession session,bool manual,bool backup)
    {
        var source=SaveV3Codec.CreateNew(new(),session.FrozenV3Navigation) with{SceneId="convenience_street",PlayerPosition=new(600,280)};
        Require(new SaveV3Codec(session.FrozenV3Navigation).Validate(source)==null,"Invalid frozen v3 street fixture");
        Require(session.V3Directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"V3 fixture outside test-output");
        System.IO.Directory.CreateDirectory(session.V3Directory);
        var path=System.IO.Path.Combine(session.V3Directory,(manual?"manual":"save")+(backup?".bak":"")+".json");
        System.IO.File.WriteAllText(path,SnapshotJson(source));return path;
    }
}
