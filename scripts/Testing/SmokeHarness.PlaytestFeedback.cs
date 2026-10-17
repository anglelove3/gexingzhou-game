using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private static Rect2I ResidentVisibleBounds(Texture2D texture)
    {
        using var image=texture.GetImage();if(image.IsCompressed())image.Decompress();image.Convert(Image.Format.Rgba8);
        var bytes=image.GetData();int width=image.GetWidth(),height=image.GetHeight(),left=width,top=height,right=0,bottom=0;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(bytes[(y*width+x)*4+3]>24){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x+1);bottom=Math.Max(bottom,y+1);}
        Require(right>left&&bottom>top,"Resident silhouette is transparent");return new(left,top,right-left,bottom-top);
    }
    private async Task PlaytestFeedbackChecks()
    {
        var failures=new List<string>();
        async Task Check(string name,Func<Task> body){try{await body();GD.Print("FEEDBACK2_PASS "+name);}catch(Exception e){failures.Add(name+": "+e.Message);GD.Print("FEEDBACK2_FAIL "+name+": "+e.Message);}}
        // Break: a floating sign advertises E, but a point target rejects the road above/below it.
        await Check("RoadEntranceAcceptsUpperAndLowerApproaches",async()=>{
            foreach(var foot in new[]{new Position2(1480,340),new Position2(1480,400),new Position2(1480,500)}){
                var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
                try{
                    main.World.Player.Position=new(foot.X,foot.Y);main.World.Player.Velocity=Vector2.Zero;await Frames(5);
                    Require(main.World.Interactions.Prompt.Contains("便利店街"),"Road entrance not selectable from "+foot);
                    KeyPress(Key.E);await WaitUntil(()=>main.World.SceneId=="convenience_street"&&s.Flow==FlowState.Field,"Road E transition "+foot);
                    var exit=main.World.GetTarget("community")!;main.World.Player.Position=exit.Position;await Frames(5);
                    KeyPress(Key.E);await WaitUntil(()=>main.World.SceneId=="community_gate"&&s.Flow==FlowState.Field,"Road E return");
                }finally{main.Free();await Frames(3);}
            }
        });
        await Check("EntryPromptOnlyPromisesAnAvailableInteraction",async()=>{
            var main=await NewPolishMain();
            try{
                main.World.Player.Position=new(1350,340);await Frames(5);
                var hint=main.GetNode<Label>("HUD/Guidance");
                Require(!hint.Visible||!hint.Text.Contains("按 E")&&!hint.Text.StartsWith("E "),"Out-of-range entrance promises E");
                var original=main.World.SceneId;KeyPress(Key.E);await Frames(5);Require(main.World.SceneId==original,"Remote E entered the street");
                main.World.Player.Position=new(1480,340);await Frames(5);
                Require(hint.Visible&&hint.Text.StartsWith("E ")&&main.World.Interactions.Prompt.Contains("便利店街"),"Ready entrance and HUD disagree");
                main.Pause.Open();await Frames(3);Require(!hint.Visible,"Entrance guidance leaks over pause");
            }finally{main.Free();await Frames(3);}
        });
        // Break: translating a single standing/seated texture does not create a walking gait.
        await Check("ResidentsHaveGroundedGaitsAndAdultProportions",async()=>{
            var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
            try{
                s.SetOptions(s.Options with{ReducedMotion=false},false);
                foreach(var scene in new[]{"community_gate","convenience_street"}){
                    if(scene!="community_gate")Require(main.ChangeWorld(scene,new(800,520)),"Resident fixture entry failed");
                    foreach(var resident in main.World.FindChildren("*","",true,false).OfType<AmbientResidentController>()){
                        resident.SetProcess(false);resident._Notification((int)NotificationApplicationFocusIn);
                        var art=resident.GetNode<Sprite2D>("Artwork");
                        var used=ResidentVisibleBounds(art.Texture);var height=used.Size.Y*Math.Abs(art.Scale.Y);
                        Require(height>=84&&height<=104,$"Resident adult height is inconsistent: {resident.Name} {height}");
                        var poses=new HashSet<string>();var min=resident.Position;var max=min;var geometry=new Dictionary<ulong,Rect2I>();
                        for(int i=0;i<160;i++){
                            resident._Process(.05);var texture=art.Texture;
                            poses.Add(texture is AtlasTexture atlas?atlas.Atlas.ResourcePath+atlas.Region:texture.ResourcePath);
                            min=min.Min(resident.Position);max=max.Max(resident.Position);
                            if(!geometry.TryGetValue(texture.GetInstanceId(),out var bounds)){bounds=ResidentVisibleBounds(texture);geometry.Add(texture.GetInstanceId(),bounds);}
                            var visibleHeight=bounds.Size.Y*Math.Abs(art.Scale.Y);
                            Require(visibleHeight>=84&&visibleHeight<=104,$"Visible walking silhouette shrinks or grows: {resident.Name} {visibleHeight}");
                            var footY=art.Position.Y+(bounds.End.Y-texture.GetHeight()/2f)*art.Scale.Y;
                            Require(Math.Abs(footY)<2.5f,"Resident foot floats or sinks "+resident.Name+" "+footY);
                        }
                        Require(poses.Count>=8&&max.DistanceTo(min)>32,"Resident slides without a complete gait/route "+resident.Name);
                        var position=resident.Position;var pose=art.Texture;var artAt=art.Position;
                        s.Flow=FlowState.Paused;resident._Process(10);Require(resident.Position==position&&art.Texture==pose&&art.Position==artAt,"Paused gait moves");s.Flow=FlowState.Field;
                        s.SetOptions(s.Options with{ReducedMotion=true},false);resident._Process(10);Require(resident.Position==position&&art.Texture==pose,"Reduced motion advances gait");
                        s.SetOptions(s.Options with{ReducedMotion=false},false);
                    }
                }
                // Scene changes are deliberate above; residents themselves must not dispatch story facts.
                Require(s.Snapshot.CompletedActions.Count==0&&s.Snapshot.CandyCount==0,"Ambient gait advanced story");
            }finally{main.Free();await Frames(3);}
        });
        await Check("StandingStoryActorsShareAdultScale",async()=>{
            var main=await NewPolishMain();
            try{
                var cannon=main.World.GetTarget("cannon")!.GetNode<Sprite2D>("Artwork");
                var cannonHeight=ResidentVisibleBounds(cannon.Texture).Size.Y*cannon.Scale.Y;
                Require(cannonHeight>=84&&cannonHeight<=104,"Standing Cannon is out of adult scale");
                Require(main.ChangeWorld("convenience_street",new(800,520)),"Hey scale fixture failed");await Frames(3);
                var hey=main.World.GetTarget("hey")!.GetNode<Sprite2D>("Artwork");var bounds=ResidentVisibleBounds(hey.Texture);
                Require(bounds.Size.Y*hey.Scale.Y>=84&&bounds.Size.Y*hey.Scale.Y<=104,"Standing Hey is still child-sized beside player: "+bounds.Size.Y*hey.Scale.Y);
                Require(Math.Abs(hey.Position.Y+(bounds.End.Y-hey.Texture.GetHeight()/2f)*hey.Scale.Y)<1.5f,"Standing Hey feet do not touch target anchor");
                Require(main.ChangeWorld("soup_shop",new(120,480)),"Shopkeeper scale fixture failed");await Frames(3);
                var shopkeeper=main.World.GetNode<Sprite2D>("DepthLayers/Actors/Shopkeeper");var shopHeight=ResidentVisibleBounds(shopkeeper.Texture).Size.Y*shopkeeper.Scale.Y;
                Require(shopHeight>=84&&shopHeight<=104,"Standing shopkeeper is out of adult scale");
            }finally{main.Free();await Frames(3);}
        });
        await Check("CloserCommunityFramingPreservesWalkableEdges",async()=>{
            var main=await NewPolishMain();
            try{
                var display=main.GetNode<WorldDisplayController>("WorldDisplay");
                foreach(var foot in new[]{new Vector2(320,460),new Vector2(800,340),new Vector2(1500,510)}){
                    main.World.Player.Position=foot;await Frames(4);
                    Require(display.VisibleWorldRect.Size.Y<=410,"Community still has the wide empty framing");
                    Require(main.World.ViewBounds.Grow(.1f).Encloses(display.VisibleWorldRect),"Closer camera exposes outside map");
                    Require(display.VisibleWorldRect.Grow(.1f).HasPoint(foot),"Closer camera loses the player's feet at map edge");
                }
            }finally{main.Free();await Frames(3);}
        });
        await Check("MenuHasBundledTitleTypographyAndReducedMotion",async()=>{
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();s.SetOptions(s.Options with{ReducedMotion=false},false);
            var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<Control>();AddChild(menu);await Frames(4);
            try{
                var title=menu.GetNode<Label>("MenuScroll/Menu/Title");
                Require(title.GetThemeFont("font") is FontFile,"Title still relies on generic system typography");
                var atmosphere=menu.GetNode<Control>("Atmosphere");var leaves=atmosphere.GetChildren().OfType<Polygon2D>().ToArray();
                var before=leaves.Select(x=>x.Position).ToArray();await Frames(30);
                Require(leaves.Where((leaf,i)=>leaf.Position!=before[i]).Any(),"Autumn leaves do not fall");
                s.SetOptions(s.Options with{ReducedMotion=true},false);await Frames(3);Require(!atmosphere.Visible,"Reduced motion still shows falling foreground leaves");
            }finally{menu.Free();await Frames(3);}
        });
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
    }
    private async Task CapturePlaytestFeedback()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32}){
            s.NewGame();s.SetOptions(new(){TextSpeed=0,SubtitleSize=font,ReducedMotion=false},false);
            var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<Control>();AddChild(menu);await Frames(90);
            await Capture("feedback2-menu-"+font);s.SetOptions(s.Options with{ReducedMotion=true},false);await Frames(3);
            await Capture("feedback2-menu-reduced-"+font);menu.Free();await Frames(3);
            var main=await NewPolishMain();
            try{
                main.World.Player.Position=new(520,400);await Frames(5);await Capture("feedback2-community-"+font);
                foreach(var (label,y) in new[]{("upper",340f),("lower",500f)}){
                    main.World.Player.Position=new(1480,y);await Frames(5);
                    Require(main.World.Interactions.SelectedId=="street","Captured entry is not actionable "+label);
                    var point=main.WorldToUi(new Vector2(1480,y));
                    Require(main.GetGlobalRect().HasPoint(point),"Entry feet outside closer view");
                    await Capture("feedback2-entrance-"+label+"-"+font);
                }
                Require(main.ChangeWorld("convenience_street",new(800,520)),"Capture street missing");await Frames(5);await Capture("feedback2-street-"+font);
            }finally{main.Free();await Frames(3);}
        }
        foreach(var identity in new[]{"green","apron","beige"}){
            s.SetOptions(new(){TextSpeed=0,SubtitleSize=24,ReducedMotion=false},false);var main=await NewPolishMain();
            try{
                if(identity=="beige")Require(main.ChangeWorld("convenience_street",new(780,640)),"Gait capture street missing");
                else main.World.Player.Position=identity=="green"?new(550,390):new(1310,400);
                var path=identity switch{"green"=>"DepthLayers/Actors/BackgroundResidents/ResidentByGate","apron"=>"DepthLayers/Actors/BackgroundResidents/ResidentByStreet",_=>"DepthLayers/Actors/ResidentSouth"};
                var resident=main.World.GetNode<AmbientResidentController>(path);resident.SetProcess(false);resident._Notification((int)NotificationApplicationFocusIn);
                resident._Process(resident.PauseSeconds);resident._Process(.025);
                for(int i=0;i<8;i++){
                    await Frames(3);await Capture("feedback2-walk-"+identity+"-"+i);
                    resident._Process(resident.StrideLength/resident.StepSpeed/8);
                }
            }finally{main.Free();await Frames(3);}
        }
    }
}
