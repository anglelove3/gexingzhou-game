using Godot;
using GeXingzhou.Domain;
public enum JournalPage { Current,History,Discoveries }
public partial class JournalController : Control
{
    private MainView main=null!;
    private PanelContainer panel=null!;private ScrollContainer scroll=null!;
    private Label text=null!,empty=null!;private Button[] tabs=Array.Empty<Button>();
    private Button close=null!;private Control? previousFocus;private JournalView? view;
    public bool IsOpen {get;private set;}
    public void Configure(MainView owner)=>main=owner;
    public override void _Ready()
    {
        try
        {
            SceneBindings.Require<ColorRect>(this,"Dim");SceneBindings.Require<Label>(this,"StartupError");
            panel=SceneBindings.Require<PanelContainer>(this,"Panel");SceneBindings.Require<Label>(this,"Panel/Content/Title");
            scroll=SceneBindings.Require<ScrollContainer>(this,"Panel/Content/BodyScroll");
            text=SceneBindings.Require<Label>(this,"Panel/Content/BodyScroll/Body/Text");
            empty=SceneBindings.Require<Label>(this,"Panel/Content/BodyScroll/Body/Empty");
            tabs=new[]{"Current","History","Discoveries"}.Select(n=>SceneBindings.Require<Button>(this,"Panel/Content/Tabs/"+n)).ToArray();
            close=SceneBindings.Require<Button>(this,"Panel/Content/CloseButton");close.Pressed+=Close;
            for(int i=0;i<tabs.Length;i++){var page=(JournalPage)i;tabs[i].Pressed+=()=>ShowPage(page);}
            var buttons=tabs.Append(close).ToArray();
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].FocusNext=buttons[i].GetPathTo(buttons[(i+1)%buttons.Length]);
                buttons[i].FocusPrevious=buttons[i].GetPathTo(buttons[(i+buttons.Length-1)%buttons.Length]);
                buttons[i].FocusNeighborLeft=buttons[i].FocusPrevious;
                buttons[i].FocusNeighborRight=buttons[i].FocusNext;
                buttons[i].FocusNeighborTop=buttons[i].GetPathTo(i==tabs.Length?tabs[0]:buttons[i]);
                buttons[i].FocusNeighborBottom=buttons[i].GetPathTo(close);
            }
            Resized+=LayoutPanel;LayoutPanel();Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    private void LayoutPanel()
    {
        if(panel==null)return;
        var width=Math.Min(900,Math.Max(0,Size.X-48));var height=Math.Min(720,Math.Max(0,Size.Y-48));
        panel.OffsetLeft=-width/2;panel.OffsetRight=width/2;panel.OffsetTop=-height/2;panel.OffsetBottom=height/2;
    }
    public bool Open()
    {
        if(IsOpen||HasMeta("binding_error")||!GodotObject.IsInstanceValid(main)||!main.CanOpenJournal)return false;
        previousFocus=GetViewport().GuiGetFocusOwner();view=JournalProjection.Build(GetNode<GameSession>("/root/GameSession").Snapshot);
        IsOpen=true;Visible=true;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Journal;
        main.Audio.SetPaused(true);ShowPage(JournalPage.Current);tabs[0].GrabFocus();return true;
    }
    public void Close()
    {
        if(!IsOpen)return;
        IsOpen=false;Visible=false;view=null;
        var session=GetNode<GameSession>("/root/GameSession");if(session.Flow==FlowState.Journal)session.Flow=FlowState.Field;
        main.Audio.SetPaused(false);
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;
    }
    public void ShowPage(JournalPage page)
    {
        if(!IsOpen||view==null)return;
        for(int i=0;i<tabs.Length;i++)tabs[i].SetPressedNoSignal(i==(int)page);
        var entries=page==JournalPage.History?view.History:view.Discoveries;
        var isEmpty=page!=JournalPage.Current&&entries.Count==0;
        text.Visible=!isEmpty;empty.Visible=isEmpty;
        empty.Text=page==JournalPage.History?"还没有记下已完成的事":"还没有记下见闻。走走看看，也不必找齐。";
        text.Text=page==JournalPage.Current?view.CurrentGoal:string.Join("\n\n",entries.Select(e=>e.Title+(e.Body.Length>0?"\n"+e.Body:"")));
        scroll.ScrollVertical=0;
    }
    public bool HandleKey(Key key)
    {
        if(!IsOpen)return false;
        var step=Math.Max(32,(int)scroll.Size.Y-16);
        if(key is Key.J or Key.Escape)Close();
        else if(key==Key.Pagedown)scroll.ScrollVertical+=step;
        else if(key==Key.Pageup)scroll.ScrollVertical-=step;
        else if(key==Key.Home)scroll.ScrollVertical=0;
        else if(key==Key.End)scroll.ScrollVertical=int.MaxValue;
        else if(key is not (Key.E or Key.F5 or Key.F9))return false;
        return true;
    }
}
