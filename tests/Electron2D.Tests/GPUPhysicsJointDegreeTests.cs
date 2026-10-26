using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;

internal static class GPUPhysicsJointDegreeTests
{
    private sealed record Sample(int Pins, bool PackedContacts, bool PackedBodies, double MeanMS, double P50MS,
        double P95MS, double P99MS, double MaxMS, double WaitMS, double UploadBytes, double ReadbackBytes,
        double UniformBytes, long OwnerBytes, long AllThreadBytes, float MaxAnchorError, float MaxSpeed, string Device);

    internal static void Run(bool measure)
    {
        using (var unpinned = new Rig(0, true, true))
        {
            for (var i = 0; i < 32; i++) unpinned.Frame(false);
            Check(unpinned.Position.X > 1, "The unpinned contact control moves under the prescribed input and conveyor");
        }
        var results = new List<Sample>();
        foreach (var count in measure ? new[] { 1, 16, 64, 256, 1024 } : new[] { 1, 64, 256 })
            foreach (var layout in measure ? new[] { (true, true) } : new[] { (true, true), (true, false), (false, false) })
            {
                using var rig = new Rig(count, layout.Item1, layout.Item2);
                for (var i = 0; i < 128; i++) rig.Frame(true);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                var times = new double[128]; var store = rig.Store;
                var upload = store.UploadBytes; var readback = store.ReadbackBytes; var uniforms = store.UniformBytes;
                var jointUpload = store.JointUploadBytes; var wait = store.WaitMS;
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < times.Length; i++)
                {
                    var start = Stopwatch.GetTimestamp(); rig.Frame(true);
                    times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                }
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0 && store.JointUploadBytes == jointUpload,
                    "Dense pinned full frames retain zero owner/all-thread allocation and no joint authoring upload");
                Array.Sort(times);
                var sample = new Sample(count, layout.Item1, layout.Item2, times.Average(), times[64], times[121], times[126], times[^1],
                    (store.WaitMS - wait) / 128, (store.UploadBytes - upload) / 128d, (store.ReadbackBytes - readback) / 128d,
                    (store.UniformBytes - uniforms) / 128d, owner, all, rig.MaxAnchorError, rig.MaxSpeed, store.DeviceName);
                results.Add(sample); Console.WriteLine(JsonSerializer.Serialize(sample));
                if (!measure) rig.VerifyEdits();
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_JOINT_DEGREE_OUTPUT") is { } path)
            File.WriteAllText(path, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private sealed class Rig : IDisposable
    {
        internal readonly Store Store = new();
        private readonly CircleShape _circle = new() { Radius = 2 };
        private readonly RectangleShape _rectangle = new() { Size = new(200, 2) };
        private readonly Store.BodyHandle _body, _floor;
        private readonly Store.BodyHandle[] _read;
        private readonly Store.Snapshot[] _state = new Store.Snapshot[1];
        private readonly int[] _limits = [4], _counts = new int[1];
        private readonly Store.ContactReport[] _contacts = new Store.ContactReport[4];
        private readonly Store.ShapeHandle _floorShape;
        private readonly Store.JointHandle[] _joints;
        private int _pins;
        internal float MaxAnchorError, MaxSpeed;
        internal Vector2 Position => _state[0].Position;

        internal Rig(int count, bool contacts, bool bodies)
        {
            _pins = count; Store.PackColoredContacts = contacts; Store.PackSolverBodyState = bodies; Store.CaptureContactReports = true;
            _body = Store.Add(new(PhysicsServer.BodyMode.RigidLinear, Vector2.Zero, 0, Vector2.Zero, 0, CanSleep: false));
            _floor = Store.Add(new(PhysicsServer.BodyMode.Static, new(0, 3), 0, new(30, 0), 0));
            Store.AddShape(_body, _circle); _floorShape = Store.AddShape(_floor, _rectangle);
            Store.SetCollisionPriority(_body, 7);
            _joints = new Store.JointHandle[count];
            for (var i = 0; i < count; i++) _joints[i] = Store.AddJoint(new(PhysicsServer.JointType.Pin, _body, default, Transform.Identity, Transform.Identity));
            _read = [_body];
        }
        internal void VerifyEdits()
        {
            var count = _pins;
            for (var i = 1; i < count; i++) Store.RemoveJoint(_joints[i]);
            _pins = 1;
            for (var i = 0; i < 16; i++) Frame(true);
            for (var i = 1; i < count; i++) _joints[i] = Store.AddJoint(new(PhysicsServer.JointType.Pin, _body, default, Transform.Identity, Transform.Identity));
            _pins = count;
            for (var i = 0; i < 16; i++) Frame(true);
            foreach (var joint in _joints) Store.RemoveJoint(joint);
            _pins = 0;
            for (var i = 0; i < 16; i++) Frame(false);
            Check(Position.X > 1, "Removing every pin releases real motion after cached degree shrink/regrowth");
        }
        internal void Frame(bool constrained)
        {
            Store.SetVelocity(_body, Vector2.Right, 0);
            Store.Simulate(1f / 60, new(0, 980), substeps: 1, iterations: 16);
            Store.Read(_read, _state); Store.ReadContactReports(_read, _limits, _counts, _contacts);
            Check(Store.Count == 2 && Store.JointCount == _pins && Store.ContactPointCount > 0 && Store.ContactColorCount > 0,
                "The complete world retains both bodies, every authored pin and a real colored contact");
            Check(_counts[0] > 0 && _contacts[0].ColliderShape == _floorShape.Index &&
                _contacts[0].ColliderShapeGeneration == _floorShape.Generation && _contacts[0].Normal.DistanceTo(Vector2.Up) < .001f,
                "The pinned body reports the actual conveyor floor and support normal");
            var speed = new Vector2(_state[0].Velocity.X, _state[0].Velocity.Y).Length();
            MaxAnchorError = MathF.Max(MaxAnchorError, Position.Length()); MaxSpeed = MathF.Max(MaxSpeed, speed);
            if (constrained) Check(Position.Length() < .01f && speed < .01f && MathF.Abs(_state[0].Velocity.Z) < .001f,
                "Every dense pin graph rejects the prescribed movement while keeping its world anchor");
        }
        public void Dispose() { Store.Dispose(); _circle.Dispose(); _rectangle.Dispose(); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
