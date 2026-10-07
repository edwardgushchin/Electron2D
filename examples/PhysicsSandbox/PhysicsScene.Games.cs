namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class PhysicsScene
{
    private const float ParticleRadius = 8;
    private readonly List<RigidBody> _particles = [];
    private readonly List<RigidBody> _wheels = [];
    private readonly List<(RigidBody Body, Vector2 Home)> _targets = [];
    private readonly HashSet<RID> _knocked = new(4);
    private RigidBody? _chassis;
    private RigidBody? _bird;
    private readonly Vector2 _sling = new(160, 526);
    private bool _pulling, _bikeDemo;
    private float _bikePower = 6500, _slingPower = 8;
    internal float StoryParameter => Index switch { 2 => _spring!.Stiffness, 3 => MathF.Abs(_stars[0].Gravity), 5 => _sweep!.Margin, 6 => _tug!.Thrust, 8 => _particles.Count, 9 => _bikePower, 10 => _slingPower, 11 => _smashSpeed, _ => _impulseScale };
    internal string StoryParameterName => Index switch { 2 => "Spring stiffness", 3 => "Star gravity", 4 => "Move speed", 5 => "Sweep margin", 6 => "Tug thrust", 8 => "Particle count", 9 => "Wheel torque", 10 => "Sling power", 11 => "Impact speed", _ => "Impulse strength" };
    internal float StoryParameterMin => Index switch { 2 => 5, 3 => 100, 5 => 0, 6 => 50, 8 => 64, 9 => 1000, 10 => 3, 11 => 200 * SmashScale, _ => .25f };
    internal float StoryParameterMax => Index switch { 2 => 120, 3 => 900, 5 => 16, 6 => 600, 8 => 1024, 9 => 12000, 10 => 13, 11 => 1000 * SmashScale, _ => 1.75f };
    internal float StoryParameterStep => Index switch { 2 or 3 or 6 => 1, 8 => 64, 9 => 100, 11 => 10 * SmashScale, _ => .05f };
    private float _impulseScale = 1;

    internal void SetStoryParameter(float value)
    {
        if (Index == 2) _spring!.Stiffness = value;
        else if (Index == 3) foreach (var star in _stars) star.Gravity = MathF.CopySign(value, star.Gravity);
        else if (Index == 5) _sweep!.Margin = value;
        else if (Index == 6) _tug!.Thrust = value;
        else if (Index == 8) SetPopulation((int)value);
        else if (Index == 9) _bikePower = value;
        else if (Index == 10) _slingPower = value;
        else if (Index == 11) _smashSpeed = value;
        else _impulseScale = value;
    }

    private void BuildStress()
    {
        Enclose();
        SetPopulation(512);
        Actions = ["Add 128 particles [B]", "Stir the tank [N]", "Gravity / float [F]"];
        Help = "Population slider: 64–1,024 active bodies · sleeping stays disabled · click a particle to edit it · drag to stir";
        Observation = "Real solver particles. Octagonal visual dots keep drawing cheap; contact events come from the solver.";
    }

    private void SetPopulation(int count)
    {
        count = Math.Clamp(count, 64, 1024);
        var shape = _particles.Count == 0 ? Circle(ParticleRadius) : _particles[0].ShapeOwnerGetShape(_particles[0].GetShapeOwners()[0], 0);
        while (_particles.Count < count)
        {
            var i = _particles.Count;
            var body = Place(new RigidBody { CanSleep = false, Mass = .2f, LinearDamp = .05f, AngularDamp = .1f }, shape,
                new(184 + i % 40 * 20 + (i / 40 % 2) * 2, 615 - i / 40 * 16), i % 3 == 0 ? Pink : i % 3 == 1 ? Apricot : Blush);
            body.PhysicsMaterialOverride = _marbleMaterial ??= SurfaceMaterial(.25f, .35f);
            _particles.Add(body);
        }
        while (_particles.Count > count)
        {
            var body = _particles[^1]; _particles.RemoveAt(_particles.Count - 1);
            _numbers.Remove(body); Bodies.Remove(body); Colliders.Remove(body); _colors.Remove(body); _shapeOwners.Remove(body); _flashes.Remove(body);
            RemoveChild(body); body.Dispose();
        }
        if (SelectedBody is { IsDisposed: true }) { SelectedBody = null; _selected = null; _grab = default; }
    }

    private void ActStress(int action)
    {
        if (action == 0) SetPopulation(_particles.Count + 128);
        else if (action == 1) foreach (var body in _particles) body.ApplyCentralImpulse((body.Position - new Vector2(576, 440)).Normalized() * 70);
        else WorldGravity = WorldGravity == 0 ? 980 : 0;
    }

    private void BuildBike()
    {
        Enclose();
        Solid(Box(210, 16), new(148, 582), Berry);
        Solid(Box(160, 16), new(323, 558), Berry, -.25f);
        Solid(Box(155, 18), new(492, 534), Berry);
        Solid(Box(140, 16), new(669, 578), Berry, .3f);
        Solid(Box(180, 16), new(866, 598), Berry, -.19f);
        Solid(Box(150, 18), new(1045, 580), Berry);
        _chassis = Dynamic(Box(70, 18), new(142, 504), Pink, 3);
        _chassis.AngularDamp = .35f;
        var tire = SurfaceMaterial(1.2f, .05f, rough: true);
        foreach (var x in new[] { 112f, 177f })
        {
            var wheel = Dynamic(Circle(19), new(x, 536), Ink);
            wheel.PhysicsMaterialOverride = tire; wheel.AngularDamp = .08f;
            _wheels.Add(wheel);
            var guide = Connect(new GrooveJoint { Length = 50, InitialOffset = 32 }, _chassis, wheel, new(x, 504));
            var suspension = Connect(new DampedSpringJoint { Length = 32, RestLength = 32, Stiffness = 95, Damping = 9 }, _chassis, wheel, new(x, 504));
        }
        var finish = Sensor(Box(42, 85), new(1070, 527), Apricot);
        finish.BodyEntered += body => { if (body == _chassis && Score == 0) { Score = 1; Observation = "Finish! Reset the motorcycle or try another gravity and suspension load."; } };
        Actions = ["Reset motorcycle [B]", "Demo drive [N]", "Jump assist [F]"];
        Help = "W / Up: drive · S / Down: brake / reverse · A / D: lean · Space: jump assist · reach the apricot finish";
    }

    private void ActBike(int action)
    {
        if (action == 0)
        {
            _chassis!.Position = new(142, 504); _chassis.Rotation = 0; _chassis.LinearVelocity = Vector2.Zero; _chassis.AngularVelocity = 0;
            for (var i = 0; i < _wheels.Count; i++) { _wheels[i].Position = new(i == 0 ? 112 : 177, 536); _wheels[i].LinearVelocity = Vector2.Zero; _wheels[i].AngularVelocity = 0; }
            Score = 0;
        }
        else if (action == 1) { _bikeDemo = !_bikeDemo; Actions[1] = _bikeDemo ? "Demo: on [N]" : "Demo: off [N]"; }
        else { _chassis!.ApplyCentralImpulse(new(0, -650)); foreach (var wheel in _wheels) wheel.ApplyCentralImpulse(new(0, -180)); }
    }

    private void BuildBirds()
    {
        Enclose();
        var wood = SurfaceMaterial(.55f, .15f);
        for (var tower = 0; tower < 3; tower++)
        {
            var x = 520 + tower * 160;
            var rows = tower == 1 ? 3 : tower == 2 ? 1 : 2;
            for (var row = 0; row < rows; row++)
            {
                foreach (var offset in new[] { -48f, 48f })
                {
                    var beam = Dynamic(Box(20, 84), new(x + offset, 587 - row * 103), tower % 2 == 0 ? Pink : Apricot);
                    beam.PhysicsMaterialOverride = wood;
                }
                var plank = Dynamic(Box(130, 18), new(x, 536 - row * 103), Berry, 1.4f);
                plank.PhysicsMaterialOverride = wood;
            }
            var target = Dynamic(Circle(24), new(x, 513 - (rows - 1) * 103), Blush, .6f);
            _targets.Add((target, target.Position));
        }
        _bird = Dynamic(Circle(23), _sling, Pink, 2);
        _bird.Freeze = true;
        Actions = ["Quick shot [B]", "Reload bird [N]", "Heavy / light bird [F]"];
        Help = "Pull the pink bird back and release · trajectory preview uses current gravity · knock all three blush targets down";
        Observation = "Pull, aim, release. Towers and targets are ordinary bodies; hits, destruction and scores come from the solver.";
    }

    private void ActBirds(int action)
    {
        if (action == 0) { _bird!.Position = _sling + new Vector2(-90, 65); ShootBird(); }
        else if (action == 1) { _pulling = false; _bird!.Freeze = true; _bird.Position = _sling; _bird.LinearVelocity = Vector2.Zero; _bird.AngularVelocity = 0; }
        else { _bird!.Mass = _bird.Mass == 2 ? 5 : 2; Actions[2] = _bird.Mass == 2 ? "Bird: light [F]" : "Bird: heavy [F]"; }
    }

    private bool HandleSling(InputEventMouseButton click, Vector2 point)
    {
        if (Index != 10 || _bird is null || !_bird.Freeze || click.ButtonIndex != MouseButton.Left || !click.Pressed || point.DistanceTo(_bird.Position) > 32) return false;
        _pulling = true; SelectedBody = _bird; SelectionRevision = unchecked(SelectionRevision + 1);
        GetViewport()!.SetInputAsHandled(); return true;
    }

    private void ShootBird()
    {
        if (_bird is null) return;
        var velocity = (_sling - _bird.Position) * _slingPower;
        _pulling = false; _bird.Freeze = false;
        _bird.LinearVelocity = velocity;
    }

    private void AdvanceGames(float delta)
    {
        _smashStep = delta;
        if (_chassis is not null)
        {
            var throttle = (_bikeDemo && Score == 0 || Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0);
            foreach (var wheel in _wheels) if (MathF.Abs(wheel.AngularVelocity) < 24 || MathF.Sign(wheel.AngularVelocity) != throttle) wheel.ApplyTorque(throttle * _bikePower);
            var lean = (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0);
            _chassis.ApplyTorque(lean * 7500);
            if (_jump) { ActBike(2); _jump = false; }
        }
        if (_pulling && _bird is not null)
        {
            var stretch = _pointer - _sling; var length = stretch.Length();
            if (length > 110) stretch *= 110 / length;
            _bird.Position = _sling + stretch;
        }
        foreach (var (body, home) in _targets)
            if (!_knocked.Contains(body.GetRID()) && (body.Position.DistanceTo(home) > 75 || body.LinearVelocity.Length() > 150))
            { _knocked.Add(body.GetRID()); Score++; _colors[body] = Muted; body.QueueRedraw(); }
    }

    private void DrawBike(CanvasItem c)
    {
        c.DrawLine(new(1040, 480), new(1040, 570), Muted, 3);
        c.DrawColoredPolygon([new(1040, 480), new(1082, 493), new(1040, 510)], Apricot);
        c.DrawString(_font, new(995, 460), "FINISH", fontSize: 12, modulate: Apricot);
        if (_chassis is null) return;
        foreach (var wheel in _wheels)
        {
            c.DrawSetTransformMatrix(wheel.GlobalTransform);
            c.DrawCircle(Vector2.Zero, 19, Paper); c.DrawCircle(Vector2.Zero, 19, Berry, false, 3);
            c.DrawCircle(Vector2.Zero, 10, Border); c.DrawCircle(Vector2.Zero, 3, Ink);
            for (var i = 0; i < 4; i++) { var a = Vector2.FromAngle(i * Mathf.Pi / 2) * 14; c.DrawLine(-a, a, Muted, 1); }
        }
        c.DrawSetTransformMatrix(_chassis.GlobalTransform);
        c.DrawColoredPolygon([new(-35, 6), new(-20, -12), new(12, -12), new(35, 4), new(25, 10), new(-24, 10)], Pink);
        c.DrawLine(new(-20, -12), new(1, 4), Berry, 4); c.DrawLine(new(1, 4), new(20, -12), Berry, 4);
        c.DrawLine(new(-17, -14), new(3, -14), Paper, 5);
        c.DrawLine(new(22, -11), new(28, -26), Ink, 3); c.DrawLine(new(23, -26), new(33, -26), Ink, 3);
        c.DrawLine(new(-8, -15), new(0, -32), Berry, 7); c.DrawLine(new(0, -32), new(21, -23), Ink, 4);
        c.DrawCircle(new(3, -44), 10, Apricot); c.DrawLine(new(3, -45), new(11, -45), Paper, 3);
        c.DrawSetTransformMatrix(Transform.Identity);
    }

    private void DrawBirds(CanvasItem c)
    {
        c.DrawLine(_sling + new Vector2(-5, 98), _sling + new Vector2(-5, 16), Muted, 12);
        c.DrawLine(_sling + new Vector2(-5, 16), _sling + new Vector2(-20, -30), Muted, 9);
        c.DrawLine(_sling + new Vector2(-5, 16), _sling + new Vector2(20, -30), Muted, 9);
        foreach (var (target, _) in _targets)
        {
            c.DrawSetTransformMatrix(target.GlobalTransform);
            c.DrawCircle(new(-7, -5), 5, Ink); c.DrawCircle(new(7, -5), 5, Ink);
            c.DrawCircle(new(-5, -5), 2, Paper); c.DrawCircle(new(9, -5), 2, Paper);
            c.DrawLine(new(-7, 8), new(7, 8), Border, 3);
        }
        if (_bird is not null)
        {
            c.DrawSetTransformMatrix(_bird.GlobalTransform);
            c.DrawCircle(new(7, -5), 7, Ink); c.DrawCircle(new(10, -5), 3, Paper);
            c.DrawColoredPolygon([new(18, -1), new(34, 5), new(18, 9)], Apricot);
            c.DrawLine(new(0, -13), new(13, -9), Paper, 3);
            c.DrawSetTransformMatrix(Transform.Identity);
        }
        if (_bird is { Freeze: true })
        {
            c.DrawLine(_sling + new Vector2(-20, -30), _bird.Position, Pink, 3);
            c.DrawLine(_sling + new Vector2(20, -30), _bird.Position, Pink, 3);
            if (_pulling)
            {
                var velocity = (_sling - _bird.Position) * _slingPower;
                for (var i = 1; i < 32; i++) { var t = i * .045f; var p = _bird.Position + velocity * t + new Vector2(0, WorldGravity * _bird.GravityScale * t * t * .5f); c.DrawCircle(p, 2, Berry); }
            }
        }
        c.DrawSetTransformMatrix(Transform.Identity);
    }
}
