using Godot;
public partial class SmokeHarness : Node
{
    public override async void _Ready()
    {
        try
        {
            var suite=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--suite="))?.Split('=')[1] ?? "Movement";
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
}
