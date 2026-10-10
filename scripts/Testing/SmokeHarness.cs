using Godot;
public partial class SmokeHarness : Node
{
    private string? captureDirectory;private readonly HashSet<string> captured=new();
    private static object? DiagnosticField(object target,string name)=>target.GetType().GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(target);
    public override void _Notification(int what)
    {
        if(captureDirectory!=null&&(what==NotificationApplicationFocusIn||what==NotificationApplicationFocusOut))
            GD.Print($"CAPTURE_FOCUS notification={what} ticks={Godot.Time.GetTicksMsec()}");
    }
    public override async void _Ready()
    {
        try
        {
            var suite=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--suite="))?.Split('=')[1] ?? "Movement";
            if(suite=="Journal"){await JournalChecks();GD.Print("GODOT_CHECKS_PASS Journal");await DrainAudio();GetTree().Quit();return;}
            if(suite=="PhoneChat"){await PhoneChatChecks();GD.Print("GODOT_CHECKS_PASS PhoneChat");await DrainAudio();GetTree().Quit();return;}
            if(suite=="FinalBoundaries"){await FinalBoundariesChecks();GD.Print("GODOT_CHECKS_PASS FinalBoundaries");await DrainAudio();GetTree().Quit();return;}
            if(suite=="DepthMovement"){await DepthMovementChecks();GD.Print("GODOT_CHECKS_PASS DepthMovement");await DrainAudio();GetTree().Quit();return;}
            if(suite=="DepthSeat"){await DepthSeatChecks();GD.Print("GODOT_CHECKS_PASS DepthSeat");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExplorationUi"){await ExplorationUiChecks();GD.Print("GODOT_CHECKS_PASS ExplorationUi");await DrainAudio();GetTree().Quit();return;}
            if(suite=="SaveUpgrade"){await SaveUpgradeChecks();GD.Print("GODOT_CHECKS_PASS SaveUpgrade");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceEnd"){await ExperienceEndChecks();GD.Print("GODOT_CHECKS_PASS ExperienceEnd");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperiencePause"){await ExperiencePauseChecks();GD.Print("GODOT_CHECKS_PASS ExperiencePause");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceAudio"){await ExperienceAudioChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS ExperienceAudio");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceCoins"){await ExperienceCoinsChecks();GD.Print("GODOT_CHECKS_PASS ExperienceCoins");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceDialogue"){await ExperienceDialogueChecks();GD.Print("GODOT_CHECKS_PASS ExperienceDialogue");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceGuidance"){await ExperienceGuidanceChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS ExperienceGuidance");await DrainAudio();GetTree().Quit();return;}
            if(suite=="ExperienceSoupSeat"){await ExperienceSoupSeatChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS ExperienceSoupSeat");await DrainAudio();GetTree().Quit();return;}
            if(suite=="EditableWorld")
            {
                await EditableWorldChecks();GD.Print("GODOT_CHECKS_PASS EditableWorld");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="EditableUi")
            {
                await EditableUiChecks();GD.Print("GODOT_CHECKS_PASS EditableUi");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="ResponsiveUi")
            {
                await ResponsiveUiChecks();GD.Print("GODOT_CHECKS_PASS ResponsiveUi");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="PolishedUi")
            {
                await PolishedUiChecks();GD.Print("GODOT_CHECKS_PASS PolishedUi");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="Rest"){await RestChecks();GD.Print("GODOT_CHECKS_PASS Rest");await DrainAudio();GetTree().Quit();return;}
            if(suite=="Art")
            {
                await ArtChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Art");await DrainAudio();GetTree().Quit();return;
            }
            if(suite is "Capture" or "CaptureNarrative" or "CapturePolish" or "CaptureExperience" or "CaptureSoupSeat" or "CaptureCoins" or "CapturePause" or "CaptureEnd" or "CaptureExploration" or "CaptureDepthSeat" or "CapturePhoneChat" or "CaptureJournal")
            {
                if(DisplayServer.GetName()=="headless")throw new Exception("Capture requires real rendering");
                var requested=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture-size="))?.Split('=',2)[1];
                if(requested!=null){var dimensions=requested.Split('x');GetWindow().Borderless=true;GetWindow().Size=new(int.Parse(dimensions[0]),int.Parse(dimensions[1]));await Frames(3);}
                var size=DisplayServer.WindowGetSize();captureDirectory=ProjectSettings.GlobalizePath($"res://test-output/captures/{size.X}x{size.Y}");GD.Print("PROJECT_USERDATA "+OS.GetUserDataDir());
                GetNode<GameSession>("/root/GameSession").SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
                if(suite=="CapturePhoneChat"){await PhoneChatChecks();GD.Print("GODOT_CHECKS_PASS CapturePhoneChat");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureJournal"){await CaptureJournalChecks();GD.Print("GODOT_CHECKS_PASS CaptureJournal");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureExperience"){await ExperienceDialogueChecks();await ExperienceGuidanceChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS CaptureExperience");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureSoupSeat"){await SoupSeatVisualChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS CaptureSoupSeat");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureExploration"){await CaptureExplorationChecks();GD.Print("GODOT_CHECKS_PASS CaptureExploration");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureDepthSeat"){await CaptureDepthSeatChecks();GD.Print("GODOT_CHECKS_PASS CaptureDepthSeat");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureCoins"){await CoinVisualChecks();GD.Print("GODOT_CHECKS_PASS CaptureCoins");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CapturePause"){await PauseVisualChecks();GD.Print("GODOT_CHECKS_PASS CapturePause");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureEnd"){await EndVisualChecks();GD.Print("GODOT_CHECKS_PASS CaptureEnd");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CapturePolish"){await PolishedUiChecks();await RestChecks();GD.Print("GODOT_CHECKS_PASS CapturePolish");await DrainAudio();GetTree().Quit();return;}
                if(suite=="CaptureNarrative")
                {
                    GetNode<GameSession>("/root/GameSession").SetOptions(new(){SubtitleSize=32,TextSpeed=0,ReducedMotion=true},false);
                    await ObservationVisualChecks();for(int food=0;food<3;food++)await MemoryPath(false,food,false);
                    GD.Print("GODOT_CHECKS_PASS CaptureNarrative");await DrainAudio();GetTree().Quit();return;
                }
                var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate();AddChild(menu);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GD.Print("MENU_FIRST_DRAW_MS "+Godot.Time.GetTicksMsec());await Capture("menu");menu.Free();await Frames(2);
                await ArtChecks();
                await MemoryPath(false,2,false);
                GD.Print("RENDER_FPS "+Engine.GetFramesPerSecond());GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Capture");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="Accessibility")
            {
                var clock=GetNode<GameSession>("/root/GameSession");clock.NewGame();
                for(int i=0;i<1000;i++)clock.AdvanceClock(.0015);
                if(clock.Snapshot.SceneActiveMilliseconds.GetValueOrDefault("community_gate")!=1500)throw new Exception("Active time loses sub-millisecond precision");
                foreach(var font in new[]{20,24,32})foreach(var enabled in new[]{false,true})
                {
                    var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true,Assistance=true,RecordEventsEnabled=enabled},false);
                    var log=System.IO.Path.Combine(s.SaveDirectory,"behavior","events.jsonl");var before=System.IO.File.Exists(log)?System.IO.File.ReadAllLines(log).Length:0;
                    await MemoryPath(false,2,false);
                    if(s.FontWarning.Length>0)throw new Exception(s.FontWarning);
                    var after=System.IO.File.Exists(log)?System.IO.File.ReadAllLines(log).Length:0;
                    if(!enabled&&after!=before||enabled&&after-before!=7)throw new Exception($"Event switch or pairing failed: enabled={enabled} before={before} after={after}");
                    GD.Print($"ACCESSIBILITY_PATH_PASS font={font} instant=true reduced=true records={enabled}");
                }
                var settingsMain=await NewPolishMain();KeyPress(Key.Escape);await Frames(2);
                if(!settingsMain.Pause.IsOpen)throw new Exception("Pause not keyboard accessible");
                settingsMain.Pause.GetNode<Button>("Panel/Scroll/Content/Settings").EmitSignal(Button.SignalName.Pressed);await Frames(2);
                if(!settingsMain.Settings.IsOpen)throw new Exception("Settings not accessible from pause");KeyPress(Key.Escape);await Frames(2);
                if(settingsMain.Settings.IsOpen||GetNode<GameSession>("/root/GameSession").Flow!=GeXingzhou.Domain.FlowState.Paused)throw new Exception("Settings did not restore pause flow");
                KeyPress(Key.Escape);await Frames(2);
                if(settingsMain.Pause.IsOpen||GetNode<GameSession>("/root/GameSession").Flow!=GeXingzhou.Domain.FlowState.Field)throw new Exception("Pause did not restore field flow");
                settingsMain.Free();await Frames(2);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Accessibility");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="Recovery")
            {
                await RecoveryChecks();GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Recovery");await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="Narrative")
            {
                GetNode<GameSession>("/root/GameSession").SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
                var returnOnly=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--narrative-return-only="))?.Split('=')[1];
                if(returnOnly!=null)await MemoryPath(false,int.Parse(returnOnly),false,false,true);
                else {await ObservationChecks();for(int food=0;food<3;food++)await MemoryPath(false,food,true,true,true);}
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Narrative");await DrainAudio();GetTree().Quit();return;
            }
            if(suite is "Resume" or "ResumeSeed")
            {
                await MemoryPath(false,0,false,true);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS "+suite);await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="ResumeRead")
            {
                var s=GetNode<GameSession>("/root/GameSession");var loaded=s.Saves.Load();if(loaded.Status!=GeXingzhou.Domain.LoadStatus.Loaded)throw new Exception("Cross-process save missing: "+loaded.Message);
                s.PendingRestore=loaded.Snapshot;var readMain=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(readMain);await Frames(3);
                if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete||s.Flow!=GeXingzhou.Domain.FlowState.EndCard||!readMain.SliceEnd.IsOpen||readMain.World.SceneId!="soup_shop")throw new Exception("Cross-process resume did not restore real world and end card");
                readMain.Free();await Frames(2);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS ResumeRead");await DrainAudio();GetTree().Quit();return;
            }
            if(suite is "Memory" or "Slice")
            {
                if(suite=="Memory")for(int food=0;food<3;food++)await MemoryPath(false,food,true);
                else foreach(var answer in new[]{false,true})await MemoryPath(answer,0,false);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS "+suite);await DrainAudio();GetTree().Quit();return;
            }
            if(suite=="Soup")
            {
                foreach(var (code,index) in new[]{("eat",0),("set_chopsticks",1),("check_phone",2)})await SoupPath(code,index);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Soup");await DrainAudio();GetTree().Quit();return;
            }
            if(suite is "Invitation" or "Hey")
            {
                foreach(var answer in new[]{false,true})await StoryPath(answer,suite);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
                GD.Print("GODOT_CHECKS_PASS "+suite);await DrainAudio();GetTree().Quit();return;
            }
            if(suite!="Movement") {GD.PrintErr("UNKNOWN_SUITE "+suite);GetTree().Quit(2);return;}
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate(); AddChild(main);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var players=main.FindChildren("Player","CharacterBody2D",true,false);
            if(players.Count!=1) throw new Exception("Real community player missing");
            var player=(CharacterBody2D)players[0];
            if(player.GetNodeOrNull<Camera2D>("Camera2D")==null) throw new Exception("Named camera missing");
            if(((MainView)main).World.Interactions==null) throw new Exception("World initialization incomplete");
            var x=player.Position.X;
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
            for(int i=0;i<25;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
            if(player.Position.X<=x) throw new Exception("Physical input did not move actual body");
            var controller=(PlayerController)player; controller.SetInputLocked(true); x=player.Position.X;
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
            for(int i=0;i<5;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(player.Position.X!=x)throw new Exception("Input lock leaked movement");
            controller.SetInputLocked(false);player.Position=new Vector2(1590,280);
            for(int i=0;i<30;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
            if(player.Position.X>1592.1f)throw new Exception("Player crossed boundary");
            player.Position=new Vector2(160,280);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=true});
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=false});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(GetNode<GameSession>("/root/GameSession").Flow!=GeXingzhou.Domain.FlowState.Dialogue)throw new Exception("Observation input did not open notice");
            GD.Print("GODOT_CHECKS_PASS "+suite);await DrainAudio();GetTree().Quit();
        }
        catch(Exception ex){GD.PrintErr("GODOT_CHECKS_FAIL "+ex.Message);GetTree().Quit(1);}
    }
    private async Task Frames(int count=1){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    // Fixed-fps simulation outruns the real audio thread; drain only test-owned root scenes before shutdown.
    private async Task DrainAudio()
    {
        foreach(var child in GetChildren().Where(n=>n is MainView or BootMenu).ToArray())child.Free();
        await Task.Delay(150);await Frames(2);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    // Catches missing runtime art, wrong frame/foot alignment, or cosmetics leaking into movement.
    private async Task ArtChecks()
    {
        // Dynamic GD.Load assets are not scene dependencies: the real export preset must explicitly include them.
        using(var preset=new ConfigFile())
        {
            if(preset.Load("res://export_presets.cfg")!=Error.Ok)throw new Exception("Cannot read production export preset");
            var patterns=preset.GetValue("preset.0","include_filter","").AsString().Split(',');
            foreach(var file in new[]{"community-v1.png","street-v1.png","soup-v1.png","player-v1.png","npcs-v1.png","props-v1.png","memory-v1.png"})
                if(!patterns.Any(pattern=>("assets/art/vs01-v1/"+file).Match(pattern.Trim())))throw new Exception("Dynamic artwork excluded from export preset: "+file);
        }
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        foreach(var scene in new[]{"community_gate","convenience_street","soup_shop"})
        {
            main.ChangeWorld(scene,scene=="soup_shop"?new(120,480):new(320,280));await Frames(3);
            var bg=main.World.GetNodeOrNull<Sprite2D>("Backdrop");
            if(bg?.Texture==null)throw new Exception("Art background missing: "+scene);
            if(bg.Texture.GetWidth()<960||bg.Texture.GetHeight()<360)throw new Exception("Background is not a production raster: "+scene);
            var sprite=main.World.Player.GetNodeOrNull<AnimatedSprite2D>("Artwork");
            if(sprite?.SpriteFrames==null||sprite.SpriteFrames.GetFrameCount("walk")<4)throw new Exception("Actual player walk frames missing");
            var start=main.World.Player.Position;
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
            for(int i=0;i<12;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(!sprite.Animation.ToString().StartsWith("walk")||main.World.Player.Position.X<=start.X)throw new Exception($"Art does not follow real movement scene={scene} start={start} end={main.World.Player.Position} velocity={main.World.Player.Velocity} animation={sprite.Animation} flow={s.Flow} physical={Input.IsPhysicalKeyPressed(Key.D)} action={Input.IsActionPressed("move_right")} focused={DiagnosticField(main.World.Player,"focused")}");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
            for(int i=0;i<18;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(!sprite.Animation.ToString().StartsWith("idle"))throw new Exception("Walking did not return to idle");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.A,Pressed=true});
            for(int i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(!sprite.FlipH)throw new Exception($"Character art faces away from leftward movement scene={scene} pos={main.World.Player.Position} animation={sprite.Animation} flow={s.Flow} physical={Input.IsPhysicalKeyPressed(Key.A)} action={Input.IsActionPressed("move_left")} focused={DiagnosticField(main.World.Player,"focused")} osfocus={GetWindow().HasFocus()}");
            main.World.Player.SetInputLocked(true);var locked=main.World.Player.Position;
            for(int i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(main.World.Player.Position!=locked||!sprite.Animation.ToString().StartsWith("idle"))throw new Exception("Artwork broke input locking");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.A,Pressed=false});main.World.Player.SetInputLocked(false);
            foreach(var animation in main.World.Mode==GeXingzhou.Domain.WorldMode.Depth2D?new[]{"idle_front","idle_back","idle_side","walk_front","walk_back","walk_side"}:new[]{"idle","walk"})for(int frame=0;frame<sprite.SpriteFrames.GetFrameCount(animation);frame++)
            {
                sprite.Stop();sprite.Animation=animation;sprite.Frame=frame;
                var texture=sprite.SpriteFrames.GetFrameTexture(animation,frame);var image=texture.GetImage();
                if(image==null||image.GetPixel(0,0).A>.02f)throw new Exception("Sprite frame lacks transparent padding");
                var used=image.GetUsedRect();var bottom=sprite.Position.Y+(used.End.Y-image.GetHeight()/2f)*sprite.Scale.Y;
                if(main.World.Mode==GeXingzhou.Domain.WorldMode.Depth2D)
                {
                    // Generated sprites contain sub-visible alpha noise; measure the visible sole, not that noise.
                    int visibleBottom=0;for(int y=image.GetHeight()-1;y>=0&&visibleBottom==0;y--)for(int x=0;x<image.GetWidth();x++)if(image.GetPixel(x,y).A>=.1f){visibleBottom=y+1;break;}
                    if(visibleBottom==0)throw new Exception("Depth sprite has no visible pixels");
                    bottom=sprite.Position.Y+(visibleBottom-image.GetHeight()/2f)*sprite.Scale.Y;
                }
                if(used.Size.X<=0||used.Size.Y<=0)throw new Exception("Character frame is empty");
                if(Math.Abs(bottom)>1.5f)throw new Exception($"Character feet jump away from collision baseline: {scene}/{animation}/{frame} bottom={bottom} used={used}");
                if(captureDirectory!=null&&scene=="community_gate"&&animation=="walk")
                {
                    main.World.Player.SetPhysicsProcess(false);await Capture("art-walk-"+frame);main.World.Player.SetPhysicsProcess(true);
                }
            }
            sprite.Play("idle");
            if(scene=="convenience_street"&&main.World.GetTargets().Single(t=>t.Id=="hey").GetNodeOrNull<Sprite2D>("Artwork")?.Texture==null)throw new Exception("Hey is still a placeholder");
            if(scene=="soup_shop"&&main.World.GetNodeOrNull<Sprite2D>("DepthLayers/Actors/Shopkeeper")?.Texture==null)throw new Exception("Soup shop owner raster missing");
            if(scene=="community_gate"){s.AdvanceClock(65);await Frames(2);if(main.World.GetNodeOrNull<Interactable>("Cannon")?.GetNodeOrNull<Sprite2D>("Artwork")?.Texture==null)throw new Exception("Arriving friend is still a placeholder");}
            if(scene=="soup_shop")main.World.Player.Position=new Vector2(720,480);
            await Capture("art-"+scene);
        }
        main.Free();await Frames(2);
        var menu=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<Control>();AddChild(menu);await Frames(2);
        if(menu.GetNodeOrNull<TextureRect>("Background")?.Texture==null)throw new Exception("Boot screen still has placeholder art");menu.Free();
        var memory=GD.Load<PackedScene>("res://scenes/world/MemorySoupTable.tscn").Instantiate<SoupMemoryController>();AddChild(memory);await Frames(2);
        if(memory.GetNodeOrNull<TextureRect>("Background")?.Texture==null)throw new Exception("Memory table art missing");
        if(!memory.GetNode<TextureRect>("Background").GetGlobalRect().IsEqualApprox(memory.GetGlobalRect())||memory.GetNode<TextureRect>("Table/Bowl").Size.X>180.1f||memory.GetNode<TextureRect>("Table/Bowl").Size.Y>140.1f)throw new Exception("Memory raster ignores its requested display size");
        if(memory.FindChildren("*","Button",true,false).OfType<Button>().Count(b=>b.Icon!=null)<4)throw new Exception("Playable coin controls have no actual art");memory.Free();
    }
    private void KeyPress(Key key){using var down=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true};using var up=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false};Input.ParseInputEvent(down);Input.ParseInputEvent(up);}
    private async Task Finish(MainView main)
    {
        if(main.SoupSeat.IsActing)await WaitUntil(()=>!main.SoupSeat.IsActing,"Soup before dialogue");
        for(int i=0;i<40&&main.Dialogue.IsOpen;i++){await GameTime(.18);if(!main.Dialogue.IsOpen)break;KeyPress(Key.E);await Frames(2);if(main.Dialogue.IsOpen){var panel=main.Dialogue.GetChildren().OfType<PanelContainer>().Single();if(!main.GetGlobalRect().Grow(.1f).Encloses(panel.GetGlobalRect()))throw new Exception("Dialogue overflows logical screen");}}
        if(main.Dialogue.IsOpen)throw new Exception("Dialogue never finished with keyboard");
    }
    private async Task StoryPath(bool answer,string suite,bool resume=false)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        s.AdvanceClock(35);KeyPress(Key.Tab);await Frames(2);
        if(!main.Phone.IsOpen)throw new Exception("Keyboard did not open phone");
        await Capture("phone");
        var time=s.Snapshot.InvitationState.Elapsed;s.AdvanceClock(100);
        if(s.Snapshot.InvitationState.Elapsed!=time)throw new Exception("Phone did not freeze invitation");
        KeyPress(Key.F5);await Frames(2);
        if(s.ManualSaves.Load().Status!=GeXingzhou.Domain.LoadStatus.Loaded||s.Flow!=GeXingzhou.Domain.FlowState.Phone)throw new Exception("Phone manual save failed or changed flow");
        if(answer){KeyPress(Key.Enter);await Frames(2);if(s.Snapshot.InvitationState.Resolution!=GeXingzhou.Domain.InvitationResolution.Answered)throw new Exception("Phone answer button not keyboard accessible");await Finish(main);}
        else {KeyPress(Key.Escape);await Frames(2);}
        if(s.Snapshot.CandyCount!=0)throw new Exception("Phone gave candy early");
        s.AdvanceClock(30);await Frames(2);
        var cannon=main.World.GetNodeOrNull<Interactable>("Cannon");
        if(cannon==null)throw new Exception($"Cannon missing: elapsed={s.Snapshot.InvitationState.Elapsed} arrived={s.Snapshot.InvitationState.CarArrived} flow={s.Flow} stage={s.Snapshot.Stage} scene={main.World.SceneId}");
        if(!cannon.TryInteract(s))throw new Exception("Cannot meet cannon");await Finish(main);
        if(s.Snapshot.CandyCount!=1)throw new Exception("Meeting did not give one candy");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");}
        if(suite=="Hey")
        {
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            main.ChangeWorld("community_gate",new(320,280));await Frames(2);
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            var hey=main.World.GetTargets().Single(t=>t.Id=="hey");hey.TryInteract(s);await Finish(main);
            if(s.Snapshot.CandyCount!=0||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.CandyHeyDelivered)throw new Exception("Hey delivery failed");
            if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");hey=main.World.GetTargets().Single(t=>t.Id=="hey");hey.TryInteract(s);}
            await Choose(main,0);await Finish(main);
            hey.TryInteract(s);await Choose(main,1);await Finish(main);if(s.Snapshot.CandyCount!=0)throw new Exception("Repeated delivery changed candy");
        }
        GD.Print("STORY_PATH_PASS "+(answer?"answered":"ignored")+" "+suite);main.Free();await Frames();
    }
    private async Task Choose(MainView main,int index)
    {
        if(main.SoupSeat.IsActing)await WaitUntil(()=>!main.SoupSeat.IsActing,"Soup before choice");
        if(!main.Choices.IsOpen)throw new Exception("Expected keyboard choice");
        await Frames(2);for(int i=0;i<index;i++){KeyPress(Key.Down);await Frames(2);}KeyPress(Key.Enter);await Frames(2);
        if(main.Choices.IsOpen)throw new Exception("Keyboard choice did not close");
    }
    private async Task<MainView> SoupPath(string code,int index,bool keep=false,bool answer=false,bool resume=false)
    {
        await StoryPath(answer,"Hey",resume);var s=GetNode<GameSession>("/root/GameSession");
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        main.ChangeWorld("soup_shop",new(400,430));await Frames(2);
        main.ChangeWorld("convenience_street",new(1120,280));main.ChangeWorld("soup_shop",new(400,430));await Frames(2);
        await Capture("soup");
        main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);
        await WaitUntil(()=>!main.SoupSeat.IsActing,"Soup first seat");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);}
        await Finish(main);await Choose(main,index);await Finish(main);
        if(s.Snapshot.ChoiceCodes["soup-response-1"]!=code||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SoupMeet)throw new Exception("Wrong soup route: want="+code+" got="+s.Snapshot.ChoiceCodes["soup-response-1"]);
        if(main.Choices.IsOpen){KeyPress(Key.Escape);await Frames(2);}
        var stage=s.Snapshot.Stage;main.ShowDialogue("missing_node");await Frames(2);KeyPress(Key.Escape);await Frames(2);
        if(s.Flow!=GeXingzhou.Domain.FlowState.Field||s.Snapshot.Stage!=stage)throw new Exception("Missing node locked or changed story");
        GD.Print("SOUP_PATH_PASS "+code);if(!keep){main.Free();await Frames();}return main;
    }
    private async Task MemoryPath(bool answer,int food,bool exercise,bool resume=false,bool narrative=false,bool missingCoinAudio=false)
    {
        var main=await SoupPath("eat",0,true,answer,resume);var s=GetNode<GameSession>("/root/GameSession");
        if(missingCoinAudio)main.Audio.CoinStream=null;
        if(narrative)SeedNarrativeMarkers(s);
        var seat=main.World.GetTargets().Single(t=>t.Id=="seat");seat.TryInteract(s);await Choose(main,0);await Frames(50);
        await WaitUntil(()=>main.Memory!=null&&s.Flow==GeXingzhou.Domain.FlowState.Memory&&s.Snapshot.MemoryState!=null,"Memory entry");
        if(main.Memory==null||s.Flow!=GeXingzhou.Domain.FlowState.Memory)throw new Exception("Memory scene failed to enter");
        await Capture("memory");
        if(captureDirectory!=null&&s.Options.SubtitleSize==32)await Capture("memory-font-32");
        KeyPress(Key.E);await Frames(2);KeyPress(Key.E);await Frames(2);var id=s.Snapshot.MemoryState!.InstanceId;
        if(s.Snapshot.PlayerPosition.Y!=280||s.Saves.Load().Snapshot?.MemoryState?.PushedTotal!=2)throw new Exception("Memory virtual coordinate overwritten by hidden world");
        if(captureDirectory!=null)GD.Print($"COIN_STATE food={food} step=2 total={s.Snapshot.MemoryState.PushedTotal} flow={s.Flow} ids={string.Join(',',s.Snapshot.MemoryState.PushedCoinIds)}");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");if(narrative)SeedNarrativeMarkers(s);if(s.Snapshot.MemoryState!.PushedTotal!=2)throw new Exception("Saved coins missing after real scene restore");}
        if(exercise)
        {
            KeyPress(Key.Escape);await Frames(50);
            if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.MemoryActive||s.Snapshot.SceneId!="soup_shop")throw new Exception("Escape incorrectly completed memory");
            main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);await Choose(main,0);await Frames(50);
            if(s.Snapshot.MemoryState!.InstanceId!=id||s.Snapshot.MemoryState.PushedTotal!=2)throw new Exception("Memory resume lost coins or instance");
        }
        KeyPress(Key.E);await Frames(2);KeyPress(Key.E);await Frames(2);
        if(captureDirectory!=null)GD.Print($"COIN_STATE food={food} step=4 total={s.Snapshot.MemoryState!.PushedTotal} flow={s.Flow} ids={string.Join(',',s.Snapshot.MemoryState.PushedCoinIds)}");
        if(s.Snapshot.MemoryState!.PushedTotal!=5)throw new Exception($"Keyboard did not push all coins: total={s.Snapshot.MemoryState.PushedTotal} flow={s.Flow} ids={string.Join(',',s.Snapshot.MemoryState.PushedCoinIds)}");
        await WaitUntil(()=>!main.Memory!.GetNode<Button>("Table/Foods/Take").Disabled,"Bowl arrival");
        for(int i=0;i<food;i++){KeyPress(Key.Right);await Frames(2);}KeyPress(Key.E);await Frames(50);await WaitUntil(()=>main.Memory==null&&s.Flow!=GeXingzhou.Domain.FlowState.Transition,"Memory return");
        if(main.SoupSeat.IsActing)await WaitUntil(()=>!main.SoupSeat.IsActing,"Seated after memory return");
        if(captureDirectory!=null){AssertDialogueFits(main);await Capture("return-"+food+"-font-"+s.Options.SubtitleSize);}
        if(narrative)AssertReturnBranch(main,food);
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");if(narrative)SeedNarrativeMarkers(s);main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);await WaitUntil(()=>!main.SoupSeat.IsActing,"Restored soup seat");if(narrative)AssertReturnBranch(main,food);}
        await Finish(main);
        if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete||s.Snapshot.SceneId!="soup_shop")throw new Exception("Memory did not return and complete slice");
        if(!s.Snapshot.CompletedActions.Contains("soup.payment:soup-payment-1"))throw new Exception("Payment feedback missing");
        Require(main.SliceEnd.IsOpen&&s.Flow==GeXingzhou.Domain.FlowState.EndCard,"Actual first completion did not show ending");
        main.SliceEnd.Close();
        if((int)s.Snapshot.MemoryState!.FoodChoice!.Value!=food)throw new Exception("Wrong memory food choice");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");main.SliceEnd.Close();}
        if(exercise)
        {
            main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);await Choose(main,0);await Frames(50);
            if(s.Snapshot.MemoryState!.InstanceId!="soup-2"||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete)throw new Exception("Replay changed story or reused instance");
            KeyPress(Key.E);await Frames(2);KeyPress(Key.Escape);await Frames(50);
            main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");
            main.World.GetTargets().Single(t=>t.Id=="seat").TryInteract(s);await Choose(main,0);await Frames(50);
            if(s.Snapshot.MemoryState!.InstanceId!="soup-2"||s.Snapshot.MemoryState.PushedTotal!=1)throw new Exception("Interrupted replay did not survive reload/reentry");
            for(int i=0;i<3;i++){KeyPress(Key.E);await Frames(2);}await WaitUntil(()=>!main.Memory!.GetNode<Button>("Table/Foods/Take").Disabled,"Replay bowl arrival");KeyPress(Key.E);await Frames(50);await Finish(main);
            if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete)throw new Exception("Replay regressed story");
        }
        var before=s.Snapshot;var result=await main.SceneFlow.TryEnter("missing_scene",new(1,1));
        if(result.Success||s.Snapshot.Stage!=before.Stage||s.Snapshot.SceneId!=before.SceneId||s.Flow!=GeXingzhou.Domain.FlowState.Field)throw new Exception("Failed transition changed state or locked input");
        GD.Print("MEMORY_PATH_PASS "+(answer?"answered":"ignored")+" food="+food);main.Free();await Frames();
    }
    private static string DialogueText(MainView main)=>main.Dialogue.GetNode<Label>("Panel/Content/Body/Text").Text;
    private static void SeedNarrativeMarkers(GameSession s)
    {
        s.SetOptions(s.Options with {TextSpeed=0},false);
        var nodes=new Dictionary<string,GeXingzhou.Domain.DialogueNode>(s.Catalog!.Dialogues);
        foreach(var (id,marker) in new[]{("soup.return.take","RETURN_TAKE"),("soup.return.wait","RETURN_WAIT"),("soup.return.share","RETURN_SHARE"),("observation.community.quiet","OBS_QUIET"),("observation.community.answered","OBS_ANSWERED"),("observation.community.unanswered","OBS_UNANSWERED")})
            nodes[id]=new("测试分支",new[]{marker});
        s.Catalog.Dialogues=nodes;
    }
    private static void AssertReturnBranch(MainView main,int food)
    {
        var expected=new[]{"RETURN_TAKE","RETURN_WAIT","RETURN_SHARE"}[food];
        if(!main.Dialogue.IsOpen||!DialogueText(main).Contains(expected))throw new Exception("Wrong visible return branch: expected="+expected+" actual="+DialogueText(main));
        GD.Print("NARRATIVE_RETURN_PASS "+expected);
    }
    private async Task ObservationChecks()
    {
        foreach(var (scenario,expected,targetId) in new[]{("quiet","OBS_QUIET","old_sign"),("answered","OBS_ANSWERED","bench"),("unanswered","OBS_UNANSWERED","old_sign")})
        {
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
            if(scenario!="quiet")s.AdvanceClock(35);
            if(scenario=="answered")s.TryDispatch(new("invitation.answer","answered","invitation-1"));
            SeedNarrativeMarkers(s);
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
            var target=main.World.GetTargets().Single(t=>t.Id==targetId);var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;
            await OpenObservation(main,target);
            if(!DialogueText(main).Contains(expected)||!DialogueText(main).Contains(target.Description))throw new Exception("Wrong first observation branch: "+scenario);
            KeyPress(Key.Escape);await Frames(2);
            if(s.Snapshot.CompletedActions.Contains("observation.community.first"))throw new Exception("Cancelled observation consumed first hint");
            await OpenObservation(main,target);await Finish(main);
            if(!s.Snapshot.CompletedActions.Contains("observation.community.first")||s.Snapshot.Stage!=stage||s.Snapshot.CandyCount!=candy)throw new Exception("Observation did not mark once or advanced quest");
            main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");SeedNarrativeMarkers(s);
            target=main.World.GetTargets().Single(t=>t.Id==targetId);await OpenObservation(main,target);
            if(!DialogueText(main).Contains(target.Description)||DialogueText(main).Contains("OBS_"))throw new Exception("Restored observation repeated first hint or lost location description");
            await Finish(main);GD.Print("NARRATIVE_OBSERVATION_PASS "+scenario);main.Free();await Frames(2);
        }
    }
    private static void AssertDialogueFits(MainView main)
    {
        var panel=main.Dialogue.GetChildren().OfType<PanelContainer>().Single();
        if(!main.GetGlobalRect().Grow(.1f).Encloses(panel.GetGlobalRect()))throw new Exception("Narrative dialogue overflows logical screen");
    }
    private async Task ObservationVisualChecks()
    {
        foreach(var scenario in new[]{"quiet","answered","unanswered"})
        {
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
            if(scenario!="quiet")s.AdvanceClock(35);
            if(scenario=="answered")s.TryDispatch(new("invitation.answer","answered","invitation-1"));
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
            main.World.GetTargets().Single(t=>t.Id=="old_sign").TryInteract(s);await Frames(3);AssertDialogueFits(main);
            await Capture("observation-"+scenario);await Finish(main);main.Free();await Frames(2);
        }
    }
    private async Task<MainView> Restart(MainView main)
    {
        var s=GetNode<GameSession>("/root/GameSession");var saved=s.Saves.Load();if(saved.Status!=GeXingzhou.Domain.LoadStatus.Loaded)throw new Exception("Checkpoint failed to save: "+saved.Message+" / "+s.SaveMessage);
        var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;var expected=saved.Snapshot!.PlayerPosition;
        main.Free();s.Free();await Frames(2);s=new GameSession{Name="GameSession"};GetTree().Root.AddChild(s);s.PendingRestore=saved.Snapshot;
        main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        if(s.Snapshot.Stage!=stage||s.Snapshot.CandyCount!=candy||s.PendingRestore!=null)throw new Exception("Checkpoint not restored");
        if(s.Snapshot.SceneId!="memory_soup_table")
        {
            var ended=s.Snapshot.Stage==GeXingzhou.Domain.SliceStage.SliceComplete;
            var expectedFlow=ended?GeXingzhou.Domain.FlowState.EndCard:GeXingzhou.Domain.FlowState.Field;
            if(main.World.Player.Position.X!=expected.X||s.Flow!=expectedFlow||ended&&!main.SliceEnd.IsOpen)throw new Exception("Restore position or input lock wrong");
            if(ended)main.SliceEnd.Close();
        }
        GD.Print("CHECKPOINT_RESTART_PASS "+stage);return main;
    }
    private async Task Capture(string label)
    {
        if(captureDirectory==null||!captured.Add(label))return;
        await Frames(3);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        System.IO.Directory.CreateDirectory(captureDirectory);using var pixels=GetViewport().GetTexture().GetImage();var path=System.IO.Path.Combine(captureDirectory,label+".png");
        if(pixels.IsEmpty()||pixels.SavePng(path)!=Error.Ok)throw new Exception("Screenshot save failed");GD.Print($"CAPTURE {label} {pixels.GetWidth()}x{pixels.GetHeight()} {path}");
    }
    private async Task WaitUntil(Func<bool> condition,string name)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&timer.Elapsed.TotalSeconds<5)await Frames();if(!condition())throw new Exception(name+" timed out");await Frames(2);
    }
    private async Task GameTime(double duration){double passed=0;var timer=System.Diagnostics.Stopwatch.StartNew();while(passed<duration&&timer.Elapsed.TotalSeconds<5){await Frames();passed+=GetProcessDeltaTime();}if(passed<duration)throw new Exception("Engine game time stalled");}
    private async Task RecoveryChecks()
    {
        var failures=new List<string>();void Verify(bool ok,string name){if(!ok)failures.Add(name);GD.Print((ok?"RECOVERY_PASS ":"RECOVERY_FAIL ")+name);}
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        var context=new GeXingzhou.Domain.SceneReturnContext("soup_shop",new(400,430),"soup.return",true);
        s.PendingRestore=GeXingzhou.Domain.SaveV2Codec.CreateNew(s.Options) with {Stage=GeXingzhou.Domain.SliceStage.MemoryActive,SceneId="memory_soup_table",MemoryState=new(),MemoryOrdinal=1,ReturnContext=context};
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        foreach(var broken in new[]{false,true}) {
            if(broken){var testRoot=ProjectSettings.GlobalizePath("res://test-output/");if(!s.SaveDirectory.StartsWith(testRoot))throw new Exception("Unsafe test directory");System.IO.Directory.CreateDirectory(s.SaveDirectory);System.IO.File.WriteAllText(System.IO.Path.Combine(s.SaveDirectory,"manual.json"),"{broken");}
            KeyPress(Key.F9);await Frames(2);var notice=main.GetNode<Label>("Notice");Verify(notice.Visible&&notice.ZIndex>main.Memory!.ZIndex&&!main.Dialogue.IsOpen,"Visible non-blocking memory F9 notice "+broken);
            Verify(s.Flow==GeXingzhou.Domain.FlowState.Memory,"Memory flow preserved "+broken);
        }
        var previousFocus=GetViewport().GuiGetFocusOwner();KeyPress(Key.Tab);await Frames(2);
        Verify(main.Phone.IsOpen&&main.Phone.ZIndex>main.Memory!.ZIndex,"Phone above memory");KeyPress(Key.Escape);await Frames(2);Verify(s.Flow==GeXingzhou.Domain.FlowState.Memory,"Phone flow recovery");Verify(previousFocus!=null&&GetViewport().GuiGetFocusOwner()==previousFocus,"Phone keyboard focus recovery");
        s.BeginMemory(context with {SceneId="missing_scene"},false);await main.ReturnMemory(false);await Frames(2);Verify(main.Dialogue.IsOpen,"Failed return notice");KeyPress(Key.Escape);await Frames(2);Verify(s.Flow==GeXingzhou.Domain.FlowState.Memory,"Failed return restores memory flow");s.Flow=GeXingzhou.Domain.FlowState.Memory;s.BeginMemory(context,false);
        main.Free();await Frames(2);
        Verify(s.Saves.Save(s.Snapshot).Success,"Save prepared for broken content");Verify(s.ManualSaves.PreserveForNewGame().Success&&s.ManualSaves.Save(s.Snapshot).Success,"Manual save prepared");
        typeof(GameSession).GetProperty(nameof(GameSession.Catalog))!.SetValue(s,null);typeof(GameSession).GetProperty(nameof(GameSession.ContentError))!.SetValue(s,"Required dialogue invalid");
        var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(2);
        var entries=boot.FindChildren("*","Button",true,false).OfType<Button>().Where(b=>b.Text.StartsWith("继续")||b.Text=="回到故乡").ToArray();Verify(entries.Length==3&&entries.All(b=>b.Disabled),"Broken content blocks all gameplay entries");boot.Free();await Frames(2);
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
    }
}
