namespace Electron2D;

public partial class ColorPicker
{
    private LinuxPortalColorPicker? _portalSample;
    private Popup? _portalSamplePopup;

    private void BeginSample()
    {
        if (!IsInsideTree) return;
        if (CanSampleDesktop())
        {
            Tree!.Defer(BeginDesktopSample);
            return;
        }
        if (Tree?.Root is Window { GUIEmbedSubwindows: false, NativeDisplay.UsesPortalColorPicker: true })
        {
            Tree.Defer(BeginPortalSample);
            return;
        }
        BeginViewportSample();
    }

    private void BeginPortalSample()
    {
        if (!IsInsideTree || !IsVisibleInTree || !_samplerVisible || _portalSample is not null) return;
        if (DisplayServer.Service is not { } display) return;
        var request = display.BeginPortalColorPick(CompletePortalSample);
        if (request is null)
        {
            BeginViewportSample();
            return;
        }

        var popup = GetWindow() is Popup { PopupWMHint: true } host && !host.IsEmbedded() ? host : null;
        try
        {
            // Release the native popup's input grab before the system selector receives clicks.
            popup?.Hide();
        }
        catch
        {
            display.CancelPortalColorPick(request);
            throw;
        }

        if (!IsInsideTree || !_samplerVisible || !CanReceivePortalSample(popup))
        {
            display.CancelPortalColorPick(request);
            return;
        }
        _portalSamplePopup = popup;
        _portalSample = request;
    }

    private bool CanReceivePortalSample(Popup? popup) =>
        popup is null ? IsVisibleInTree : ReferenceEquals(GetWindow(), popup) && popup.Parent is not Control { IsVisibleInTree: false };

    private void CompletePortalSample(Color? color)
    {
        var popup = _portalSamplePopup;
        _portalSample = null;
        _portalSamplePopup = null;
        if (color is { } selected && IsInsideTree && _samplerVisible && CanReceivePortalSample(popup))
            CommitSample(selected);
    }

    internal void CancelPortalSample()
    {
        if (_portalSample is { } request)
            DisplayServer.Service?.CancelPortalColorPick(request);
        _portalSample = null;
        _portalSamplePopup = null;
    }

    private void BeginViewportSample()
    {
        try
        {
            var viewport = GetViewport()!; while (viewport is Window { Embedder: { } parent }) viewport = parent;
            using var image = viewport.GetTexture().GetImage() ?? throw new InvalidOperationException("The application viewport has no completed rendered image.");
            _sampler ??= new SamplerPopup(this) { Name = "_color_sampler" }; if (_sampler.Parent == null) AddChild(_sampler, InternalMode.Front);
            _sampler.Capture(image); _sampler.PopupFromViewport(viewport, new(Vector2i.Zero, (Vector2i)viewport.GetVisibleRect().Size)); _sampler.FitCapture();
        }
        catch (Exception error) { ShowError(error.Message); }
    }
    private void CommitSample(Color color)
    {
        SetColor(color, true);
        AddRecentPreset(color);
        BeginEventEmission(_publisherColorChanged.Handlers)?.Invoke(color);
    }

    private sealed class SamplerPopup : Popup
    {
        private readonly ColorPicker _owner;
        private readonly SamplerControl _control;
        private Image? _image;
        private ImageTexture? _texture;
        private Color _selected;
        private Vector2 _point;
        internal SamplerPopup(ColorPicker owner) { _owner = owner; _control = new SamplerControl(this) { MouseDefaultCursorShape = CursorShape.Cross }; AddChild(_control); SizeChanged += FitCapture; PopupHide += ReleaseCapture; }
        internal void Capture(Image image) { ReleaseCapture(); _image = image.Duplicate() as Image; _texture = ImageTexture.CreateFromImage(image); _selected = _owner._color; }
        internal void ReleaseCapture() { _texture?.Dispose(); _texture = null; _image?.Dispose(); _image = null; }
        internal void FitCapture() { _control.Position = Vector2.Zero; _control.Size = Size; }
        protected override void Dispose(bool disposing) { if (disposing) ReleaseCapture(); base.Dispose(disposing); }
        private void Input(InputEvent input)
        {
            if (_image == null) return;
            if (input is InputEventMouseMotion motion)
            {
                _point = motion.Position; var uv = _point / _control.Size; var pixel = new Vector2i(Math.Clamp((int)(uv.X * _image.Width), 0, _image.Width - 1), Math.Clamp((int)(uv.Y * _image.Height), 0, _image.Height - 1)); _selected = _image.GetPixel(pixel.X, pixel.Y); _control.QueueRedraw();
            }
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) { var color = _selected; Hide(); _owner.CommitSample(color); }
            else if (input is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }) Hide();
        }
        private sealed class SamplerControl(SamplerPopup popup) : Control
        {
            protected override void OnGUIInput(InputEvent input) { popup.Input(input); if (!IsDisposed) AcceptEvent(); }
            protected override void OnDraw()
            {
                if (popup._texture == null || Size.X <= 0 || Size.Y <= 0) return; DrawTextureRect(popup._texture, new(Vector2.Zero, Size), false);
                var rect = new Rect2(popup._point + new Vector2(12, 12), new(55, 72)); rect.Position = rect.Position.Clamp(Vector2.Zero, (Size - rect.Size).Max(Vector2.Zero));
                DrawRect(rect, popup._selected.Luminance < .5 ? Colors.White : Colors.Black);
                var center = popup._point / Size * popup._texture.GetSize(); DrawTextureRectRegion(popup._texture, new(rect.Position + new Vector2(2, 2), new(51, 51)), new(center - new Vector2(8, 8), new(17, 17)));
                DrawRect(new(rect.Position + new Vector2(2, 55), new(51, 15)), popup._selected);
            }
        }
    }
}
