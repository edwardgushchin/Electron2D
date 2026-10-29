namespace Electron2D;

/// <summary>Contains the leading and trailing caret shapes at a logical scalar position.</summary>
/// <remarks>Rectangles are baseline-relative. Signed advance sizes preserve direction; a missing side has a zero rectangle and Auto direction.</remarks>
public readonly struct TextCaretInfo
{
    /// <summary>Creates a copied pair of baseline-relative caret shapes.</summary>
    /// <param name="leadingRect">Finite signed preceding-cluster rectangle; zero for an absent side.</param>
    /// <param name="leadingDirection">Preceding cluster direction, or Auto.</param>
    /// <param name="trailingRect">Finite signed following-cluster rectangle; zero for an absent side.</param>
    /// <param name="trailingDirection">Following cluster direction, or Auto.</param>
    /// <exception cref="ArgumentException">A rectangle contains nonfinite geometry.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A direction is not Auto, LTR or RTL.</exception>
    public TextCaretInfo(Rect2 leadingRect, TextDirection leadingDirection, Rect2 trailingRect, TextDirection trailingDirection)
    {
        if (!leadingRect.Position.IsFinite() || !leadingRect.Size.IsFinite() || !leadingRect.End.IsFinite() || !trailingRect.Position.IsFinite() || !trailingRect.Size.IsFinite() || !trailingRect.End.IsFinite())
            throw new ArgumentException("Caret rectangles must have finite geometry.");
        if (leadingDirection is < TextDirection.Auto or > TextDirection.RTL) throw new ArgumentOutOfRangeException(nameof(leadingDirection));
        if (trailingDirection is < TextDirection.Auto or > TextDirection.RTL) throw new ArgumentOutOfRangeException(nameof(trailingDirection));
        LeadingRect = leadingRect; LeadingDirection = leadingDirection; TrailingRect = trailingRect; TrailingDirection = trailingDirection;
    }
    /// <summary>Gets the lower or right half-caret after the preceding shaped cluster, or its full edge shape.</summary><value>The leading rectangle.</value>
    public Rect2 LeadingRect { get; }
    /// <summary>Gets the preceding cluster direction, or Auto inside a cluster or when absent.</summary><value>The leading direction.</value>
    public TextDirection LeadingDirection { get; }
    /// <summary>Gets the upper or left half-caret before the following shaped cluster, or its full edge shape.</summary><value>The trailing rectangle.</value>
    public Rect2 TrailingRect { get; }
    /// <summary>Gets the following cluster direction, or Auto inside a cluster or when absent.</summary><value>The trailing direction.</value>
    public TextDirection TrailingDirection { get; }
}
