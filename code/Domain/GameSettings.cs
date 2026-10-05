namespace GeXingzhou.Domain;
public sealed record GameSettings
{
    public int SubtitleSize {get;init;}=24;
    public int TextSpeed {get;init;}=30;
    public bool Assistance {get;init;}
    public bool ReducedMotion {get;init;}
    public bool RecordEventsEnabled {get;init;}
    public double MasterVolume {get;init;}=.8;
    public double MusicVolume {get;init;}=.4;
    public double EffectsVolume {get;init;}=.7;
    public double EnvironmentVolume {get;init;}=.35;
    public double FadeDuration=>ReducedMotion?.15:.65;
    private static double Volume(double value,double fallback)=>double.IsFinite(value)?Math.Clamp(value,0,1):fallback;
    public GameSettings Normalize()=>this with {SubtitleSize=Math.Clamp(SubtitleSize,20,32),TextSpeed=TextSpeed==0?0:Math.Clamp(TextSpeed,15,60),MasterVolume=Volume(MasterVolume,.8),MusicVolume=Volume(MusicVolume,.4),EffectsVolume=Volume(EffectsVolume,.7),EnvironmentVolume=Volume(EnvironmentVolume,.35)};
}
