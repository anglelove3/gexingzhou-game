using Godot;
public partial class Interactable : Node2D
{
    public string Id {get;set;}="";
    public string Caption {get;set;}="";
    public string Description {get;set;}="";
    public string ActionId {get;set;}="observe";
    public override void _Ready()
    {
        AddToGroup("interactables");
        if(Id=="cannon")AddChild(ArtAssets.Grounded(ArtAssets.Npc(0),Vector2.Zero));
        else if(Id=="hey")AddChild(ArtAssets.Grounded(ArtAssets.Npc(1),Vector2.Zero));
        else if(Id=="seat")AddChild(ArtAssets.Grounded(ArtAssets.Npc(3),new Vector2(30,0),56));
        var actor=GetNodeOrNull<Sprite2D>("Artwork");if(actor!=null)actor.FlipH=true;
        // Background scenery now carries signs/benches/counters. The marker is only an interaction affordance.
        var marker=new Label{Text=ActionId.StartsWith("scene:")?"›":"◇",Position=new Vector2(-8,-88)};
        marker.AddThemeFontSizeOverride("font_size",16);marker.AddThemeColorOverride("font_color",new Color("efd294"));marker.AddThemeColorOverride("font_outline_color",new Color("18212b"));marker.AddThemeConstantOverride("outline_size",3);AddChild(marker);
    }
    public bool TryInteract(GameSession session)
    {
        if(session.Flow!=GeXingzhou.Domain.FlowState.Field)return false;
        var main=GetTree().CurrentScene as MainView;
        if(main==null)main=GetParent().GetParent()?.GetParent()?.GetParent() as MainView;
        if(main==null)return false;
        main.HandleInteraction(this);return true;
    }
}
