using Godot;
public partial class Interactable : Node2D
{
    [Export] public string Id {get;set;}="";
    [Export] public string Caption {get;set;}="";
    [Export(PropertyHint.MultilineText)] public string Description {get;set;}="";
    [Export] public string ActionId {get;set;}="observe";
    [Export] public bool IsActive {get;set;}=true;
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
