using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Electron2D;

internal static partial class PublicServerPerformance
{
    internal static void RunNodes(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Use nodes cpu|gpu output.json.");
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        Type[] families = [typeof(CollisionObject), typeof(PhysicsBody), typeof(RigidBody), typeof(CharacterBody),
            typeof(StaticBody), typeof(AnimatableBody), typeof(Area), typeof(RayCast), typeof(ShapeCast),
            typeof(CollisionShape), typeof(CollisionPolygon), typeof(Joint), typeof(PinJoint), typeof(GrooveJoint),
            typeof(DampedSpringJoint), typeof(KinematicCollision)];
        var methods = families.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName).OrderBy(method => method.DeclaringType!.Name + method, StringComparer.Ordinal).ToArray();
        var results = new List<object>();
        foreach (var method in methods)
        {
            using var fixture = new NodeFixture(backend, method.DeclaringType!);
            var call = fixture.Special(method) ?? fixture.Bind(method);
            results.Add(new { type = method.DeclaringType!.Name, measurement = Measure(method, call, fixture.PrepareBatch, fixture.CleanupBatch) });
            fixture.Validate();
            Console.WriteLine($"{backend} {method.DeclaringType.Name}: {method}");
        }
        var output = System.IO.Path.GetFullPath(args[1]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            schema = 1,
            backend = backend.ToString(),
            publicAPIOnly = true,
            scope = "every declared public non-accessor method in the listed physical node and collision-snapshot families; inherited base declarations measured once",
            families = families.Select(type => type.Name),
            fixture = "attached floor, rigid contact, character slide, overlapping areas and positive ray/shape casts; fresh public scene per overload",
            timing = "warmed repeated host-return latency; scene authoring and cleanup outside clock; percentiles describe batch means",
            movement = "MoveAndCollide is test-only; TestMove uses retained origin and reusable result; MoveAndSlide/ApplyFloorSnap repeat at the settled floor with zero velocity",
            authoring = "prepared single-object edits; repeated exception insertion/removal may hit existing/absent entries; deferred simulation excluded",
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            measuredOverloads = methods.Length,
            results
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class NodeFixture : IDisposable
    {
        private readonly World _world;
        private readonly SubViewport _root;
        private readonly SceneTree _tree;
        private readonly CircleShape _circle = new() { Radius = 1 };
        private readonly RectangleShape _floorShape = new() { Size = new(100, 2) };
        private readonly KinematicCollision _reusable = new();
        private readonly CharacterBody _character = new() { Name = "Character", Velocity = new(0, 2000) };
        private readonly RigidBody _rigid = new() { Name = "Rigid", Position = new(10, 8.5f), CanSleep = false, ContactMonitor = true, MaxContactsReported = 8 };
        private readonly StaticBody _peer = new() { Name = "Peer", Position = new(1000, 0) };
        private readonly Area _area = new() { Position = new(10, 8.5f) };
        private readonly Area _otherArea = new() { Position = new(10, 8.5f) };
        private readonly RayCast _ray = new() { Position = new(-20, 0), TargetPosition = new(0, 20) };
        private readonly ShapeCast _cast = new() { Position = new(-10, 0), TargetPosition = new(0, 20) };
        private readonly KinematicCollision?[] _copies = new KinematicCollision?[32];
        private readonly uint[] _owners = new uint[32];
        private readonly bool[] _ownerAlive = new bool[32];
        private readonly CollisionObject _collisionObject;
        private readonly uint _owner;
        private readonly object _target;
        private readonly PhysicsServer.Backend _backend;
        private Action? _prepare;
        private bool _disposed;

        internal NodeFixture(PhysicsServer.Backend backend, Type type)
        {
            _backend = backend;
            _world = new(backend); _root = new() { World = _world };
            _tree = new(_root);
            _collisionObject = type == typeof(CollisionObject) ? _rigid : _character;
            _owner = _collisionObject.CreateShapeOwner(_collisionObject);
            try
            {
                PhysicsServer.AreaSetGravity(_world.Space, 0);
                var floor = new StaticBody { Name = "Floor", Position = new(0, 10) };
                floor.AddChild(new CollisionShape { Shape = _floorShape }); _root.AddChild(floor);
                foreach (var body in new PhysicsBody[] { _character, _rigid, _peer })
                { body.AddChild(new CollisionShape { Shape = _circle }); _root.AddChild(body); }
                _area.AddChild(new CollisionShape { Shape = _circle }); _root.AddChild(_area);
                _otherArea.AddChild(new CollisionShape { Shape = _circle }); _root.AddChild(_otherArea);
                _cast.Shape = _circle; _root.AddChild(_cast); _root.AddChild(_ray);
                _collisionObject.ShapeOwnerAddShape(_owner, _circle);
                _tree.PhysicsFrame(1d / 60);
                _tree.ProcessFrame(1d / 60);
                _character.MoveAndSlide(); _character.Velocity = Vector2.Zero;
                if (_character.GetSlideCollisionCount() == 0 || !_character.GetLastSlideCollision(_reusable))
                    throw new InvalidOperationException("Node fixture requires a real public character collision.");
                _ray.ForceRaycastUpdate(); _cast.ForceShapecastUpdate();
                if (!_ray.IsColliding() || !_cast.IsColliding() || !_area.HasOverlappingBodies() || !_area.HasOverlappingAreas() ||
                    !_area.OverlapsArea(_otherArea) || !_area.OverlapsBody(_rigid) || _rigid.GetContactCount() == 0)
                    throw new InvalidOperationException("Node fixture requires positive contacts, overlaps and casts.");
                if (type == typeof(RigidBody)) _target = _rigid;
                else if (type == typeof(Area)) _target = _area;
                else if (type == typeof(RayCast)) _target = _ray;
                else if (type == typeof(ShapeCast)) _target = _cast;
                else if (type == typeof(KinematicCollision)) _target = _reusable;
                else if (type == typeof(CollisionObject)) _target = _collisionObject;
                else if (type == typeof(CollisionShape))
                { var child = new CollisionShape { Shape = _circle }; _peer.AddChild(child); _target = child; }
                else if (type == typeof(CollisionPolygon))
                { var child = new CollisionPolygon { Polygon = [new(-1, -1), new(1, -1), new(0, 1)] }; _peer.AddChild(child); _target = child; }
                else if (typeof(Joint).IsAssignableFrom(type))
                { var joint = new PinJoint { NodeA = "../Rigid", NodeB = "../Peer" }; _root.AddChild(joint); _target = joint; }
                else _target = _character;
            }
            catch (Exception primary)
            { try { Dispose(); } catch (Exception cleanup) { throw new AggregateException(primary, cleanup); } throw; }
        }

        internal void Validate()
        {
            if (_world.PhysicsBackend != _backend || !_character.GlobalTransform.IsFinite() || !_rigid.GlobalTransform.IsFinite())
                throw new InvalidOperationException("Public node fixture lost its backend or finite poses.");
        }

        internal void PrepareBatch() => _prepare?.Invoke();

        internal Func<int, int>? Special(MethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(CollisionObject.CreateShapeOwner):
                    return i => { _owners[i] = _collisionObject.CreateShapeOwner(_collisionObject); _ownerAlive[i] = true; return 1; };
                case nameof(CollisionObject.RemoveShapeOwner):
                    _prepare = () => PrepareOwners(shaped: true);
                    return i => { _collisionObject.RemoveShapeOwner(_owners[i]); _ownerAlive[i] = false; return 1; };
                case nameof(CollisionObject.ShapeOwnerAddShape):
                    _prepare = () => PrepareOwners(shaped: false);
                    return i => { _collisionObject.ShapeOwnerAddShape(_owners[i], _circle); return 1; };
                case nameof(CollisionObject.ShapeOwnerRemoveShape):
                    _prepare = () => PrepareOwners(shaped: true);
                    return i => { _collisionObject.ShapeOwnerRemoveShape(_owners[i], 0); return 1; };
                case nameof(CollisionObject.ShapeOwnerClearShapes):
                    _prepare = () => PrepareOwners(shaped: true);
                    return i => { _collisionObject.ShapeOwnerClearShapes(_owners[i]); return 1; };
                default: return null;
            }
        }

        private void PrepareOwners(bool shaped)
        {
            for (var i = 0; i < _owners.Length; i++)
            {
                _owners[i] = _collisionObject.CreateShapeOwner(_collisionObject); _ownerAlive[i] = true;
                if (shaped) _collisionObject.ShapeOwnerAddShape(_owners[i], _circle);
            }
        }

        internal Func<int, int> Bind(MethodInfo method)
        {
            var index = Expression.Parameter(typeof(int), "index");
            var even = Expression.Equal(Expression.And(index, Expression.Constant(1)), Expression.Constant(0));
            Expression Argument(ParameterInfo parameter)
            {
                var type = parameter.ParameterType; var name = parameter.Name!;
                object first, second;
                if (type == typeof(int)) { first = second = name == "layerNumber" ? 1 : 0; }
                else if (type == typeof(uint)) { first = second = _owner; }
                else if (type == typeof(float)) { first = .08f; second = .09f; }
                else if (type == typeof(bool)) { first = name == "testOnly"; second = name == "testOnly" || name == "value" || name == "disabled" || name == "enable"; }
                else if (type == typeof(Vector2))
                { first = name == "motion" ? new Vector2(0, 20) : name == "direction" ? Vector2.Up : new Vector2(.01f, 0); second = name == "motion" || name == "direction" ? first : new Vector2(-.01f, 0); }
                else if (type == typeof(Vector2?)) return Expression.Convert(Expression.Constant(Vector2.Up), type);
                else if (type == typeof(Transform)) { first = _character.GlobalTransform; second = method.Name == "ShapeOwnerSetTransform" ? new Transform(0, new(.01f, 0)) : first; }
                else if (type == typeof(RID)) { first = second = _peer.GetRID(); }
                else if (type == typeof(KinematicCollision)) { first = second = _reusable; }
                else if (type == typeof(Shape)) { first = second = _circle; }
                else if (type == typeof(Node)) { first = second = name == "area" ? _otherArea : _rigid; }
                else if (type.IsAssignableFrom(typeof(StaticBody))) { first = second = _peer; }
                else throw new InvalidOperationException("Unbound public node argument: " + method + "/" + parameter);
                return Expression.Condition(even, Expression.Constant(first, type), Expression.Constant(second, type));
            }
            var invoke = Expression.Call(Expression.Constant(_target), method, method.GetParameters().Select(Argument));
            Expression body = invoke;
            if (method.ReturnType == typeof(KinematicCollision)) body = Expression.Assign(Expression.ArrayAccess(Expression.Constant(_copies), index), invoke);
            else if (method.ReturnType != typeof(void))
            {
                var sink = typeof(ResultSink<>).MakeGenericType(method.ReturnType).GetField(nameof(ResultSink<int>.Value), BindingFlags.Public | BindingFlags.Static)!;
                body = Expression.Assign(Expression.Field(null, sink), invoke);
            }
            return Expression.Lambda<Func<int, int>>(Expression.Block(body, Expression.Constant(1)), index).Compile();
        }

        internal void CleanupBatch()
        {
            for (var i = 0; i < _copies.Length; i++)
            {
                _copies[i]?.Dispose(); _copies[i] = null;
                if (_ownerAlive[i]) { _collisionObject.RemoveShapeOwner(_owners[i]); _ownerAlive[i] = false; }
            }
        }

        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            try { CleanupBatch(); }
            finally
            {
                try { _tree.Dispose(); }
                finally { _root.Dispose(); _world.Dispose(); _reusable.Dispose(); _circle.Dispose(); _floorShape.Dispose(); }
            }
        }
    }
}
