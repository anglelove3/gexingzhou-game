using Godot;

public partial class BenchView : Interactable
{
    [Export] public Marker2D SeatAnchor {get;set;}=null!;
    [Export] public Marker2D StandAnchor {get;set;}=null!;
    public override void _Ready()
    {
        base._Ready();
        if(SeatAnchor==null||StandAnchor==null)
            SceneBindings.ReportFailure(this,"长椅缺少SeatAnchor或StandAnchor引用。");
    }
}
