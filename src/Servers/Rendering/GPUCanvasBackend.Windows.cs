using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly HashSet<ChildPresentation> _childPresentations = [];

    internal sealed class ChildPresentation : IDisposable
    {
        private readonly GPUCanvasBackend _backend;
        private readonly SafeHandle _window;
        private bool _retained, _claimed;
        internal CanvasRenderTarget? Target;
        internal DisplayServer.VSyncMode VSync;
        internal SDL.GPUSwapchainComposition Composition;
        internal CanvasOutput Output;
        internal nint Window => _window.DangerousGetHandle();

        internal ChildPresentation(GPUCanvasBackend backend, SafeHandle window)
        {
            _backend = backend; _window = window;
            window.DangerousAddRef(ref _retained);
        }

        internal void Claim(DisplayServer.VSyncMode mode)
        {
            Check(SDL.ClaimWindowForGPUDevice(_backend.Device, Window), "claim child window for GPU rendering");
            _claimed = true;
            VSync = _backend.ApplyVSync(Window, mode);
            _backend._childPresentations.Add(this);
        }

        internal void SetVSync(DisplayServer.VSyncMode mode) => VSync = _backend.ApplyVSync(Window, mode, Composition);

        public void Dispose()
        {
            _backend._childPresentations.Remove(this);
            try { Target?.Dispose(); Target = null; }
            finally
            {
                try { if (_claimed) { _claimed = false; SDL.ReleaseWindowFromGPUDevice(_backend.Device, Window); } }
                finally { if (_retained) { _retained = false; _window.DangerousRelease(); } }
            }
        }
    }

    internal ChildPresentation ClaimChildWindow(SafeHandle window, DisplayServer.VSyncMode mode)
    {
        var presentation = new ChildPresentation(this, window);
        try { presentation.Claim(mode); return presentation; }
        catch { presentation.Dispose(); throw; }
    }

    internal bool Present(ChildPresentation child, ReadOnlySpan<CanvasPresentation> viewports, nint screen = 0, Vector2i screenSize = default, bool screenHDR = false)
    {
        Check(SDL.GetWindowSizeInPixels(child.Window, out var width, out var height), "query child presentation size");
        var target = PreparePresentation(new(width, height), ref child.Target, child.Output.Enabled);
        return PresentToWindow(child.Window, target, viewports, root: false, screen, child.Output, child.Composition, screenHDR);
    }
}
