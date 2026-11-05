using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Electron2D;

internal static partial class PublicServerPerformance
{
    internal static void RunBody(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Use body cpu|gpu output.json.");
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        var methods = typeof(PhysicsDirectBodyState).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal))
            .OrderBy(method => method.ToString(), StringComparer.Ordinal).ToArray();
        var results = new List<object>();
        foreach (var method in methods)
        {
            using var fixture = new ServerFixture(backend);
            var state = fixture.PrepareDirectBody();
            results.Add(Measure(method, BindBody(method, state), fixture.PrepareBatch, fixture.CleanupBatch));
            fixture.Validate();
            if (state.GetContactCount() == 0 || state.GetContactCollider(0) != fixture.Bodies[1])
                throw new InvalidOperationException("Public contact snapshot lost the prepared collider.");
            Console.WriteLine($"{backend} body: {method}");
        }
        var output = System.IO.Path.GetFullPath(args[1]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            schema = 1,
            backend = backend.ToString(),
            publicAPIOnly = true,
            scope = "every declared public PhysicsDirectBodyState method overload and property accessor",
            fixture = "one rigid circle contacting one static circle; retained completed contact snapshot; fresh world per member",
            timing = "warmed repeated host-return latency outside simulation; deferred solving excluded; batch percentiles are not individual call tails",
            contacts = "details warmed before measured batches; scene collider object is null for server-owned bodies",
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            measuredOverloads = methods.Length,
            results
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{backend}: {methods.Length} public direct-body members passed.");
    }

    private static Func<int, int> BindBody(MethodInfo declaration, PhysicsDirectBodyState state)
    {
        var method = declaration.IsGenericMethodDefinition ? declaration.MakeGenericMethod(typeof(Node)) : declaration;
        var index = Expression.Parameter(typeof(int), "index");
        var even = Expression.Equal(Expression.And(index, Expression.Constant(1)), Expression.Constant(0));
        Expression Argument(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            object first, second;
            if (type == typeof(int)) { first = second = 0; }
            else if (type == typeof(float)) { first = .01f; second = -.01f; }
            else if (type == typeof(Vector2)) { first = new Vector2(.01f, 0); second = new Vector2(-.01f, 0); }
            else if (type == typeof(uint)) { first = 1u; second = 2u; }
            else if (type == typeof(bool)) { first = false; second = true; }
            else if (type == typeof(Transform)) { first = Transform.Identity; second = new Transform(0, new(.01f, 0)); }
            else throw new InvalidOperationException("Unbound public body argument: " + method + "/" + parameter);
            return Expression.Condition(even, Expression.Constant(first, type), Expression.Constant(second, type));
        }
        var invoke = Expression.Call(Expression.Constant(state), method, method.GetParameters().Select(Argument));
        Expression body = invoke;
        if (method.ReturnType != typeof(void))
        {
            var sink = typeof(ResultSink<>).MakeGenericType(method.ReturnType).GetField(nameof(ResultSink<int>.Value), BindingFlags.Public | BindingFlags.Static)!;
            body = Expression.Assign(Expression.Field(null, sink), invoke);
        }
        return Expression.Lambda<Func<int, int>>(Expression.Block(body, Expression.Constant(1)), index).Compile();
    }

    private sealed partial class ServerFixture
    {
        internal PhysicsDirectBodyState PrepareDirectBody()
        {
            PhysicsServer.JointClear(_pin); PhysicsServer.JointClear(_spring);
            PhysicsServer.AreaSetGravity(Space, 1);
            PhysicsServer.BodySetMode(Bodies[1], PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(Bodies[1], new(0, new(5, 0)));
            PhysicsServer.BodySetMaxContactsReported(Bodies[0], 8);
            PhysicsServer.SpaceStep(Space, 1d / 60);
            var state = PhysicsServer.BodyGetDirectState(Bodies[0]) ?? throw new InvalidOperationException("Missing public direct body view.");
            if (state.GetContactCount() == 0 || state.GetContactCollider(0) != Bodies[1])
                throw new InvalidOperationException("Public direct-body fixture requires a solved contact.");
            return state;
        }
    }
}
