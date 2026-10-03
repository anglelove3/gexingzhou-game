using Godot;
public partial class SmokeHarness : Node
{
    public override async void _Ready()
    {
        try
        {
            var suite=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--suite="))?.Split('=')[1] ?? "Movement";
            if(suite=="Soup")
            {
                foreach(var (code,index) in new[]{("eat",0),("set_chopsticks",1),("check_phone",2)})await SoupPath(code,index);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Soup");GetTree().Quit();return;
            }
            if(suite is "Invitation" or "Hey")
            {
                foreach(var answer in new[]{false,true})await StoryPath(answer,suite);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
                GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();return;
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
            GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();
        }
        catch(Exception ex){GD.PrintErr("GODOT_CHECKS_FAIL "+ex.Message);GetTree().Quit(1);}
    }
    private async Task Frames(int count=1){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private void KeyPress(Key key){using var down=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true};using var up=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false};Input.ParseInputEvent(down);Input.ParseInputEvent(up);}
    private async Task Finish(MainView main)
    {
        for(int i=0;i<40&&main.Dialogue.IsOpen;i++){await Frames(12);if(!main.Dialogue.IsOpen)break;KeyPress(Key.E);await Frames(2);}
        if(main.Dialogue.IsOpen)throw new Exception("Dialogue never finished with keyboard");
    }
    private async Task StoryPath(bool answer,string suite)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        s.AdvanceClock(35);KeyPress(Key.Tab);await Frames(2);
        if(!main.Phone.IsOpen)throw new Exception("Keyboard did not open phone");
        var time=s.Snapshot.InvitationState.Elapsed;s.AdvanceClock(100);
        if(s.Snapshot.InvitationState.Elapsed!=time)throw new Exception("Phone did not freeze invitation");
        if(answer){KeyPress(Key.Enter);await Frames(2);if(s.Snapshot.InvitationState.Resolution!=GeXingzhou.Domain.InvitationResolution.Answered)throw new Exception("Phone answer button not keyboard accessible");await Finish(main);}
        else {KeyPress(Key.Escape);await Frames(2);}
        if(s.Snapshot.CandyCount!=0)throw new Exception("Phone gave candy early");
        s.AdvanceClock(30);await Frames(2);
        var cannon=main.World.GetNodeOrNull<Interactable>("Cannon");
        if(cannon==null)throw new Exception($"Cannon missing: elapsed={s.Snapshot.InvitationState.Elapsed} arrived={s.Snapshot.InvitationState.CarArrived} flow={s.Flow} stage={s.Snapshot.Stage} scene={main.World.SceneId}");
        if(!cannon.TryInteract(s))throw new Exception("Cannot meet cannon");await Finish(main);
        if(s.Snapshot.CandyCount!=1)throw new Exception("Meeting did not give one candy");
        if(suite=="Hey")
        {
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            main.ChangeWorld("community_gate",new(320,280));await Frames(2);
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            var hey=main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="hey");hey.TryInteract(s);await Finish(main);
            if(s.Snapshot.CandyCount!=0||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.CandyHeyDelivered)throw new Exception("Hey delivery failed");
            await Choose(main,0);await Finish(main);
            hey.TryInteract(s);await Choose(main,1);await Finish(main);if(s.Snapshot.CandyCount!=0)throw new Exception("Repeated delivery changed candy");
        }
        GD.Print("STORY_PATH_PASS "+(answer?"answered":"ignored")+" "+suite);main.Free();await Frames();
    }
    private async Task Choose(MainView main,int index)
    {
        if(!main.Choices.IsOpen)throw new Exception("Expected keyboard choice");
        await Frames(2);for(int i=0;i<index;i++){KeyPress(Key.Down);await Frames(2);}KeyPress(Key.Enter);await Frames(2);
        if(main.Choices.IsOpen)throw new Exception("Keyboard choice did not close");
    }
    private async Task SoupPath(string code,int index)
    {
        await StoryPath(false,"Hey");var s=GetNode<GameSession>("/root/GameSession");
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        main.ChangeWorld("soup_shop",new(440,280));await Frames(2);
        main.ChangeWorld("convenience_street",new(1120,280));main.ChangeWorld("soup_shop",new(440,280));await Frames(2);
        main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);await Finish(main);await Choose(main,index);await Finish(main);
        if(s.Snapshot.ChoiceCodes["soup-response-1"]!=code||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SoupMeet)throw new Exception("Wrong soup route: want="+code+" got="+s.Snapshot.ChoiceCodes["soup-response-1"]);
        var stage=s.Snapshot.Stage;main.ShowDialogue("missing_node");await Frames(2);KeyPress(Key.Escape);await Frames(2);
        if(s.Flow!=GeXingzhou.Domain.FlowState.Field||s.Snapshot.Stage!=stage)throw new Exception("Missing node locked or changed story");
        GD.Print("SOUP_PATH_PASS "+code);main.Free();await Frames();
    }
}
