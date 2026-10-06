namespace Electron2D;

/// <summary>A color editor with shaped color spaces, numeric channels, intensity, text and local swatches.</summary>
/// <remarks>Programmatic color assignments are silent. User edits publish ColorChanged; DeferredMode defers
/// slider and surface gestures until release. Internal controls and their resources remain owned by the picker.
/// Palette file I/O and completed viewport sampling are explicit cold operations on the scene owner.</remarks>
public partial class ColorPicker : VBoxContainer
{
    /// <summary>Selects the numeric channel encoding.</summary>
    public enum ColorModeType
    {
        /// <summary>Nonlinear sRGB channels in the range zero through 255.</summary>
        RGB = 0,
        /// <summary>Hue in degrees and saturation/value in percent.</summary>
        HSV = 1,
        /// <summary>Linear RGB channels in the unit range.</summary>
        Linear = 2,
        /// <summary>Perceptual OKHSL hue in degrees and saturation/lightness in percent.</summary>
        OKHSL = 3
    }
    /// <summary>Selects the spatial color editing surface.</summary>
    public enum PickerShapeType
    {
        /// <summary>Saturation/value rectangle and hue bar.</summary>
        HSVRectangle = 0,
        /// <summary>Hue ring surrounding a saturation/value square.</summary>
        HSVWheel = 1,
        /// <summary>Hue/saturation circle and value bar.</summary>
        VHSCircle = 2,
        /// <summary>Perceptual hue/saturation circle and lightness bar.</summary>
        OKHSLCircle = 3,
        /// <summary>Hides the surface and its shape selector.</summary>
        None = 4,
        /// <summary>Perceptual hue/saturation rectangle and lightness bar.</summary>
        OKHSRectangle = 5,
        /// <summary>Perceptual hue/lightness rectangle and saturation bar.</summary>
        OKHLRectangle = 6
    }
    private Color _color = Colors.White, _normalized = Colors.White;
    private ColorModeType _mode;
    private PickerShapeType _shape;
    private bool _editAlpha = true, _editIntensity = true, _modesVisible = true, _slidersVisible = true, _hexVisible = true, _samplerVisible = true, _presetsVisible = true, _canAdd = true, _deferred;
    private bool _updating, _disposing, _dragging, _textConstructor, _colorized = true;
    private float _h, _s, _v = 1, _oh, _os, _ol = 1;
    private double _intensity;
    private readonly VBoxContainer _content, _swatches;
    private readonly HBoxContainer _surfaceRow, _sampleRow, _modeRow, _textRow, _recentRow;
    private readonly GridContainer _channels, _presetGrid;
    private readonly ChannelSlider[] _sliders = new ChannelSlider[5];
    private readonly SpinBox[] _values = new SpinBox[5];
    private readonly Label[] _labels = new Label[5];
    private readonly Button[] _modeButtons = new Button[4];
    private readonly ShapeControl _surface, _bar;
    private readonly PreviewControl _preview;
    private readonly Button _pick, _textType, _copy, _add;
    private readonly MenuButton _shapeMenu, _modeMenu, _paletteMenu;
    private readonly LineEdit _text;
    private readonly Label _textLabel, _paletteLabel;
    private readonly List<Color> _presets = [], _recent = [];
    private readonly List<SwatchButton> _presetButtons = [], _recentButtons = [];
    private string _palettePath = "";
    private FileDialog? _paletteDialog;
    private AcceptDialog? _error;
    private SamplerPopup? _sampler;
    private readonly Action<string> _submitText;
    /// <summary>Creates a white RGB editor with all sections enabled and an HSV rectangle.</summary>
    public ColorPicker()
    {
        _submitText = SubmitText;
        var inset = new InsetContainer(this) { Name = "_color_picker_inset" }; AddChild(inset, InternalMode.Front);
        _content = new VBoxContainer { Name = "_color_picker" }; inset.AddChild(_content);
        _surfaceRow = new HBoxContainer { Name = "Shape" }; _content.AddChild(_surfaceRow);
        _surface = new ShapeControl(this, false) { Name = "Surface", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _bar = new ShapeControl(this, true) { Name = "Bar" }; _surfaceRow.AddChild(_surface); _surfaceRow.AddChild(_bar);
        _sampleRow = new HBoxContainer { Name = "Sample" }; _content.AddChild(_sampleRow);
        _pick = new Button("Pick") { Name = "Pick", TooltipText = "Pick a color from this application window." }; _sampleRow.AddChild(_pick); _pick.Pressed += BeginSample;
        _preview = new PreviewControl(this) { Name = "Preview", SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new(80, 28) }; _sampleRow.AddChild(_preview);
        _shapeMenu = new MenuButton("Shape") { Name = "ShapeMenu", Flat = false, FocusMode = FocusMode.All }; _sampleRow.AddChild(_shapeMenu);
        for (var i = 0; i < 7; i++) if (i != 4) _shapeMenu.GetPopup().AddRadioCheckItem(ShapeNames[i], i);
        _shapeMenu.GetPopup().IDPressed += id => PickerShape = (PickerShapeType)id;
        _modeRow = new HBoxContainer { Name = "Modes" }; _content.AddChild(_modeRow);
        for (var i = 0; i < 4; i++) { var index = i; var button = new Button(ModeNames[i]) { Name = ModeNames[i], ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill }; _modeButtons[i] = button; _modeRow.AddChild(button); button.Pressed += () => ColorMode = (ColorModeType)index; }
        _modeMenu = new MenuButton("Mode") { Flat = false, FocusMode = FocusMode.All }; _modeRow.AddChild(_modeMenu);
        for (var i = 0; i < 4; i++) _modeMenu.GetPopup().AddRadioCheckItem(ModeNames[i], i);
        _modeMenu.GetPopup().AddSeparator(); _modeMenu.GetPopup().AddCheckItem("Colorized Sliders", 4);
        _modeMenu.GetPopup().IDPressed += id => { if (id == 4) { _colorized = !_colorized; Refresh(); } else ColorMode = (ColorModeType)id; };
        _channels = new GridContainer { Name = "Channels", Columns = 3 }; _content.AddChild(_channels);
        for (var i = 0; i < 5; i++)
        {
            var index = i;
            _labels[i] = new Label { Name = "Label" + i, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            _sliders[i] = new ChannelSlider(this, i) { Name = "Channel" + i, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, FocusMode = FocusMode.All };
            _values[i] = new SpinBox { Name = "Value" + i, SelectAllOnFocus = true }; _sliders[i].Share(_values[i]);
            _values[i].GetLineEdit().Alignment = HorizontalAlignment.Right;
            _channels.AddChild(_labels[i]); _channels.AddChild(_sliders[i]); _channels.AddChild(_values[i]);
            _sliders[i].ValueChanged += _ => ChannelChanged(index); _sliders[i].DragStarted += () => _dragging = true; _sliders[i].DragEnded += _ => EndChannelDrag();
            _sliders[i].GUIInput += RememberOnRelease; _values[i].GUIInput += RememberOnRelease;
        }
        _textRow = new HBoxContainer { Name = "Text" }; _content.AddChild(_textRow);
        _textLabel = new Label { Text = "Hex" }; _textType = new Button("#") { Name = "TextType", TooltipText = "Switch color text between hexadecimal and numeric constructor." };
        _text = new LineEdit { Name = "ColorText", SizeFlagsHorizontal = SizeFlags.ExpandFill, SelectAllOnFocus = true }; _copy = new Button("Copy") { Name = "Copy" };
        _textRow.AddChild(_textLabel); _textRow.AddChild(_textType); _textRow.AddChild(_text); _textRow.AddChild(_copy);
        _text.TextSubmitted += QueueText; _text.FocusExited += TextFocusExit; _textType.Pressed += () => { _textConstructor = !_textConstructor; RefreshText(); }; _copy.Pressed += () => DisplayServer.ClipboardSet(_text.Text);
        _swatches = new VBoxContainer { Name = "Swatches" }; _content.AddChild(_swatches);
        var paletteHeader = new HBoxContainer { Name = "PaletteHeader" }; _swatches.AddChild(paletteHeader);
        _presetFold = new Button("Swatches") { ToggleMode = true, ButtonPressed = true }; paletteHeader.AddChild(_presetFold); _paletteLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill }; paletteHeader.AddChild(_paletteLabel);
        _paletteMenu = new MenuButton("Palette") { Name = "PaletteMenu", Flat = false, FocusMode = FocusMode.All }; paletteHeader.AddChild(_paletteMenu);
        _paletteMenu.GetPopup().AddItem("Save", 0); _paletteMenu.GetPopup().AddItem("Save As", 1); _paletteMenu.GetPopup().AddItem("Load", 2); _paletteMenu.GetPopup().AddItem("Clear", 3); _paletteMenu.GetPopup().IDPressed += PaletteCommand;
        _presetGrid = new GridContainer { Name = "Presets", Columns = 9 }; _swatches.AddChild(_presetGrid);
        _add = new Button("+") { Name = "Add", TooltipText = "Add the current color to swatches.", CustomMinimumSize = new(24, 24) }; _presetGrid.AddChild(_add); _add.Pressed += AddFromUser;
        _recentFold = new Button("Recent Colors") { ToggleMode = true, ButtonPressed = true }; _swatches.AddChild(_recentFold); _recentRow = new HBoxContainer { Name = "Recent" }; _swatches.AddChild(_recentRow);
        _presetFold.Toggled += v => { _presetGrid.Visible = v; RefreshIcons(); }; _recentFold.Toggled += v => { _recentRow.Visible = v; RefreshIcons(); };
        Refresh();
    }
    private static readonly string[] ModeNames = ["RGB", "HSV", "Linear", "OKHSL"];
    private static readonly string[] ShapeNames = ["HSV Rectangle", "HSV Wheel", "VHS Circle", "OKHSL Circle", "None", "OK HS Rectangle", "OK HL Rectangle"];
    /// <summary>Gets or sets the selected finite color without publishing ColorChanged.</summary><value>Opaque white initially; signed and HDR channels are retained.</value>
    public Color Color { get { CheckPicker(); return _color; } set { EnsureMutable(); ValidateColor(value); SetColor(value, true); } }
    /// <summary>Gets or sets the numeric channel mode without changing the color.</summary><value>RGB initially.</value>
    public ColorModeType ColorMode { get { CheckPicker(); return _mode; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; Refresh(); } }
    /// <summary>Gets or sets the spatial editor shape without changing the color.</summary><value>HSVRectangle initially.</value>
    public PickerShapeType PickerShape { get { CheckPicker(); return _shape; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _shape = value; _surface.ResetShape(); _bar.CancelGesture(); Refresh(); } }
    /// <summary>Gets or sets alpha channel visibility.</summary><value>True initially; hidden alpha is preserved.</value>
    public bool EditAlpha { get { CheckPicker(); return _editAlpha; } set { EnsureMutable(); _editAlpha = value; Refresh(); } }
    /// <summary>Gets or sets linear exposure editing.</summary><value>True initially; exposure multiplies linear RGB by two to the intensity power.</value>
    public bool EditIntensity { get { CheckPicker(); return _editIntensity; } set { EnsureMutable(); _editIntensity = value; SetColor(_color, true); } }
    /// <summary>Gets or sets publishing slider and surface edits only at the end of a gesture.</summary><value>False initially; programmatic writes remain silent.</value>
    public bool DeferredMode { get { CheckPicker(); return _deferred; } set { EnsureMutable(); _deferred = value; } }
    /// <summary>Gets or sets visibility of the mode controls.</summary><value>True initially.</value>
    public bool ColorModesVisible { get { CheckPicker(); return _modesVisible; } set { EnsureMutable(); _modesVisible = value; Refresh(); } }
    /// <summary>Gets or sets visibility of channel and intensity controls.</summary><value>True initially.</value>
    public bool SlidersVisible { get { CheckPicker(); return _slidersVisible; } set { EnsureMutable(); _slidersVisible = value; Refresh(); } }
    /// <summary>Gets or sets visibility of color text controls.</summary><value>True initially.</value>
    public bool HexVisible { get { CheckPicker(); return _hexVisible; } set { EnsureMutable(); _hexVisible = value; Refresh(); } }
    /// <summary>Gets or sets visibility of the sample, sampler and shape menu.</summary><value>True initially.</value>
    public bool SamplerVisible { get { CheckPicker(); return _samplerVisible; } set { EnsureMutable(); _samplerVisible = value; Refresh(); } }
    /// <summary>Gets or sets visibility of local and recent swatches.</summary><value>True initially.</value>
    public bool PresetsVisible { get { CheckPicker(); return _presetsVisible; } set { EnsureMutable(); _presetsVisible = value; Refresh(); } }
    /// <summary>Gets or sets user addition/removal of swatches.</summary><value>True initially; programmatic preset methods remain available.</value>
    public bool CanAddSwatches { get { CheckPicker(); return _canAdd; } set { EnsureMutable(); _canAdd = value; Refresh(); } }
    /// <summary>Occurs after a user color edit has committed and refreshed the editor.</summary>
    public event Action<Color>? ColorChanged;
    /// <summary>Occurs after the user adds the current color to local swatches.</summary>
    public event Action<Color>? PresetAdded;
    /// <summary>Occurs after the user removes a local swatch.</summary>
    public event Action<Color>? PresetRemoved;
    private void CheckPicker() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void ValidateColor(Color color) { if (!color.IsFinite()) throw new ArgumentException("Color channels must be finite.", nameof(color)); }
    private void SetColor(Color color, bool normalize)
    {
        _color = color;
        if (normalize)
        {
            var r = ToLinear(color.R); var g = ToLinear(color.G); var b = ToLinear(color.B); var multiplier = _editIntensity ? Math.Max(1, Math.Max(r, Math.Max(g, b))) : 1;
            _normalized = new Color(ToSRGB(r / multiplier), ToSRGB(g / multiplier), ToSRGB(b / multiplier), color.A); _intensity = Math.Log2(multiplier);
            _h = _normalized.H; _s = _normalized.S; _v = _normalized.V; _oh = _normalized.OKHSLH; _os = _normalized.OKHSLS; _ol = _normalized.OKHSLL;
        }
        Refresh();
    }
    private static double ToLinear(double channel) => channel < .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4);
    private static float ToSRGB(double channel) => (float)(channel < .0031308 ? channel * 12.92 : 1.055 * Math.Pow(channel, 1 / 2.4) - .055);
    private Color ApplyIntensity(Color color)
    { if (_intensity == 0) return color; var multiplier = Math.Pow(2, _intensity); return new(ToSRGB(ToLinear(color.R) * multiplier), ToSRGB(ToLinear(color.G) * multiplier), ToSRGB(ToLinear(color.B) * multiplier), color.A); }
    private void ChannelChanged(int channel)
    {
        if (_updating || _disposing) return;
        var previousIntensity = _intensity; var intensity = _values[4].Value; var a = (float)(_values[3].Value / (_mode == ColorModeType.Linear ? 1 : 255));
        var x = (float)_values[0].Value; var y = (float)_values[1].Value; var z = (float)_values[2].Value;
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) || !float.IsFinite(a)) { Refresh(); throw new ArgumentOutOfRangeException(nameof(channel), "Numeric channels exceed the finite color range."); }
        var normalized = channel == 4 ? _normalized : _mode switch { ColorModeType.RGB => new(x / 255, y / 255, z / 255, a), ColorModeType.Linear => new Color(x, y, z, a).LinearToSRGB(), ColorModeType.HSV => Color.FromHSV(x / 360, y / 100, z / 100, a), _ => Color.FromOKHSL(x / 360, y / 100, z / 100, a) };
        _intensity = intensity; var color = ApplyIntensity(normalized);
        if (!normalized.IsFinite() || !color.IsFinite()) { _intensity = previousIntensity; Refresh(); throw new ArgumentOutOfRangeException(nameof(channel), "Exposure exceeds the finite color range."); }
        _normalized = normalized;
        if (channel == 4) { SetColor(color, false); if (!_deferred || !_dragging) ColorChanged?.Invoke(_color); return; }
        if (_mode == ColorModeType.HSV) { _h = x / 360; _s = y / 100; _v = z / 100; } else { _h = _normalized.H; _s = _normalized.S; _v = _normalized.V; }
        if (_mode == ColorModeType.OKHSL) { _oh = x / 360; _os = y / 100; _ol = z / 100; } else { _oh = _normalized.OKHSLH; _os = _normalized.OKHSLS; _ol = _normalized.OKHSLL; }
        SetColor(color, _editIntensity && (_normalized.R > 1 || _normalized.G > 1 || _normalized.B > 1));
        if (!_deferred || !_dragging) ColorChanged?.Invoke(_color);
    }
    private void EndChannelDrag() { _dragging = false; if (_deferred && !IsDisposed) ColorChanged?.Invoke(_color); }
    private void RememberOnRelease(InputEvent input) { if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) AddRecentPreset(_color); }
    internal void SurfaceColor(bool perceptual, bool finished)
    {
        _normalized = perceptual ? Color.FromOKHSL(_oh, _os, _ol, _normalized.A) : Color.FromHSV(_h, _s, _v, _normalized.A);
        if (perceptual) { _h = _normalized.H; _s = _normalized.S; _v = _normalized.V; } else { _oh = _normalized.OKHSLH; _os = _normalized.OKHSLS; _ol = _normalized.OKHSLL; }
        SetColor(ApplyIntensity(_normalized), false);
        List<Exception>? errors = null;
        try { if (!_deferred || finished) ColorChanged?.Invoke(_color); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (finished && !IsDisposed) AddRecentPreset(_color); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Color surface callbacks failed.", errors);
    }
    private void QueueText(string text) { if (Tree is { IsClosing: false } tree) tree.SetDeferred(_submitText, text); else if (Tree is null) SubmitText(text); }
    private void TextFocusExit() { if (!_disposing && IsVisibleInTree) QueueText(_text.Text); }
    private void SubmitText(string text)
    {
        if (IsDisposed) return; Color value;
        if (Color.HTMLIsValid(text)) value = Color.FromHTML(text);
        else if (Colors.TryGetNamed(text, out value)) { }
        else if (!TryConstructor(text, out value)) { RefreshText(); return; }
        if (!_editAlpha) value.A = _color.A; ValidateColor(value); SetColor(value, true); AddRecentPreset(_color); ColorChanged?.Invoke(_color);
    }
    private static bool TryConstructor(string text, out Color color)
    {
        color = default; var input = text.AsSpan().Trim(); if (!input.StartsWith("Color(") || !input.EndsWith(")")) return false;
        if (input.Length > 65536) return false;
        input = input[6..^1]; Span<double> values = stackalloc double[4]; var count = 0;
        while (!input.IsEmpty)
        {
            if (count == 4) return false; var comma = -1; var depth = 0;
            for (var i = 0; i < input.Length; i++) { if (input[i] == '(') depth++; else if (input[i] == ')') { if (--depth < 0) return false; } else if (input[i] == ',' && depth == 0) { comma = i; break; } }
            var part = comma < 0 ? input : input[..comma]; if (!NumericExpression.TryEvaluate(part.ToString(), out values[count]) || !double.IsFinite(values[count]) || Math.Abs(values[count]) > float.MaxValue) return false;
            count++; if (comma < 0) break; input = input[(comma + 1)..]; if (input.Trim().IsEmpty) return false;
        }
        if (count is not (3 or 4)) return false; color = new((float)values[0], (float)values[1], (float)values[2], count == 4 ? (float)values[3] : 1); return true;
    }
    private void RefreshText()
    {
        var constructor = _textConstructor || _color.R < 0 || _color.G < 0 || _color.B < 0 || _color.R > 1 || _color.G > 1 || _color.B > 1;
        _textLabel.Text = constructor ? "Expr" : "Hex"; _textType.Text = constructor ? "()" : "#";
        var value = ColorText(constructor);
        if (_text.Text != value) _text.SetTextPreservingSelection(value);
    }
    private void Refresh()
    {
        if (_content == null || _disposing || _updating) return;
        _updating = true;
        try
        {
            _surfaceRow.Visible = _shape != PickerShapeType.None; _bar.Visible = _shape != PickerShapeType.HSVWheel;
            _sampleRow.Visible = _samplerVisible; _shapeMenu.Visible = _shape != PickerShapeType.None; _modeRow.Visible = _modesVisible; _channels.Visible = _slidersVisible; _textRow.Visible = _hexVisible; _swatches.Visible = _presetsVisible; _add.Disabled = !_canAdd;
            for (var i = 0; i < 4; i++) { _modeButtons[i].SetPressedNoSignal(i == (int)_mode); _modeMenu.GetPopup().SetItemChecked(i, i == (int)_mode); }
            _modeMenu.GetPopup().SetItemChecked(5, _colorized);
            var shapePopup = _shapeMenu.GetPopup(); for (var i = 0; i < shapePopup.ItemCount; i++) shapePopup.SetItemChecked(i, shapePopup.GetItemID(i) == (int)_shape);
            var linear = _normalized.SRGBToLinear();
            for (var i = 0; i < 5; i++)
            {
                var visible = i < 3 || i == 3 && _editAlpha || i == 4 && _editIntensity; _labels[i].Visible = _sliders[i].Visible = _values[i].Visible = visible;
                var label = i == 4 ? "I" : i == 3 ? "A" : _mode is ColorModeType.RGB or ColorModeType.Linear ? RGBLabels[i] : i == 0 ? "H" : i == 1 ? "S" : _mode == ColorModeType.HSV ? "V" : "L"; _labels[i].Text = label;
                var min = i == 4 ? -10 : 0; var max = i == 4 ? 10 : _mode == ColorModeType.Linear ? 1 : i == 3 || _mode == ColorModeType.RGB ? 255 : i == 0 ? 359 : 100;
                _sliders[i].MinValue = min; _sliders[i].MaxValue = max; _sliders[i].Step = i == 4 || _mode == ColorModeType.Linear ? .001 : 1;
                _sliders[i].AllowGreater = i == 4 || i < 3 && _mode is ColorModeType.RGB or ColorModeType.Linear; _values[i].CustomArrowStep = i == 4 ? 1 : _mode == ColorModeType.Linear ? .01 : 0; _values[i].CustomArrowRound = i == 4;
                var value = i == 4 ? _intensity : i == 3 ? _color.A * (_mode == ColorModeType.Linear ? 1 : 255) : _mode switch { ColorModeType.RGB => (double)_normalized[i] * 255, ColorModeType.Linear => linear[i], ColorModeType.HSV => i == 0 ? _h * 360 : i == 1 ? _s * 100 : _v * 100, _ => i == 0 ? _oh * 360 : i == 1 ? _os * 100 : _ol * 100 };
                _sliders[i].SetValueNoSignal(value); _values[i].Prefix = i == 4 && _intensity >= 0 ? "+" : ""; _sliders[i].QueueRedraw();
                _labels[i].CustomMinimumSize = new(GetThemeConstant("label_width"), 0);
            }
            _surface.CustomMinimumSize = new(Math.Max(1, GetThemeConstant("sv_width")), Math.Max(1, GetThemeConstant("sv_height"))); _bar.CustomMinimumSize = new(Math.Max(1, GetThemeConstant("h_width")), 0);
            _surface.QueueRedraw(); _bar.QueueRedraw(); _preview.QueueRedraw(); RefreshText(); RefreshIcons();
            _paletteMenu.GetPopup().SetItemDisabled(0, _presets.Count == 0); _paletteMenu.GetPopup().SetItemDisabled(1, _palettePath.Length == 0); _paletteMenu.GetPopup().SetItemDisabled(3, _presets.Count == 0);
            UpdateMinimumSize(); QueueRedraw();
        }
        finally { _updating = false; }
    }
    internal void SetOpeningColor(Color color) { _preview.OldColor = color; _preview.DisplayOld = true; _preview.FocusMode = FocusMode.All; _preview.QueueRedraw(); }
    internal void CommitFocusedField()
    {
        var focus = GetViewport()?.GetGUIFocusOwner();
        if (focus == _text && _text.IsEditing()) SubmitText(_text.Text);
        else if (focus?.Parent is SpinBox spin && IsAncestorOf(spin)) spin.Apply();
    }
    internal void FocusEditor() { if (_hexVisible) _text.GrabFocus(); else if (_shape != PickerShapeType.None) _surface.GrabFocus(); }
    private static readonly string[] RGBLabels = ["R", "G", "B"];
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (what is NotificationEnterTree or NotificationThemeChanged or NotificationTranslationChanged) Refresh(); if (what is NotificationExitTree or NotificationVisibilityChanged && !IsVisibleInTree) { _surface?.CancelGesture(); _bar?.CancelGesture(); _dragging = false; } }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ColorPicker) ? CreateColorPicker : base.CreateSceneInstanceFactory();
    private static Node CreateColorPicker() => new ColorPicker();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; _sampler?.ReleaseCapture(); ColorChanged = PresetAdded = PresetRemoved = null; } base.Dispose(disposing); }
}
