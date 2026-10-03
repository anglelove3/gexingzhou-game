namespace GeXingzhou.Domain;
public enum InvitationResolution { Answered, Arrived }
public sealed record InvitationState
{
    public double Elapsed {get;init;}
    public bool VoiceReceived {get;init;}
    public bool PhoneRinging {get;init;}
    public bool CarArrived {get;init;}
    public InvitationResolution? Resolution {get;init;}
}
public static class InvitationClock
{
    public static InvitationState Advance(InvitationState state,double dt,bool controllable,double voiceDelay=20,double callDelay=15,double carDelay=30)
    {
        if(!controllable||!double.IsFinite(dt)||dt<=0)return state;
        var elapsed=state.Elapsed+dt;
        return state with {Elapsed=elapsed,VoiceReceived=elapsed>=voiceDelay,PhoneRinging=elapsed>=voiceDelay+callDelay&&state.Resolution!=InvitationResolution.Answered,CarArrived=elapsed>=voiceDelay+callDelay+carDelay};
    }
}
