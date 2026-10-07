using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

/// <summary>A complete scene owns its geometry, input, physics story and optional observations.</summary>
internal sealed partial class PhysicsScene : Entity
{
    internal static readonly Color Paper = Color.FromHTML("#25192B");
    internal static readonly Color Ink = Color.FromHTML("#F9F3EE");
    internal static readonly Color Muted = Color.FromHTML("#C2A6C8");
    internal static readonly Color Border = Color.FromHTML("#6D506D");
    internal static readonly Color Surface = Color.FromHTML("#322338");
    internal static readonly Color Hover = Color.FromHTML("#443049");
    internal static readonly Color Pressed = Color.FromHTML("#A93B71");
    internal static readonly Color Contour = Color.FromHTML("#583149");
    internal static readonly Color Blush = Color.FromHTML("#FCCCDD");
    internal static readonly Color Pink = Color.FromHTML("#FD9ECA");
    internal static readonly Color Berry = Color.FromHTML("#A93B71");
    internal static readonly Color Apricot = Color.FromHTML("#F09776");
    internal static readonly Rect2 Stage = new(24, 184, 1104, 510);
    internal readonly List<RigidBody> Bodies = [];
    internal readonly List<CollisionObject> Colliders = [];
    internal readonly List<Area> Areas = [];
    internal readonly List<Joint> Joints = [];
    private readonly List<IDisposable> _resources = [];
    private readonly List<RID> _serverHandles = [];
    private readonly Dictionary<CollisionObject, Color> _colors = [];
    private readonly Font _font;
    private RID _worldSpace;
    private float _worldGravity = 980, _worldLinearDamp = .1f, _worldAngularDamp = 1;
    internal PhysicsBody? SelectedBody { get; private set; }
    internal int SelectionRevision { get; private set; }
    internal float WorldGravity { get => _worldGravity; set { _worldGravity = value; ApplyWorldParameters(); } }
    internal float WorldLinearDamp { get => _worldLinearDamp; set { _worldLinearDamp = value; ApplyWorldParameters(); } }
    internal float WorldAngularDamp { get => _worldAngularDamp; set { _worldAngularDamp = value; ApplyWorldParameters(); } }
    internal int BodyLimit => Index switch { 8 => 1024, 11 => SmashMaximumCount + 1, _ => 160 };
    private readonly Entity _selection;
    private readonly HashSet<CollisionObject> _boundaries = [];
    private readonly Dictionary<CollisionObject, int> _numbers = [];
    internal Rect2? InputBounds { get; set; }
    internal Vector2 CameraTarget => (_chassis ?? (PhysicsBody?)_character ?? _probe ?? _tug)?.GlobalPosition ?? new Vector2(576, 400);
    internal float PresentationZoom { get; set; } = 1;
    internal int SelectedNumber => _selectedFragment?.Number ?? (SelectedBody is { } body ? _numbers.GetValueOrDefault(body) : 0);
    internal string SelectedState => _selectedFragment is { } fragment ? fragment.Policy == 1 ? "removed" : fragment.Frozen || fragment.Policy == 2 ? "frozen" : fragment.State?.Sleeping == true ? "sleeping" : "awake" : SelectedBody is RigidBody b ? b.Freeze ? "frozen" : b.Sleeping ? "sleeping" : "awake" : "static / character";
    internal string SelectedRole
    {
        get
        {
            if (_selectedFragment is not null) return "Fragment";
            if (SelectedBody is not { } body) return "Select an object";
            if (body == _smashBlock) return "Smash block";
            if (Index == 11) return "Fragment";
            if (body == _bird) return "Projectile";
            if (body == _chassis) return "Motorcycle frame";
            if (body is RigidBody wheel && _wheels.Contains(wheel)) return "Driven wheel";
            if (body == _character) return "Courier";
            if (body == _tug) return "Tug";
            if (body == _cargo) return "Cargo";
            foreach (var target in _targets) if (body == target.Body) return "Blush target";
            return Index switch
            {
                0 => _shapeOwners.TryGetValue(body, out var owners) && owners.Length > 0 && body.ShapeOwnerGetShape(owners[0], 0) is CircleShape ? "Heavy ball" : "Warehouse crate",
                1 => "Marble",
                8 => "Particle",
                10 => "Tower beam",
                _ => "Physical object"
            };
        }
    }

