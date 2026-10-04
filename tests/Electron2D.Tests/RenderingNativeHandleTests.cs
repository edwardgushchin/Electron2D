using Electron2D;
using SDL3;

internal static class RenderingNativeHandleTests
{
    private static readonly DisplayServer.HandleType[] Types = [DisplayServer.HandleType.OpenGLContext,
        DisplayServer.HandleType.EGLDisplay, DisplayServer.HandleType.EGLConfig,
        DisplayServer.HandleType.GLXVisualID, DisplayServer.HandleType.GLXFBConfig];

    internal static void Run()
    {
        var settings = ProjectSettings.Service;
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            for (var i = 0; i < Types.Length; i++) Check((int)Types[i] == i + 3, "Native handle numeric identity.");
            using (var display = DisplayServer.Open("No renderer", new Vector2i(64, 64), hidden: true))
                foreach (var type in Types) Reject<NotSupportedException>(() => DisplayServer.WindowGetNativeHandle(type));
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_HANDLE_METHOD") ?? "compatibility");
            for (var run = 0; run < 2; run++)
            {
                using var window = new Window { Title = "Electron2D native handles", Size = new Vector2i(96, 64) };
                var probe = new Probe();
                window.AddChild(probe);
                Check(Engine.Run(window) == 0 && probe.Frames == 2, "Two rendered frames and clean shutdown.");
                foreach (var type in Types)
                {
                    Reject<InvalidOperationException>(() => DisplayServer.WindowGetNativeHandle(type));
                    Reject<ObjectDisposedException>(() => probe.Display!.WindowGetNativeHandleCore(type));
                }
                Check(DisplayServer.Service is null && RenderingServer.Service is null, "Native ownership released for reopen.");
            }
            Console.WriteLine("Native graphics handle checks passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }

    private sealed class Probe : Entity
    {
        internal DisplayServer? Display;
        internal int Frames;
        private nint[] _handles = [];
        private nint _window;

        protected override void OnReady()
        {
            var display = Display = DisplayServer.Service!;
            var server = RenderingServer.Service!;
            var driver = RenderingServer.GetCurrentRenderingDriverName();
            Console.WriteLine($"Handle check: {DisplayServer.GetName()}/{RenderingServer.GetCurrentRenderingMethod()}/{driver}");
            var gl = RenderingServer.GetCurrentRenderingMethod() == "compatibility" && driver is "opengl" or "opengles2";
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetNativeHandle((DisplayServer.HandleType)2));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetNativeHandle(Types[0], 1));
            Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.WindowGetNativeHandle(Types[0])).GetAwaiter().GetResult());
            if (gl)
            {
                _window = SDL.GLGetCurrentWindow();
                _handles = Types.Select(type =>
                {
                    try { return DisplayServer.WindowGetNativeHandle(type); }
                    catch (NotSupportedException) { return 0; }
                }).ToArray();
                Check(_window != 0 && _handles[0] == SDL.GLGetCurrentContext() && _handles[0] != 0, "Window-associated owned context.");
                VerifyNative(_handles, display);
                var egl = SDL.EGLGetCurrentDisplay() != 0;
                for (var i = 1; i < Types.Length; i++)
                    Check((_handles[i] != 0) == (egl ? i <= 2 : i >= 3), "Each handle follows the actual EGL/GLX driver.");
                VerifyForeignContext();
            }
            else foreach (var type in Types) Reject<NotSupportedException>(() => DisplayServer.WindowGetNativeHandle(type));
            RenderingServer.SetDefaultClearColor(Colors.Red);
            RenderingServer.FramePostDraw += () =>
            {
                Frames++;
                using var pixels = server.Readback();
                Check(pixels.GetPixel(2, 2) == Colors.Red, "Handle queries retain rendering behavior.");
                if (gl)
                {
                    Check(SDL.GLGetCurrentWindow() == _window && SDL.GLGetCurrentContext() == _handles[0], "Renderer restored its context.");
                    for (var i = 0; i < Types.Length; i++)
                        if (_handles[i] != 0) Check(DisplayServer.WindowGetNativeHandle(Types[i]) == _handles[i], "Borrowed identity stable across frames.");
                }
                if (Frames == 2) Tree!.Quit();
            };
        }

        private void VerifyForeignContext()
        {
            Check(SDL.GLGetAttribute(SDL.GLAttr.DepthSize, out var depth), SDL.GetError());
            Check(SDL.GLGetAttribute(SDL.GLAttr.StencilSize, out var stencil), SDL.GetError());
            Check(SDL.GLSetAttribute(SDL.GLAttr.DepthSize, 24) && SDL.GLSetAttribute(SDL.GLAttr.StencilSize, 8), SDL.GetError());
            var foreign = SDL.CreateWindow("Foreign context check", 32, 32, SDL.WindowFlags.OpenGL | SDL.WindowFlags.Hidden);
            Check(foreign != 0, SDL.GetError());
            nint context = 0;
            try
            {
                context = SDL.GLCreateContext(foreign);
                Check(context != 0 && SDL.GLGetCurrentWindow() == foreign, "A different native window is current.");
                if (_handles[2] != 0) Check(SDL.EGLGetCurrentConfig() != _handles[2], "Foreign window selected a different EGL config.");
                for (var i = 0; i < Types.Length; i++)
                    if (_handles[i] != 0) Check(DisplayServer.WindowGetNativeHandle(Types[i]) == _handles[i], "Query must not leak a foreign current context.");
                Check(SDL.GLGetCurrentContext() == context && SDL.GLGetCurrentWindow() == foreign, "Query does not change context selection.");
            }
            finally
            {
                if (context != 0) Check(SDL.GLDestroyContext(context), SDL.GetError());
                SDL.DestroyWindow(foreign);
                Check(SDL.GLSetAttribute(SDL.GLAttr.DepthSize, depth) && SDL.GLSetAttribute(SDL.GLAttr.StencilSize, stencil), SDL.GetError());
            }
            Check(DisplayServer.WindowGetNativeHandle(Types[0]) == _handles[0], "Owned context remains queryable while none is current.");
        }
    }

    private static unsafe void VerifyNative(nint[] handles, DisplayServer display)
    {
        if (handles[1] != 0)
        {
            var currentDisplay = (delegate* unmanaged[Cdecl]<nint>)SDL.EGLGetProcAddress("eglGetCurrentDisplay");
            var query = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, uint>)SDL.EGLGetProcAddress("eglQueryContext");
            var attribute = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, uint>)SDL.EGLGetProcAddress("eglGetConfigAttrib");
            int a = 0, b = 0;
            Check(currentDisplay() == handles[1] && query(handles[1], handles[0], 0x3028, &a) != 0 &&
                attribute(handles[1], handles[2], 0x3028, &b) != 0 && a == b, "Borrowed EGL config belongs to the native context.");
        }
        else
        {
            var nativeDisplay = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle);
            var query = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, int>)SDL.GLGetProcAddress("glXQueryContext");
            var attribute = (delegate* unmanaged[Cdecl]<nint, nint, int, int*, int>)SDL.GLGetProcAddress("glXGetFBConfigAttrib");
            int a = 0, b = 0, visual = 0;
            Check(handles[4] != 0 && query(nativeDisplay, handles[0], 0x8013, &a) == 0 &&
                attribute(nativeDisplay, handles[4], 0x8013, &b) == 0 && a == b &&
                attribute(nativeDisplay, handles[4], 0x800B, &visual) == 0 && (nint)(uint)visual == handles[3],
                "Borrowed GLX framebuffer config and visual belong to the native context.");
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
