using Godot;
using GeXingzhou.Domain;

public partial class WorldView : Node2D
{
    [Export] public string SceneId {get;set;}="community_gate";
    [Export] public int Width {get;set;}=1600;
    [Export] public Rect2 ViewBounds {get;set;}=new(0,0,1600,360);
    public PlayerController Player {get;private set;}=null!;
    public InteractionController Interactions {get;private set;}=null!;
    public override void _Ready()
    {
        try
        {
            Player=SceneBindings.Require<PlayerController>(this,"Player");
            Interactions=SceneBindings.Require<InteractionController>(this,"Interactions");
            SceneBindings.Require<Sprite2D>(this,"Backdrop");
            foreach(var path in new[]{"Floor/CollisionShape2D","LeftBoundary/CollisionShape2D","RightBoundary/CollisionShape2D"})
                SceneBindings.Require<CollisionShape2D>(this,path);
            if(HasMeta("binding_error"))throw new InvalidOperationException(GetMeta("binding_error").AsString());
            var ids=new HashSet<string>();
            foreach(var target in GetChildren().OfType<Interactable>())
                if(string.IsNullOrWhiteSpace(target.Id)||!ids.Add(target.Id))
                    throw new InvalidOperationException("交互标识为空或重复："+target.Id);
            Interactions.Player=Player;
            Player.GetNode<Camera2D>("Camera2D").LimitRight=Width;
            RefreshQuestActors(GetNode<GameSession>("/root/GameSession").Snapshot);
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public Interactable? GetTarget(string id)=>GetChildren().OfType<Interactable>().FirstOrDefault(t=>t.Id==id);
    public void RefreshQuestActors(WorldSnapshot snapshot)
    {
        GetTarget("cannon")?.SetActive(SceneId=="community_gate"&&snapshot.InvitationState.CarArrived&&snapshot.Stage<=SliceStage.InvitationResolved);
    }
    public void ShowCandyHandover()
    {
        if(GetNodeOrNull<Sprite2D>("Hey/HandoverCandy") is not {} candy)return;
        candy.Visible=true;candy.Position=new(-30,-40);candy.Modulate=Colors.White;
        var tween=CreateTween();tween.TweenProperty(candy,"position",new Vector2(-2,-35),GetNode<GameSession>("/root/GameSession").Options.ReducedMotion?.2:.65);
        tween.TweenInterval(.35);tween.TweenProperty(candy,"modulate:a",0f,.2);tween.TweenCallback(Callable.From(()=>candy.Visible=false));
    }
    public void ShowPayment()
    {
        if(GetNodeOrNull<Sprite2D>("PaymentCoin") is not {} coin)return;
        coin.Visible=true;coin.Position=new(390,224);coin.Modulate=Colors.White;
        var tween=CreateTween();tween.TweenProperty(coin,"position",new Vector2(418,216),GetNode<GameSession>("/root/GameSession").Options.FadeDuration);
        tween.TweenInterval(.6);tween.TweenProperty(coin,"modulate:a",0f,.2);tween.TweenCallback(Callable.From(()=>coin.Visible=false));
    }
    public Interactable AddTarget(string id,float x,string caption,string body,string action)
    {
        var target=GD.Load<PackedScene>("res://scenes/world/Interactable.tscn").Instantiate<Interactable>();
        target.Name="Dynamic_"+id;target.Id=id;target.Position=new Vector2(x,280);
        target.Caption=caption;target.Description=body;target.ActionId=action;AddChild(target);return target;
    }
}
