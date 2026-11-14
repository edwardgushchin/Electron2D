namespace Electron2D;

public partial class Control
{
    /// <summary>Names the endpoints of either axis of a control's parent area.</summary>
    public enum Anchor
    {
        /// <summary>The beginning of the axis, at fraction zero.</summary>
        Begin = 0,
        /// <summary>The end of the axis, at fraction one.</summary>
        End = 1
    }
}
