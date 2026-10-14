using Electron2D;
using System.Runtime.InteropServices;

public static partial class BackendChecks
{
    private struct ContactConstraint(Body a, Body b, int first, int second, Vector2 normal, Vector2 point, float depth, float residual, float bounce)
    {
        internal Body A = a, B = b;
        internal int First = first, Second = second;
        internal Vector2 Normal = normal, Point = point;
        internal float Depth = depth, Residual = residual, Bounce = bounce, NormalImpulse, TangentImpulse;
        internal Vector2 Impulse => Normal * NormalImpulse + new Vector2(-Normal.Y, Normal.X) * TangentImpulse;
    }
    private sealed partial class Solver
    {
        private readonly List<ContactConstraint> _constraints = [];
        private void SolveConstraints(float interval, bool begin, bool first = true)
        {
            if (begin) foreach (var joint in Joints.Values) joint.BeginSolve();
            try
            {
                PrepareContacts(interval);
                if (_constraints.Count != 0 || Joints.Count != 0)
                    for (var iteration = 0; iteration < Iterations; iteration++)
                    {
                        foreach (ref var contact in CollectionsMarshal.AsSpan(_constraints)) SolveContact(ref contact);
                        foreach (var joint in Joints.Values) joint.Solve(Delta, first && iteration == 0);
                        foreach (ref var contact in CollectionsMarshal.AsSpan(_constraints)) CorrectContact(contact);
                    }
                foreach (var contact in _constraints)
                {
                    var a = contact.A; var b = contact.B; var own = a.Parts[contact.First]; var other = b.Parts[contact.Second];
                    a.RecordContact(b, own.Index, other.Index, contact.Point, -contact.Normal, -contact.Impulse, contact.Depth, contact.First, contact.Second);
                    b.RecordContact(a, other.Index, own.Index, contact.Point, contact.Normal, contact.Impulse, contact.Depth, contact.Second, contact.First);
                }
                if (FailSolve) throw new InvalidOperationException("Injected independent solver failure.");
            }
            finally { _constraints.Clear(); }
        }
        private void PrepareContacts(float interval)
        {
            _constraints.Clear();
            foreach (var a in Bodies.Values) foreach (var b in Bodies.Values)
                {
                    if (a.RID.GetID() >= b.RID.GetID() || a.IsArea || b.IsArea || a.Shapes == 0 || b.Shapes == 0 ||
                        (a.Mask & b.Layer) == 0 && (b.Mask & a.Layer) == 0 || Exceptions.Contains((a.RID, b.RID)) || Exceptions.Contains((b.RID, a.RID)) ||
                        a.Mode == PhysicsServer.BodyMode.Static && b.Mode == PhysicsServer.BodyMode.Static ||
                        a.InvMass + b.InvMass == 0 && a.ReportedLimit == 0 && b.ReportedLimit == 0) continue;
                    for (var i = 0; i < a.Shapes; i++) for (var j = 0; j < b.Shapes; j++)
                        {
                            var own = a.Parts[i]; var other = b.Parts[j];
                            if (!AllowsContact(a.RID, own.Index, b.RID, other.Index) ||
                                !PartContact(a, own, b, other, out var normal, out var depth, out var point)) continue;
                            var bias = own.Bias == 0 ? other.Bias == 0 ? _contactSettings.Bias : other.Bias :
                                other.Bias == 0 ? own.Bias : (own.Bias + other.Bias) * .5f;
                            var fraction = bias == 1 ? 1 : (float)(1 - Math.Pow(1 - bias, interval / Delta));
                            var residual = depth - MathF.Max(0, depth - _contactSettings.AllowedPenetration) * fraction;
                            var velocity = (b.PointVelocity(point) - a.PointVelocity(point)).Dot(normal);
                            _constraints.Add(new(a, b, i, j, normal, point, depth, residual, -Math.Clamp(a.Bounce + b.Bounce, 0, 1) * MathF.Min(0, velocity)));
                            if (depth > 0 && (a.Awake || b.Awake))
                            { var half = normal * (depth * .5f); AddDebugPoint(point - half); AddDebugPoint(point + half); }
                            if (_framePairs.Add((a.RID, i, b.RID, j))) Pairs++;
                        }
                }
        }
        private static bool ContactActive(Body a, Body b) => a.Dynamic && a.Awake || b.Dynamic && b.Awake ||
            a.Mode == PhysicsServer.BodyMode.Kinematic && (a.Velocity != Vector2.Zero || a.Angular != 0) ||
            b.Mode == PhysicsServer.BodyMode.Kinematic && (b.Velocity != Vector2.Zero || b.Angular != 0);
        private static void CorrectContact(in ContactConstraint contact)
        {
            var a = contact.A; var b = contact.B;
            if (!ContactActive(a, b) || !PartContact(a, a.Parts[contact.First], b, b.Parts[contact.Second], out var normal, out var depth, out _)) return;
            var excess = depth - contact.Residual; if (excess <= 0) return;
            var massA = (a.Mask & b.Layer) != 0 ? a.InvMass : 0; var massB = (b.Mask & a.Layer) != 0 ? b.InvMass : 0;
            var inverse = massA + massB; if (inverse == 0) return;
            a.Position -= normal * excess * (massA / inverse); b.Position += normal * excess * (massB / inverse);
        }
    }
}
