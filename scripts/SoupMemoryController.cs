using Godot;
using GeXingzhou.Domain;
public partial class SoupMemoryController : Control
{
    public MainView Main {get;set;}=null!;private Label status=null!;private Button[] coins=Array.Empty<Button>();private Button[] foods=Array.Empty<Button>();private int selected;private bool initialized;private bool dragging;private Vector2 dragStart;
    public override void _Ready()
    {
        try
        {
            status=SceneBindings.Require<Label>(this,"Panel/Content/Status");
            coins=Enumerable.Range(1,4).Select(i=>SceneBindings.Require<Button>(this,"Panel/Content/Coins/Coin"+i)).ToArray();
            foods=new[]{"Take","Wait","Share"}.Select(id=>SceneBindings.Require<Button>(this,"Panel/Content/Foods/"+id)).ToArray();
            for(int i=0;i<coins.Length;i++)
            {
                var index=i;coins[i].Pressed+=()=>Push(index);
                coins[i].GuiInput+=ev=>{
                    if(ev is InputEventMouseButton mb&&mb.ButtonIndex==MouseButton.Left)
                    {
                        if(mb.Pressed){dragging=true;dragStart=mb.Position;}
                        else if(dragging){dragging=false;if(mb.Position.DistanceTo(dragStart)>30)Push(index);}
                    }};
            }
            for(int i=0;i<foods.Length;i++){var index=i;foods[i].Pressed+=()=>Resolve(index);}
            Refresh();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
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
