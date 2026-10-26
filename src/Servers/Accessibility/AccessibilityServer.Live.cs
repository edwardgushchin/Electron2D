namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Identifies the presentation priority of live region changes.</summary>
    public enum AccessibilityLiveMode
    {
        /// <summary>Does not announce updates.</summary>
        Off = 0,
        /// <summary>Announces updates at the next opportunity.</summary>
        Polite = 1,
        /// <summary>Announces updates with immediate priority.</summary>
        Assertive = 2,
    }
    /// <summary>Identifies the content opened by an element's popup action.</summary>
    public enum AccessibilityPopupType
    {
        /// <summary>Opens a menu.</summary>
        Menu = 0,
        /// <summary>Opens a list.</summary>
        List = 1,
        /// <summary>Opens a tree.</summary>
        Tree = 2,
        /// <summary>Opens a dialog.</summary>
        Dialog = 3,
    }
    /// <summary>Publishes the priority for announcements of this live region's content changes.</summary>
    /// <param name="id">Live element.</param><param name="mode">Typed presentation priority.</param>
    /// <remarks>Updates coalesce until scene reconciliation or the native owner pump completes. Off suppresses pending announcements.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is unknown.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetLive(RID id, AccessibilityLiveMode mode)
    {
        if (mode is < AccessibilityLiveMode.Off or > AccessibilityLiveMode.Assertive) throw new ArgumentOutOfRangeException(nameof(mode));
        var e = Require().Get(id); if (e.LiveMode == mode) return; e.LiveMode = mode;
        if (mode == AccessibilityLiveMode.Off) e.AnnouncementText = null;
        Runtime._native!.SemanticAttributesChanged(e); Runtime._native.QueueAnnouncement(e);
    }
    /// <summary>Publishes the kind of popup opened by this element's action.</summary>
    /// <param name="id">Live element.</param><param name="popupType">Typed content kind.</param>
    /// <remarks>Publishes capability; opening the popup remains the producer's registered action.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The popup type is unknown.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetPopupType(RID id, AccessibilityPopupType popupType)
    {
        if (popupType is < AccessibilityPopupType.Menu or > AccessibilityPopupType.Dialog) throw new ArgumentOutOfRangeException(nameof(popupType));
        var e = Require().Get(id); if (e.PopupType == popupType) return; e.PopupType = popupType; Runtime._native!.SemanticAttributesChanged(e);
    }
    /// <summary>Publishes supplementary tooltip text without presenting a tooltip.</summary>
    /// <param name="id">Live element.</param><param name="tooltip">Text; empty removes the tooltip capability.</param>
    /// <exception cref="ArgumentNullException">Text is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetTooltip(RID id, string tooltip)
    {
        ArgumentNullException.ThrowIfNull(tooltip); var e = Require().Get(id); if (e.Tooltip == tooltip) return;
        e.Tooltip = tooltip; Runtime._native!.SemanticAttributesChanged(e);
    }
    internal static void FlushAnnouncements() => Runtime._native?.FlushAnnouncements();
}
