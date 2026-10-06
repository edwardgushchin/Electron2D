using System.Runtime.InteropServices;
using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

/// <summary>A complete scene owns its geometry, input, physics story and optional observations.</summary>
internal sealed partial class PhysicsScene : Entity
{
    internal static readonly Color Paper = Color.FromHTML("#241B2C");
    internal static readonly Color Ink = Color.FromHTML("#F9F3EE");
    internal static readonly Color Muted = Color.FromHTML("#C6B7CC");
    internal static readonly Color Border = Color.FromHTML("#594562");
    internal static readonly Color Surface = Color.FromHTML("#35273F");
    internal static readonly Color Hover = Color.FromHTML("#46334F");
    internal static readonly Color Pressed = Color.FromHTML("#573748");
    internal static readonly Color Contour = Color.FromHTML("#55364C");
    internal static readonly Color Mint = Color.FromHTML("#B8DCD0");
    internal static readonly Color Peach = Color.FromHTML("#F2A6CC");
    internal static readonly Color Lavender = Color.FromHTML("#C9B6E4");
    internal static readonly Color Blue = Color.FromHTML("#ACCFE6");
    internal static readonly Color Yellow = Color.FromHTML("#EDDCAC");
    internal static readonly Color DebugColor = Color.FromHTML("#A7E2CE");
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
    internal float WorldGravity { get => _worldGravity; set { _worldGravity = value; ApplyWorldParameters(); } }
    internal float WorldLinearDamp { get => _worldLinearDamp; set { _worldLinearDamp = value; ApplyWorldParameters(); } }
    internal float WorldAngularDamp { get => _worldAngularDamp; set { _worldAngularDamp = value; ApplyWorldParameters(); } }
    internal int BodyLimit => Index == 8 ? 1024 : 160;
    private readonly Entity _overlay;
    private readonly Entity _observations;
    private readonly Entity _storyVisual;
    private readonly Dictionary<CollisionObject, uint[]> _shapeOwners = [];
    private readonly Dictionary<Shape, Vector2[]> _contours = [];
    private readonly Dictionary<RID, PhysicsDirectBodyState> _views = [];
    private string _recordedObservation = "";
    private double _readoutTime;
    private readonly KinematicCollision _slideContact;
    private readonly PhysicsPointQueryParameters _pick;
    private readonly PhysicsPointResult[] _pickHits = new PhysicsPointResult[64];
    private readonly List<Vector2> _debugVectors = [], _debugNormals = [], _debugPoints = [];
    private static readonly Vector2[] DotContour = Enumerable.Range(0, 9).Select(i => new Vector2(MathF.Cos(i * Mathf.Tau / 8), MathF.Sin(i * Mathf.Tau / 8))).ToArray();
    private readonly Dictionary<RigidBody, double> _flashes = new(160);
    private RID _grab;
    private Vector2 _grabLocal;
    private Vector2 _pointer = new(570, 380);
    private bool _singleStep;
    private bool _debugEnabled;
    private bool _debugPrepared;
    private double _time;
    private double _spawnClock;
    private int _serial;
    private bool _mode;
    private RigidBody? _selected;
    private bool _disposed;
    internal int Index { get; }
    internal int BodyCount => Colliders.Count(b => !b.IsDisposed && (b is RigidBody or CharacterBody or AnimatableBody)) + _serverBodies.Count(b => PhysicsServer.BodyGetMode(b.RID) != PhysicsServer.BodyMode.Static);
    internal int ContactEvents { get; private set; }
    internal int Score { get; private set; }
    internal int PhysicsSteps { get; private set; }
    internal bool Running { get; set; } = true;
    internal bool DebugEnabled { get => _debugEnabled; set { _debugEnabled = value; _overlay.Visible = value; _overlay.QueueRedraw(); } }
    internal string[] Actions { get; private set; } = ["", "", ""];
    internal string Help { get; private set; } = "";
    internal string Observation { get; private set; } = "Drag a body to inspect it. K: freeze · Z: sleep · L: lock rotation · Q / E: torque";

