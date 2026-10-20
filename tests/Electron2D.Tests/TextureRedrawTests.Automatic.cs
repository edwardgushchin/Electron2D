using Electron2D;
using static TextureArrayTests;

internal static partial class TextureRedrawTests
{
    private static void Automatic(ImageTexture source, string backend)
    {
        using var window = new Window { Size = new(256, 128), Title = "Electron2D visible image cadence" }; var patch = new Patch { Position = new(8, 8) }; patch.Use(source); window.AddChild(patch); var driver = new Cadence(window, patch, source, backend); window.AddChild(driver);
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePreDraw += driver.BeforeDraw; RenderingServer.FramePostDraw += driver.Drawn; };
        Check(Engine.Run(window) == 0 && driver.Completed && window.IsDisposed, "Automatic idle/visible texture session completes.");
    }
    private sealed class Cadence(Window window, Patch patch, Texture source, string backend) : Node
    {
        private int _phase, _tick, _total, _active, _records, _callbacks, _callbacksBefore;
        private ulong _baseline;
        private long _idleBefore, _before;
        internal bool Completed;
        internal void BeforeDraw() { _callbacks++; }
        protected override void OnEnterTree() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            Check(++_total < 512, "Texture cadence reaches completion without depending on absent frame events."); _tick++;
            switch (_phase)
            {
                case 0:
                    if (_tick == 16) { _baseline = Engine.GetFramesDrawn(); _idleBefore = GC.GetAllocatedBytesForCurrentThread(); }
                    if (_tick is > 16 and <= 48) Check(Engine.GetFramesDrawn() == _baseline, "New session does not inherit the previous session's policy.");
                    if (_tick != 48) return;
                    Check(GC.GetAllocatedBytesForCurrentThread() == _idleBefore, "Prepared idle iterations allocate zero owner bytes."); _phase = 1; _tick = 0; _records = patch.Draws; RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), true); return;
                case 1: _before = GC.GetAllocatedBytesForCurrentThread(); _callbacksBefore = _callbacks; return;
                case 2:
                    if (_tick == 4) { _baseline = Engine.GetFramesDrawn(); _idleBefore = GC.GetAllocatedBytesForCurrentThread(); }
                    if (_tick is > 4 and <= 36) Check(Engine.GetFramesDrawn() == _baseline, "Hidden flagged texture leaves the renderer idle.");
                    if (_tick != 36) return;
                    Check(GC.GetAllocatedBytesForCurrentThread() == _idleBefore, "Prepared hidden iterations allocate zero owner bytes."); patch.Visible = true; _phase = 3; _tick = 0; return;
                case 4:
                    if (_tick == 4) _baseline = Engine.GetFramesDrawn();
                    if (_tick <= 4) return;
                    Check(Engine.GetFramesDrawn() == _baseline, "Disabled texture policy stops native frames after settling.");
                    if (_tick != 8) return;
                    RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), true); RenderingServer.RenderLoopEnabled = false; _phase = 5; _tick = 0; _baseline = Engine.GetFramesDrawn(); return;
                case 5:
                    Check(Engine.GetFramesDrawn() == _baseline, "Visible policy respects RenderLoopEnabled=false."); if (_tick != 4) return;
                    var process = Engine.ProcessFrames; var physics = Engine.PhysicsFrames; RenderingServer.ForceDraw(); Check(Engine.GetFramesDrawn() == _baseline + 1 && Engine.ProcessFrames == process && Engine.PhysicsFrames == physics, "Manual rendering retains the visible texture policy without simulation steps."); RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), false); RenderingServer.RenderLoopEnabled = true; _phase = 6; _tick = 0; return;
                case 6:
                    if (_tick == 4) _baseline = Engine.GetFramesDrawn(); if (_tick <= 4) return; Check(Engine.GetFramesDrawn() == _baseline, "Final unflagged image returns to idle."); if (_tick != 8) return;
                    Completed = true; RenderingServer.FramePreDraw -= BeforeDraw; RenderingServer.FramePostDraw -= Drawn; window.Tree!.Quit(); return;
            }
        }
        internal void Drawn()
        {
            if (_phase == 1)
            {
                if (_tick is > 16 and <= 48) { Check(GC.GetAllocatedBytesForCurrentThread() == _before && patch.Draws == _records && _callbacks == _callbacksBefore + 1, "Prepared continuous texture frames allocate zero owner bytes and retain authored canvas commands."); _active++; }
                if (_tick != 48) return; Check(_active == 32, "Exactly 32 prepared automatic frames were measured.");
                using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(12, 12).R > .99f, "Actual native visible-texture frames contain the expected sampled pixels."); if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXTURE_REDRAW_OUTPUT") is { } output) { Directory.CreateDirectory(output); image.SavePNG(System.IO.Path.Combine(output, backend + "-continuous.png")); }
                patch.Visible = false; _phase = 2; _tick = 0; return;
            }
            if (_phase == 3 && _tick == 4) { RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), false); _phase = 4; _tick = 0; }
        }
    }
}
