using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task FinalBoundariesChecks()
    {
        var selected=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--boundary-case="))?.Split('=',2)[1];
        var checks=new (string Name,Func<Task> Run)[]{
            ("MemorySave",MemoryManualSaveBoundary), ("PhoneFailure",PhoneFailureBoundary),
            ("DialogueFailure",DialogueFailureBoundary), ("PhysicsAuthoring",PhysicsAuthoringBoundary),
            ("MemoryCandidate",MemoryCandidateBoundary), ("EntranceFailure",EntranceFailureBoundary),
            ("BootRecovery",BootRecoveryBoundary), ("SeatFocus",SeatFocusBoundary)};
        Require(selected==null||checks.Any(c=>c.Name==selected),"Unknown boundary case");
        foreach(var check in checks.Where(c=>selected==null||c.Name==selected))
        {
            var directory=GetNode<GameSession>("/root/GameSession").SaveDirectory;
            Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/"),StringComparison.OrdinalIgnoreCase),"Boundary cleanup outside test fixtures");
            foreach(var file in new[]{"save.json","save.bak.json","manual.json","manual.bak.json"})
            {var path=System.IO.Path.Combine(directory,file);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
            await check.Run();GD.Print("FINAL_BOUNDARY_PASS "+check.Name);
        }
    }
    private async Task MemoryManualSaveBoundary()
    {
        var s=GetNode<GameSession>("/root/GameSession");var seed=await NewCoinTable(1);var snapshot=s.Snapshot;seed.Free();await Frames(2);
        s.PendingRestore=snapshot;var main=s.GetScene("res://scenes/Main.tscn")!.Instantiate<MainView>();AddChild(main);await Frames(4);
        var context=s.Snapshot.ReturnContext;Require(main.Memory!=null,"Memory F5 fixture not in actual Main");
        KeyPress(Key.F5);await Frames(3);var manual=s.ManualSaves.Load();
        Require(manual.Status==LoadStatus.Loaded&&manual.Snapshot!.PlayerPosition==new Position2(320,280),"Memory F5 overwrote virtual coordinates or failed save");
        Require(s.Snapshot.ReturnContext==context,"Memory F5 changed return context");
        Require(main.Memory!.TryPush(1),"Memory coin after F5 rejected");await Frames(18);
        var checkpoint=s.Saves.Load();Require(checkpoint.Status==LoadStatus.Loaded&&checkpoint.Snapshot!.MemoryState!.PushedCoinIds.SetEquals(new[]{"c1","c2"}),"Checkpoint after memory F5 failed");
        GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(8);var loaded=GetTree().CurrentScene as MainView;
        Require(loaded!=null&&loaded.Memory!=null&&s.Snapshot.PlayerPosition==new Position2(320,280)&&s.Snapshot.MemoryState!.PushedCoinIds.SetEquals(new[]{"c1"})&&s.Snapshot.ReturnContext==context,"Memory F9 failed round trip");
        GetTree().CurrentScene=null;loaded!.Free();main.Free();await Frames(3);
    }
    private async Task PhoneFailureBoundary()
    {
        var main=await NewDepthSoupMain(new(120,480));var s=GetNode<GameSession>("/root/GameSession");var world=main.World;var before=s.Snapshot;
        main.Phone.Open("messages");var focus=GetViewport().GuiGetFocusOwner();KeyPress(Key.F9);await Frames(3);
        Require(main.Phone.IsOpen&&!main.Dialogue.IsOpen&&s.Flow==FlowState.Phone&&GetViewport().GuiGetFocusOwner()==focus,"Failed F9 replaced phone flow/focus with error dialogue");
        Require(ReferenceEquals(main.World,world)&&s.Snapshot.PlaythroughId==before.PlaythroughId,"Failed F9 replaced world/progress");
        KeyPress(Key.Escape);await Frames(3);Require(!main.Phone.IsOpen&&!main.Dialogue.IsOpen&&s.Flow==FlowState.Field,"Failed phone F9 left ghost modal flow");
        var foot=main.World.Player.Position;Input.ActionPress("move_right");await DepthPhysics(12);Input.ActionRelease("move_right");await DepthPhysics(12);
        Require(main.World.Player.Position.X>foot.X+5,"Failed phone F9 stranded movement");main.Free();await Frames(3);
    }
    private async Task DialogueFailureBoundary()
    {
        var main=await NewDepthSoupMain(new(120,480));var s=GetNode<GameSession>("/root/GameSession");int done=0;
        Require(main.Dialogue.Open("soup.tomorrow",()=>done++),"Original dialogue missing");await GameTime(.17);
        var label=main.Dialogue.GetNode<Label>("Panel/Content/Body/Text");var text=label.Text;KeyPress(Key.F9);await Frames(3);
        Require(s.Flow==FlowState.Dialogue&&label.Text==text,"Failed F9 replaced original dialogue");
        await Finish(main);Require(done==1&&s.Flow==FlowState.Field,"Failed F9 lost original completion callback");main.Free();await Frames(3);
    }
    private async Task PhysicsAuthoringBoundary()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        var edits=new (string Name,Action<WorldView> Edit)[]{
            ("disabled boundary",w=>w.GetNode<CollisionPolygon2D>("Navigation/GroundBoundary/CollisionPolygon2D").Disabled=true),
            ("boundary layer",w=>w.GetNode<StaticBody2D>("Navigation/GroundBoundary").CollisionLayer=0),
            ("player mask",w=>w.GetNode<PlayerController>("DepthLayers/Actors/Player").CollisionMask=0),
            ("player scale",w=>w.GetNode<PlayerController>("DepthLayers/Actors/Player").Scale=new(2,2)),
            ("actors scale",w=>w.GetNode<Node2D>("DepthLayers/Actors").Scale=new(2,2)),
            ("actors offset",w=>w.GetNode<Node2D>("DepthLayers/Actors").Position=new(10,0)),
            ("depth layer rotation",w=>w.GetNode<Node2D>("DepthLayers").Rotation=.2f)};
        foreach(var edit in edits)
        {
            var world=s.GetScene("res://scenes/world/SoupShop.tscn")!.Instantiate<WorldView>();edit.Edit(world);bool rejected=false;
            try{NavigationSceneReader.Read(world);}catch(InvalidOperationException){rejected=true;}finally{world.Free();}
            Require(rejected,"Invalid physics authoring accepted: "+edit.Name);
        }
        var main=await NewDepthSoupMain(new(120,480));
        foreach(var id in new[]{"community_gate","convenience_street"})Require(main.ChangeWorld(id,s.Navigation.Profiles[id].Anchors["safe"]),"Physics rejection broke scene");
        main.Free();await Frames(3);
    }
    private async Task MemoryCandidateBoundary()
    {
        var s=GetNode<GameSession>("/root/GameSession");var seed=await NewCoinTable(1);Require(s.SaveManual().Success,"Memory candidate save fixture failed");seed.Free();await Frames(3);
        var main=await NewDepthSoupMain(new(120,480));main.Phone.Open("messages");var world=main.World;var before=s.Snapshot;
        var packed=s.GetScene("res://scenes/world/MemorySoupTable.tscn")!;var pristine=packed.Instantiate<SoupMemoryController>();var damaged=packed.Instantiate<SoupMemoryController>();
        damaged.GetNode("Table/Coin1").Free();Require(packed.Pack(damaged)==Error.Ok,"Damaged memory fixture did not pack");
        try
        {
            GetTree().CurrentScene=null;
            KeyPress(Key.F9);await Frames(8);
            Require(main.IsInsideTree()&&ReferenceEquals(main.World,world)&&main.Phone.IsOpen&&s.Flow==FlowState.Phone&&s.PendingRestore==null&&s.Snapshot.PlaythroughId==before.PlaythroughId,"Invalid memory candidate destroyed usable session");
        }
        finally{Require(packed.Pack(pristine)==Error.Ok,"Memory fixture restore failed");pristine.Free();damaged.Free();}
        main.Phone.Close();main.Free();await Frames(3);
    }
    private async Task EntranceFailureBoundary()
    {
        var main=await NewDepthSoupMain(new(120,480));var s=GetNode<GameSession>("/root/GameSession");
        Require(main.ChangeWorld("convenience_street",new(1120,280)),"Entrance fixture failed");await Frames(3);var world=main.World;
        var packed=s.GetScene("res://scenes/world/SoupShop.tscn")!;var pristine=packed.Instantiate<WorldView>();var damaged=packed.Instantiate<WorldView>();
        damaged.GetNode<Node2D>("Navigation/Obstacles/Counter").Position=new(4,0);Require(packed.Pack(damaged)==Error.Ok,"Navigation mismatch fixture did not pack");
        try
        {
            KeyPress(Key.E);await Frames(12);
            var hud=main.GetNode<Label>("Notice").Text;
            Require(ReferenceEquals(main.World,world)&&s.Flow==FlowState.Field&&hud.Contains("导航"),"Entrance failure did not preserve world and diagnose navigation");
            Require(!main.Phone.IsOpen&&!main.Dialogue.IsOpen,"Entrance failure opened another blocking session");
        }
        finally{Require(packed.Pack(pristine)==Error.Ok,"Navigation fixture restore failed");pristine.Free();damaged.Free();}
        main.Free();await Frames(3);
    }
    private async Task BootRecoveryBoundary()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();s.SetOptions(new(){SubtitleSize=32,TextSpeed=0},false);
        Require(s.Saves.Save(s.Snapshot).Success&&s.ManualSaves.Save(s.Snapshot).Success,"Recovery fixture save failed");
        foreach(var name in new[]{"save","manual"})
        {
            var file=System.IO.Path.Combine(s.SaveDirectory,name+".json");System.IO.File.Copy(file,System.IO.Path.Combine(s.SaveDirectory,name+".bak.json"),true);System.IO.File.WriteAllText(file,"{broken");
        }
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        try
        {
            foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1440,1080),new Vector2I(1920,1080)})
            {
                window.Size=size;var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(4);
                var menu=boot.FindChildren("Menu","VBoxContainer",true,false).OfType<VBoxContainer>().Single();
                foreach(var name in new[]{"StartButton","RecoveryButton","ManualRecoveryButton","SettingsButton","QuitButton"})
                {
                    var button=menu.GetNode<Button>(name);Require(button.Visible&&!button.Disabled,"Recovery action unavailable "+name);button.GrabFocus();await Frames(3);
                    Require(boot.GetGlobalRect().Encloses(button.GetGlobalRect()),"Large-font recovery menu clips keyboard action "+name+" "+size+" "+button.GetGlobalRect());
                }
                var message=menu.GetNode<Label>("Message");
                if(boot.GetNodeOrNull<ScrollContainer>("MenuScroll") is {} scroll)
                {
                    var point=scroll.GetGlobalRect().GetCenter();
                    using(var motion=new InputEventMouseMotion{Position=point,GlobalPosition=point})Input.ParseInputEvent(motion);
                    for(int i=0;i<30;i++)
                    {using var wheel=new InputEventMouseButton{Position=point,GlobalPosition=point,Pressed=true,ButtonIndex=MouseButton.WheelDown};Input.ParseInputEvent(wheel);}
                    await Frames(3);
                    Require(scroll.GetGlobalRect().Encloses(message.GetGlobalRect()),"Recovery error remains outside actual scroll viewport");
                }
                Require(boot.GetGlobalRect().Encloses(message.GetGlobalRect()),"Large-font recovery error text clipped "+size+" "+message.GetGlobalRect());
                boot.Free();await Frames(3);
            }
        }
        finally{window.Size=oldSize;window.ContentScaleMode=oldMode;}
    }
    private async Task SeatFocusBoundary()
    {
        var main=await NewDepthSoupMain(new(380,430));int done=0;Require(main.SoupSeat.Begin(()=>done++),"Focus approach fixture failed");await DepthPhysics(2);
        main.PropagateNotification((int)Node.NotificationApplicationFocusOut);var foot=main.World.Player.Position;await DepthPhysics(30);
        Require(main.World.Player.Position==foot&&main.SoupSeat.IsApproaching&&done==0,"Focus loss continued approach and story callback");
        main.PropagateNotification((int)Node.NotificationApplicationFocusIn);await WaitUntil(()=>done==1,"Focus approach resume");
        Require(!main.SoupSeat.IsActing&&done==1,"Focus return repeated/missed seated callback");main.SoupSeat.Cancel();main.Free();await Frames(3);
    }
}
