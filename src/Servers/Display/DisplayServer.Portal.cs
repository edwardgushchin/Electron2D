using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private readonly List<(LinuxPortalFileChooser Portal, DialogRequest Request)> _portalDialogs = [];
    private bool UsesPortalFileChooser => OperatingSystem.IsLinux() && _videoDriver is "wayland" or "x11";

    private void ShowPortalFileDialog(nint window, int parentWindowId, string title, string directory, string filename, FileDialogMode mode, IReadOnlyList<string> filters, Action<bool, IReadOnlyList<string>, int>? callback, FileDialogOption[]? options, Action<bool, IReadOnlyList<string>, int, IReadOnlyDictionary<string, FileDialogOptionValue>>? optionsCallback)
    {
        var properties = SDL.GetWindowProperties(window);
        var handle = _videoDriver == "wayland" ? SDL.GetStringProperty(properties, SDL.Props.WindowWaylandXDGToplevelExportHandleString, "") : "";
        var parent = handle.Length != 0 ? "wayland:" + handle : _videoDriver == "x11" ? "x11:" + SDL.GetNumberProperty(properties, SDL.Props.WindowX11WindowNumber, 0).ToString("x", System.Globalization.CultureInfo.InvariantCulture) : "";
        LinuxPortalFileChooser? portal = null;
        var request = new DialogRequest(this, ResolveWindow(parentWindowId), optionsCallback == null ? callback! : (ok, paths, filter) => optionsCallback(ok, paths, filter, portal!.SelectedOptions), []);
        request.RetainParent();
        try
        {
            portal = new(parent, title, directory, filename, mode, filters, options ?? []);
            _portalDialogs.Add((portal, request));
            lock (_dialogGate) _pendingNativeDialogs++;
        }
        catch { portal?.Dispose(); request.DisposeFilters(); throw; }
    }

    private void PollPortalFileDialogs()
    {
        for (var index = _portalDialogs.Count - 1; index >= 0; index--)
        {
            var item = _portalDialogs[index];
            if (!item.Portal.TryComplete(out var paths, out var filter, out var error)) continue;
            _portalDialogs.RemoveAt(index);
            try { item.Portal.Dispose(); }
            finally
            {
                item.Request.DisposeFilters();
                QueueDialogCompletion(new(item.Request.Callback, paths, filter, error));
            }
        }
    }
}
