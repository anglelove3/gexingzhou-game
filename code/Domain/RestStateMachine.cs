namespace GeXingzhou.Domain;
public enum RestPhase { Standing,SittingDown,Seated,Smoking,StandingUp }
public enum RestChoice { Rest,Smoke,Rise }
public sealed class RestStateMachine
{
    public RestPhase Phase {get;private set;}=RestPhase.Standing;
    public bool MenuOpen {get;private set;}
    public bool Suspended {get;private set;}
    private bool suspendedMenu;
    public bool TrySit()
    {if(Suspended||Phase!=RestPhase.Standing)return false;Phase=RestPhase.SittingDown;MenuOpen=false;return true;}
    public void AnimationFinished()
    {
        if(Suspended)return;
        if(Phase==RestPhase.SittingDown){Phase=RestPhase.Seated;MenuOpen=true;}
        else if(Phase==RestPhase.Smoking)Phase=RestPhase.Seated;
        else if(Phase==RestPhase.StandingUp)Phase=RestPhase.Standing;
    }
    public bool TryChoose(RestChoice choice)
    {
        if(Suspended||Phase!=RestPhase.Seated||!MenuOpen||!Enum.IsDefined(choice))return false;
        MenuOpen=false;
        if(choice==RestChoice.Smoke)Phase=RestPhase.Smoking;
        else if(choice==RestChoice.Rise)Phase=RestPhase.StandingUp;
        return true;
    }
    public void HandleEscape()
    {
        if(Suspended||Phase==RestPhase.Standing)return;
        if(MenuOpen){MenuOpen=false;return;}
        if(Phase!=RestPhase.StandingUp)Phase=RestPhase.StandingUp;
    }
    public bool ReopenMenu()
    {if(Suspended||Phase!=RestPhase.Seated)return false;MenuOpen=true;return true;}
    public void Suspend(){if(Suspended)return;suspendedMenu=MenuOpen;Suspended=true;MenuOpen=false;}
    public void Resume()
    {
        if(!Suspended)return;Suspended=false;
        MenuOpen=suspendedMenu&&Phase==RestPhase.Seated;suspendedMenu=false;
    }
    public void Cancel(){Phase=RestPhase.Standing;MenuOpen=false;Suspended=false;suspendedMenu=false;}
}
