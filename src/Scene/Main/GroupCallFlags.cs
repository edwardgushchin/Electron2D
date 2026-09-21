namespace Electron2D;

/// <summary>Controls ordering and scheduling for typed scene-group operations.</summary>
/// <remarks>
/// Public group methods reject unknown bits and require <see cref="Unique"/> to be combined with
/// <see cref="Deferred"/>. Unique identity is the operation kind, group, and delegate or notification identifier;
/// later setter values and other supported flags do not replace the first accepted operation.
/// </remarks>
[Flags]
public enum GroupCallFlags
{
    /// <summary>Executes immediately in hierarchy order.</summary>
    Default = 0,

    /// <summary>Visits descendants before their ancestors by reversing hierarchy order.</summary>
    Reverse = 1,

    /// <summary>Queues the operation for a future deferred flush instead of executing it immediately.</summary>
    Deferred = 2,

    /// <summary>
    /// Coalesces equal deferred operations until their queued callback starts. This value requires
    /// <see cref="Deferred"/>; the first operation's captured arguments are retained.
    /// </summary>
    Unique = 4
}
