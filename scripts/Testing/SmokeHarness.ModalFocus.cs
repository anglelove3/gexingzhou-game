using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task ModalFocusChecks()
    {
        var failures=new List<string>();
        void Tab(bool reverse)
        {
            foreach(var pressed in new[]{true,false})Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.Tab,Keycode=Key.Tab,ShiftPressed=reverse,Pressed=pressed});
        }
        async Task Boundary(Control modal,IReadOnlyList<Control> controls,string name,bool tab=true,bool arrows=true,bool horizontal=false)
        {
            for(int i=0;i<controls.Count;i++)
            {
                var control=controls[i];
                foreach(var key in arrows?new[]{Key.Up,Key.Down,Key.Left,Key.Right}:Array.Empty<Key>())
                {
                    control.GrabFocus();KeyPress(key);await Frames(2);
                    var focus=GetViewport().GuiGetFocusOwner();if(focus==null||!modal.IsAncestorOf(focus))failures.Add(name+" escaped with "+key);
                    var step=key==(horizontal?Key.Right:Key.Down)?1:key==(horizontal?Key.Left:Key.Up)?-1:0;
                    if(focus!=controls[(i+step+controls.Count)%controls.Count])failures.Add(name+" wrong visual order with "+key);
                }
                if(!tab)continue;
                foreach(var reverse in new[]{false,true})
                {
                    control.GrabFocus();Tab(reverse);await Frames(2);
                    var focus=GetViewport().GuiGetFocusOwner();if(focus==null||!modal.IsAncestorOf(focus))failures.Add(name+" escaped with "+(reverse?"ShiftTab":"Tab"));
                    if(focus!=controls[(i+(reverse?-1:1)+controls.Count)%controls.Count])failures.Add(name+" wrong visual order with "+(reverse?"ShiftTab":"Tab"));
                }
            }
            GD.Print("MODAL_FOCUS_CASE_DONE "+name);
        }
        var main=await NewPolishMain();var session=GetNode<GameSession>("/root/GameSession");var chosen=0;var activations=0;
        foreach(var count in new[]{1,3})
        {
            main.Choices.Open("测试键盘选项",Enumerable.Range(1,count).Select(i=>("选项"+i,(Action)(()=>{chosen=i;activations++;}))).ToArray());await Frames(3);
            var buttons=main.Choices.GetNode("Panel/Content/Options").GetChildren().OfType<Button>().ToArray();
            await Boundary(main.Choices,buttons,"Choices"+count);
            buttons[0].GrabFocus();KeyPress(Key.Enter);await Frames(2);
            Require(chosen==1&&activations==1&&!main.Choices.IsOpen,"Choice Enter did not invoke the selected action once");chosen=0;activations=0;
        }
        main.Phone.Open("messages");await Frames(2);
        await Boundary(main.Phone,new[]{main.Phone.GetNode<Button>("Frame/Content/CloseButton")},"PhoneEmpty",false);KeyPress(Key.Escape);await Frames(2);
        session.AdvanceClock(23);await Frames(2);main.Phone.Open("messages");await Frames(2);
        var answer=main.Phone.GetNode<Button>("Frame/Content/AnswerButton");var close=main.Phone.GetNode<Button>("Frame/Content/CloseButton");
        await Boundary(main.Phone,new[]{answer,close},"PhoneCall",false);
        answer.GrabFocus();KeyPress(Key.Enter);await Frames(2);Require(!main.Phone.IsOpen&&main.Dialogue.IsOpen,"Phone Enter answer changed");await Finish(main);
        Require(main.Pause.Open(),"Pause fixture rejected");await Frames(2);
        var pauseButtons=main.Pause.GetNode("Panel/Scroll/Content").GetChildren().OfType<Button>().ToArray();
        await Boundary(main.Pause,pauseButtons,"Pause");
        main.Pause.GetNode<Button>("Panel/Scroll/Content/Controls").EmitSignal(Button.SignalName.Pressed);await Frames(3);pauseButtons[^1].GrabFocus();await Frames(3);
        if(!main.Pause.GetNode<ScrollContainer>("Panel/Scroll").GetGlobalRect().Grow(.5f).Encloses(pauseButtons[^1].GetGlobalRect()))failures.Add("Pause focused bottom action stayed clipped behind long help");
        main.Settings.Open();await Frames(2);
        var settings=main.Settings;var prefix="Panel/Scroll/Content/";
        var settingsControls=new List<Control>{settings.GetNode<SpinBox>(prefix+"FontSize").GetLineEdit(),settings.GetNode<OptionButton>(prefix+"TextSpeed")};
        settingsControls.AddRange(new[]{"Assistance","ReducedMotion","RecordEvents","MasterVolume","MusicVolume","EffectsVolume","EnvironmentVolume","ClearButton","ExportButton","CloseButton"}.Select(n=>settings.GetNode<Control>(prefix+n)));
        await Boundary(settings,settingsControls,"Settings",true,false);
        settingsControls[^1].GrabFocus();await Frames(3);
        if(!settings.GetNode<ScrollContainer>("Panel/Scroll").GetGlobalRect().Grow(.5f).Encloses(settingsControls[^1].GetGlobalRect()))failures.Add("Settings focused close button stayed clipped");
        var slider=settings.GetNode<HSlider>(prefix+"MasterVolume");slider.GrabFocus();var before=slider.Value;
        KeyPress(Key.Left);await Frames(2);Require(slider.Value<before&&slider.HasFocus(),"Settings slider arrow stopped adjusting value");
        KeyPress(Key.Escape);await Frames(2);Require(!settings.IsOpen&&main.Pause.IsOpen,"Settings Esc did not restore pause");
        KeyPress(Key.Escape);await Frames(2);Require(!main.Pause.IsOpen&&session.Flow==FlowState.Field,"Pause Esc changed");main.Free();await Frames(3);
        await MemoryPath(false,0,false);
        var ending=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(ending);await Frames(3);
        Require(ending.SliceEnd.IsOpen,"Ending fixture absent");
        var endButtons=ending.SliceEnd.GetNode("Panel/Actions").GetChildren().OfType<Button>().ToArray();
        await Boundary(ending.SliceEnd,endButtons,"Ending",horizontal:true);
        endButtons[0].GrabFocus();KeyPress(Key.Enter);await Frames(2);Require(!ending.SliceEnd.IsOpen&&session.Flow==FlowState.Field,"Ending Continue key changed");ending.Free();await Frames(3);
        var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(3);
        var bootSettings=boot.GetNode<SettingsController>("Settings");bootSettings.Open();await Frames(2);KeyPress(Key.Escape);await Frames(2);
        if(bootSettings.IsOpen)failures.Add("BootSettings Escape did not close settings");bootSettings.Close();boot.Free();await Frames(3);
        if(failures.Count>0){foreach(var failure in failures.Distinct())GD.Print("MODAL_FOCUS_FAIL "+failure);throw new Exception("Modal keyboard focus escaped: "+failures.Count);}
        GD.Print("MODAL_FOCUS_PASS Choices Phone Pause Settings Ending Tab ShiftTab Arrows NativeSlider Enter Esc");
    }
}
