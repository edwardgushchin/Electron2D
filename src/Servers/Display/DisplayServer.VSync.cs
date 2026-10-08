namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Selects the window's presentation synchronization policy.</summary>
    public enum VSyncMode
    {
        /// <summary>Presents immediately, without waiting for vertical blank.</summary>
        Disabled = 0,
        /// <summary>Presents in order at vertical blank.</summary>
        Enabled = 1,
        /// <summary>Synchronizes when on time; unsupported adaptive presentation falls back to Enabled.</summary>
        Adaptive = 2,
        /// <summary>Presents the newest completed image at vertical blank; unsupported mailbox presentation falls back to Enabled.</summary>
        Mailbox = 3
    }
    private VSyncMode _requestedVSync = VSyncMode.Enabled;
    private bool _vSyncConfigured;
    /// <summary>Requests a presentation policy for the main window.</summary>
    /// <param name="vsyncMode">A defined presentation policy.</param>
    /// <param name="windowId">The main window identity.</param>
    /// <remarks>Before renderer creation this retains the request. An active renderer applies it immediately;
    /// unsupported policies fall back to Enabled, observable through WindowGetVSyncMode. Native errors preserve
    /// the previous policy. A compositor may independently control when an image reaches the screen.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The policy or window identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">No display is active, the thread is wrong, rendering is in progress, or the native request fails.</exception>
    public static void WindowSetVSyncMode(VSyncMode vsyncMode, int windowId = MainWindowId)
    {
        var display = RequireService(); display.EnsureOwner(); display.GetWindow(windowId);
        if (!Enum.IsDefined(vsyncMode)) throw new ArgumentOutOfRangeException(nameof(vsyncMode));
        RenderingServer.Service?.SetVSync(vsyncMode);
        display._requestedVSync = vsyncMode; display._vSyncConfigured = true;
    }
    /// <summary>Returns the applied renderer policy, or the retained request before renderer creation.</summary>
    /// <param name="windowId">The main window identity.</param>
    /// <returns>The effective presentation policy, including supported-mode fallback.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">No display is active or the thread is wrong.</exception>
    public static VSyncMode WindowGetVSyncMode(int windowId = MainWindowId)
    {
        var display = RequireService(); display.EnsureOwner(); display.GetWindow(windowId);
        return RenderingServer.Service?.GetVSync() ?? display._requestedVSync;
    }
    internal void ApplyVSyncPolicy(CanvasBackend backend) { if (_vSyncConfigured) backend.SetVSync(_requestedVSync); }
}