    internal PhysicsScene(int index, Font font)
    {
        Index = index;
        _flashes.EnsureCapacity(BodyLimit);
        _debugVectors.EnsureCapacity(BodyLimit * 22);
        _debugNormals.EnsureCapacity(BodyLimit * 16);
        _debugPoints.EnsureCapacity(BodyLimit * 16);
        _font = font;
        Name = "Story";
        PhysicsProcessEnabled = ProcessEnabled = UnhandledInputEnabled = true;
        _slideContact = Own(new KinematicCollision());
        _pick = Own(new PhysicsPointQueryParameters { CollisionMask = 1 });
        Draw += DrawStage;
        _storyVisual = new Entity { Name = "StoryIllustration" };
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
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
        SelectedBody ??= Bodies.Count > 0 ? Bodies[0] : (PhysicsBody?)_character ?? _probe;
        _overlay = new Entity { Name = "PhysicsDebug", ZIndex = 20, Visible = false };
        _overlay.Draw += DrawDebug;
        AddChild(_overlay);
        var observations = _observations = new Entity { Name = "Observations", ZIndex = 30 };
        observations.Draw += c =>
        {
            c.DrawRect(new(24, 651, 848, 27), new Color(Paper.R, Paper.G, Paper.B, .93f));
            DrawObservation(c);
        };
        var readoutLayer = new CanvasLayer { Name = "Readouts", Layer = 9 };
        readoutLayer.AddChild(observations); AddChild(readoutLayer);
    }

    internal void StepOnce() => _singleStep = true;
    internal void SetPointer(Vector2 position)
    {
        var local = IsInsideTree ? GetGlobalTransformWithCanvas().AffineInverse() * position : position;
        if (new Rect2(48, 204, 1056, 430).HasPoint(local)) _pointer = local;
    }
    internal void ReleaseGrab(bool shoot = true)
    {
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
        body.Position = position;
        body.AddChild(new CollisionShape { Name = "Geometry", Shape = shape });
        Colliders.Add(body);
        _colors[body] = color;
        body.Draw += c => DrawCollider(c, body, _colors[body], false);
        AddChild(body);
        if (body is RigidBody rigid)
        {
            _debugPrepared = false;
            Bodies.Add(rigid);
            rigid.ContactMonitor = true;
            rigid.MaxContactsReported = 8;
            rigid.BodyShapeEntered += (_, _, _, _) => { ContactEvents++; _flashes[rigid] = _time + .16; rigid.QueueRedraw(); };
            rigid.SleepingStateChanged += _ => rigid.QueueRedraw();
        }
        return body;
    }

    private RigidBody Dynamic(Shape shape, Vector2 position, Color color, float mass = 1) =>
        Place(new RigidBody { Mass = mass, LinearDamp = .12f, AngularDamp = .4f }, shape, position, color);

    private StaticBody Solid(Shape shape, Vector2 position, Color? color = null, float angle = 0)
    {
        var body = Place(new StaticBody(), shape, position, color ?? Lavender);
        body.Rotation = angle;
        return body;
    }

    private PhysicsMaterial SurfaceMaterial(float friction, float bounce, bool rough = false, bool absorbent = false) =>
        Own(new PhysicsMaterial { Friction = friction, Bounce = bounce, Rough = rough, Absorbent = absorbent });

    private void Enclose()
    {
        Solid(Box(1080, 18), new(576, 638), Mint);
        Solid(Box(18, 432), new(38, 420), Mint);
        Solid(Box(18, 432), new(1114, 420), Mint);
        Solid(Box(1080, 12), new(576, 190), Mint);
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
        if (_independentSpace.IsValid()) PhysicsServer.SpaceStep(_independentSpace, delta);
    }

