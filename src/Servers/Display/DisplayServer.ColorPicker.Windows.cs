using System.ComponentModel;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private WindowsDesktopColorPick? _windowsColorPick;
    private bool _colorPickEscapeDown;

    private bool BeginWindowsColorPick(Action<bool, Color> callback)
    {
        if (_windowsColorPick is not null || _mouseMode != MouseMode.Visible ||
            FocusedWindow() is null || !WindowsColorSampler.HasForeground)
            return false;

        try
        {
            _windowsColorPick = new WindowsDesktopColorPick(this, callback);
            return true;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or Win32Exception or EntryPointNotFoundException or DllNotFoundException)
        {
            return false;
        }
    }

    private bool ConsumeWindowsColorPickKey(SDL.EventType type, in SDL.Event nativeEvent)
    {
        if (type is not (SDL.EventType.KeyDown or SDL.EventType.KeyUp) || nativeEvent.Key.Scancode != SDL.Scancode.Escape)
            return false;
        if (type == SDL.EventType.KeyUp)
        {
            var consume = _colorPickEscapeDown;
            _colorPickEscapeDown = false;
            return consume;
        }
        if (_windowsColorPick is not { } pick)
            return _colorPickEscapeDown;

        pick.Cancel();
        if (nativeEvent.Key.Repeat && !_colorPickEscapeDown)
            return false;
        _colorPickEscapeDown = true;
        return true;
    }

    private void PollWindowsColorPick(ref List<Exception>? failures)
    {
        if (_colorPickEscapeDown && !WindowsColorSampler.HasForeground)
            _colorPickEscapeDown = false;
        if (_windowsColorPick is not { } pick || !pick.TryComplete(out var color))
            return;
        RetireWindowsColorPick();
        try { pick.Completed(color.HasValue, color ?? Colors.Black); }
        catch (Exception error) { (failures ??= []).Add(error); }
    }

    private void RetireWindowsColorPick()
    {
        var pick = _windowsColorPick;
        _windowsColorPick = null;
        pick?.Dispose();
    }

    private sealed class WindowsDesktopColorPick : IDisposable
    {
        private const int Size = 28;
        private const int Gap = 8;
        private readonly DisplayServer _display;
        private readonly WindowsColorSampler _sampler;
        private readonly SdlWindowHandle _preview;
        private readonly bool _excluded;
        private Rect2i _previewBounds;
        private Color? _previewColor;
        private bool _canceled;

        internal readonly Action<bool, Color> Completed;

        internal WindowsDesktopColorPick(DisplayServer display, Action<bool, Color> completed)
        {
            _display = display;
            Completed = completed;
            _sampler = WindowsColorSampler.Open();
            try
            {
                var window = SDL.CreateWindow("Select a desktop color", Size, Size, SDL.WindowFlags.Hidden |
                    SDL.WindowFlags.Borderless | SDL.WindowFlags.Utility | SDL.WindowFlags.AlwaysOnTop | SDL.WindowFlags.NotFocusable);
                if (window == 0) throw SDLFailure("create the color picker preview");
                _preview = new SdlWindowHandle(window);
                GC.SuppressFinalize(_preview);
                if (display.HasCaptureExclusion())
                {
                    var properties = SDL.GetWindowProperties(window);
                    var hwnd = SDL.GetPointerProperty(properties, SDL.Props.WindowWin32HWNDPointer, 0);
                    _excluded = hwnd != 0 && SetWindowDisplayAffinity(hwnd, CaptureExcluded) != 0;
                }

                UpdatePreview();
                if (!SDL.ShowWindow(window) || !WindowsColorSampler.HasForeground)
                    throw SDLFailure("show the color picker preview without changing focus");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Cancel() => _canceled = true;

        internal bool TryComplete(out Color? color)
        {
            color = null;
            if (_canceled || _display._mouseMode != MouseMode.Visible) return true;

            try
            {
                if (_sampler.TryReadSelection(out var point, out var accepted))
                {
                    if (accepted)
                    {
                        if (!_excluded && _previewBounds.HasPoint(point))
                        {
                            if (!SDL.HideWindow(_preview.DangerousGetHandle()))
                                throw SDLFailure("hide the color picker preview");
                            if (DwmFlush() < 0)
                                throw new IOException("Could not finish composing the desktop for color selection.");
                        }
                        color = WindowsScreenCapture.Pixel(point);
                    }
                    return true;
                }

                if (!WindowsColorSampler.HasForeground) return true;
                UpdatePreview();
                return false;
            }
            catch (Exception error) when (error is IOException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return true;
            }
        }

        private void UpdatePreview()
        {
            var point = _display.MouseGetPositionCore();
            var screen = _display.DesktopColorSampleBounds(point);
            var position = point + new Vector2i(point.X + Gap + Size <= screen.End.X ? Gap : -Gap - Size,
                point.Y + Gap + Size <= screen.End.Y ? Gap : -Gap - Size);
            var covered = !_excluded && _previewBounds.HasPoint(point);
            var window = _preview.DangerousGetHandle();
            if (_previewBounds.Position != position || _previewBounds.Size == Vector2i.Zero)
            {
                if (!SDL.SetWindowPosition(window, position.X, position.Y))
                    throw SDLFailure("move the color picker preview");
                _previewBounds = new Rect2i(position, new Vector2i(Size, Size));
            }

            if (covered) return;
            var color = WindowsScreenCapture.Pixel(point);
            if (color == _previewColor) return;
            var surface = SDL.GetWindowSurface(window);
            if (surface == 0) throw SDLFailure("get the color picker preview surface");
            var border = color.Luminance < .5f ? byte.MaxValue : (byte)0;
            var swatch = new SDL.Rect { X = 2, Y = 2, W = Size - 4, H = Size - 4 };
            if (!SDL.FillSurfaceRect(surface, 0, SDL.MapSurfaceRGB(surface, border, border, border)) ||
                !SDL.FillSurfaceRect(surface, in swatch, SDL.MapSurfaceRGB(surface, (byte)color.R8, (byte)color.G8, (byte)color.B8)) ||
                !SDL.UpdateWindowSurface(window))
                throw SDLFailure("paint the color picker preview");
            _previewColor = color;
        }

        public void Dispose()
        {
            try { _sampler.Dispose(); }
            finally { _preview?.Dispose(); }
        }
    }

    [LibraryImport("dwmapi.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DwmFlush();
}
