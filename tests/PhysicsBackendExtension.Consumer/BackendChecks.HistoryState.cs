using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct BodyHistory(Body Body, Vector2 Position, float Rotation, Vector2 Velocity, float Angular,
        Vector2 Force, float Torque, bool Awake, bool CanSleep, float SleepTime, bool Locked, bool Omit,
        Vector2 Surface, float SurfaceAngular, float Friction, float Bounce, PhysicsBodyFields Fields, Transform Saved,
        int ContactStart, int ContactCount, int ContactLimit, float ContactDepthThreshold);

    private sealed partial class Body
    {
        internal BodyHistory CaptureHistory(int contactStart) => new(this, Position, Rotation, Velocity, Angular, Force, Torque, Awake,
            CanSleep, SleepTime, Locked, Omit, Surface, SurfaceAngular, Friction, Bounce, _fields, _saved,
            contactStart, Contacts.Count, ReportedLimit, ReportedDepth);
        internal void RestoreHistory(in BodyHistory state)
        {
            Position = state.Position; Rotation = state.Rotation; Velocity = state.Velocity; Angular = state.Angular;
            Force = state.Force; Torque = state.Torque; Awake = state.Awake; CanSleep = state.CanSleep; SleepTime = state.SleepTime;
            Locked = state.Locked; Omit = state.Omit; Surface = state.Surface; SurfaceAngular = state.SurfaceAngular;
            Friction = state.Friction; Bounce = state.Bounce;
            _fields = state.Fields; _saved = state.Saved;
            ReportedLimit = state.ContactLimit; ReportedDepth = state.ContactDepthThreshold;
        }
    }

    private sealed class History(Solver solver) : PhysicsBackendCheckpoint
    {
        private Solver? _owner = solver;
        private Solver Owner => _owner ?? throw new ObjectDisposedException(nameof(History));
        private BodyHistory[] _states = [];
        private PhysicsBackendOneWay[] _oneWays = [];
        private Vector2[] _debug = [];
        private FrameContact[] _contacts = [];
        private int _contactCount;
        private int _count, _oneWayCount, _debugCount, _pairs;
        private float _delta;
        private bool _ready;
        protected override long DeviceCapacityBytes { get { _ = Owner; return 0; } }
        protected override void Capture()
        {
            var owner = Owner;
            if (_states.Length < owner.Bodies.Count) Array.Resize(ref _states, owner.Bodies.Count);
            var total = 0; foreach (var body in owner.Bodies.Values) total += body.Contacts.Count;
            if (_contacts.Length < total) Array.Resize(ref _contacts, total);
            var i = 0; var offset = 0;
            foreach (var body in owner.Bodies.Values)
            {
                _states[i++] = body.CaptureHistory(offset); body.Contacts.CopyTo(_contacts, offset); offset += body.Contacts.Count;
            }
            if (_contactCount > total) Array.Clear(_contacts, total, _contactCount - total);
            _contactCount = total;
            Array.Clear(_states, i, _count > i ? _count - i : 0); _count = i;
            _oneWayCount = owner.OneWays.Count;
            if (_oneWays.Length < _oneWayCount) Array.Resize(ref _oneWays, _oneWayCount);
            owner.OneWays.CopyTo(_oneWays);
            _debugCount = owner.DebugPointCount;
            if (_debug.Length < _debugCount) Array.Resize(ref _debug, _debugCount);
            owner.DebugPoints.AsSpan(0, _debugCount).CopyTo(_debug);
            _delta = owner.Delta; _pairs = owner.Pairs; _ready = owner.Ready;
        }
        protected override void ValidateRestore()
        {
            var owner = Owner;
            if (owner.Bodies.Count != _count) throw new InvalidOperationException("Independent history topology changed.");
            for (var i = 0; i < _count; i++)
            {
                var body = _states[i].Body;
                if (body.Detached || !owner.Bodies.TryGetValue(body.RID, out var current) || current != body)
                    throw new InvalidOperationException("Independent history attachment expired.");
                body.Contacts.EnsureCapacity(_states[i].ContactCount);
            }
            owner.OneWays.EnsureCapacity(_oneWayCount);
        }
        protected override void Restore()
        {
            ValidateRestore(); var owner = Owner;
            for (var i = 0; i < _count; i++)
            {
                var state = _states[i]; var body = state.Body; body.RestoreHistory(state); body.Contacts.Clear();
                for (var j = 0; j < state.ContactCount; j++) body.Contacts.Add(_contacts[state.ContactStart + j]);
            }
            owner.OneWays.Clear(); for (var i = 0; i < _oneWayCount; i++) owner.OneWays.Add(_oneWays[i]);
            owner.DebugPointCount = Math.Min(_debugCount, owner.DebugLimit);
            _debug.AsSpan(0, owner.DebugPointCount).CopyTo(owner.DebugPoints);
            owner.Delta = _delta; owner.Pairs = _pairs; owner.Ready = _ready;
            owner.RestoreExecutionHook?.Invoke();
        }
        public override void Dispose()
        {
            var fail = _owner?.FailHistoryRelease ?? false;
            _owner = null; _states = []; _oneWays = []; _debug = []; _contacts = []; _count = _oneWayCount = _debugCount = _contactCount = 0;
            if (fail) throw new InvalidOperationException("Expected independent history release failure.");
        }
    }
}
