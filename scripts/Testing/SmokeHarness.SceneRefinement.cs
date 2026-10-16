using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    // Catches misplaced wall/floor art, unreadable or distant names, oversized HUDs,
    // and hints that advertise controls unavailable in the current state.
    private async Task SceneRefinementChecks()
    {
        var failures=new List<string>();
        void Check(string name,Action assertion)
        {
            try{assertion();GD.Print("REFINEMENT_PASS "+name);}
            catch(Exception ex){failures.Add(name+": "+ex.Message);GD.Print("REFINEMENT_FAIL "+name+": "+ex.Message);}
        }
        var session=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            session.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();
            try
            {
                Check("CompactGoal-"+font,()=>{
                    var card=main.GetNode<PanelContainer>("HUD/TaskCard");var label=card.GetNode<Label>("TaskText");
                    var textWidth=label.GetThemeFont("font").GetStringSize(label.Text,fontSize:font).X;
                    Require(card.Size.X<=Math.Max(248,textWidth+56),"Short goal retains empty full-width padding");
                });
                Check("HudFits-"+font,()=>AssertRefinementHud(main));
                await Capture("refinement-community-"+font);
                Require(main.ChangeWorld("convenience_street",new(800,520)),"Refinement street fixture failed");await Frames(4);
                var world=main.World;
                Check("RegisteredBackground-"+font,()=>Require(world.GetNode<Sprite2D>("Backdrop").Material is ShaderMaterial,"Background is still using unregistered roof coordinates"));
                if(captureDirectory!=null)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    Check("BackgroundPavementPixels-"+font,()=>{
                        var viewport=main.GetNode<SubViewport>("WorldDisplay/WorldViewport");using var pixels=viewport.GetTexture().GetImage();
                        // Hand-checked source paving at x=708,y=502 maps to world (600,520).
                        // This checks the actual GPU output, including accidental double-texture tint.
                        var point=viewport.GetCanvasTransform()*new Vector2(600,520);
                        var color=pixels.GetPixel((int)point.X,(int)point.Y);var expected=new Color(212/255f,147/255f,109/255f);
                        GD.Print($"REFINEMENT_PAVEMENT pixel={point} actual={color} expected={expected}");
                        Require(Math.Abs(color.R-expected.R)<.16&&Math.Abs(color.G-expected.G)<.16&&Math.Abs(color.B-expected.B)<.16,"Main road texture is misplaced or darkened by a second texture sample");
                    });
                }
                Check("DrainOnPavement-"+font,()=>Require(new Rect2(1060,340,220,110).Encloses(RefinementSpriteBounds(world.GetNode<Sprite2D>("DepthLayers/Props/Drain/Artwork"))),"Drain is floating over the courtyard instead of resting on the street curb"));
                Check("PaintOnWall-"+font,()=>{
                    var bound=RefinementSpriteBounds(world.GetNode<Sprite2D>("DepthLayers/Props/WallPaint/Artwork"));GD.Print("REFINEMENT_PAINT_BOUNDS "+bound);
                    Require(new Rect2(740,588,140,36).Encloses(bound),"Paint must be on the south planter's visible wall face, not below the main actors");
                });
                Check("ActorGrounding-"+font,()=>{
                    foreach(var path in new[]{"DepthLayers/Actors/Player","DepthLayers/Actors/Hey","DepthLayers/Actors/ResidentNorth","DepthLayers/Actors/ResidentSouth"})AssertRefinementShadow(world.GetNode<Node2D>(path));
                });
                Check("FurnitureGrounding-"+font,()=>{
                    foreach(var name in new[]{"Newsstand","BikeShed","LaundryRack","StreetStool"}){
                        var prop=world.GetNode<Node2D>("DepthLayers/Props/"+name);AssertRefinementShadow(prop);
                        var bound=RefinementSpriteBounds(prop.GetNode<Sprite2D>("Artwork"));
                        GD.Print($"REFINEMENT_CONTACT {name} bottom={bound.End.Y} foot={prop.GlobalPosition.Y}");
                        Require(Math.Abs(bound.End.Y-prop.GlobalPosition.Y)<=18,"Furniture does not reach its authored foot: "+name);
                    }
                });
                var hey=world.GetTarget("hey")!;var nameLabel=hey.GetNode<Label>("NameLabel");
                Check("NameReadable-"+font,()=>{
                    Require(nameLabel.Visible,"Nearby Hey name missing");
                    Require(nameLabel.GetThemeColor("font_outline_color").A>=.95f,"Name lacks opaque contrasting outline");
                    Require(nameLabel.GetThemeFontSize("font_size")>=font,"Name does not follow readable text size");
                });
                Check("ContextHint-"+font,()=>{
                    var prompt=main.GetNode<Label>("HUD/InteractionHint");
                    Require(prompt.Text.Contains("Hey哥")&&!prompt.Text.Contains("Tab")&&!prompt.Text.Contains("今日记事"),"Near interaction still repeats unrelated keyboard help");
                });
                await Capture("refinement-street-"+font);
                main.GetNode<Button>("HUD/TaskFold").EmitSignal(Button.SignalName.Pressed);await Frames(4);
                Check("ExpandedHudFits-"+font,()=>AssertRefinementHud(main));await Capture("refinement-goal-expanded-"+font);
                Require(session.SaveManual().Success,"Refinement isolated manual-save fixture failed");await Frames(4);
                Check("SaveFeedbackHudFits-"+font,()=>AssertRefinementHud(main));
                // Controlled presentation input: persistence is unchanged and tested separately.
                typeof(GameSession).GetProperty(nameof(GameSession.SaveMessage))!.SetValue(session,"旧进度已复制至v4；便利店街站位已适配，原档未修改。保存提示也应在大字号下完整换行，不遮住按钮。");await Frames(4);
                Check("LongFeedbackHudFits-"+font,()=>AssertRefinementHud(main));await Capture("refinement-notice-"+font);
                main.GetNode<Button>("HUD/TaskFold").EmitSignal(Button.SignalName.Pressed);
                world.Player.Position=new(800,220);await Frames(4);
                Check("DistantVerticalName-"+font,()=>Require(!nameLabel.Visible,"Hey name remains visible 280 units away on the other side of the street"));
                await Capture("refinement-north-"+font);
                session.SetOptions(session.Options with {Assistance=true},false);await Frames(4);
                Check("FourDirectionHelp-"+font,()=>Require(!main.GetNode<Label>("HUD/InteractionHint").Text.Contains("←→移动"),"Four-direction scene advertises only horizontal movement"));
                world.Player.Position=new(1220,420);await Frames(4);await Capture("refinement-curb-"+font);
                world.Player.Position=new(1380,420);await Frames(4);
                if(captureDirectory!=null)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    Check("EastSpurPavementPixels-"+font,()=>{
                        var viewport=main.GetNode<SubViewport>("WorldDisplay/WorldViewport");using var pixels=viewport.GetTexture().GetImage();
                        var point=viewport.GetCanvasTransform()*new Vector2(1330,400);var color=pixels.GetPixel((int)point.X,(int)point.Y);
                        var expected=new Color(118/255f,103/255f,117/255f);GD.Print($"REFINEMENT_EAST_PAVEMENT actual={color} expected={expected}");
                        Require(Math.Abs(color.R-expected.R)<.10&&Math.Abs(color.G-expected.G)<.10&&Math.Abs(color.B-expected.B)<.10,"East spur still displays the too-tall garden rather than its checked paving sample");
                    });
                }
                await Capture("refinement-east-"+font);
                world.Player.Position=new(810,650);await Frames(4);await Capture("refinement-wall-"+font);
                Require(main.ChangeWorld("soup_shop",new(460,450)),"Refinement soup fixture failed");await Frames(4);
                Check("SoupNameReadable-"+font,()=>{
                    var name=main.World.GetNode<Label>("DepthLayers/Actors/Cannon/NameLabel");
                    Require(name.Visible&&name.GetThemeFontSize("font_size")>=font&&name.GetThemeColor("font_outline_color").A>=.95f,"Soup name is unreadable or missing");
                });
                await Capture("refinement-soup-"+font);
                var player=main.World.Player;
                player.SetRestPose("seated",new(30,-20),false);
                Check("RestShadowNotDetached-"+font,()=>Require(!player.GetNode<Polygon2D>("GroundShadow").Visible,"Standing shadow remains detached from shifted bench pose"));
                player.ClearRestPose();
                player.SetSoupPose("seated",new(30,-30),false);
                Check("SoupShadowNotDetached-"+font,()=>Require(!player.GetNode<Polygon2D>("GroundShadow").Visible,"Standing shadow remains detached from shifted soup pose"));
                player.ClearSoupPose();
                Check("StandingShadowRestored-"+font,()=>Require(player.GetNode<Polygon2D>("GroundShadow").Visible,"Standing contact shadow not restored after rising"));
                main.Pause.Open();await Frames(3);
                Check("PauseHintHidden-"+font,()=>Require(!main.GetNode<Label>("HUD/InteractionHint").Visible,"World controls visible inside pause"));
                main.Pause.Close();
            }
            finally{main.Free();await Frames(3);}
        }
        if(failures.Count>0)throw new Exception(string.Join(" | ",failures));
    }

    private static void AssertRefinementHud(MainView main)
    {
        var card=main.GetNode<Control>("HUD/TaskCard");var fold=main.GetNode<Control>("HUD/TaskFold");var journal=main.GetNode<Control>("HUD/JournalButton");
        Require(main.GetGlobalRect().Encloses(card.GetGlobalRect())&&main.GetGlobalRect().Encloses(fold.GetGlobalRect()),"Goal outside viewport");
        Require(!card.GetGlobalRect().Intersects(fold.GetGlobalRect())&&!fold.GetGlobalRect().Intersects(journal.GetGlobalRect())&&!card.GetGlobalRect().Intersects(journal.GetGlobalRect()),"Goal overlaps its buttons");
        var text=main.GetNode<Label>("HUD/TaskCard/TaskText");Require(card.GetGlobalRect().Encloses(text.GetGlobalRect()),"Task text clipped by compact card");
        var lines=text.GetLineCount();var fontSize=text.GetThemeFontSize("font_size");
        Require(text.Size.Y>=lines*text.GetThemeFont("font").GetHeight(fontSize)+(lines-1)*text.GetThemeConstant("line_spacing")-1,"Wrapped feedback lines clipped inside label");
    }

    private static Rect2 RefinementSpriteBounds(Sprite2D sprite)
    {
        using var image=sprite.Texture.GetImage();var used=(Rect2)image.GetUsedRect();
        var origin=sprite.Centered?sprite.Texture.GetSize()/2:Vector2.Zero;
        var points=new[]{used.Position,used.Position+new Vector2(used.Size.X,0),used.End,used.Position+new Vector2(0,used.Size.Y)}.Select(p=>sprite.GlobalTransform*(p-origin)).ToArray();
        var minimum=new Vector2(points.Min(p=>p.X),points.Min(p=>p.Y));return new(minimum,new Vector2(points.Max(p=>p.X),points.Max(p=>p.Y))-minimum);
    }

    private static void AssertRefinementShadow(Node2D owner)
    {
        var shadow=owner.GetNodeOrNull<Polygon2D>("GroundShadow");
        Require(shadow!=null&&shadow.Color.A>0&&shadow.Color.A<.3f,"Missing subtle authored ground contact: "+owner.Name);
        Require(shadow!.Position.Length()<2&&shadow.Polygon.Length>=8,"Contact shadow is detached from the foot: "+owner.Name);
    }
}
