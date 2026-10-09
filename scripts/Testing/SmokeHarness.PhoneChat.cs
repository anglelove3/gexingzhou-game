using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    // Breaks caught: square phone, unreadable bubbles, absent empty/call states,
    // stale scrolling, cosmetics advancing the invitation, or losing the source modal.
    private async Task PhoneChatChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        var window=GetWindow();var oldSize=window.Size;var oldMode=window.ContentScaleMode;
        if(captureDirectory==null)window.ContentScaleMode=Window.ContentScaleModeEnum.Disabled;
        var sizes=captureDirectory==null?new[]{new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(1440,1080)}:new[]{window.Size};
        foreach(var size in sizes)foreach(var font in new[]{20,24,32})
        {
            if(captureDirectory==null)window.Size=size;
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();main.Phone.Open("messages");await Frames(5);
            var frame=main.Phone.GetNode<PanelContainer>("Frame");
            Require(frame.Size.Y>frame.Size.X*1.4f,"Phone must have a portrait handset silhouette, not a square panel");
            Require(main.GetGlobalRect().Encloses(frame.GetGlobalRect()),"Phone frame outside window");
            var empty=main.Phone.GetNodeOrNull<Label>("Frame/Content/MessageScroll/Messages/Empty");
            Require(empty!=null&&empty.IsVisibleInTree(),"Phone lacks readable empty chat state");
            var row=main.Phone.GetNode<Control>("Frame/Content/MessageScroll/Messages/Incoming");
            var body=main.Phone.GetNode<Label>("Frame/Content/MessageScroll/Messages/Incoming/Bubble/Text");
            var close=main.Phone.GetNode<Button>("Frame/Content/CloseButton");
            var focusedText=close.GetThemeColor("font_focus_color");
            Require(close.HasFocus()&&focusedText.R*.2126f+focusedText.G*.7152f+focusedText.B*.0722f<.5f,"Focused close button text disappears on white surface");
            Require(!row.IsVisibleInTree()&&!main.Phone.GetNode<Button>("Frame/Content/AnswerButton").IsVisibleInTree(),"Empty chat leaks future message/call");
            Require(body.GetThemeFontSize("font_size")==font,"Phone shrunk chat font");
            await Capture("chat-empty-"+font);main.Phone.Close();s.AdvanceClock(8);
            var elapsed=s.Snapshot.InvitationState.Elapsed;var stage=s.Snapshot.Stage;var facts=s.Snapshot.CompletedActions.ToArray();
            main.Phone.Open("messages");await Frames(5);
            Require(row.IsVisibleInTree()&&!empty!.IsVisibleInTree()&&body.Text.Contains("我要结婚了"),"Received invitation not shown in chat bubble");
            Require(main.Phone.GetNode<Control>("Frame/Content/MessageScroll/Messages/Incoming/Avatar").IsVisibleInTree(),"Incoming message lacks avatar");
            Require(main.Phone.GetNode<Label>("Frame/Content/Contact").Text=="张大炮","Contact header mixed with task/call text");
            s.AdvanceClock(60);await Frames(2);Require(s.Snapshot.InvitationState.Elapsed==elapsed&&s.Snapshot.Stage==stage&&s.Snapshot.CompletedActions.SetEquals(facts),"Reading phone changed clock/story");
            await Capture("chat-message-"+font);
            var scroll=main.Phone.GetNode<ScrollContainer>("Frame/Content/MessageScroll");
            var full=string.Join('\n',Enumerable.Repeat("葛大爷，等会儿来找你。",30));body.Text=full;await Frames(5);
            Require(body.Text==full,"Long chat text truncated");KeyPress(Key.Pagedown);await Frames(3);
            Require(scroll.ScrollVertical>0,"Keyboard PageDown cannot read long chat");
            scroll.ScrollVertical=0;var wheelPoint=scroll.GetGlobalRect().GetCenter();
            main.GetViewport().PushInput(new InputEventMouseButton{Position=wheelPoint,GlobalPosition=wheelPoint,ButtonIndex=MouseButton.WheelDown,Pressed=true},true);await Frames(3);
            Require(scroll.ScrollVertical>0,"Mouse wheel cannot read long chat");scroll.ScrollVertical=100000;await Frames(3);
            Require(scroll.ScrollVertical>0,"Long chat cannot scroll");
            foreach(var path in new[]{"Frame/Content/Contact","Frame/Content/Task","Frame/Content/CloseButton"})Require(frame.GetGlobalRect().Encloses(main.Phone.GetNode<Control>(path).GetGlobalRect()),"Phone fixed control clipped "+path);
            await Capture("chat-long-"+font);main.Phone.Close();main.Phone.Open("messages");await Frames(5);
            Require(body.Text.Contains("我要结婚了")&&scroll.ScrollVertical==0,"Reopen retained test text/scroll");main.Phone.Close();s.AdvanceClock(15);main.Phone.Open("messages");await Frames(5);
            var answer=main.Phone.GetNode<Button>("Frame/Content/AnswerButton");
            Require(answer.IsVisibleInTree()&&answer.HasFocus()&&frame.GetGlobalRect().Encloses(answer.GetGlobalRect()),"Incoming call cannot be answered by keyboard");
            Require(main.Phone.GetNode<Control>("Frame/Content/MessageScroll/Messages/CallNotice").IsVisibleInTree(),"Incoming call has no separate notice");
            await Capture("chat-call-"+font);KeyPress(Key.Enter);await Frames(3);
            Require(!main.Phone.IsOpen&&main.Dialogue.IsOpen&&s.Snapshot.InvitationState.Resolution==InvitationResolution.Answered,"Chat answer lost existing story dialogue");
            await Finish(main);main.Phone.Open("messages");await Frames(5);
            Require(main.Phone.GetNode<Control>("Frame/Content/MessageScroll/Messages/Outgoing").IsVisibleInTree()&&!answer.IsVisibleInTree(),"Answered call history missing/re-answer offered");
            await Capture("chat-answered-"+font);main.Phone.Close();
            main.Dialogue.ShowText("葛行舟","我把手机放回口袋。");await Frames(3);var focus=GetViewport().GuiGetFocusOwner();
            main.Phone.Open("messages");await Frames(3);KeyPress(Key.Escape);await Frames(3);
            Require(main.Dialogue.IsOpen&&main.Dialogue.Visible&&s.Flow==FlowState.Dialogue&&GetViewport().GuiGetFocusOwner()==focus,"Phone close lost dialogue/focus");
            main.Dialogue.HandleKey(Key.Escape);main.Free();await Frames(3);
        }
        s.SetOptions(new(){SubtitleSize=24,TextSpeed=0,ReducedMotion=true},false);
        var ignored=await NewPolishMain();s.AdvanceClock(65);
        Require(s.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1")).Applied,"Unanswered fixture could not meet friend");
        Require(s.Saves.Load().Status==LoadStatus.Loaded,"Unanswered meeting fixture failed real checkpoint validation");
        ignored.Phone.Open("messages");await Frames(4);
        Require(ignored.Phone.GetNode<Label>("Frame/Content/MessageScroll/Messages/CallNotice").Text=="未接听语音通话"&&ignored.Phone.GetNode<Label>("Frame/Content/MessageScroll/Messages/CallNotice").IsVisibleInTree(),"Ignored call history falsely claims answered");
        Require(!ignored.Phone.GetNode<Control>("Frame/Content/MessageScroll/Messages/Outgoing").IsVisibleInTree()&&!ignored.Phone.GetNode<Button>("Frame/Content/AnswerButton").IsVisibleInTree(),"Ignored call can still be answered after meeting");
        await Capture("chat-missed");ignored.Phone.Close();ignored.Free();await Frames(3);
        if(captureDirectory==null){window.Size=oldSize;window.ContentScaleMode=oldMode;}
        GD.Print("PHONE_CHAT_PASS Portrait Empty Received Call Answered Scroll Font SourceRestoration ClockIsolation");
    }
}
