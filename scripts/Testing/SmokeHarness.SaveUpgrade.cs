using Godot;
using GeXingzhou.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class SmokeHarness
{
    // Each real session _Ready selects a new isolated test root; no user:// fixture.
    private GameSession UpgradeFixture()
    {
        var session=new GameSession();AddChild(session);session.NewGame();
        Require(session.SaveDirectory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Upgrade fixture outside test-output");
        Require(session.LegacyDirectory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Legacy fixture outside test-output");
        return session;
    }
    private static void WriteLegacy(GameSession session,bool manual,bool backup=false,WorldSnapshot? snapshot=null)
    {
        System.IO.Directory.CreateDirectory(session.LegacyDirectory);
        var options=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}};
        var name=manual?"manual":"save";
        System.IO.File.WriteAllText(System.IO.Path.Combine(session.LegacyDirectory,name+(backup?".bak":"")+".json"),JsonSerializer.Serialize(snapshot??new WorldSnapshot{SceneId="soup_shop",PlayerPosition=new(440,280),Stage=SliceStage.CandyHeyDelivered},options));
    }
    private async Task SaveUpgradeChecks()
    {
        var root=GetNode<GameSession>("/root/GameSession");root.NewGame();
        Require(root.Snapshot.SchemaVersion==2&&root.Snapshot.ContentVersion=="vs01-0.2","Production NewGame still uses v1");
        Require(!root.Restore(new()).Success,"Production Restore silently upgrades v1");
        foreach(var manual in new[]{false,true})foreach(var backup in new[]{false,true})
        {
            var session=UpgradeFixture();WriteLegacy(session,manual,backup);
            var path=System.IO.Path.Combine(session.LegacyDirectory,(manual?"manual":"save")+(backup?".bak":"")+".json");
            var bytes=System.IO.File.ReadAllBytes(path);var play=session.Snapshot.PlaythroughId;
            Require(session.ProbeResume(manual,backup).Source==ResumeSource.Legacy,"Explicit upgrade missing");
            var loaded=session.TryUpgradeLegacy(manual,backup);
            Require(loaded.Status==LoadStatus.Loaded&&loaded.Snapshot!.PlayerPosition==session.Navigation.Profiles["soup_shop"].Anchors["safe"],"Legacy soup position not mapped");
            Require(System.IO.File.ReadAllBytes(path).SequenceEqual(bytes),"Migration rewrote source");
            Require((manual?session.ManualSaves:session.Saves).Load().Status==LoadStatus.Loaded,"Migration continued before new slot saved");
            Require(session.Snapshot.PlaythroughId==play,"Probe or upgrade silently restored gameplay");
            Require(session.ProbeResume(manual).Source==ResumeSource.V2,"New slot not preferred");session.Free();
        }
        foreach(var condition in new[]{"corrupt","unknown","backup_only"})
        {
            var session=UpgradeFixture();WriteLegacy(session,false);System.IO.Directory.CreateDirectory(session.SaveDirectory);
            if(condition=="backup_only"){
                Require(session.Saves.Save(SaveV2Codec.CreateNew(new())).Success,"V2 seed failed");
                System.IO.File.Move(System.IO.Path.Combine(session.SaveDirectory,"save.json"),System.IO.Path.Combine(session.SaveDirectory,"save.bak.json"));
                Require(session.ProbeResume(false,true).Source==ResumeSource.V2,"V2 backup inaccessible");
            }else System.IO.File.WriteAllText(System.IO.Path.Combine(session.SaveDirectory,"save.json"),condition=="corrupt"?"{broken":"{\"schema_version\":999,\"content_version\":\"future\"}");
            Require(session.ProbeResume(false).Source==ResumeSource.Blocked,"Bad v2 silently fell back to v1");
            Require(session.TryUpgradeLegacy(false).Status!=LoadStatus.Loaded,"Upgrade overwrote existing v2 slot");session.Free();
        }
        {
            var session=UpgradeFixture();WriteLegacy(session,false);
            System.IO.File.WriteAllText(System.IO.Path.Combine(session.LegacyDirectory,"save.json"),"{\"schema_version\":999,\"content_version\":\"future\"}");
            Require(session.ProbeResume(false).Source==ResumeSource.Blocked&&session.TryUpgradeLegacy(false).Status==LoadStatus.UnsupportedVersion,"Unknown legacy guessed");
            Require(!System.IO.File.Exists(System.IO.Path.Combine(session.SaveDirectory,"save.json")),"Unknown legacy wrote new slot");session.Free();
        }
        foreach(var condition in new[]{"missing","new_valid","new_corrupt","copy_failure"})
        {
            var session=UpgradeFixture();var oldDirectory=System.IO.Path.Combine(session.LegacyDirectory,"preferences");
            var oldPreferences=new SettingsRepository(oldDirectory);Require(oldPreferences.Save(new(){SubtitleSize=32,ReducedMotion=true}).Success,"Old settings fixture failed");
            var oldPath=System.IO.Path.Combine(oldDirectory,"settings.json");var oldBytes=System.IO.File.ReadAllBytes(oldPath);
            var newDirectory=System.IO.Path.Combine(session.SaveDirectory,"preferences");
            if(condition=="new_valid")Require(new SettingsRepository(newDirectory).Save(new(){SubtitleSize=20}).Success,"New settings fixture failed");
            if(condition=="new_corrupt"){System.IO.Directory.CreateDirectory(newDirectory);System.IO.File.WriteAllText(System.IO.Path.Combine(newDirectory,"settings.json"),"{broken");}
            if(condition=="copy_failure"){System.IO.Directory.CreateDirectory(session.SaveDirectory);System.IO.File.WriteAllText(newDirectory,"blocked");}
            typeof(GameSession).GetMethod("InitializePreferences",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(session,null);
            Require(session.Options.SubtitleSize==(condition=="new_valid"?20:condition=="new_corrupt"?24:32),"Preferences priority/fallback wrong "+condition);
            Require(System.IO.File.ReadAllBytes(oldPath).SequenceEqual(oldBytes),"Preferences import changed old file");
            if(condition=="copy_failure")Require(session.EventWarning.Length>0,"Settings copy failure hidden");
            if(condition=="missing")Require(new SettingsRepository(newDirectory).Load().SubtitleSize==32,"Legacy settings not copied");
            WriteLegacy(session,false);var migrated=session.TryUpgradeLegacy(false);
            Require(migrated.Snapshot!.Settings.SubtitleSize==session.Options.SubtitleSize,"Save settings overrode machine preference");session.Free();
        }
        WriteLegacy(root,false);WriteLegacy(root,true,true);
        var legacyPath=System.IO.Path.Combine(root.LegacyDirectory,"save.json");var legacyBytes=System.IO.File.ReadAllBytes(legacyPath);
        var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(2);
        var upgrade=boot.GetNodeOrNull<Button>("MenuScroll/Menu/LegacyAutoUpgradeButton");
        Require(upgrade is {Visible:true,Disabled:false},"Explicit upgrade menu not connected");
        var recovery=boot.GetNodeOrNull<Button>("MenuScroll/Menu/ManualRecoveryButton");Require(recovery is {Visible:true,Disabled:false},"Legacy manual backup menu absent");
        upgrade!.EmitSignal(Button.SignalName.Pressed);await Frames(1);
        Require(boot.GetNode<ChoiceController>("Choices").IsOpen&&root.Saves.Load().Status==LoadStatus.NotFound,"Upgrade saved before confirmation");
        boot.GetNode<ChoiceController>("Choices").HandleKey(Key.Escape);await Frames(1);
        Require(root.Saves.Load().Status==LoadStatus.NotFound&&System.IO.File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes),"Canceled upgrade mutated slots");
        upgrade.EmitSignal(Button.SignalName.Pressed);await Frames(1);GetTree().CurrentScene=null;boot.GetNode<ChoiceController>("Choices").HandleKey(Key.E);await Frames(8);
        Require(root.Saves.Load().Status==LoadStatus.Loaded&&GetTree().CurrentScene is MainView,"Confirmed upgrade did not continue");
        Require(System.IO.File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes),"Menu upgrade modified legacy source");
        var upgradedMain=GetTree().CurrentScene;GetTree().CurrentScene=null;upgradedMain!.Free();boot.Free();await Frames(2);
        var main=await NewDepthSoupMain(new(120,480));var beforeWorld=main.World;var before=root.Snapshot;
        Require(!main.ChangeWorld("soup_shop",new(500,430))&&ReferenceEquals(beforeWorld,main.World)&&root.Snapshot==before,"Blocked position replaced valid world");
        main.Phone.Open("messages");KeyPress(Key.F9);await Frames(3);
        Require(main.Phone.IsOpen&&ReferenceEquals(beforeWorld,main.World)&&root.Snapshot.PlaythroughId==before.PlaythroughId,"Failed F9 canceled old phone/world");
        main.Phone.Close();if(main.Dialogue.IsOpen)main.Dialogue.HandleKey(Key.Escape);root.Flow=FlowState.Field;
        KeyPress(Key.F5);await Frames(2);Require(root.ManualSaves.Load().Snapshot?.PlayerPosition==main.SafeSavePosition,"F5 lost depth position");
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(8);var reopened=GetTree().CurrentScene as MainView;
        Require(reopened!=null&&reopened!=main&&reopened.World.Player.Position==new Vector2(120,480)&&!reopened.SoupSeat.IsActive,"F9 did not restore standing depth position");
        main.Free();GetTree().CurrentScene=null;reopened!.Free();await Frames(2);
        foreach(var (manual,backup) in new[]{(false,true),(true,false),(true,true)})
        {
            Require(root.SaveDirectory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Menu fixtures outside test-output");
            foreach(var directory in new[]{root.SaveDirectory,root.LegacyDirectory})foreach(var name in new[]{"save.json","save.bak.json","manual.json","manual.bak.json"})System.IO.File.Delete(System.IO.Path.Combine(directory,name));
            root.NewGame();WriteLegacy(root,manual,backup);
            var sourcePath=System.IO.Path.Combine(root.LegacyDirectory,(manual?"manual":"save")+(backup?".bak":"")+".json");var sourceBytes=System.IO.File.ReadAllBytes(sourcePath);
            var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(menu);await Frames(2);
            var button=menu.GetNode<Button>("MenuScroll/Menu/"+(backup?(manual?"ManualRecoveryButton":"RecoveryButton"):(manual?"LegacyManualUpgradeButton":"LegacyAutoUpgradeButton")));
            Require(button.Visible&&!button.Disabled,"Slot upgrade menu unavailable");button.EmitSignal(Button.SignalName.Pressed);await Frames(1);
            Require(menu.GetNode<ChoiceController>("Choices").IsOpen&&(manual?root.ManualSaves:root.Saves).Load().Status==LoadStatus.NotFound,"Menu bypassed upgrade confirmation");
            GetTree().CurrentScene=null;menu.GetNode<ChoiceController>("Choices").HandleKey(Key.E);await Frames(8);
            var resumed=GetTree().CurrentScene as MainView;Require(resumed!=null&&(manual?root.ManualSaves:root.Saves).Load().Status==LoadStatus.Loaded,"Menu upgrade did not save target slot");
            Require((manual?root.Saves:root.ManualSaves).Load().Status==LoadStatus.NotFound&&System.IO.File.ReadAllBytes(sourcePath).SequenceEqual(sourceBytes),"Menu upgrade crossed slots or modified source");
            GetTree().CurrentScene=null;resumed!.Free();menu.Free();await Frames(2);
        }
        GD.Print("SAVE_UPGRADE_PASS ExplicitAuto Manual Backups V2Priority BackupOnly BadV2 UnknownLegacy PreferencesPriority F5F9 FailedLoad");
    }
}
