using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class SandboxWindow
{
    private readonly CanvasLayer _ui;
    private readonly CanvasLayer _worldLayer;
    private readonly List<HSlider> _parameters = [];
    private readonly List<string> _parameterNames = [];
    private readonly List<float> _parameterRows = [];
    private Entity _parameterReadout = null!;
    private PhysicsBody? _parameterBody;
    private bool _syncingParameters;

    private void BuildParameters(Font font)
    {
        var panel = new Entity { Name = "Parameters" };
        panel.Draw += c =>
        {
            c.DrawRect(new(888, 184, 240, 510), PhysicsScene.Surface);
            c.DrawRect(new(888, 184, 240, 510), PhysicsScene.Border, false, 1);
            c.DrawString(font, new(904, 210), "WORLD", fontSize: 13, modulate: PhysicsScene.Muted);
            c.DrawString(font, new(904, 378), "SELECTED OBJECT", fontSize: 13, modulate: PhysicsScene.Muted);
        };
        _ui.AddChild(panel);
        Parameter("Gravity", 214, 0, 2200, 10);
        Parameter("Linear damping", 250, 0, 8, .1);
        Parameter("Angular damping", 286, 0, 8, .1);
        Parameter("Time scale", 322, .25, 2, .05);
        Parameter("Mass", 386, .1, 20, .1);
        Parameter("Friction", 422, 0, 1.5, .05);
        Parameter("Bounce", 458, 0, 1, .05);
        Parameter("Gravity scale", 494, 0, 3, .1);
        Parameter("Object linear damp", 530, 0, 8, .01);
        Parameter("Object angular damp", 566, 0, 8, .01);
        Parameter("Scene power", 612, 0, 1, .05);
        _parameterReadout = new Entity { Name = "ParameterValues" };
        _parameterReadout.Draw += c =>
        {
            // All changing HUD digits are prepared before gameplay numbers first use them.
            c.DrawString(font, new(-300, -300), "0123456789.-", fontSize: 13, modulate: new Color(0, 0, 0, 0));
            c.DrawString(font, new(-300, -300), "0123456789.-", fontSize: 15, modulate: new Color(0, 0, 0, 0));
            Span<char> value = stackalloc char[32];
            for (var i = 0; i < _parameters.Count; i++)
            {
                var y = _parameterRows[i];
                PhysicsScene.DrawReadout(c, font, new(904, y + 13), _parameterNames[i], 13, PhysicsScene.Ink);
                value.TryWrite(CultureInfo.InvariantCulture, $"{_parameters[i].Value:0.00}", out var count);
                var width = 0f;
                foreach (var character in value[..count]) width += font.GetCharSize(character, 13).X;
                PhysicsScene.DrawReadout(c, font, new(1112 - width, y + 13), value[..count], 13, PhysicsScene.Mint);
            }
            PhysicsScene.DrawReadout(c, font, new(904, 674), _parameterBody?.Name ?? "Click a physical body", 13, PhysicsScene.Muted);
        };
        _ui.AddChild(_parameterReadout);
    }

    private void Parameter(string name, float y, double min, double max, double step)
    {
        var index = _parameters.Count;
        var slider = new HSlider { Name = "Parameter" + index, Position = new(904, y + 17), Size = new(208, 20), MinValue = min, MaxValue = max, Step = step, FocusMode = FocusMode.Click };
        slider.AddThemeStyleBoxOverride("slider", Style(PhysicsScene.Border));
        slider.AddThemeStyleBoxOverride("grabber_area", Style(PhysicsScene.Peach));
        slider.AddThemeStyleBoxOverride("grabber_area_highlight", Style(PhysicsScene.Mint));
        slider.ValueChanged += value => { if (!_syncingParameters) ChangeParameter(index, (float)value); };
        slider.DragEnded += _ => slider.ReleaseFocus();
        _parameters.Add(slider); _parameterNames.Add(name); _parameterRows.Add(y); _ui.AddChild(slider);
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
        if (ReferenceEquals(_parameterBody, Scene.SelectedBody)) return;
        SyncParameters();
    }

    private void SyncParameters()
    {
        if (_parameters.Count == 0) return;
        _syncingParameters = true;
        try
        {
            _parameterBody = Scene.SelectedBody;
            _parameters[0].SetValueNoSignal(Scene.WorldGravity);
            _parameters[1].SetValueNoSignal(Scene.WorldLinearDamp);
            _parameters[2].SetValueNoSignal(Scene.WorldAngularDamp);
            _parameters[3].SetValueNoSignal(Engine.TimeScale);
            _parameters[4].Editable = _parameters[7].Editable = _parameters[8].Editable = _parameters[9].Editable = _parameterBody is RigidBody;
            _parameters[5].Editable = _parameters[6].Editable = _parameterBody is not null;
            _parameters[4].SetValueNoSignal((_parameterBody as RigidBody)?.Mass ?? 1);
            _parameters[5].SetValueNoSignal(_parameterBody is null ? 1 : MathF.Abs(PhysicsServer.BodyGetFriction(_parameterBody.GetRID())));
            _parameters[6].SetValueNoSignal(_parameterBody is null ? 0 : MathF.Abs(PhysicsServer.BodyGetBounce(_parameterBody.GetRID())));
            _parameters[7].SetValueNoSignal((_parameterBody as RigidBody)?.GravityScale ?? 1);
            _parameters[8].SetValueNoSignal((_parameterBody as RigidBody)?.LinearDamp ?? 0);
            _parameters[9].SetValueNoSignal((_parameterBody as RigidBody)?.AngularDamp ?? 0);
            _parameterNames[10] = Scene.StoryParameterName;
            _parameters[10].MinValue = Scene.StoryParameterMin;
            _parameters[10].MaxValue = Scene.StoryParameterMax;
            _parameters[10].Step = Scene.StoryParameterStep;
            _parameters[10].SetValueNoSignal(Scene.StoryParameter);
            _parameterReadout.QueueRedraw();
        }
        finally { _syncingParameters = false; }
    }
}