    private readonly Entity _storyVisual;
    private readonly Dictionary<CollisionObject, uint[]> _shapeOwners = [];
    private readonly Dictionary<Shape, Vector2[]> _contours = [];
    private readonly PhysicsPointQueryParameters _pick;
    private readonly PhysicsPointResult[] _pickHits = new PhysicsPointResult[64];
    private static readonly Vector2[] DotContour = Enumerable.Range(0, 9).Select(i => new Vector2(MathF.Cos(i * Mathf.Tau / 8), MathF.Sin(i * Mathf.Tau / 8))).ToArray();
    private readonly Dictionary<RigidBody, double> _flashes = new(160);
    private RID _grab;
    private Vector2 _grabLocal;
    private Vector2 _pointer = new(570, 380);
    private bool _singleStep;
    private double _time;
    private double _spawnClock;
    private int _serial;
    private bool _mode;
    private RigidBody? _selected;
    private bool _disposed;
    internal int Index { get; }
    internal int BodyCount => Colliders.Count(b => !b.IsDisposed && (b is RigidBody or CharacterBody or AnimatableBody)) + _serverBodies.Count(b => PhysicsServer.BodyGetMode(b.RID) != PhysicsServer.BodyMode.Static) + _smashPieces.Count;
    internal int ContactEvents { get; private set; }
    internal int Score { get; private set; }
    internal int PhysicsSteps { get; private set; }
    internal bool Running { get; set; } = true;
    internal string[] Actions { get; private set; } = ["", "", ""];
    internal string Help { get; private set; } = "";
    internal string Observation { get; private set; } = "Drag a body to inspect it. K: freeze · Z: sleep · L: lock rotation · Q / E: torque";

