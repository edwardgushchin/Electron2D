using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Constraint
    {
        internal bool Connects(Body a, Body b) => _attached && (a.RID == _first && b.RID == _second || a.RID == _second && b.RID == _first);
    }

    private sealed partial class Solver
    {
        private readonly List<Body> _sleepGroup = [];
        private readonly List<Body> _wakeGroup = [];
        private bool Connected(Body a, Body b)
        {
            foreach (var joint in Joints.Values) if (joint.Connects(a, b)) return true;
            return a.Shapes > 0 && b.Shapes > 0 && ((a.Mask & b.Layer) != 0 || (b.Mask & a.Layer) != 0) &&
                !Exceptions.Contains((a.RID, b.RID)) && !Exceptions.Contains((b.RID, a.RID)) &&
                AllowsContact(a.RID, b.RID) && Contact(a, b, out _, out _, out _);
        }
        private void Group(Body seed, List<Body> group)
        {
            group.Clear(); group.Add(seed); seed.SleepVisited = true;
            for (var i = 0; i < group.Count; i++)
                foreach (var other in Bodies.Values)
                    if (other.Dynamic && !other.SleepVisited && Connected(group[i], other)) { other.SleepVisited = true; group.Add(other); }
        }
        internal void WakeConnected(Body source)
        {
            foreach (var body in Bodies.Values) body.SleepVisited = false;
            Group(source, _wakeGroup);
            foreach (var body in _wakeGroup) if (body.Dynamic) { body.Awake = true; body.SleepTime = 0; }
            _wakeGroup.Clear();
        }
        private void UpdateSleep(float delta)
        {
            foreach (var body in Bodies.Values) body.SleepVisited = false;
            foreach (var seed in Bodies.Values)
            {
                if (!seed.Dynamic || seed.SleepVisited) continue;
                Group(seed, _sleepGroup);
                var active = false; var quiet = _sleepSettings.LinearThreshold > 0 && _sleepSettings.AngularThreshold > 0;
                foreach (var body in _sleepGroup)
                {
                    active |= body.Awake;
                    quiet &= body.CanSleep && body.Velocity.Length() < _sleepSettings.LinearThreshold &&
                        MathF.Abs(body.Angular) < _sleepSettings.AngularThreshold;
                }
                if (!active) continue;
                if (!quiet)
                {
                    foreach (var body in _sleepGroup) { body.Awake = true; body.SleepTime = 0; }
                    continue;
                }
                var minimum = float.PositiveInfinity;
                foreach (var body in _sleepGroup)
                {
                    if (!body.Awake) { body.Awake = true; body.SleepTime = 0; }
                    body.SleepTime += delta;
                    minimum = MathF.Min(minimum, body.SleepTime);
                }
                if (minimum <= _sleepSettings.TimeToSleep) continue;
                foreach (var body in _sleepGroup) { body.Awake = false; body.Velocity = Vector2.Zero; body.Angular = 0; }
            }
            _sleepGroup.Clear();
        }
    }
}
