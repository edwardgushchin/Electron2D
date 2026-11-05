using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Electron2D;

internal static partial class PublicServerPerformance
{
    internal static void RunResources(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Use resources cpu|gpu output.json.");
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        Type[] families = [typeof(Shape), typeof(CircleShape), typeof(CapsuleShape), typeof(SegmentShape), typeof(RectangleShape),
            typeof(ConvexPolygonShape), typeof(ConcavePolygonShape), typeof(SeparationRayShape), typeof(WorldBoundaryShape), typeof(CustomShape),
            typeof(ShapeGeometry), typeof(PhysicsTestMotionResult), typeof(PhysicsRayQueryParameters), typeof(PhysicsCheckpoint), typeof(PhysicsSnapshot), typeof(PhysicsSnapshotMap)];
        var methods = families.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName && method.Name != nameof(Shape.Draw))
            .OrderBy(method => method.DeclaringType!.Name + method, StringComparer.Ordinal).ToArray();
        var results = new List<object>();
        foreach (var method in methods)
        {
            using var fixture = new ServerFixture(backend);
            using var resources = new ResourceFixture(fixture);
            var call = resources.Bind(method);
            results.Add(new { type = method.DeclaringType!.Name, measurement = Measure(method, call, resources.PrepareBatch, resources.CleanupBatch) });
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
            scope = "declared public non-accessor methods in listed resource/result/checkpoint/snapshot families; Shape.Draw belongs to rendered diagnostics and is excluded",
            families = families.Select(type => type.Name),
            fixture = "same two-body/Area/two-joint completed raw world; every object bound for snapshot capture/apply",
            timing = "warmed repeated host-return latency; prepared objects/storage; caller-owned cleanup outside clock except explicitly timed Dispose",
            neutral = "standalone shape/resource/query-parameter/result methods use the shared host implementation in both backend contexts; these are not GPU kernel comparisons",
            snapshots = "same-world capture/apply and prepared binary buffers; no transport or separate-process network latency",
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            measuredOverloads = methods.Length,
            results
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class ResourceFixture : IDisposable
    {
        private readonly ServerFixture _server;
        private readonly Shape[] _shapes = [new CircleShape { Radius = 3 }, new CapsuleShape(), new SegmentShape(), new RectangleShape(),
            new ConvexPolygonShape { Points = [new(-1, -1), new(1, -1), new(0, 1)] },
            new ConcavePolygonShape { Segments = [new(-1, 0), new(1, 0)] }, new SeparationRayShape(), new WorldBoundaryShape()];
        private readonly PhysicsTestMotionResult _motion = new();
        private readonly PhysicsRayQueryParameters?[] _queries = new PhysicsRayQueryParameters?[32];
        private readonly PhysicsSnapshotMap?[] _maps = new PhysicsSnapshotMap?[32];
        private readonly PhysicsCheckpoint?[] _points = new PhysicsCheckpoint?[32];
        private readonly PhysicsCheckpoint _checkpoint;
        private readonly PhysicsSnapshotMap _map;
        private readonly PhysicsSnapshot _snapshot = new();
        private readonly byte[] _packet;
        private readonly byte[] _destination;
        private Action? _prepare;
        private bool _checkWrite;
        private bool _disposed;

        internal ResourceFixture(ServerFixture server)
        {
            _server = server;
            _checkpoint = PhysicsServer.SpaceCreateCheckpoint(server.Space);
            _map = new(server.Space);
            foreach (var (rid, index) in server.SnapshotRIDs.Select((rid, index) => (rid, index))) _map.Bind((ulong)index + 1, 1, rid);
            _map.Capture(_snapshot);
            _packet = new byte[_snapshot.GetEncodedSize()]; _snapshot.WriteTo(_packet);
            _destination = new byte[_packet.Length];
            _motion.SetCollision(server.Bodies[0], 0, server.Bodies[1], 0, Vector2.Zero, Vector2.Up, .1f, Vector2.Zero, Vector2.Zero, Vector2.Zero, .5f, .7f);
        }

        internal void PrepareBatch() => _prepare?.Invoke();

        internal Func<int, int> Bind(MethodInfo method)
        {
            if (method.DeclaringType == typeof(ShapeGeometry)) return GeometryCase(method);
            if (method.DeclaringType == typeof(ConvexPolygonShape) && method.Name == nameof(ConvexPolygonShape.SetPointCloud))
            {
                Vector2[] cloud = [new(-2, -1), new(2, -1), new(0, 2)];
                return _ => { ((ConvexPolygonShape)_shapes[4]).SetPointCloud(cloud); return 1; };
            }
            if (method.DeclaringType == typeof(PhysicsRayQueryParameters))
                return i => { _queries[i] = PhysicsRayQueryParameters.Create(Vector2.Zero, new(20, 0)); return 1; };
            if (method.DeclaringType == typeof(PhysicsCheckpoint) && method.Name == nameof(IDisposable.Dispose))
            {
                _prepare = () => { for (var i = 0; i < _points.Length; i++) _points[i] = PhysicsServer.SpaceCreateCheckpoint(_server.Space); };
                return i => { _points[i]!.Dispose(); return 1; };
            }
            if (method.DeclaringType == typeof(PhysicsSnapshotMap) && method.Name is "Bind" or "Unbind" or "Dispose")
            {
                _prepare = () =>
                {
                    for (var i = 0; i < _maps.Length; i++)
                    { _maps[i] = new(_server.Space); if (method.Name != "Bind") _maps[i]!.Bind(1, 1, _server.Bodies[0]); }
                };
                return method.Name switch
                {
                    "Bind" => i => { _maps[i]!.Bind(1, 1, _server.Bodies[0]); return 1; }
                    ,
                    "Unbind" => i => { ResultSink<bool>.Value = _maps[i]!.Unbind(1); return 1; }
                    ,
                    _ => i => { _maps[i]!.Dispose(); return 1; }
                };
            }
            if (method.DeclaringType == typeof(PhysicsSnapshot))
            {
                _checkWrite = method.Name == "WriteTo";
                return method.Name switch
                {
                    "WriteTo" => _ => { ResultSink<int>.Value = _snapshot.WriteTo(_destination); return 1; }
                    ,
                    "ReadFrom" => _ => { _snapshot.ReadFrom(_packet); return 1; }
                    ,
                    "GetEncodedSize" => _ => { ResultSink<int>.Value = _snapshot.GetEncodedSize(); return 1; }
                    ,
                    _ => throw new InvalidOperationException("Missing public snapshot case: " + method)
                };
            }
            object target = method.DeclaringType == typeof(PhysicsCheckpoint) ? _checkpoint :
                method.DeclaringType == typeof(PhysicsSnapshotMap) ? _map :
                method.DeclaringType == typeof(PhysicsTestMotionResult) ? _motion :
                method.DeclaringType == typeof(Shape) ? _shapes[0] : _shapes.Single(shape => shape.GetType() == method.DeclaringType);
            Expression Argument(ParameterInfo parameter)
            {
                var type = parameter.ParameterType; var name = parameter.Name!;
                object value;
                if (type == typeof(PhysicsSnapshot)) value = _snapshot;
                else if (type == typeof(Shape)) value = _shapes[0];
                else if (type == typeof(Transform)) value = name == "shapeTransform" ? new Transform(0, new(1, 0)) : Transform.Identity;
                else if (type == typeof(Vector2)) value = name == "normal" ? Vector2.Up : new Vector2(.01f, 0);
                else if (type == typeof(RID)) value = _server.Bodies[name == "body" ? 0 : 1];
                else if (type == typeof(int)) value = 0;
                else if (type == typeof(float)) value = .5f;
                else throw new InvalidOperationException("Unbound public resource argument: " + method + "/" + parameter);
                return Expression.Constant(value, type);
            }
            var invoke = Expression.Call(Expression.Constant(target), method, method.GetParameters().Select(Argument));
            Expression body = invoke;
            if (method.ReturnType != typeof(void))
            {
                var sink = typeof(ResultSink<>).MakeGenericType(method.ReturnType).GetField(nameof(ResultSink<int>.Value), BindingFlags.Public | BindingFlags.Static)!;
                body = Expression.Assign(Expression.Field(null, sink), invoke);
            }
            var index = Expression.Parameter(typeof(int), "index");
            return Expression.Lambda<Func<int, int>>(Expression.Block(body, Expression.Constant(1)), index).Compile();
        }

        internal void CleanupBatch()
        {
            for (var i = 0; i < _queries.Length; i++)
            { _queries[i]?.Dispose(); _queries[i] = null; _maps[i]?.Dispose(); _maps[i] = null; _points[i]?.Dispose(); _points[i] = null; }
            if (_snapshot.GetEncodedSize() != _packet.Length || _snapshot.ObjectCount != _server.Bodies.Length + 1)
                throw new InvalidOperationException("Public portable snapshot changed its population or encoded size.");
            if (_checkWrite && (ResultSink<int>.Value != _packet.Length || !_destination.AsSpan().SequenceEqual(_packet)))
                throw new InvalidOperationException("Public snapshot write changed its size or bytes.");
        }

        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            try { CleanupBatch(); }
            finally { _map.Dispose(); _checkpoint.Dispose(); _motion.Dispose(); foreach (var shape in _shapes) shape.Dispose(); }
        }
    }

    private sealed partial class ServerFixture
    {
        internal RID[] SnapshotRIDs => [.. Bodies, _area, _pin, _spring];
    }
}
