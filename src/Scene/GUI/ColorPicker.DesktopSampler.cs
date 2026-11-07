namespace Electron2D;

public partial class ColorPicker
{
    private DesktopSamplerPopup? _desktopSampler;

    private bool CanSampleDesktop() =>
        Tree?.Root is Window { GUIEmbedSubwindows: false, NativeDisplay.CanSampleDesktopColor: true };

    private void BeginDesktopSample()
    {
        if (!IsInsideTree || !IsVisibleInTree || !_samplerVisible || _desktopSampler?.Visible == true) return;
        try
        {
            _desktopSampler ??= new DesktopSamplerPopup(this) { Name = "_desktop_color_sampler" };
            if (_desktopSampler.Parent == null) AddChild(_desktopSampler, InternalMode.Front);
            _desktopSampler.Start();
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private sealed class DesktopSamplerPopup : Popup
    {
        private static readonly Vector2i PreviewSize = new(28, 28);
        private readonly ColorPicker _owner;
        private readonly ColorRect _border = new() { Size = PreviewSize, Color = Colors.Black, MouseFilter = MouseFilter.Ignore };
        private readonly ColorRect _swatch = new() { Position = new(2, 2), Size = new(24, 24), MouseFilter = MouseFilter.Ignore };
        private SceneTree? _session;
        private X11ColorSampler? _grab;
        private Color _openingColor;
        private bool _leftDown;

        internal DesktopSamplerPopup(ColorPicker owner)
        {
            _owner = owner;
            ForceNative = true;
            WrapControls = false;
            AddChild(_border);
            _border.AddChild(_swatch);
            PopupHide += Cancel;
        }

        internal void Start()
        {
            _openingColor = _owner._color;
            _grab = DisplayServer.Service!.OpenDesktopColorSampler();
            try
            {
                var point = DisplayServer.MouseGetPosition();
                Popup(new(PreviewPosition(point), PreviewSize));
                if (!Visible) { Cancel(); return; }
                _session = Tree!;
                _session.ProcessFrameStarted += UpdateSample;
            }
            catch
            {
                Cancel();
                if (Visible) Hide();
                throw;
            }
        }

        private void UpdateSample(SceneTree tree)
        {
            if (_grab is not { } grab) return;
            if (!_owner.IsVisibleInTree || !_owner._samplerVisible) { Hide(); return; }
            try
            {
                while (grab.TryReadButton(out var button, out var pressed, out var point))
                {
                    if (button == 3 && !pressed) { Hide(); return; }
                    if (button != 1) continue;
                    if (pressed) _leftDown = true;
                    else if (_leftDown)
                    {
                        var color = Sample(point);
                        Stop();
                        Hide();
                        _owner.CommitSample(color);
                        return;
                    }
                }
                Sample(DisplayServer.MouseGetPosition());
            }
            catch (Exception error) when (_grab is not null && error is IOException or ArgumentOutOfRangeException)
            {
                Hide();
                _owner.ShowError(error.Message);
            }
        }

        private Color Sample(Vector2i point)
        {
            var position = PreviewPosition(point);
            if (Position != position) Position = position;
            var color = DisplayServer.ScreenGetPixel(point);
            if (_swatch.Color != color)
            {
                _swatch.Color = color;
                _border.Color = color.Luminance < .5 ? Colors.White : Colors.Black;
            }
            if (_owner._color != color) _owner.SetColor(color, true);
            return color;
        }

        private static Vector2i PreviewPosition(Vector2i point)
        {
            const int gap = 8;
            var screen = DisplayServer.ScreenWithMouseFocus;
            var end = DisplayServer.ScreenGetPosition(screen) + DisplayServer.ScreenGetSize(screen);
            var offset = new Vector2i(point.X + gap + PreviewSize.X <= end.X ? gap : -gap - PreviewSize.X,
                point.Y + gap + PreviewSize.Y <= end.Y ? gap : -gap - PreviewSize.Y);
            return point + offset;
        }

        private void Cancel()
        {
            if (_grab == null) return;
            Stop();
            if (!_owner.IsDisposed && !_owner._disposing && _owner.Tree is not { IsClosing: true } && _owner._color != _openingColor)
                _owner.SetColor(_openingColor, true);
        }

        private void Stop()
        {
            _grab?.Dispose();
            _grab = null;
            _leftDown = false;
            if (_session is { } session) session.ProcessFrameStarted -= UpdateSample;
            _session = null;
        }

        protected override void OnNotification(int what)
        {
            if (what == NotificationExitTree) Cancel();
            base.OnNotification(what);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Stop();
            base.Dispose(disposing);
        }
    }
}
