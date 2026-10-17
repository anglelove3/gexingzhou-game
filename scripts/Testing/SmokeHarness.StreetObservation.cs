using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private static readonly string[] StreetTargets={"doorplate","newsstand","bike_shed","drain","wall_paint","laundry_rack","stool"};
    private async Task StreetObservationChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        var window=GetWindow();var oldMode=window.ContentScaleMode;var oldSize=window.Size;
        window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;if(captureDirectory==null)window.Size=new(1280,720);await Frames(3);
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();
            try
            {
                Require(main.ChangeWorld("convenience_street",new(240,520)),"Street observation entry rejected");await Frames(3);
                foreach(var id in StreetTargets)
                {
                    var target=(ObservationHotspot)main.World.GetTarget(id)!;
                    Require(target.IsActive&&target.DiscoveryId=="street."+id,"Inactive or placeholder discovery "+id);
                    Require(!string.IsNullOrWhiteSpace(target.Description),"Missing approved street copy "+id);
                    Require(NavigationGeometry.CanStand(main.World.Navigation,new(target.Position.X,target.Position.Y)),"Hotspot foot not reachable "+id);
                    main.World.Player.Position=target.Position;await Frames(4);
                    var point=main.GetGlobalTransformWithCanvas()*main.WorldToUi(target.Shape.GlobalPosition);
                    Require(target.ContainsWorldPoint(target.Shape.GlobalPosition),"Street shape miss "+id);
                    ClickExploration(point);await Frames(3);Require(main.Dialogue.IsOpen,"Street mouse/camera miss "+id+" point="+point+" flow="+s.Flow+" blockers="+string.Join(',',main.FindChildren("*","",true,false).OfType<Control>().Where(c=>c.GetViewport()==main.GetViewport()&&c.IsVisibleInTree()&&c.MouseFilter==Control.MouseFilterEnum.Stop&&c.GetGlobalRect().HasPoint(point)).Select(c=>c.GetPath())));
                    KeyPress(Key.Escape);await Frames(2);Require(!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Cancel recorded "+id);
                    // Real click occlusion: author a test-only opaque/transparent foreground over the shape.
                    var image=Image.CreateEmpty(80,80,false,Image.Format.Rgba8);image.Fill(Colors.White);
                    var foreground=new Sprite2D{Texture=ImageTexture.CreateFromImage(image),Position=target.Shape.GlobalPosition};
                    main.World.GetNode<Node2D>("Foreground").AddChild(foreground);await Frames(2);
                    ClickExploration(point);await Frames(2);Require(!main.Dialogue.IsOpen,"Opaque foreground click leaked "+id);
                    image.Fill(new Color(1,1,1,0));foreground.Texture=ImageTexture.CreateFromImage(image);await Frames(2);
                    ClickExploration(point);await Frames(2);Require(main.Dialogue.IsOpen,"Transparent foreground blocked "+id);
                    KeyPress(Key.Escape);foreground.Free();image.Dispose();await Frames(2);
                    // Deliver to the real world controller, without activating legitimate menu buttons.
                    using var modalClick=new InputEventMouseButton{Position=point,ButtonIndex=MouseButton.Left,Pressed=true};
                    main.Pause.Open();main.Observations._Input(modalClick);await Frames(2);Require(!main.Dialogue.IsOpen,"Pause click leaked");main.Pause.Close();
                    Require(main.Journal.Open(),"Journal fixture unavailable");main.Observations._Input(modalClick);await Frames(2);Require(!main.Dialogue.IsOpen,"Journal click leaked");main.Journal.Close();
                    s.Flow=FlowState.Phone;Require(!main.Observations.TryObserve(target),"Phone allowed observation");
                    s.Flow=FlowState.Transition;Require(!main.Observations.TryObserve(target),"Transition allowed observation");s.Flow=FlowState.Field;
                    Require(s.SaveCheckpoint().Success,"Street isolated save seed failed");
                    var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;var choices=s.Snapshot.ChoiceCodes.ToArray();
                    KeyPress(Key.E);ClickExploration(point);await Frames(3);
                    Require(main.Dialogue.IsOpen&&!s.Snapshot.DiscoveredIds.Contains(target.DiscoveryId),"Same-frame input finished unread street prop "+id);
                    await Finish(main);
                    var saved=s.Saves.Load().Snapshot!;Require(saved.DiscoveredIds.Contains(target.DiscoveryId),"Completed read missing from save");
                    Require(!s.Saves.LoadBackup().Snapshot!.DiscoveredIds.Contains(target.DiscoveryId),"Observation wrote multiple checkpoints");
                    Require(saved.PlayerPosition==new Position2(target.Position.X,target.Position.Y),"Saved stale observation foot");
                    var path=System.IO.Path.Combine(s.SaveDirectory,"save.json");var bytes=System.IO.File.ReadAllBytes(path);
                    Require(main.Observations.TryObserve(target),"Repeat observation unavailable");await Finish(main);
                    Require(bytes.SequenceEqual(System.IO.File.ReadAllBytes(path)),"Repeat read rewrote checkpoint");
                    Require(s.Snapshot.Stage==stage&&s.Snapshot.CandyCount==candy&&choices.SequenceEqual(s.Snapshot.ChoiceCodes),"Optional read changed story");
                    await Capture("street-observe-"+id+"-"+font);
                }
                Require(s.Snapshot.DiscoveredIds.Count==7&&s.Snapshot.CompletedActions.Count==0,"Street props added main facts");
                Require(main.Journal.Open(),"Street journal failed");main.Journal.ShowPage(JournalPage.Discoveries);await Frames(3);
                Require(main.Journal.GetNode<Label>(JournalText).Text.Contains("便利店街"),"Street journal wrong location");
                main.Journal.Close();
            }
            finally{main.Free();await Frames(3);}
        }
        GD.Print("STREET_OBSERVATION_PASS Seven Mouse E Alpha Modal Cancel Checkpoint Repeat ThreeFonts StoryPreserved");
        window.Size=oldSize;window.ContentScaleMode=oldMode;
    }
}
