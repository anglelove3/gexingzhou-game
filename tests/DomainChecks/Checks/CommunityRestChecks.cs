using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class CommunityRestChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        var nav=CommunityFixtures.Current().Profiles["community_gate"];
        tests.Add(("CommunityRest","DirectBounded",()=>Verify(nav,new(930,440),new(960,440),72,2)));
        tests.Add(("CommunityRest","BlockedShortDetour",()=>{
            var detour=nav with{Obstacles=nav.Obstacles.Concat(new[]{new[]{new Position2(944,432),new(948,432),new(948,448),new(944,448)}}).ToArray()};
            Check.True(!NavigationGeometry.CanTraverse(detour,new(930,440),new(960,440)));
            Verify(detour,new(930,440),new(960,440),72,3);
        }));
        tests.Add(("CommunityRest","RejectUnsafeAndOverBudget",()=>{
            foreach(var point in new[]{new Position2(930,380),new(-1,440),new(float.NaN,440)})Check.True(ShortApproachPolicy.TryPlan(nav,point,new(960,440))==null);
            Check.True(ShortApproachPolicy.TryPlan(nav,new(930,440),new(960,440),29)==null);
            Check.True(ShortApproachPolicy.TryPlan(nav,new(930,440),new(960,440),float.NaN)==null);
        }));
        tests.Add(("CommunityRest","SamePointZeroReadOnly",()=>{
            var path=ShortApproachPolicy.TryPlan(nav,new(960,440),new(960,440),0);Check.True(path!=null);Check.Equal(0f,path!.Length);Check.Equal(1,path.Points.Count);
            if(path.Points is IList<Position2> list){bool immutable=false;try{list[0]=new(120,460);}catch(NotSupportedException){immutable=true;}Check.True(immutable);}
        }));
        tests.Add(("CommunityRest","ResumeKeepsPhaseAndMenu",()=>{
            foreach(var phase in new[]{RestPhase.SittingDown,RestPhase.Seated,RestPhase.Smoking,RestPhase.StandingUp}){
                var rest=new RestStateMachine();rest.TrySit();if(phase!=RestPhase.SittingDown)rest.AnimationFinished();
                if(phase==RestPhase.Smoking)rest.TryChoose(RestChoice.Smoke);if(phase==RestPhase.StandingUp)rest.TryChoose(RestChoice.Rise);
                var menu=rest.MenuOpen;rest.Suspend();rest.Suspend();rest.AnimationFinished();rest.Resume();Check.Equal(phase,rest.Phase);Check.Equal(menu,rest.MenuOpen);
            }
        }));
    }
    private static void Verify(NavigationProfile nav,Position2 from,Position2 to,float budget,int minimumCount)
    {
        var path=ShortApproachPolicy.TryPlan(nav,from,to,budget);Check.True(path!=null);Check.True(path!.Points.Count>=minimumCount);Check.Equal(from,path.Points[0]);Check.Equal(to,path.Points[^1]);
        float length=0;for(int i=1;i<path.Points.Count;i++){Check.True(NavigationGeometry.CanTraverse(nav,path.Points[i-1],path.Points[i]));length+=NavigationGeometry.Distance(path.Points[i-1],path.Points[i]);}
        Check.True(Math.Abs(length-path.Length)<.01f&&length<=budget+.001f);
    }
}
