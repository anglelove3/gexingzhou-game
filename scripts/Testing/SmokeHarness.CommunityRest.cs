using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task CommunityRestChecks()
    {
        var main=await NewPolishMain();
        try{
            var player=main.World.Player;var bench=(BenchView)main.World.GetTarget("bench")!;
            player.Position=new(940,440);var before=player.Position;
            Require(main.Rest.Begin(bench),"Community nearby bench rejected");Require(player.Position==before,"Community approach warped to stand");
            Require(main.World.GetNodeOrNull<BenchView>("DepthLayers/Props/Bench")!=null,"Community nested Bench missing");
            Require(typeof(RestController).GetProperty("IsApproaching")?.GetValue(main.Rest) is true,"Community approach not active");
            await WaitForPhase(main,RestPhase.Seated);
            Require(Math.Abs(player.GetNode<AnimatedSprite2D>("Artwork").Scale.Y-96/307f)<.001,"Community rest shrinks character");
        }finally{main.Free();await Frames(2);}
        foreach(var reduced in new[]{false,true})await CommunityRestActions(reduced);
        await CommunityRestDetour();
        GD.Print("COMMUNITY_REST_PASS Nested Approach Scale Lifecycle Detour");
    }
    private static void AssertCommunityContact(MainView main)
    {
        if(main.Rest.IsApproaching||!main.Rest.IsActive)return;
        var bench=(BenchView)main.World.GetTarget("bench")!;var art=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");
        var frame=(AtlasTexture)art.SpriteFrames.GetFrameTexture(art.Animation,art.Frame);var count=art.SpriteFrames.GetFrameCount(art.Animation);
        float t=art.Animation=="sit_down"?art.Frame/(float)(count-1):art.Animation=="stand_up"?1-art.Frame/(float)(count-1):1;
        var pivot=frame.GetMeta("stand_pivot").AsVector2().Lerp(frame.GetMeta("seat_pivot").AsVector2(),t);
        var actual=art.ToGlobal(pivot-frame.GetSize()/2);var expected=bench.StandAnchor.GlobalPosition.Lerp(bench.SeatAnchor.GlobalPosition,t);
        Require(actual.DistanceTo(expected)<=1,"Community contact disconnected "+art.Animation+"/"+art.Frame);
        Require(Math.Abs(art.Scale.Y-96/307f)<.001,"Community rest scale drift");
    }
    private async Task CommunityRestActions(bool reduced)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){ReducedMotion=reduced,TextSpeed=0},false);var main=await NewPolishMain();
        try{
            var player=main.World.Player;var bench=(BenchView)main.World.GetTarget("bench")!;var art=player.GetNode<AnimatedSprite2D>("Artwork");
            var foreign=s.GetScene("res://scenes/world/CommunityGate.tscn")!.Instantiate<WorldView>();AddChild(foreign);await Frames(2);
            try{Require(!main.Rest.Begin((BenchView)foreign.GetTarget("bench")!),"Foreign bench accepted");}finally{foreign.Free();}
            Require(!main.Rest.Begin(bench),"Distant bench accepted");player.Position=new(940,440);var before=player.Position;Require(main.Rest.Begin(bench)&&!main.Rest.Begin(bench),"Repeated Begin reset approach");
            Require(!main.CanOpenJournal&&main.SafeSavePosition==new Position2(before.X,before.Y),"Approach interaction/save not isolated");
            while(main.Rest.IsApproaching){var prior=player.Position;await DepthPhysics(1);Require(player.Position.DistanceTo(prior)<=112*player.GetPhysicsProcessDeltaTime()+.02,"Approach warped physics step");Require(NavigationGeometry.CanStand(main.World.Navigation,new(player.Position.X,player.Position.Y)),"Approach crossed obstacle");}
            for(int tick=0;main.Rest.Phase==RestPhase.SittingDown&&tick<180;tick++){AssertCommunityContact(main);if(art.Frame==0)await Capture("community-sit-start"+(reduced?"-reduced":""));if(art.Frame==1)await Capture("community-sit-mid"+(reduced?"-reduced":""));if(art.Frame==3)await Capture("community-sit-end"+(reduced?"-reduced":""));await DepthPhysics(1);}
            Require(main.Rest.Phase==RestPhase.Seated,"Approach never seated");AssertCommunityContact(main);
            var menu=main.GetNode<RestOptionsController>("RestOptions");Require(menu.IsOpen&&menu.GetNode<Button>("Panel/Options/Rest").HasFocus(),"Smoke selected automatically");
            var facts=SnapshotJson(s.Snapshot);menu.GetNode<Button>("Panel/Options/Smoke").EmitSignal(Button.SignalName.Pressed);
            await WaitUntil(()=>art.Frame>=1,"Smoke progress");main.Phone.Open("messages");await Frames(2);var phase=main.Rest.Phase;var frame=art.Frame;var progress=art.FrameProgress;
            await Frames(15);Require(main.Rest.Phase==phase&&art.Frame==frame&&Math.Abs(art.FrameProgress-progress)<.001,"Phone advanced rest action");main.Phone.Close();await Frames(2);Require(main.Rest.Phase==RestPhase.Smoking,"Phone skipped smoke phase");
            main.Rest.Notification((int)NotificationApplicationFocusOut);player.Notification((int)NotificationApplicationFocusOut);await Frames(2);frame=art.Frame;progress=art.FrameProgress;await Frames(15);Require(frame==art.Frame&&Math.Abs(progress-art.FrameProgress)<.001,"Focus loss advanced smoke");
            main.Rest.Notification((int)NotificationApplicationFocusIn);player.Notification((int)NotificationApplicationFocusIn);
            for(int tick=0;main.Rest.Phase==RestPhase.Smoking&&tick<300;tick++){AssertCommunityContact(main);await DepthPhysics(1);}Require(main.Rest.Phase==RestPhase.Seated,"Smoke never finished");
            Require(s.Snapshot.Stage==SliceStage.FreeArrival&&s.Snapshot.CandyCount==0&&!s.Snapshot.CompletedActions.Contains("observation.community.first"),"Smoke granted facts/reward");
            main.World.Position=new(11,17);Require(main.SafeSavePosition==new Position2(960,440),"Seat save is global/hip coordinate");main.World.Position=Vector2.Zero;
            var file=System.IO.Path.Combine(s.SaveDirectory,"manual.json");Require(file.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Manual fixture outside project");System.IO.Directory.CreateDirectory(s.SaveDirectory);System.IO.File.WriteAllText(file,"{broken");
            KeyPress(Key.F9);await Frames(3);Require(main.Rest.Phase==RestPhase.Seated&&main.Rest.IsActive&&System.IO.File.ReadAllText(file)=="{broken","Failed F9 canceled seat/modified bad save");System.IO.File.Delete(file);KeyPress(Key.F5);await Frames(3);Require(s.ManualSaves.Load().Snapshot?.PlayerPosition==new Position2(960,440),"F5 saved hip/invalid position");
            main.Rest.HandleKey(Key.Escape);await Capture("community-stand-start"+(reduced?"-reduced":""));await WaitUntil(()=>art.Animation=="stand_up"&&art.Frame>=1,"Half stand");s.Flow=FlowState.Paused;await Frames(2);var normalized=(art.Frame+art.FrameProgress)/art.SpriteFrames.GetFrameCount(art.Animation);
            s.SetOptions(s.Options with{ReducedMotion=!reduced},false);await Frames(3);Require(main.Rest.Phase==RestPhase.StandingUp&&Math.Abs(normalized-(art.Frame+art.FrameProgress)/art.SpriteFrames.GetFrameCount(art.Animation))<.01,"Reduced toggle reset paused progress");
            await Frames(15);AssertCommunityContact(main);await Capture("community-stand-mid"+(reduced?"-reduced":""));Require(main.Rest.Phase==RestPhase.StandingUp,"Paused stand completed");s.Flow=FlowState.Field;
            for(int tick=0;main.Rest.IsActive&&tick<180;tick++){AssertCommunityContact(main);if(art.Frame==3)await Capture("community-stand-end"+(reduced?"-reduced":""));await DepthPhysics(1);}Require(!main.Rest.IsActive&&Math.Abs(art.Scale.Y-.24)<.001,"Risen Depth scale changed");
            player.Position=new(960,440);Require(main.Rest.Begin(bench),"Seat after pause rejected");await WaitForPhase(main,RestPhase.Seated);main.Rest.Cancel();art.EmitSignal(AnimatedSprite2D.SignalName.AnimationFinished);Require(main.Rest.Phase==RestPhase.Standing,"Disposed callback restored seat");
        }finally{s.Flow=FlowState.Field;main.Free();await Frames(3);}
    }
    private async Task CommunityRestDetour()
    {
        var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
        try{
            var obstacle=new StaticBody2D{Name="TestApproach",CollisionLayer=1,CollisionMask=0};var polygon=new CollisionPolygon2D{Name="CollisionPolygon2D",Polygon=new[]{new Vector2(944,432),new(948,432),new(948,448),new(944,448)}};obstacle.AddChild(polygon);main.World.GetNode("Navigation/Obstacles").AddChild(obstacle);
            var nav=NavigationSceneReader.Read(main.World);typeof(WorldView).GetProperty("Navigation")!.SetValue(main.World,nav);main.World.Player.ApplyNavigation(nav);main.World.Player.Position=new(930,440);await Frames(3);
            Require(!NavigationGeometry.CanTraverse(nav,new(930,440),new(960,440)),"Detour fixture clear");KeyPress(Key.E);await Frames(2);Require(main.Rest.IsActive,"E ignored planned bench reachability");
            await WaitForPhase(main,RestPhase.Seated);main.Rest.Cancel();main.World.Player.Position=new(920,440);var before=main.World.Player.Position;
            Require(main.Rest.Begin((BenchView)main.World.GetTarget("bench")!),"Second detour rejected");main.Rest.HandleKey(Key.Escape);Require(main.World.Player.Position==before&&!main.Rest.IsActive,"Approach cancel warped");
            main.World.Player.Position=new(946,440);before=main.World.Player.Position;Require(!main.Rest.Begin((BenchView)main.World.GetTarget("bench")!)&&main.World.Player.Position==before,"Blocked approach relocated player");
        }finally{s.Flow=FlowState.Field;main.Free();await Frames(3);}
    }
}
