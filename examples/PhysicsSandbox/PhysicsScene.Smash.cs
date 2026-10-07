namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class PhysicsScene
{
    internal const int SmashDefaultCount = 9600, SmashMaximumCount = 65536;
    internal const float SmashScale = 20;
    internal static readonly Color SmashSleepingColor = Muted.Lerp(Paper, .4f);
    internal sealed class SmashFragment(RID rid, int number, Transform pose)
    {
        internal readonly RID RID = rid;
        internal readonly int Number = number;
        internal Transform Pose = pose;
        internal PhysicsDirectBodyState? State;
        internal bool Frozen, Kinematic, Locked, IsDisposed;
        internal bool Sleeping = true;
        internal int Policy;
    }
    private readonly List<SmashFragment> _smashPieces = [];
    private readonly Dictionary<RID, SmashFragment> _smashLookup = [];
    private readonly List<RectangleShape> _smashShapes = [];
    private SmashFragment? _selectedFragment;
    private RigidBody? _smashBlock;
    private PhysicsDirectBodyState? _smashBlockState;
    private readonly RID[] _smashContacts = new RID[8];
    private int _smashContactCount, _smashContactStep, _smashCapturedStep = -1;
    private bool[] _smashMoved = [];
    private Vector2[] _smashPrevious = [];
    private float[] _smashAngles = [], _smashCurrentAngles = [], _smashInstances = [];
    private Color[] _smashPalette = [];
    private MultiMesh? _smashMesh;
    private int _smashColumns, _smashRows;
    private float _smashSize, _smashStep = 1f / 60, _smashSpeed = 600 * SmashScale;
    private bool _smashAttached, _smashDirty = true;
    internal int SmashFragmentCount => _smashPieces.Count;
    internal IReadOnlyList<SmashFragment> SmashFragments => _smashPieces;
    internal RID SelectedRID => _selectedFragment?.RID ?? SelectedBody?.GetRID() ?? default;
    internal bool HasSelection => SelectedRID.IsValid();
    internal bool SelectedIsRigid => _selectedFragment is not null || SelectedBody is RigidBody;

    internal bool ContainsBody(RID rid)
    {
        if (_smashLookup.ContainsKey(rid)) return true;
        foreach (var body in Colliders) if (!body.IsDisposed && body.GetRID() == rid) return true;
        return false;
    }
    internal void RefreshSmash() => _smashDirty = true;

    private void BuildSmash()
    {
        _smashMoved = new bool[SmashMaximumCount]; _smashPrevious = new Vector2[SmashMaximumCount];
        _smashAngles = new float[SmashMaximumCount]; _smashCurrentAngles = new float[SmashMaximumCount];
        _smashPalette = new Color[SmashMaximumCount];
        var quad = Own(new ArrayMesh());
        quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
        {
            Vertices = [new(-.5f, -.5f), new(.5f, -.5f), new(.5f, .5f), new(-.5f, .5f)],
            Indices = [0, 1, 2, 0, 2, 3]
        });
        _smashMesh = Own(new MultiMesh { Mesh = quad, UseColors = true });
        _worldGravity = _worldAngularDamp = 0; _worldLinearDamp = .02f;
        Enclose();
        _smashBlock = Place(new RigidBody { Mass = 12, LinearDamp = 0, AngularDamp = 0 }, Box(64 * SmashScale, 64 * SmashScale), new Vector2(230, 361) * SmashScale, Pink);
        _smashBlock.PhysicsMaterialOverride = SurfaceMaterial(.05f, .15f);
        _smashBlock.ContactMonitor = false;
        _storyVisual.Draw += DrawSmash;
        SetSmashPopulation(SmashDefaultCount);
        ResetSmash(true);
        Actions = ["Launch block [B]", "Shockwave [N]", "Rebuild wall [F]"];
        Help = "B: launch · N: shockwave · F: rebuild · drag the block or fragments · tune impact speed and fragment count";
        Observation = "A heavy block wakes and scatters a sleeping wall in zero gravity. All fragments use real rigid-body contacts.";
    }

    internal void SetSmashPopulation(int count)
    {
        count = Math.Clamp(count, 64, SmashMaximumCount);
        if (_smashMesh!.InstanceCount != count) { _smashMesh.InstanceCount = count; _smashInstances = new float[count * 12]; }
        _smashColumns = (int)MathF.Ceiling(MathF.Sqrt(count * 1.5f));
        _smashRows = (count + _smashColumns - 1) / _smashColumns;
        _smashSize = MathF.Min(2.4f, 360f / _smashColumns * .85f) * SmashScale;
        foreach (var shape in _smashShapes) shape.Size = new(_smashSize, _smashSize);
        while (_smashPieces.Count < count)
        {
            var i = _smashPieces.Count; var group = i / 128;
            if (group == _smashShapes.Count) _smashShapes.Add((RectangleShape)Box(_smashSize, _smashSize));
            var rid = PhysicsServer.BodyCreate();
            var fragment = new SmashFragment(rid, i + 6, new Transform(0, SmashHome(i))) { Sleeping = WorldGravity == 0 };
            _smashPieces.Add(fragment); _smashLookup.Add(rid, fragment);
            PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodyAddShape(rid, _smashShapes[group].GetRID());
            PhysicsServer.BodySetMass(rid, .0045f); PhysicsServer.BodySetFriction(rid, .05f); PhysicsServer.BodySetBounce(rid, .1f);
            PhysicsServer.BodySetLinearDamp(rid, 0); PhysicsServer.BodySetAngularDamp(rid, .05f);
            PhysicsServer.BodySetTransform(rid, fragment.Pose);
            if (_smashAttached) AttachSmashFragment(fragment);
        }
        while (_smashPieces.Count > count)
        {
            var fragment = _smashPieces[^1]; _smashPieces.RemoveAt(_smashPieces.Count - 1);
            _smashLookup.Remove(fragment.RID); fragment.State?.Dispose(); fragment.State = null;
            PhysicsServer.FreeRID(fragment.RID); fragment.IsDisposed = true;
        }
        if (_selectedFragment is { IsDisposed: true }) { _selectedFragment = null; _grab = default; SelectionRevision++; }
        ResetSmash(false);
    }

    private Vector2 SmashHome(int index) => new(625 * SmashScale + (index % _smashColumns - (_smashColumns - 1) * .5f) * (_smashSize / .85f), 361 * SmashScale + (index / _smashColumns - (_smashRows - 1) * .5f) * (_smashSize / .85f));

    private void AttachSmashFragment(SmashFragment fragment)
    {
        PhysicsServer.BodySetSpace(fragment.RID, _worldSpace);
        fragment.State = PhysicsServer.BodyGetDirectState(fragment.RID)!;
        fragment.State.Sleeping = fragment.Sleeping;
    }
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what != NotificationPostEnterTree || Index != 11) return;
        _smashAttached = true;
        foreach (var fragment in _smashPieces) AttachSmashFragment(fragment);
        _smashBlockState = PhysicsServer.BodyGetDirectState(_smashBlock!.GetRID());
        _smashDirty = true;
    }
    private void DetachSmash()
    {
        _smashAttached = false; _smashBlockState = null;
        for (var i = _smashPieces.Count - 1; i >= 0; i--)
        {
            var fragment = _smashPieces[i];
            if (fragment.IsDisposed || fragment.State is null) continue;
            fragment.Pose = fragment.State.Transform; fragment.Sleeping = fragment.State.Sleeping; fragment.State.Dispose(); fragment.State = null;
            PhysicsServer.BodySetSpace(fragment.RID, default);
        }
    }
    private void DisposeSmash()
    {
        for (var i = _smashPieces.Count - 1; i >= 0; i--)
        {
            var fragment = _smashPieces[i]; fragment.State?.Dispose(); fragment.State = null;
            PhysicsServer.FreeRID(fragment.RID); fragment.IsDisposed = true;
        }
        _smashPieces.Clear(); _smashLookup.Clear(); _selectedFragment = null;
    }

    private void ResetSmash(bool launch)
    {
        ReleaseGrab(false); _flashes.Clear(); Array.Clear(_smashMoved); Score = 0;
        _smashContactCount = 0; _smashContactStep = PhysicsSteps;
        for (var i = 0; i < _smashPieces.Count; i++)
        {
            var fragment = _smashPieces[i]; fragment.Frozen = false; fragment.Policy = 0; fragment.Sleeping = WorldGravity == 0;
            PhysicsServer.BodySetMode(fragment.RID, fragment.Locked ? PhysicsServer.BodyMode.RigidLinear : PhysicsServer.BodyMode.Rigid);
            fragment.Pose = new(0, SmashHome(i)); PhysicsServer.BodySetTransform(fragment.RID, fragment.Pose);
            if (_smashAttached && fragment.State is null) AttachSmashFragment(fragment);
            PhysicsServer.BodySetLinearVelocity(fragment.RID, Vector2.Zero);
            if (fragment.State is { } state) { state.AngularVelocity = 0; state.Sleeping = fragment.Sleeping; }
            _smashPrevious[i] = fragment.Pose.Origin; _smashAngles[i] = _smashCurrentAngles[i] = 0;
            _smashPalette[i] = SmashSleepingColor;
        }
        _smashDirty = true;
        if (_smashBlock is not { } block) return;
        block.Freeze = false; block.Position = new Vector2(230, 361) * SmashScale; block.Rotation = 0;
        block.LinearVelocity = launch ? new(_smashSpeed, 0) : Vector2.Zero; block.AngularVelocity = 0; block.Sleeping = !launch; block.QueueRedraw();
    }

    internal Color GetSmashColor(RigidBody body)
    {
        if (body.Freeze) return body.FreezeMode == RigidFreezeMode.Static ? Blush : Berry;
        if (body.Sleeping) return SmashSleepingColor;
        return SmashMotionColor(body.LinearVelocity, body.AngularVelocity, 64 * SmashScale);
    }
    internal Color GetSmashColor(SmashFragment fragment)
    {
        if (fragment.Frozen || fragment.Policy == 2) return fragment.Kinematic && fragment.Policy != 2 ? Berry : Blush;
        if (fragment.State is not { } state) return _smashPalette[fragment.Number - 6];
        if (state.Sleeping) return SmashSleepingColor;
        return SmashMotionColor(state.LinearVelocity, state.AngularVelocity, _smashSize);
    }
    private Color SmashMotionColor(Vector2 velocity, float angular, float size) =>
        (velocity.Length() + MathF.Abs(angular) * size * .7071068f) * _smashStep > size * .25f ? Apricot : Pink;

    // Called once after simulation by idle processing, or explicitly by the headless frame host.
    internal void CaptureSmashState()
    {
        if (Index != 11 || !_smashAttached || !_smashDirty && _smashCapturedStep == PhysicsSteps) return;
        var advanced = _smashCapturedStep != PhysicsSteps;
        for (var i = 0; i < _smashPieces.Count; i++)
        {
            var fragment = _smashPieces[i];
            if (advanced) { _smashPrevious[i] = fragment.Pose.Origin; _smashAngles[i] = _smashCurrentAngles[i]; }
            if (fragment.State is { } state) fragment.Pose = state.Transform;
            _smashCurrentAngles[i] = fragment.Pose.Rotation;
            _smashPalette[i] = GetSmashColor(fragment);
            if (!_smashMoved[i] && fragment.Pose.Origin.DistanceSquaredTo(SmashHome(i)) > 28 * 28 * SmashScale * SmashScale) { _smashMoved[i] = true; Score++; }
        }
        if (_smashBlockState is { } block && _smashContactStep != PhysicsSteps)
        {
            Span<RID> current = stackalloc RID[8]; var count = 0;
            for (var i = 0; i < Math.Min(8, block.GetContactCount()); i++)
            {
                var rid = block.GetContactCollider(i);
                if (current[..count].Contains(rid)) continue;
                if (!_smashContacts.AsSpan(0, _smashContactCount).Contains(rid)) ContactEvents++;
                current[count++] = rid;
            }
            current[..count].CopyTo(_smashContacts); _smashContactCount = count; _smashContactStep = PhysicsSteps;
        }
        _smashCapturedStep = PhysicsSteps; _smashDirty = false;
    }

    private void DrawSmash(CanvasItem canvas)
    {
        CaptureSmashState();
        var count = _smashPieces.Count;
        var fraction = Running ? (float)Engine.PhysicsInterpolationFraction : 1f;
        var visualSize = _smashSize / .85f + .5f / PresentationZoom;
        var minimum = new Vector2(float.MaxValue, float.MaxValue); var maximum = -minimum;
        for (var i = 0; i < count; i++)
        {
            var center = _smashPrevious[i].Lerp(_smashPieces[i].Pose.Origin, fraction);
            var axis = Vector2.FromAngle(Mathf.LerpAngle(_smashAngles[i], _smashCurrentAngles[i], fraction)) * visualSize;
            var color = _smashPalette[i]; var record = _smashInstances.AsSpan(i * 12, 12);
            record[0] = axis.X; record[1] = -axis.Y; record[3] = center.X;
            record[4] = axis.Y; record[5] = axis.X; record[7] = center.Y;
            record[8] = color.R; record[9] = color.G; record[10] = color.B; record[11] = color.A;
            minimum = minimum.Min(center); maximum = maximum.Max(center);
        }
        _smashMesh!.Buffer = _smashInstances;
        _smashMesh.CustomAABB = new Rect2(minimum, maximum - minimum).Grow(visualSize);
        canvas.DrawMultiMesh(_smashMesh);
    }
    private void ActSmash(int action)
    {
        if (action != 1) { ResetSmash(action == 0); return; }
        foreach (var fragment in _smashPieces)
            PhysicsServer.BodyApplyCentralImpulse(fragment.RID, (fragment.Pose.Origin - new Vector2(625, 361) * SmashScale).Normalized() * (PhysicsServer.BodyGetMass(fragment.RID) * 260 * SmashScale));
        _smashDirty = true;
    }

    private void ApplySmashMode(SmashFragment fragment)
    {
        if (fragment.Policy == 1)
        {
            if (fragment.State is { } state) { fragment.Pose = state.Transform; fragment.Sleeping = state.Sleeping; state.Dispose(); fragment.State = null; }
            PhysicsServer.BodySetSpace(fragment.RID, default);
        }
        else
        {
            var mode = fragment.Policy == 2 || fragment.Frozen && !fragment.Kinematic ? PhysicsServer.BodyMode.Static :
                fragment.Frozen ? PhysicsServer.BodyMode.Kinematic : fragment.Locked ? PhysicsServer.BodyMode.RigidLinear : PhysicsServer.BodyMode.Rigid;
            PhysicsServer.BodySetMode(fragment.RID, mode);
            if (_smashAttached && fragment.State is null) AttachSmashFragment(fragment);
        }
        _smashDirty = true;
    }
    private bool SmashKey(Key key)
    {
        if (_selectedFragment is not { } fragment) return false;
        switch (key)
        {
            case Key.K: fragment.Frozen = !fragment.Frozen; ApplySmashMode(fragment); break;
            case Key.H: fragment.Kinematic = !fragment.Kinematic; fragment.Frozen = true; ApplySmashMode(fragment); break;
            case Key.Z: if (fragment.State is { } state) state.Sleeping = !state.Sleeping; break;
            case Key.L: fragment.Locked = !fragment.Locked; ApplySmashMode(fragment); break;
            case Key.C:
                PhysicsServer.BodySetConstantForce(fragment.RID, PhysicsServer.BodyGetConstantForce(fragment.RID).IsZeroApprox() ? new(0, -1300 * PhysicsServer.BodyGetMass(fragment.RID)) : Vector2.Zero); break;
            case Key.V: ReleaseGrab(); fragment.Policy = (fragment.Policy + 1) % 4; ApplySmashMode(fragment); break;
            default: return false;
        }
        _smashDirty = true; return true;
    }
}
