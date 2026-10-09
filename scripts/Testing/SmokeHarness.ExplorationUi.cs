using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task CaptureExplorationChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");var main=await NewDepthSoupMain(new(500,480));
        var player=main.World.Player;var art=player.GetNode<AnimatedSprite2D>("Artwork");
        foreach(var (facing,action) in new[]{("front","move_down"),("back","move_up"),("side","move_right")})
        {
            player.Position=new(500,480);player.Velocity=Vector2.Zero;
            Input.ActionPress(action);await DepthPhysics(6);Input.ActionRelease(action);await DepthPhysics(12);
            Require(player.Facing==facing&&art.Animation=="idle_"+facing,"Depth facing capture does not show actual input result");
            await Capture("depth-"+facing);
        }
        player.Position=new(510,405);await Frames(3);await Capture("depth-table-occlusion");
        player.Position=new(500,480);await Frames(3);
        foreach(var (id,label) in new[]{("sign","sign"),("menu","menu"),("soup.note","note")})
        {
            var target=main.World.GetTargets().OfType<ObservationHotspot>().Single(t=>t.Id==id);
            Require(main.Observations.TryObserve(target),"Observation capture failed");await Frames(3);await Capture("observation-"+label);main.Dialogue.HandleKey(Key.Escape);await Frames(2);
        }
        var note=main.World.GetTargets().OfType<ObservationHotspot>().Single(t=>t.Id=="soup.note");
        var point=main.GetGlobalTransformWithCanvas()*main.WorldToUi(note.Shape.GlobalPosition);
        // ParseInputEvent takes native window pixels, then Godot applies content scaling.
        point*=new Vector2(GetWindow().Size.X,GetWindow().Size.Y)/GetViewport().GetVisibleRect().Size;
        using(var move=new InputEventMouseMotion{Position=point,GlobalPosition=point})Input.ParseInputEvent(move);
        await Frames(3);Require(main.GetNode<Control>("ObservationHover").Visible,"Mouse hover missing from actual capture");await Capture("observation-hover");
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(s.Options with{SubtitleSize=font,TextSpeed=0},false);
            main.Dialogue.ShowText("张大炮","先坐，汤马上好。");await Frames(3);AssertDialogueFits(main);await Capture("compact-short-"+font);main.Dialogue.HandleKey(Key.Escape);
        }
        main.Dialogue.ShowText("葛行舟",string.Join('\n',Enumerable.Repeat("我看着熟悉的店，却没有急着说什么。",24)),kind:DialogueLineKind.Thought);await Frames(3);await Capture("compact-long-32");main.Dialogue.HandleKey(Key.Escape);
        main.Free();await Frames(3);
    }
    private async Task ExplorationUiChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,SubtitleSize=24,ReducedMotion=true},false);
        var main=await NewDepthSoupMain(new(120,480));main.Dialogue.ShowText("张大炮","先坐，汤马上好。");await Frames(4);
        var panel=main.Dialogue.GetNode<PanelContainer>("Panel");
        Require(panel.Size.Y is >=120 and <=170,"Short dialogue still covers the room: "+panel.Size.Y);
        main.Dialogue.HandleKey(Key.Escape);
        Require(main.World.GetNodeOrNull<Interactable>("DepthLayers/Props/Sign")!=null,"No editor-visible observation sign");
        Require(!main.GetNode<Label>("HUD/TaskCard/TaskText").Text.Contains('\n'),"Goal HUD not folded by default");
        main.GetNode<Button>("HUD/TaskFold").EmitSignal(Button.SignalName.Pressed);await Frames(2);
        Require(main.GetNode<Label>("HUD/TaskCard/TaskText").Text.Contains('\n'),"Goal detail never expands");main.Free();await Frames(2);
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(1440,1080)})foreach(var font in new[]{20,24,32})
        {
            window.Size=size;await Frames(3);s.SetOptions(new(){TextSpeed=0,SubtitleSize=font,ReducedMotion=true,RecordEventsEnabled=true},false);
            main=await NewDepthSoupMain(new(400,480));var facts=s.Snapshot.CompletedActions.ToArray();var stage=s.Snapshot.Stage;
            main.Dialogue.ShowText("张大炮","先坐，汤马上好。");await Frames(3);AssertDialogueFits(main);
            Require(main.Dialogue.GetNode<Label>("Panel/Content/Body/Text").GetThemeFontSize("font_size")==font,"Subtitle silently shrunk");
            if(font==24)Require(main.Dialogue.GetNode<PanelContainer>("Panel").Size.Y is >=120 and <=170,"Short default subtitle height regressed");main.Dialogue.HandleKey(Key.Escape);
            main.Choices.Open("张大炮：最近怎么样？",new (string,Action)[]{("先吃一口汤",()=>{}),("替他摆好筷子",()=>{}),("看一眼旧手机",()=>{})});await Frames(3);
            foreach(var button in main.Choices.GetNode<VBoxContainer>("Panel/Content/Options").GetChildren().OfType<Button>())Require(main.GetGlobalRect().Encloses(button.GetGlobalRect())&&button.FocusMode==Control.FocusModeEnum.All,"Choice clipped/unreachable");
            KeyPress(Key.Down);await Frames(2);Require(GetViewport().GuiGetFocusOwner() is Button,"Choice lost keyboard focus");main.Choices.Close();
            var hotspots=main.World.GetTargets().OfType<ObservationHotspot>().OrderBy(t=>t.Id).ToArray();Require(hotspots.Length==3,"Missing optional discoveries");
            foreach(var target in hotspots)
            {
                main.World.Player.Position=new(500,480);await Frames(4);
                var visual=target.GetNode<Node2D>(target.VisualTarget);var original=visual.Position;var center=target.Shape.GlobalPosition;
                Require(main.GetNode<WorldDisplayController>("WorldDisplay").VisibleWorldRect.HasPoint(main.World.ToLocal(center)),"Mouse fixture object outside camera");
                var point=main.GetGlobalTransformWithCanvas()*main.WorldToUi(center);
                // Break: applying window pixels directly to a camera-scrolled SubViewport hits the wrong object.
                ClickExploration(point);await Frames(3);Require(main.Dialogue.IsOpen&&main.Dialogue.GetNode<Label>("Panel/Content/NameLabel").Text.StartsWith(target.Caption),"Camera-transformed mouse missed "+target.Id+" "+size);
                KeyPress(Key.Escape);await Frames(2);Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Cancel consumed discovery");
                var originalBody=target.Description;target.Description=string.Join('\n',Enumerable.Repeat("我看着这个熟悉的物件，却没有急着说什么。",24));
                Require(main.Observations.TryObserve(target),"Long observe rejected");await Frames(3);var scroll=main.Dialogue.GetNode<ScrollContainer>("Panel/Content/Body");
                KeyPress(Key.Pagedown);await Frames(3);Require(scroll.ScrollVertical>0&&main.Dialogue.IsOpen,"Keyboard scrolling advanced/failed");
                scroll.ScrollVertical=0;var wheelPoint=scroll.GetGlobalRect().GetCenter();
                using(var wheel=new InputEventMouseButton{Position=wheelPoint,GlobalPosition=wheelPoint,ButtonIndex=MouseButton.WheelDown,Pressed=true})Input.ParseInputEvent(wheel);
                await Frames(3);Require(scroll.ScrollVertical>0&&main.Dialogue.IsOpen,"Mouse-wheel scrolling advanced/failed");
                KeyPress(Key.Escape);await Frames(2);Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Long Esc consumed discovery");target.Description=originalBody;
                var pause=main.GetNode<Button>("HUD/PauseButton");var uiPoint=pause.GetGlobalRect().GetCenter();visual.GlobalPosition=main.UiToWorld(uiPoint);
                ClickExploration(uiPoint);await Frames(2);Require(!main.Dialogue.IsOpen,"UI click passed through to observation");if(main.Pause.IsOpen)main.Pause.Close();visual.Position=original;await Frames(2);
                center=target.Shape.GlobalPosition;point=main.GetGlobalTransformWithCanvas()*main.WorldToUi(center);
                var blocker=new Polygon2D{Polygon=new[]{new Vector2(-70,-50),new Vector2(70,-50),new Vector2(70,50),new Vector2(-70,50)},Position=main.World.ToLocal(center)};
                main.World.GetNode<Node2D>("Foreground").AddChild(blocker);await Frames(2);ClickExploration(point);await Frames(2);Require(!main.Dialogue.IsOpen,"Foreground-covered object accepted click");blocker.Free();await Frames(2);
                main.World.Player.Position=main.World.ToLocal(target.GlobalPosition);await Frames(3);
                KeyPress(Key.E);ClickExploration(point);await Frames(2);Require(main.Dialogue.IsOpen,"Shared E entry missed "+target.Id);Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Same-frame click/E completed unread observation");
                await Finish(main);Require(s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Complete observation not recorded");
                var savePath=System.IO.Path.Combine(s.SaveDirectory,"save.json");var bytes=System.IO.File.ReadAllBytes(savePath);Require(s.Saves.Load().Snapshot!.DiscoveredIds.Contains(target.DiscoveryId),"Discovery not persisted");
                Require(main.Observations.TryObserve(target),"Repeat observe rejected");await Finish(main);Require(bytes.SequenceEqual(System.IO.File.ReadAllBytes(savePath)),"Repeat observation wrote checkpoint again");
            }
            Require(s.Snapshot.Stage==stage&&s.Snapshot.CompletedActions.SetEquals(facts)&&s.Snapshot.ChoiceCodes.Count==0,"Observation mutated story facts");
            var log=System.IO.Path.Combine(s.SaveDirectory,"behavior","events.jsonl");Require(!System.IO.File.Exists(log)||System.IO.File.ReadAllLines(log).Length==0,"Optional discoveries leaked behavior events");
            main.World.Player.Position=new(400,480);await Frames(3);KeyPress(Key.E);await Frames(2);Require(!main.SoupSeat.IsActive&&!main.Dialogue.IsOpen,"E activated seat beyond depth radius");
            main.World.Player.Position=new(400,430);await Frames(3);Require(main.SoupSeat.Begin(()=>{}),"Subtitle safety seat seed");await WaitUntil(()=>!main.SoupSeat.IsActing,"Subtitle safety seated");
            main.Dialogue.ShowText("张大炮","先坐，汤马上好。");await Frames(3);
            var faceA=main.WorldToUi(main.World.ToGlobal(new Vector2(408,326)));var faceB=main.WorldToUi(main.World.ToGlobal(new Vector2(604,410)));
            Require(!main.Dialogue.GetNode<PanelContainer>("Panel").GetRect().Intersects(new Rect2(faceA,faceB-faceA).Abs()),"Subtitle covers seated faces/hands");
            main.Dialogue.HandleKey(Key.Escape);main.SoupSeat.Cancel();
            main.Free();await Frames(2);
        }
        window.Size=oldSize;window.ContentScaleMode=oldMode;
        GD.Print("EXPLORATION_UI_PASS Compact Goal Discoveries Cancel Repeat ThreeFonts ThreeWindows MouseCamera UiBlock Foreground Keyboard Scroll NoStoryEvents");
    }
    private void ClickExploration(Vector2 point)
    {
        using var motion=new InputEventMouseMotion{Position=point,GlobalPosition=point};Input.ParseInputEvent(motion);
        using var down=new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true};Input.ParseInputEvent(down);
        using var up=new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false};Input.ParseInputEvent(up);
    }
}
