using Godot;
using GeXingzhou.Domain;
public partial class GameSession : Node
{
    public WorldSnapshot Snapshot {get;private set;} = new();
    public ContentCatalog? Catalog {get;private set;}
    public string ContentError {get;private set;} = "";
    public FlowState Flow {get;set;} = FlowState.Field;
    public void UpdatePosition(Position2 position) => Snapshot=Snapshot with {PlayerPosition=position};
    public override void _Ready()
    {
        var result=ContentCatalog.LoadText(name => Godot.FileAccess.GetFileAsString("res://content/vs01/"+name));
        Catalog=result.Catalog; ContentError=string.Join("\n",result.Errors);
    }
    public void NewGame() => Snapshot = new();
    public TransitionResult TryDispatch(StoryAction action) => new(false,Snapshot,"not_available");
}
