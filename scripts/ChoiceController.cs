using Godot;
using GeXingzhou.Domain;
public partial class ChoiceController : Control
{
    private VBoxContainer optionsBox=null!;
    private Label titleLabel=null!;
    private FlowState source;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            titleLabel=SceneBindings.Require<Label>(this,"Panel/Content/Title");
            optionsBox=SceneBindings.Require<VBoxContainer>(this,"Panel/Content/Options");Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void Open(string title,IReadOnlyList<(string Caption,Action Act)> options)
    {
        if(HasMeta("binding_error")||options.Count==0)return;
        Close();var s=GetNode<GameSession>("/root/GameSession");source=s.Flow;
        foreach(var child in optionsBox.GetChildren()){optionsBox.RemoveChild(child);child.QueueFree();}
        titleLabel.Text=title;var buttons=new List<Button>();
        for(int i=0;i<options.Count;i++)
        {
            var option=options[i];var button=new Button{Name="Option"+(i+1),Text=option.Caption,CustomMinimumSize=new Vector2(0,56)};
            optionsBox.AddChild(button);buttons.Add(button);button.Pressed+=()=>{if(!IsOpen)return;Close();option.Act();};
        }
        for(int i=0;i<buttons.Count;i++)
        {
            buttons[i].FocusNeighborBottom=buttons[i].GetPathTo(buttons[(i+1)%buttons.Count]);
            buttons[i].FocusNeighborTop=buttons[i].GetPathTo(buttons[(i+buttons.Count-1)%buttons.Count]);
        }
        IsOpen=true;Visible=true;s.Flow=FlowState.Dialogue;buttons[0].GrabFocus();
    }
    public void Close(){if(!IsOpen)return;IsOpen=false;Visible=false;GetNode<GameSession>("/root/GameSession").Flow=source;}
    public void HandleKey(Key key)
    {
        if(key==Key.Escape)Close();
        else if(key==Key.E&&GetViewport().GuiGetFocusOwner() is Button button&&optionsBox.IsAncestorOf(button))
            button.EmitSignal(BaseButton.SignalName.Pressed);
    }
}
