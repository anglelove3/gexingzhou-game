using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class MovementChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        foreach(var (name,v,a,fast,locked,dt,want) in new[] {
            ("Accelerates",0f,1f,false,false,.1f,80f), ("Stops",112f,0f,false,false,.1f,0f),
            ("CapsWalk",0f,1f,false,false,1f,112f),("CapsRun",0f,-1f,true,false,1f,-168f),
            ("Locks",112f,1f,false,true,.1f,0f),("ZeroDt",100f,1f,false,false,0f,100f) })
        tests.Add(("Movement",name,()=>Check.Equal(want,MovementModel.Step(v,a,fast,locked,dt))));
        tests.Add(("Interaction","BoundaryAndOcclusion",()=>{
            var p=new InteractionPolicy(); var ts=new[]{new InteractionCandidate("b",40,true),new InteractionCandidate("a",-40,true),new InteractionCandidate("wall",0,false)};
            Check.Equal("a",p.Select(ts,null,0));
            Check.Equal("b",p.Select(new[]{new InteractionCandidate("b",48,true)},"b",0));
            Check.Equal<string?>(null,p.Select(new[]{new InteractionCandidate("b",48.1f,true)},"b",0));
            Check.Equal<string?>(null,p.Select(new[]{new InteractionCandidate("wall",0,false)},null,0));
        }));
        tests.Add(("Interaction","DebouncesGlobally",()=>{
            var p=new InteractionPolicy(); Check.True(p.TryActivate("a",0)); Check.True(!p.TryActivate("a",.1));
            Check.True(!p.TryActivate("b",.2)); Check.True(p.TryActivate("b",.25));
        }));
    }
}
