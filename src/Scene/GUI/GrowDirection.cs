namespace Electron2D;

/// <summary>Chooses the edge that remains fixed when a minimum size enlarges a control.</summary>
public enum GrowDirection
{
    /// <summary>Keep the trailing edge fixed.</summary>
    Begin = 0,
    /// <summary>Keep the leading edge fixed.</summary>
    End = 1,
    /// <summary>Move both edges equally around the center.</summary>
    Both = 2
}
