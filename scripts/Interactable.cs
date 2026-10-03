using Godot;
public partial class Interactable : Node2D
{
    public string Id {get;set;}="";
    public string Caption {get;set;}="";
    public string Description {get;set;}="";
    public string ActionId {get;set;}="observe";
    public override void _Ready(){AddToGroup("interactables");QueueRedraw();}
    public bool TryInteract(GameSession session)
    {
        if(session.Flow!=GeXingzhou.Domain.FlowState.Field)return false;
        var main=GetTree().CurrentScene as MainView;
        if(main==null)main=GetParent().GetParent()?.GetParent()?.GetParent() as MainView;
        if(main==null)return false;
        main.HandleInteraction(this);return true;
    }
    public override void _Draw(){DrawRect(new Rect2(-10,-36,20,36),new Color("b6a071"));}
}
