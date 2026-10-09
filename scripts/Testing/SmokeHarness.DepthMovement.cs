using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task<MainView> NewDepthSoupMain(Position2 foot)
    {
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();
        Require(session.Restore(new(){Stage=SliceStage.CandyHeyDelivered}).Success,"Depth fixture stage rejected");
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        Require(main.ChangeWorld("soup_shop",foot),"Depth fixture map failed");await Frames(3);
        if(main.Dialogue.IsOpen)main.Dialogue.HandleKey(Key.Escape);session.Flow=FlowState.Field;
        return main;
    }
    private async Task DepthPhysics(int count){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);await Frames();}
    private async Task DepthMovementChecks()
    {
        using var saved=GD.Load<PackedScene>("res://scenes/world/SoupShop.tscn").Instantiate<WorldView>();
        Require(saved.Mode==WorldMode.Depth2D,"Soup must use Depth2D");
        var authored=NavigationSceneReader.Read(saved);
        var exported=NavigationCatalog.Parse(Godot.FileAccess.GetFileAsString("res://content/vs01/navigation.json"));
        Require(NavigationSceneReader.Matches(authored,exported.Profiles["soup_shop"]),"Offline navigation differs from saved nodes");
        foreach(var path in new[]{"DepthLayers/Actors/Player","DepthLayers/Actors/Cannon","DepthLayers/Props/Table","DepthLayers/Props/Chairs/LeftChair","DepthLayers/Props/Counter","DepthLayers/Props/Pot","Foreground/TableEdge","Navigation/GroundBoundary/CollisionPolygon2D"})
            Require(saved.GetNodeOrNull(path)!=null,"Depth layer missing "+path);
        saved.Free();
        var main=await NewDepthSoupMain(new(120,480));var player=main.World.Player;var session=GetNode<GameSession>("/root/GameSession");
        Require(!main.World.HasMeta("binding_error"),"Depth world binding failed");
        Require(main.World.Navigation.FootRadius==8,"Physics and save foot radius drifted");
        Require(InputMap.HasAction("move_up")&&InputMap.HasAction("move_down"),"Authored vertical actions missing");
        foreach(var (action,key) in new[]{("move_up",Key.Up),("move_down",Key.Down),("move_left",Key.Left),("move_right",Key.Right)})
            Require(InputMap.ActionHasEvent(action,new InputEventKey{PhysicalKeycode=key}),"Arrow action binding wrong "+action+" "+(int)key);
        using var cue=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[44100*6]};
        main.Audio.FootstepStream=cue;main.Audio.StopTransient();
        var before=player.Position;Input.ActionPress("move_up");await DepthPhysics(30);Input.ActionRelease("move_up");await DepthPhysics(15);
        Require(player.Position.Y<before.Y-10&&player.Facing=="back","No correctly facing vertical exploration");
        Require(main.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().Any(p=>p.Playing),"Vertical travel has no footsteps");
        Input.ActionPress("move_down");await DepthPhysics(30);Input.ActionRelease("move_down");await DepthPhysics(15);Require(player.Facing=="front"&&player.Position.Y>before.Y-10,"Downward walking missing");
        player.Position=new(350,480);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");Input.ActionPress("move_right");await DepthPhysics(10);
        Require(Math.Abs(player.Velocity.Length()-112)<.05,"Diagonal speed differs from walking speed");Input.ActionRelease("move_up");Input.ActionRelease("move_right");await DepthPhysics(15);
        player.Position=new(440,470);player.Velocity=Vector2.Zero;Input.ActionPress("move_up");Input.ActionPress("move_right");await DepthPhysics(50);
        Input.ActionRelease("move_up");Input.ActionRelease("move_right");await DepthPhysics(15);
        Require(NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Slide crossed table footprint");
        player.Position=new(450,430);player.Velocity=Vector2.Zero;Input.ActionPress("move_right");await DepthPhysics(60);Input.ActionRelease("move_right");await DepthPhysics(15);
        Require(player.Position.X<=452.02,"Player crossed whole table");Require(NavigationGeometry.CanStand(authored,new(player.Position.X,player.Position.Y)),"Physics position cannot be saved");
        main.Audio.StopTransient();await DepthPhysics(20);Require(main.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().All(p=>!p.Playing),"Stationary collision emits footsteps");
        player.Position=new(120,480);Input.ActionPress("move_right");await DepthPhysics(15);main.Phone.Open("messages");await DepthPhysics(10);
        var frozen=player.Position;main.Phone.Close();await DepthPhysics(15);Require(player.Position==frozen&&player.Velocity==Vector2.Zero,"Held key resumed after phone");
        Input.ActionRelease("move_right");await DepthPhysics(2);Input.ActionPress("move_right");await DepthPhysics(10);Input.ActionRelease("move_right");await DepthPhysics(15);Require(player.Position.X>frozen.X+5,"Released input never restored");
        Input.ActionPress("move_up");await DepthPhysics(4);player.Notification((int)Node.NotificationApplicationFocusOut);await DepthPhysics(8);var lost=player.Position;
        player.Notification((int)Node.NotificationApplicationFocusIn);await DepthPhysics(10);Require(player.Position==lost,"Focus return retained held input");Input.ActionRelease("move_up");await DepthPhysics(2);
        var art=player.GetNode<AnimatedSprite2D>("Artwork");var scale=art.Scale;player.Position=new(120,380);await DepthPhysics(2);Require(art.Scale==scale,"Depth changes character scale");
        var window=GetWindow();var oldSize=window.Size;var oldScale=window.ContentScaleMode;
        window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1440,1080)})
        {
            window.Size=size;await Frames(4);var display=main.GetNode<WorldDisplayController>("WorldDisplay");
            Require(main.World.ViewBounds.Grow(.02f).Encloses(display.VisibleWorldRect),"Depth camera leaves room");
            var backdrop=main.World.GetNode<Sprite2D>("Backdrop");
            Require(new Rect2(backdrop.Position,backdrop.Texture.GetSize()*backdrop.Scale).Grow(.02f).Encloses(display.VisibleWorldRect),"Depth camera leaves actual art");
            Require(Math.Abs(display.VisibleWorldRect.Size.X/display.VisibleWorldRect.Size.Y-main.Size.X/main.Size.Y)<.01,"Depth camera aspect drift");
            Require(art.Scale==scale,"Window aspect resized character");
        }
        window.Size=oldSize;window.ContentScaleMode=oldScale;await Frames(3);
        foreach(var scene in new[]{"community_gate","convenience_street"}){
            Require(main.ChangeWorld(scene,new(320,280)),"Legacy scene lost");await Frames(3);Require(main.World.Mode==WorldMode.Horizontal,"Legacy scene converted early");
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=true});await DepthPhysics(10);Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=false});Require(main.World.Player.Position.Y==280,"Horizontal Y drifted");
        }
        main.Free();await Frames(2);session.NewGame();
        var host=new Control();var error=new Label{Name="StartupError",Visible=false};host.AddChild(error);AddChild(host);
        var edited=GD.Load<PackedScene>("res://scenes/world/SoupShop.tscn").Instantiate<WorldView>();
        edited.GetNode<Node2D>("Navigation/Obstacles/Counter").Position=new(4,0);
        var shifted=NavigationSceneReader.Read(edited);
        Require(shifted.Obstacles[0][0].X==754,"Offline transforms ignored");
        host.AddChild(edited);await Frames(3);
        Require(edited.HasMeta("binding_error")&&error.Visible&&edited.ProcessMode==Node.ProcessModeEnum.Disabled,"Forgotten navigation export allowed interaction");
        host.Free();await Frames(2);
        GD.Print("DEPTH_MOVEMENT_PASS OfflineNavigation FourWay Sliding Footsteps Modal Focus LegacyModes Camera Mismatch");
    }
}
