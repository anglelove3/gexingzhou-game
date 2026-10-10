using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task ExperienceEndChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=true,RecordEventsEnabled=true},false);
        await MemoryPath(false,0,false);
        var log=System.IO.Path.Combine(s.SaveDirectory,"behavior","events.jsonl");var count=System.IO.File.ReadAllLines(log).Length;
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        Require(main.SliceEnd.IsOpen&&s.Flow==FlowState.EndCard&&!main.CanPause,"Completed save has no end card");
        Require(main.SliceEnd.GetNode<Label>("Panel/Title").Text=="今天先到这里。明天见。"&&main.SliceEnd.GetNode<Label>("Panel/Label").Text=="首段试玩","Wrong ending copy");
        Require(main.SliceEnd.GetNode<Button>("Panel/Scroll/Content/Continue").HasFocus(),"Ending default focus missing");
        foreach(var button in new[]{"Continue","Menu","Restart"})
        {
            var control=main.SliceEnd.GetNode<Button>("Panel/Scroll/Content/"+button);control.GrabFocus();await Frames(2);Require(control.HasFocus(),"Ending button inaccessible "+button);
        }
        Require(System.IO.File.ReadAllLines(log).Length==count,"Loading completed save re-recorded ending");
        KeyPress(Key.Tab);await Frames(2);Require(!main.Phone.IsOpen&&main.SliceEnd.IsOpen,"Ending Tab opened phone");
        var time=s.Snapshot.SceneActiveMilliseconds.GetValueOrDefault("soup_shop");s.AdvanceClock(30);
        Require(time==s.Snapshot.SceneActiveMilliseconds.GetValueOrDefault("soup_shop"),"End card did not freeze clock");
        Require(!s.TryDispatch(new("slice.complete","completed","slice-1")).Applied&&System.IO.File.ReadAllLines(log).Length==count,"Repeat completion appended event");
        KeyPress(Key.Escape);await Frames(100);Require(!main.SliceEnd.IsOpen&&s.Flow==FlowState.Field&&!main.Pause.IsOpen,"Ending closed twice or reopened");
        main.Free();await Frames(2);
        var loaded=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(loaded);await Frames(3);
        Require(loaded.SliceEnd.IsOpen&&System.IO.File.ReadAllLines(log).Length==count,"Reload ending duplicated event");
        var path=TestAutoPath();var original=System.IO.File.ReadAllBytes(path);var play=s.Snapshot.PlaythroughId;
        using(var locked=new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.None))
        {
            Require(!loaded.SliceEnd.TryRestart()&&s.Snapshot.PlaythroughId==play&&loaded.SliceEnd.IsOpen,"Restart ignored preservation failure");
        }
        Require(System.IO.File.ReadAllBytes(path).SequenceEqual(original),"Failed restart altered original save");
        const string unknown="{\"schema_version\":999,\"content_version\":\"future\"}";
        System.IO.File.WriteAllText(path,unknown);
        Require(!loaded.SliceEnd.TryReturnToMenu()&&loaded.SliceEnd.IsOpen&&System.IO.File.ReadAllText(path)==unknown,"Ending menu bypassed unknown-version protection");
        Require(loaded.SliceEnd.GetNode<Label>("Panel/Scroll/Content/Message").Text.Length>0,"Ending save failure invisible");
        System.IO.File.WriteAllBytes(path,original);Require(s.ManualSaves.Save(s.Snapshot).Success,"Restart manual fixture");
        GetTree().CurrentScene=null;Require(loaded.SliceEnd.TryRestart(),"Normal restart failed");await Frames(8);
        var restarted=GetTree().CurrentScene as MainView;
        Require(restarted!=null&&s.Snapshot.Stage==SliceStage.FreeArrival&&s.Snapshot.PlaythroughId!=play&&!restarted.SliceEnd.IsOpen,"Restart did not create fresh actual Main");
        Require(System.IO.Directory.GetFiles(s.SaveDirectory,"preserved-*.json").Length>=2,"Both slots not preserved before restart");
        GetTree().CurrentScene=null;loaded.Free();restarted!.Free();await Frames(2);
    }
    private async Task EndVisualChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var complete=SaveV3Codec.CreateNew(s.Options,s.Navigation) with{Stage=SliceStage.SliceComplete,SceneId="soup_shop",PlayerPosition=new(400,430),MemoryOrdinal=1,
                MemoryState=new(){PushedCoinIds=new(){"c1","c2","c3","c4"},PushedTotal=5,FoodChoice=FoodChoice.Take,Completed=true},
                CompletedActions=new(){"slice.complete:slice-1"},ReturnContext=new("soup_shop",new(400,430),"soup.return",true)};
            Require(s.Restore(complete).Success,"End visual snapshot invalid");
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
            Require(main.GetGlobalRect().Encloses(main.SliceEnd.GetNode<Control>("Panel").GetGlobalRect()),"End card clipped");
            foreach(var button in main.SliceEnd.FindChildren("*","Button",true,false).OfType<Button>())Require(main.GetGlobalRect().Encloses(button.GetGlobalRect()),"End button clipped");
            await Capture("end-"+font);var path=TestAutoPath();
            System.IO.File.WriteAllText(path,"{\"schema_version\":999,\"content_version\":\"future\"}");
            Require(!main.SliceEnd.TryReturnToMenu(),"End failure gallery navigated");await Frames(3);
            await Capture("end-save-failed-"+font);System.IO.File.Delete(path);main.Free();await Frames(2);
        }
    }
    private string TestAutoPath()
    {
        var directory=GetNode<GameSession>("/root/GameSession").SaveDirectory;
        Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Fixture must not touch user saves");
        System.IO.Directory.CreateDirectory(directory);
        return System.IO.Path.Combine(directory,"save.json");
    }
    private async Task PauseVisualChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();Require(main.Pause.Open(),"Pause gallery open failed");await Frames(3);
            Require(main.GetGlobalRect().Encloses(main.Pause.GetNode<Control>("Panel").GetGlobalRect()),"Pause panel clipped");
            var title=main.Pause.FindChildren("Title","Label",true,false).OfType<Label>().Single();
            var scroll=main.Pause.GetNode<ScrollContainer>("Panel/Scroll");
            Require(!scroll.IsAncestorOf(title)||scroll.GetGlobalRect().Encloses(title.GetGlobalRect()),"Focused pause clips title");
            await Capture("pause-"+font);
            main.Pause.GetNode<Button>("Panel/Scroll/Content/Settings").EmitSignal(Button.SignalName.Pressed);await Frames(3);
            await Capture("pause-settings-"+font);main.Settings.Close();await Frames(2);
            var path=TestAutoPath();System.IO.Directory.CreateDirectory(path);
            Require(!main.Pause.TryReturnToMenu(),"Pause failure gallery navigated");System.IO.Directory.Delete(path);
            await Frames(3);await Capture("pause-save-failed-"+font);main.Free();await Frames(2);
        }
    }
    private async Task ExperiencePauseChecks()
    {
        var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
        KeyPress(Key.Escape);await Frames(2);Require(s.Flow==FlowState.Paused&&main.Pause.IsOpen,"Esc did not open pause");
        var invitation=s.Snapshot.InvitationState;var time=s.Snapshot.SceneActiveMilliseconds.GetValueOrDefault("community_gate");var x=main.World.Player.Position.X;
        s.AdvanceClock(30);Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
        for(int i=0;i<8;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
        Require(s.Snapshot.InvitationState==invitation&&s.Snapshot.SceneActiveMilliseconds.GetValueOrDefault("community_gate")==time&&main.World.Player.Position.X==x,"Pause advanced clock or player");
        main.Pause.GetNode<Button>("Panel/Scroll/Content/Settings").EmitSignal(Button.SignalName.Pressed);await Frames(2);
        Require(main.Settings.IsOpen&&main.Pause.IsOpen,"Pause settings not opened");KeyPress(Key.Escape);await Frames(2);
        Require(s.Flow==FlowState.Paused&&main.Pause.IsOpen&&!main.Settings.IsOpen,"Esc closed settings and pause together");
        var path=TestAutoPath();Require(!System.IO.File.Exists(path),"Failure fixture unexpectedly has auto save");
        System.IO.Directory.CreateDirectory(path);
        Require(!main.Pause.TryReturnToMenu()&&!main.Pause.TryExit()&&main.Pause.IsOpen,"IO failure ignored by navigation");
        Require(main.Pause.GetNode<Label>("Panel/Scroll/Content/Message").Text.Contains("未能"),"Save failure invisible");
        System.IO.Directory.Delete(path);
        const string unknown="{\"schema_version\":999,\"content_version\":\"future\"}";
        System.IO.File.WriteAllText(path,unknown);
        Require(!main.Pause.TryReturnToMenu()&&!main.Pause.TryExit()&&System.IO.File.ReadAllText(path)==unknown&&main.Pause.IsOpen,"Unknown version overwritten or navigation escaped");
        System.IO.File.Delete(path);Require(main.Pause.Save().Success,"Normal manual save failed");
        main.Pause.Close();await Frames(2);Require(s.Flow==FlowState.Field,"Resume did not restore field");
        main.Phone.Open("messages");KeyPress(Key.Escape);await Frames(2);Require(!main.Phone.IsOpen&&!main.Pause.IsOpen,"Phone Esc also opened pause");
        main.ShowNotice("测试","关闭后仍回自由活动。");KeyPress(Key.Escape);await Frames(2);Require(!main.Dialogue.IsOpen&&!main.Pause.IsOpen,"Dialogue Esc also opened pause");
        var memory=SaveV3Codec.CreateNew(s.Options,s.Navigation) with{Stage=SliceStage.MemoryActive,SceneId="memory_soup_table",PlayerPosition=new(320,280),MemoryState=new(),MemoryOrdinal=1,ReturnContext=new("soup_shop",new(400,430),"soup.return",true)};
        Require(s.Restore(memory).Success&&main.EnterMemoryView(),"Pause memory fixture failed");s.Flow=FlowState.Memory;
        KeyPress(Key.Escape);await WaitUntil(()=>main.Memory==null&&s.Flow==FlowState.Field,"Memory escape");
        Require(!main.Pause.IsOpen,"Memory Esc also paused");main.Free();await Frames(2);
        var benchMain=await NewPolishMain();var bench=(BenchView)benchMain.World.GetTarget("bench")!;benchMain.World.Player.GlobalPosition=bench.StandAnchor.GlobalPosition;
        KeyPress(Key.E);await WaitForPhase(benchMain,RestPhase.Seated);Require(!benchMain.CanPause,"Bench can pause");
        KeyPress(Key.Escape);await Frames(2);Require(!benchMain.Pause.IsOpen&&benchMain.Rest.IsActive,"Bench menu Esc also paused");
        KeyPress(Key.Escape);await WaitForPhase(benchMain,RestPhase.Standing);Require(!benchMain.Pause.IsOpen,"Stand Esc also paused");benchMain.Free();await Frames(2);
        var soup=await NewSoupMain(true);soup.SoupSeat.Begin(()=>{});Require(!soup.CanPause,"Can pause in sit transition");
        await WaitUntil(()=>!soup.SoupSeat.IsActing,"Pause soup seated");Require(!soup.CanPause,"Can pause while soup seated");
        KeyPress(Key.Escape);await WaitUntil(()=>!soup.SoupSeat.IsActive,"Pause soup stand");Require(!soup.Pause.IsOpen,"Soup Esc also paused");soup.Free();await Frames(2);
        var nav=await NewPolishMain();nav.World.Player.Position=new(900,460);nav.Pause.Open();KeyPress(Key.F5);await Frames(2);
        Require(s.ManualSaves.Load().Snapshot?.PlayerPosition.Y==460,"Paused F5 lost safe position");
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(8);
        var manual=GetTree().CurrentScene as MainView;
        Require(manual!=null&&s.Flow==FlowState.Field&&manual.World.Player.Position.X==900,"Paused F9 did not restore actual Main");
        nav.Free();nav=manual!;Require(nav.Pause.Open(),"Pause after manual restore failed");
        GetTree().CurrentScene=null;Require(nav.Pause.TryReturnToMenu(),"Normal menu navigation failed");await Frames(8);
        var boot=GetTree().CurrentScene as BootMenu;Require(boot!=null,"Navigation did not show actual Boot");nav.Free();
        GetTree().CurrentScene=null;boot!.GetNode<Button>("MenuScroll/Menu/AutoResumeButton").EmitSignal(Button.SignalName.Pressed);await Frames(8);
        var continued=GetTree().CurrentScene as MainView;Require(continued!=null&&continued.World.Player.Position.X==900&&s.Flow==FlowState.Field,"Menu continue failed safe restore");
        GetTree().CurrentScene=null;boot.Free();continued!.Free();await Frames(2);
    }
}
