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
        var texture=ArtAssets.Texture(SceneId=="soup_shop"?"soup-v1.png":SceneId=="convenience_street"?"street-v1.png":"community-v1.png");
        float scale=Math.Max(Width/(float)texture.GetWidth(),360f/texture.GetHeight());
        AddChild(new Sprite2D{Name="Backdrop",Texture=texture,Centered=false,Scale=Vector2.One*scale,Position=new Vector2(0,280-texture.GetHeight()*scale*.78f),ZIndex=-10,TextureFilter=TextureFilterEnum.Linear});
        Player=GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<PlayerController>();Player.Position=new Vector2(320,280);AddChild(Player);
        Player.GetNode<Camera2D>("Camera2D").LimitRight=Width;
        Floor(new Vector2(Width/2f,288),new Vector2(Width,16));Floor(new Vector2(-8,180),new Vector2(16,360));Floor(new Vector2(Width+8,180),new Vector2(16,360));
        Interactions=new InteractionController{Player=Player};AddChild(Interactions);
        if(SceneId=="convenience_street")
        {
            AddTarget("community",80,"回安置小区","","scene:community_gate");
            AddTarget("hey",600,"Hey哥 · 递喜糖 / 聊两句","","candy.hey.delivered");
            AddTarget("soup",1120,"去鸭血粉丝汤店","","scene:soup_shop");
        }
        else if(SceneId=="soup_shop")
        {
            AddTarget("street",80,"回便利店街","","scene:convenience_street");
            AddTarget("seat",440,"张大炮 · 桌边坐下","","soup.meet");
            AddTarget("sign",200,"旧招牌","店换了地方，招牌搬了过来。边角的油烟擦不掉。","observe");
            AddTarget("menu",700,"看看菜单","鸭血粉丝汤。记忆里便宜，现在也够吃顿热乎的。","observe");
            AddTarget("counter",800,"收银台","吃完再结账。老板摆摆手：先坐。","observe");
            var owner=ArtAssets.Grounded(ArtAssets.Npc(2),new Vector2(850,280),68,"Shopkeeper");owner.FlipH=true;AddChild(owner);
            AddChild(ArtAssets.Grounded(ArtAssets.Prop(2),new Vector2(415,214),15,"SoupBowl"));
        }
        else
        {
            AddTarget("old_sign",160,"看看旧路牌","路是新的，牌还是旧的。以前闭着眼能走到家的地方，现在得看导航。", "observe");
            AddTarget("bench",960,"坐一会儿","统一搬来的楼，一排一排。人也整齐了，记忆没那么听话。", "observe");
            AddTarget("street",1480,"去便利店街","","scene:convenience_street");
        }
        QueueRedraw();
    }
    public Interactable AddTarget(string id,float x,string caption,string body,string action)
    {
        var target=new Interactable{Id=id,Position=new Vector2(x,280),Caption=caption,Description=body,ActionId=action};AddChild(target);return target;
    }
    private void Floor(Vector2 p,Vector2 size){var body=new StaticBody2D{Position=p,CollisionLayer=1};body.AddChild(new CollisionShape2D{Shape=new RectangleShape2D{Size=size}});AddChild(body);}
}
