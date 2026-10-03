namespace GeXingzhou.Domain;
public sealed record WorldSnapshot
{
    public int SchemaVersion {get;init;} = 1;
    public string ContentVersion {get;init;} = "vs01-0.1";
    public string SceneId {get;init;} = "community_gate";
    public Position2 PlayerPosition {get;init;} = new(320,280);
    public SliceStage Stage {get;init;} = SliceStage.FreeArrival;
    public int GameDay {get;init;} = 1;
    public string TimeBlock {get;init;} = "arrival";
    public int CandyCount {get;init;}
    public HashSet<string> CompletedActions {get;init;} = new();
    public string PhoneState {get;init;} = "closed";
}
