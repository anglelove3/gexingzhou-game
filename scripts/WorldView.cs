using Godot;
public partial class WorldView : Node2D
{
    [Export] public string SceneId {get;set;}="community_gate";
    public int Width {get;private set;}=1600;
    public PlayerController Player {get;private set;}=null!;
    public InteractionController Interactions {get;private set;}=null!;
    public override void _Ready()
    {
        Width=SceneId=="soup_shop"?960:SceneId=="convenience_street"?1280:1600;
        Player=GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<PlayerController>();Player.Position=new Vector2(320,280);AddChild(Player);
        Player.GetNode<Camera2D>("Camera2D").LimitRight=Width;
        Floor(new Vector2(Width/2f,288),new Vector2(Width,16));Floor(new Vector2(-8,180),new Vector2(16,360));Floor(new Vector2(Width+8,180),new Vector2(16,360));
        Interactions=new InteractionController{Player=Player};AddChild(Interactions);
        AddTarget("old_sign",160,"看看旧路牌","路是新的，牌还是旧的。以前闭着眼能走到家的地方，现在得看导航。", "observe");
        AddTarget("bench",960,"坐一会儿","统一搬来的楼，一排一排。人也整齐了，记忆没那么听话。", "observe");
        QueueRedraw();
    }
    public Interactable AddTarget(string id,float x,string caption,string body,string action)
    {
        var target=new Interactable{Id=id,Position=new Vector2(x,280),Caption=caption,Description=body,ActionId=action};AddChild(target);return target;
    }
    private void Floor(Vector2 p,Vector2 size){var body=new StaticBody2D{Position=p,CollisionLayer=1};body.AddChild(new CollisionShape2D{Shape=new RectangleShape2D{Size=size}});AddChild(body);}
    public override void _Draw()
    {
        DrawRect(new Rect2(0,0,Width,360),new Color("a0adb3"));
        for(int x=40;x<Width;x+=220){DrawRect(new Rect2(x,34,172,218),new Color("697c88"));for(int row=0;row<5;row++)for(int col=0;col<5;col++)DrawRect(new Rect2(x+12+col*30,48+row*37,14,22),new Color("bbbdac"));}
        DrawRect(new Rect2(0,252,Width,28),new Color("657c6d"));DrawRect(new Rect2(0,280,Width,80),new Color("494f56"));
        DrawLine(new Vector2(0,308),new Vector2(Width,308),new Color("aba48a"),2);
    }
}
