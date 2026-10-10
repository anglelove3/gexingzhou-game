using Godot;
using GeXingzhou.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class SmokeHarness
{
    private const string JournalText="Panel/Content/BodyScroll/Body/Text";
    private const string JournalEmpty="Panel/Content/BodyScroll/Body/Empty";
    private static WorldSnapshot JournalCompletedSnapshot(GameSettings options)=>SaveV2Codec.CreateNew(options) with{Stage=SliceStage.SliceComplete,SceneId="soup_shop",PlayerPosition=new(400,430),MemoryOrdinal=1,
        MemoryState=new(){PushedCoinIds=new(){"c1","c2","c3","c4"},PushedTotal=5,FoodChoice=FoodChoice.Take,Completed=true},
        CompletedActions=new(){"invitation.meeting_complete:invitation-1","candy.hey.delivered:hey-1","soup.meet:soup-seat-1","memory.return:soup-1","memory.food.resolve:soup-1","soup.payment:soup-payment-1","slice.complete:slice-1"},ReturnContext=new("soup_shop",new(400,430),"soup.return",true)};
    private static string SnapshotJson(WorldSnapshot snapshot)=>JsonSerializer.Serialize(snapshot,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}});
    private static string JournalFiles(string directory)
    {
        Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Journal fixture outside test-output");
        return string.Join("\n",System.IO.Directory.GetFiles(directory,"*",System.IO.SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p+":"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(p)))));
    }
    private async Task JournalClick(Control control)
    {
        var point=control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false},true);
        await Frames(3);
    }
    private async Task JournalChecks()
    {
        await JournalOpenClose();GD.Print("JOURNAL_PASS OpenClose");
        await JournalModalIsolation();GD.Print("JOURNAL_PASS ModalIsolation");
        await JournalBlockedSources();GD.Print("JOURNAL_PASS BlockedSources");
        await JournalFocusLifetime();GD.Print("JOURNAL_PASS FocusLifetime Scroll");
        await JournalReloadRefresh();GD.Print("JOURNAL_PASS ReloadRefresh");
        await JournalBindingFailure();GD.Print("JOURNAL_PASS BindingFailure");
    }
    private async Task JournalOpenClose()
    {
        var main=await NewPolishMain();
        Require(main.GetNodeOrNull<Control>("Journal")!=null,"Missing authored Journal node");
        var s=GetNode<GameSession>("/root/GameSession");KeyPress(Key.J);await Frames(3);
        Require(main.Journal.IsOpen&&s.Flow==FlowState.Journal,"J did not open journal");
        Input.ParseInputEvent(new InputEventKey{Keycode=Key.J,PhysicalKeycode=Key.J,Pressed=true,Echo=true});await Frames(2);
        Require(main.Journal.IsOpen,"Echo J closed journal");KeyPress(Key.Escape);await Frames(3);
        Require(!main.Journal.IsOpen&&s.Flow==FlowState.Field,"Esc did not restore Field");
        await JournalClick(main.GetNode<Button>("HUD/JournalButton"));Require(main.Journal.IsOpen,"HUD click did not open journal");
        await JournalClick(main.Journal.GetNode<Button>("Panel/Content/CloseButton"));
        Require(!main.Journal.IsOpen&&s.Flow==FlowState.Field,"Close click did not restore Field");main.Journal.Close();main.Journal.Close();
        main.Free();await Frames(2);
    }
    private async Task JournalModalIsolation()
    {
        var main=await NewDepthSoupMain(new(120,480));var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(s.Options with{RecordEventsEnabled=true},false);
        Require(s.SaveManual().Success,"Journal save fixture failed");
        main.Audio.PlayCue(AudioCue.PhoneRing);var music=main.Audio.GetNode<AudioStreamPlayer>("Music");var gain=music.VolumeLinear;var stream=music.Stream;
        KeyPress(Key.J);await DepthPhysics(3);Require(main.Journal.IsOpen,"Journal isolation open failed");
        var beforeJson=SnapshotJson(s.Snapshot);var beforeFiles=JournalFiles(s.SaveDirectory);var foot=main.World.Player.Position;
        Require(music.Playing&&ReferenceEquals(stream,music.Stream)&&Math.Abs(music.VolumeLinear-gain*.35)<.001,"Journal audio did not attenuate continuing music");
        Require(main.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().All(p=>!p.Playing),"Journal did not stop transient audio");
        s.AdvanceClock(60);foreach(var key in new[]{Key.E,Key.F5,Key.F9,Key.Enter})KeyPress(key);
        foreach(var action in new[]{"move_up","move_down","move_left","move_right"})
        {Input.ActionPress(action);await DepthPhysics(8);Input.ActionRelease(action);await DepthPhysics(2);Require(main.World.Player.Position==foot,"Journal allowed "+action);}
        Require(!main.Observations.TryObserve(main.World.GetTargets().OfType<ObservationHotspot>().First()),"Journal allowed observation callback");
        var target=main.World.GetTargets().OfType<ObservationHotspot>().First();var point=main.WorldToUi(target.Shape.GlobalPosition);
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false},true);await DepthPhysics(4);
        Require(beforeJson==SnapshotJson(s.Snapshot),"Journal mutated snapshot");Require(beforeFiles==JournalFiles(s.SaveDirectory),"Journal wrote save/event files");
        Require(main.World.Player.Position==foot&&!main.Dialogue.IsOpen&&main.Journal.IsOpen,"Journal allowed movement/observation/load");
        KeyPress(Key.J);await Frames(2);Require(music.Playing&&ReferenceEquals(stream,music.Stream)&&Math.Abs(music.VolumeLinear-gain)<.001,"Journal close restarted or attenuated music");
        Require(main.Observations.TryObserve(target),"Observation not restored after journal");await Finish(main);main.Free();await Frames(3);
    }
    private async Task JournalAssertBlocked(MainView main,string source)
    {
        var s=GetNode<GameSession>("/root/GameSession");var flow=s.Flow;var focus=GetViewport().GuiGetFocusOwner();var files=JournalFiles(s.SaveDirectory);var stage=s.Snapshot.Stage;
        KeyPress(Key.J);KeyPress(Key.J);await Frames(2);
        Require(!main.Journal.IsOpen&&s.Flow==flow&&s.Snapshot.Stage==stage,"Blocked J changed "+source);
        Require(GetViewport().GuiGetFocusOwner()==focus&&files==JournalFiles(s.SaveDirectory),"Blocked J changed focus/save "+source);
    }
    private async Task JournalBlockedSources()
    {
        var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
        main.Phone.Open("messages");await JournalAssertBlocked(main,"phone");main.Phone.Close();
        main.Dialogue.ShowText("葛行舟","只是站一会儿。");await JournalAssertBlocked(main,"dialogue");main.Dialogue.HandleKey(Key.Escape);
        main.Choices.Open("先做什么？",new(string,Action)[]{("继续走",()=>{})});await JournalAssertBlocked(main,"choices");main.Choices.Close();
        s.Flow=FlowState.Transition;await JournalAssertBlocked(main,"transition");s.Flow=FlowState.Field;
        Require(main.Pause.Open(),"Pause fixture failed");await JournalAssertBlocked(main,"pause");main.Settings.Open();await JournalAssertBlocked(main,"settings");main.Settings.Close();main.Pause.Close();
        var bench=main.World.GetNode<BenchView>("Bench");main.World.Player.GlobalPosition=bench.StandAnchor.GlobalPosition;main.Rest.Begin(bench);await WaitForPhase(main,RestPhase.Seated);await JournalAssertBlocked(main,"bench");main.Rest.Cancel();main.Free();await Frames(3);
        main=await NewDepthSoupMain(new(380,430));Require(main.SoupSeat.Begin(()=>{}),"Soup seat fixture failed");
        await JournalAssertBlocked(main,"approaching soup seat");await WaitUntil(()=>main.SoupSeat.IsActive&&!main.SoupSeat.IsActing,"soup seated");await JournalAssertBlocked(main,"soup seated");
        Require(main.SoupSeat.PlayAction("eat",()=>{}),"Soup action fixture failed");await JournalAssertBlocked(main,"soup action");main.SoupSeat.Cancel();main.Free();await Frames(3);
        var seed=await NewCoinTable(1);var snapshot=s.Snapshot;seed.Free();await Frames(2);s.PendingRestore=snapshot;
        main=s.GetScene("res://scenes/Main.tscn")!.Instantiate<MainView>();AddChild(main);await Frames(4);Require(main.Memory!=null,"Memory block fixture failed");await JournalAssertBlocked(main,"memory");main.Free();await Frames(3);
        Require(s.Restore(JournalCompletedSnapshot(s.Options)).Success,"End fixture failed");main=s.GetScene("res://scenes/Main.tscn")!.Instantiate<MainView>();AddChild(main);await Frames(3);main.SliceEnd.ShowCompleted();await JournalAssertBlocked(main,"endcard");main.SliceEnd.Close();main.Free();await Frames(3);
    }
    private async Task JournalFocusLifetime()
    {
        var main=await NewPolishMain();var journal=main.Journal;var hud=main.GetNode<Button>("HUD/JournalButton");hud.GrabFocus();KeyPress(Key.J);await Frames(3);
        var buttons=new[]{journal.GetNode<Button>("Panel/Content/Tabs/Current"),journal.GetNode<Button>("Panel/Content/Tabs/History"),journal.GetNode<Button>("Panel/Content/Tabs/Discoveries"),journal.GetNode<Button>("Panel/Content/CloseButton")};
        foreach(var button in buttons.Skip(1).Append(buttons[0])){KeyPress(Key.Tab);await Frames(2);Require(button.HasFocus()&&!main.Phone.IsOpen,"Journal Tab escaped focus loop");}
        foreach(var page in new[]{JournalPage.History,JournalPage.Discoveries}){journal.ShowPage(page);Require(journal.GetNode<Label>(JournalEmpty).Visible&&!journal.GetNode<Label>(JournalText).Visible,"Empty page leaked placeholders");}
        journal.ShowPage(JournalPage.Current);var text=journal.GetNode<Label>(JournalText);text.Text=string.Join('\n',Enumerable.Repeat("我走过街道，也记下今天的生活。",60));await Frames(4);var scroll=journal.GetNode<ScrollContainer>("Panel/Content/BodyScroll");
        KeyPress(Key.Pagedown);await Frames(2);Require(scroll.ScrollVertical>0,"Journal PgDn cannot read long text");KeyPress(Key.Home);await Frames(2);Require(scroll.ScrollVertical==0,"Journal Home failed");
        var point=scroll.GetGlobalRect().GetCenter();GetViewport().PushInput(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.WheelDown,Pressed=true},true);await Frames(2);Require(scroll.ScrollVertical>0,"Journal wheel failed");
        KeyPress(Key.End);await Frames(2);var bottom=scroll.ScrollVertical;KeyPress(Key.Pageup);await Frames(2);Require(scroll.ScrollVertical<bottom,"Journal PgUp failed");
        KeyPress(Key.Escape);await Frames(2);Require(hud.HasFocus(),"Journal did not restore HUD focus");
        var disposableFocus=new Button{Text="测试焦点"};main.AddChild(disposableFocus);disposableFocus.GrabFocus();KeyPress(Key.J);await Frames(2);disposableFocus.Free();KeyPress(Key.Escape);await Frames(2);Require(!journal.IsOpen,"Freed prior focus prevented close");main.Free();await Frames(2);
        main=await NewPolishMain();var previous=new Button{Text="测试焦点"};main.AddChild(previous);previous.GrabFocus();KeyPress(Key.J);await Frames(2);previous.Visible=false;KeyPress(Key.Escape);await Frames(2);Require(!main.Journal.IsOpen&&!previous.HasFocus(),"Hidden focus restored");main.Free();await Frames(3);
    }
    private async Task JournalReloadRefresh()
    {
        var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");Require(s.SaveManual().Success,"Early journal fixture failed");
        s.AdvanceClock(65);Require(s.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1")).Applied,"Meeting fixture failed");Require(s.TryDispatch(new("candy.hey.delivered","delivered","hey-1")).Applied,"Hey fixture failed");
        Require(main.ChangeWorld("soup_shop",new(120,480)),"Journal progress world failed");await Frames(3);
        foreach(var target in main.World.GetTargets().OfType<ObservationHotspot>()){Require(main.Observations.TryObserve(target),"Journal discovery fixture failed");await Finish(main);}
        Require(main.Journal.Open(),"Progress journal open failed");main.Journal.ShowPage(JournalPage.History);Require(main.Journal.GetNode<Label>(JournalText).Text.Contains("喜糖"),"History not projected");
        main.Journal.ShowPage(JournalPage.Discoveries);Require(main.Journal.GetNode<Label>(JournalText).Text.Contains("柜台便条"),"Discoveries not projected");main.Journal.Close();
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(8);var loaded=GetTree().CurrentScene as MainView;Require(loaded!=null,"F9 journal Main reload failed");main.Free();
        Require(loaded!.Journal.Open(),"Reloaded journal open failed");loaded.Journal.ShowPage(JournalPage.History);Require(loaded.Journal.GetNode<Label>(JournalEmpty).Visible,"Early F9 retained history");loaded.Journal.ShowPage(JournalPage.Discoveries);Require(loaded.Journal.GetNode<Label>(JournalEmpty).Visible,"Early F9 retained discoveries");loaded.Journal.Close();GetTree().CurrentScene=null;loaded.Free();await Frames(3);
        main=await NewPolishMain();Require(main.Journal.Open(),"New game journal failed");main.Journal.ShowPage(JournalPage.Discoveries);Require(main.Journal.GetNode<Label>(JournalEmpty).Visible,"New game retained discoveries");main.Journal.Close();main.Free();await Frames(3);
        var legacy=UpgradeFixture();WriteLegacy(legacy,false);var source=System.IO.Path.Combine(legacy.LegacyDirectory,"save.json");var bytes=System.IO.File.ReadAllBytes(source);var upgraded=legacy.TryUpgradeLegacy(false);
        Require(upgraded.Status==LoadStatus.Loaded&&System.IO.File.ReadAllBytes(source).SequenceEqual(bytes),"Journal legacy upgrade failed or modified source");Require(s.Restore(upgraded.Snapshot!).Success,"Journal legacy restore failed");legacy.Free();
        main=s.GetScene("res://scenes/Main.tscn")!.Instantiate<MainView>();AddChild(main);await Frames(3);Require(main.Journal.Open(),"Upgraded journal failed");main.Journal.ShowPage(JournalPage.Discoveries);Require(main.Journal.GetNode<Label>(JournalEmpty).Visible,"Legacy upgrade invented discoveries");main.Journal.Close();main.Free();await Frames(3);
    }
    private async Task JournalBindingFailure()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();var before=SnapshotJson(s.Snapshot);
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();main.GetNode("Journal/"+JournalText).Free();AddChild(main);await Frames(3);
        Require(main.HasMeta("binding_error")&&main.Journal.HasMeta("binding_error")&&!main.Journal.Open(),"Missing journal binding silently repaired");
        Require(before==SnapshotJson(s.Snapshot),"Broken journal binding mutated snapshot");main.Free();await Frames(3);
    }
}
