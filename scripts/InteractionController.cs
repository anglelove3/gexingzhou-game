using Godot;
using GeXingzhou.Domain;
public partial class InteractionController : Node
{
    public PlayerController Player {get;set;}=null!;
    public string Prompt {get;private set;}="";
    private InteractionPolicy policy=null!;private string? selected;private double elapsed;
    public override void _Ready(){var p=GetNode<GameSession>("/root/GameSession").Catalog!.Parameters;policy=new((float)p["interact.radius"],(float)p["interact.hysteresis"],p["interact.repeat_guard"]);}
    public override void _Process(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession"); if(session.Flow!=FlowState.Field){Prompt="";return;} elapsed+=delta;
        var targets=GetTree().GetNodesInGroup("interactables").Cast<Interactable>().Where(t=>t.IsInsideTree()).ToArray();
        var candidates=targets.Select(t=>{
            using var query=PhysicsRayQueryParameters2D.Create(Player.GlobalPosition+new Vector2(0,-30),t.GlobalPosition+new Vector2(0,-30),1);
            return new InteractionCandidate(t.Id,t.GlobalPosition.X,Player.GetWorld2D().DirectSpaceState.IntersectRay(query).Count==0);
        }).ToArray();
        selected=policy.Select(candidates,selected,Player.GlobalPosition.X); Prompt=selected==null?"A/D 移动 · Shift 加快": "E · "+targets.First(t=>t.Id==selected).Caption;
    }
    public override void _UnhandledInput(InputEvent ev)
    {
        if(ev is InputEventKey{Pressed:true,Echo:false} key && key.PhysicalKeycode==Key.E && selected!=null)
        {
            var session=GetNode<GameSession>("/root/GameSession");
            var target=GetTree().GetNodesInGroup("interactables").Cast<Interactable>().FirstOrDefault(t=>t.Id==selected);
            if(target!=null && session.Flow==FlowState.Field && policy.TryActivate(selected,elapsed)){target.TryInteract(session);GetViewport().SetInputAsHandled();}
        }
    }
}
