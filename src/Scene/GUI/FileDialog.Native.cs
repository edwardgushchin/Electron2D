namespace Electron2D;

public partial class FileDialog
{
    private bool CanUseNative() => _native && IsInsideTree && _access == FileDialogAccess.FileSystem && _options.Count == 0 && _root.Length == 0 && DisplayServer.IsAvailable && DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFile);
    internal override bool TryNativeVisibility(bool visible) { if (!visible || !CanUseNative()) return false; PresentNative(); return true; }
    internal override bool TryNativePopup() { if (!CanUseNative()) return false; PresentNative(); return true; }
    private void PresentNative()
    {
        EnsureMutable(); if (!CanUseNative()) { PopupCentered(); return; }
        if (_nativePending) throw new InvalidOperationException("A native file chooser is already pending.");
        var directory = ProjectSettings.GlobalizePath(CurrentDir); var file = CurrentFile; var filters = (string[])_filters.Clone(); var nativeFilters = NativeFilters(filters); var mode = _mode; var parentId = DisplayServer.MainWindowId;
        for (var node = Parent; node != null; node = node.Parent) if (node is Window window && window.GetWindowID() == DisplayServer.MainWindowId) { parentId = window.GetWindowID(); break; }
        _nativePending = true;
        try { DisplayServer.FileDialogShow(Title, directory, file, _showHidden, mode, nativeFilters, (ok, paths, filter) => NativeCompleted(mode, ok, paths, filter, filters), parentId); }
        catch { _nativePending = false; throw; }
    }
    private static string[] NativeFilters(string[] filters)
    {
        var result = new List<string>();
        if (filters.Length > 1)
        {
            var patterns = filters.SelectMany(filter => filter.Split(';')[0].Split(',')).Select(pattern => pattern.Trim()).ToArray();
            result.Add((patterns.Any(pattern => pattern is "*" or "*.*") ? "*" : string.Join(',', patterns)) + ";All Recognized");
        }
        result.AddRange(filters); result.Add("*;All Files"); return result.ToArray();
    }
    private void NativeCompleted(FileDialogMode mode, bool accepted, IReadOnlyList<string> paths, int filter, string[] requestFilters)
    {
        _nativePending = false; if (IsDisposed) return;
        if (!accepted) { CurrentFile = ""; RaiseNativeCanceled(); return; }
        if (paths.Count == 0) return;
        var path = paths[0];
        if (mode == FileDialogMode.SaveFile && requestFilters.Length != 0 && filter != requestFilters.Length + (requestFilters.Length > 1 ? 1 : 0))
        {
            var index = filter - (requestFilters.Length > 1 ? 1 : 0);
            if (index < 0) { if (!requestFilters.Any(f => MatchesPatterns(System.IO.Path.GetFileName(path), f))) index = 0; }
            if ((uint)index < requestFilters.Length && !MatchesPatterns(System.IO.Path.GetFileName(path), requestFilters[index]))
            {
                var pattern = requestFilters[index].Split(';')[0].Split(',')[0].Trim();
                if (pattern.StartsWith("*.", StringComparison.Ordinal) && !pattern.AsSpan(2).ContainsAny('*', '?')) path += pattern[1..];
            }
        }
        CurrentPath = path; if (filter >= 0 && filter < _filter.ItemCount) _filter.Select(filter);
        if (mode == FileDialogMode.OpenFiles) FinishFiles(paths.ToArray()); else if (mode == FileDialogMode.OpenDirectory) FinishDirectory(path); else FinishFile(path);
    }
}
