using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    private static void VerifyConstraintColorWake(GPUPhysicsWorld gpu)
    {
        const int bodyCount = B2Constants.B2_GRAPH_COLOR_COUNT + 8;
        B2WorldId Create()
        {
            var wd = b2DefaultWorldDef(); wd.gravity = new(0, 0); var id = b2CreateWorld(wd);
            var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody; var sd = b2DefaultShapeDef();
            for (var i = 0; i < bodyCount; i++)
            {
                bd.position = new(i == 0 ? -20 : 1.99f * (i - 1), 0);
                b2CreateCircleShape(b2CreateBody(id, bd), sd, new B2Circle { radius = 1 });
            }
            var world = b2GetWorldFromId(id);
            for (var i = 2; i < bodyCount; i++) SplitJoint(id, b2MakeBodyId(world, 1), b2MakeBodyId(world, i), 0);
            b2World_Step(id, 1f / 60, 4);
            b2Body_SetAwake(b2MakeBodyId(world, 1), false);
            return id;
        }
        var c = Create(); var g = Create(); var cpu = b2GetWorldFromId(c); var actual = b2GetWorldFromId(g);
        try
        {
            gpu.EnableConstraintColors(actual); var select = actual.selectConstraintColor;
            var joints = 0; var contacts = 0; var overflow = 0;
            actual.selectConstraintColor = (w, kind, id, a, b, color) =>
            {
                var result = select(w, kind, id, a, b, color);
                if (kind == 1) joints++; else contacts++;
                if (result == B2ConstraintGraphs.B2_OVERFLOW_INDEX) overflow++;
                return result;
            };
            foreach (var world in new[] { cpu, actual }) b2Body_SetTransform(b2MakeBodyId(world, 0), new(-1.8f, 0), new(1, 0));
            b2World_Step(c, 1f / 60, 4); b2World_Step(g, 1f / 60, 4); CompareIslands(cpu, actual);
            if (joints != bodyCount - 2 || contacts < bodyCount - 2 || overflow == 0) throw new Exception($"The color fixture must wake touching contacts and overflow joints: joints={joints}, contacts={contacts}, overflow={overflow}.");
            var submissions = gpu.ConstraintColorSubmissionCount;
            // Contact pairs stay touching: no color mutation needs another GPU submission.
            b2World_Step(c, 1f / 60, 4); b2World_Step(g, 1f / 60, 4); CompareIslands(cpu, actual);
            if (gpu.ConstraintColorSubmissionCount != submissions) throw new Exception("Unchanged constraints must skip coloring.");
        }
        finally { b2DestroyWorld(c); b2DestroyWorld(g); }
        Console.WriteLine("GPU coloring preserves sleeping contact/joint wake order, overflow, occupancy and unchanged-batch elision.");
    }

    private static void VerifyConstraintColorFailure(GPUPhysicsWorld gpu)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (var shader in new[] { false, true })
        {
            var id = b2CreateWorld(b2DefaultWorldDef()); var world = b2GetWorldFromId(id);
            try
            {
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody;
                for (var i = 0; i < 2; i++) b2CreateCircleShape(b2CreateBody(id, bd), b2DefaultShapeDef(), new B2Circle { radius = 1 });
                gpu.EnableConstraintColors(world); var begin = world.beginConstraintColors;
                world.beginConstraintColors = w =>
                {
                    begin(w);
                    var storage = typeof(GPUPhysicsWorld).GetField(shader ? "_colorChanges" : "_colorResults", flags)!.GetValue(gpu)!;
                    var data = (Array)storage.GetType().GetField("Data", flags)!.GetValue(storage)!;
                    if (shader)
                    {
                        var record = data.GetValue(0)!; record.GetType().GetField("A", flags)!.SetValue(record, w.bodies.count); data.SetValue(record, 0);
                        var words = (int)typeof(GPUPhysicsWorld).GetField("_colorWords", flags)!.GetValue(gpu)!;
                        typeof(GPUPhysicsWorld).GetMethod("DispatchConstraintColors", flags)!.Invoke(gpu, [w.bodies.count, words * B2ConstraintGraphs.B2_OVERFLOW_INDEX]);
                    }
                    else
                    {
                        data.SetValue(B2Constants.B2_GRAPH_COLOR_COUNT, 0);
                        typeof(GPUPhysicsWorld).GetMethod("ValidateConstraintColors", flags)!.Invoke(gpu, null);
                    }
                };
                try { b2World_Step(id, 1f / 60, 4); throw new Exception("Invalid GPU coloring reached publication."); }
                catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException error &&
                    error.Message == (shader ? "GPU constraint coloring rejected the batch." : "GPU constraint color is invalid."))
                { }
                if (world.contacts.data[0].colorIndex != -1) throw new Exception("Rejected color output mutated the graph.");
            }
            finally
            {
                world.locked = false; foreach (var arena in world.arena.AsSpan()) arena.Abort(); b2DestroyWorld(id);
            }
        }
    }
}
