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
    public InvitationState InvitationState {get;init;}=new();
    public Dictionary<string,string> ChoiceCodes {get;init;}=new();
    public MemoryState? MemoryState {get;init;}
    public SceneReturnContext? ReturnContext {get;init;}
    public int MemoryOrdinal {get;init;}
    public int MemoryVisitOrdinal {get;init;}
    public string PlaythroughId {get;init;}=Guid.NewGuid().ToString();
    public Dictionary<string,long> SceneActiveMilliseconds {get;init;}=new();
    public GameSettings Settings {get;init;}=new();
}