    internal PhysicsScene(int index, Font font)
    {
        Index = index;
        _flashes.EnsureCapacity(Index == 11 ? 1 : BodyLimit);
        _font = font;
        Name = "Story";
        PhysicsProcessEnabled = ProcessEnabled = UnhandledInputEnabled = true;
        _pick = Own(new PhysicsPointQueryParameters { CollisionMask = 1 });
        Draw += DrawStage;
        _storyVisual = new Entity { Name = "StoryIllustration", ZIndex = 10 };
        _storyVisual.Draw += DrawStory;
        AddChild(_storyVisual);
        switch (index)
        {
            case 0: BuildWarehouse(); break;
            case 1: BuildMarbles(); break;
            case 2: BuildClockwork(); break;
            case 3: BuildGarden(); break;
            case 4: BuildRooftops(); break;
            case 5: BuildRadar(); break;
            case 6: BuildTug(); break;
            case 7: BuildAtelier(); break;
            case 8: BuildStress(); break;
            case 9: BuildBike(); break;
            case 10: BuildBirds(); break;
            case 11: BuildSmash(); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
        _selection = new Entity { Name = "SelectedObject", ZIndex = 30 };
        _selection.Draw += DrawSelection; AddChild(_selection);
    }

    internal void StepOnce() => _singleStep = true;
    internal void SetPointer(Vector2 position)
    {
        var local = IsInsideTree ? GetGlobalTransformWithCanvas().AffineInverse() * position : position;
        if (InputBounds is { } bounds ? bounds.HasPoint(position) : Stage.HasPoint(local)) _pointer = local;
    }
    internal void ReleaseGrab(bool shoot = true)
    {
        if (_grab.IsValid() && _selectedFragment is { Frozen: true, Kinematic: true, State: { } state }) state.LinearVelocity = Vector2.Zero;
        _grab = default;
        if (_pulling && shoot) ShootBird();
        _pulling = false;
    }
    private T Own<T>(T resource) where T : IDisposable { _resources.Add(resource); return resource; }
    private Shape Circle(float radius) => Own(new CircleShape { Radius = radius });
    private Shape Box(float width, float height) => Own(new RectangleShape { Size = new(width, height) });
    private Shape Capsule(float radius, float height) => Own(new CapsuleShape { Radius = radius, Height = height });

    private T Place<T>(T body, Shape shape, Vector2 position, Color color) where T : CollisionObject
    {
        body.Name = "Body" + ++_serial;
        _numbers[body] = _serial;
        body.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
        body.Position = position;
        body.AddChild(new CollisionShape { Name = "Geometry", Shape = shape });
        Colliders.Add(body); _colors[body] = color;
        body.Draw += c => DrawCollider(c, body, _colors[body]);
        AddChild(body);
        if (body is RigidBody rigid)
        {
            Bodies.Add(rigid);
            rigid.ContactMonitor = true;
            rigid.MaxContactsReported = 8;
            rigid.BodyShapeEntered += (_, _, _, _) => { ContactEvents++; if (Index != 8) { _flashes[rigid] = _time + .12; rigid.QueueRedraw(); } };
            rigid.SleepingStateChanged += _ => rigid.QueueRedraw();
        }
        return body;
    }

    private RigidBody Dynamic(Shape shape, Vector2 position, Color color, float mass = 1) =>
        Place(new RigidBody { Mass = mass, LinearDamp = .12f, AngularDamp = .4f }, shape, position, color);

    private StaticBody Solid(Shape shape, Vector2 position, Color? color = null, float angle = 0)
    {
        var body = Place(new StaticBody(), shape, position, color ?? Berry);
        body.Rotation = angle;
        return body;
    }

    private PhysicsMaterial SurfaceMaterial(float friction, float bounce, bool rough = false, bool absorbent = false) =>
        Own(new PhysicsMaterial { Friction = friction, Bounce = bounce, Rough = rough, Absorbent = absorbent });

    private void Enclose()
    {
        var scale = Index == 11 ? SmashScale : 1;
        var half = Index is 8 or 11 ? 420 : 540;
        var height = Index == 11 ? 536 : 432; var centerY = Index == 11 ? 367 : 420;
        _boundaries.Add(Solid(Box(half * 2 * scale, 18 * scale), new Vector2(576, 638) * scale, Border));
        _boundaries.Add(Solid(Box(18 * scale, height * scale), new Vector2(576 - half + 2, centerY) * scale, Border));
        _boundaries.Add(Solid(Box(18 * scale, height * scale), new Vector2(576 + half - 2, centerY) * scale, Border));
        _boundaries.Add(Solid(Box(half * 2 * scale, 12 * scale), new Vector2(576, Index switch { 0 => 90, 11 => 94, _ => 190 }) * scale, Border));
    }

    private Area Sensor(Shape shape, Vector2 position, Color color)
    {
        var area = Place(new Area { CollisionLayer = 2, CollisionMask = 1 }, shape, position, color);
        Areas.Add(area);
        return area;
    }

    private T Connect<T>(T joint, PhysicsBody a, PhysicsBody b, Vector2 position) where T : Joint
    {
        joint.Name = "Joint" + ++_serial;
        joint.NodeA = "../" + a.Name;
        joint.NodeB = "../" + b.Name;
        joint.Position = position;
        Joints.Add(joint);
        AddChild(joint);
        return joint;
    }

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _worldSpace = GetWorld()!.Space;
        ApplyWorldParameters();
    }

    protected override void OnExitTree()
    {
        DetachSmash();
        _worldSpace = default;
        base.OnExitTree();
    }

    private void ApplyWorldParameters()
    {
        Set(_worldSpace); Set(_independentSpace);
        void Set(RID space)
        {
            if (!space.IsValid()) return;
            PhysicsServer.AreaSetGravity(space, _worldGravity);
            PhysicsServer.AreaSetLinearDamp(space, _worldLinearDamp);
            PhysicsServer.AreaSetAngularDamp(space, _worldAngularDamp);
        }
    }

