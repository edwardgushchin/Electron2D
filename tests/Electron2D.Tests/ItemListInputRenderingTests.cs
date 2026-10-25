using Electron2D;

internal static class ItemListInputRenderingTests
{
    internal static void Run(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var maxFPS = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 0;
            InputMap.LoadFromProjectSettings();
            using var selected = new StyleBoxFlat { BGColor = Colors.Green };
            using var root = new Window { Size = new(280, 180) };
            var list = new ItemList { Position = new(10, 10), Size = new(160, 130) };
            list.AddThemeStyleBoxOverride("selected", selected); list.AddThemeStyleBoxOverride("selected_focus", selected);
            for (var i = 0; i < 8; i++) list.AddItem("Entry " + i);
            root.AddChild(list);
            using var down = new InputEventKey { Keycode = Key.Down, Pressed = true };
            using var up = new InputEventKey { Keycode = Key.Up, Pressed = true };
            using var cancel = new InputEventKey { Keycode = Key.Escape, Pressed = true };
            using var typed = new InputEventKey { Keycode = Key.E, Unicode = 'E', Pressed = true };
            using var cancelRelease = new InputEventKey { Keycode = Key.Escape };
            using var downRelease = new InputEventKey { Keycode = Key.Down };
            using var upRelease = new InputEventKey { Keycode = Key.Up };
            using var typedRelease = new InputEventKey { Keycode = Key.E };
            var frame = 0; var notifications = 0; var before = 0L; var allocated = -1L;
            list.ItemSelected += _ => notifications++;
            root.Ready += _ => { list.Current = 0; list.GrabFocus(); RenderingServer.FramePostDraw += AfterDraw; };
            void AfterDraw()
            {
                frame++;
                if (frame == 2)
                {
                    using var image = root.GetTexture().GetImage()!;
                    var point = list.Position + list.GetItemRect(1, false).Position + new Vector2(2, 2);
                    var pixel = image.GetPixel((int)point.X, (int)point.Y);
                    Check(list.HasFocus() && list.Current == 1 && pixel.G > .45f && pixel.R < .2f, "Public routed keyboard/search input produces the selected row's native pixels and retains keyboard focus.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_ITEM_INPUT_OUTPUT") is { Length: > 0 } output)
                    { Directory.CreateDirectory(output); image.SavePNG(System.IO.Path.Combine(output, "input-" + backend + ".png")); }
                }
                if (frame == 36) before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 100)
                {
                    allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                    root.Tree!.Quit(); return;
                }

            }
            void BeforeFrame(Window window, ulong index)
            {
                list.Current = 0;
                Input.ParseInputEvent(cancel); Input.ParseInputEvent(cancelRelease);
                Input.ParseInputEvent(down); Input.ParseInputEvent(downRelease);
                Input.ParseInputEvent(up); Input.ParseInputEvent(upRelease);
                Input.ParseInputEvent(typed); Input.ParseInputEvent(typedRelease);
            }
            Check(Engine.Run(BeforeFrame, root) == 0 && root.IsDisposed && frame == 100 && notifications == 300, "The public input/render session commits all routed selections and retires its hierarchy.");
            Check(allocated == 0, $"64 complete active input/render frames allocated {allocated} owner managed bytes.");
            Console.WriteLine(backend + ": public routed input/selection pixels and 64 complete active frames with zero owner managed bytes passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); Engine.MaxFPS = maxFPS; }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
