namespace GeXingzhou.Domain;
public enum VersionedResumeSource {Current,V2,Legacy,Missing,Blocked}
public sealed record VersionedResumeOffer(VersionedResumeSource Source,LoadResult Result);
public static class SaveV3UpgradePolicy
{
    public static VersionedResumeOffer Probe(SaveRepository current,SaveRepository v2,SaveRepository v1,bool backup=false)
    {
        foreach(var (repository,source) in new[]{(current,VersionedResumeSource.Current),(v2,VersionedResumeSource.V2),(v1,VersionedResumeSource.Legacy)}){
            var main=repository.Load();var alternate=repository.LoadBackup();
            if(main.Status==LoadStatus.NotFound&&alternate.Status==LoadStatus.NotFound)continue;
            var selected=backup?alternate:main;
            return new(selected.Status==LoadStatus.Loaded?source:VersionedResumeSource.Blocked,selected);
        }
        return new(VersionedResumeSource.Missing,new(LoadStatus.NotFound,null,"暂无存档。"));
    }
}
