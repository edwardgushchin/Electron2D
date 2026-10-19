using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct CachedContact(RID A, RID B, int First, int Second, Vector2 LocalA, Vector2 LocalB,
        Vector2 Normal, Vector2 Impulse, float Interval, float MassA, float MassB, float InertiaA, float InertiaB);

    private sealed partial class Solver
    {
        internal List<CachedContact> CachedContacts = [];
        private List<CachedContact> _nextContacts = [];
        internal void ForgetContactHistory(RID body)
        {
            for (var i = CachedContacts.Count - 1; i >= 0; i--)
                if (CachedContacts[i].A == body || CachedContacts[i].B == body) CachedContacts.RemoveAt(i);
        }
        private void WarmContacts(float interval)
        {
            if (_contactSettings.RecycleRadius == 0) return;
            var radiusSquared = _contactSettings.RecycleRadius * _contactSettings.RecycleRadius;
            foreach (ref var contact in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_constraints))
            {
                var a = contact.A; var b = contact.B;
                if (!ContactActive(a, b)) continue;
                var ma = (a.Mask & b.Layer) != 0 ? a.InvMass : 0; var mb = (b.Mask & a.Layer) != 0 ? b.InvMass : 0;
                var ia = (a.Mask & b.Layer) != 0 ? a.InvInertia : 0; var ib = (b.Mask & a.Layer) != 0 ? b.InvInertia : 0;
                foreach (var old in CachedContacts)
                {
                    if (old.A != a.RID || old.B != b.RID || old.First != contact.First || old.Second != contact.Second ||
                        old.MassA != ma || old.MassB != mb || old.InertiaA != ia || old.InertiaB != ib ||
                        old.Normal.Dot(contact.Normal) < .98f || !(old.Interval > 0) ||
                        old.LocalA.DistanceSquaredTo(contact.LocalA) >= radiusSquared || old.LocalB.DistanceSquaredTo(contact.LocalB) >= radiusSquared) continue;
                    var separation = b.Pose * old.LocalB - a.Pose * old.LocalA;
                    var tangent = new Vector2(-contact.Normal.Y, contact.Normal.X);
                    if (separation.Dot(contact.Normal) > _contactSettings.MaxSeparation ||
                        MathF.Abs(separation.Dot(tangent)) > _contactSettings.MaxSeparation) continue;
                    var scale = MathF.Min(2, interval / old.Interval);
                    contact.NormalImpulse = MathF.Max(0, old.Impulse.Dot(contact.Normal) * scale);
                    var cap = contact.NormalImpulse * MathF.Abs(MathF.Min(a.Friction, b.Friction));
                    contact.TangentImpulse = Math.Clamp(old.Impulse.Dot(tangent) * scale, -cap, cap);
                    var impulse = contact.Impulse;
                    if (ma != 0 || ia != 0) a.AddImpulse(-impulse, -Cross(contact.Point - a.Center, impulse));
                    if (mb != 0 || ib != 0) b.AddImpulse(impulse, Cross(contact.Point - b.Center, impulse));
                    ReusedContactPoints++; break;
                }
            }
        }
        private void SaveContactHistory(float interval)
        {
            _nextContacts.Clear();
            foreach (var contact in _constraints)
            {
                var a = contact.A; var b = contact.B;
                _nextContacts.Add(new(a.RID, b.RID, contact.First, contact.Second, contact.LocalA, contact.LocalB,
                    contact.Normal, contact.Impulse, interval, (a.Mask & b.Layer) != 0 ? a.InvMass : 0, (b.Mask & a.Layer) != 0 ? b.InvMass : 0,
                    (a.Mask & b.Layer) != 0 ? a.InvInertia : 0, (b.Mask & a.Layer) != 0 ? b.InvInertia : 0));
            }
            (CachedContacts, _nextContacts) = (_nextContacts, CachedContacts);
            _nextContacts.Clear();
        }
    }
}
