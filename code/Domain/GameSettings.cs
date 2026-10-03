namespace GeXingzhou.Domain;
public sealed record GameSettings
{
    public int SubtitleSize {get;init;}=24;
    public int TextSpeed {get;init;}=30;
    public bool Assistance {get;init;}
    public bool ReducedMotion {get;init;}
    public bool RecordEventsEnabled {get;init;}
    public double FadeDuration=>ReducedMotion?.15:.65;
    public GameSettings Normalize()=>this with {SubtitleSize=Math.Clamp(SubtitleSize,20,32),TextSpeed=TextSpeed==0?0:Math.Clamp(TextSpeed,15,60)};
}
