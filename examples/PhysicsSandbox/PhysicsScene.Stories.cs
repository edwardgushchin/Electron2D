using System.Runtime.InteropServices;

namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class PhysicsScene
{
    private PhysicsMaterial? _marbleMaterial;
    private readonly List<Area> _stars = [];
    private readonly List<Area> _parcels = [];
    private readonly List<(RID RID, Entity Visual, Shape Shape)> _serverBodies = [];
    private RID _independentSpace;
    private int _serverEntries;
    private PhysicsShapeQueryParameters? _queryProbe;
    private readonly Vector2[] _queryContacts = new Vector2[16];
    private readonly PhysicsShapeResult[] _queryHits = new PhysicsShapeResult[32];
    private int _queryContactCount;
    private readonly HashSet<RID> _delivered = [];
    private PhysicsRestInfo? _rest;
    private int _overlaps;
    private CharacterBody? _character;
    private AnimatableBody? _lift;
    private PinJoint? _motor;
    private DampedSpringJoint? _spring;
    private RayCast? _ray;
    private ShapeCast? _sweep;
    private PhysicsBody? _probe;
    private TugBody? _tug;
    private RigidBody? _cargo;
    private RigidBody? _creature;
    private uint _creatureOwner;
    private Shape[] _morphShapes = [];
    private readonly List<Vector2> _trace = new(150);
    private bool _jump;
    private bool _autoFeed = true;
    private int _materialIndex;
    private int _editIndex;
    private int _detected;
    private bool _blocked;
    private PhysicsTestMotionParameters? _motionProbe;

    private void NoGravity() => WorldGravity = 0;

    private void BuildWarehouse()
    {
        Enclose();
        var shape = Box(48, 48);
        var material = SurfaceMaterial(.7f, .08f);
        for (var tower = 0; tower < 3; tower++)
            for (var row = 0; row < 6 + tower * 2; row++)
                for (var column = 0; column < 3; column++)
                {
                    var box = Dynamic(shape, new(280 + tower * 245 + column * 50, 604 - row * 50), tower == 0 ? Peach : tower == 1 ? Blue : Lavender);
                    box.PhysicsMaterialOverride = material;
                }
        Actions = ["Add crate [B]", "Fire heavy ball [N]", "Shockwave [F]"];
        Help = "Left drag: grab and throw   ·   Right click: kick   ·   K: freeze   ·   Z: sleep   ·   L: lock rotation   ·   Q / E: torque";
    }

    private void BuildMarbles()
    {
        Enclose();
        _marbleMaterial = SurfaceMaterial(.12f, .45f);
        Solid(Box(700, 14), new(420, 318), Peach, .15f);
        Solid(Box(700, 14), new(723, 433), Blue, -.15f);
        Solid(Box(580, 14), new(355, 548), Lavender, .12f);
        var bowl = Own(new ConcavePolygonShape { Segments = [new(-80, -60), new(-80, 36), new(-80, 36), new(80, 36), new(80, 36), new(80, -60)] });
        Solid(bowl, new(947, 590), Mint);
        Solid(Own(new SegmentShape { A = new(-60, 0), B = new(60, 0) }), new(931, 523), Mint, .2f);
        var gate = Sensor(Box(140, 50), new(947, 596), Mint);
        _delivered.EnsureCapacity(160);
        gate.BodyEntered += body => { if (body is RigidBody rigid && _delivered.Add(rigid.GetRID())) Score++; };
        for (var i = 0; i < 6; i++)
        {
            var b = Dynamic(Circle(13), new(110 + i * 30, 238), i % 2 == 0 ? Yellow : Peach);
            b.PhysicsMaterialOverride = _marbleMaterial;
        }
        var capsule = Dynamic(Capsule(12, 48), new(320, 245), Blue);
        capsule.PhysicsMaterialOverride = _marbleMaterial;
        Dynamic(Own(new ConvexPolygonShape { Points = [new(-20, 16), new(20, 16), new(0, -22)] }), new(405, 245), Lavender);
        Actions = ["Drop marble [B]", "Material: rubber [N]", "Feed: on [F]"];
        Help = "Rubber bounces, ice slides, clay absorbs. The delivery sensor counts each marble once. Drag any piece to change its route.";
    }

    private void BuildClockwork()
    {
        Enclose();
        var anchor = Solid(Circle(8), new(250, 265), Ink);
        var bob = Dynamic(Circle(30), new(374, 405), Peach, 4);
        var pendulum = Connect(new PinJoint { AngularLimitEnabled = true, AngularLimitLower = -1.2f, AngularLimitUpper = 1.2f }, anchor, bob, anchor.Position);
        var wheelAnchor = Solid(Circle(8), new(590, 358), Ink);
        var wheel = Dynamic(Box(190, 24), wheelAnchor.Position, Lavender, 4);
        _motor = Connect(new PinJoint { MotorEnabled = true, MotorTargetVelocity = 1.5f, MotorMaxTorque = 180, AngularLimitEnabled = false }, wheelAnchor, wheel, wheel.Position);
        var guideAnchor = Solid(Circle(7), new(924, 270), Ink);
        var slider = Dynamic(Box(84, 30), new(924, 405), Blue, 3);
        var groove = Connect(new GrooveJoint { Length = 230, InitialOffset = 135 }, guideAnchor, slider, guideAnchor.Position);
        var springAnchor = Solid(Circle(7), new(694, 252), Ink);
        var springBob = Dynamic(Circle(20), new(694, 470), Mint, 2);
        _spring = Connect(new DampedSpringJoint { Length = 218, RestLength = 135, Stiffness = 32, Damping = 5 }, springAnchor, springBob, springAnchor.Position);
        Actions = ["Motor: on [B]", "Spring: soft [N]", "Kick mechanisms [F]"];
        Help = "Grab the pendulum, slider or spring weight. Their constraints remain live. Q / E adds torque to the selected body.";
        pendulum.Draw += c => DrawJoint(c, pendulum, anchor, bob);
        _motor.Draw += c => DrawJoint(c, _motor, wheelAnchor, wheel);
        groove.Draw += c => { c.DrawLine(Vector2.Zero, new(0, groove.Length), Muted, 4); c.DrawLine(new(-12, 0), new(12, 0), Ink, 2); };
        _spring.Draw += c => DrawJoint(c, _spring, springAnchor, springBob, true);
    }

    private void BuildGarden()
    {
        Enclose();
        NoGravity();
        var positions = new[] { new Vector2(336, 406), new Vector2(784, 406) };
        for (var i = 0; i < positions.Length; i++)
        {
            var star = Sensor(Circle(195), positions[i], i == 0 ? Peach : Lavender);
            star.GravitySpaceOverride = i == 0 ? Area.SpaceOverride.CombineReplace : Area.SpaceOverride.Combine;
            star.Gravity = i == 0 ? 480 : 220;
            star.GravityPoint = true;
            star.GravityPointCenter = Vector2.Zero;
            star.GravityPointUnitDistance = 120;
            star.Priority = 2 - i;
            star.LinearDampSpaceOverride = Area.SpaceOverride.Combine;
            star.LinearDamp = .08f;
            _stars.Add(star);
            var decoration = new Entity();
            decoration.Draw += c => { c.DrawCircle(Vector2.Zero, 23, _colors[star]); c.DrawCircle(Vector2.Zero, 23, Ink, false, 2); c.DrawArc(Vector2.Zero, 31, 0, Mathf.Tau, 40, Muted, 1); };
            star.AddChild(decoration);
        }
        var mist = Sensor(Box(240, 300), new(565, 420), Blue);
        mist.LinearDampSpaceOverride = Area.SpaceOverride.Replace;
        mist.LinearDamp = 2.5f;
        mist.AngularDampSpaceOverride = Area.SpaceOverride.Replace;
        mist.AngularDamp = 3;
        mist.Priority = 3;
        for (var i = 0; i < 18; i++)
        {
            var angle = i * Mathf.Tau / 18;
            var b = Dynamic(Circle(7 + i % 3 * 2), positions[i % 2] + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 126, i % 2 == 0 ? Yellow : Mint, .3f);
            b.LinearDampMode = RigidBody.DampMode.Replace;
            b.LinearDamp = 0;
            b.LinearVelocity = new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * 235;
        }
        Actions = ["Release seeds [B]", "Attract / repel [N]", "Mist: on [F]"];
        Help = "Shift + click: move the pink star   ·   G: field mixing   ·   Blue mist replaces damping   ·   Drag and throw seeds";
    }

    private void BuildRooftops()
    {
        Enclose();
        Solid(Box(190, 18), new(146, 567), Peach);
        var shelf = Solid(Box(170, 14), new(356, 488), Blue);
        ((CollisionShape)shelf.GetChild(0)).OneWayCollision = true;
        Solid(Box(170, 18), new(573, 428), Lavender, -.22f);
        Solid(Own(new ConvexPolygonShape { Points = [new(-100, 30), new(100, 30), new(100, -40)] }), new(838, 522), Peach);
        _lift = Place(new AnimatableBody { SyncToPhysics = true }, Box(114, 16), new(684, 572), Mint);
        _character = Place(new CharacterBody { FloorSnapLength = 12, FloorConstantSpeed = true }, Capsule(13, 42), new(110, 530), Peach);
        _character.PhysicsProcessEnabled = true;
        var foot = Own(new SeparationRayShape { Length = 27, SlideOnSlope = true });
        _character.AddChild(new CollisionShape { Shape = foot });
        var face = new Entity();
        face.Draw += c => { c.DrawCircle(new(-4, -8), 2, Ink); c.DrawCircle(new(4, -8), 2, Ink); c.DrawArc(new(0, -6), 6, .2f, 2.9f, 10, Ink, 1.5f); };
        _character.AddChild(face);
        foreach (var position in new[] { new Vector2(359, 448), new(575, 381), new(858, 422), new(1039, 585) })
        {
            var parcel = Sensor(Box(20, 20), position, Yellow);
            _parcels.Add(parcel);
            parcel.BodyEntered += body =>
            {
                if (body == _character && parcel.Visible) { Score++; parcel.Visible = false; }
            };
        }
        Actions = ["Restart courier [B]", "Ground / float [N]", "Lift: moving [F]"];
        Help = "A / D or arrows: walk   ·   Space: jump   ·   W / S in floating mode   ·   Collect four parcels   ·   One-way blue shelf";
        Observation = "Four parcels, one courier. The lift carries the character; the separation ray keeps feet on slopes.";
    }

    private void BuildRadar()
    {
        Enclose();
        NoGravity();
        for (var i = 0; i < 5; i++)
        {
            var wall = Solid(Box(28, 130 + i % 2 * 60), new(345 + i * 135, i % 2 == 0 ? 320 : 538), i % 2 == 0 ? Lavender : Blue);
            wall.CollisionLayer = i % 2 == 0 ? 1u : 4u;
        }
        _probe = Place(new CharacterBody { MotionMode = CharacterMotionMode.Floating }, Circle(18), new(110, 407), Peach);
        _ray = new RayCast { TargetPosition = new(600, 0), CollisionMask = 5 };
        _probe.AddChild(_ray);
        _sweep = new ShapeCast { Shape = Circle(23), TargetPosition = new(600, 0), CollisionMask = 5 };
        _probe.AddChild(_sweep);
        _queryProbe = Own(new PhysicsShapeQueryParameters { Shape = Circle(34), CollisionMask = 5, Exclude = [_probe.GetRID()] });
        _motionProbe = Own(new PhysicsTestMotionParameters());
        var rescue = Sensor(Circle(26), new(1050, 550), Yellow);
        rescue.BodyEntered += body => { if (body == _probe) Score++; };
        Actions = ["Ray / wide sweep [B]", "All / blue filter [N]", "Ghost violet [F]"];
        Help = "WASD / arrows: steer   ·   Mouse: aim   ·   Violet and blue obstacles use different layers   ·   Reach the golden rescue beacon";
    }

    private void BuildTug()
    {
        Enclose();
        NoGravity();
        _tug = Place(new TugBody(), Own(new ConvexPolygonShape { Points = [new(27, 0), new(-17, -18), new(-17, 18)] }), new(228, 415), Peach);
        _cargo = Dynamic(Box(54, 54), new(402, 415), Blue, 4);
        _cargo.GravityScale = 0;
        _cargo.CenterOfMassMode = RigidCenterOfMassMode.Custom;
        _cargo.CenterOfMass = new(9, 0);
        _cargo.Inertia = 3200;
        _spring = Connect(new DampedSpringJoint { Length = 174, RestLength = 145, Stiffness = 12, Damping = 4, Rotation = -Mathf.Pi / 2 }, _tug, _cargo, _tug.Position);
        _spring.Draw += c => DrawJoint(c, _spring, _tug, _cargo, true);
        var dock = Sensor(Box(160, 144), new(1008, 478), Mint);
        dock.BodyEntered += body => { if (body == _cargo) Score++; };
        Actions = ["Tow cable: on [B]", "Thrust: gentle [N]", "Cargo mass [F]"];
        Help = "W / Up: thrust   ·   S / Down: reverse   ·   A / D: turn   ·   Drag cargo too   ·   Dock the blue crate inside the mint bay";
        Observation = "The tug integrates its own velocity after the solver. Cargo has a custom centre of mass and inertia.";
    }

    private void BuildAtelier()
    {
        Enclose();
        Solid(Box(690, 18), new(410, 576), Lavender, -.09f);
        _creature = Dynamic(Box(68, 28), new(302, 460), Peach, 3);
        _morphShapes = [Circle(21), Capsule(15, 55), Box(44, 28)];
        _creatureOwner = _creature.CreateShapeOwner(_creature);
        _creature.ShapeOwnerAddShape(_creatureOwner, _morphShapes[0]);
        _creature.ShapeOwnerSetTransform(_creatureOwner, new Transform(0, new Vector2(38, 0)));
        _creature.CenterOfMassMode = RigidCenterOfMassMode.Custom;
        _creature.CenterOfMass = new(20, -4);
        var compound = new CollisionPolygon { Polygon = [new(-50, 20), new(50, 20), new(50, -12), new(12, -12), new(12, -38), new(-12, -38), new(-12, -12), new(-50, -12)] };
        var robot = new RigidBody { Position = new(555, 392) };
        robot.Name = "Compound";
        robot.AddChild(compound);
        Colliders.Add(robot); Bodies.Add(robot); _colors[robot] = Blue;
        robot.Draw += c => DrawCollider(c, robot, Blue, false);
        AddChild(robot);
        // This independent world demonstrates explicit RID lifetime and manual stepping.
        _independentSpace = PhysicsServer.SpaceCreate();
        _serverHandles.Add(_independentSpace);
        PhysicsServer.SpaceSetActive(_independentSpace, true);
        AddServerBody(Box(266, 14), new(939, 586), true, Mint);
        AddServerBody(Box(14, 300), new(802, 438), true, Mint);
        AddServerBody(Box(14, 300), new(1076, 438), true, Mint);
        for (var i = 0; i < 5; i++) AddServerBody(Circle(19), new(844 + i * 46, 302 + i * 40), false, i % 2 == 0 ? Yellow : Lavender);
        var receiver = PhysicsServer.AreaCreate();
        _serverHandles.Add(receiver);
        PhysicsServer.AreaAddShape(receiver, Box(236, 80).GetRID());
        PhysicsServer.AreaSetTransform(receiver, new Transform(0, new Vector2(939, 460)));
        PhysicsServer.AreaSetCollisionMask(receiver, 1);
        PhysicsServer.AreaSetGravitySpaceOverride(receiver, Area.SpaceOverride.Combine);
        PhysicsServer.AreaSetGravity(receiver, -480);
        PhysicsServer.AreaSetGravityVector(receiver, Vector2.Down);
        PhysicsServer.AreaSetSpace(receiver, _independentSpace);
        PhysicsServer.AreaSetMonitorCallback(receiver, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) _serverEntries++; });
        Actions = ["Morph creature [B]", "Slots on / off [N]", "RID impulse [F]"];
        Help = "Drag the creature   ·   Live slots keep stable identity   ·   Right-side RID bodies belong to an independently stepped space";
    }

    private void AddServerBody(Shape shape, Vector2 position, bool stationary, Color color)
    {
        var rid = PhysicsServer.BodyCreate();
        _serverHandles.Add(rid);
        PhysicsServer.BodySetMode(rid, stationary ? PhysicsServer.BodyMode.Static : PhysicsServer.BodyMode.Rigid);
        PhysicsServer.BodyAddShape(rid, shape.GetRID());
        PhysicsServer.BodySetTransform(rid, new Transform(0, position));
        PhysicsServer.BodySetBounce(rid, .65f);
        PhysicsServer.BodySetSpace(rid, _independentSpace);
        var visual = new Entity { Name = "RIDBody" + _serverBodies.Count, Position = position };
        visual.Draw += c => DrawShape(c, shape, color, false);
        AddChild(visual);
        _serverBodies.Add((rid, visual, shape));
    }

    internal void Act(int action)
    {
        if ((uint)action > 2) throw new ArgumentOutOfRangeException(nameof(action));
        switch (Index)
        {
            case 8: ActStress(action); break;
            case 9: ActBike(action); break;
            case 10: ActBirds(action); break;
            case 0:
                if (action == 0) Spawn(new(576, 250));
                else if (action == 1 && BodyCount < 160)
                {
                    var ball = Dynamic(Circle(26), new(106, 574), Yellow, 5);
                    ball.ApplyCentralImpulse(new Vector2(4100, -500) * _impulseScale);
                }
                else if (action == 2)
                    foreach (var b in Bodies) b.ApplyCentralImpulse((b.Position - new Vector2(570, 610)).Normalized() * (350 * b.Mass * _impulseScale));
                break;
            case 1:
                if (action == 0) Spawn(new(142, 225));
                else if (action == 1)
                {
                    _materialIndex = (_materialIndex + 1) % 3;
                    _marbleMaterial!.Bounce = _materialIndex == 0 ? .6f : 0;
                    _marbleMaterial.Friction = _materialIndex == 1 ? 0 : .7f;
                    _marbleMaterial.Absorbent = _materialIndex == 2;
                    Actions[1] = $"Material: {new[] { "rubber", "ice", "clay" }[_materialIndex]} [N]";
                }
                else { _autoFeed = !_autoFeed; Actions[2] = $"Feed: {(_autoFeed ? "on" : "off")} [F]"; }
                break;
            case 2:
                if (action == 0) { _motor!.MotorEnabled = !_motor.MotorEnabled; Actions[0] = $"Motor: {(_motor.MotorEnabled ? "on" : "off")} [B]"; }
                else if (action == 1) { _mode = !_mode; _spring!.Stiffness = _mode ? 100 : 32; Actions[1] = $"Spring: {(_mode ? "stiff" : "soft")} [N]"; }
                else foreach (var b in Bodies) b.ApplyCentralImpulse(new Vector2(150 * b.Mass, -240 * b.Mass) * _impulseScale);
                break;
            case 3:
                if (action == 0)
                    for (var i = 0; i < 5; i++) { if (BodyCount >= 160) break; var b = Dynamic(Circle(9), new(460 + i * 24, 248), Yellow, .4f); b.LinearVelocity = new(160 * _impulseScale, 0); }
                else if (action == 1) { _mode = !_mode; foreach (var star in _stars) star.Gravity = -star.Gravity; Actions[1] = $"Gravity: {(_mode ? "repel" : "attract")} [N]"; }
                else { var mist = Areas[^1]; mist.LinearDampSpaceOverride = mist.LinearDampSpaceOverride == Area.SpaceOverride.Disabled ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled; mist.AngularDampSpaceOverride = mist.LinearDampSpaceOverride; Actions[2] = $"Mist: {(mist.LinearDampSpaceOverride == Area.SpaceOverride.Disabled ? "off" : "on")} [F]"; }
                break;
            case 4:
                if (action == 0) { _character!.Position = new(110, 530); _character.Velocity = Vector2.Zero; }
                else if (action == 1) { _character!.MotionMode = _character.MotionMode == CharacterMotionMode.Grounded ? CharacterMotionMode.Floating : CharacterMotionMode.Grounded; _character.Velocity = Vector2.Zero; Actions[1] = $"Mode: {_character.MotionMode} [N]"; }
                else { _mode = !_mode; Actions[2] = $"Lift: {(_mode ? "stopped" : "moving")} [F]"; }
                break;
            case 5:
                if (action == 0) { _mode = !_mode; Actions[0] = $"View: {(_mode ? "wide sweep" : "ray")} [B]"; }
                else if (action == 1) { _ray!.CollisionMask = _ray.CollisionMask == 5 ? 4u : 5u; _sweep!.CollisionMask = _ray.CollisionMask; Actions[1] = $"Filter: {(_ray.CollisionMask == 5 ? "all" : "blue")} [N]"; }
                else
                {
                    if (_probe!.GetCollisionExceptions().Length == 0)
                    {
                        foreach (var wall in Colliders.OfType<StaticBody>().Where(b => b.CollisionLayer == 1 && b.Position.X > 200 && b.Position.X < 1050)) _probe.AddCollisionExceptionWith(wall);
                    }
                    else foreach (var wall in _probe.GetCollisionExceptions()) if (wall is not null) _probe.RemoveCollisionExceptionWith(wall);
                    Actions[2] = $"Violet: {(_probe.GetCollisionExceptions().Length > 0 ? "ghost" : "solid")} [F]";
                }
                break;
            case 6:
                if (action == 0) { _mode = !_mode; if (_mode) PhysicsServer.JointClear(_spring!.GetRID()); else PhysicsServer.JointMakeDampedSpring(_spring!.GetRID(), _tug!.Position, _cargo!.Position, _tug.GetRID(), _cargo.GetRID()); _spring!.Visible = !_mode; Actions[0] = $"Tow cable: {(_mode ? "off" : "on")} [B]"; }
                else if (action == 1) { _tug!.Thrust = _tug.Thrust == 230 ? 520 : 230; Actions[1] = $"Thrust: {(_tug.Thrust == 230 ? "gentle" : "strong")} [N]"; }
                else { _cargo!.Mass = _cargo.Mass == 4 ? 12 : 4; Actions[2] = $"Cargo: {_cargo.Mass:0} kg [F]"; }
                break;
            case 7:
                var rid = _creature!.GetRID();
                if (action == 0)
                {
                    _editIndex = (_editIndex + 1) % 3;
                    var replacement = _morphShapes[_editIndex];
                    PhysicsServer.BodySetShape(rid, 1, replacement.GetRID());
                    PhysicsServer.BodySetShapeTransform(rid, 1, new Transform(_editIndex * .25f, new Vector2(38, 0)));
                    _creature.QueueRedraw();
                    Observation = "The live server slot was replaced without adding a scene child. The debug outline follows its real geometry.";
                }
                else if (action == 1) { _mode = !_mode; _creature.ShapeOwnerSetDisabled(_creatureOwner, _mode); _creature.QueueRedraw(); Actions[1] = $"Slots: {(_mode ? "off" : "on")} [N]"; }
                else foreach (var (body, _, _) in _serverBodies.Skip(3)) PhysicsServer.BodyApplyCentralImpulse(body, new Vector2(-100, -420) * _impulseScale);
                break;
        }
        _storyVisual.QueueRedraw();
    }

    private void AdvanceStory(float delta)
    {
        if (Index == 1 && _autoFeed && _spawnClock > 1.1)
        {
            _spawnClock = 0;
            foreach (var body in Bodies)
            {
                if (!_delivered.Remove(body.GetRID()) && body.Position.Y < 600) continue;
                body.Position = new(132, 225); body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0;
                break;
            }
        }
        AdvanceGames(delta);
        if (_lift is not null && !_mode) _lift.Position = new(684, 527 + 65 * MathF.Sin((float)_time * .9f));
        if (_character is not null)
        {
            foreach (var parcel in _parcels) if (!parcel.Visible && parcel.Monitoring) parcel.Monitoring = false;
            var direction = Steering();
            if (_character.MotionMode == CharacterMotionMode.Floating) _character.Velocity = direction * (210 * _impulseScale);
            else
            {
                _character.Velocity = new(direction.X * 220 * _impulseScale, _character.Velocity.Y + _character.GetGravity().Y * delta);
                if (_jump && _character.IsOnFloor()) _character.Velocity = new(_character.Velocity.X, -430);
            }
            _character.MoveAndSlide();
            _jump = false;
            if (Score == _parcels.Count) Observation = "All four parcels delivered. Try floating mode or pause and inspect a platform contact.";
        }
        if (_probe is CharacterBody probe)
        {
            probe.Velocity = Steering() * (190 * _impulseScale);
            probe.MoveAndSlide();
            _queryProbe!.Transform = new Transform(0, _pointer);

            var direct = GetWorld()!.DirectSpaceState;
            _overlaps = direct.IntersectShape(_queryProbe, _queryHits);
            _queryContactCount = direct.CollideShape(_queryProbe, _queryContacts) * 2;
            _rest = direct.GetRestInfo(_queryProbe);
            var target = _pointer - probe.Position;
            _ray!.TargetPosition = target;
            _sweep!.TargetPosition = target;
            _ray.ForceRaycastUpdate();
            _sweep.ForceShapecastUpdate();
            _detected = _sweep.GetCollisionCount();
            _motionProbe!.From = probe.GlobalTransform; _motionProbe.Motion = target;
            _blocked = PhysicsServer.BodyTestMotion(probe.GetRID(), _motionProbe);

        }
        foreach (var joint in Joints) joint.QueueRedraw();
        if (Index == 3 && Bodies.Count > 0)
        {
            if (_trace.Count >= 150) _trace.RemoveAt(0);
            _trace.Add(Bodies[0].Position);
        }

    }

    private static Vector2 Steering()
    {
        var x = (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0);
        var y = (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0);
        return new Vector2(x, y).Normalized();
    }

    private void DrawJoint(CanvasItem c, Joint joint, PhysicsBody a, PhysicsBody b, bool spring = false)
    {
        var from = joint.GlobalTransform.AffineInverse() * a.GlobalPosition;
        var to = joint.GlobalTransform.AffineInverse() * b.GlobalPosition;
        if (!spring) { c.DrawLine(from, to, Muted, 3); return; }
        var axis = to - from;
        var side = new Vector2(-axis.Y, axis.X).Normalized() * 7;
        Span<Vector2> points = stackalloc Vector2[18];
        points[0] = from;
        points[^1] = to;
        for (var i = 1; i < points.Length - 1; i++) points[i] = from + axis * i / (points.Length - 1) + side * (i % 2 == 0 ? 1 : -1);
        c.DrawPolyline(points, Muted, 2);
    }

    private void DrawStory(CanvasItem c)
    {
        switch (Index)
        {
            case 9: DrawBike(c); break;
            case 10: DrawBirds(c); break;
            case 0: break;
            case 1:
                c.DrawString(_font, new(110, 221), "FEED", fontSize: 13, modulate: Muted);
                c.DrawString(_font, new(875, 505), "DELIVER HERE", fontSize: 14, modulate: Ink);
                break;
            case 2:
                foreach (var (x, title) in new[] { (190, "PENDULUM"), (517, "MOTOR"), (671, "SPRING"), (881, "GUIDE") })
                    c.DrawString(_font, new(x, 229), title, fontSize: 13, modulate: Muted);
                break;
            case 3:
                c.DrawString(_font, new(475, 243), "DAMPING MIST", fontSize: 13, modulate: Muted);
                if (_trace.Count > 1) c.DrawPolyline(CollectionsMarshal.AsSpan(_trace), new Color(Ink.R, Ink.G, Ink.B, .3f), 1.5f);
                break;
            case 4:
                c.DrawString(_font, new(70, 250), "DELIVERY ROUTE", fontSize: 13, modulate: Muted);
                break;
            case 5:
                if (_probe is null || _ray is null || _sweep is null) break;
                var start = _probe.Position;
                var end = _ray.IsColliding() ? _ray.GetCollisionPoint() : _pointer;
                c.DrawLine(start, end, Peach, 2);
                c.DrawCircle(end, 5, Peach);
                if (_mode)
                {
                    var safe = start.Lerp(_pointer, _sweep.GetClosestCollisionSafeFraction());
                    c.DrawLine(start, safe, Blue, 12);
                    c.DrawCircle(safe, 23, Ink, false, 2);
                    c.DrawCircle(_pointer, 34, Mint, false, 1.5f);
                    for (var i = 0; i < _queryContactCount; i++) c.DrawCircle(_queryContacts[i], 3, Mint);
                    if (_rest is { } rest) c.DrawLine(rest.Point, rest.Point + rest.Normal * 32, Mint, 2);
                    for (var i = 0; i < _sweep.GetCollisionCount(); i++)
                    {
                        var p = _sweep.GetCollisionPoint(i);
                        c.DrawLine(p, p + _sweep.GetCollisionNormal(i) * 25, Ink, 2);
                    }
                }
                break;
            case 6:
                c.DrawString(_font, new(931, 391), "CARGO DOCK", fontSize: 14, modulate: Muted);
                if (_tug is { Firing: true })
                {
                    var p = _tug.Position;
                    var direction = new Vector2(MathF.Cos(_tug.Rotation), MathF.Sin(_tug.Rotation));
                    c.DrawLine(p - direction * 20, p - direction * 48, Peach, 10);
                }
                break;
            case 7:
                c.DrawLine(new(788, 232), new(788, 615), Border, 2);
                c.DrawString(_font, new(83, 241), "LIVE SCENE GEOMETRY", fontSize: 13, modulate: Muted);
                c.DrawString(_font, new(816, 241), "INDEPENDENT RID WORLD", fontSize: 13, modulate: Muted);
                c.DrawRect(new(821, 420, 236, 80), Mint, false, 1.5f);
                c.DrawString(_font, new(827, 444), "UPDRAFT SENSOR", fontSize: 12, modulate: Mint);
                if (_creature is not null)
                {
                    var first = _creature.ShapeOwnerGetShape(_creatureOwner, 0);
                    var overlaps = first.Collide(_creature.GlobalTransform * PhysicsServer.BodyGetShapeTransform(_creature.GetRID(), _creature.ShapeOwnerGetShapeIndex(_creatureOwner, 0)), first, new Transform(0, _pointer));
                    c.DrawSetTransformMatrix(new Transform(0, _pointer));
                    DrawShape(c, first, overlaps ? Peach : Mint, true);
                    c.DrawSetTransformMatrix(Transform.Identity);
                }
                break;
        }
    }
}

/// <summary>The player ship replaces gravity/damping with its own post-solver velocity integration.</summary>
internal sealed class TugBody : RigidBody
{
    internal float Thrust { get; set; } = 230;
    internal bool Firing { get; private set; }
    internal TugBody() { CustomIntegrator = true; GravityScale = 0; Mass = 2; Inertia = 700; }
    protected override void IntegrateForces(PhysicsDirectBodyState state)
    {
        var turn = (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0);
        var thrust = (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0);
        Firing = thrust != 0;
        state.AngularVelocity = (state.AngularVelocity + turn * 4 * state.Step) * MathF.Max(0, 1 - state.Step * 2);
        var direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        state.LinearVelocity = (state.LinearVelocity + direction * (thrust * Thrust * state.Step)) * MathF.Max(0, 1 - state.Step * .35f);
    }
}
