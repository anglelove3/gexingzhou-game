using Godot;
using GeXingzhou.Domain;
public partial class ChoiceController : Control
{
    private PanelContainer? panel;public bool IsOpen=>panel!=null;
    public void Open(string title,IReadOnlyList<(string Caption,Action Act)> options)
    {
        Close();panel=new PanelContainer{Position=new Vector2(230,220),Size=new Vector2(820,380)};AddChild(panel);
        var box=new VBoxContainer{CustomMinimumSize=new Vector2(780,320)};panel.AddChild(box);
        box.AddChild(new Label{Text=title,AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(760,90)});
        foreach(var option in options){var button=new Button{Text=option.Caption,CustomMinimumSize=new Vector2(760,56)};box.AddChild(button);button.Pressed+=()=>{Close();option.Act();};}
        box.AddChild(new Label{Text="↑↓ 选择 · Enter / E 确认 · Esc 返回"});GetNode<GameSession>("/root/GameSession").Flow=FlowState.Dialogue;
        for(int i=1;i<=options.Count;i++){var button=box.GetChild<Button>(i);button.FocusNeighborBottom=button.GetPathTo(box.GetChild<Button>(i==options.Count?1:i+1));button.FocusNeighborTop=button.GetPathTo(box.GetChild<Button>(i==1?options.Count:i-1));}
        box.GetChild<Button>(1).GrabFocus();
    }
    public void Close(){if(panel==null)return;panel.QueueFree();panel=null;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;}
    public void HandleKey(Key key){if(key==Key.Escape)Close();else if(key==Key.E&&GetViewport().GuiGetFocusOwner() is Button button)button.EmitSignal(BaseButton.SignalName.Pressed);}
}
