using Godot;
public partial class BootMenu : Control
{
    public override void _Ready()
    {
        var box=new VBoxContainer{Position=new Vector2(390,170),Size=new Vector2(500,400)}; AddChild(box);
        box.AddChild(new Label{Text="葛行舟",HorizontalAlignment=HorizontalAlignment.Center});
        box.AddChild(new Label{Text="首段试玩 · 原型美术",HorizontalAlignment=HorizontalAlignment.Center});
        var start=new Button{Text="回到故乡",CustomMinimumSize=new Vector2(480,54)}; box.AddChild(start);
        start.Pressed += ()=>{GetNode<GameSession>("/root/GameSession").NewGame(); GetTree().ChangeSceneToFile("res://scenes/Main.tscn");};
        var resume=new Button{Text="继续（暂无存档）",Disabled=true}; box.AddChild(resume);
        var settings=new Button{Text="设置（开发中）",Disabled=true}; box.AddChild(settings);
        var quit=new Button{Text="退出"}; box.AddChild(quit); quit.Pressed+=()=>GetTree().Quit(); start.GrabFocus();
        var error=GetNode<GameSession>("/root/GameSession").ContentError;
        if(error.Length>0){start.Disabled=true; box.AddChild(new Label{Text="内容加载失败："+error});}
    }
}
