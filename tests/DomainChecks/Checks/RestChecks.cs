using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class RestChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Rest","DefaultNoSmoke",()=>{
            var r=new RestStateMachine();Check.True(r.TrySit());Check.True(!r.TrySit());
            Check.Equal(RestPhase.SittingDown,r.Phase);r.AnimationFinished();
            Check.Equal(RestPhase.Seated,r.Phase);Check.True(r.MenuOpen);
            Check.True(r.TryChoose(RestChoice.Rest));Check.True(!r.MenuOpen);Check.Equal(RestPhase.Seated,r.Phase);
            r.AnimationFinished();Check.Equal(RestPhase.Seated,r.Phase);Check.True(!r.MenuOpen);
        }));
        tests.Add(("Rest","RepeatedInputAndEscape",()=>{
            var r=new RestStateMachine();r.TrySit();r.AnimationFinished();r.HandleEscape();
            Check.True(!r.MenuOpen);Check.Equal(RestPhase.Seated,r.Phase);Check.True(r.ReopenMenu());
            Check.True(r.TryChoose(RestChoice.Smoke));Check.True(!r.TryChoose(RestChoice.Smoke));
            Check.Equal(RestPhase.Smoking,r.Phase);r.AnimationFinished();Check.Equal(RestPhase.Seated,r.Phase);
            r.HandleEscape();Check.Equal(RestPhase.StandingUp,r.Phase);r.AnimationFinished();Check.Equal(RestPhase.Standing,r.Phase);
        }));
        tests.Add(("Rest","SuspensionAndCancel",()=>{
            foreach(var phase in new[]{RestPhase.Standing,RestPhase.SittingDown,RestPhase.Seated,RestPhase.Smoking,RestPhase.StandingUp})
            {
                var r=new RestStateMachine();
                if(phase!=RestPhase.Standing)r.TrySit();
                if(phase is RestPhase.Seated or RestPhase.Smoking or RestPhase.StandingUp)r.AnimationFinished();
                if(phase==RestPhase.Smoking)r.TryChoose(RestChoice.Smoke);
                if(phase==RestPhase.StandingUp)r.TryChoose(RestChoice.Rise);
                Check.Equal(phase,r.Phase);r.Suspend();Check.True(r.Suspended);Check.True(!r.MenuOpen);
                Check.True(!r.TrySit());Check.True(!r.TryChoose(RestChoice.Smoke));r.AnimationFinished();Check.Equal(phase,r.Phase);
                r.Resume();Check.Equal(phase is RestPhase.Standing or RestPhase.StandingUp?RestPhase.Standing:RestPhase.Seated,r.Phase);
                r.Cancel();r.AnimationFinished();Check.Equal(RestPhase.Standing,r.Phase);Check.True(!r.MenuOpen&&!r.Suspended);
            }
        }));
    }
}
