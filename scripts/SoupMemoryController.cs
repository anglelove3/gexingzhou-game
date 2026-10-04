using Godot;
using GeXingzhou.Domain;
public partial class SoupMemoryController : Control
{
    public MainView Main {get;set;}=null!;private Label status=null!;private Button[] coins=Array.Empty<Button>();private Button[] foods=Array.Empty<Button>();private int selected;private bool initialized;private bool dragging;private Vector2 dragStart;
    public override void _Ready()
    {
        AddChild(ArtAssets.Picture(ArtAssets.Texture("memory-v1.png"),Vector2.Zero,new Vector2(1280,720)));
        var bowl=ArtAssets.Picture(ArtAssets.Prop(2),new Vector2(970,34),new Vector2(180,140),"SoupBowl");bowl.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;AddChild(bowl);
        var panel=new PanelContainer{Position=new Vector2(120,160),Size=new Vector2(1040,460)};var style=UiStyles.Panel();style.BgColor=new Color(.06f,.06f,.07f,.75f);panel.AddThemeStyleboxOverride("panel",style);AddChild(panel);var box=new VBoxContainer{CustomMinimumSize=new Vector2(1000,420)};panel.AddChild(box);
        box.AddChild(new Label{Text="那年清晨 · 网吧包夜之后\n回忆不会改写过去",CustomMinimumSize=new Vector2(960,80)});
        status=new Label{CustomMinimumSize=new Vector2(960,65)};box.AddChild(status);
        var row=new HBoxContainer();box.AddChild(row);coins=new Button[4];
        for(int i=0;i<4;i++)
        {
            var index=i;var button=new Button{Text=$"{(i==3?2:1)}",Icon=ArtAssets.Prop(i==3?1:0),ExpandIcon=true,CustomMinimumSize=new Vector2(236,94)};button.AddThemeConstantOverride("icon_max_width",64);coins[i]=button;row.AddChild(button);button.Pressed+=()=>Push(index);
            button.GuiInput+=ev=>{if(ev is InputEventMouseButton mb&&mb.ButtonIndex==MouseButton.Left){if(mb.Pressed){dragging=true;dragStart=mb.Position;}else if(dragging){dragging=false;if(mb.Position.DistanceTo(dragStart)>30)Push(index);}}};
        }
        box.AddChild(new Label{Text="把四枚硬币推到桌子那边，凑足五个单位。",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var foodRow=new HBoxContainer();box.AddChild(foodRow);foods=new Button[3];
        for(int i=0;i<3;i++){var index=i;var b=new Button{Text=new[]{"接过汤","让他先吃（明确等待）","一起分着吃"}[i],CustomMinimumSize=new Vector2(315,70)};foods[i]=b;foodRow.AddChild(b);b.Pressed+=()=>Resolve(index);}
        box.AddChild(new Label{Text="← → / A D 选择 · E 推币/确认 · 鼠标点选或拖币\nEsc 回现实，下次继续；五个单位为叙事，不代表现实价格。",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        Refresh();
    }
    public override void _Process(double delta)=>Refresh();
    private void Refresh()
    {
        var m=GetNode<GameSession>("/root/GameSession").Snapshot.MemoryState;if(m==null)return;
        status.Text=$"推过：{m.PushedTotal} / 5"+(m.Replay?"（主动重看）":"");
        for(int i=0;i<4;i++){coins[i].Disabled=m.PushedCoinIds.Contains("c"+(i+1));coins[i].Text=coins[i].Disabled?"已推过":$"{(i==3?2:1)}";coins[i].Modulate=coins[i].Disabled?new Color(1,1,1,.55f):Colors.White;}
        foreach(var f in foods)f.Disabled=m.PushedTotal!=5;
        if(!initialized){initialized=true;selected=m.PushedTotal==5?0:Array.FindIndex(coins,b=>!b.Disabled);if(selected<0)selected=0;(m.PushedTotal==5?foods[selected]:coins[selected]).GrabFocus();}
    }
    private void Push(int index)
    {
        var s=GetNode<GameSession>("/root/GameSession");var m=s.Snapshot.MemoryState;if(m==null||s.Flow!=FlowState.Memory)return;
        var id="c"+(index+1);s.TryDispatch(new("memory.coin.push",id,m.InstanceId+":"+id));Refresh();
        if(s.Snapshot.MemoryState!.PushedTotal==5){selected=0;foods[0].GrabFocus();}
        else{for(int offset=1;offset<=4;offset++){var next=(index+offset)%4;if(!coins[next].Disabled){selected=next;coins[next].GrabFocus();break;}}}
    }
    private void Resolve(int index)
    {
        var s=GetNode<GameSession>("/root/GameSession");var m=s.Snapshot.MemoryState;if(m==null||s.Flow!=FlowState.Memory)return;
        if(s.TryDispatch(new("memory.food.resolve",new[]{"Take","Wait","Share"}[index],m.InstanceId)).Applied)_=Main.ReturnMemory(true);
    }
    public void HandleKey(Key key)
    {
        var s=GetNode<GameSession>("/root/GameSession");if(s.Flow!=FlowState.Memory)return;
        if(key==Key.Escape){_=Main.ReturnMemory(false);return;}
        bool food=s.Snapshot.MemoryState?.PushedTotal==5;
        if(key is Key.Left or Key.A or Key.Right or Key.D){var count=food?3:4;selected=(selected+(key is Key.Left or Key.A?-1:1)+count)%count;(food?foods[selected]:coins[selected]).GrabFocus();}
        else if(key is Key.E or Key.Enter){if(food)Resolve(selected);else Push(selected);}
    }
}
