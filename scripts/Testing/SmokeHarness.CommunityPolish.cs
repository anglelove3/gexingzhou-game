using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private Task CommunityPolishCapture(string label)=>DisplayServer.GetName()=="headless"?Task.CompletedTask:Capture(label);
    private async Task CommunityPolishChecks()
    {
        var failures=new List<string>();
        foreach(var check in new Func<Task>[] {CommunityNoticeWallDepth,CommunityCloserFraming,CommunityAmbientResidents,CommunityStreetWayfinding})
            try{await check();}catch(Exception error){failures.Add(error.Message);GD.PrintErr("COMMUNITY_POLISH_RED "+error.Message);}
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
        GD.Print("COMMUNITY_POLISH_PASS "+(DisplayServer.GetName()=="headless"?"WallNoticeLayer":"WallNoticePixelDepth")+" ProportionalFraming AmbientNoQuest StreetWalkEAndReturn");
    }
    // Regression: a wall notice drawn as a root sibling used to cover the player's upper body.
    private async Task CommunityNoticeWallDepth()
    {
        var main=await NewPolishMain();
        try{
            var player=main.World.Player;var notice=(ObservationHotspot)main.World.GetTarget("community.notice")!;
            var visual=notice.GetNode<Node2D>(notice.VisualTarget);var viewport=main.GetNode<SubViewport>("WorldDisplay/WorldViewport");
            player.Position=new(740,315);player.Velocity=Vector2.Zero;await Frames(4);player.SetPhysicsProcess(false);player.GetNode<AnimatedSprite2D>("Artwork").Stop();
            if(DisplayServer.GetName()=="headless")
            {
                static int Layer(Node node){int z=0;for(Node? n=node;n is not WorldView&&n!=null;n=n.GetParent())if(n is CanvasItem item){z+=item.ZIndex;if(!item.ZAsRelative)break;}return z;}
                Require(Layer(visual)<Layer(player),"Wall notice covers player upper body: wall layer is not behind actors");
            }
            else
            {
            async Task<Image> Read(bool actor,bool board){player.Visible=actor;visual.Visible=board;await Frames(3);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);return viewport.GetTexture().GetImage();}
            using var background=await Read(false,false);using var actor=await Read(true,false);using var board=await Read(false,true);using var both=await Read(true,true);
            static float Difference(Color a,Color b)=>Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);
            var artwork=player.GetNode<AnimatedSprite2D>("Artwork");
            var texture=artwork.SpriteFrames.GetFrameTexture(artwork.Animation,artwork.Frame);
            using var source=texture.GetImage();var inverse=artwork.GetGlobalTransformWithCanvas().AffineInverse();
            int overlap=0,correct=0;
            for(int y=0;y<both.GetHeight();y++)for(int x=0;x<both.GetWidth();x++){
                // Semi-transparent silhouette edges correctly blend with the board; test opaque body pixels.
                var point=inverse*new Vector2(x+.5f,y+.5f)+texture.GetSize()/2;
                int sx=(int)point.X,sy=(int)point.Y;if(artwork.FlipH)sx=source.GetWidth()-1-sx;
                if(sx<1||sy<1||sx>=source.GetWidth()-1||sy>=source.GetHeight()-1)continue;
                bool opaque=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(source.GetPixel(sx+dx,sy+dy).A<.98f)opaque=false;
                if(!opaque)continue;
                var a=actor.GetPixel(x,y);var b=board.GetPixel(x,y);var ground=background.GetPixel(x,y);
                if(Difference(a,ground)<.15f||Difference(b,ground)<.15f||Difference(a,b)<.15f)continue;
                overlap++;if(Difference(both.GetPixel(x,y),a)<.03f)correct++;
            }
            await CommunityPolishCapture("notice-player-wall-depth");GD.Print($"COMMUNITY_NOTICE_DEPTH overlap={overlap} playerFront={correct}");
            Require(overlap>=20&&correct>=overlap*.90,"Wall notice covers player's upper body");
            }
            player.SetPhysicsProcess(true);player.Position=notice.Position;await Frames(4);
            Require(notice.ContainsWorldPoint(notice.Shape.GlobalPosition),"Wall notice lost authored mouse hotspot");
            KeyPress(Key.E);await Frames(3);Require(main.Dialogue.IsOpen,"Wall notice moved out of reachable E range");KeyPress(Key.Escape);
        }finally{main.Free();await Frames(2);}
    }
    private async Task CommunityCloserFraming()
    {
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        try{
            foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1440,900),new Vector2I(1024,768)}){
                window.Size=size;await Frames(3);var main=await NewPolishMain();
                try{
                    var display=main.GetNode<WorldDisplayController>("WorldDisplay");var camera=main.World.Player.GetNode<Camera2D>("Camera2D");
                    foreach(var foot in new[]{new Vector2(320,460),new Vector2(740,310),new Vector2(1480,510)}){
                        main.World.Player.Position=foot;await Frames(4);var view=display.VisibleWorldRect;
                        Require(view.Size.Y<=460.02f,"Community still frames 540 world units of empty foreground");
                        Require(Math.Abs(camera.Zoom.X-camera.Zoom.Y)<.001,"Community backdrop stretches");
                        Require(main.World.ViewBounds.Grow(.02f).Encloses(view)&&view.HasPoint(foot),"Closer framing crops foot or leaves actual art");
                        Require(Math.Abs(view.Size.X/view.Size.Y-main.Size.X/main.Size.Y)<.01,"Community framing aspect drift");
                    }
                    main.World.Player.Position=new(960,440);await Frames(4);await CommunityPolishCapture("closer-community-"+size.X+"x"+size.Y);
                    Require(main.World.GetAnchor("stand").Position==new Vector2(960,440)&&main.World.GetAnchor("seat").Position==new Vector2(960,380),"Framing moved furniture world anchors");
                }finally{main.Free();await Frames(2);}
            }
        }finally{window.Size=oldSize;window.ContentScaleMode=oldMode;await Frames(3);}
    }
    private async Task CommunityAmbientResidents()
    {
        GetNode<GameSession>("/root/GameSession").SetOptions(new(){TextSpeed=0,ReducedMotion=false},false);
        var main=await NewPolishMain();
        try{
            var session=GetNode<GameSession>("/root/GameSession");var stage=session.Snapshot.Stage;var actions=session.Snapshot.CompletedActions.ToArray();
            var population=main.World.GetNodeOrNull<Node2D>("DepthLayers/Actors/BackgroundResidents");
            Require(population!=null&&population.GetChildCount()==2,"Community lacks two background residents");
            var residents=population!.GetChildren().OfType<Node2D>().ToArray();var before=residents.Select(r=>r.Position).ToArray();
            Require(residents.All(r=>r.GetNodeOrNull<Sprite2D>("Artwork")?.Texture!=null),"Ambient resident lacks real artwork");
            var cannon=main.World.GetTarget("cannon")!.GetNode<Sprite2D>("Artwork").Texture.ResourcePath;
            Require(residents.All(r=>r.GetNode<Sprite2D>("Artwork").Texture.ResourcePath!=cannon),"Ambient resident duplicates Zhang Dapao");
            await DepthPhysics(160);
            Require(residents.Select((r,i)=>r.Position.DistanceTo(before[i])).Any(d=>d>=1),"Background residents never move on short paths");
            Require(residents.All(r=>NavigationGeometry.CanStand(main.World.Navigation,new(r.Position.X,r.Position.Y))),"Background resident path leaves walkable area");
            Require(main.World.GetTargets().Count==6,"Background ambience creates new quest interaction");
            Require(session.Snapshot.Stage==stage&&session.Snapshot.CompletedActions.SetEquals(actions)&&session.Snapshot.CandyCount==0,"Background residents advance story");
            Require(main.World.GetNodeOrNull<Sprite2D>("WallDetails/LivingDetails")?.Texture!=null,"Community lacks living detail artwork");await CommunityPolishCapture("ambient-community");
        }finally{main.Free();await Frames(2);}
    }
    private async Task CommunityStreetWayfinding()
    {
        var main=await NewPolishMain();
        try{
            var session=GetNode<GameSession>("/root/GameSession");var stage=session.Snapshot.Stage;
            main.World.Player.Position=new(1380,460);main.World.Player.Velocity=Vector2.Zero;await Frames(4);
            var sign=main.World.GetNodeOrNull<Label>("StreetExit/DirectionSign");var ground=main.World.GetNodeOrNull<Label>("StreetExit/GroundDirection");
            Require(sign is {Visible:true}&&sign.Text.Contains("便利店街")&&ground is {Visible:true},"Community street exit has no clear static destination and ground arrow");
            Require(main.GetNode<Label>("HUD/Guidance") is {Visible:true} hint&&hint.Text.Contains("便利店街"),"Street direction missing from nearby HUD");
            Input.ActionPress("move_right");await DepthPhysics(45);Input.ActionRelease("move_right");await DepthPhysics(12);
            Require(main.World.Player.Position.X>1440&&NavigationGeometry.CanStand(main.World.Navigation,new(main.World.Player.Position.X,main.World.Player.Position.Y)),"Player cannot walk to street exit");
            Require(main.World.Interactions.Prompt.Contains("便利店街"),"Street exit not selected from walkable approach");await CommunityPolishCapture("street-exit-ready");
            KeyPress(Key.E);await WaitUntil(()=>main.World.SceneId=="convenience_street"&&session.Flow==FlowState.Field,"Street E transition");
            Require(main.World.Mode==WorldMode.Horizontal,"Community polish changes convenience street movement");
            var exit=main.World.GetTarget("community")!;main.World.Player.Position=new(exit.Position.X+70,exit.Position.Y);main.World.Player.Velocity=Vector2.Zero;
            using(var down=new InputEventKey{Keycode=Key.A,PhysicalKeycode=Key.A,Pressed=true})Input.ParseInputEvent(down);
            await DepthPhysics(30);
            using(var up=new InputEventKey{Keycode=Key.A,PhysicalKeycode=Key.A,Pressed=false})Input.ParseInputEvent(up);
            await DepthPhysics(12);
            GD.Print($"COMMUNITY_STREET_RETURN position={main.World.Player.Position} exit={exit.Position} prompt={main.World.Interactions.Prompt}");
            Require(main.World.Player.Position.X<exit.Position.X+40&&main.World.Interactions.Prompt.Contains("安置小区"),"Street physical A input cannot reach return exit");
            KeyPress(Key.E);
            await WaitUntil(()=>main.World.SceneId=="community_gate"&&session.Flow==FlowState.Field,"Street E return");
            Require(session.Snapshot.Stage==stage&&session.Snapshot.CompletedActions.Count==0,"Street round trip advances quest");await CommunityPolishCapture("street-return-community");
        }finally{
            using(var up=new InputEventKey{Keycode=Key.A,PhysicalKeycode=Key.A,Pressed=false})Input.ParseInputEvent(up);
            Input.ActionRelease("move_left");Input.ActionRelease("move_right");main.Free();await Frames(2);
        }
    }
}
