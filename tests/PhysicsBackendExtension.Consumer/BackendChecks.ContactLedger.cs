using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct FrameContact(RID Peer, int LocalShape, int PeerShape, Vector2 Point, Vector2 Normal, Vector2 Impulse, float Depth,
        Vector2 LocalVelocity = default, Vector2 PeerVelocity = default, int LocalPart = -1, int PeerPart = -1);

    private sealed partial class Body
    {
        internal readonly List<FrameContact> Contacts = [];
        internal float ReportedDepth;
        internal void RecordContact(Body peer, int localShape, int peerShape, Vector2 point, Vector2 normal, Vector2 impulse, float depth, int localPart, int peerPart)
        {
            if (ReportedLimit == 0) return;
            for (var i = 0; i < Contacts.Count; i++)
                if (Contacts[i].Peer == peer.RID && Contacts[i].LocalShape == localShape && Contacts[i].PeerShape == peerShape && Contacts[i].LocalPart == localPart && Contacts[i].PeerPart == peerPart)
                {
                    var old = Contacts[i]; Contacts[i] = new(peer.RID, localShape, peerShape, point, normal, old.Impulse + impulse, MathF.Max(old.Depth, depth), LocalPart: localPart, PeerPart: peerPart); return;
                }
            Contacts.Add(new(peer.RID, localShape, peerShape, point, normal, impulse, depth, LocalPart: localPart, PeerPart: peerPart));
        }
        internal void FreezeContacts()
        {
            for (var i = 0; i < Contacts.Count; i++)
            {
                var contact = Contacts[i]; var peer = solver.Bodies[contact.Peer];
                Contacts[i] = contact with { LocalVelocity = PointVelocity(contact.Point), PeerVelocity = peer.PointVelocity(contact.Point) };
            }
        }
        internal int CopyContacts(Span<PhysicsBodyContact> output)
        {
            var limit = Math.Min(output.Length, ReportedLimit); var count = 0;
            if (limit == 0) return 0;
            foreach (var contact in Contacts)
            {
                if (ReportedDepth > 0 && contact.Depth < ReportedDepth) continue;
                var slot = count;
                if (count == limit)
                {
                    slot = 0;
                    for (var i = 1; i < count; i++) if (output[i].Depth < output[slot].Depth) slot = i;
                    if (contact.Depth <= output[slot].Depth) continue;
                }
                else count++;
                output[slot] = new(rid, contact.LocalShape, contact.Peer, contact.PeerShape, contact.Point, contact.Point, contact.Normal,
                    contact.LocalVelocity, contact.PeerVelocity, contact.Impulse, contact.Depth);
            }
            return count;
        }
    }
}
