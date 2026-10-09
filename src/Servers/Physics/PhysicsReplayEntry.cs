namespace Electron2D;

/// <summary>Common attached scene/server state accompanying a backend-local replay checkpoint.</summary>
internal sealed class PhysicsReplayEntry
{
    internal readonly PhysicsColliderBackend Backend;
    internal readonly CollisionObject? Scene;
    internal readonly PhysicsServerCollider? Server;
    private readonly PhysicsBodyRuntime? _runtime;
    private readonly PhysicsAreaRuntime? _areaRuntime;
    private readonly PhysicsBody.BodyReplay? _body;
    private readonly RigidBody.RigidReplay? _rigid;
    private readonly StaticBody.StaticReplay? _surface;
    private readonly AnimatableBody.AnimatableReplay? _animatable;
    private readonly CharacterBody.CharacterReplay? _character;
    private readonly Area.AreaReplay? _area;
    private readonly PhysicsBodyRuntime.ReplayState? _bodyRuntime;
    private PhysicsServerCollider.ReplayState _server;
    private PhysicsColliderBackend.ReplayState _backend;
    private readonly PhysicsShapePairTracker _areaPairs = new();
    private readonly List<RID> _exceptions = [];
    private ShapeState[] _shapes = [];
    private int _shapeCount;
    private long _attachment;
    private Configuration _configuration;
    private Node? _parent;
    private SceneTree? _tree;
    private Transform _pose;
    internal int Depth { get; private set; }
    internal readonly record struct ShapeState(Shape Shape, Transform Pose, bool Active, int Index, bool OneWay, float Margin, Vector2 Direction, ulong Revision, float Bias, ulong Owner = 0);
    private readonly record struct Configuration(uint Layer, uint Mask, ulong Object, ulong Canvas, float Priority, PhysicsServer.BodyMode Mode,
        bool MadeStatic, bool TopLevel, PhysicsMaterial? Material, ulong MaterialRevision, bool Monitorable, GPUPhysicsBodyStore.FieldParameters Fields,
        Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? BodyCallback,
        Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? AreaCallback, ulong AreaGeneration,
        string? BonePath, bool BoneSimulate, bool BoneActive, bool BoneFollow, bool BoneAuto);

