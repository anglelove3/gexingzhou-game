using Godot;
using GeXingzhou.Domain;
public partial class RestOptionsController : Control
{
    private Action<RestChoice>? chosen;private Control? previousFocus;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            foreach(var (name,choice) in new[]{("Rest",RestChoice.Rest),("Smoke",RestChoice.Smoke),("Rise",RestChoice.Rise)})
                SceneBindings.Require<Button>(this,"Panel/Options/"+name).Pressed+=()=>{if(IsOpen)chosen?.Invoke(choice);};
            Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void Open(Action<RestChoice> callback)
    {
        if(IsOpen||HasMeta("binding_error"))return;
        previousFocus=GetViewport().GuiGetFocusOwner();chosen=callback;IsOpen=true;Visible=true;
        GetNode<Button>("Panel/Options/Rest").GrabFocus();
    }
    public void ActivateFocused()
    {
        if(!IsOpen)return;
        var button=GetViewport().GuiGetFocusOwner() as Button;
        if(button!=null&&IsAncestorOf(button))button.EmitSignal(Button.SignalName.Pressed);
    }
    public void Close()
    {
        chosen=null;IsOpen=false;Visible=false;
        if(IsInsideTree()&&GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;
    }
}
