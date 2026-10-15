using System.Diagnostics;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed class NetworkWindow(NetworkSession session, string capturePath) : Entity
{
    private readonly double[] _frameTimes = new double[4096];
    private readonly PhysicsSnapshot _before = new(), _after = new();
    private readonly byte[] _beforeBytes = new byte[ImpairedLink.PacketBytes], _afterBytes = new byte[ImpairedLink.PacketBytes];
    private long _firstDraw, _lastDraw, _lastOwner, _lastTotal, _windowOwner, _windowTotal;
    private int _frames, _samples, _smoothed, _interpolated;
    private Vector2? _captureVisual, _captureSolved;
    private bool _captured, _preserved;
    private float _viewScale = 1;
    private Vector2 _viewOffset;
    private string _vSync = "";

    internal static void Run(NetworkSession session, SessionReport report, string renderer, string capturePath)
    {
        if (renderer is not ("gpu" or "compatibility")) throw new ArgumentException("Renderer must be gpu or compatibility.");
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            var window = new Window { Title = "Authoritative physics client", Size = new(800, 400) };
            var view = new NetworkWindow(session, capturePath) { ProcessEnabled = true };
            window.AddChild(view);
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(new(.04f, .06f, .09f));
                DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled, window.GetWindowID());
                view._vSync = DisplayServer.WindowGetVSyncMode(window.GetWindowID()).ToString();
                RenderingServer.FramePostDraw += () => view.DrawCompleted(window);
            };
            if (Engine.Run(window) != 0 || !session.Done) throw new InvalidOperationException("Window closed before the authority session completed.");
            report.WindowFrames = view._frames;
            report.WindowFPS = view._frames > 1 ? (view._frames - 1) / Stopwatch.GetElapsedTime(view._firstDraw, view._lastDraw).TotalSeconds : 0;
            report.WindowFramePercentilesMS = NetworkSession.Percentiles(view._frameTimes, view._samples);
            report.RenderedSmoothedSamples = view._smoothed; report.RenderedInterpolatedSamples = view._interpolated;
            report.RenderedPhysicsPreserved = view._preserved;
            report.Renderer = renderer; report.RequestedWindowVSync = "Disabled"; report.WindowVSync = view._vSync; report.WindowMaxFPS = Engine.MaxFPS;
            report.WindowOwnerAllocations = view._windowOwner; report.WindowTotalAllocations = view._windowTotal;
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }

    protected override void OnProcess(double delta)
    {
        session.Poll();
        if (session.Done) { Tree!.Quit(); return; }
        QueueRedraw();
    }

    protected override void OnDraw()
    {
        var minX = 0f; var minY = 0f; var maxX = 640f; var maxY = 360f;
        for (var i = 0; i < session.Simulation.Actors.Length; i++)
            if (session.Simulation.Actors[i]?.Body is { } body)
            {
                var visual = session.PresentationPosition(i); var solved = body.GlobalPosition;
                minX = MathF.Min(minX, MathF.Min(visual.X, solved.X) - 20); minY = MathF.Min(minY, MathF.Min(visual.Y, solved.Y) - 20);
                maxX = MathF.Max(maxX, MathF.Max(visual.X, solved.X) + 20); maxY = MathF.Max(maxY, MathF.Max(visual.Y, solved.Y) + 20);
            }
        _viewScale = MathF.Min(768 / (maxX - minX), 368 / (maxY - minY));
        _viewOffset = new Vector2(16, 16) - new Vector2(minX, minY) * _viewScale;
        foreach (var actor in session.Simulation.Actors)
            if (actor?.Body is { } body)
            {
                var slot = Array.IndexOf(session.Simulation.Actors, actor);
                var visual = session.PresentationPosition(slot);
                var solved = body.GlobalTransform;
                DrawSetTransform(Screen(visual), solved.Rotation, new Vector2(_viewScale, _viewScale));
                switch (actor.Spec.Kind)
                {
                    case ObjectKind.GeometryBoundary:
                    case ObjectKind.GeometryLedge:
                    case ObjectKind.GeometryCapsule:
                    case ObjectKind.GeometryPolygon:
                    case ObjectKind.GeometryRay:
                    case ObjectKind.GeometryCompound:
                        for (var child = 0; child < body.GetChildCount(); child++)
                        {
                            var collision = (CollisionShape)body.GetChild(child);
                            DrawSetTransformMatrix(new Transform(solved.Rotation, new Vector2(_viewScale, _viewScale), 0, Screen(visual)) * collision.Transform);
                            collision.Shape!.Draw(GetCanvasItem(), Colors.Gold);
                        }
                        break;
                    case ObjectKind.Floor: DrawRect(new(new(-320, -10), new(640, 20)), new(.25f, .3f, .38f)); break;
                    case ObjectKind.SupportFloor: DrawRect(new(new(-100, -1), new(200, 2)), Colors.Gray); break;
                    case ObjectKind.SupportBody: DrawCircle(Vector2.Zero, 2, Colors.Gold); break;
                    case ObjectKind.Sensor: DrawRect(new(new(-60, -50), new(120, 100)), new(.2f, .8f, .5f, .15f)); break;
                    case ObjectKind.Box: DrawRect(new(new(-12, -12), new(24, 24)), Colors.CornflowerBlue); break;
                    case ObjectKind.Anchor: DrawCircle(Vector2.Zero, 12, Colors.Gray); break;
                    case ObjectKind.Ball: DrawCircle(Vector2.Zero, 12, Colors.Pink); break;
                }
                DrawSetTransform(Vector2.Zero);
                if (actor.Spec.Kind is ObjectKind.Box or ObjectKind.Ball or ObjectKind.SupportBody or ObjectKind.GeometryCapsule or ObjectKind.GeometryPolygon or ObjectKind.GeometryRay or ObjectKind.GeometryCompound)
                {
                    if (session.IsSmoothedPresentation(slot)) _smoothed++;
                    if (session.IsInterpolatedPresentation(slot)) _interpolated++;
                    if (!_captured && _captureVisual is null && session.IsSmoothedPresentation(slot) &&
                        Screen(visual).DistanceTo(Screen(solved.Origin)) > 8)
                    {
                        _captureVisual = Screen(visual); _captureSolved = Screen(solved.Origin);
                        session.Simulation.Map.Capture(_before);
                    }
                }
                if (body.GlobalTransform != solved) throw new InvalidOperationException("Drawing changed a solved body pose.");
            }
        foreach (var actor in session.Simulation.Actors)
            if (actor?.Joint is not null && session.Simulation.Find(actor.Spec.First) is { } first && session.Simulation.Find(actor.Spec.Second) is { } second)
                DrawLine(Screen(session.PresentationPosition(Array.IndexOf(session.Simulation.Actors, first))),
                    Screen(session.PresentationPosition(Array.IndexOf(session.Simulation.Actors, second))), Colors.Gold, 2);
        foreach (var actor in session.Simulation.Actors)
            if (actor?.Body is not null && actor.Spec.Kind is ObjectKind.Box or ObjectKind.Ball or ObjectKind.SupportBody or ObjectKind.GeometryCapsule or ObjectKind.GeometryPolygon or ObjectKind.GeometryRay or ObjectKind.GeometryCompound)
            {
                var slot = Array.IndexOf(session.Simulation.Actors, actor);
                var visual = Screen(session.PresentationPosition(slot)); var solved = Screen(actor.Position);
                DrawCircle(visual, 3, Colors.White);
                if (session.IsSmoothedPresentation(slot) && visual.DistanceTo(solved) > 8) DrawCircle(solved, 3, Colors.Orange);
            }
    }

    private void DrawCompleted(Window window)
    {
        var now = Stopwatch.GetTimestamp();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        if (_frames > 0) { _windowOwner += owner - _lastOwner; _windowTotal += total - _lastTotal; }
        _lastOwner = owner; _lastTotal = total;
        if (_frames++ == 0) _firstDraw = now;
        else if (_samples < _frameTimes.Length) _frameTimes[_samples++] = Stopwatch.GetElapsedTime(_lastDraw, now).TotalMilliseconds;
        _lastDraw = now;
        if (_captured || _captureVisual is not { } visual || _captureSolved is not { } solved) return;
        session.Simulation.Map.Capture(_after);
        var length = _before.WriteTo(_beforeBytes);
        if (_after.WriteTo(_afterBytes) != length || !_beforeBytes.AsSpan(0, length).SequenceEqual(_afterBytes.AsSpan(0, length)))
            throw new InvalidOperationException("Rendered smoothing changed the portable physical state.");
        using var image = window.GetTexture().GetImage()!;
        Pixel(visual, Colors.White); Pixel(solved, Colors.Orange);
        if (!string.IsNullOrEmpty(capturePath)) image.SavePNG(capturePath);
        _captured = _preserved = true;

        void Pixel(Vector2 position, Color expected)
        {
            var actual = image.GetPixel((int)position.X, (int)position.Y);
            if (MathF.Abs(actual.R - expected.R) > .05f || MathF.Abs(actual.G - expected.G) > .05f || MathF.Abs(actual.B - expected.B) > .05f)
                throw new InvalidOperationException($"Rendered presentation marker differs at {position}: {actual}, expected {expected}.");
        }
    }
    private Vector2 Screen(Vector2 position) => _viewOffset + position * _viewScale;
}
