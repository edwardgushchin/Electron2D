using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Electron2D;

var consumerName = typeof(Program).Assembly.GetName().Name;
if (typeof(PhysicsServer).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
    .Cast<InternalsVisibleToAttribute>().Any(friend => friend.AssemblyName.Split(',')[0] == consumerName))
    throw new InvalidOperationException("Public performance consumers must have no friend access to the runtime.");

if (args.Length > 0 && args[0] == "scene") { PublicScenePerformance.Run(args[1..]); return; }
if (args.Length > 0 && args[0] == "server") { PublicServerPerformance.Run(args[1..]); return; }
if (args.Length > 0 && args[0] == "body") { PublicServerPerformance.RunBody(args[1..]); return; }
if (args.Length > 0 && args[0] == "nodes") { PublicServerPerformance.RunNodes(args[1..]); return; }
if (args.Length > 0 && args[0] == "resources") { PublicServerPerformance.RunResources(args[1..]); return; }
if (args.Length > 0 && args[0] == "bones") { PublicServerPerformance.RunBones(args[1..]); return; }

if (args.Length != 3 || args[0] is not ("cpu" or "gpu") ||
    !int.TryParse(args[1], out var count) || count < 2 || count > 16384)
    throw new ArgumentException("Use cpu|gpu body-count output.json; body-count must be 2..16384.");

var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
Console.WriteLine($"Preparing {backend} query fixture with {count} bodies.");
var started = Stopwatch.GetTimestamp();
using var fixture = new QueryFixture(backend, count);
var setupMS = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
var cases = fixture.Cases();
var declared = typeof(PhysicsDirectSpaceState).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
    .Where(method => !method.IsSpecialName).ToHashSet();
if (!declared.SetEquals(cases.Select(item => item.Method)))
    throw new InvalidOperationException("The query benchmark must cover every declared public query overload exactly once.");
if (cases.Select(item => item.Method).Distinct().Count() != cases.Length)
    throw new InvalidOperationException("Duplicate query benchmark overload.");

var results = new List<object>();
foreach (var hit in new[] { true, false })
{
    fixture.Configure(hit);
    foreach (var item in cases)
    {
        results.Add(Measure(item, hit));
        Console.WriteLine($"Completed {(hit ? "hit" : "miss")}: {item.Method}");
    }
}
var output = System.IO.Path.GetFullPath(args[2]);
Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new
{
    schema = 1,
    backend = backend.ToString(),
    bodies = count,
    workload = "static circle grid; repeated prepared hit/miss queries; no authoring or simulation between calls",
    scope = "all nine PhysicsDirectSpaceState public query overloads; not all physical API or a complete-step comparison",
    timing = "wall nanoseconds per call from batches, including delegate/loop overhead; percentiles describe batch means, not individual calls; baseline is not subtracted",
    gpuCompletion = "synchronous query results include required transfers and completion; no kernel-only timing",
    cache = "warmed repeated queries on unchanged geometry; cold/edited-world queries are not measured",
    managed = "owner and process-wide allocated bytes during measured batches; not retained/native/device memory",
    runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
    runtimeSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(PhysicsServer).Assembly.Location))),
    setupMS,
    warmBatches = 64,
    measuredBatches = 128,
    callsPerBatch = 64,
    measuredOverloads = cases.Length,
    results
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{backend}: {cases.Length} query overloads, hit/miss, {count} bodies; semantic checks passed. {output}");

static object Measure(QueryCase item, bool hit)
{
    const int batch = 64, samples = 128;
    var expected = hit ? 1 : 0;
    if (item.Call() != expected) throw new InvalidOperationException("Query hit/miss precondition failed: " + item.Method);
    long checksum = 0;
    for (var warm = 0; warm < 64; warm++) for (var i = 0; i < batch; i++) checksum += item.Call();
    var times = new double[samples];
    var baseline = new double[samples];
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
    GC.WaitForPendingFinalizers();
    var all = GC.GetTotalAllocatedBytes(true);
    var owner = GC.GetAllocatedBytesForCurrentThread();
    for (var sample = 0; sample < samples; sample++)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < batch; i++) checksum += item.Call();
        times[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / batch;
    }
    owner = GC.GetAllocatedBytesForCurrentThread() - owner;
    all = GC.GetTotalAllocatedBytes(true) - all;
    if (checksum != (64L + samples) * batch * expected || item.Call() != expected)
        throw new InvalidOperationException("Query result changed in the measured interval: " + item.Method);
    Func<int> empty = static () => 1;
    for (var sample = 0; sample < samples; sample++)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < batch; i++) checksum += empty();
        baseline[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / batch;
    }
    GC.KeepAlive(checksum);
    return new
    {
        method = item.Method.ToString(),
        scenario = hit ? "hit" : "miss",
        batchAverageCallNS = Distribution(times),
        baselineBatchAverageNS = Distribution(baseline),
        ownerBytesPerCall = (double)owner / (samples * batch),
        allThreadBytesPerCall = (double)all / (samples * batch),
        positiveAndNegativeResultChecks = true
    };
}

static object Distribution(double[] values)
{
    var mean = values.Average();
    Array.Sort(values);
    if (!double.IsFinite(mean) || mean <= 0) throw new InvalidOperationException("Timer did not resolve the query batch.");
    return new { mean, p50 = values[values.Length / 2], p95 = values[(int)(values.Length * .95)], p99 = values[(int)(values.Length * .99)], max = values[^1] };
}

