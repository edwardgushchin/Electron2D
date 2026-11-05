using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Electron2D;

internal static partial class PublicServerPerformance
{
    internal static void RunBones(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Use bones cpu|gpu output.json.");
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        Type[] families = [typeof(PhysicalBone), typeof(SkeletonModificationJiggle), typeof(SkeletonModificationPhysicalBones)];
        var methods = families.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName).OrderBy(method => method.DeclaringType!.Name + method, StringComparer.Ordinal).ToArray();
        var results = new List<object>();
        foreach (var method in methods)
        {
            using var fixture = new BoneFixture(backend);
            results.Add(new { type = method.DeclaringType!.Name, measurement = Measure(method, fixture.Bind(method), static () => { }, static () => { }) });
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
            scope = "all declared public non-accessor methods of PhysicalBone and its two physical skeleton modifiers",
            fixture = "one attached selected physical bone, real child pin joint, one authored skeleton bone and bound one-slot modifier chains",
            timing = "repeated warmed host-return latency; modification setters and start/stop requests do not include later simulation or animation execution",
            requests = "StartSimulation and StopSimulation repeat a retained request; getters observe completed state; FetchPhysicalBones performs actual traversal",
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            measuredOverloads = methods.Length,
            results
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class BoneFixture : IDisposable
    {
        private readonly CircleShape _shape = new() { Radius = 1 };
        private readonly SkeletonModificationJiggle _jiggle = new() { JiggleDataChainLength = 1, TargetNodePath = "Target" };
        private readonly SkeletonModificationPhysicalBones _physical = new() { PhysicalBoneChainLength = 1 };
        private readonly SkeletonModificationStack _stack = new() { Enabled = true };
        private readonly PhysicalBone _body = new() { Name = "P", BoneNodePath = "../A", GravityScale = 0, CanSleep = false };
        private readonly Bone _bone = new() { Name = "A", Rest = Transform.Identity };
        private readonly World _world;
        private readonly SubViewport _root;
        private readonly SceneTree _tree;
        private readonly PhysicsServer.Backend _backend;

        internal BoneFixture(PhysicsServer.Backend backend)
        {
            _backend = backend;
            _bone.SetAutocalculateLengthAndAngle(false); _bone.SetLength(10);
            var rig = new Skeleton(); rig.AddChild(_bone);
            rig.AddChild(new Entity { Name = "Target", Position = new(10, 0) });
            _body.AddChild(new CollisionShape { Shape = _shape });
            _body.AddChild(new PinJoint { NodeA = "..", NodeB = "../../Peer" }); rig.AddChild(_body);
            var peer = new StaticBody { Name = "Peer", Position = new(10, 0) };
            peer.AddChild(new CollisionShape { Shape = _shape }); rig.AddChild(peer);
            _jiggle.SetJiggleJointBoneNode(0, "A"); _physical.SetPhysicalBoneNode(0, "P");
            _stack.AddModification(_jiggle); _stack.AddModification(_physical); rig.SetModificationStack(_stack);
            _world = new(backend); _root = new() { World = _world }; _root.AddChild(rig); _tree = new(_root);
            _physical.StartSimulation(); _tree.PhysicsFrame(1d / 60); _tree.ProcessFrame(1d / 60);
            if (!_body.IsSimulatingPhysics() || _body.GetJoint() is null || !_stack.GetIsSetup())
                throw new InvalidOperationException("Public physical bone fixture requires an active selected body, joint and bound modifier stack.");
        }

        internal Func<int, int> Bind(MethodInfo method)
        {
            object target = method.DeclaringType == typeof(PhysicalBone) ? _body :
                method.DeclaringType == typeof(SkeletonModificationJiggle) ? _jiggle : _physical;
            var index = Expression.Parameter(typeof(int), "index");
            var even = Expression.Equal(Expression.And(index, Expression.Constant(1)), Expression.Constant(0));
            Expression Argument(ParameterInfo parameter)
            {
                var type = parameter.ParameterType;
                object first, second;
                if (type == typeof(int)) { first = second = 0; }
                else if (type == typeof(uint)) { first = 1u; second = 2u; }
                else if (type == typeof(float)) { first = .2f; second = .3f; }
                else if (type == typeof(bool)) { first = true; second = false; }
                else if (type == typeof(Vector2)) { first = new Vector2(0, 1); second = new Vector2(0, 2); }
                else if (type == typeof(string)) { first = second = method.Name == "SetPhysicalBoneNode" ? "P" : "A"; }
                else if (type == typeof(string[])) return Expression.Constant(null, type);
                else throw new InvalidOperationException("Unbound public bone argument: " + method + "/" + parameter);
                return Expression.Condition(even, Expression.Constant(first, type), Expression.Constant(second, type));
            }
            var invoke = Expression.Call(Expression.Constant(target), method, method.GetParameters().Select(Argument));
            Expression body = invoke;
            if (method.ReturnType != typeof(void))
            {
                var sink = typeof(ResultSink<>).MakeGenericType(method.ReturnType).GetField(nameof(ResultSink<int>.Value), BindingFlags.Public | BindingFlags.Static)!;
                body = Expression.Assign(Expression.Field(null, sink), invoke);
            }
            return Expression.Lambda<Func<int, int>>(Expression.Block(body, Expression.Constant(1)), index).Compile();
        }

        internal void Validate()
        {
            if (_world.PhysicsBackend != _backend || !_body.GlobalTransform.IsFinite() || !_bone.GlobalTransform.IsFinite())
                throw new InvalidOperationException("Public physical skeleton fixture lost its backend or finite poses.");
        }

        public void Dispose()
        {
            try { _tree.Dispose(); }
            finally { _root.Dispose(); _world.Dispose(); _stack.Dispose(); _physical.Dispose(); _jiggle.Dispose(); _shape.Dispose(); }
        }
    }
}
