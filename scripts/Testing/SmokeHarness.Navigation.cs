using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private string TestAutoPath()
    {
        var directory=GetNode<GameSession>("/root/GameSession").SaveDirectory;
        Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Fixture must not touch user saves");
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
        var memory=new WorldSnapshot{Stage=SliceStage.MemoryActive,SceneId="memory_soup_table",MemoryState=new(),MemoryOrdinal=1,ReturnContext=new("soup_shop",new(440,280),"soup.return",true)};
        Require(s.Restore(memory).Success&&main.EnterMemoryView(),"Pause memory fixture failed");s.Flow=FlowState.Memory;
        KeyPress(Key.Escape);await WaitUntil(()=>main.Memory==null&&s.Flow==FlowState.Field,"Memory escape");
        Require(!main.Pause.IsOpen,"Memory Esc also paused");main.Free();await Frames(2);
        var benchMain=await NewPolishMain();var bench=benchMain.World.GetNode<BenchView>("Bench");benchMain.World.Player.GlobalPosition=bench.StandAnchor.GlobalPosition;
        KeyPress(Key.E);await WaitForPhase(benchMain,RestPhase.Seated);Require(!benchMain.CanPause,"Bench can pause");
        KeyPress(Key.Escape);await Frames(2);Require(!benchMain.Pause.IsOpen&&benchMain.Rest.IsActive,"Bench menu Esc also paused");
        KeyPress(Key.Escape);await WaitForPhase(benchMain,RestPhase.Standing);Require(!benchMain.Pause.IsOpen,"Stand Esc also paused");benchMain.Free();await Frames(2);
        var soup=await NewSoupMain(true);soup.SoupSeat.Begin(()=>{});Require(!soup.CanPause,"Can pause in sit transition");
        await WaitUntil(()=>!soup.SoupSeat.IsActing,"Pause soup seated");Require(!soup.CanPause,"Can pause while soup seated");
        KeyPress(Key.Escape);await WaitUntil(()=>!soup.SoupSeat.IsActive,"Pause soup stand");Require(!soup.Pause.IsOpen,"Soup Esc also paused");soup.Free();await Frames(2);
        var nav=await NewPolishMain();nav.World.Player.Position=new(900,280);nav.Pause.Open();KeyPress(Key.F5);await Frames(2);
        Require(s.ManualSaves.Load().Snapshot?.PlayerPosition.Y==280,"Paused F5 lost safe position");
        GetTree().CurrentScene=null;Require(nav.Pause.TryReturnToMenu(),"Normal menu navigation failed");await Frames(8);
        var boot=GetTree().CurrentScene as BootMenu;Require(boot!=null,"Navigation did not show actual Boot");nav.Free();
        GetTree().CurrentScene=null;boot!.GetNode<Button>("Menu/AutoResumeButton").EmitSignal(Button.SignalName.Pressed);await Frames(8);
        var continued=GetTree().CurrentScene as MainView;Require(continued!=null&&continued.World.Player.Position.X==900&&s.Flow==FlowState.Field,"Menu continue failed safe restore");
        GetTree().CurrentScene=null;boot.Free();continued!.Free();await Frames(2);
    }
}