internal sealed record QueryCase(MethodInfo Method, Func<int> Call);

internal sealed class QueryFixture : IDisposable
{
    private readonly RID _space;
    private readonly RID[] _bodies;
    private readonly CircleShape _circle = new() { Radius = 3 };
    private readonly CircleShape _queryShape = new() { Radius = 2 };
    private readonly PhysicsDirectSpaceState _state;
    private readonly PhysicsRayQueryParameters _ray = new();
    private readonly PhysicsPointQueryParameters _point = new();
    private readonly PhysicsShapeQueryParameters _overlap = new();
    private readonly PhysicsShapeQueryParameters _sweep = new();
    private readonly PhysicsPointResult[] _points = new PhysicsPointResult[8];
    private readonly PhysicsShapeResult[] _shapes = new PhysicsShapeResult[8];
    private readonly Vector2[] _contacts = new Vector2[16];
    private bool _disposed;

    internal QueryFixture(PhysicsServer.Backend backend, int count)
    {
        _bodies = new RID[count];
        try
        {
            _space = PhysicsServer.SpaceCreate(backend);
            PhysicsServer.SpaceSetActive(_space, true);
            PhysicsServer.AreaSetGravity(_space, 0);
            for (var i = 0; i < count; i++)
            {
                var body = _bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(body, _circle.GetRID());
                PhysicsServer.BodySetTransform(body, new(0, new(i % 64 * 20, i / 64 * 20)));
                PhysicsServer.BodySetSpace(body, _space);
            }
            PhysicsServer.SpaceStep(_space, 1d / 60);
            if (PhysicsServer.SpaceGetBackend(_space) != backend) throw new InvalidOperationException("Unexpected backend fallback.");
            _state = PhysicsServer.SpaceGetDirectState(_space);
            _overlap.Shape = _queryShape;
            _sweep.Shape = _queryShape;
            _sweep.Motion = new(20, 0);
        }
        catch (Exception primary)
        {
            try { Dispose(); }
            catch (Exception cleanup) { throw new AggregateException(primary, cleanup); }
            throw;
        }
    }

    internal void Configure(bool hit)
    {
        var y = hit ? 0 : -1000;
        _ray.From = new(-12, y); _ray.To = new(12, y);
        _point.Position = new(0, y);
        _overlap.Transform = new(0, new(1, y));
        _sweep.Transform = new(0, new(-12, y));
        var ray = _state.IntersectRay(_ray);
        if (hit && (ray is null || ray.Value.ColliderRID != _bodies[0]) || !hit && ray is not null)
            throw new InvalidOperationException("Query fixture must preserve exact collider identity and the empty control.");
    }

    internal QueryCase[] Cases() =>
    [
        Case(nameof(PhysicsDirectSpaceState.IntersectRay), [typeof(PhysicsRayQueryParameters)], () => _state.IntersectRay(_ray).HasValue ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.IntersectPoint), [typeof(PhysicsPointQueryParameters), typeof(int)], () => _state.IntersectPoint(_point, 8).Length > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.IntersectPoint), [typeof(PhysicsPointQueryParameters), typeof(Span<PhysicsPointResult>)], () => _state.IntersectPoint(_point, _points.AsSpan()) > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.IntersectShape), [typeof(PhysicsShapeQueryParameters), typeof(int)], () => _state.IntersectShape(_overlap, 8).Length > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.IntersectShape), [typeof(PhysicsShapeQueryParameters), typeof(Span<PhysicsShapeResult>)], () => _state.IntersectShape(_overlap, _shapes.AsSpan()) > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.CastMotion), [typeof(PhysicsShapeQueryParameters)], () => _state.CastMotion(_sweep).Item1 < 1 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.CollideShape), [typeof(PhysicsShapeQueryParameters), typeof(int)], () => _state.CollideShape(_overlap, 8).Length > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.CollideShape), [typeof(PhysicsShapeQueryParameters), typeof(Span<Vector2>)], () => _state.CollideShape(_overlap, _contacts.AsSpan()) > 0 ? 1 : 0),
        Case(nameof(PhysicsDirectSpaceState.GetRestInfo), [typeof(PhysicsShapeQueryParameters)], () => _state.GetRestInfo(_overlap).HasValue ? 1 : 0)
    ];

    private static QueryCase Case(string name, Type[] parameters, Func<int> call) =>
        new(typeof(PhysicsDirectSpaceState).GetMethod(name, parameters) ?? throw new InvalidOperationException("Query overload is missing: " + name), call);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        List<Exception>? errors = null;
        Release(() => _state?.Dispose());
        foreach (var body in _bodies) if (body.IsValid()) Release(() => PhysicsServer.FreeRID(body));
        if (_space.IsValid()) Release(() => PhysicsServer.FreeRID(_space));
        Release(_circle.Dispose); Release(_queryShape.Dispose); Release(_ray.Dispose);
        Release(_point.Dispose); Release(_overlap.Dispose); Release(_sweep.Dispose);
        if (errors is not null) throw new AggregateException("Query fixture cleanup failed.", errors);
        void Release(Action release)
        {
            try { release(); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
}
