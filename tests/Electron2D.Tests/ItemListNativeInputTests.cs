using System.Runtime.InteropServices;
using Electron2D;
using SDL = SDL3.SDL;

internal static class ItemListNativeInputTests
{
    internal static void Run(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        nint commit = 0;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); InputMap.LoadFromProjectSettings();
            using var root = new Window { Size = new(220, 160) };
            var list = new ItemList { Size = new(180, 140) }; foreach (var text in new[] { "Alpha", "Beta", "Bee", "Boat" }) list.AddItem(text);
            root.AddChild(list); var selected = new List<int>(); list.ItemSelected += selected.Add; var committed = 0;
            list.TextInput += text => { Check(text == "Bo" && list.Current == 3, "Committed text performs search before the public text observer."); committed++; };
            root.Ready += _ =>
            {
                list.Current = 0; list.GrabFocus(); root.SetIMEActive(true);
                var windows = SDL.GetWindows(out var count); Check(count == 1, "The fixture owns one native window."); var id = SDL.GetWindowID(windows![0]);
                var down = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = id, Key = SDL.Keycode.Down, Scancode = SDL.Scancode.Down, Down = true } };
                var up = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyUp, WindowID = id, Key = SDL.Keycode.Down, Scancode = SDL.Scancode.Down } };
                commit = Marshal.StringToCoTaskMemUTF8("Bo");
                var text = new SDL.Event { Text = new SDL.TextInputEvent { Type = SDL.EventType.TextInput, WindowID = id, Text = commit } };
                Check(SDL.PushEvent(ref down) && SDL.PushEvent(ref up) && SDL.PushEvent(ref text), "The real native queue accepts key and committed-text records.");
                RenderingServer.FramePostDraw += () =>
                {
                    Check(committed == 1 && list.Current == 3 && selected.SequenceEqual([1, 2, 3]), "Native keyboard and committed text traverse the focused ItemList in order.");
                    using var image = root.GetTexture().GetImage()!; Check(image.Width >= 220, "The native input scene produced actual renderer output.");
                    root.Tree!.Quit();
                };
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "The native producer session retires normally.");
            Console.WriteLine(backend + ": native key queue and committed-text ItemList search passed.");
        }
        finally { if (commit != 0) Marshal.FreeCoTaskMem(commit); ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
