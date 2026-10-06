using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class SandboxWindow
{
    private readonly CanvasLayer _ui, _worldLayer;
    private readonly List<HSlider> _parameters = [];
    private readonly List<string> _parameterNames = [];
    private readonly float[] _defaults = new float[11];
    private readonly Dictionary<PhysicsBody, float[]> _objectDefaults = [];
    private readonly Button[] _tabs = new Button[3];
    private readonly string[] _units = ["u/s²", "s⁻¹", "s⁻¹", "×", "kg", "", "", "×", "s⁻¹", "s⁻¹", ""];
    private Entity _parameterReadout = null!;
    private PhysicsBody? _parameterBody;
    private bool _syncingParameters;
    private int _parameterGroup, _selectionRevision;

    private void BuildParameters(Font font, Font bold)
    {
        var card = Style(PhysicsScene.Surface);
        var panel = new Entity { Name = "Parameters" };
        panel.Draw += c =>
        {
            c.DrawStyleBox(card, new(880, 156, 248, 536));
            c.DrawString(bold, new(896, 180), "INSPECTOR", fontSize: 13, modulate: PhysicsScene.Muted);
        };
        _ui.AddChild(panel);
        var titles = new[] { "World", "Object", "Scene" };
        for (var i = 0; i < 3; i++)
        {
            var group = i;
            var tab = _tabs[i] = MakeButton(titles[i], new(892 + i * 76, 192), 72, font, "Inspector" + i);
            tab.Size = new(72, 34); tab.ToggleMode = true; tab.Pressed += () => ShowParameters(group);
        }
        _tabs[1].TooltipText = "Selected body · K freeze · H mode · Z sleep · L lock · Q/E torque · C lift · V policy";
        Parameter("Gravity", 0, 2200, 10); Parameter("Linear damping", 0, 8, .1); Parameter("Angular damping", 0, 8, .1); Parameter("Time scale", .25, 2, .05);
        Parameter("Mass", .1, 20, .1); Parameter("Friction", 0, 1.5, .05); Parameter("Bounce", 0, 1, .05); Parameter("Gravity scale", 0, 3, .1);
        Parameter("Linear damping", 0, 8, .01); Parameter("Angular damping", 0, 8, .01); Parameter("Scene power", 0, 1, .05);
        _parameterReadout = new Entity { Name = "ParameterValues" };
        _parameterReadout.Draw += c =>
        {
            // Warm every changing numeric glyph at the sizes used by the inspector and footer.
            foreach (var size in (ReadOnlySpan<int>)[11, 14, 17])
                PhysicsScene.DrawReadout(c, font, new(-300, -300), "0123456789.-×²⁻¹ u/s kg·bodies #—", size, new Color(0, 0, 0, 0));
            var title = _parameterGroup switch { 0 => "World settings", 1 => Scene.SelectedRole, _ => "Experiment" };
            c.DrawString(bold, new(896, 252), title, fontSize: 17, modulate: PhysicsScene.Ink);
            Span<char> text = stackalloc char[96];
            int count;
            if (_parameterGroup == 1) text.TryWrite(CultureInfo.InvariantCulture, $"#{Scene.SelectedNumber} · {Scene.SelectedState}", out count);
            else text.TryWrite(CultureInfo.InvariantCulture, $"{(_parameterGroup == 0 ? "Affects every body" : "Tune this scene's main action")}", out count);
            PhysicsScene.DrawReadout(c, font, new(896, 273), text[..count], 14, PhysicsScene.Muted);
            for (var i = 0; i < _parameters.Count; i++)
            {
                var slider = _parameters[i]; if (!slider.Visible) continue;
                var y = Row(i); var color = i < 4 ? PhysicsScene.Mint : i < 10 ? PhysicsScene.Peach : PhysicsScene.Lavender;
                c.DrawString(font, new(896, y + 14), _parameterNames[i], fontSize: 14, modulate: slider.Editable ? PhysicsScene.Ink : PhysicsScene.Muted);
                if (slider.Editable) WriteValue(text, slider.Value, i, out count); else { text[0] = '—'; count = 1; }
                DrawRight(c, font, new(1112, y + 14), text[..count], 14, color);
                slider.MinValue.TryFormat(text, out count, "0.##", CultureInfo.InvariantCulture);
                PhysicsScene.DrawReadout(c, font, new(896, y + 55), text[..count], 11, PhysicsScene.Muted);
                slider.MaxValue.TryFormat(text, out count, "0.##", CultureInfo.InvariantCulture);
                DrawRight(c, font, new(1112, y + 55), text[..count], 11, PhysicsScene.Muted);
                text.TryWrite(CultureInfo.InvariantCulture, $"default {_defaults[i]:0.##}", out count);
                PhysicsScene.DrawReadout(c, font, new(951, y + 55), text[..count], 11, PhysicsScene.Muted);
                var ratio = (float)Math.Clamp((_defaults[i] - slider.MinValue) / (slider.MaxValue - slider.MinValue), 0, 1);
                var x = 904 + 200 * ratio;
                c.DrawLine(new(x, y + 43), new(x, y + 47), color, 2);
            }
            if (_parameterGroup == 0)
            {
                c.DrawLine(new(896, 551), new(1112, 551), PhysicsScene.Border);
                c.DrawString(font, new(896, 579), "Mint adjusts the whole world.", fontSize: 13, modulate: PhysicsScene.Muted);
                c.DrawString(font, new(896, 602), "u = scene units", fontSize: 13, modulate: PhysicsScene.Muted);
                c.DrawString(font, new(896, 625), "Tick marks show factory values.", fontSize: 13, modulate: PhysicsScene.Muted);
            }
            else if (_parameterGroup == 2)
            {
                c.DrawString(font, new(896, 390), SceneIndex switch { 8 => "Particles always stay awake.", 9 => "Torque drives both wheels.", 10 => "Power changes launch speed.", _ => "Changes the scene's main action." }, fontSize: 13, modulate: PhysicsScene.Muted);
                c.DrawString(font, new(896, 415), "Reset restores the experiment.", fontSize: 13, modulate: PhysicsScene.Muted);
            }
        };
        _ui.AddChild(_parameterReadout);
    }

    private static void DrawRight(CanvasItem c, Font font, Vector2 end, ReadOnlySpan<char> text, int size, Color color)
    {
        foreach (var character in text) end.X -= font.GetCharSize(character, size).X;
        PhysicsScene.DrawReadout(c, font, end, text, size, color);
    }
    private void WriteValue(Span<char> text, double value, int index, out int count)
    {
        var format = index == 0 || index == 10 && SceneIndex is 8 or 9 ? "0" : "0.##";
        value.TryFormat(text, out count, format, CultureInfo.InvariantCulture);
        var unit = _units[index];
        if (unit.Length == 0) return;
        if (unit != "×") text[count++] = ' ';
        unit.AsSpan().CopyTo(text[count..]); count += unit.Length;
    }
    private static int Group(int index) => index < 4 ? 0 : index < 10 ? 1 : 2;
    private static float Row(int index) => 292 + (index < 4 ? index : index < 10 ? index - 4 : 0) * 60;
    internal void ShowParameters(int group)
    {
        _parameterGroup = group;
        for (var i = 0; i < 3; i++) _tabs[i].SetPressedNoSignal(i == group);
        for (var i = 0; i < _parameters.Count; i++) _parameters[i].Visible = Group(i) == group;
        _parameterReadout.QueueRedraw();
    }
    private Texture Thumb(Color color)
    {
        using var pixels = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        for (var y = 0; y < 16; y++) for (var x = 0; x < 16; x++)
            {
                var alpha = Math.Clamp(7.5f - new Vector2(x - 7.5f, y - 7.5f).Length(), 0, 1);
                pixels.SetPixel(x, y, new(color.R, color.G, color.B, alpha));
            }
        var texture = ImageTexture.CreateFromImage(pixels); _styles.Add(texture); return texture;
    }
    private void Parameter(string name, double min, double max, double step)
    {
        var index = _parameters.Count; var color = index < 4 ? PhysicsScene.Mint : index < 10 ? PhysicsScene.Peach : PhysicsScene.Lavender;
        var slider = new HSlider { Name = "Parameter" + index, Position = new(896, Row(index) + 23), Size = new(216, 28), MinValue = min, MaxValue = max, Step = step, FocusMode = FocusMode.All };
        var track = Style(PhysicsScene.Border); track.SetBorderWidthAll(0); track.SetCornerRadiusAll(2); track.ContentMarginTop = track.ContentMarginBottom = 2;
        slider.AddThemeStyleBoxOverride("slider", track);
        var fill = Style(color); fill.SetBorderWidthAll(0); fill.SetCornerRadiusAll(2); fill.ContentMarginTop = fill.ContentMarginBottom = 2;
        slider.AddThemeStyleBoxOverride("grabber_area", fill); slider.AddThemeStyleBoxOverride("grabber_area_highlight", fill);
        slider.AddThemeConstantOverride("center_grabber", 0); slider.AddThemeConstantOverride("grabber_offset", 0);
        slider.AddThemeIconOverride("grabber", Thumb(PhysicsScene.Ink)); slider.AddThemeIconOverride("grabber_highlight", Thumb(color)); slider.AddThemeIconOverride("grabber_disabled", Thumb(PhysicsScene.Muted));
        slider.ValueChanged += value => { if (!_syncingParameters) ChangeParameter(index, (float)value); };
        slider.DragEnded += _ => slider.ReleaseFocus();
        _parameters.Add(slider); _parameterNames.Add(name); _ui.AddChild(slider);
    }
    private void ChangeParameter(int index, float value)
    {
        switch (index)
        {
            case 0: Scene.WorldGravity = value; break;
            case 1: Scene.WorldLinearDamp = value; break;
            case 2: Scene.WorldAngularDamp = value; break;
            case 3: Engine.TimeScale = value; break;
            case 4: if (_parameterBody is RigidBody rigid) rigid.Mass = value; break;
            case 5: if (_parameterBody is not null) PhysicsServer.BodySetFriction(_parameterBody.GetRID(), MathF.CopySign(value, PhysicsServer.BodyGetFriction(_parameterBody.GetRID()))); break;
            case 6: if (_parameterBody is not null) PhysicsServer.BodySetBounce(_parameterBody.GetRID(), MathF.CopySign(value, PhysicsServer.BodyGetBounce(_parameterBody.GetRID()))); break;
            case 7: if (_parameterBody is RigidBody gravity) gravity.GravityScale = value; break;
            case 8: if (_parameterBody is RigidBody linear) linear.LinearDamp = value; break;
            case 9: if (_parameterBody is RigidBody angular) angular.AngularDamp = value; break;
            case 10: Scene.SetStoryParameter(value); break;
        }
        _parameterReadout.QueueRedraw();
    }
    private void RefreshSelection()
    {
        if (_selectionRevision == Scene.SelectionRevision && ReferenceEquals(_parameterBody, Scene.SelectedBody)) return;
        _selectionRevision = Scene.SelectionRevision;
        SyncParameters(); ShowParameters(1);
    }
    private static float[] ObjectValues(PhysicsBody body) => [(body as RigidBody)?.Mass ?? 1, MathF.Abs(PhysicsServer.BodyGetFriction(body.GetRID())), MathF.Abs(PhysicsServer.BodyGetBounce(body.GetRID())), (body as RigidBody)?.GravityScale ?? 1, (body as RigidBody)?.LinearDamp ?? 0, (body as RigidBody)?.AngularDamp ?? 0];
    private void CaptureDefaults()
    {
        _objectDefaults.Clear();
        foreach (var body in Scene.Colliders.OfType<PhysicsBody>()) _objectDefaults.Add(body, ObjectValues(body));
        _defaults[0] = Scene.WorldGravity; _defaults[1] = Scene.WorldLinearDamp; _defaults[2] = Scene.WorldAngularDamp; _defaults[3] = 1; _defaults[10] = Scene.StoryParameter;
    }
    private void SyncParameters()
    {
        if (_parameters.Count == 0) return;
        foreach (var body in _objectDefaults.Keys) if (body.IsDisposed) _objectDefaults.Remove(body);
        _syncingParameters = true;
        try
        {
            _parameterBody = Scene.SelectedBody;
            _parameters[0].SetValueNoSignal(Scene.WorldGravity); _parameters[1].SetValueNoSignal(Scene.WorldLinearDamp); _parameters[2].SetValueNoSignal(Scene.WorldAngularDamp); _parameters[3].SetValueNoSignal(Engine.TimeScale);
            _parameters[4].Editable = _parameters[7].Editable = _parameters[8].Editable = _parameters[9].Editable = _parameterBody is RigidBody;
            _parameters[5].Editable = _parameters[6].Editable = _parameterBody is not null;
            _parameters[4].SetValueNoSignal((_parameterBody as RigidBody)?.Mass ?? 1);
            _parameters[5].SetValueNoSignal(_parameterBody is null ? 1 : MathF.Abs(PhysicsServer.BodyGetFriction(_parameterBody.GetRID())));
            _parameters[6].SetValueNoSignal(_parameterBody is null ? 0 : MathF.Abs(PhysicsServer.BodyGetBounce(_parameterBody.GetRID())));
            _parameters[7].SetValueNoSignal((_parameterBody as RigidBody)?.GravityScale ?? 1); _parameters[8].SetValueNoSignal((_parameterBody as RigidBody)?.LinearDamp ?? 0); _parameters[9].SetValueNoSignal((_parameterBody as RigidBody)?.AngularDamp ?? 0);
            if (_parameterBody is not null)
            {
                if (!_objectDefaults.TryGetValue(_parameterBody, out var initial)) _objectDefaults.Add(_parameterBody, initial = ObjectValues(_parameterBody));
                initial.CopyTo(_defaults, 4);
            }
            _parameterNames[10] = Scene.StoryParameterName;
            _units[10] = SceneIndex switch { 8 => "bodies", 9 => "kg·u²/s²", 2 => "kg/s²", 3 => "u/s²", 6 => "kg·u/s²", 5 => "u", _ => "×" };
            _parameters[10].MinValue = Scene.StoryParameterMin; _parameters[10].MaxValue = Scene.StoryParameterMax; _parameters[10].Step = Scene.StoryParameterStep; _parameters[10].SetValueNoSignal(Scene.StoryParameter);
            _parameterReadout.QueueRedraw();
        }
        finally { _syncingParameters = false; }
    }
}
