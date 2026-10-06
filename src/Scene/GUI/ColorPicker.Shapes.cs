namespace Electron2D;

public partial class ColorPicker
{
    private sealed class ShapeControl : Control
    {
        private readonly ColorPicker _owner;
        private readonly bool _bar;
        private bool _gesture, _ring, _editing = true;
        private Vector2 _keyDirection;
        private double _repeat;
        // ponytail: a retained 64 by 64 color mesh works on both renderers; increase subdivision or use an owned shader for larger surfaces.
        private readonly CanvasVertex[] _mesh = new CanvasVertex[64 * 64 * 6 + 64 * 6];
        private int _count;
        internal ShapeControl(ColorPicker owner, bool bar) { _owner = owner; _bar = bar; FocusMode = FocusMode.All; MouseFilter = MouseFilter.Stop; MouseDefaultCursorShape = CursorShape.Cross; }
        private bool Perceptual => _owner._shape is PickerShapeType.OKHSLCircle or PickerShapeType.OKHSRectangle or PickerShapeType.OKHLRectangle;
        private bool Circle => !_bar && _owner._shape is PickerShapeType.VHSCircle or PickerShapeType.OKHSLCircle;
        internal void CancelGesture() { if (_owner._shape != PickerShapeType.HSVWheel) _ring = false; _gesture = false; _keyDirection = Vector2.Zero; if (!IsDisposed) SetInternalProcessing(false, false); }
        internal void ResetShape() { CancelGesture(); _ring = _owner._shape == PickerShapeType.HSVWheel; _editing = true; }
        private Rect2 Square
        {
            get { var center = Size / 2; var radius = Size * (.42f / MathF.Sqrt(2)); return new(center - radius, radius * 2); }
        }
        private void Coordinates(Vector2 point)
        {
            var uv = new Vector2(Size.X == 0 ? 0 : point.X / Size.X, Size.Y == 0 ? 0 : point.Y / Size.Y).Clamp(Vector2.Zero, Vector2.One);
            if (_bar)
            {
                if (_owner._shape == PickerShapeType.HSVRectangle) _owner._h = uv.Y;
                else if (_owner._shape == PickerShapeType.VHSCircle) _owner._v = 1 - uv.Y;
                else if (_owner._shape == PickerShapeType.OKHLRectangle) _owner._os = 1 - uv.Y;
                else _owner._ol = 1 - uv.Y;
            }
            else if (Circle || _owner._shape == PickerShapeType.HSVWheel && _ring)
            {
                var direction = (point - Size / 2) / (Size / 2); var hue = Mathf.PosMod(MathF.Atan2(direction.Y, direction.X) / Mathf.Tau, 1); var saturation = Math.Min(1, direction.Length());
                if (Perceptual) { _owner._oh = hue; _owner._os = saturation; } else { _owner._h = hue; if (Circle) _owner._s = saturation; }
            }
            else if (_owner._shape == PickerShapeType.HSVWheel)
            { var rect = Square; uv = ((point - rect.Position) / rect.Size).Clamp(Vector2.Zero, Vector2.One); _owner._s = uv.X; _owner._v = 1 - uv.Y; }
            else if (Perceptual) { _owner._oh = uv.X; if (_owner._shape == PickerShapeType.OKHLRectangle) _owner._ol = 1 - uv.Y; else _owner._os = 1 - uv.Y; }
            else { _owner._s = uv.X; _owner._v = 1 - uv.Y; }
        }
        protected override void OnGUIInput(InputEvent input)
        {
            base.OnGUIInput(input);
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            {
                if (!button.Pressed) { if (_gesture) { _gesture = false; _owner.SurfaceColor(Perceptual, true); AcceptEvent(); } return; }
                if (!_bar && (_owner._shape == PickerShapeType.HSVWheel || Circle))
                {
                    var radius = ((button.Position - Size / 2) / (Size / 2)).Length(); if (radius > 1) return;
                    _ring = _owner._shape == PickerShapeType.HSVWheel && radius >= .84f;
                    if (_owner._shape == PickerShapeType.HSVWheel && !_ring && !Square.HasPoint(button.Position)) return;
                }
                _gesture = true; _editing = true; GrabFocus(true); Coordinates(button.Position); _owner.SurfaceColor(Perceptual, false); AcceptEvent(); return;
            }
            if (_gesture && input is InputEventMouseMotion motion) { Coordinates(motion.Position); _owner.SurfaceColor(Perceptual, false); AcceptEvent(); return; }
            if (Action(input, "ui_accept")) { _editing = !_editing; QueueRedraw(); AcceptEvent(); return; }
            if (_editing && Action(input, "ui_cancel")) { _editing = false; CancelGesture(); QueueRedraw(); AcceptEvent(); return; }
            if (!_editing)
            {
                if (!_bar && _owner._shape == PickerShapeType.HSVWheel && (_ring && Action(input, "ui_up") || !_ring && Action(input, "ui_down"))) { _ring = !_ring; QueueRedraw(); AcceptEvent(); }
                return;
            }
            var direction = new Vector2(Action(input, "ui_right") ? 1 : Action(input, "ui_left") ? -1 : 0, Action(input, "ui_down") ? 1 : Action(input, "ui_up") ? -1 : 0);
            if (input is InputEventJoypadMotion or InputEventJoypadButton)
            {
                _keyDirection = new(Input.IsActionPressed("ui_right") ? 1 : Input.IsActionPressed("ui_left") ? -1 : 0, Input.IsActionPressed("ui_down") ? 1 : Input.IsActionPressed("ui_up") ? -1 : 0);
                SetInternalProcessing(_keyDirection != Vector2.Zero, false); _repeat = 0;
            }
            if (direction != Vector2.Zero) { KeyMove(direction); AcceptEvent(); }
        }
        private static bool Action(InputEvent input, string name) => InputMap.HasAction(name) && input.IsActionPressed(name, allowEcho: true);
        private Vector2 Cursor()
        {
            if (_bar) return new(Size.X / 2, Size.Y * (_owner._shape == PickerShapeType.HSVRectangle ? _owner._h : 1 - (_owner._shape == PickerShapeType.VHSCircle ? _owner._v : _owner._shape == PickerShapeType.OKHLRectangle ? _owner._os : _owner._ol)));
            var hue = Perceptual ? _owner._oh : _owner._h; var sat = Perceptual ? _owner._os : _owner._s;
            if (Circle || _owner._shape == PickerShapeType.HSVWheel && _ring) return Size / 2 + Size / 2 * new Vector2(MathF.Cos(hue * Mathf.Tau), MathF.Sin(hue * Mathf.Tau)) * (_ring ? .92f : sat);
            if (_owner._shape == PickerShapeType.HSVWheel) { var rect = Square; return rect.Position + rect.Size * new Vector2(_owner._s, 1 - _owner._v); }
            return Size * (Perceptual ? new Vector2(_owner._oh, 1 - (_owner._shape == PickerShapeType.OKHLRectangle ? _owner._ol : _owner._os)) : new Vector2(_owner._s, 1 - _owner._v));
        }
        private void KeyMove(Vector2 direction)
        {
            if (_bar)
            {
                if (_owner._shape == PickerShapeType.HSVRectangle) _owner._h = Math.Clamp(_owner._h + direction.Y / 360, 0, 1);
                else Coordinates(Cursor() + new Vector2(0, direction.Y * Size.Y / 100));
            }
            else if (Circle || _owner._shape == PickerShapeType.HSVWheel && _ring) Coordinates(Cursor() + direction);
            else Coordinates(Cursor() + direction * (_owner._shape == PickerShapeType.HSVWheel ? Square.Size : Size) / 100);
            _owner.SurfaceColor(Perceptual, true);
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationInternalProcess && _keyDirection != Vector2.Zero) { _repeat -= ProcessDeltaTime; if (_repeat <= 0) { _repeat += .05; KeyMove(_keyDirection); } }
            if (what is NotificationExitTree or NotificationFocusExit || what == NotificationVisibilityChanged && !IsVisibleInTree) CancelGesture();
            if (what is NotificationFocusEnter or NotificationFocusExit) QueueRedraw();
        }
        private Color Surface(float x, float y)
        {
            if (_bar) return _owner._shape switch { PickerShapeType.HSVRectangle => Color.FromHSV(y, 1, 1), PickerShapeType.VHSCircle => Color.FromHSV(_owner._h, _owner._s, 1 - y), PickerShapeType.OKHLRectangle => Color.FromOKHSL(_owner._oh, 1 - y, _owner._ol), _ => Color.FromOKHSL(_owner._oh, _owner._os, 1 - y) };
            if (Perceptual) return Color.FromOKHSL(x, _owner._shape == PickerShapeType.OKHLRectangle ? _owner._os : 1 - y, _owner._shape == PickerShapeType.OKHLRectangle ? 1 - y : _owner._ol);
            return Color.FromHSV(_owner._h, x, 1 - y);
        }
        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            _mesh[_count++] = new(a, ca); _mesh[_count++] = new(b, cb); _mesh[_count++] = new(c, cc);
            _mesh[_count++] = new(a, ca); _mesh[_count++] = new(c, cc); _mesh[_count++] = new(d, cd);
        }
        protected override void OnDraw()
        {
            _count = 0; if (Size.X <= 0 || Size.Y <= 0) return;
            if (Circle || !_bar && _owner._shape == PickerShapeType.HSVWheel)
            {
                var center = Size / 2;
                var wheel = _owner._shape == PickerShapeType.HSVWheel;
                var rings = wheel ? 1 : 32;
                for (var r = 0; r < rings; r++) for (var i = 0; i < 64; i++)
                    {
                        var h0 = i / 64f; var h1 = (i + 1) / 64f; var r0 = wheel ? .84f : r / 32f; var r1 = wheel ? 1 : (r + 1) / 32f;
                        var p0 = new Vector2(MathF.Cos(h0 * Mathf.Tau), MathF.Sin(h0 * Mathf.Tau)); var p1 = new Vector2(MathF.Cos(h1 * Mathf.Tau), MathF.Sin(h1 * Mathf.Tau));
                        Color C(float h, float s) => Perceptual ? Color.FromOKHSL(h, s, _owner._ol) : Color.FromHSV(h, wheel ? 1 : s, wheel ? 1 : _owner._v);
                        if (r0 == 0) { _mesh[_count++] = new(center, C(h0, 0)); _mesh[_count++] = new(center + center * p0 * r1, C(h0, r1)); _mesh[_count++] = new(center + center * p1 * r1, C(h1, r1)); }
                        else Quad(center + center * p0 * r0, center + center * p0 * r1, center + center * p1 * r1, center + center * p1 * r0, C(h0, r0), C(h0, r1), C(h1, r1), C(h1, r0));
                    }
            }
            if (!Circle)
            {
                var rect = !_bar && _owner._shape == PickerShapeType.HSVWheel ? Square : new Rect2(Vector2.Zero, Size);
                var columns = _bar ? 1 : 64;
                for (var y = 0; y < 64; y++) for (var x = 0; x < columns; x++)
                    {
                        var x0 = x / (float)columns; var x1 = (x + 1) / (float)columns; var y0 = y / 64f; var y1 = (y + 1) / 64f;
                        Quad(rect.Position + rect.Size * new Vector2(x0, y0), rect.Position + rect.Size * new Vector2(x1, y0), rect.Position + rect.Size * new Vector2(x1, y1), rect.Position + rect.Size * new Vector2(x0, y1), Surface(x0, y0), Surface(x1, y0), Surface(x1, y1), Surface(x0, y1));
                    }
            }
            if (_count != 0) DrawTriangleArray(_mesh.AsSpan(0, _count), null);
            if (_bar && _owner._shape == PickerShapeType.HSVRectangle && _owner.GetThemeIcon("color_hue") is { } hueTexture) DrawTextureRect(hueTexture, new(Vector2.Zero, new(Size.Y, Size.X)), false, Colors.White, true);
            var cursor = Cursor();
            if (_bar) { DrawLine(new(0, cursor.Y), new(Size.X, cursor.Y), Colors.White, 2); if (_owner.GetThemeIcon("bar_arrow") is { } arrow) DrawTexture(arrow, new(0, cursor.Y - arrow.GetHeight() / 2)); }
            else { if (_owner.GetThemeIcon("picker_cursor_bg") is { } bg) DrawTexture(bg, cursor - bg.GetSize() / 2, _owner._normalized); if (_owner.GetThemeIcon("picker_cursor") is { } icon) DrawTexture(icon, cursor - icon.GetSize() / 2); }
            if (HasFocus()) { if (!_editing) { if (Circle || _ring) DrawCircle(Size / 2, Math.Min(Size.X, Size.Y) / 2, _owner.GetThemeColor("focused_not_editing_cursor_color")); else DrawRect(new(Vector2.Zero, Size), _owner.GetThemeColor("focused_not_editing_cursor_color")); } if (_owner.GetThemeStyleBox(Circle || _ring ? "picker_focus_circle" : "picker_focus_rectangle") is { } focus) DrawStyleBox(focus, new(Vector2.Zero, Size)); }
        }
    }
    private sealed class ChannelSlider(ColorPicker owner, int channel) : HSlider
    {
        private readonly CanvasVertex[] _mesh = new CanvasVertex[64 * 6];
        protected override void OnDraw()
        {
            if (!owner._colorized || channel == 4 || Size.X <= 0) return;
            var height = Math.Min(16, Size.Y);
            if (channel == 3) Checker(this, new(Vector2.Zero, new(Size.X, height)), owner.GetThemeIcon("sample_bg"));
            for (var i = 0; i < 64; i++)
            {
                var left = i / 64f; var right = (i + 1) / 64f;
                Color C(float value)
                {
                    var color = owner._normalized;
                    if (channel == 3) { color.A = value; return color; }
                    if (owner._mode == ColorModeType.RGB) { color[channel] = value; color.A = 1; return color; }
                    if (owner._mode == ColorModeType.Linear) { color = color.SRGBToLinear(); color[channel] = value; color.A = 1; return color.LinearToSRGB(); }
                    if (owner._mode == ColorModeType.HSV) return Color.FromHSV(channel == 0 ? value : owner._h, channel == 1 ? value : owner._s, channel == 2 ? value : owner._v);
                    return Color.FromOKHSL(channel == 0 ? value : owner._oh, channel == 1 ? value : owner._os, channel == 2 ? value : owner._ol);
                }
                var a = new Vector2(Size.X * left, 0); var b = new Vector2(Size.X * right, 0); var c = new Vector2(b.X, height); var d = new Vector2(a.X, height); var ca = C(left); var cb = C(right); var index = i * 6;
                _mesh[index] = new(a, ca); _mesh[index + 1] = new(b, cb); _mesh[index + 2] = new(c, cb); _mesh[index + 3] = new(a, ca); _mesh[index + 4] = new(c, cb); _mesh[index + 5] = new(d, ca);
            }
            DrawTriangleArray(_mesh, null);
        }
    }
    private static void Checker(CanvasItem item, Rect2 rect, Texture? texture)
    { if (texture != null) item.DrawTextureRect(texture, rect, true); else item.DrawRect(rect, new Color(.65f, .65f, .65f)); }
    private sealed class PreviewControl(ColorPicker owner) : Control
    {
        internal Color OldColor;
        internal bool DisplayOld;
        protected override void OnDraw()
        {
            var rect = new Rect2(Vector2.Zero, new(Size.X, Size.Y * .95f)); var half = new Rect2(rect.Position, new Vector2(rect.Size.X / 2, rect.Size.Y));
            if (DisplayOld) { DrawColor(half, OldColor); rect = new Rect2(new Vector2(half.End.X, rect.Position.Y), half.Size); if (OldColor != owner._color && owner.GetThemeIcon("sample_revert") is { } revert) DrawTexture(revert, half.GetCenter() - revert.GetSize() / 2, OldColor.Luminance < .455f ? Colors.White : Colors.Black); if (HasFocus() && owner.GetThemeStyleBox("sample_focus") is { } focus) DrawStyleBox(focus, half); }
            DrawColor(rect, owner._color);
        }
        private void DrawColor(Rect2 rect, Color color) { Checker(this, rect, owner.GetThemeIcon("sample_bg")); DrawRect(rect, color); if ((color.R > 1 || color.G > 1 || color.B > 1) && owner.GetThemeIcon("overbright_indicator") is { } icon) DrawTexture(icon, rect.Position); }
        protected override void OnGUIInput(InputEvent input)
        { if (DisplayOld && (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button && button.Position.X < Size.X / 2 || InputMap.HasAction("ui_accept") && input.IsActionPressed("ui_accept"))) { owner.SetColor(OldColor, true); owner.ColorChanged?.Invoke(owner._color); AcceptEvent(); } }
    }
}
