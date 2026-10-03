using System.Text.Json;
using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class SettingsEventChecks
{
    private static string Temp()=>Path.GetFullPath(Path.Combine("test-output","events-"+Guid.NewGuid()));
    private static BehaviorEvent Event(string id="slice.complete",string opportunity="slice-1",string code="completed")=>new(){EventId=id,OpportunityId=opportunity,ChoiceCode=code,PlaythroughId="test-run"};
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Settings","ReadableBoundsAndZeroSpeed",()=>{
            Check.Equal(20,new GameSettings{SubtitleSize=1}.Normalize().SubtitleSize);Check.Equal(32,new GameSettings{SubtitleSize=99}.Normalize().SubtitleSize);
            Check.Equal(0,new GameSettings{TextSpeed=0}.Normalize().TextSpeed);Check.Equal(15,new GameSettings{TextSpeed=1}.Normalize().TextSpeed);Check.Equal(60,new GameSettings{TextSpeed=99}.Normalize().TextSpeed);
            Check.Equal(.15,new GameSettings{ReducedMotion=true}.FadeDuration);
        }));
        tests.Add(("Events","DisabledCreatesNothing",()=>{
            var dir=Temp();var r=new LocalEventRecorder(dir);r.TryAppend(Event());Check.True(!Directory.Exists(dir));
        }));
        tests.Add(("Events","DedupAndSequenceSurviveNewRecorder",()=>{
            var dir=Temp();var r=new LocalEventRecorder(dir,true);Check.True(r.TryAppend(Event()).Success);Check.True(r.TryAppend(Event()).Success);
            r=new LocalEventRecorder(dir,true);Check.True(r.TryAppend(Event("candy.hey.delivered","hey-1","delivered")).Success);
            var lines=File.ReadAllLines(Path.Combine(dir,"events.jsonl"));Check.Equal(2,lines.Length);Check.Equal(1L,JsonDocument.Parse(lines[0]).RootElement.GetProperty("sequence").GetInt64());Check.Equal(2L,JsonDocument.Parse(lines[1]).RootElement.GetProperty("sequence").GetInt64());
        }));
        tests.Add(("Events","BadLineExportAndClearOnlyEvents",()=>{
            var dir=Temp();Directory.CreateDirectory(dir);var r=new LocalEventRecorder(dir,true);Check.True(r.TryAppend(Event()).Success);File.AppendAllText(Path.Combine(dir,"events.jsonl"),"bad\n");
            File.WriteAllText(Path.Combine(dir,"save.json"),"keep");r=new LocalEventRecorder(dir,true);Check.Equal(1,r.SkippedLines);var export=Path.Combine(dir,"export.jsonl");Check.True(r.Export(export).Success);Check.Equal(1,r.SkippedLines);Check.Equal(1,File.ReadAllLines(export).Length);
            Check.True(r.Clear().Success);Check.Equal("keep",File.ReadAllText(Path.Combine(dir,"save.json")));Check.Equal(0,File.ReadAllLines(Path.Combine(dir,"events.jsonl")).Length);
        }));
        tests.Add(("Events","WriteErrorAndPrivateChoiceRejected",()=>{
            var dir=Temp();Directory.CreateDirectory(dir);var file=Path.Combine(dir,"blocked");File.WriteAllText(file,"keep");Check.True(!new LocalEventRecorder(file,true).TryAppend(Event()).Success);
            Check.True(!new LocalEventRecorder(Path.Combine(dir,"safe"),true).TryAppend(Event(code:"私人聊天内容")).Success);Check.Equal("keep",File.ReadAllText(file));
        }));
    }
}