    protected override void OnPhysicsProcess(double delta)
    {
        var active = Running || _singleStep;
        PhysicsServer.SpaceSetActive(GetWorld()!.Space, active);
        if (_independentSpace.IsValid()) PhysicsServer.SpaceSetActive(_independentSpace, active);
        _singleStep = false;
        if (!active) return;
        PhysicsSteps++;
        _time += delta;
        _spawnClock += delta;
        ApplyGrab((float)delta);
        AdvanceStory((float)delta);
        if (_selected is { IsDisposed: false, Freeze: false } body)
        {
            var turn = (Input.IsPhysicalKeyPressed(Key.E) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Q) ? 1 : 0);
            if (turn != 0) body.ApplyTorque(turn * 14000 * _impulseScale);
        }
        if (_selectedFragment is { Frozen: false, Policy: not 1 and not 2 } fragment)
        {
            var turn = (Input.IsPhysicalKeyPressed(Key.E) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Q) ? 1 : 0);
            if (turn != 0) PhysicsServer.BodyApplyTorque(fragment.RID, turn * 14000 * _impulseScale);
        }
        if (_independentSpace.IsValid()) PhysicsServer.SpaceStep(_independentSpace, delta);
    }

    protected override void OnProcess(double delta)
    {
        CaptureSmashState();
        foreach (var body in Bodies)
        {
            if (body.IsDisposed) continue;
            if (_flashes.TryGetValue(body, out var until) && _time > until) { _flashes.Remove(body); body.QueueRedraw(); }
        }
        _selection.QueueRedraw();
        if (Index == 11) _smashBlock?.QueueRedraw();
        if (Index is 3 or 5 or 6 or 7 or 9 or 10 or 11) _storyVisual.QueueRedraw();
        foreach (var (rid, visual, _) in _serverBodies) visual.Transform = PhysicsServer.BodyGetTransform(rid);
    }

    protected override void OnUnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion) { SetPointer(motion.Position); return; }
        if (@event is InputEventMouseButton click)
        {
            var point = GetGlobalTransformWithCanvas().AffineInverse() * click.Position;
            SetPointer(click.Position);
            if (click.ButtonIndex == MouseButton.Left && !click.Pressed) { ReleaseGrab(); return; }
            if (InputBounds is { } bounds && !bounds.HasPoint(click.Position)) return;
            if ((InputBounds is null && !Stage.HasPoint(point)) || !click.Pressed) return;
            if (Index == 3 && click.ButtonIndex == MouseButton.Left && Input.IsPhysicalKeyPressed(Key.Shift))
            {
                _stars[0].Position = point;
                GetViewport()!.SetInputAsHandled();
                return;
            }
            if (HandleSling(click, point)) return;
            Pick(point);
            if (click.ButtonIndex == MouseButton.Right)
            {
                if (_selected is { Freeze: false }) _selected.ApplyImpulse(new Vector2(450, -260) * _impulseScale, point - _selected.GlobalPosition);
                else if (_selectedFragment is { Frozen: false, State: { } state } fragment)
                    PhysicsServer.BodyApplyImpulse(fragment.RID, new Vector2(450, -260) * _impulseScale, point - state.Transform.Origin);
                else Spawn(point);
                ReleaseGrab();
            }
            else if (click.ButtonIndex != MouseButton.Left) return;
            GetViewport()!.SetInputAsHandled();
            return;
        }
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (SmashKey(key.Keycode)) { GetViewport()!.SetInputAsHandled(); return; }
        switch (key.Keycode)
        {
            case Key.B: Act(0); break;
            case Key.N: Act(1); break;
            case Key.F: Act(2); break;
            case Key.K: if (_selected is not null) _selected.Freeze = !_selected.Freeze; break;
            case Key.H:
                if (_selected is not null) { _selected.FreezeMode = _selected.FreezeMode == RigidFreezeMode.Static ? RigidFreezeMode.Kinematic : RigidFreezeMode.Static; _selected.Freeze = true; }
                break;
            case Key.Z: if (_selected is not null) _selected.Sleeping = !_selected.Sleeping; break;
            case Key.L: if (_selected is not null) _selected.LockRotation = !_selected.LockRotation; break;
            case Key.C:
                if (_selected is not null) _selected.ConstantForce = _selected.ConstantForce.IsZeroApprox() ? new(0, -1300 * _selected.Mass) : Vector2.Zero;
                break;
            case Key.V:
                if (_selected is not null)
                {
                    ReleaseGrab();
                    if (_selected.ProcessMode != ProcessMode.Disabled) { _selected.DisableMode = CollisionDisableMode.Remove; _selected.ProcessMode = ProcessMode.Disabled; }
                    else if (_selected.DisableMode != CollisionDisableMode.KeepActive) _selected.DisableMode = (CollisionDisableMode)((int)_selected.DisableMode + 1);
                    else _selected.ProcessMode = ProcessMode.Inherit;
                }
                break;
            case Key.G when Index == 3:
                foreach (var star in _stars) star.GravitySpaceOverride = (Area.SpaceOverride)(((int)star.GravitySpaceOverride + 1) % 5);
                Observation = $"Field mixing: {_stars[0].GravitySpaceOverride} / {_stars[1].GravitySpaceOverride}. G cycles all five override modes.";
                break;
            case Key.Space: _jump = true; break;
            default: return;
        }
        GetViewport()!.SetInputAsHandled();
    }

    private void Pick(Vector2 point)
    {
        SelectionRevision = unchecked(SelectionRevision + 1);
        _pick.Position = point;
        SelectedBody = null;
        _selectedFragment = null;
        _selected = null;
        _grab = default;
        var count = GetWorld()!.DirectSpaceState.IntersectPoint(_pick, _pickHits.AsSpan(0, 32));
        if (_independentSpace.IsValid()) count += PhysicsServer.SpaceGetDirectState(_independentSpace).IntersectPoint(_pick, _pickHits.AsSpan(count, 32));
        for (var i = 0; i < count; i++)
        {
            var hit = _pickHits[i];
            if (hit.Collider is PhysicsBody selected) SelectedBody = selected;
            if (hit.Collider is RigidBody body)
            {
                _selected = body;
                body.QueueRedraw();
                _grab = body.Freeze && body.FreezeMode == RigidFreezeMode.Static ? default : body.GetRID();
                _grabLocal = body.GlobalTransform.AffineInverse() * point;
                return;
            }
            if (_smashLookup.TryGetValue(hit.ColliderRID, out var fragment))
            {
                _selectedFragment = fragment;
                _grab = PhysicsServer.BodyGetMode(fragment.RID) == PhysicsServer.BodyMode.Static ? default : fragment.RID;
                _grabLocal = (fragment.State?.Transform ?? fragment.Pose).AffineInverse() * point;
                return;
            }
            if (_serverBodies.Any(b => b.RID == hit.ColliderRID))
            {
                _grab = hit.ColliderRID;
                _grabLocal = PhysicsServer.BodyGetTransform(_grab).AffineInverse() * point;
                return;
            }
        }
    }

    private void ApplyGrab(float delta)
    {
        if (!_grab.IsValid()) return;
        if (_selectedFragment is { Frozen: true, Kinematic: true, State: { } fragmentState })
        {
            var fragmentPose = fragmentState.Transform;
            fragmentState.LinearVelocity = delta > 0 ? (_pointer - fragmentPose * _grabLocal) / delta : Vector2.Zero;
            return;
        }
        if (_selected is { Freeze: true, FreezeMode: RigidFreezeMode.Kinematic })
        {
            _selected.GlobalPosition = _pointer - (_selected.GlobalTransform * _grabLocal - _selected.GlobalPosition);
            return;
        }
        var state = PhysicsServer.BodyGetDirectState(_grab);
        if (state is null || state.InverseMass <= 0) return;
        var pose = state.Transform;
        var point = pose * _grabLocal;
        var error = _pointer - point;
        var force = (error * 45 - state.GetVelocityAtLocalPosition(point - pose.Origin) * 10) / state.InverseMass;
        var length = force.Length();
        if (length > 18000) force *= 18000 / length;
        if (_selected is { CustomIntegrator: true }) PhysicsServer.BodyApplyImpulse(_grab, force * delta, point - pose.Origin);
        else PhysicsServer.BodyApplyForce(_grab, force, point - pose.Origin);
    }

    private void Spawn(Vector2 point)
    {
        if (Index == 8) { SetPopulation(_particles.Count + 64); return; }
        if (Index == 11) { if (_smashPieces.Count < SmashMaximumCount) SetSmashPopulation(_smashPieces.Count + 64); return; }
        if (BodyCount >= BodyLimit) { Observation = "160-body limit reached. Reset the story to start a new experiment."; return; }
        point = point.Clamp(new Vector2(65, 218), new Vector2(1080, 605));
        var body = Dynamic(Index is 1 or 3 ? Circle(14) : Box(34, 34), point, _serial % 2 == 0 ? Pink : Apricot);
        if (Index is 2 or 3 or 6) body.GravityScale = 0;
        if (Index == 1) body.PhysicsMaterialOverride = _marbleMaterial;
    }

    private void DrawStage(CanvasItem c)
    {
        var scale = Index == 11 ? SmashScale : 1;
        c.DrawRect(new(-2000 * scale, -2000 * scale, 6000 * scale, 5000 * scale), Paper);
        c.DrawLine(new(36 * scale, 629 * scale), new(1116 * scale, 629 * scale), Muted, 2 * scale);
    }

    private void DrawCollider(CanvasItem c, CollisionObject body, Color color)
    {
        if (_boundaries.Contains(body)) return;
        if (Index == 9 && (body == _chassis || body is RigidBody wheel && _wheels.Contains(wheel))) return;
        if (body is RigidBody rigid)
        {
            if (Index == 11) color = GetSmashColor(rigid);
            else if (_flashes.ContainsKey(rigid)) color = color.Lerp(Ink, .18f);
            else if (rigid.Sleeping) color = color.Lerp(Paper, .35f);
        }
        if (!_shapeOwners.TryGetValue(body, out var owners)) _shapeOwners.Add(body, owners = body.GetShapeOwners());
        foreach (var owner in owners)
        {
            if (body.IsShapeOwnerDisabled(owner)) continue;
            for (var i = 0; i < body.ShapeOwnerGetShapeCount(owner); i++)
            {
                var index = body.ShapeOwnerGetShapeIndex(owner, i);
                var local = body is PhysicsBody ? PhysicsServer.BodyGetShapeTransform(body.GetRID(), index) : PhysicsServer.AreaGetShapeTransform(body.GetRID(), index);
                c.DrawSetTransformMatrix(local);
                DrawShape(c, body.ShapeOwnerGetShape(owner, i), color, body is Area);
            }
        }
        c.DrawSetTransformMatrix(Transform.Identity);
    }

    private void DrawShape(CanvasItem c, Shape shape, Color color, bool outline)
    {
        var stroke = outline ? color : Contour;
        switch (shape)
        {
            case CircleShape circle when Index == 8:
                Span<Vector2> dots = stackalloc Vector2[9];
                for (var i = 0; i < 8; i++) dots[i] = DotContour[i] * circle.Radius;
                dots[8] = dots[0];
                if (!outline) c.DrawColoredPolygon(dots[..8], color);
                if (outline) c.DrawPolyline(dots, stroke, 1);
                break;
            case CircleShape circle:
                c.DrawCircle(Vector2.Zero, circle.Radius, color, !outline);
                c.DrawCircle(Vector2.Zero, circle.Radius, stroke, false, outline ? 1.5f : 2);
                if (!outline) c.DrawCircle(new(-circle.Radius * .25f, -circle.Radius * .25f), circle.Radius * .2f, Ink);
                break;
            case RectangleShape rect:
                c.DrawRect(rect.GetRect(), color, !outline, 1.5f);
                c.DrawRect(rect.GetRect(), stroke, false, outline ? 1.5f : 2);
                break;
            case CapsuleShape capsule:
                var half = capsule.MidHeight / 2;
                if (!outline)
                {
                    c.DrawCircle(new(0, -half), capsule.Radius, color);
                    c.DrawCircle(new(0, half), capsule.Radius, color);
                    c.DrawRect(new(-capsule.Radius, -half, capsule.Radius * 2, half * 2), color);
                }
                c.DrawArc(new(0, -half), capsule.Radius, Mathf.Pi, Mathf.Tau, 20, stroke, 2);
                c.DrawArc(new(0, half), capsule.Radius, 0, Mathf.Pi, 20, stroke, 2);
                c.DrawLine(new(-capsule.Radius, -half), new(-capsule.Radius, half), stroke, 2);
                c.DrawLine(new(capsule.Radius, -half), new(capsule.Radius, half), stroke, 2);
                break;
            case ConvexPolygonShape polygon:
                if (!_contours.TryGetValue(shape, out var points)) _contours.Add(shape, points = polygon.Points);
                if (!outline) c.DrawColoredPolygon(points, color);
                c.DrawPolyline(points, stroke, 2);
                c.DrawLine(points[^1], points[0], stroke, 2);
                break;
            case ConcavePolygonShape concave:
                if (!_contours.TryGetValue(shape, out var edges)) _contours.Add(shape, edges = concave.Segments);
                for (var i = 0; i < edges.Length; i += 2) c.DrawLine(edges[i], edges[i + 1], color, outline ? 1.5f : 8);
                break;
            case SegmentShape segment: c.DrawLine(segment.A, segment.B, color, outline ? 1.5f : 8); break;
            case SeparationRayShape ray: c.DrawLine(Vector2.Zero, new(0, ray.Length), color, 2); break;
        }
    }

    // Numeric HUD uses existing glyph draws: changing a number does not create a string or a text layout.
    internal static void DrawReadout(CanvasItem c, Font font, Vector2 position, ReadOnlySpan<char> text, int size, Color color)
    {
        foreach (var character in text) position.X += font.DrawChar(c, position.Round(), character, size, color);
    }

    private void DrawSelection(CanvasItem c)
    {
        Rect2? bounds = null;
        if (_selectedFragment is { IsDisposed: false } fragment)
            bounds = fragment.Pose * new Rect2(new Vector2(-_smashSize / 2, -_smashSize / 2), new Vector2(_smashSize, _smashSize));
        else if (SelectedBody is { IsDisposed: false } body)
        {
            if (!_shapeOwners.TryGetValue(body, out var owners)) _shapeOwners.Add(body, owners = body.GetShapeOwners());
            foreach (var owner in owners)
                for (var i = 0; i < body.ShapeOwnerGetShapeCount(owner); i++)
                {
                    var r = body.GlobalTransform * body.ShapeOwnerGetTransform(owner) * body.ShapeOwnerGetShape(owner, i).GetRect();
                    bounds = bounds is { } old ? old.Merge(r) : r;
                }
        }
        if (bounds is not { } rect) return;
        rect = rect.Grow(5 / PresentationZoom);
        c.DrawRect(rect, Pink, false, 2 / PresentationZoom);
        var label = rect.Position + new Vector2(0, -10 / PresentationZoom);
        const int size = 13;
        c.DrawSetTransformMatrix(new Transform(0, Vector2.One / PresentationZoom, 0, label));
        DrawReadout(c, _font, new(-2000, -2000), "0123456789#", size, new Color(0, 0, 0, 0));
        Span<char> text = stackalloc char[80];
        text.TryWrite(CultureInfo.InvariantCulture, $"{SelectedRole} #{SelectedNumber}", out var count);
        var labelWidth = 0f; foreach (var ch in text[..count]) labelWidth += _font.GetCharSize(ch, size).X;
        c.DrawRect(new(new Vector2(-3, -size - 1), new Vector2(labelWidth + 6, size + 6)), Paper);
        DrawReadout(c, _font, Vector2.Zero, text[..count], size, Ink);
        c.DrawSetTransformMatrix(Transform.Identity);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            ReleaseGrab(false);
            DisposeSmash();
            foreach (var handle in _serverHandles.AsEnumerable().Reverse()) PhysicsServer.FreeRID(handle);
            _serverHandles.Clear();
        }
        base.Dispose(disposing);
        if (disposing) { foreach (var resource in _resources.AsEnumerable().Reverse()) resource.Dispose(); _resources.Clear(); }
    }
}
