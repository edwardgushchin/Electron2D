using Electron2D;
using System.Buffers.Binary;

internal static partial class SceneReplicationTests
{
    private sealed class BindingAPI : SceneMultiplayer
    {
        internal Action<MultiplayerSynchronizer>? Added;
        internal int Additions;
        public override void ObjectConfigurationAdd(Node node, MultiplayerSynchronizer synchronizer)
        { base.ObjectConfigurationAdd(node, synchronizer); Additions++; Added?.Invoke(synchronizer); }
    }
    private static readonly ReplicationProperty<RigidBody, Vector2> BodyPosition = new(11,
        static body => body.Position, static (body, value) => body.Position = value, static _ => 8,
        static (value, bytes) => { BinaryPrimitives.WriteSingleLittleEndian(bytes, value.X); BinaryPrimitives.WriteSingleLittleEndian(bytes[4..], value.Y); return 8; },
        static bytes => bytes.Length == 8 ? new(BinaryPrimitives.ReadSingleLittleEndian(bytes), BinaryPrimitives.ReadSingleLittleEndian(bytes[4..])) : throw new InvalidDataException(), 8);

    internal static void RunReconfiguration(PhysicsServer.Backend backend)
    {
        List<Exception> failures = [];
        for (var fault = 0; fault < 8; fault++)
        {
            try { Reconfiguration(backend, fault); }
            catch (Exception error) { failures.Add(error); Console.WriteLine($"Rejected reconfiguration {backend}/{fault}: {error.Message}"); }
        }
        if (failures.Count != 0) throw new AggregateException("Rejected synchronizer authoring damaged active replication.", failures);
        Console.WriteLine($"Synchronizer rejected authority/config/root edits retain live {backend} physical replication; accepted replacement, disabling and reattachment passed.");
    }

