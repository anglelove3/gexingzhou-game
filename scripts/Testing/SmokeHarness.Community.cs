using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task CommunityChecks()
    {
        var problems=new List<string>();
        using var saved=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();
        if(saved.Mode!=WorldMode.Depth2D)problems.Add("Community still Horizontal");
        if(saved.GetNodeOrNull<PlayerController>("DepthLayers/Actors/Player")==null)problems.Add("Static DepthLayers/Actors/Player missing");
        if(saved.ViewBounds!=new Rect2(0,0,1600,540))problems.Add("Community dimensions not 1600x540");
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();
        if(session.Snapshot.SchemaVersion!=3||session.Snapshot.PlayerPosition!=new Position2(320,460))problems.Add("NewGame still uses old protocol or foot lane");
        if(problems.Count>0)throw new Exception(string.Join("; ",problems));
        var authored=NavigationSceneReader.Read(saved);
        Require(NavigationSceneReader.Matches(authored,session.Navigation.Profiles["community_gate"]),"Community export mismatch");
        Require(authored.Obstacles.Length==5&&authored.FootRadius==8,"Community footprint contract");
        Require(authored.Anchors["stand"]==new Position2(960,440)&&authored.Anchors["seat"]==new Position2(960,380),"Community seat units");
        foreach(var anchor in new[]{"StandAnchor","SeatAnchor"}){
            var bench=saved.GetTarget("bench")!;var marker=bench.GetNode<Marker2D>(anchor);var prior=marker.Position;marker.Position+=new Vector2(1,0);
            bool rejected=false;try{NavigationSceneReader.Read(saved);}catch(InvalidOperationException){rejected=true;}marker.Position=prior;
            Require(rejected,"Community bench marker mismatch accepted "+anchor);
        }
        foreach(var path in new[]{"DepthLayers/Actors/Cannon","DepthLayers/Props","Navigation/GroundBoundary/CollisionPolygon2D","Foreground"})Require(saved.GetNodeOrNull(path)!=null,"Community layer missing "+path);
        saved.Free();
        var main=await NewPolishMain();var player=main.World.Player;
        Require(main.World.Mode==WorldMode.Depth2D&&!main.World.HasMeta("binding_error"),"Community runtime binding failed");
        var before=player.Position;Input.ActionPress("move_up");await DepthPhysics(20);Input.ActionRelease("move_up");await DepthPhysics(12);Require(player.Position.Y<before.Y-10,"Community cannot move up");
        Input.ActionPress("move_down");await DepthPhysics(20);Input.ActionRelease("move_down");await DepthPhysics(12);Require(player.Position.Y>before.Y-10,"Community cannot move down");
        player.Position=new(400,460);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");Input.ActionPress("move_right");await DepthPhysics(10);
        Require(Math.Abs(player.Velocity.Length()-112)<.05,"Community diagonal speed");Input.ActionPress("move_fast");await DepthPhysics(15);Require(Math.Abs(player.Velocity.Length()-168)<.05,"Community diagonal running speed");
        Input.ActionRelease("move_fast");Input.ActionRelease("move_up");Input.ActionRelease("move_right");await DepthPhysics(15);
        player.Position=new(800,440);player.Velocity=Vector2.Zero;Input.ActionPress("move_right");await DepthPhysics(150);Input.ActionRelease("move_right");await DepthPhysics(15);
        Require(player.Position.X>1050&&NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Community main lane is disconnected");
        player.Position=new(960,460);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");await DepthPhysics(80);Input.ActionRelease("move_up");await DepthPhysics(12);
        Require(player.Position.Y>=421.98&&NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Community crossed bench base");
        using var cue=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[44100*6]};main.Audio.FootstepStream=cue;
        foreach(var polygon in authored.Obstacles){
            var low=polygon.Min(v=>v.Y);var high=polygon.Max(v=>v.Y);var x=(polygon.Min(v=>v.X)+polygon.Max(v=>v.X))/2;
            player.Position=new(x,high+18);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");await DepthPhysics(55);
            Require(player.Position.Y>=high+7.98&&NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Community obstacle crossed");
            main.Audio.StopTransient();await DepthPhysics(20);Require(main.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().All(p=>!p.Playing),"Community wall generates footsteps");
            Input.ActionRelease("move_up");await DepthPhysics(12);
        }
        foreach(var test in new[]{("move_left",new Vector2(60,460)),("move_right",new Vector2(1540,460)),("move_up",new Vector2(120,340)),("move_down",new Vector2(120,480))}){
            player.Position=test.Item2;player.Velocity=Vector2.Zero;Input.ActionPress(test.Item1);await DepthPhysics(65);Input.ActionRelease(test.Item1);await DepthPhysics(12);
            Require(NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Community ground boundary crossed "+test.Item1);
        }
        var display=main.GetNode<WorldDisplayController>("WorldDisplay");Require(main.World.ViewBounds.Grow(.02f).Encloses(display.VisibleWorldRect),"Community camera leaves artwork");
        player.Position=new(120,460);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");Input.ActionPress("move_right");Input.ActionPress("move_fast");await DepthPhysics(5);
        player.Notification((int)NotificationApplicationFocusOut);await DepthPhysics(8);var lost=player.Position;player.Notification((int)NotificationApplicationFocusIn);await DepthPhysics(12);
        Require(player.Position==lost&&player.Velocity==Vector2.Zero,"Focus held diagonal drift");
        Input.ActionRelease("move_up");Input.ActionRelease("move_right");Input.ActionRelease("move_fast");await DepthPhysics(3);Input.ActionPress("move_right");await DepthPhysics(12);Input.ActionRelease("move_right");await DepthPhysics(12);Require(player.Position.X>lost.X+5,"Released keys cannot resume movement");
        main.Free();await Frames(2);GD.Print("COMMUNITY_PASS Layout FourWay Collisions NormalizedRun FocusHeldDiagonal");
    }
    private async Task CommunityUpgradeChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();
        Require(session.Snapshot.SchemaVersion==3,"Upgrade runtime not v3");
        Require(typeof(GameSession).GetProperty("V2Directory")!=null&&typeof(GameSession).GetProperty("FrozenV2Navigation")!=null,"Frozen v2 runtime boundary missing");
        Require(session.Saves.Load().Status==LoadStatus.NotFound,"Community upgrade suite not isolated");
        foreach(var version in new[]{1,2})foreach(var manual in new[]{false,true})foreach(var backup in new[]{false,true}){
            var fixture=UpgradeFixture();var source=new WorldSnapshot{SchemaVersion=version,ContentVersion=version==1?"vs01-0.1":"vs01-0.2",PlayerPosition=new(960,280),CompletedActions=new(){"observation.community.first"}};
            if(version==1)WriteLegacy(fixture,manual,backup,source);else WriteV2(fixture,manual,backup,source);
            var oldDirectory=version==1?fixture.LegacyDirectory:fixture.V2Directory;var hash=CommunityFileState(oldDirectory);
            var snapshot=fixture.Snapshot;var flow=fixture.Flow;
            Require(fixture.ProbeResume(manual,backup).Source==(version==1?VersionedResumeSource.Legacy:VersionedResumeSource.V2),"Upgrade selected wrong source");
            var upgraded=fixture.TryUpgradePrevious(manual,backup);Require(upgraded.Status==LoadStatus.Loaded,upgraded.Message);
            Require(upgraded.Snapshot!.PlayerPosition==new Position2(960,460)&&upgraded.Snapshot.CompletedActions.Contains("observation.community.first"),"Community upgrade mapped facts/position incorrectly");
            Require(CommunityFileState(oldDirectory)==hash&&ReferenceEquals(fixture.Snapshot,snapshot)&&fixture.Flow==flow,"Upgrade wrote old files or restored runtime early");
            Require((manual?fixture.Saves:fixture.ManualSaves).Load().Status==LoadStatus.NotFound,"Upgrade crossed manual/auto slots");
            fixture.Free();await Frames();
        }
        foreach(var version in new[]{2,3}){
            var fixture=UpgradeFixture();WriteLegacy(fixture,false,snapshot:new());WriteV2(fixture,false,false);
            var directory=version==3?fixture.SaveDirectory:fixture.V2Directory;System.IO.Directory.CreateDirectory(directory);
            var current=SaveV3Codec.CreateNew(new(),fixture.Navigation);var source=version==3?current:SaveV2Codec.CreateNew(new());
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"save.bak.json"),SnapshotJson(source));
            if(version==2)System.IO.File.Delete(System.IO.Path.Combine(directory,"save.json"));
            var before=CommunityFileState(fixture.LegacyDirectory)+CommunityFileState(fixture.V2Directory);
            Require(fixture.ProbeResume(false).Source==VersionedResumeSource.Blocked,"Backup only downgraded automatically");
            Require(fixture.ProbeResume(false,true).Source==(version==3?VersionedResumeSource.Current:VersionedResumeSource.V2),"Explicit backup read wrong version");
            Require(fixture.TryUpgradePrevious(false).Status!=LoadStatus.Loaded,"Backup-only upgrade overwrote current files");
            Require(before==CommunityFileState(fixture.LegacyDirectory)+CommunityFileState(fixture.V2Directory),"Backup probe modified old files");fixture.Free();await Frames();
        }
        {
            var fixture=UpgradeFixture();WriteLegacy(fixture,false,snapshot:new());System.IO.Directory.CreateDirectory(fixture.V2Directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(fixture.V2Directory,"save.json"),"{\"schema_version\":9,\"content_version\":\"future\"}");
            Require(fixture.ProbeResume(false).Source==VersionedResumeSource.Blocked&&fixture.TryUpgradePrevious(false).Status==LoadStatus.UnsupportedVersion,"Bad v2 silently downgraded");fixture.Free();await Frames();
        }
        foreach(var invalid in new[]{"backdrop","rest_frames","depth_frames"}){
            var fixture=UpgradeFixture();WriteV2(fixture,false,false);var old=CommunityFileState(fixture.V2Directory);var snapshot=fixture.Snapshot;
            var resources=(Dictionary<string,PackedScene>)DiagnosticField(fixture,"sceneResources")!;
            var path="res://scenes/world/CommunityGate.tscn";var original=resources[path];
            var candidate=original.Instantiate<WorldView>();
            if(invalid=="backdrop")candidate.GetNode("Backdrop").Free();
            else if(invalid=="rest_frames")candidate.GetNode<PlayerController>("DepthLayers/Actors/Player").RestFrames=new SpriteFrames();
            else candidate.GetNode<PlayerController>("DepthLayers/Actors/Player").DepthFrames=new SpriteFrames();
            using var broken=new PackedScene();Require(broken.Pack(candidate)==Error.Ok,"Broken candidate fixture pack failed");candidate.Free();resources[path]=broken;
            try {Require(fixture.TryUpgradePrevious(false).Status==LoadStatus.Corrupt,"Invalid candidate wrote v3");Require(fixture.Saves.Load().Status==LoadStatus.NotFound&&CommunityFileState(fixture.V2Directory)==old&&ReferenceEquals(fixture.Snapshot,snapshot),"Candidate failure changed runtime/source");}
            finally{resources[path]=original;fixture.Free();}await Frames();
        }
        foreach(var condition in new[]{"v3_valid","v3_corrupt","v3_parent_file","v2_valid","v2_corrupt","v2_parent_file","v1_valid"}){
            var fixture=UpgradeFixture();var oldDirectory=System.IO.Path.Combine(fixture.LegacyDirectory,"preferences");Require(new SettingsRepository(oldDirectory).Save(new(){SubtitleSize=32}).Success,"Old preference fixture");
            var v2Directory=System.IO.Path.Combine(fixture.V2Directory,"preferences");var currentDirectory=System.IO.Path.Combine(fixture.SaveDirectory,"preferences");
            var selected=condition.StartsWith("v3")?currentDirectory:v2Directory;
            if(condition.EndsWith("valid")&&condition!="v1_valid")Require(new SettingsRepository(selected).Save(new(){SubtitleSize=20}).Success,"Priority preference fixture");
            if(condition.EndsWith("corrupt")){System.IO.Directory.CreateDirectory(selected);System.IO.File.WriteAllText(System.IO.Path.Combine(selected,"settings.json"),"{broken");}
            if(condition.EndsWith("parent_file")){System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(selected)!);System.IO.File.WriteAllText(selected,"preserve me");}
            var oldHash=CommunityFileState(fixture.LegacyDirectory)+CommunityFileState(fixture.V2Directory);
            typeof(GameSession).GetMethod("InitializePreferences",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(fixture,null);
            Require(fixture.Options.SubtitleSize==(condition is "v3_valid" or "v2_valid"?20:condition=="v1_valid"?32:24),"Preference precedence wrong "+condition);
            Require(oldHash==CommunityFileState(fixture.LegacyDirectory)+CommunityFileState(fixture.V2Directory),"Preferences changed old directories");
            if(condition.EndsWith("corrupt")||condition.EndsWith("parent_file"))Require(fixture.EventWarning.Length>0,"Preference failure hidden "+condition);
            fixture.Free();await Frames();
        }
        WriteV2(session,false,false);var files=CommunityFileState(session.V2Directory);
        var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(2);
        boot.GetNode<Button>("MenuScroll/Menu/LegacyAutoUpgradeButton").EmitSignal(Button.SignalName.Pressed);await Frames();
        Require(boot.GetNode<ChoiceController>("Choices").IsOpen&&session.Saves.Load().Status==LoadStatus.NotFound,"V2 menu bypassed confirmation");
        boot.GetNode<ChoiceController>("Choices").HandleKey(Key.Escape);await Frames();Require(session.Saves.Load().Status==LoadStatus.NotFound&&CommunityFileState(session.V2Directory)==files,"Canceled V2 upgrade mutated files");boot.Free();await Frames();
        GD.Print("COMMUNITY_UPGRADE_PASS RuntimeBoundary AllOldSlots SourceHashes BackupOnly BadV2 CandidateFailure PreferencesParentIsFile Cancel");
    }
    private static void WriteV2(GameSession session,bool manual,bool backup,WorldSnapshot? snapshot=null)
    {
        System.IO.Directory.CreateDirectory(session.V2Directory);var source=snapshot??SaveV2Codec.CreateNew(new());
        Require(new SaveV2Codec(session.FrozenV2Navigation).Validate(source)==null,"Invalid frozen v2 fixture");
        System.IO.File.WriteAllText(System.IO.Path.Combine(session.V2Directory,(manual?"manual":"save")+(backup?".bak":"")+".json"),SnapshotJson(source));
    }
    private static string CommunityFileState(string directory)
    {
        Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Community file fixture outside project");
        return !System.IO.Directory.Exists(directory)?"missing":string.Join(";",System.IO.Directory.GetFiles(directory,"*",System.IO.SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p+":"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(p)))));
    }
}
