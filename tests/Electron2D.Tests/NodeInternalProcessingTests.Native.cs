using Electron2D;

internal static partial class NodeInternalProcessingTests
{
    private sealed class Picture : Entity
    {
        internal int Idle, Physics;
        private Color _color = Colors.Black;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationInternalProcess) { Idle++; _color = Idle == 1 ? Colors.Red : Colors.Blue; QueueRedraw(); }
            if (what == NotificationInternalPhysicsProcess) Physics++;
        }
        protected override void OnDraw() => DrawRect(new(8, 8, 16, 12), _color);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var picture = new Picture { InternalProcessEnabled = true, InternalPhysicsProcessEnabled = true };
        var window = new Window { Size = new(64, 48) }; window.AddChild(picture); var stage = 0; var frames = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback(); var pixel = image.GetPixel(12, 12);
                if (stage == 0) { Check(picture.Idle == 1 && pixel.R > .95f && pixel.B < .05f, "Internal notification paints first native frame."); picture.InternalProcessEnabled = false; stage++; }
                else if (stage == 1) { Check(picture.Idle == 1 && pixel.R > .95f && pixel.B < .05f, "Disabled internal lane preserves actual pixels."); picture.InternalProcessEnabled = true; stage++; }
                else { Check(picture.Idle >= 2 && pixel.B > .95f && pixel.R < .05f, "Resumed internal lane repaints native blue pixels."); if (picture.Physics > 0) { stage = 3; window.Tree!.Quit(); } }
                Check(++frames < 180, "Native physics lane reaches a tick.");
            };
        };
        Check(Engine.Run(window) == 0 && stage == 3 && window.IsDisposed && !RenderingServer.IsAvailable, "Native internal idle/physics consumer cleans up.");
        Console.WriteLine("Node internal " + backend + " red/frozen-red/resumed-blue pixels and physics ticks passed.");
    }
}
