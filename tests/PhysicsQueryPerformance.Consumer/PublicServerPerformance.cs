using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Electron2D;

internal static partial class PublicServerPerformance
{
    internal static void Run(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Use server cpu|gpu output.json.");
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        var methods = typeof(PhysicsServer).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.ToString(), StringComparer.Ordinal).ToArray();
        var results = new List<object>();
        foreach (var method in methods)
        {
            using var fixture = new ServerFixture(backend);
            Func<int, int> call;
            if (method.Name == nameof(PhysicsServer.BodyGetTransform) && method.GetParameters().Length == 3)
                call = _ => { PhysicsServer.BodyGetTransform(fixture.Space, fixture.Bodies, fixture.Poses); return 1; };
            else call = fixture.Special(method) ?? fixture.Bind(method);
            results.Add(Measure(method, call, fixture.PrepareBatch, fixture.CleanupBatch));
            fixture.Validate();
            Console.WriteLine($"{backend}: {method}");
        }
        var output = System.IO.Path.GetFullPath(args[1]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            schema = 1,
            backend = backend.ToString(),
            publicAPIOnly = true,
            scope = "every declared public static PhysicsServer method overload; other physical classes remain separate",
            fixture = "two attached rigid circles, one Area, retained pin/spring joints; fresh world for each overload",
            timing = "warmed host-return wall nanoseconds, including direct compiled delegate/loop overhead; no private counters",
            authoring = "prepared arguments; deferred step work is not charged to setters; SpaceStep separately times simulation; preparation/cleanup outside clock",
            getters = "repeated cached reads on unchanged completed state",
            managed = "owner/process allocated bytes, not retained/native/device memory",
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            sampling = "ordinary operations: 32 warm/64 measured batches of 32; SpaceCreate: 1 warm/8 measured batches of one with another retained world/device",
            measuredOverloads = methods.Length,
            results
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{backend}: {methods.Length} public server overloads passed.");
    }

