using Godot;
using GeXingzhou.Domain;

// Session-only presentation: never advances quests or writes hint state to saves.
public partial class GuidanceController : Node
{
    private MainView? main;private Label hint=null!;
    private bool observationShown,messageShown;private double remaining;private string current="";
    public void Configure(MainView owner){main=owner;hint=SceneBindings.Require<Label>(owner,"HUD/Guidance");}
    public override void _Process(double delta)
    {
        if(main==null)return;
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Flow==FlowState.Field)remaining=Math.Max(0,remaining-delta);
    }
    public void Refresh()
    {
        if(main==null||!GodotObject.IsInstanceValid(main.World))return;
        var s=GetNode<GameSession>("/root/GameSession");var field=s.Flow==FlowState.Field;
        var targets=main.World.GetTargets();
        foreach(var name in main.World.FindChildren("NameLabel","Label",true,false).OfType<Label>())
        {
            var owner=(Node2D)name.GetParent();var target=owner as Interactable;
            var distance=main.World.Mode==WorldMode.Depth2D?owner.GlobalPosition.DistanceTo(main.World.Player.GlobalPosition):Math.Abs(owner.GlobalPosition.X-main.World.Player.GlobalPosition.X);
            name.Visible=field&&(target?.IsActive??true)&&owner.IsVisibleInTree()&&distance<=(target==null?200:140);
            name.AddThemeFontSizeOverride("font_size",s.Options.SubtitleSize);
            name.AddThemeColorOverride("font_color",new Color(.98f,.94f,.84f,1));
            name.AddThemeColorOverride("font_outline_color",new Color(.06f,.045f,.025f,1));
            name.AddThemeConstantOverride("outline_size",4);
            if(owner.GetNodeOrNull<Sprite2D>("Artwork") is {} art&&art.Texture!=null)
            {
                var font=name.GetThemeFont("font");var size=s.Options.SubtitleSize;
                var width=Math.Max(160,font.GetStringSize(name.Text,fontSize:size).X+20);
                var height=font.GetHeight(size)+8;
                var top=art.Position.Y-(art.Centered?art.Texture.GetHeight()*art.Scale.Y/2:0);
                name.Size=new(width,height);name.Position=new(-width/2,top-height-6);
            }
        }
        var nearbyObservation=field&&targets.Any(t=>t.ActionId=="observe"&&t.IsActive&&(main.World.Mode==WorldMode.Depth2D?t.GlobalPosition.DistanceTo(main.World.Player.GlobalPosition)<=40:Math.Abs(t.GlobalPosition.X-main.World.Player.GlobalPosition.X)<=40));
        var invitation=s.Snapshot.Stage<=SliceStage.InvitationResolved;
        if(field&&invitation&&!messageShown&&s.Snapshot.InvitationState.VoiceReceived)
        {messageShown=true;current="Tab 打开旧手机 · 张大炮发来了消息";remaining=6;}
        else if(nearbyObservation&&!observationShown&&!s.Snapshot.CompletedActions.Contains("observation.community.first"))
        {observationShown=true;current="E 观察 · 可以看看，也可以继续走";remaining=4;}
        var street=main.World.SceneId=="community_gate"?targets.FirstOrDefault(t=>t.Id=="street"&&t.IsActive&&t.IsVisibleInTree()):null;
        var streetDistance=street==null?float.PositiveInfinity:street.GlobalPosition.DistanceTo(main.World.Player.GlobalPosition);
        if(field&&streetDistance<=125){
            hint.Text=streetDistance<=40?"E 前往便利店街":"走近便利店街路口 · 按 E 进入";
            hint.Visible=true;return;
        }
        hint.Text=current;
        hint.Visible=field&&remaining>0&&(current.StartsWith("Tab")?invitation:nearbyObservation);
    }
}