    private static void Reconfiguration(PhysicsServer.Backend backend, int fault)
    {
        using var config = new SceneReplicationConfig(); config.AddProperty(BodyPosition);
        using var invalid = new SceneReplicationConfig();
        invalid.AddProperty(fault == 3
            ? new ReplicationProperty<RigidBody, int>(12, static _ => 0, static (_, _) => { }, static _ => 4, static (_, _) => 4, static _ => 0, 4, "Missing")
            : fault == 4
                ? new ReplicationProperty<RigidBody, int>(12, static _ => 0, static (_, _) => { }, static _ => 0, static (_, _) => 0, static _ => 0, 33554433)
                : new ReplicationProperty<MultiplayerSpawner, int>(12, static _ => 0, static (_, _) => { }, static _ => 4, static (_, _) => 4, static _ => 0, 4));
        using var world = new World(backend); using var shape = new CircleShape { Radius = 2 };
        var root = new SubViewport { Name = "Root", World = world };
        var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(2, 0) };
        body.AddChild(new CollisionShape { Shape = shape });
        var alternate = new RigidBody { Name = "Alternate", GravityScale = 0, Position = new(100, 0) };
        alternate.AddChild(new CollisionShape { Shape = shape });
        var sync = new MultiplayerSynchronizer { Name = "Sync", ReplicationConfig = config };
        sync.AddVisibilityFilter(static _ => true);
        body.AddChild(sync); root.AddChild(body); root.AddChild(alternate);
        using var peer = new WirePeer(); using var api = new BindingAPI { MultiplayerPeer = peer };
        using var tree = new SceneTree(root); tree.SetMultiplayer(api); peer.Admit(2);
        var rid = body.GetRID(); var identity = body.InstanceID;
        Frame(body);
        switch (fault)
        {
            case 0: Reject<ArgumentOutOfRangeException>(() => sync.SetMultiplayerAuthority(0)); break;
            case 1: Reject<ArgumentOutOfRangeException>(() => sync.SetMultiplayerAuthority(int.MinValue)); break;
            case 2: Reject<ArgumentException>(() => sync.ReplicationConfig = invalid); break;
            case 3: Reject<KeyNotFoundException>(() => sync.ReplicationConfig = invalid); break;
            case 4: Reject<ArgumentException>(() => sync.ReplicationConfig = invalid); break;
            case 5: Reject<ArgumentException>(() => sync.RootPath = "../.."); break;
            case 6: invalid.Dispose(); Reject<ObjectDisposedException>(() => sync.ReplicationConfig = invalid); break;
            case 7: Reject<ArgumentException>(() => sync.RootPath = "\0"); break;
        }
        Check(sync.GetMultiplayerAuthority() == 1 && sync.RootPath == ".." && ReferenceEquals(sync.ReplicationConfig, config),
            $"Rejected edit {fault} preserves authored authority, path and configuration");
        for (var i = 0; i < 16; i++) Frame(body);
        using var replacement = new SceneReplicationConfig(); replacement.AddProperty(BodyPosition);
        sync.ReplicationConfig = replacement; Frame(body);
        var additions = api.Additions;
        sync.RootPath = "../."; Frame(body);
        Check(api.Additions == additions + 1, "A changed root path retains registration callbacks even when it resolves to the same node");
        sync.RootPath = ".."; Frame(body);
        sync.RootPath = "../../Alternate"; Frame(alternate);
        sync.RootPath = ".."; Frame(body);
        sync.ReplicationConfig = null; Frame(null);
        sync.ReplicationConfig = config; Frame(body);
        sync.RootPath = "Missing"; Frame(null);
        Check(!sync.InternalProcessEnabled && !sync.InternalPhysicsProcessEnabled, "An absent root stops automatic visibility work");
        sync.RootPath = ".."; Frame(body);
        Check(sync.InternalProcessEnabled && !sync.InternalPhysicsProcessEnabled, "A restored root resumes the configured visibility lane");
        sync.RootPath = ""; Frame(null);
        Check(!sync.InternalProcessEnabled && !sync.InternalPhysicsProcessEnabled, "An empty root stops automatic visibility work");
        sync.RootPath = ".."; Frame(body);
        sync.SetMultiplayerAuthority(2, recursive: false); Frame(null);
        sync.SetMultiplayerAuthority(1, recursive: false); Frame(body);
        body.RemoveChild(sync); Frame(null); body.AddChild(sync); Frame(body);
        using var callbackConfig = new SceneReplicationConfig();
        callbackConfig.AddProperty(new ReplicationProperty<RigidBody, Vector2>(99, static body => body.Position, static (body, value) => body.Position = value,
            static _ => 8, static (value, bytes) => { BinaryPrimitives.WriteSingleLittleEndian(bytes, value.X); BinaryPrimitives.WriteSingleLittleEndian(bytes[4..], value.Y); return 8; },
            static bytes => new(BinaryPrimitives.ReadSingleLittleEndian(bytes), BinaryPrimitives.ReadSingleLittleEndian(bytes[4..])), 8));
        api.Added = current => current.ReplicationConfig = callbackConfig;
        sync.RootPath = "../../Alternate"; Frame(alternate, 99);
        api.Added = null; sync.RootPath = ".."; Frame(body, 99);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend && world.PhysicsFallbackReason is null,
            "Reconfiguration retains the explicitly selected physics backend");

        void Frame(RigidBody? source, uint propertyID = 11)
        {
            var before = body.Position.X; var tick = PhysicsServer.SpaceGetTick(world.Space);
            tree.PhysicsFrame(1d / 60); peer.Sent.Clear(); tree.ProcessFrame(0);
            Check(body.Position.X > before && MathF.Abs(body.Position.X - before - 2f / 60) < .0001f &&
                body.GetRID() == rid && body.InstanceID == identity && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                "Complete physical steps retain motion, identity and committed tick after configuration attempts");
            var packets = peer.Sent.Where(packet => packet.Bytes[0] == 6).ToArray();
            Check(packets.Length == (source is null ? 0 : 1), $"Physical state replication continues after rejected edit {fault}");
            if (source is null) return;
            var value = new byte[8]; BinaryPrimitives.WriteSingleLittleEndian(value, source.Position.X); BinaryPrimitives.WriteSingleLittleEndian(value.AsSpan(4), source.Position.Y);
            var packet = packets[0]; var sequence = BinaryPrimitives.ReadUInt32LittleEndian(packet.Bytes.AsSpan(1));
            Check(packet.Mode == TransferMode.Unreliable && packet.Bytes.AsSpan().SequenceEqual(StatePacket("Body/Sync", 6, sequence, (propertyID, value))),
                "The ordinary typed wire carries the current selected physical body's complete position");
        }
    }
}
