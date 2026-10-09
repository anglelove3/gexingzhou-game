namespace GeXingzhou.Domain;
public enum ResumeSource { V2, Legacy, Missing, Blocked }
public sealed record ResumeOffer(ResumeSource Source,LoadResult Result);
public static class SaveUpgradePolicy
{
    public static ResumeOffer Probe(SaveRepository v2,SaveRepository legacy,bool backup=false)
    {
        var main=v2.Load();var alternate=v2.LoadBackup();
        if(main.Status!=LoadStatus.NotFound||alternate.Status!=LoadStatus.NotFound){
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?ResumeSource.V2:ResumeSource.Blocked,selected);
        }
        var old=backup?legacy.LoadBackup():legacy.Load();
        return new(old.Status==LoadStatus.Loaded?ResumeSource.Legacy:old.Status==LoadStatus.NotFound?ResumeSource.Missing:ResumeSource.Blocked,old);
    }
}