    internal PhysicsReplayEntry(PhysicsColliderBackend backend, CollisionObject? scene, PhysicsServerCollider? server)
    {
        Backend = backend; Scene = scene; Server = server;
        if (scene is PhysicsBody body) { _body = new(); _runtime = body.Runtime; }
        if (server is { IsArea: false }) _runtime = server.Runtime;
        if (_runtime is not null) _bodyRuntime = new();
        if (scene is RigidBody) _rigid = new();
        if (scene is StaticBody) _surface = new();
        if (scene is AnimatableBody) _animatable = new();
        if (scene is CharacterBody) _character = new();
        if (scene is Area) _area = new();
        if (scene is Area || server is { IsArea: true }) _areaRuntime = PhysicsServer.Service.AreaRuntime(backend.RID);
    }
    private Configuration Settings()
    {
        var body = Scene as PhysicsBody; var area = Scene as Area; var bone = Scene as PhysicalBone;
        var fields = area?.Fields ?? Server?.AreaFields;
        var material = body?.MaterialOverride;
        return new(Scene?.CollisionLayer ?? Server!.CollisionLayer, Scene?.CollisionMask ?? Server!.CollisionMask,
            Backend.ObjectIdentity.ID, Backend.CanvasInstanceID, Backend.CollisionPriority, body?.RequestedBodyMode ?? Server?.Mode ?? PhysicsServer.BodyMode.Static,
            Scene?.PhysicsMadeStatic ?? false, Scene?.TopLevel ?? false, material, material?.Revision ?? 0, area?.Monitorable ?? Server?.Monitorable ?? false,
            fields is null ? default : PhysicsSpace.ReplayFields(fields), _areaRuntime?.BodyCallback, _areaRuntime?.AreaCallback, _areaRuntime?.Generation ?? 0,
            bone?.BoneNodePath, bone?.SimulatePhysics ?? false, bone?.IsSimulatingPhysics() ?? false, bone?.FollowBoneWhenSimulating ?? false, bone?.AutoConfigureJoint ?? false);
    }
    private int ShapeCount => Scene?.ShapeSlots.Count ?? Server!.ShapeCount;
    private ShapeState ShapeAt(int index)
    {
        if (Scene is null) return Server!.ReplayShape(index);
        var slot = Scene.ShapeSlots[index];
        var oneWay = slot.OneWayOverride ?? slot.Owner.OneWay;
        var margin = slot.OneWayOverride.HasValue ? slot.MarginOverride : slot.Owner.Margin;
        var direction = slot.OneWayOverride.HasValue ? slot.DirectionOverride : slot.Owner.Direction;
        return new(slot.Shape, slot.Transform, slot.Active, slot.Index, oneWay, margin, direction, slot.Revision,
            slot.Shape.IsDisposed ? 0 : slot.Shape.CustomSolverBias, Scene.GetShapeOwnerObject(index)?.InstanceID ?? 0);
    }
    internal void Capture()
    {
        _attachment = Backend.AttachmentVersion; _configuration = Settings(); _backend = Backend.CaptureReplay();
        _parent = Scene?.Parent; _tree = Scene?.Tree; _pose = Scene?.GlobalTransform ?? default;
        Depth = 0; for (var ancestor = _parent; ancestor is not null; ancestor = ancestor.Parent) Depth++;
        _shapeCount = ShapeCount; if (_shapes.Length < _shapeCount) Array.Resize(ref _shapes, _shapeCount);
        for (var i = 0; i < _shapeCount; i++) _shapes[i] = ShapeAt(i);
        if (Scene is PhysicsBody body) body.CaptureReplay(_body!);
        if (Scene is RigidBody rigid) rigid.CaptureReplay(_rigid!);
        if (Scene is StaticBody surface) surface.CaptureReplay(_surface!);
        if (Scene is AnimatableBody moving) moving.CaptureReplay(_animatable!);
        if (Scene is CharacterBody character) character.CaptureReplay(_character!);
        if (Scene is Area area) area.CaptureReplay(_area!);
        if (Server is not null) _server = Server.CaptureReplay();
        _runtime?.CaptureReplay(_bodyRuntime!);
        _areaRuntime?.Pairs.CopyTo(_areaPairs);
        PhysicsServer.Service.CopyReplayExceptions(Backend.RID, _exceptions);
    }
    internal void Validate(PhysicsSpace space)
    {
        PhysicsReplayCopy.Require(Backend.Space == space && Backend.AttachmentVersion == _attachment && Settings() == _configuration && ShapeCount == _shapeCount);
        PhysicsReplayCopy.Require(PhysicsServer.Service.ReplayExceptionsMatch(Backend.RID, _exceptions));
        if (Scene is { } scene)
        {
            PhysicsReplayCopy.Require(!scene.IsDisposed && scene.Parent == _parent && scene.Tree == _tree);
            if (scene.GetParentItem() is { } parent) PhysicsReplayCopy.Require(Math.Abs(parent.GetGlobalTransform().Determinant()) > 0);
        }
        for (var i = 0; i < _shapeCount; i++) PhysicsReplayCopy.Require(ShapeAt(i) == _shapes[i]);
        if (Scene is PhysicsBody body) body.ValidateReplay(_body!);
        if (Scene is RigidBody rigid) rigid.ValidateReplay(_rigid!);
        if (Scene is AnimatableBody moving) moving.ValidateReplay(_animatable!);
        if (Scene is CharacterBody character) character.ValidateReplay(_character!);
        if (Scene is Area area) area.ValidateReplay(_area!);
        _runtime?.ValidateReplay(_bodyRuntime!);
    }
    internal void Restore()
    {
        Backend.RestoreReplay(_backend);
        if (Scene is PhysicsBody body) body.RestoreReplay(_body!);
        if (Scene is RigidBody rigid) rigid.RestoreReplay(_rigid!);
        if (Scene is StaticBody surface) surface.RestoreReplay(_surface!);
        if (Scene is AnimatableBody moving) moving.RestoreReplay(_animatable!);
        if (Scene is CharacterBody character) character.RestoreReplay(_character!);
        if (Scene is Area area) area.RestoreReplay(_area!);
        if (Server is not null) Server.RestoreReplay(_server);
        _runtime?.RestoreReplay(_bodyRuntime!);
        if (_areaRuntime is { } runtime) { _areaPairs.CopyTo(runtime.Pairs); runtime.Changes.Clear(); }
    }
    internal void RestorePose() => Scene?.RestorePhysicsPose(_pose);
}
