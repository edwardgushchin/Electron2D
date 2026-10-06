using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private sealed class TabPage : Control
    { internal Electron2D.Color Color; protected override void OnDraw() { base.OnDraw(); DrawRect(new(Vector2.Zero, Size), Color); } }
    private static void VerifyTabContainerRendering(string backend)
    {
        var root = new Window { Size = new(340, 220), GUIEmbedSubwindows = true }; var tabs = new TabContainer { Name = "Panels", Size = new(300, 180), Position = new(15, 15) };
        var red = new TabPage { Name = "Red page", Color = Colors.Red }; var blue = new TabPage { Name = "Blue page", Color = Colors.Blue }; tabs.AddChild(red); tabs.AddChild(blue); root.AddChild(tabs);
        var menu = new PopupMenu { Name = "Menu" }; menu.AddItem("Command"); root.AddChild(menu); tabs.SetPopup(menu); var frame = 0;
        root.Ready += _ =>
        {
            tabs.GetTabBar().GrabFocus(); RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = RenderingServer.Service!.Readback(); frame++;
                var point = tabs.GetCurrentTabControl()!.GetGlobalRect().GetCenter(); var color = pixels.GetPixel((int)point.X, (int)point.Y);
                Check(frame == 2 ? color.B > .9f && color.R < .1f : color.R > .9f && color.B < .1f, $"Tab panels {backend} draw only the selected child's pixels (frame {frame}).");
                if (frame == 1) { var input = new SDL.Event { Type = (uint)SDL.EventType.KeyDown }; input.Key.WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); input.Key.Key = SDL.Keycode.Right; input.Key.Down = true; SDL.PushEvent(ref input); return; }
                if (frame == 2) { Check(tabs.CurrentTab == 1 && blue.Visible && !red.Visible, "Native keyboard switches real page."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-tab-panels-{backend}.png"), pixels.SavePNGToBuffer()); tabs.TabsPosition = TabContainer.TabPosition.Bottom; tabs.LayoutDirection = LayoutDirection.RTL; tabs.CurrentTab = 0; return; }
                Check(tabs.GetTabBar().Position.Y > red.Position.Y, "Bottom header is rendered below page content."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-tab-panels-rtl-{backend}.png"), pixels.SavePNGToBuffer()); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Released(root); Check(frame == 3, "Tab panel renderer completed all phases."); VerifyTabContainerWarm(backend); Console.WriteLine($"TabContainer themed panel, native selection, popup affordance and bottom/RTL rendered on {backend}.");
    }
    private static void VerifyTabContainerWarm(string backend)
    {
        var root = new Window { Size = new(260, 160) }; var tabs = new TabContainer { Name = "WarmPanels", Size = new(220, 130) }; tabs.AddChild(new TabPage { Name = "First", Color = Colors.Red }); tabs.AddChild(new TabPage { Name = "Second", Color = Colors.Blue }); root.AddChild(tabs); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); tabs.CurrentTab = frame & 1; }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"TabContainer {backend} warmed selection/layout/visibility/render allocated {bytes} managed bytes over 64 frames."); Console.WriteLine($"TabContainer {backend}: 64 warmed selection/layout/visibility/render frames, {bytes} managed bytes.");
    }
}
