using Godot;
using GeXingzhou.Domain;
public partial class InteractionController : Node
{
    public PlayerController Player {get;set;}=null!;
    public string Prompt {get;private set;}="";
    private InteractionPolicy policy=null!;private readonly Interaction2DPolicy depthPolicy=new();
    private string? selected;private double elapsed;
    private static Vector2 InteractionPoint(Interactable target)=>target is BenchView bench?bench.StandAnchor.GlobalPosition:target.GlobalPosition;
    public override void _Ready(){var p=GetNode<GameSession>("/root/GameSession").Catalog!.Parameters;policy=new((float)p["interact.radius"],(float)p["interact.hysteresis"],p["interact.repeat_guard"]);}
    private bool Reachable(WorldView world,Interactable target)
    {
        if(world.Mode==WorldMode.Depth2D){
            var p=world.ToLocal(Player.GlobalPosition);var t=world.ToLocal(InteractionPoint(target));
            if(world.SceneId=="community_gate"&&target is BenchView)
                return p.DistanceTo(t)<=48&&ShortApproachPolicy.TryPlan(world.Navigation,new(p.X,p.Y),new(t.X,t.Y))!=null;
            return NavigationGeometry.CanTraverse(world.Navigation,new(p.X,p.Y),new(t.X,t.Y));
        }
        using var query=PhysicsRayQueryParameters2D.Create(Player.GlobalPosition+new Vector2(0,-30),target.GlobalPosition+new Vector2(0,-30),1);
        return Player.GetWorld2D().DirectSpaceState.IntersectRay(query).Count==0;
    }
    public override void _Process(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        if(Player==null||session.Flow!=FlowState.Field){Prompt="";selected=null;return;}
        elapsed+=delta;var world=(WorldView)GetParent();
        var targets=world.GetTargets().Where(t=>t.IsInsideTree()&&t.IsActive&&t.IsVisibleInTree()).ToArray();
        if(world.Mode==WorldMode.Depth2D)
            selected=depthPolicy.Select(targets.Select(t=>{var p=world.ToLocal(InteractionPoint(t));return new Interaction2DCandidate(t.Id,new(p.X,p.Y),Reachable(world,t));}).ToArray(),selected,new(Player.Position.X,Player.Position.Y));
        else
            selected=policy.Select(targets.Select(t=>new InteractionCandidate(t.Id,t.GlobalPosition.X,Reachable(world,t))).ToArray(),selected,Player.GlobalPosition.X);
        Prompt=selected==null?(world.Mode==WorldMode.Depth2D?"WASD / 方向键探索 · Shift 加快":"A/D 移动 · Shift 加快"):"E · "+targets.First(t=>t.Id==selected).Caption;
    }
    public override void _UnhandledInput(InputEvent ev)
    {
        if(ev is InputEventKey{Pressed:true,Echo:false} key&&key.PhysicalKeycode==Key.E&&selected!=null)
        {
            var session=GetNode<GameSession>("/root/GameSession");var world=(WorldView)GetParent();
            var target=world.GetTargets().FirstOrDefault(t=>t.Id==selected&&t.IsActive&&t.IsVisibleInTree());
            if(target!=null&&session.Flow==FlowState.Field&&Reachable(world,target)&&
                (world.Mode==WorldMode.Depth2D?depthPolicy.TryActivate(selected,elapsed):policy.TryActivate(selected,elapsed)))
            {target.TryInteract(session);GetViewport().SetInputAsHandled();}
        }
    }
}
