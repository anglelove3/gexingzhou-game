using Godot;
using GeXingzhou.Domain;
public partial class MainView : Control
{
    public WorldView World {get;private set;}=null!;
    private Label prompt=null!; private PanelContainer? notice;
    public override void _Ready()
    {
        var container=new SubViewportContainer{Size=new Vector2(1280,720),Stretch=true,StretchShrink=2}; AddChild(container);
        var viewport=new SubViewport{Size=new Vector2I(640,360),RenderTargetUpdateMode=SubViewport.UpdateMode.Always}; container.AddChild(viewport);
        World=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();viewport.AddChild(World);
        AddChild(new Label{Text="故乡 · 安置小区\n原型美术 / 首段开发中",Position=new Vector2(30,24)});
        prompt=new Label{Position=new Vector2(30,658)};AddChild(prompt);
    }
    public override void _Process(double delta){prompt.Text=World.Interactions.Prompt;}
    public void ShowNotice(string title,string body)
    {
        if(notice!=null)return;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Dialogue;
        notice=new PanelContainer{Position=new Vector2(100,470),Size=new Vector2(1080,160)};AddChild(notice);
        notice.AddChild(new Label{Text=title+"\n"+body+"\n[E / Esc 继续]",AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(1040,140)});
    }
    public override void _Input(InputEvent ev)
    {
        if(notice!=null && ev is InputEventKey{Pressed:true,Echo:false} key && (key.PhysicalKeycode==Key.E||key.PhysicalKeycode==Key.Escape))
        {notice.QueueFree();notice=null;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;GetViewport().SetInputAsHandled();}
    }
}
