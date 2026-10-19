using Electron2D;
using SDL = SDL3.SDL;

internal static partial class NativePopupWindowTests
{
    private sealed class PopupInputProbe : Control
    {
        internal int Keys, Buttons, Touches;
        internal Vector2 LastPoint;
        internal long Origin;
        internal PopupInputProbe() { InputEnabled = true; MouseFilter = MouseFilter.Stop; }
        protected override void OnInput(InputEvent input) { if (input is InputEventKey { Pressed: true }) Keys++; }
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is InputEventMouseButton { Pressed: true } mouse) { Buttons++; LastPoint = mouse.Position; Origin = mouse.WindowID; }
            if (input is InputEventScreenTouch { Pressed: true } touch) { Touches++; LastPoint = touch.Position; }
        }
    }

    private static void InputPolicy(string backend)
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var root = new Window { Size = new(320, 240), GUIEmbedSubwindows = true }; var parentProbe = new PopupInputProbe { Size = new(320, 240) }; root.AddChild(parentProbe);
        var popup = new Popup { ForceNative = true, Exclusive = true, Size = new(80, 60) }; var probe = new PopupInputProbe { Size = new(80, 60) }; popup.AddChild(probe); root.AddChild(popup);
        var fitted = new Popup { ForceNative = true, Transparent = backend == "compatibility", TransparentBG = true, ContentScaleMode = ContentScaleMode.Viewport, ContentScaleSize = new(32, 32), ContentScaleAspect = ContentScaleAspect.Keep };
        fitted.AddChild(new ColorRect { Color = Colors.Green, Size = new(32, 32) }); root.AddChild(fitted);
        var embedded = new Window { Position = new(180, 20), Size = new(120, 96), Borderless = true }; root.AddChild(embedded);
        var forced = new PopupMenu { ForceNative = true, Transparent = false }; forced.AddItem("Native from embedded"); embedded.AddChild(forced);
        var modal = new Window { Visible = false, Transient = true, Exclusive = true, Borderless = true, Size = new(80, 60) };
        var modalProbe = new PopupInputProbe { Size = new(80, 60) }; modal.AddChild(modalProbe); root.AddChild(modal);
        var phase = -30; var originSignals = 0;
        root.WindowInput += input => { if (input is InputEventKey { Pressed: true }) originSignals++; };
        root.Ready += _ =>
        {
            forced.PopupOnParent(new(8, 8, 48, 32));
            Check(embedded.IsEmbedded() && !forced.IsEmbedded() && SDL.GetWindowParent(Native(forced)) == Native(root), "ForceNative menus resolve screen bounds and physical parenting through an embedded Window.");
            forced.Hide();
            RenderingServer.FramePostDraw += () =>
            {
                // Let native focus from the retired setup popup settle before exercising input.
                if (phase < 0)
                {
                    if (++phase == 0)
                    {
                        popup.PopupOnParent(new(100, 90, 80, 60));
                        var initialPosition = popup.NativeDisplay!.PopupClientOrigin;
                        Reject<OverflowException>(() => popup.PopupOnParent(new(int.MaxValue, int.MaxValue, 32, 24)));
                        Check(popup.Visible && popup.NativeDisplay.PopupClientOrigin == initialPosition, "Overflowing transformed popup coordinates reject before live geometry mutation.");
                    }
                    return;
                }
                switch (phase++)
                {
                    case 0:
                        NativeKey(root, SDL.Keycode.A); NativeKey(popup, SDL.Keycode.B);
                        NativeButton(root, new(4, 4), true); NativeButton(root, new(4, 4), false); break;
                    case 1:
                        Check(popup.Visible && parentProbe.Keys == 0 && probe.Keys == 1 && originSignals == 0 && parentProbe.Buttons == 0, "An exclusive popup blocks parent native reception and viewport input while direct popup input remains live.");
                        popup.Exclusive = false;
                        var point = (Vector2)popup.NativeDisplay!.PopupClientOrigin + new Vector2(20, 20);
                        NativeButton(root, point, true); NativeButton(root, point, false); break;
                    case 2:
                        Check(probe.Buttons == 1 && parentProbe.Buttons == 0 && probe.LastPoint.DistanceTo(new(20, 20)) <= 1.5f && probe.Origin == root.GetWindowID() && !Input.IsMouseButtonPressed(MouseButton.Left), "Popup retargeting localizes coordinates, retains the native origin and pairs global releases.");
                        var native = Native(root); var at = (Vector2)popup.NativeDisplay!.PopupClientOrigin + new Vector2(20, 20); var size = (Vector2)root.Size;
                        var down = new SDL.Event { TFinger = new SDL.TouchFingerEvent { Type = SDL.EventType.FingerDown, WindowID = SDL.GetWindowID(native), TouchID = 91, FingerID = 7, X = at.X / size.X, Y = at.Y / size.Y, Pressure = 1 } };
                        var up = down; up.TFinger.Type = SDL.EventType.FingerUp;
                        Check(SDL.PushEvent(ref down) && SDL.PushEvent(ref up), "Native touch reaches the popup router."); break;
                    case 3:
                        Check(probe.Touches == 1 && popup.Visible && probe.LastPoint.DistanceTo(new(20, 20)) <= 1.5f, "Native touch uses the same parent-client popup geometry.");
                        popup.Hide(); fitted.PopupOnParent(new(100, 90, 96, 64)); break;
                    case 4:
                        using (var image = RenderingServer.Service!.ReadbackNativeWindow(fitted.GetWindowID()))
                        {
                            var alpha = image.GetPixel(0, 32).A;
                            Check(MathF.Abs(alpha - (backend == "compatibility" ? 0 : 1)) < .02f, "Actual presentation fitting margins preserve the native window alpha role.");
                            var color = image.GetPixel(image.Width / 2, image.Height / 2); Check(color.G > .95f && color.R < .05f, "Fitted popup content reaches actual presentation.");
                        }
                        fitted.Hide(); fitted.ContentScaleMode = ContentScaleMode.Disabled;
                        if (SDL.GetCurrentVideoDriver() == "wayland")
                        {
                            Reject<NotSupportedException>(() => fitted.PopupCentered(new(80, 60)));
                            Check(!fitted.Visible && fitted.GetWindowID() == -1, "A global centered placement fails without inventing Wayland desktop positions.");
                        }
                        else fitted.PopupCentered(new(80, 60));
                        break;
                    case 5:
                        if (fitted.Visible)
                        {
                            var screen = root.CurrentScreen;
                            Check(fitted.Position == DisplayServer.ScreenGetPosition(screen) + (DisplayServer.ScreenGetSize(screen) - fitted.Size) / 2, "Native centering uses the complete parent display rectangle.");
                        }
                        fitted.Hide(); modal.Show(); NativeKey(root, SDL.Keycode.C); break;
                    default:
                        Check(modal.IsEmbedded() && modalProbe.Keys == 1 && parentProbe.Keys == 0 && originSignals == 1, "An embedded exclusive child retains input from its shared native parent while blocking the parent viewport.");
                        modal.Hide(); popup.Exclusive = true; popup.PopupOnParent(new(100, 90, 80, 60));
                        var exclusive = new Popup { ForceNative = true }; exclusive.PopupExclusiveOnParent(root, new(8, 8, 32, 24));
                        Check(exclusive.Parent == popup && SDL.GetWindowParent(Native(exclusive)) == Native(popup), "The public exclusive helper chooses the active exclusive native owner.");
                        root.Tree!.Quit(); break;
                }
            };
        };
        try { Check(Engine.Run(root) == 0 && phase == 7, "Native popup input policy completes."); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine($"Native popup exclusive/origin/mouse/touch routing passed on {backend}.");
    }
}
