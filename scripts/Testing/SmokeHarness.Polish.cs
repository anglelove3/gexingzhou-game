using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task OpenObservation(MainView main,Interactable target)
    {
        target.TryInteract(GetNode<GameSession>("/root/GameSession"));await Frames(2);
        if(target is BenchView)
        {
            await WaitForPhase(main,RestPhase.Seated);
            var menu=main.GetNode<RestOptionsController>("RestOptions");
            if(!menu.IsOpen)main.Rest.HandleKey(Key.E);
            menu.GetNode<Button>("Panel/Options/Rest").EmitSignal(Button.SignalName.Pressed);await Frames(2);
        }
    }
    private async Task RestChecks()
    {
        Require(ResourceLoader.Exists("res://assets/animations/player-rest-v2.tres"),"AnimationResources missing player rest frames");
        var frames=GD.Load<SpriteFrames>("res://assets/animations/player-rest-v2.tres");
        foreach(var (name,count) in new[]{("sit_down",3),("seated",1),("smoke",6),("stand_up",3)})
        {
            Require(frames.HasAnimation(name)&&frames.GetFrameCount(name)>=count,"Missing actual rest frames "+name);
            Require(name=="seated"||frames.GetAnimationLoopMode(name)==SpriteFrames.LoopMode.None,"Rest action loops "+name);
            var regions=new HashSet<Rect2>();
            for(int i=0;i<frames.GetFrameCount(name);i++)
            {
                Require(frames.GetFrameTexture(name,i) is AtlasTexture,"Rest frame not raster");
                var atlas=(AtlasTexture)frames.GetFrameTexture(name,i);regions.Add(atlas.Region);
                Require(atlas.Atlas.ResourcePath.Contains("player-rest.png"),"Rest reuses another NPC");
            }
            Require(regions.Count>=count,"Rest uses duplicated poses "+name);
        }
        GD.Print("REST_PASS AnimationResources");
        var main=await NewPolishMain();var bench=main.World.GetNode<BenchView>("Bench");var player=main.World.Player;
        player.GlobalPosition=bench.StandAnchor.GlobalPosition;bench.GetNode<Sprite2D>("BenchForeground").Visible=true;
        var original=player.Position;var art=player.GetNode<AnimatedSprite2D>("Artwork");
        player.SetRestPose("seated",bench.SeatAnchor.GlobalPosition-player.GlobalPosition,false);await Frames(3);
        Require(art.Animation=="seated"&&player.Position==original,"Seat pose altered collision position");
        var before=art.Position;bench.SeatAnchor.Position+=new Vector2(5,0);
        player.SetRestPose("seated",bench.SeatAnchor.GlobalPosition-player.GlobalPosition,false);
        Require(Math.Abs(art.Position.X-before.X-5)<.01,"Edited seat anchor not used");
        player.ClearRestPose();Require(art.Animation=="idle","Walking pose not restored");main.Free();
        var integrated=await NewPolishMain();
        Require(integrated.GetNodeOrNull("Rest")!=null&&integrated.GetNodeOrNull("RestOptions")!=null,"ActualSeatAndNoAutoplay: rest integration missing");
        integrated.Free();
        await RestIntegrationChecks();
        await Frames();
    }
    private async Task WaitForPhase(MainView main,RestPhase phase)
    {
        await WaitUntil(()=>main.Rest.Phase==phase,"rest phase "+phase+" actual="+main.Rest.Phase);
    }
    private async Task RestIntegrationChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){SubtitleSize=32,TextSpeed=0,ReducedMotion=false},false);
        var main=await NewPolishMain();var bench=main.World.GetNode<BenchView>("Bench");main.World.Player.Position=bench.StandAnchor.Position+bench.Position;
        await Frames(3);KeyPress(Key.E);await WaitForPhase(main,RestPhase.Seated);
        var menu=main.GetNode<RestOptionsController>("RestOptions");var art=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");
        var rest=menu.GetNode<Button>("Panel/Options/Rest");var smoke=menu.GetNode<Button>("Panel/Options/Smoke");var rise=menu.GetNode<Button>("Panel/Options/Rise");
        Require(menu.IsOpen&&art.Animation=="seated"&&rest.HasFocus(),"ActualSeatAndNoAutoplay no default seated rest");
        foreach(var button in new[]{rest,smoke,rise})Require(main.GetGlobalRect().Encloses(button.GetGlobalRect()),"Rest menu font32 clipped");
        var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;await Capture("rest-options");
        main.Rest.HandleKey(Key.Escape);Require(main.Rest.Phase==RestPhase.Seated&&!menu.IsOpen,"First Escape stood up");
        await Capture("rest-seated");KeyPress(Key.E);await Frames(2);
        // Send one deterministic local click; do not interleave off-screen OS mouse updates between down/up.
        var clickPosition=smoke.GetGlobalRect().GetCenter();
        var inputViewport=main.GetViewport();
        inputViewport.PushInput(new InputEventMouseMotion{Position=clickPosition,GlobalPosition=clickPosition},true);
        Require(inputViewport.GuiGetHoveredControl()==smoke,"Mouse smoke hover missed saved button");
        inputViewport.PushInput(new InputEventMouseButton{Position=clickPosition,GlobalPosition=clickPosition,ButtonIndex=MouseButton.Left,Pressed=true},true);
        inputViewport.PushInput(new InputEventMouseButton{Position=clickPosition,GlobalPosition=clickPosition,ButtonIndex=MouseButton.Left,Pressed=false},true);await Frames(2);
        Require(main.Rest.Phase==RestPhase.Smoking,"Mouse smoke option ignored");
        await WaitUntil(()=>art.Frame>=4,"rest smoke visible frame");
        await Capture("rest-smoke");await WaitForPhase(main,RestPhase.Seated);
        Require(!menu.IsOpen&&art.Animation=="seated","Smoking autolooped or reopened menu");
        Require(s.Snapshot.Stage==stage&&s.Snapshot.CandyCount==candy,"Rest changed quest/reward");
        KeyPress(Key.E);await Frames(2);main.Phone.Open("messages");
        Require(!menu.IsOpen,"Phone blocked by rest menu");main.Phone.Close();
        Require(main.Rest.Phase==RestPhase.Seated,"Phone lost seat state");
        KeyPress(Key.E);await Frames(2);for(int i=0;i<20;i++)KeyPress(Key.E);await Frames(2);
        Require(main.FindChildren("RestOptions","",true,false).Count==1,"Repeated E duplicated menu");
        if(main.Dialogue.IsOpen){KeyPress(Key.Escape);await Frames(2);}
        Require(!s.Snapshot.CompletedActions.Contains("observation.community.first"),"Cancelled observation consumed marker");
        KeyPress(Key.E);await Frames(2);rest.EmitSignal(Button.SignalName.Pressed);await Frames(2);await Finish(main);await Frames(2);
        Require(s.Snapshot.CompletedActions.Contains("observation.community.first"),"Confirmed rest did not mark observation");
        KeyPress(Key.E);await Frames(2);rest.EmitSignal(Button.SignalName.Pressed);await Frames(2);await Finish(main);
        Require(s.Snapshot.Stage==stage&&s.Snapshot.CandyCount==candy,"Repeated observation changed quest/reward");
        s.AdvanceClock(35);await Frames(2);Require(s.Snapshot.InvitationState.PhoneRinging,"Invitation clock blocked by rest");
        KeyPress(Key.Tab);await Frames(2);Require(main.Phone.IsOpen&&!menu.IsOpen,"CallWhileSeated phone blocked");
        KeyPress(Key.Enter);await Frames(2);Require(main.Dialogue.IsOpen,"CallWhileSeated answer failed");await Finish(main);await Frames(2);
        Require(main.Rest.Phase==RestPhase.Seated,"Call did not return to seated");
        KeyPress(Key.F5);await Frames(2);var loaded=s.ManualSaves.Load();
        Require(loaded.Status==LoadStatus.Loaded&&loaded.Snapshot!.PlayerPosition==main.Rest.SavePosition,"Seated save is not standing-safe");
        // Keep the harness alive while exercising the real F9 scene replacement.
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(5);
        var reloaded=GetTree().CurrentScene as MainView;Require(reloaded!=null&&reloaded!=main&&reloaded.Rest.Phase==RestPhase.Standing,"F9 did not rebuild standing instance");
        main.Free();main=reloaded!;await Frames(2);bench=main.World.GetNode<BenchView>("Bench");
        Require(main.Rest.Begin(bench),"Reloaded bench rejected");await WaitForPhase(main,RestPhase.Seated);
        main.Rest.HandleKey(Key.Escape);main.Rest.HandleKey(Key.Escape);await WaitForPhase(main,RestPhase.Standing);
        var x=main.World.Player.Position.X;Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});await DepthPhysics(30);
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});Require(main.World.Player.Position.X>x+5,"Rise left movement locked");
        await Capture("rest-risen");Require(main.Rest.Begin(bench),"Repeated bench rejected");await WaitForPhase(main,RestPhase.Seated);
        main.Rest.HandleKey(Key.Escape);KeyPress(Key.E);await Frames(2);smoke=main.GetNode<Button>("RestOptions/Panel/Options/Smoke");smoke.EmitSignal(Button.SignalName.Pressed);await Frames(2);
        var oldArt=main.World.Player.GetNode<AnimatedSprite2D>("Artwork");var transition=main.SceneFlow.TryEnter("convenience_street",new(120,280));
        oldArt.EmitSignal(AnimatedSprite2D.SignalName.AnimationFinished);var result=await transition;await Frames(2);
        Require(result.Success&&main.Rest.Phase==RestPhase.Standing&&!main.GetNode<RestOptionsController>("RestOptions").IsOpen,"Transition allowed old animation callback");
        GetTree().CurrentScene=null;main.Free();await Frames(2);
        GD.Print("REST_PASS ActualSeatAndNoAutoplay MenuKeyboardAndButtonSignal RepeatedKeysUnlock CallWhileSeated SaveLoadStanding TransitionCancelsCallbacks ObservationExactlyOnce");
    }
    private async Task PolishedUiChecks()
    {
        foreach(var file in new[]{"phone-frame","dialogue-frame"})
        {
            var path=$"res://assets/art/vs01-v2/{file}.png";
            Require(ResourceLoader.Exists(path),"FrameArtwork missing "+file);
            using var image=Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
            Require(image.GetWidth()>=256&&image.GetHeight()>=128,"FrameArtwork too small");
            Require(image.GetPixel(0,0).A<.01f,"FrameArtwork exterior is not transparent");
        }
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();s.AdvanceClock(65);
            var expected=string.Join("\n",s.Catalog!.Dialogues.Values.OrderByDescending(n=>string.Join("",n.Lines).Length).First().Lines);
            main.Dialogue.ShowText("葛行舟",expected,kind:DialogueLineKind.Thought);await Frames(3);
            var body=main.Dialogue.GetNode<Label>("Panel/Content/Body/Text");
            Require(body.Text==expected,"Story truncated to fit artwork");
            var scroll=main.Dialogue.GetNode<ScrollContainer>("Panel/Content/Body");scroll.ScrollVertical=10000;
            Require(main.GetGlobalRect().Encloses(main.Dialogue.GetNode<Label>("Panel/Content/ContinueHint").GetGlobalRect()),"Continue hint clipped");
            Require(main.Dialogue.GetNode<PanelContainer>("Panel").GetThemeStylebox("panel") is StyleBoxFlat,"Dialogue lacks compact style");
            var monologueStyle=(StyleBoxFlat)main.Dialogue.GetNode<PanelContainer>("Panel").GetThemeStylebox("panel");
            Require(monologueStyle.BgColor.A<.85f,"Monologue does not use lighter presentation");
            await Capture("polish-dialogue-"+font);
            main.Phone.Open("messages");await Frames(3);
            var answer=main.Phone.GetNode<Button>("Frame/Content/AnswerButton");var close=main.Phone.GetNode<Button>("Frame/Content/CloseButton");
            Require(answer.IsVisibleInTree()&&answer.FocusMode==Control.FocusModeEnum.All,"Answer unreachable");
            Require(close.IsVisibleInTree()&&main.GetGlobalRect().Encloses(close.GetGlobalRect()),"Close clipped");
            Require(main.Phone.GetNode<PanelContainer>("Frame").GetThemeStylebox("panel") is StyleBoxTexture,"Phone has no raster frame");
            await Capture("polish-phone-"+font);
            main.Phone.Close();await Frames(2);
            Require(main.Dialogue.IsOpen&&main.Dialogue.Visible,"PhoneOverDialogueRestoration lost dialogue");
            main.Dialogue.HandleKey(Key.Escape);main.Free();await Frames(2);
            var spoken=await NewPolishMain();spoken.Dialogue.ShowText("张大炮",expected);await Frames(2);
            Require(((StyleBoxFlat)spoken.Dialogue.GetNode<PanelContainer>("Panel").GetThemeStylebox("panel")).BgColor.A>.85f,"Monologue tint leaked into spoken dialogue");
            spoken.Dialogue.HandleKey(Key.Escape);spoken.Free();await Frames(2);
        }
        GD.Print("POLISHED_UI_PASS FrameArtwork FontAndFocus PhoneOverDialogueRestoration");
    }
    private async Task ResponsiveUiChecks()
    {
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;
        window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(1440,1080)})
        {
            window.Size=size;await Frames(3);var main=await NewPolishMain();
            foreach(var id in new[]{"community_gate","convenience_street","soup_shop"})
            {
                if(main.World.SceneId!=id)Require(main.ChangeWorld(id,id=="soup_shop"?new(120,480):new(320,280)),"Map change failed");await Frames(3);
                var display=main.GetNode<SubViewportContainer>("WorldDisplay");
                Require(display.GetGlobalRect().IsEqualApprox(main.GetGlobalRect()),"WindowCoverage: display leaves game margins "+size);
                Require(main.GetNode<PanelContainer>("HUD/TaskCard").Size.X<main.Size.X*.65f,"Full-width task bar remains");
                Require(main.GetGlobalRect().Encloses(main.GetNode<Label>("HUD/InteractionHint").GetGlobalRect()),"Hint clipped");
                var viewProperty=display.GetType().GetProperty("VisibleWorldRect");
                var boundsProperty=main.World.GetType().GetProperty("ViewBounds");
                Require(viewProperty!=null&&boundsProperty!=null,"Non169WorldView missing view geometry");
                var view=(Rect2)viewProperty!.GetValue(display)!;var bounds=(Rect2)boundsProperty!.GetValue(main.World)!;
                Require(bounds.Grow(.02f).Encloses(view),"Camera outside artwork");
                var backdrop=main.World.GetNode<Sprite2D>("Backdrop");
                var artBounds=new Rect2(backdrop.Position,backdrop.Texture.GetSize()*backdrop.Scale);
                Require(artBounds.Grow(.02f).Encloses(view),"ViewBounds not within actual backdrop");
                var camera=main.World.Player.GetNode<Camera2D>("Camera2D");
                Require(Math.Abs(camera.Zoom.X-camera.Zoom.Y)<.001,"World stretched");
                Require(Math.Abs(view.Size.X/view.Size.Y-main.Size.X/main.Size.Y)<.01,"View aspect differs from display");
                Require(display.GetNode<SubViewport>("WorldViewport").Size==new Vector2I((int)display.Size.X/2,(int)display.Size.Y/2),"Viewport resolution mismatch");
            }
            main.Free();await Frames(2);
        }
        window.Size=oldSize;window.ContentScaleMode=oldMode;
        GD.Print("RESPONSIVE_UI_PASS WindowCoverage Non169WorldView");
    }
    private async Task EditableUiChecks()
    {
        var retained=new List<Resource>();
        foreach(var path in new[]{"scenes/ui/RestOptions.tscn","scenes/world/Bench.tscn","assets/ui/vs01-v2/phone.tres","assets/ui/vs01-v2/dialogue.tres","assets/animations/player-rest-v2.tres","assets/animations/player-rest-reduced-v2.tres","assets/art/vs01-v2/phone-frame.png","assets/art/vs01-v2/dialogue-frame.png","assets/art/vs01-v2/player-rest.png","assets/art/vs01-v2/bench-foreground.png"})
        {
            var resource=ResourceLoader.Load("res://"+path);Require(ResourceLoader.Exists("res://"+path)&&resource!=null,"BundleDependencies "+path);retained.Add(resource!);
        }
        var saved=new (string File,string[] Paths)[]{
            ("Boot",new[]{"Background","Menu/Title","Menu/StartButton","Menu/AutoResumeButton","Menu/ManualResumeButton","Menu/RecoveryButton","Menu/SettingsButton","Menu/QuitButton","StartupError","Choices","Settings"}),
            ("Main",new[]{"WorldDisplay/WorldViewport","HUD/TaskCard","HUD/InteractionHint","Phone","Dialogue","Choices","Settings","SceneFlow","TransitionOverlay","StartupError"}),
            ("ui/Phone",new[]{"Frame/Content/Contact","Frame/Content/MessageScroll/Messages","Frame/Content/Task","Frame/Content/AnswerButton","Frame/Content/CloseButton"}),
            ("ui/Dialogue",new[]{"Panel/Content/NameLabel","Panel/Content/Body","Panel/Content/ContinueHint","Portrait"}),
            ("ui/Choices",new[]{"Panel/Content/Title","Panel/Content/Options","Panel/Content/ReturnHint"}),
            ("ui/Settings",new[]{"Panel/Scroll/Content/FontSize","Panel/Scroll/Content/TextSpeed","Panel/Scroll/Content/Assistance","Panel/Scroll/Content/ReducedMotion","Panel/Scroll/Content/RecordEvents","Panel/Scroll/Content/ClearButton","Panel/Scroll/Content/ExportButton","Panel/Scroll/Content/CloseButton"}),
            ("world/MemorySoupTable",new[]{"Background","Table/Bowl","Status","Table/Coin1","Table/Coin4","Table/Foods/Take","Table/Foods/Wait","Table/Foods/Share","Hint"})};
        foreach(var (file,paths) in saved)
        {
            Require(ResourceLoader.Exists($"res://scenes/{file}.tscn"),"OfflineUiTree missing scene "+file);
            GD.Print("EDITABLE_UI_OFFLINE "+file);
            var packed=GD.Load<PackedScene>($"res://scenes/{file}.tscn");retained.Add(packed);var node=packed.Instantiate();
            try{foreach(var path in paths)Require(node.GetNodeOrNull(path)!=null,"OfflineUiTree missing "+file+"/"+path);}
            finally{node.Free();}
        }
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();session.SetOptions(new(){TextSpeed=0},false);
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();
        var nameLabel=main.GetNode<Label>("Dialogue/Panel/Content/NameLabel");
        var panel=main.GetNode<PanelContainer>("Dialogue/Panel");
        var style=new StyleBoxFlat{BgColor=new Color(.23f,.17f,.14f,1)};panel.AddThemeStyleboxOverride("panel",style);
        var editedOffset=panel.OffsetLeft+11;panel.OffsetLeft=editedOffset;
        AddChild(main);await Frames(3);main.Dialogue.ShowText("张大炮","测试正文");await Frames(3);
        Require(ReferenceEquals(nameLabel,main.Dialogue.GetNode<Label>("Panel/Content/NameLabel")),"NoDuplicateUiOnOpen label replaced");
        Require(nameLabel.Text=="张大炮","Speaker not separately bound");
        Require(ReferenceEquals(panel.GetThemeStylebox("panel"),style)&&Math.Abs(panel.OffsetLeft-editedOffset)<0.1,"SavedStylesSurviveReady overwritten");
        main.Dialogue.HandleKey(Key.Escape);main.Phone.Open("messages");main.Phone.Close();main.Phone.Open("messages");
        Require(main.FindChildren("Phone","",true,false).Count==1,"NoDuplicateUiOnOpen phone duplicated");main.Phone.Close();main.Free();await Frames(2);
        GD.Print("EDITABLE_UI_PASS OfflineUiTree NoDuplicateUiOnOpen SavedStylesSurviveReady");
        GC.KeepAlive(retained);
    }
    private static void Require(bool value,string message)
    { if(!value)throw new InvalidOperationException(message); }

    private async Task<MainView> NewPolishMain()
    {
        GetNode<GameSession>("/root/GameSession").NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();
        AddChild(main);await Frames(3);return main;
    }

    // A runtime-only rebuild would lose edited positions, references and actor activation.
    private async Task EditableWorldChecks()
    {
        foreach(var (file,paths) in new[] {
            ("CommunityGate",new[]{"OldSign","Bench/SeatAnchor","Bench/StandAnchor","Cannon","StreetExit"}),
            ("ConvenienceStreet",new[]{"CommunityExit","Hey","SoupExit"}),
            ("SoupShop",new[]{"Targets/StreetExit","Targets/Seat","DepthLayers/Props/Sign","DepthLayers/Props/Menu","DepthLayers/Props/CounterNote","DepthLayers/Props/Counter","DepthLayers/Actors/Shopkeeper","DepthLayers/Props/Table/SoupBowl","Navigation/GroundBoundary/CollisionPolygon2D"}) })
        {
            var world=GD.Load<PackedScene>($"res://scenes/world/{file}.tscn").Instantiate<WorldView>();
            try
            {
                string playerPath=file=="SoupShop"?"DepthLayers/Actors/Player":"Player";
                var shared=new[]{"Backdrop",playerPath+"/Artwork",playerPath+"/Camera2D",playerPath+"/CollisionShape2D","Interactions"};
                var floor=file=="SoupShop"?new[]{"Navigation/Obstacles/Table/CollisionPolygon2D",playerPath+"/FootCollisionShape2D"}:new[]{"Floor/CollisionShape2D","LeftBoundary/CollisionShape2D","RightBoundary/CollisionShape2D"};
                foreach(var path in paths.Concat(shared).Concat(floor))
                    Require(world.GetNodeOrNull(path)!=null,$"SavedWorldNodes: {file}/{path} not editable offline");
                var before=world.GetNode<PlayerController>(playerPath);
                var backdrop=world.GetNode<Sprite2D>("Backdrop");var authored=backdrop.Position+new Vector2(0,2);backdrop.Position=authored;
                if(file=="CommunityGate")world.GetNode<Node2D>("Bench").Position=new Vector2(940,280);
                GetNode<GameSession>("/root/GameSession").NewGame();AddChild(world);await Frames(3);
                Require(ReferenceEquals(before,world.Player),"InspectorChangesSurviveReady: player rebuilt");
                Require(backdrop.Position==authored,"InspectorChangesSurviveReady: background reset");
                var ids=world.GetTargets().Select(t=>t.Id).ToArray();
                Require(ids.Length==ids.Distinct().Count(),"Duplicate interaction IDs");
                if(file=="CommunityGate")
                {
                    Require(world.GetNode<Node2D>("Bench").Position==new Vector2(940,280),"InspectorChangesSurviveReady: bench reset");
                    var cannon=world.GetNode<Interactable>("Cannon");
                    Require(!cannon.Visible,"QuestActorActivation: cannon already visible");
                    var s=GetNode<GameSession>("/root/GameSession");s.AdvanceClock(65);
                    var refresh=world.GetType().GetMethod("RefreshQuestActors");Require(refresh!=null,"QuestActorActivation: missing state binding");
                    refresh!.Invoke(world,new object[]{s.Snapshot});
                    Require(cannon.Visible,"QuestActorActivation: cannon never arrived");
                }
                GD.Print("EDITABLE_WORLD_PASS "+file);
            }
            finally {world.Free();await Frames(2);}
        }
        // Remove real required nodes from an instantiated authored world; no fake controller.
        foreach(var invalid in new[]{"missing_artwork","duplicate_id","missing_rest","missing_reduced_rest","invalid_rest_frames","missing_rest_pivot","missing_bench_foreground"})
        {
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
            var host=new Control();var error=new Label{Name="StartupError",Visible=false};host.AddChild(error);AddChild(host);
            var bad=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();
            if(invalid=="missing_artwork")bad.GetNode("Player/Artwork").Free();
            else if(invalid=="duplicate_id")bad.GetNode<Interactable>("StreetExit").Id="old_sign";
            else if(invalid=="missing_bench_foreground")bad.GetNode("Bench/BenchForeground").Free();
            else
            {
                var player=bad.GetNode<PlayerController>("Player");
                if(invalid=="missing_rest")player.RestFrames=null!;
                else if(invalid=="missing_reduced_rest")player.ReducedRestFrames=null!;
                else if(invalid=="invalid_rest_frames")player.RestFrames=new SpriteFrames();
                else
                {
                    player.RestFrames=(SpriteFrames)player.RestFrames.Duplicate(true);
                    player.RestFrames.GetFrameTexture("smoke",0).RemoveMeta("seat_pivot");
                }
            }
            host.AddChild(bad);await Frames(2);
            Require(error.Visible&&error.Text.Length>0,"MissingBindingStopsInteraction: no readable error "+invalid);
            Require(bad.HasMeta("binding_error"),"MissingBindingStopsInteraction: world still initialized "+invalid);
            var stage=s.Snapshot.Stage;bad.GetNode<Interactable>("OldSign").TryInteract(s);KeyPress(Key.E);await Frames(2);
            Require(s.Snapshot.Stage==stage,"Invalid world advanced story");host.Free();await Frames(2);
        }
        GetNode<GameSession>("/root/GameSession").NewGame();
        GD.Print("EDITABLE_WORLD_PASS MissingBindingStopsInteraction");
    }
}
