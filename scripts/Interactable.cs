using Godot;
public partial class Interactable : Node2D
{
    [Export] public string Id {get;set;}="";
    [Export] public string Caption {get;set;}="";
    [Export(PropertyHint.MultilineText)] public string Description {get;set;}="";
    [Export] public string ActionId {get;set;}="observe";
    [Export] public bool IsActive {get;set;}=true;
    // Optional authored road region. Ordinary people/objects keep point interaction.
    [Export] public Rect2 InteractionRegion {get;set;}
    public Vector2 NearestInteractionPoint(Vector2 playerGlobal)
    {
        if(!InteractionRegion.HasArea())return GlobalPosition;
        var local=ToLocal(playerGlobal);
        return ToGlobal(new Vector2(Math.Clamp(local.X,InteractionRegion.Position.X,InteractionRegion.End.X),
            Math.Clamp(local.Y,InteractionRegion.Position.Y,InteractionRegion.End.Y)));
    }
    public override void _Ready()
    {
        AddToGroup("interactables");
        SetActive(IsActive);
    }
    public void SetActive(bool value){IsActive=value;Visible=value;}
    public bool TryInteract(GameSession session)
    {
        if(!IsActive||!IsVisibleInTree()||session.Flow!=GeXingzhou.Domain.FlowState.Field)return false;
        MainView? main=null;
        for(Node? p=this;p!=null;p=p.GetParent())
        {if(p.HasMeta("binding_error"))return false;if(p is MainView m){main=m;break;}}
        if(main==null)return false;
        main.HandleInteraction(this);return true;
    }
}
