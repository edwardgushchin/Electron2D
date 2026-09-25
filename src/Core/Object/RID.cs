namespace Electron2D;

/// <summary>An opaque identity for a resource owned by an engine server.</summary>
/// <remarks>Zero is empty. A nonzero value only indicates that an identity was assigned;
/// the owning server separately checks its resource kind and whether it is still alive.</remarks>
public readonly struct RID : IEquatable<RID>, IComparable<RID>
{
    private static long _lastID;
    private readonly long _id;

    /// <summary>Creates an empty identity.</summary>
    public RID() { }

    /// <summary>Copies another resource identity.</summary>
    /// <param name="from">The identity to copy.</param>
    public RID(RID from) => _id = from._id;

    private RID(long id) => _id = id;

    /// <summary>Gets the session-local numeric identity.</summary>
    /// <returns>Zero for an empty RID; otherwise a positive value never reassigned in this session.</returns>
    public long GetID() => _id;

    /// <summary>Tests whether this value is nonempty.</summary>
    /// <returns>True for a nonzero identity, even after its server resource is freed.</returns>
    public bool IsValid() => _id != 0;

    /// <inheritdoc />
    public bool Equals(RID other) => _id == other._id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RID other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _id.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(RID other) => _id.CompareTo(other._id);

    /// <summary>Tests whether two values identify the same resource.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when both numeric identities match.</returns>
    public static bool operator ==(RID left, RID right) => left.Equals(right);

    /// <summary>Tests whether two values identify different resources.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when the numeric identities differ.</returns>
    public static bool operator !=(RID left, RID right) => !left.Equals(right);

    /// <summary>Orders two identities by their session-local numeric values.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when left precedes right.</returns>
    public static bool operator <(RID left, RID right) => left._id < right._id;

    /// <summary>Orders two identities by their session-local numeric values.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when left precedes or equals right.</returns>
    public static bool operator <=(RID left, RID right) => left._id <= right._id;

    /// <summary>Orders two identities by their session-local numeric values.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when left follows right.</returns>
    public static bool operator >(RID left, RID right) => left._id > right._id;

    /// <summary>Orders two identities by their session-local numeric values.</summary>
    /// <param name="left">First identity.</param>
    /// <param name="right">Second identity.</param>
    /// <returns>True when left follows or equals right.</returns>
    public static bool operator >=(RID left, RID right) => left._id >= right._id;

    /// <inheritdoc />
    public override string ToString() => $"RID({_id})";

    internal static RID Allocate()
    {
        while (true)
        {
            var last = Volatile.Read(ref _lastID);
            if (last == long.MaxValue) throw new InvalidOperationException("RID identity range is exhausted.");
            if (Interlocked.CompareExchange(ref _lastID, last + 1, last) == last) return new(last + 1);
        }
    }
}
