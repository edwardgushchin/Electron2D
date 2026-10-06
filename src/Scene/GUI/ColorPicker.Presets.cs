namespace Electron2D;

public partial class ColorPicker
{
    private long _nextSwatch, _nextRecent;
    /// <summary>Adds a local swatch, moving an existing exact color to the end; no user event is emitted.</summary><param name="color">Finite color, including alpha and HDR channels.</param>
    public void AddPreset(Color color)
    {
        EnsureMutable(); ValidateColor(color); var index = _presets.IndexOf(color);
        if (index >= 0) { _presets.RemoveAt(index); var button = _presetButtons[index]; _presetButtons.RemoveAt(index); _presetButtons.Add(button); _presetGrid.MoveChild(button, _presetGrid.GetChildCount() - 1); }
        else { var button = new SwatchButton(this, color, false) { Name = "Swatch" + _nextSwatch++ }; _presetButtons.Add(button); _presetGrid.AddChild(button); }
        _presets.Add(color); PaletteEdited(); Refresh();
    }
    /// <summary>Adds a recent swatch unless already present; at most nine colors are retained, oldest first.</summary><param name="color">Finite color.</param>
    public void AddRecentPreset(Color color)
    {
        EnsureMutable(); ValidateColor(color); if (_recent.Contains(color)) return;
        if (_recent.Count == 9) { _recent.RemoveAt(0); var old = _recentButtons[0]; _recentButtons.RemoveAt(0); _recentRow.RemoveChild(old); old.Dispose(); }
        _recent.Add(color); var button = new SwatchButton(this, color, true) { Name = "RecentSwatch" + _nextRecent++ }; _recentButtons.Add(button); _recentRow.AddChild(button); _recentRow.MoveChild(button, 0);
    }
    /// <summary>Removes an exact local swatch, if present, without a user event.</summary><param name="color">Color to remove.</param>
    public void ErasePreset(Color color)
    {
        EnsureMutable(); var index = _presets.IndexOf(color); if (index < 0) return;
        var button = _presetButtons[index]; var focus = button.HasFocus(); _presets.RemoveAt(index); _presetButtons.RemoveAt(index); _presetGrid.RemoveChild(button); button.Dispose();
        if (focus && IsInsideTree) (index == 0 ? _add : _presetButtons[index - 1]).GrabFocus();
        if (_presets.Count == 0) { _palettePath = ""; _paletteLabel.Text = ""; } else PaletteEdited(); Refresh();
    }
    /// <summary>Removes an exact recent swatch, if present.</summary><param name="color">Color to remove.</param>
    public void EraseRecentPreset(Color color)
    { EnsureMutable(); var index = _recent.IndexOf(color); if (index < 0) return; _recent.RemoveAt(index); var button = _recentButtons[index]; _recentButtons.RemoveAt(index); _recentRow.RemoveChild(button); button.Dispose(); }
    /// <summary>Returns a caller-owned copy of local swatches in insertion order.</summary><returns>An independent array.</returns>
    public Color[] GetPresets() { CheckPicker(); return _presets.ToArray(); }
    /// <summary>Returns a caller-owned copy of recent swatches, oldest first.</summary><returns>An independent array containing at most nine colors.</returns>
    public Color[] GetRecentPresets() { CheckPicker(); return _recent.ToArray(); }
    private void PaletteEdited() { if (_paletteLabel.Text.Length != 0) _paletteLabel.Text = _paletteLabel.Text.TrimEnd('*') + "*"; }
    private void AddFromUser() { var color = _color; AddPreset(color); if (!IsDisposed) PresetAdded?.Invoke(color); }
    private void RemoveFromUser(Color color) { ErasePreset(color); if (!IsDisposed) PresetRemoved?.Invoke(color); }
    private void ChooseSwatch(Color color, bool recent)
    {
        SetColor(color, true);
        if (recent) { var index = _recent.IndexOf(color); if (index >= 0) { _recent.RemoveAt(index); _recent.Add(color); var button = _recentButtons[index]; _recentButtons.RemoveAt(index); _recentButtons.Add(button); _recentRow.MoveChild(button, 0); } }
        else AddRecentPreset(color);
        ColorChanged?.Invoke(color);
    }
    private readonly record struct PresetDrag(ColorPicker Owner, SwatchButton Button);
    private sealed class SwatchButton : Button
    {
        private readonly ColorPicker _owner;
        internal readonly Color Swatch;
        private readonly bool _recent;
        internal SwatchButton(ColorPicker owner, Color color, bool recent) { _owner = owner; Swatch = color; _recent = recent; CustomMinimumSize = new(24, 24); TooltipText = color.ToHTML(); Pressed += () => owner.ChooseSwatch(color, recent); }
        protected override void OnDraw() { var rect = new Rect2(new(3, 3), Size - new Vector2(6, 6)); Checker(this, rect, _owner.GetThemeIcon("sample_bg")); DrawRect(rect, Swatch); }
        protected override void OnGUIInput(InputEvent input)
        {
            if (!_recent && _owner._canAdd && (input is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } || InputMap.HasAction("ui_colorpicker_delete_preset") && input.IsActionPressed("ui_colorpicker_delete_preset"))) { _owner.RemoveFromUser(Swatch); return; }
            base.OnGUIInput(input);
        }
        protected override DragPayload? OnGetDragData(Vector2 point)
        {
            if (_recent || !_owner._canAdd) return null;
            var preview = new SwatchButton(_owner, Swatch, true) { MouseFilter = MouseFilter.Ignore }; SetDragPreview(preview);
            return new DragPayload<PresetDrag>(new(_owner, this));
        }
        protected override bool OnCanDropData(Vector2 point, DragPayload payload) => !_recent && _owner._canAdd && payload is DragPayload<PresetDrag> drag && ReferenceEquals(drag.Value.Owner, _owner) && !drag.Value.Button.IsDisposed;
        protected override void OnDropData(Vector2 point, DragPayload payload)
        {
            if (!OnCanDropData(point, payload)) return; var from = ((DragPayload<PresetDrag>)payload).Value.Button; var source = _owner._presetButtons.IndexOf(from); var destination = _owner._presetButtons.IndexOf(this); if (source < 0 || destination < 0) return;
            _owner._presetButtons.RemoveAt(source); _owner._presetButtons.Insert(destination, from); var color = _owner._presets[source]; _owner._presets.RemoveAt(source); _owner._presets.Insert(destination, color); _owner._presetGrid.MoveChild(from, destination + 1); _owner.PaletteEdited();
        }
    }
    private void PaletteCommand(int command)
    {
        if (command == 3) { foreach (var color in GetPresets()) ErasePreset(color); return; }
        if (command == 0 && _palettePath.Length != 0) { SavePalette(_palettePath); return; }
        _paletteDialog ??= CreatePaletteDialog(); _paletteDialog.FileMode = command == 2 ? FileDialogMode.OpenFile : FileDialogMode.SaveFile; _paletteDialog.PopupCentered();
    }
    private FileDialog CreatePaletteDialog()
    {
        var dialog = new FileDialog { Name = "_palette_file", Access = FileDialogAccess.FileSystem, Filters = ["*.e2dres;Color Palette"] };
        AddChild(dialog, InternalMode.Front); dialog.FileSelected += PaletteFileSelected; return dialog;
    }
    private void PaletteFileSelected(string path)
    {
        try
        {
            if (_paletteDialog!.FileMode == FileDialogMode.OpenFile)
            {
                using var palette = ResourceLoader.Load<ColorPalette>(path, ResourceLoader.CacheMode.Ignore); var colors = palette.Colors;
                foreach (var color in colors) ValidateColor(color);
                foreach (var color in GetPresets()) ErasePreset(color); foreach (var color in colors) AddPreset(color);
            }
            else SavePalette(path);
            _palettePath = path; _paletteLabel.Text = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        catch (Exception error) { ShowError(error.Message); }
    }
    private void SavePalette(string path)
    { using var palette = new ColorPalette { Colors = GetPresets() }; ResourceSaver.Save(palette, path); _palettePath = path; _paletteLabel.Text = System.IO.Path.GetFileNameWithoutExtension(path); }
    private void ShowError(string message) { _error ??= new AcceptDialog { Name = "_color_error" }; if (_error.Parent == null) AddChild(_error, InternalMode.Front); _error.DialogText = message; _error.PopupCentered(); }
}
