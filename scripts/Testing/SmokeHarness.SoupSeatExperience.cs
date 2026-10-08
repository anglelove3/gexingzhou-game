using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task SoupSeatVisualChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var reduced in new[]{false,true})
        {
            var main=await NewSoupMain(reduced);var seat=main.SoupSeat;var art=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");
            Require(seat.Begin(()=>{}),"Visual seat rejected");await WaitUntil(()=>!seat.IsActing,"Visual seated");await Capture("soup-seated"+(reduced?"-reduced":""));
            foreach(var code in new[]{"eat","set_chopsticks","check_phone"})
            {
                Require(seat.PlayAction(code,()=>{}),"Visual action rejected");await WaitUntil(()=>art.Frame>=1,"Visual action middle frame");
                s.Flow=FlowState.Phone;seat.Suspend();Require(art.Animation==code,"Visual action already completed");
                await Capture((code=="set_chopsticks"?"soup-chopsticks":code=="check_phone"?"soup-phone":"soup-eat")+(reduced?"-reduced":""));
                s.Flow=FlowState.Field;seat.Resume();await WaitUntil(()=>!seat.IsActing,"Visual action complete");
            }
            seat.Stand();await WaitUntil(()=>!seat.IsActive,"Visual stand");await Capture("soup-risen"+(reduced?"-reduced":""));main.Free();await Frames(2);
        }
        var delivery=await NewPolishMain();s.AdvanceClock(53);s.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1"));delivery.ChangeWorld("convenience_street",new(600,280));await Frames(3);
        delivery.World.GetTarget("hey")!.TryInteract(s);await Finish(delivery);delivery.Choices.Close();await Frames(2);await Capture("candy-handover");delivery.Free();await Frames(2);
    }
    private async Task<MainView> NewSoupMain(bool reduced)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=reduced},false);
        var main=await NewPolishMain();s.AdvanceClock(53);
        Require(s.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1")).Applied,"Soup seed meeting");
        Require(s.TryDispatch(new("candy.hey.delivered","delivered","hey-1")).Applied,"Soup seed candy");
        Require(main.ChangeWorld("soup_shop",new(350,280)),"Soup seed world");await Frames(3);return main;
    }
    private async Task ExperienceSoupSeatChecks()
    {
        await SoupChoiceEntryChecks();
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var reduced in new[]{false,true})
        {
            var main=await NewSoupMain(reduced);var seat=main.SoupSeat;var callbacks=0;
            Require(!main.GetNode<Label>("HUD/Guidance").Visible,"Old invitation tutorial resurfaced after delivery");
            Require(seat.Begin(()=>callbacks++),"Seat rejected legitimate begin");Require(!seat.Begin(()=>callbacks+=100),"Repeated begin accepted");
            await WaitUntil(()=>!seat.IsActing,"Soup sitting");Require(seat.IsActive&&callbacks==1,"Sit callback count");
            var art=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");Require(art.Animation=="seated","Not seated pose");
            Require(main.World.GetNode<Sprite2D>("TableForeground").Visible,"Missing leg occlusion");Require(main.SafeSavePosition.Y==280,"Unsafe soup save");
            await Capture("soup-seated"+(reduced?"-reduced":""));
            foreach(var code in new[]{"eat","set_chopsticks","check_phone"})
            {
                var before=callbacks;Require(seat.PlayAction(code,()=>callbacks++),"Action rejected "+code);
                Require(!seat.PlayAction(code,()=>callbacks+=100),"Repeated action accepted");
                Require(art.Animation==code&&art.SpriteFrames.GetFrameCount(code)>=3,"Missing distinct action frames "+code);
                await Frames(2);main.Phone.Open("messages");await Frames(2);var frame=art.Frame;var progress=art.FrameProgress;await Frames(20);
                Require(art.Frame==frame&&Math.Abs(art.FrameProgress-progress)<.001,"Phone leaked animation progress");main.Phone.Close();
                for(int i=0;i<120&&art.Frame<1;i++)await Frames();
                await Capture((code=="set_chopsticks"?"soup-chopsticks":code=="check_phone"?"soup-phone":"soup-eat")+(reduced?"-reduced":""));
                await WaitUntil(()=>!seat.IsActing,"Soup action "+code);Require(callbacks==before+1,"Action completion duplicated");
                Require(art.Animation=="seated"&&s.Snapshot.CandyCount==0,"Action changed candy or resting pose");
            }
            s.UpdatePosition(main.SafeSavePosition);s.SaveManual();var saved=s.ManualSaves.Load();
            Require(saved.Status==LoadStatus.Loaded&&saved.Snapshot!.PlayerPosition.Y==280,"Safe save rejected");
            callbacks=0;Require(seat.PlayAction("eat",()=>callbacks++),"Cancel test action");seat.Cancel();
            Require(!seat.IsActive&&art.Animation=="idle","Cancel did not unlock");
            Require(seat.Begin(()=>{}),"New seat generation rejected");await WaitUntil(()=>!seat.IsActing,"New seat generation");await Frames(120);
            Require(callbacks==0,"Stale action callback committed");seat.Stand();await WaitUntil(()=>!seat.IsActive,"Soup standing");
            Require(art.Animation=="idle"&&!main.World.GetNode<Sprite2D>("TableForeground").Visible,"Stand did not clear pose");
            await Capture("soup-risen"+(reduced?"-reduced":""));
            Require(seat.Begin(()=>callbacks++),"Transition cancel seed");Require(main.ChangeWorld("community_gate",new(960,280)),"Change world rejected");await Frames(100);Require(callbacks==0&&!seat.IsActive,"World retained old callback");
            var bench=main.World.GetNode<BenchView>("Bench");Require(main.Rest.Begin(bench),"Bench seed failed");Require(!seat.Begin(()=>callbacks++),"Soup and bench simultaneously active");main.Rest.Cancel();
            main.Free();await Frames(2);
            s.PendingRestore=saved.Snapshot;var restored=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(restored);await Frames(4);
            Require(!restored.SoupSeat.IsActive&&restored.World.Player.GetNode<AnimatedSprite2D>("Artwork").Animation=="idle","Saved soup restored transient pose");restored.Free();await Frames(2);
        }
        var transfer=await NewSoupMain(false);var stale=0;Require(transfer.SoupSeat.Begin(()=>stale++),"Transition begin");
        Require((await transfer.SceneFlow.TryEnter("convenience_street",new(600,280))).Success,"Transition failed");await Frames(100);Require(stale==0&&!transfer.SoupSeat.IsActive,"SceneFlow retained old seat");transfer.Free();await Frames(2);
        var read=await NewSoupMain(false);Require(read.SoupSeat.Begin(()=>{}),"F9 begin");await WaitUntil(()=>!read.SoupSeat.IsActing,"F9 seated");
        Require(read.SoupSeat.PlayAction("eat",()=>stale++),"F9 action");KeyPress(Key.F5);await Frames(2);
        var savedAction=s.ManualSaves.Load();Require(savedAction.Status==LoadStatus.Loaded&&savedAction.Snapshot!.PlayerPosition==read.SafeSavePosition,"F5 mid-action unsafe");
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(6);var reopened=GetTree().CurrentScene as MainView;
        Require(reopened!=null&&reopened!=read&&!reopened.SoupSeat.IsActive&&reopened.World.Player.GetNode<AnimatedSprite2D>("Artwork").Animation=="idle","F9 retained transient soup pose");
        read.Free();await Frames(100);Require(stale==0,"F9 delayed callback committed");reopened!.Free();await Frames(2);
        var delivery=await NewPolishMain();s.AdvanceClock(53);s.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1"));delivery.ChangeWorld("convenience_street",new(600,280));await Frames(3);
        delivery.World.GetTarget("hey")!.TryInteract(s);await Finish(delivery);delivery.Choices.Close();await Frames(2);await Capture("candy-handover");
        Require(s.Snapshot.CandyCount==0,"Handover did not commit once");delivery.World.GetTarget("hey")!.TryInteract(s);Require(s.Snapshot.CandyCount==0,"Repeated handover altered candy");delivery.Choices.Close();delivery.Free();await Frames(2);
    }
    private async Task SoupChoiceEntryChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var reduced in new[]{false,true})foreach(var (code,index) in new[]{("eat",0),("set_chopsticks",1),("check_phone",2)})
        {
            var main=await NewSoupMain(reduced);
            main.HandleInteraction(main.World.GetTarget("seat")!);
            await WaitUntil(()=>main.Dialogue.IsOpen,"Real soup opening");await Finish(main);
            Require(main.Choices.IsOpen,"Soup choices absent");await Frames(3);
            var option=main.Choices.GetNode<Button>("Panel/Content/Options/Option"+(index+1));option.GrabFocus();
            KeyPress(Key.E);await Frames(2);
            Require(main.SoupSeat.IsActing&&main.World.Player.GetNode<AnimatedSprite2D>("Artwork").Animation==code,"Normal choice skipped action "+code);
            Require(s.Snapshot.ChoiceCodes.GetValueOrDefault("soup-response-1")==code,"Normal choice not recorded");
            await WaitUntil(()=>main.Dialogue.IsOpen,"Action branch dialogue "+code);
            var expected=code switch{"eat"=>"行，先吃。","set_chopsticks"=>"哟，葛大爷还会伺候人了？",_=>"又卡了？"};
            Require(main.Dialogue.GetNode<Label>("Panel/Content/Body/Text").Text.StartsWith(expected),"Wrong action branch dialogue "+code);
            await Finish(main);Require(main.Choices.IsOpen,"Action did not offer memory");main.Choices.Close();
            main.Free();await Frames(2);
        }
        GD.Print("SOUP_CHOICE_ENTRY_PASS 6");
    }
}
