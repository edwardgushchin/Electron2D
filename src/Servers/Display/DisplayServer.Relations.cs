namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Returns the instance identity of the scene Window attached to a native window.</summary>
    /// <param name="windowID">A live native window identity; zero selects the main window.</param>
    /// <returns>The attached Window's InstanceID, or zero for a lower-level host without a scene Window.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The native window is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">The service is inactive or accessed off-owner.</exception>
    public static ulong WindowGetAttachedInstanceID(int windowID = MainWindowId) => RequireService().ResolveWindow(windowID).SceneWindow?.InstanceID ?? 0;

    /// <summary>Assigns or clears an ordinary native window's transient parent.</summary>
    /// <param name="windowID">A live native window identity.</param>
    /// <param name="parentWindowID">A live parent identity, or InvalidWindowId to clear the parent.</param>
    /// <remarks>Requests native parenting without changing stored Window.Transient or scene ancestry. The next scene
    /// relationship update may reapply those policies. Reparenting preserves the requested exclusive setting and rolls
    /// back native failure. Retiring a parent detaches surviving ordinary windows; popup descendants retain their owned
    /// lifetime. A live native popup cannot change its fixed parent. Native window changes reject frame submission.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either identity is absent or retired.</exception>
    /// <exception cref="ArgumentException">The requested parent forms a cycle.</exception>
    /// <exception cref="InvalidOperationException">The service is inactive, off-owner, retiring, submitting, or the native request fails.</exception>
    /// <exception cref="AggregateException">Both a native request and its rollback fail.</exception>
    public static void WindowSetTransient(int windowID, int parentWindowID)
    {
        var root = RequireService();
        var window = root.ResolveWindow(windowID);
        var parent = parentWindowID == InvalidWindowId ? null : root.ResolveWindow(parentWindowID);
        window.EnsureRelationMutable(); parent?.EnsureRelationMutable();
        if (window.NativePopupParent is { } fixedParent && !ReferenceEquals(parent, fixedParent))
            throw new InvalidOperationException("Hide a native popup before changing its parent.");
        window.SetTransientParent(parent, window._transientExclusive);
    }

    /// <summary>Requests native modality for a transient window.</summary>
    /// <param name="windowID">A live native window identity.</param>
    /// <param name="exclusive">Whether the native parent should ignore input while this window is open.</param>
    /// <remarks>Without a parent the request is retained until parenting. Stored Window.Exclusive is unchanged;
    /// subsequent scene relationship updates may reapply it. Ordinary windows request native modality. Fixed-parent
    /// popup roles use the existing exclusive input and outside-dismissal path because their native role cannot change
    /// modality. Native failures preserve the previous request; platform window managers own actual focus behavior.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The native window is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">The service is inactive, off-owner, retiring, submitting, or the native request fails.</exception>
    /// <exception cref="AggregateException">Both a native request and its rollback fail.</exception>
    public static void WindowSetExclusive(int windowID, bool exclusive)
    {
        var window = RequireService().ResolveWindow(windowID);
        window.EnsureRelationMutable();
        window.SetTransientParent(window.NativePopupParent ?? window._transientDisplay, exclusive);
    }

    private void EnsureRelationMutable()
    {
        EnsureOwner();
        if (_retiringNativeWindow) throw new InvalidOperationException("The native window is retiring.");
        RenderingServer.Service?.EnsureNativeWindowMutation();
    }

    internal bool TransientExclusive => _transientExclusive;

    internal bool HasExclusiveNativeChild()
    {
        var root = _rootDisplay ?? this;
        if (root.BlocksParentInput(this)) return true;
        foreach (var child in root._childWindows.Values)
            if (child.BlocksParentInput(this)) return true;
        return false;
    }

    private bool BlocksParentInput(DisplayServer parent) => _transientExclusive &&
        ReferenceEquals(NativePopupParent ?? _transientDisplay, parent) &&
        (SDL3.SDL.GetWindowFlags(_window.DangerousGetHandle()) & (SDL3.SDL.WindowFlags.Hidden | SDL3.SDL.WindowFlags.Minimized)) == 0;
}
