namespace GeXingzhou.Domain;
public enum FoodChoice {Take,Wait,Share}
public sealed record MemoryState
{
    public string InstanceId {get;init;}="soup-1";
    public bool Replay {get;init;}
    public HashSet<string> PushedCoinIds {get;init;}=new();
    public int PushedTotal {get;init;}
    public FoodChoice? FoodChoice {get;init;}
    public bool Completed {get;init;}
}
public static class MemorySession
{
    public static MemoryState Begin(WorldSnapshot state,SceneReturnContext context,bool replay)
    {
        if(!replay&&state.MemoryState is {Completed:false} old)return old;
        return new(){InstanceId="soup-"+(state.MemoryOrdinal+1),Replay=replay};
    }
    public static MemoryState PushCoin(MemoryState state,string coinId)
    {
        if(state.Completed||state.PushedCoinIds.Contains(coinId)||coinId is not ("c1" or "c2" or "c3" or "c4"))return state;
        return state with {PushedCoinIds=new HashSet<string>(state.PushedCoinIds){coinId},PushedTotal=state.PushedTotal+(coinId=="c4"?2:1)};
    }
    public static MemoryState ResolveFood(MemoryState state,FoodChoice choice)=>state.Completed||state.PushedTotal!=5||!Enum.IsDefined(choice)?state:state with {FoodChoice=choice,Completed=true};
}
