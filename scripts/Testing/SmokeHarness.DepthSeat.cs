using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task CaptureDepthSeatChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        foreach(var reduced in new[]{false,true})
        {
            var main=await NewSoupMain(reduced);var seat=main.SoupSeat;
            var art=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");var suffix=reduced?"-reduced":"";
            Require(seat.Begin(()=>{}),"Depth keyframe sit rejected");
            foreach(var (frame,label) in new[]{(0,"start"),(1,"mid"),(3,"end")})
            {
                session.Flow=FlowState.Phone;seat.Suspend();art.Frame=frame;art.FrameProgress=0;
                await Capture("depth-sit-"+label+suffix);
            }
            session.Flow=FlowState.Field;seat.Resume();await WaitUntil(()=>!seat.IsActing,"Depth keyframe seated");
            foreach(var (code,label,frame) in new[]{("eat","eat",1),("set_chopsticks","chopsticks",3),("check_phone","phone",1)})
            {
                Require(seat.PlayAction(code,()=>{}),"Depth contact capture rejected");
                session.Flow=FlowState.Phone;seat.Suspend();art.Frame=frame;art.FrameProgress=0;
                await Capture("depth-"+label+suffix);
                session.Flow=FlowState.Field;seat.Resume();
                try{await WaitUntil(()=>!seat.IsActing,"Depth contact capture complete");}
                catch{GD.Print($"CONTACT_STALL code={code} pose={art.Animation} frame={art.Frame} progress={art.FrameProgress} playing={art.IsPlaying()} flow={session.Flow} focused={DiagnosticField(seat,"focused")} suspended={DiagnosticField(seat,"suspended")} osfocus={GetWindow().HasFocus()}");throw;}
            }
            seat.Stand();
            foreach(var (frame,label) in new[]{(0,"start"),(1,"mid"),(3,"end")})
            {
                session.Flow=FlowState.Phone;seat.Suspend();art.Frame=frame;art.FrameProgress=0;
                await Capture("depth-stand-"+label+suffix);
            }
            session.Flow=FlowState.Field;seat.Resume();await WaitUntil(()=>!seat.IsActive,"Depth keyframe risen");
            main.Free();await Frames(2);
        }
    }
    private async Task DepthSeatChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        var far=await NewDepthSoupMain(new(400,480));var facts=s.Snapshot.CompletedActions.Count;
        Require(!far.SoupSeat.Begin(()=>{}),"Too far seat entry accepted");Require(s.Snapshot.CompletedActions.Count==facts,"Rejected approach advanced story");far.Free();await Frames(2);
        var blocked=await NewDepthSoupMain(new(430,410));
        Require(!blocked.SoupSeat.Begin(()=>{}),"Blocked short approach crossed chair");blocked.Free();await Frames(2);
        foreach(var reduced in new[]{false,true})
        {
            var main=await NewDepthSoupMain(new(380,430));s.SetOptions(s.Options with{ReducedMotion=reduced,TextSpeed=0},false);
            var seat=main.SoupSeat;var player=main.World.Player;int done=0;var start=player.Position;
            Require(seat.Begin(()=>done++),"Legal short approach rejected");Require(seat.IsApproaching&&player.Position==start&&done==0,"Approach teleported or completed synchronously");
            await DepthPhysics(3);var reached=player.Position;
            Require(reached.X>380&&reached.X<400&&seat.SavePosition==new Position2(reached.X,reached.Y),"Approach save is not current legal foot");
            main.Phone.Open("messages");await DepthPhysics(15);Require(player.Position==reached&&done==0,"Phone failed to freeze approach");main.Phone.Close();
            seat.Cancel();Require(player.Position==reached&&!seat.IsActive,"Approach cancel teleported to stand");
            Require(seat.Begin(()=>done++),"Resumed approach rejected");await WaitUntil(()=>!seat.IsActing,"Depth seated");
            Require(done==1&&player.Position==new Vector2(400,430),"Seated callback/stand wrong");
            var art=player.GetNode<AnimatedSprite2D>("Artwork");var atlas=(AtlasTexture)art.SpriteFrames.GetFrameTexture("seated",0);
            var hip=art.ToGlobal(atlas.GetMeta("seat_pivot").AsVector2()-atlas.GetSize()/2);
            var surface=main.World.GetNode<Marker2D>("DepthLayers/Props/Chairs/LeftChair/SeatSurface").GlobalPosition;
            Require(hip.DistanceTo(surface)<1,"Seated hip misses chair surface");
            var height=(float)atlas.GetMeta("standing_height").AsDouble()*art.Scale.Y;
            Require(Math.Abs(height-96)<1,"Soup body shrank relative to depth walking");
            main.World.ShowPayment();Require(main.World.GetNode<Sprite2D>("PaymentCoin").Position.Y>370,"Payment feedback left the actual tabletop");
            Require(main.World.GetNode<CanvasItem>("DepthLayers/Props/Chairs").ZIndex<main.World.GetNode<CanvasItem>("DepthLayers/Actors").ZIndex,"Chair back can cover seated face");
            Require(seat.PlayAction("set_chopsticks",()=>{}),"Chopstick contact seed");
            Require(!main.World.GetNode<Line2D>("ActionProps/Chopsticks").Visible,"Chopsticks already deposited before release");
            await WaitUntil(()=>!seat.IsActing,"Chopstick contact complete");
            main.Choices.Open("桌边",new (string,Action)[]{("返回",()=>{})});KeyPress(Key.Escape);await Frames(2);
            Require(seat.IsActive&&!main.Choices.IsOpen&&!seat.IsActing,"Esc closed choices and stood in same key");
            KeyPress(Key.Escape);await Frames(2);Require(seat.IsActing,"Second Esc did not start standing");
            seat.Stand(()=>done+=100);await WaitUntil(()=>!seat.IsActive,"Depth standing");Require(done==1,"Repeated stand replaced callback");
            Input.ActionPress("move_down");await DepthPhysics(12);Input.ActionRelease("move_down");await DepthPhysics(12);Require(player.Position.Y>440,"Risen player remained locked");
            main.Free();await Frames(2);
        }
        await SoupChoiceEntryChecks();
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(4);
        await ExperienceSoupSeatChecks();
        GD.Print("DEPTH_SEAT_PASS Reachability ShortApproach Phone Cancel Hip Scale Esc Generation SixRealChoices F5F9");
    }
}
