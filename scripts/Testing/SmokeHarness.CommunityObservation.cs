using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task CommunityObservationChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
        var main=await NewPolishMain();SeedNarrativeMarkers(s);
        var sign=main.World.GetTarget("old_sign")!;sign.TryInteract(s);await Frames(3);
        Require(DialogueText(main).Contains("OBS_QUIET"),"FirstSignKeepsThought lost original thought");
        await Finish(main);Require(s.Snapshot.DiscoveredIds.Contains("community.sign"),"FirstSignKeepsThought did not record sign");
        Require(s.Snapshot.CompletedActions.Contains("observation.community.first"),"First sign lost first fact");main.Free();await Frames(2);
        main=await NewPolishMain();SeedNarrativeMarkers(s);var bench=(BenchView)main.World.GetTarget("bench")!;
        await OpenObservation(main,bench);await Finish(main);main.Rest.Cancel();
        sign=main.World.GetTarget("old_sign")!;sign.TryInteract(s);await Frames(3);
        Require(!DialogueText(main).Contains("OBS_"),"BenchFirstThenSign repeated first thought");await Finish(main);
        Require(s.Snapshot.DiscoveredIds.Contains("community.sign"),"BenchFirstThenSign lost discovery");main.Free();await Frames(2);
        await CommunityFirstObservationBranches();
        await CommunityObservationInputBoundaries();
        await CommunityObservationArrivalAndJournal();
        GD.Print("COMMUNITY_OBSERVATION_PASS FirstBranches AtomicSave Mouse E Cancel Repeat Ui Alpha Rest Arrival Journal F9");
    }
    private async Task CommunityFirstObservationBranches()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var scenario in new[]{"quiet","answered","unanswered"})foreach(var benchFirst in new[]{false,true})
        {
            var main=await NewPolishMain();
            if(scenario!="quiet")s.AdvanceClock(35);if(scenario=="answered")Require(s.TryDispatch(new("invitation.answer","answered","invitation-1")).Applied,"Answered fixture was not ringing");SeedNarrativeMarkers(s);
            var expected=scenario=="quiet"?"OBS_QUIET":scenario=="answered"?"OBS_ANSWERED":"OBS_UNANSWERED";
            var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;
            foreach(var id in new[]{"community.notice","community.planter"}){
                main.ShowObservation(main.World.GetTarget(id)!);await Finish(main);
                Require(!s.Snapshot.CompletedActions.Contains("observation.community.first"),"Other prop consumed first thought");
            }
            if(benchFirst){await OpenObservation(main,main.World.GetTarget("bench")!);Require(DialogueText(main).Contains(expected),"Bench first wrong branch");await Finish(main);Require(s.Saves.Load().Snapshot!.PlayerPosition==new Position2(960,440),"Bench saved hip");main.Rest.Cancel();}
            var sign=(ObservationHotspot)main.World.GetTarget("old_sign")!;main.World.Player.Position=new(160,400);await Frames(3);
            Require(s.SaveCheckpoint().Success,"Atomic seed save");main.ShowObservation(sign);await Frames(3);
            Require(DialogueText(main).Contains(expected)==!benchFirst,"Sign did not retain first thought branch "+scenario+" benchFirst="+benchFirst+" actual="+DialogueText(main));
            KeyPress(Key.Escape);await Frames(2);Require(!s.Snapshot.DiscoveredIds.Contains("community.sign"),"Cancelled sign recorded");
            main.ShowObservation(sign);await Finish(main);
            var loaded=s.Saves.Load().Snapshot!;var backup=s.Saves.LoadBackup().Snapshot!;
            Require(loaded.DiscoveredIds.Contains("community.sign")&&loaded.CompletedActions.Contains("observation.community.first"),"Sign checkpoint not atomic");
            Require(!backup.DiscoveredIds.Contains("community.sign"),"Sign wrote more than one checkpoint");
            Require(s.Snapshot.Stage==stage&&s.Snapshot.CandyCount==candy,"First thought changed quest");
            Require(loaded.PlayerPosition==new Position2(160,400),"Observation saved stale position");
            if(!benchFirst){await OpenObservation(main,main.World.GetTarget("bench")!);Require(!DialogueText(main).Contains("OBS_"),"Sign then bench repeated first");await Finish(main);main.Rest.Cancel();}
            main.Free();await Frames(2);
        }
        var old=await NewPolishMain();s.MarkFirstCommunityObservation();SeedNarrativeMarkers(s);old.ShowObservation(old.World.GetTarget("old_sign")!);await Frames(3);
        Require(!DialogueText(old).Contains("OBS_"),"Retained old first fact repeated");await Finish(old);Require(s.Snapshot.DiscoveredIds.Contains("community.sign"),"Retained old first lost sign");old.Free();await Frames(2);
    }
    private async Task CommunityObservationInputBoundaries()
    {
        var window=GetWindow();var originalSize=window.Size;var originalMode=window.ContentScaleMode;window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1440,900),new Vector2I(1024,768)})
        {
            window.Size=size;s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);await Frames(3);var main=await NewPolishMain();
            var targets=main.World.GetTargets().OfType<ObservationHotspot>().OrderBy(x=>x.Id).ToArray();Require(targets.Length==3,"Missing community props");
            var foreign=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();AddChild(foreign);await Frames(2);
            Require(!main.Observations.CanObserve((ObservationHotspot)foreign.GetTarget("old_sign")!),"Foreign observation accepted");foreign.Free();
            foreach(var target in targets)
            {
                main.World.Player.Position=target.Position;await Frames(4);Require(main.Observations.CanObserve(target),"Valid observation unavailable");
                var center=target.Shape.GlobalPosition;var point=main.GetGlobalTransformWithCanvas()*main.WorldToUi(center);
                Require(target.ContainsWorldPoint(center),"Shape center outside real hit polygon");
                ClickExploration(point);await Frames(3);Require(main.Dialogue.IsOpen,"Community camera mouse missed "+target.Id+" "+size);
                KeyPress(Key.Escape);await Frames(2);Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Mouse cancel wrote fact");
                var visual=target.GetNode<Node2D>(target.VisualTarget);var oldPosition=visual.Position;
                var pause=main.GetNode<Button>("HUD/PauseButton");var ui=pause.GetGlobalRect().GetCenter();visual.GlobalPosition=main.UiToWorld(ui);
                ClickExploration(ui);await Frames(3);Require(!main.Dialogue.IsOpen,"UI passed through to prop");if(main.Pause.IsOpen)main.Pause.Close();visual.Position=oldPosition;await Frames(3);
                using var image=Image.CreateEmpty(20,20,false,Image.Format.Rgba8);image.Fill(new Color(1,1,1,1));
                var foreground=new Sprite2D{Texture=ImageTexture.CreateFromImage(image),Position=main.World.ToLocal(center)};main.World.GetNode<Node2D>("Foreground").AddChild(foreground);await Frames(2);
                ClickExploration(point);await Frames(2);Require(!main.Dialogue.IsOpen,"Opaque foreground passed click");
                image.Fill(new Color(1,1,1,0));foreground.Texture=ImageTexture.CreateFromImage(image);await Frames(2);ClickExploration(point);await Frames(2);
                Require(main.Dialogue.IsOpen,"Transparent foreground blocked prop");KeyPress(Key.Escape);foreground.Free();await Frames(3);
                KeyPress(Key.E);ClickExploration(point);await Frames(3);Require(main.Dialogue.IsOpen,"Community E missed "+target.Id);
                Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Same-frame E/mouse finished unread observation");await Finish(main);
                var path=System.IO.Path.Combine(s.SaveDirectory,"save.json");var bytes=System.IO.File.ReadAllBytes(path);
                Require(s.Saves.Load().Snapshot!.DiscoveredIds.Contains(target.DiscoveryId),"Discovery not saved");main.ShowObservation(target);await Finish(main);
                Require(bytes.SequenceEqual(System.IO.File.ReadAllBytes(path)),"Repeat observation rewrote checkpoint");
            }
            var sign=(ObservationHotspot)main.World.GetTarget("old_sign")!;var bench=(BenchView)main.World.GetTarget("bench")!;
            main.World.Player.Position=new(940,440);Require(main.Rest.Begin(bench)&&main.Rest.IsApproaching,"Approach isolation seed");
            Require(!main.Observations.CanObserve(sign),"Approach allowed observation");main.ShowObservation(sign);Require(!main.Dialogue.IsOpen,"Approach opened thought");
            await WaitForPhase(main,RestPhase.Seated);Require(!main.Observations.TryObserve(sign),"Seated allowed prop");main.Rest.Cancel();
            Require(s.Snapshot.CompletedActions.SetEquals(new[]{"observation.community.first"})&&s.Snapshot.CandyCount==0,"Optional props changed main quest");
            var eventPath=System.IO.Path.Combine(s.SaveDirectory,"behavior","events.jsonl");Require(!System.IO.File.Exists(eventPath)||System.IO.File.ReadAllLines(eventPath).Length==0,"Optional props recorded story events");main.Free();await Frames(3);
        }
        window.Size=originalSize;window.ContentScaleMode=originalMode;
    }
    private async Task CommunityObservationArrivalAndJournal()
    {
        var s=GetNode<GameSession>("/root/GameSession");var main=await NewPolishMain();
        s.AdvanceClock(52);Require(!s.Snapshot.InvitationState.CarArrived,"Arrival seed already arrived");main.ShowObservation(main.World.GetTarget("old_sign")!);await Frames(2);
        s.AdvanceClock(30);Require(!s.Snapshot.InvitationState.CarArrived,"Observation advanced invitation clock");KeyPress(Key.Escape);await Frames(2);
        Require(s.Snapshot.DiscoveredIds.Count==0&&!s.Snapshot.CompletedActions.Contains("observation.community.first"),"Arrival cancellation wrote first");
        s.AdvanceClock(2);await Frames(2);Require(s.Snapshot.InvitationState.CarArrived&&main.World.GetTarget("cannon")!.IsActive,"Observation swallowed arrival");main.Free();await Frames(2);
        main=await NewPolishMain();Require(s.SaveManual().Success,"Early journal manual seed");
        foreach(var id in new[]{"community.sign","community.notice","community.planter","soup.sign","soup.menu","soup.note"})s.MarkDiscovery(id);
        foreach(var repeat in new[]{false,true}){
            Require(main.Journal.Open(),"Six discoveries journal unavailable");main.Journal.ShowPage(JournalPage.Discoveries);await Frames(2);
            var text=main.Journal.GetNode<Label>(JournalText).Text;foreach(var title in new[]{"旧路牌","小区公告","旧花箱","旧招牌","看看菜单","柜台便条"})Require(text.Contains(title),"Six discoveries journal lost "+title);main.Journal.Close();
        }
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(6);var restored=GetTree().CurrentScene as MainView;
        Require(restored!=null&&restored!=main&&s.Snapshot.DiscoveredIds.Count==0,"F9 early snapshot leaked discoveries");main.Free();restored!.Free();await Frames(3);
        main=await NewPolishMain();Require(main.Journal.Open(),"New game journal unavailable");main.Journal.ShowPage(JournalPage.Discoveries);await Frames(2);
        Require(main.Journal.GetNode<Label>(JournalEmpty).Visible,"New game retained discoveries");main.Journal.Close();main.Free();await Frames(2);
    }
}