    protected override void OnProcess(double delta)
    {
        if (_debugEnabled) _overlay.QueueRedraw();
        foreach (var body in Bodies)
        {
            if (body.IsDisposed) continue;
            if (_flashes.TryGetValue(body, out var until) && _time > until) { _flashes.Remove(body); body.QueueRedraw(); }
        }
        _readoutTime += delta;
        if (_readoutTime >= .1)
        {
            _readoutTime = 0;
            if (_selected is not null || Index is 5 or 7) _observations.QueueRedraw();
        }
        if (Observation != _recordedObservation) { _recordedObservation = Observation; _observations.QueueRedraw(); }
        if (Index is 3 or 5 or 6 or 7 or 9 or 10) _storyVisual.QueueRedraw();
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
            if (!Stage.HasPoint(point) || !click.Pressed) return;
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
                else Spawn(point);
                ReleaseGrab();
            }
            else if (click.ButtonIndex != MouseButton.Left) return;
            GetViewport()!.SetInputAsHandled();
            return;
        }
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
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
                    _views.Remove(_selected.GetRID());
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
        _pick.Position = point;
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
        if (BodyCount >= BodyLimit) { Observation = "160-body limit reached. Reset the story to start a new experiment."; return; }
        point = point.Clamp(new Vector2(65, 218), new Vector2(1080, 605));
        var body = Dynamic(Index is 1 or 3 ? Circle(14) : Box(34, 34), point, _serial % 2 == 0 ? Peach : Blue);
        if (Index is 2 or 3 or 6) body.GravityScale = 0;
        if (Index == 1) body.PhysicsMaterialOverride = _marbleMaterial;
    }

    private void DrawStage(CanvasItem c)
    {
        c.DrawRect(Stage, Color.FromHTML("#2E2238"));
        var grid = Color.FromHTML("#403049");
        for (var x = 56; x < 1128; x += 32)
            for (var y = 216; y < 638; y += 32) c.DrawRect(new(x, y, 2, 2), grid);
        c.DrawRect(Stage, Border, false, 1);
    }

    private void DrawCollider(CanvasItem c, CollisionObject body, Color color, bool debug)
    {
        if (body is RigidBody rigid && !debug)
        {
            if (_flashes.ContainsKey(rigid)) color = Yellow;
            else if (rigid.Sleeping) color = color.Lerp(Paper, .35f);
        }
        if (!_shapeOwners.TryGetValue(body, out var owners)) _shapeOwners.Add(body, owners = body.GetShapeOwners());
        foreach (var owner in owners)
        {
            var disabled = body.IsShapeOwnerDisabled(owner);
            if (disabled && !debug) continue;
            for (var i = 0; i < body.ShapeOwnerGetShapeCount(owner); i++)
            {
                var index = body.ShapeOwnerGetShapeIndex(owner, i);
                var local = body is PhysicsBody ? PhysicsServer.BodyGetShapeTransform(body.GetRID(), index) : PhysicsServer.AreaGetShapeTransform(body.GetRID(), index);
                c.DrawSetTransformMatrix((debug ? body.GlobalTransform : Transform.Identity) * local);
                DrawShape(c, body.ShapeOwnerGetShape(owner, i), disabled ? Muted : color, debug || body is Area);
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
                c.DrawPolyline(dots, stroke, outline ? 1 : 1.5f);
                break;
            case CircleShape circle:
                c.DrawCircle(Vector2.Zero, circle.Radius, color, !outline);
                c.DrawCircle(Vector2.Zero, circle.Radius, stroke, false, outline ? 1.5f : 2);
                if (!outline) c.DrawCircle(new(-circle.Radius * .25f, -circle.Radius * .25f), circle.Radius * .2f, Ink);
                break;
            case RectangleShape rect:
                c.DrawRect(rect.GetRect(), color, !outline, 1.5f);
                c.DrawRect(rect.GetRect(), stroke, false, outline ? 1.5f : 2);
                if (!outline && rect.Size.X < 85 && rect.Size.Y < 85)
                {
                    var a = rect.Size * .28f;
                    c.DrawLine(-a, a, new Color(Ink.R, Ink.G, Ink.B, .35f), 1);
                    c.DrawLine(new(-a.X, a.Y), new(a.X, -a.Y), new Color(Ink.R, Ink.G, Ink.B, .35f), 1);
                }
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

    private void DrawDebug(CanvasItem c)
    {
        _debugVectors.Clear(); _debugNormals.Clear(); _debugPoints.Clear();
        foreach (var body in Colliders)
        {
            if (body.IsDisposed) continue;
            if (Index == 8 && body is RigidBody)
            {
                var pose = body.GlobalTransform;
                for (var i = 0; i < 8; i++)
                {
                    _debugVectors.Add(pose * (DotContour[i] * 6));
                    _debugVectors.Add(pose * (DotContour[i + 1] * 6));
                }
            }
            else
            {
                DrawCollider(c, body, DebugColor, true);
                c.DrawSetTransformMatrix(Transform.Identity);
            }
            if (body is not RigidBody rigid) continue;
            var rid = rigid.GetRID();
            if (!_views.TryGetValue(rid, out var state) || state.IsDisposed)
            {
                state = PhysicsServer.BodyGetDirectState(rid);
                if (state is not null) _views[rid] = state;
            }
            if (state is null) continue;
            var center = rigid.GlobalPosition + state.CenterOfMass;
            _debugVectors.Add(center - new Vector2(5, 0)); _debugVectors.Add(center + new Vector2(5, 0));
            _debugVectors.Add(center - new Vector2(0, 5)); _debugVectors.Add(center + new Vector2(0, 5));
            _debugVectors.Add(center); _debugVectors.Add(center + rigid.LinearVelocity * .08f);
            var contacts = state.GetContactCount();
            for (var i = 0; i < contacts; i++)
            {
                // A pair reported by both dynamic bodies needs one marker and normal.
                if (state.GetContactColliderObject(i) is RigidBody other && rid > other.GetRID()) continue;
                var p = state.GetContactLocalPosition(i);
                _debugPoints.Add(p - new Vector2(2, 0)); _debugPoints.Add(p + new Vector2(2, 0));
                _debugNormals.Add(p); _debugNormals.Add(p + state.GetContactLocalNormal(i) * 24);
            }
        }
        if (!_debugPrepared)
        {
            // Prepare the configured contact ceiling once; spare segments lie outside the viewport.
            var maximum = 0;
            foreach (var body in Bodies) maximum += body.MaxContactsReported * 2;
            Pad(_debugNormals, Math.Max(2, maximum)); Pad(_debugPoints, Math.Max(2, maximum));
            for (var i = 0; i < _debugVectors.Count; i += 2)
                if (_debugVectors[i] == _debugVectors[i + 1])
                { _debugVectors[i] = new(-10000, -10000); _debugVectors[i + 1] = new(-10000, -9999); }
            _debugPrepared = true;
        }
        if (_debugVectors.Count > 0) c.DrawMultiline(CollectionsMarshal.AsSpan(_debugVectors), DebugColor, 1);
        c.DrawMultiline(_debugNormals.Count == 0 ? [Vector2.Zero, Vector2.Zero] : CollectionsMarshal.AsSpan(_debugNormals), Ink, 2);
        c.DrawMultiline(_debugPoints.Count == 0 ? [Vector2.Zero, Vector2.Zero] : CollectionsMarshal.AsSpan(_debugPoints), Peach, 4);
        foreach (var (rid, _, shape) in _serverBodies)
        {
            c.DrawSetTransformMatrix(PhysicsServer.BodyGetTransform(rid));
            DrawShape(c, shape, DebugColor, true);
        }
        c.DrawSetTransformMatrix(Transform.Identity);
        foreach (var joint in Joints) c.DrawCircle(joint.GlobalPosition, 6, DebugColor, false, 2);
        if (_grab.IsValid() && PhysicsServer.BodyGetDirectState(_grab) is { } grabbed) c.DrawLine(grabbed.Transform * _grabLocal, _pointer, Ink, 2);
        if (_character is not null && _character.GetLastSlideCollision(_slideContact))
            c.DrawLine(_slideContact.GetPosition(), _slideContact.GetPosition() + _slideContact.GetNormal() * 32, DebugColor, 2);
    }

    private static void Pad(List<Vector2> segments, int count)
    {
        while (segments.Count < count)
        { segments.Add(new(-10000, -10000)); segments.Add(new(-10000, -9999)); }
    }

    // Numeric HUD uses existing glyph draws: changing a number does not create a string or a text layout.
    internal static void DrawReadout(CanvasItem c, Font font, Vector2 position, ReadOnlySpan<char> text, int size, Color color)
    {
        foreach (var character in text) position.X += font.DrawChar(c, position, character, size, color);
    }

    private void DrawObservation(CanvasItem c)
    {
        Span<char> text = stackalloc char[384];
        int count;
        if (_selected is { IsDisposed: false } selected)
        {
            var status = selected.ProcessMode == ProcessMode.Disabled ? "disabled" : selected.Freeze ? "frozen" : selected.Sleeping ? "sleeping" : "awake";
            text.TryWrite(CultureInfo.InvariantCulture, $"{selected.Name} · mass {selected.Mass:0.0} · velocity {selected.LinearVelocity.Length():0} · contacts {selected.GetContactCount()} · {status}", out count);
            DrawReadout(c, _font, new(54, 648), "K freeze · H mode · Z sleep · L lock · Q/E torque · C lift · V policy", 13, Muted);
        }
        else if (Index == 5 && _ray is not null && _sweep is not null)
            text.TryWrite(CultureInfo.InvariantCulture, $"Ray: {(_ray.IsColliding() ? "hit" : "clear")} · wide sweep: {_detected} hit(s) · safe fraction {_sweep.GetClosestCollisionSafeFraction():0.00} · body motion: {(_blocked ? "blocked" : "clear")} · cursor overlaps {_overlaps}", out count);
        else if (Index == 7)
            text.TryWrite(CultureInfo.InvariantCulture, $"Independent space: {_serverBodies.Count - 3} RID bodies · sensor entries {_serverEntries} · live scene slots on the left", out count);
        else { c.DrawString(_font, new(54, 670), Observation, fontSize: 13, modulate: Ink); return; }
        DrawReadout(c, _font, new(54, 670), text[..count], 13, Ink);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            ReleaseGrab(false);
            foreach (var handle in _serverHandles.AsEnumerable().Reverse()) PhysicsServer.FreeRID(handle);
            _serverHandles.Clear();
        }
        base.Dispose(disposing);
        if (disposing) { foreach (var resource in _resources.AsEnumerable().Reverse()) resource.Dispose(); _resources.Clear(); }
    }
}