    private static object Measure(MethodInfo method, Func<int, int> call, Action prepare, Action cleanup)
    {
        var batch = method.Name == nameof(PhysicsServer.SpaceCreate) ? 1 : 32;
        var warmCount = batch == 1 ? 1 : 32;
        var samples = batch == 1 ? 8 : 64;
        long checksum = 0;
        for (var warm = 0; warm < warmCount; warm++)
        {
            prepare();
            for (var i = 0; i < batch; i++) checksum += call(i);
            cleanup();
        }
        var times = new double[samples];
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        long all = 0, owner = 0;
        for (var sample = 0; sample < samples; sample++)
        {
            prepare();
            var allStart = GC.GetTotalAllocatedBytes(true); var ownerStart = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            for (var i = 0; i < batch; i++) checksum += call(i);
            times[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / batch;
            owner += GC.GetAllocatedBytesForCurrentThread() - ownerStart;
            all += GC.GetTotalAllocatedBytes(true) - allStart;
            cleanup();
        }
        if (checksum != (warmCount + samples) * batch) throw new InvalidOperationException("Public call checksum failed: " + method);
        var mean = times.Average(); Array.Sort(times);
        if (!double.IsFinite(mean) || mean <= 0) throw new InvalidOperationException("Unresolved call timer: " + method);
        Func<int, int> empty = Empty;
        var baseline = new double[samples];
        for (var sample = 0; sample < samples; sample++)
        {
            var start = Stopwatch.GetTimestamp();
            for (var i = 0; i < batch; i++) checksum += empty(i);
            baseline[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / batch;
        }
        GC.KeepAlive(checksum);
        return new
        {
            method = method.ToString(),
            warmBatches = warmCount,
            measuredBatches = samples,
            callsPerBatch = batch,
            scenario = Scenario(method),
            batchAverageCallNS = new { mean, p50 = times[samples / 2], p95 = times[(int)(samples * .95)], p99 = times[(int)(samples * .99)], max = times[^1] },
            emptyLoopMeanNS = baseline.Average(),
            batchTimerResolutionNSPerCall = 1e9 / Stopwatch.Frequency / batch,
            ownerBytesPerCall = (double)owner / (batch * samples),
            allThreadBytesPerCall = (double)all / (batch * samples)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Empty(int _) => 1;

    private static string Scenario(MethodInfo method) => method.Name switch
    {
        "ShapeGetData" => "detached caller-owned copy",
        "SpaceStep" => "complete two-body public simulation step",
        "SpaceCreate" => method.GetParameters().Length == 0 ? "default CPU world even in GPU fixture; retained peer world" : "selected backend world; retained peer world/device",
        "CustomShapeCreate" => "unattached custom empty-geometry RID; no custom solving",
        "FreeRID" => "release attached shaped static body",
        "SpaceCreateCheckpoint" => "caller-owned two-body checkpoint",
        "ShapeCollide" or "BodyCollideShape" => "overlapping prepared circles and reusable contact storage",
        "BodyTestMotion" => "prepared positive two-body sweep; reusable result",
        "MoveAndCollide" => "test-only positive character sweep; caller-owned result",
        "TestMove" => "prepared positive character sweep; reusable result",
        "MoveAndSlide" or "ApplyFloorSnap" => "settled floor; zero requested velocity",
        "Collide" or "CollideWithMotion" or "CollideAndGetContacts" or "CollideWithMotionAndGetContacts" => "standalone overlapping circles; shared host algorithm",
        "Dispose" => "prepared caller-owned object release",
        "Capture" or "Restore" or "Apply" => "prepared same-world snapshot operation",
        _ when method.Name.EndsWith("Create", StringComparison.Ordinal) => "unattached server-owned RID",
        _ when method.Name.Contains("Get", StringComparison.Ordinal) || method.Name.StartsWith("Is", StringComparison.Ordinal) || method.Name.StartsWith("get_", StringComparison.Ordinal) => "cached observation",
        _ => "prepared authoring inputs; deferred step cost excluded"
    };

    private static class ResultSink<T> { public static T Value = default!; }

    private sealed partial class ServerFixture : IDisposable
    {
        internal RID Space { get; }
        internal RID[] Bodies { get; } = new RID[2];
        internal Transform[] Poses { get; } = new Transform[2];
        private readonly CircleShape _first = new() { Radius = 3 };
        private readonly CircleShape _second = new() { Radius = 4 };
        private readonly CircleShape _dataLow = new() { Radius = 2 };
        private readonly RID _area, _pin, _spring, _rawShape;
        private readonly Shape?[] _copies = new Shape?[32];
        private readonly PhysicsServer.Backend _backend;
        private bool _disposed;
        private static readonly Action<PhysicsDirectBodyState> BodyA = static _ => { }, BodyB = static _ => { };
        private static readonly Action<PhysicsDirectBodyState, int> DataA = static (_, _) => { }, DataB = static (_, _) => { };
        private static readonly Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> AreaA = static (_, _, _, _, _) => { }, AreaB = static (_, _, _, _, _) => { };

        internal ServerFixture(PhysicsServer.Backend backend)
        {
            _backend = backend;
            try
            {
                Space = PhysicsServer.SpaceCreate(backend);
                PhysicsServer.SetActive(true);
                PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.AreaSetGravity(Space, 0);
                _rawShape = PhysicsServer.CircleShapeCreate(); PhysicsServer.ShapeSetData(_rawShape, _dataLow);
                for (var i = 0; i < Bodies.Length; i++)
                {
                    var body = Bodies[i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodyAddShape(body, _first.GetRID());
                    PhysicsServer.BodySetTransform(body, new(0, new(i * 20, 0))); PhysicsServer.BodySetSpace(body, Space);
                }
                _area = PhysicsServer.AreaCreate(); PhysicsServer.AreaAddShape(_area, _first.GetRID()); PhysicsServer.AreaSetSpace(_area, Space);
                _pin = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(_pin, Vector2.Zero, Bodies[0], Bodies[1]);
                PhysicsServer.PinJointSetAngularLimitLower(_pin, -1); PhysicsServer.PinJointSetAngularLimitUpper(_pin, 1);
                _spring = PhysicsServer.JointCreate(); PhysicsServer.JointMakeDampedSpring(_spring, Vector2.Zero, new(20, 0), Bodies[0], Bodies[1]);
                PhysicsServer.SpaceStep(Space, 1d / 60); Validate();
            }
            catch (Exception primary)
            {
                try { Dispose(); } catch (Exception cleanup) { throw new AggregateException(primary, cleanup); }
                throw;
            }
        }

        internal Func<int, int> Bind(MethodInfo declaration)
        {
            var method = declaration.IsGenericMethodDefinition ? declaration.MakeGenericMethod(typeof(int)) : declaration;
            var index = Expression.Parameter(typeof(int), "index");
            var even = Expression.Equal(Expression.And(index, Expression.Constant(1)), Expression.Constant(0));
            var arguments = method.GetParameters().Select(parameter => Argument(method, parameter, even)).ToArray();
            var invoke = Expression.Call(method, arguments);
            Expression body = invoke;
            if (method.ReturnType == typeof(Shape)) body = Expression.Assign(Expression.ArrayAccess(Expression.Constant(_copies), index), invoke);
            else if (method.ReturnType != typeof(void))
            {
                var sink = typeof(ResultSink<>).MakeGenericType(method.ReturnType).GetField(nameof(ResultSink<int>.Value), BindingFlags.Public | BindingFlags.Static)!;
                body = Expression.Assign(Expression.Field(null, sink), invoke);
            }
            return Expression.Lambda<Func<int, int>>(Expression.Block(body, Expression.Constant(1)), index).Compile();
        }

        private Expression Argument(MethodInfo method, ParameterInfo parameter, Expression even)
        {
            var type = parameter.ParameterType; var name = parameter.Name!;
            object first, second;
            if (type == typeof(RID))
            {
                var value = name switch
                {
                    "body" => Bodies[0],
                    "area" => _area,
                    "shape" => method.Name.StartsWith("Shape", StringComparison.Ordinal) ? _rawShape : _first.GetRID(),
                    "space" => Space,
                    "joint" => method.Name.StartsWith("DampedSpring", StringComparison.Ordinal) ? _spring : _pin,
                    _ => throw new InvalidOperationException("Unbound public RID argument: " + method + "/" + name)
                };
                first = value; second = name == "shape" && !method.Name.StartsWith("Shape", StringComparison.Ordinal) ? _second.GetRID() : value;
                if (name == "space" && method.Name is "BodySetSpace" or "AreaSetSpace") first = default(RID);
            }
            else if (type == typeof(int)) { first = name == "index" ? 0 : 1; second = name == "index" ? 0 : 2; }
            else if (type == typeof(uint)) { first = 1u; second = 2u; }
            else if (type == typeof(float)) { first = method.Name.EndsWith("Lower", StringComparison.Ordinal) ? -.5f : .1f; second = method.Name.EndsWith("Lower", StringComparison.Ordinal) ? -.25f : .2f; }
            else if (type == typeof(bool)) { first = true; second = false; }
            else if (type == typeof(Vector2)) { first = Vector2.Zero; second = new Vector2(1, 0); }
            else if (type == typeof(Vector2?)) { first = (Vector2?)new Vector2(0, 1); second = (Vector2?)new Vector2(0, -1); }
            else if (type == typeof(Transform)) { first = Transform.Identity; second = new Transform(0, new(1, 0)); }
            else if (type == typeof(Shape)) { first = _dataLow; second = _second; }
            else if (type == typeof(Action<PhysicsDirectBodyState>)) { first = BodyA; second = BodyB; }
            else if (type == typeof(Action<PhysicsDirectBodyState, int>)) { first = DataA; second = DataB; }
            else if (type == typeof(Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>)) { first = AreaA; second = AreaB; }
            else if (type.IsEnum) { var values = Enum.GetValues(type); first = values.GetValue(0)!; second = values.GetValue(Math.Min(1, values.Length - 1))!; }
            else throw new InvalidOperationException("Unbound public argument: " + method + "/" + name + "/" + type);
            if (type == typeof(Vector2?))
                return Expression.Condition(even, Expression.Convert(Expression.Constant((Vector2)first), type), Expression.Convert(Expression.Constant((Vector2)second), type));
            return Expression.Condition(even, Expression.Constant(first, type), Expression.Constant(second, type));
        }

        internal void Validate()
        {
            if (PhysicsServer.SpaceGetBackend(Space) != _backend || Bodies.Any(body => !PhysicsServer.BodyGetTransform(body).IsFinite()))
                throw new InvalidOperationException("Public server fixture lost its backend or finite authoring state.");
        }

        internal void CleanupBatch()
        {
            CleanupSpecial();
            for (var i = 0; i < _copies.Length; i++)
            {
                _copies[i]?.Dispose(); _copies[i] = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            List<Exception>? errors = null;
            Release(CleanupBatch);
            foreach (var rid in new[] { _pin, _spring, _area }.Concat(Bodies).Append(_rawShape).Append(Space))
                if (rid.IsValid()) Release(() => PhysicsServer.FreeRID(rid));
            Release(_first.Dispose); Release(_second.Dispose); Release(_dataLow.Dispose);
            Release(DisposeSpecial);
            if (errors is not null) throw new AggregateException("Public server fixture cleanup failed.", errors);
            void Release(Action release) { try { release(); } catch (Exception error) { (errors ??= []).Add(error); } }
        }
    }
}
