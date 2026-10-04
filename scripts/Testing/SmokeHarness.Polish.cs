using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task ResponsiveUiChecks()
    {
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;
        window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(1440,1080)})
        {
            window.Size=size;await Frames(3);var main=await NewPolishMain();
            foreach(var id in new[]{"community_gate","convenience_street","soup_shop"})
            {
                if(main.World.SceneId!=id)Require(main.ChangeWorld(id,new(320,280)),"Map change failed");await Frames(3);
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
        var saved=new (string File,string[] Paths)[]{
            ("Boot",new[]{"Background","Menu/Title","Menu/StartButton","Menu/AutoResumeButton","Menu/ManualResumeButton","Menu/RecoveryButton","Menu/SettingsButton","Menu/QuitButton","StartupError","Choices","Settings"}),
            ("Main",new[]{"WorldDisplay/WorldViewport","HUD/TaskCard","HUD/InteractionHint","Phone","Dialogue","Choices","Settings","SceneFlow","TransitionOverlay","StartupError"}),
            ("ui/Phone",new[]{"Frame/Content/Contact","Frame/Content/Messages","Frame/Content/Task","Frame/Content/AnswerButton","Frame/Content/CloseButton"}),
            ("ui/Dialogue",new[]{"Panel/Content/NameLabel","Panel/Content/Body","Panel/Content/ContinueHint","Portrait"}),
            ("ui/Choices",new[]{"Panel/Content/Title","Panel/Content/Options","Panel/Content/ReturnHint"}),
            ("ui/Settings",new[]{"Panel/Scroll/Content/FontSize","Panel/Scroll/Content/TextSpeed","Panel/Scroll/Content/Assistance","Panel/Scroll/Content/ReducedMotion","Panel/Scroll/Content/RecordEvents","Panel/Scroll/Content/ClearButton","Panel/Scroll/Content/ExportButton","Panel/Scroll/Content/CloseButton"}),
            ("world/MemorySoupTable",new[]{"Background","SoupBowl","Panel/Content/Status","Panel/Content/Coins/Coin1","Panel/Content/Coins/Coin4","Panel/Content/Foods/Take","Panel/Content/Foods/Wait","Panel/Content/Foods/Share","Panel/Content/Hint"})};
        foreach(var (file,paths) in saved)
        {
            Require(ResourceLoader.Exists($"res://scenes/{file}.tscn"),"OfflineUiTree missing scene "+file);
            var node=GD.Load<PackedScene>($"res://scenes/{file}.tscn").Instantiate();
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
            ("SoupShop",new[]{"StreetExit","Seat","Sign","Menu","Counter","Shopkeeper","SoupBowl"}) })
        {
            var world=GD.Load<PackedScene>($"res://scenes/world/{file}.tscn").Instantiate<WorldView>();
            try
            {
                foreach(var path in paths.Concat(new[]{"Backdrop","Player/Artwork","Player/Camera2D","Player/CollisionShape2D","Interactions","Floor/CollisionShape2D","LeftBoundary/CollisionShape2D","RightBoundary/CollisionShape2D"}))
                    Require(world.GetNodeOrNull(path)!=null,$"SavedWorldNodes: {file}/{path} not editable offline");
                var before=world.GetNode<PlayerController>("Player");
                var backdrop=world.GetNode<Sprite2D>("Backdrop");var authored=backdrop.Position+new Vector2(0,2);backdrop.Position=authored;
                if(file=="CommunityGate")world.GetNode<Node2D>("Bench").Position=new Vector2(940,280);
                GetNode<GameSession>("/root/GameSession").NewGame();AddChild(world);await Frames(3);
                Require(ReferenceEquals(before,world.Player),"InspectorChangesSurviveReady: player rebuilt");
                Require(backdrop.Position==authored,"InspectorChangesSurviveReady: background reset");
                var ids=world.GetChildren().OfType<Interactable>().Select(t=>t.Id).ToArray();
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
        foreach(var invalid in new[]{"missing_artwork","duplicate_id"})
        {
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
            var host=new Control();var error=new Label{Name="StartupError",Visible=false};host.AddChild(error);AddChild(host);
            var bad=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();
            if(invalid=="missing_artwork")bad.GetNode("Player/Artwork").Free();
            else bad.GetNode<Interactable>("StreetExit").Id="old_sign";
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
