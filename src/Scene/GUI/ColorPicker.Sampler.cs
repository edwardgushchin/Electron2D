namespace Electron2D;

public partial class ColorPicker
{
    private void BeginSample()
    {
        if (!IsInsideTree) return;
        try
        {
            var viewport = GetViewport()!; while (viewport is Window { Embedder: { } parent }) viewport = parent;
            using var image = viewport.GetTexture().GetImage() ?? throw new InvalidOperationException("The application viewport has no completed rendered image.");
            _sampler ??= new SamplerPopup(this) { Name = "_color_sampler" }; if (_sampler.Parent == null) AddChild(_sampler, InternalMode.Front);
            _sampler.Capture(image); _sampler.Position = Vector2i.Zero; _sampler.Size = (Vector2i)viewport.GetVisibleRect().Size; _sampler.Popup(); _sampler.FitCapture();
        }
        catch (Exception error) { ShowError(error.Message); }
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
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) { var color = _selected; Hide(); _owner.SetColor(color, true); _owner.AddRecentPreset(color); _owner.ColorChanged?.Invoke(color); }
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
