namespace GeXingzhou.Domain;

public static class SaveV4UpgradePolicy
{
    public static VersionedResumeOffer Probe(SaveRepository current,SaveRepository v3,SaveRepository v2,SaveRepository v1,bool backup=false)
    {
        var main=current.Load();var alternate=current.LoadBackup();
        if(main.Status!=LoadStatus.NotFound||alternate.Status!=LoadStatus.NotFound)
        {
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?VersionedResumeSource.Current:VersionedResumeSource.Blocked,selected);
        }
        main=v3.Load();alternate=v3.LoadBackup();
        if(main.Status!=LoadStatus.NotFound||alternate.Status!=LoadStatus.NotFound)
        {
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?VersionedResumeSource.V3:VersionedResumeSource.Blocked,selected);
        }
        main=v2.Load();alternate=v2.LoadBackup();
        if(main.Status!=LoadStatus.NotFound||alternate.Status!=LoadStatus.NotFound)
        {
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?VersionedResumeSource.V2:VersionedResumeSource.Blocked,selected);
        }
        main=v1.Load();alternate=v1.LoadBackup();
        if(main.Status!=LoadStatus.NotFound||alternate.Status!=LoadStatus.NotFound)
        {
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?VersionedResumeSource.Legacy:VersionedResumeSource.Blocked,selected);
        }
        return new(VersionedResumeSource.Missing,new(LoadStatus.NotFound,null,"暂无存档。"));
    }
}
