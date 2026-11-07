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
            var display = DisplayServer.Service!;
            _desktopSampler ??= new DesktopSamplerPopup(this, display.UsesWindowsScreenCapture, display.CanMagnifyDesktopColor) { Name = "_desktop_color_sampler" };
            if (_desktopSampler.Parent == null) AddChild(_desktopSampler, InternalMode.Front);
            _desktopSampler.Start();
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private sealed class DesktopSamplerPopup : Popup
    {
        private readonly ColorPicker _owner;
        private readonly Vector2i _previewSize;
        private readonly ColorRect _border;
        private readonly ColorRect _swatch;
        private readonly TextureRect? _zoom;
        private ImageTexture? _zoomTexture;
        private SceneTree? _session;
        private IDesktopColorSampler? _grab;
        private Color _openingColor;

        internal DesktopSamplerPopup(ColorPicker owner, bool windows, bool magnify)
        {
            _owner = owner;
            _previewSize = magnify ? new(55, 72) : new(28, 28);
            _border = new ColorRect { Size = _previewSize, Color = Colors.Black, MouseFilter = MouseFilter.Ignore };
            _swatch = new ColorRect
            {
                Position = magnify ? new(2, 55) : new(2, 2),
                Size = magnify ? new(51, 15) : new(24, 24),
                MouseFilter = MouseFilter.Ignore,
            };
            ForceNative = true;
            WrapControls = false;
            ExcludeFromCapture = magnify;
            if (windows)
            {
                // SDL menu popups cannot become topmost above other applications.
                PopupWMHint = PopupWindow = MinimizeDisabled = MaximizeDisabled = Transient = false;
                AlwaysOnTop = true;
            }
            AddChild(_border);
            _border.AddChild(_swatch);
            if (magnify)
            {
                _zoom = new TextureRect
                {
                    Position = new(2, 2),
                    Size = new(51, 51),
                    ExpandMode = TextureRectExpandMode.IgnoreSize,
                    StretchMode = TextureStretchMode.Scale,
                    TextureFilter = TextureFilter.Nearest,
                    MouseFilter = MouseFilter.Ignore,
                };
                _border.AddChild(_zoom);
            }
            PopupHide += Cancel;
        }

        internal void Start()
        {
            _openingColor = _owner._color;
            _grab = DisplayServer.Service!.OpenDesktopColorSampler();
            try
            {
                var point = DisplayServer.MouseGetPosition();
                var position = PreviewPosition(point, DisplayServer.Service!.DesktopColorSampleBounds(point));
                if (PopupWMHint) Popup(new(position, _previewSize));
                else
                {
                    Position = position;
                    Size = _previewSize;
                    Show();
                    GrabFocus();
                }
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
                if (grab.TryReadSelection(out var point, out var accepted))
                {
                    if (!accepted) { Hide(); return; }
                    var color = Sample(point);
                    Stop();
                    Hide();
                    _owner.CommitSample(color);
                    return;
                }
                Sample(DisplayServer.MouseGetPosition());
            }
            catch (Exception error) when (_grab is not null && error is IOException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                Hide();
                _owner.ShowError(error.Message);
            }
        }

        private Color Sample(Vector2i point)
        {
            var screen = DisplayServer.Service!.DesktopColorSampleBounds(point);
            var position = PreviewPosition(point, screen);
            if (Position != position) Position = position;
            var color = _zoom is null ? DisplayServer.ScreenGetPixel(point) : UpdateZoom(point, screen);
            if (_swatch.Color != color)
            {
                _swatch.Color = color;
                _border.Color = color.Luminance < .5 ? Colors.White : Colors.Black;
            }
            if (_owner._color != color) _owner.SetColor(color, true);
            return color;
        }

        private Color UpdateZoom(Vector2i point, Rect2i screen)
        {
            var crop = screen.Intersection(new(point - new Vector2i(8, 8), new(17, 17)));
            using var image = DisplayServer.ScreenGetImageRect(crop);
            if (_zoomTexture is { } existing && existing.GetSize() == image.Size)
                existing.Update(image);
            else
            {
                var texture = ImageTexture.CreateFromImage(image);
                var previous = _zoomTexture;
                _zoom!.Texture = _zoomTexture = texture;
                previous?.Dispose();
            }
            var local = point - crop.Position;
            return image.GetPixel(local.X, local.Y);
        }

        private Vector2i PreviewPosition(Vector2i point, Rect2i screen)
        {
            if (_zoom is not null)
            {
                var position = point - new Vector2i(28, 28);
                var last = (screen.End - _previewSize).Max(screen.Position);
                return position.Clamp(screen.Position, last);
            }

            const int gap = 8;
            var offset = new Vector2i(point.X + gap + _previewSize.X <= screen.End.X ? gap : -gap - _previewSize.X,
                point.Y + gap + _previewSize.Y <= screen.End.Y ? gap : -gap - _previewSize.Y);
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
            if (_zoomTexture is { } texture)
            {
                _zoomTexture = null;
                _zoom!.Texture = null;
                texture.Dispose();
            }
            if (_session is { } session) session.ProcessFrameStarted -= UpdateSample;
            _session = null;
        }

        internal override void HandlePopupInput(InputEvent input)
        {
            if (!PopupWMHint && input.IsActionPressed("ui_cancel", false, true)) RequestEmbeddedClose();
            else base.HandlePopupInput(input);
        }

        protected override void OnNotification(int what)
        {
            if (what == NotificationExitTree) Cancel();
            else if (what == NotificationApplicationFocusOut && !PopupWMHint && Visible) RequestEmbeddedClose();
            base.OnNotification(what);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Stop();
            base.Dispose(disposing);
        }
    }
}
