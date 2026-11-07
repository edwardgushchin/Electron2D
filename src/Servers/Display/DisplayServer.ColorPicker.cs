namespace Electron2D;

public sealed partial class DisplayServer
{
    private readonly List<LinuxPortalColorPicker> _portalColorPickers = [];

    internal bool UsesPortalColorPicker => OperatingSystem.IsLinux() && _videoDriver is "wayland" or "x11";

    /// <summary>Starts the system picker for a color on the desktop.</summary>
    /// <param name="callback">Receives the selection status and opaque sRGB color on the display owner thread. An unsuccessful request reports false with black.</param>
    /// <returns><see langword="true"/> when the request was accepted, or <see langword="false"/> when it could not start.</returns>
    /// <remarks>Each accepted request delivers at most one callback. Display shutdown retires outstanding requests.</remarks>
    /// <exception cref="InvalidOperationException">The display is inactive or the caller is off its owner thread.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
    public static bool ColorPicker(Action<bool, Color> callback)
    {
        var display = RequireService();
        display.EnsureOwner();
        ArgumentNullException.ThrowIfNull(callback);
        return display.BeginPortalColorPick(color => callback(color.HasValue, color ?? Colors.Black)) is not null;
    }

    internal LinuxPortalColorPicker? BeginPortalColorPick(Action<Color?> callback)
    {
        EnsureOwner();
        if (!UsesPortalColorPicker)
            return null;

        LinuxPortalColorPicker request;
        try { request = new(GetPortalParent(_window.DangerousGetHandle()), callback); }
        catch (Exception error) when (error is NotSupportedException or InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }

        try { _portalColorPickers.Add(request); }
        catch { request.Dispose(); throw; }
        return request;
    }

    internal void CancelPortalColorPick(LinuxPortalColorPicker? request)
    {
        if (request is null) return;
        _portalColorPickers.Remove(request);
        request.Cancel();
    }

    private void PollPortalColorPickers(ref List<Exception>? failures)
    {
        List<(LinuxPortalColorPicker Request, Color? Color)>? completed = null;
        for (var i = _portalColorPickers.Count - 1; i >= 0; i--)
        {
            var request = _portalColorPickers[i];
            if (!request.TryComplete(out var color)) continue;
            _portalColorPickers.RemoveAt(i);
            request.Dispose();
            (completed ??= []).Add((request, color));
        }

        if (completed is null) return;
        for (var i = completed.Count - 1; i >= 0 && !IsDisposed; i--)
        {
            var item = completed[i];
            if (item.Request.Canceled) continue;
            try { item.Request.Completed(item.Color); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
    }

    private void RetirePortalColorPickers()
    {
        foreach (var request in _portalColorPickers)
            request.Dispose();
        _portalColorPickers.Clear();
    }
}
