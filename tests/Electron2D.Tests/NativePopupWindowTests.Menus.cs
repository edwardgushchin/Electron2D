using Electron2D;
using SDL = SDL3.SDL;

internal static partial class NativePopupWindowTests
{
    private static void Menus(string backend)
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var root = new Window { Title = "Native menu consumers", Size = new(440, 300) };
        var button = new MenuButton("Commands") { Position = new(24, 20), Size = new(120, 32) };
        var safe = new Button("Safe") { Size = new(120, 32) }; button.AddChild(safe); var safePresses = 0; safe.Pressed += () => safePresses++;
        var menu = button.GetPopup(); menu.AddItem("Open", 10);
        var submenu = new PopupMenu { Name = "Nested" }; submenu.AddItem("Nested command", 30); menu.AddSubmenuNodeItem("More", submenu, 20);
        var option = new OptionButton { Position = new(190, 20), Size = new(140, 32) }; option.AddItem("First", 1); option.AddItem("Second", 2); var selected = -1; option.ItemSelected += index => selected = index;
        using var style = new StyleBoxFlat { BGColor = Colors.Green, ShadowSize = 4 };
        var panel = new PopupPanel { Name = "Panel", Size = new(96, 64) }; panel.AddThemeStyleBoxOverride("panel", style);
        var text = new TextEdit { Text = "Native context menu", Position = new(24, 210), Size = new(260, 64) };
        root.AddChild(button); root.AddChild(option); root.AddChild(panel); root.AddChild(text);
        var phase = -30; var frames = 0; var focusWait = 0; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            root.Tree!.ProcessFrameStarted += _ =>
            {
                if (phase < 9) return;
                before = GC.GetAllocatedBytesForCurrentThread();
                if (phase == 9) menu.SetFocusedItem(frames & 1);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase < 0)
                {
                    if (++phase == 0)
                    {
                        if (backend == "gpu")
                        {
                            try { button.ShowPopup(); throw new InvalidOperationException("Expected transparent GPU refusal."); }
                            catch (AggregateException error) when (error.Flatten().InnerExceptions.Any(failure => failure is NotSupportedException)) { }
                            Check(!menu.Visible && menu.GetWindowID() == -1 && !button.ButtonPressed, "Unsupported transparent GPU presentation rolls back the native lifetime and owner state.");
                            menu.Transparent = submenu.Transparent = option.GetPopup().Transparent = panel.Transparent = text.GetMenu().Transparent = false;
                        }
                        button.ShowPopup();
                    }
                    return;
                }
                if (phase >= 9)
                {
                    if (frames >= 32) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++frames < 96) return;
                    Check(allocated == 0, $"Native popup {backend} phase {phase} allocated {allocated} managed owner bytes in 64 prepared frames.");
                    Console.WriteLine($"Native popup {backend}: 64 prepared {(phase == 9 ? "active focus/render" : "idle render")} frames, {allocated} managed owner bytes.");
                    if (phase == 9) { phase++; frames = 0; allocated = 0; } else root.Tree!.Quit();
                    return;
                }
                if (SDL.GetCurrentVideoDriver() == "wayland" && phase is 0 or 1 or 2)
                {
                    var pendingFocus = phase == 1 ? submenu : menu;
                    if (pendingFocus.Visible && !pendingFocus.HasFocus())
                    { Check(++focusWait <= 120, "The compositor must acknowledge the current native menu grab before injected navigation."); return; }
                    focusWait = 0;
                }
                switch (phase++)
                {
                    case 0:
                        Check(menu.Visible && menu.GetWindowID() > 0 && DisplayServer.WindowGetFlag(WindowFlag.Transparent, menu.GetWindowID()) == (backend == "compatibility"), "MenuButton opens a native popup with the supported requested alpha role.");
                        TextPixels(menu);
                        if (SDL.GetCurrentVideoDriver() == "wayland") Reject<NotSupportedException>(() => DisplayServer.WindowGetPopupSafeRect(menu.GetWindowID()));
                        if (SDL.GetCurrentVideoDriver() == "x11")
                        {
                            var area = new Rect2i(root.Position + new Vector2i(24, 20), new(120, 32));
                            DisplayServer.WindowSetPopupSafeRect(menu.GetWindowID(), area);
                            Check(DisplayServer.WindowGetPopupSafeRect(menu.GetWindowID()) == area, "The public screen-safe area controls actual outside routing.");
                        }
                        menu.SetFocusedItem(1); NativeKey(menu, SDL.Keycode.Right); break;
                    case 1:
                        Check(submenu.Visible && SDL.GetWindowParent(Native(submenu)) == Native(menu), "A real nested menu opens via the native right action.");
                        TextPixels(submenu); NativeKey(submenu, SDL.Keycode.Left); break;
                    case 2:
                        Check(!submenu.Visible && menu.Visible, $"Native left closes only the submenu (child={submenu.Visible}, parent={menu.Visible}).");
                        NativeButton(root, new(40, 30), true); NativeButton(root, new(40, 30), false); break;
                    case 3:
                        Check(safePresses == 1 && menu.Visible, "The opening control safe area allows owner GUI input without dismissal.");
                        NativeKey(menu, SDL.Keycode.Escape); break;
                    case 4:
                        Check(!menu.Visible && !button.ButtonPressed, "Native cancellation synchronizes the owner button.");
                        option.ShowPopup(); NativeKey(option.GetPopup(), SDL.Keycode.Down); NativeKey(option.GetPopup(), SDL.Keycode.Return); break;
                    case 5:
                        Check(selected == 1 && option.Selected == 1 && !option.GetPopup().Visible, "OptionButton native keyboard activation commits the selected item.");
                        if (SDL.GetCurrentVideoDriver() == "x11") panel.Popup(new(root.Position + new Vector2i(24, 100), new(96, 64)));
                        else panel.Popup();
                        break;
                    case 6:
                        Pixel(panel, Colors.Green);
                        using (var image = panel.GetTexture().GetImage()!) Check(image.GetPixel(0, 0).A < .8f, "The popup panel target preserves its transparent shadow corner.");
                        panel.Hide(); Check(panel.Size == new Vector2i(96, 64), "Native panel hiding restores the content size.");
                        NativeButton(root, new(44, 230), true, MouseButton.Right); NativeButton(root, new(44, 230), false, MouseButton.Right); break;
                    case 7:
                        Check(text.GetMenu().Visible && text.GetMenu().GetWindowID() > 0, "TextEdit opens its native context menu from viewport coordinates.");
                        TextPixels(text.GetMenu()); NativeKey(text.GetMenu(), SDL.Keycode.Escape); break;
                    case 8:
                        Check(!text.GetMenu().Visible, "The native context menu cancels."); button.ShowPopup(); frames = 0; break;
                }
            };
        };
        try { Check(Engine.Run(root) == 0 && phase == 10 && frames == 96, "Native consumer and warm profiles complete."); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine($"Native menu/button/option/panel/context input and actual pixels passed on {backend}.");
    }

    private static void TextPixels(Window window)
    {
        using var image = window.GetTexture().GetImage()!; var bright = 0;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
            { var color = image.GetPixel(x, y); if (color.R > .65f && color.G > .65f && color.B > .65f && color.A > .8f) bright++; }
        Check(bright > 20, "The native menu draws actual shaped text.");
        using var presented = RenderingServer.Service!.ReadbackNativeWindow(window.GetWindowID());
        Check(presented.Width == image.Width && presented.Height == image.Height, "The native menu owns actual presentation storage.");
    }

    private static void NativeKey(Window window, SDL.Keycode key)
    {
        var id = SDL.GetWindowID(Native(window));
        var press = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = id, Key = key, Down = true } };
        var release = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyUp, WindowID = id, Key = key } };
        Check(SDL.PushEvent(ref press) && SDL.PushEvent(ref release), "Native popup action enters the SDL queue.");
    }

    private static void NativeButton(Window window, Vector2 point, bool pressed, MouseButton button = MouseButton.Left)
    {
        var native = Native(window); var density = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(native) : 1;
        var input = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp, WindowID = SDL.GetWindowID(native), Button = button == MouseButton.Right ? (byte)3 : button == MouseButton.Middle ? (byte)2 : (byte)button, Down = pressed, X = point.X / density, Y = point.Y / density } };
        Check(SDL.PushEvent(ref input), "Native popup pointer input enters the SDL queue.");
    }
}
