using IOPath = System.IO.Path;
using System.Globalization;
using System.IO.Enumeration;

namespace Electron2D;

public partial class FileDialog
{
    private static readonly StringComparer FileNames = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);
    private void ChangeDirectory(string path, bool history)
    {
        ArgumentNullException.ThrowIfNull(path); var before = CurrentDir;
        try
        {
            _directory.ChangeDir(path);
            if (_root.Length != 0 && !WithinRoot(CurrentDir)) { _directory.ChangeDir(before); throw new UnauthorizedAccessException("Directory navigation cannot leave RootSubfolder."); }
        }
        catch { if (_directory.GetCurrentDir() != before) _directory.ChangeDir(before); throw; }
        _directoryEdit.Text = _root.Length == 0 ? CurrentDir : IOPath.GetRelativePath(ProjectSettings.GlobalizePath(_root), ProjectSettings.GlobalizePath(CurrentDir));
        _files.DeselectAll(); if (history) PushHistory(); Invalidate();
    }
    private bool WithinRoot(string path)
    {
        var relative = IOPath.GetRelativePath(ProjectSettings.GlobalizePath(_root), ProjectSettings.GlobalizePath(path));
        return !IOPath.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + IOPath.DirectorySeparatorChar, StringComparison.Ordinal);
    }
    private void SubmitDirectory(string path) => TryOperation(() => { ChangeDirectory(_root.Length == 0 ? path : Join(_root, path), true); if (_mode != FileDialogMode.SaveFile) CurrentFile = ""; });
    private void PushHistory()
    {
        if (_historyIndex >= 0 && _history[_historyIndex] == CurrentDir) return;
        if (_historyIndex + 1 < _history.Count) _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        _history.Add(CurrentDir); _historyIndex = _history.Count - 1; RefreshHistory();
    }
    private void RefreshHistory() { _previous.Disabled = _historyIndex <= 0; _next.Disabled = _historyIndex >= _history.Count - 1; }
    private void GoBack() => TryOperation(() => { if (_historyIndex <= 0) return; ChangeDirectory(_history[--_historyIndex], false); RefreshHistory(); });
    private void GoForward() => TryOperation(() => { if (_historyIndex >= _history.Count - 1) return; ChangeDirectory(_history[++_historyIndex], false); RefreshHistory(); });
    private void GoUp() => TryOperation(() => { if (_root.Length != 0 && CurrentDir == _root || CurrentDir is "res://" or "user://") return; ChangeDirectory("..", true); });
    private void UserFilenameFilter(string text) => FilenameFilter = text;
    private void SetFilenameFilter(string text)
    {
        if (_filenameFilterText == text) return; if (text.Length != 0 && !_filterVisible) { _filterVisible = true; ApplyFeatures(); }
        _filenameFilterText = text; _filenameFilter.Text = text; List<Exception>? errors = null;
        try { FilenameFilterChanged?.Invoke(text); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) Invalidate(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("File filter callbacks failed.", errors);
    }
    private void SetFilterVisible(bool value) { _filterVisible = value; ApplyFeatures(); if (!value) ClearFilenameFilter(); else _filenameFilter.GrabFocus(); }
    private void UpdateFilenameExtension()
    {
        if (_mode != FileDialogMode.SaveFile || CurrentFile.Length == 0 || MatchesType(CurrentFile)) return;
        var index = _filter.Selected - (_filters.Length > 1 ? 1 : 0); if ((uint)index >= _filters.Length) return;
        var patterns = _filters[index].Split(';')[0].Split(','); if (patterns.Length != 1) return; var pattern = patterns[0].Trim();
        if (pattern.StartsWith("*.", StringComparison.Ordinal) && !pattern.AsSpan(2).ContainsAny('*', '?')) CurrentFile = IOPath.GetFileNameWithoutExtension(CurrentFile) + pattern[1..];
    }
    private void RebuildFilters()
    {
        var old = _filter.Selected; _filter.Clear();
        if (_filters.Length > 1) _filter.AddItem("All Recognized");
        foreach (var filter in _filters) { var pieces = filter.Split(';', StringSplitOptions.TrimEntries); _filter.AddItem(pieces.Length > 1 && pieces[1].Length != 0 ? pieces[1] + " (" + pieces[0] + ")" : pieces[0]); }
        _filter.AddItem("All Files"); if (old >= 0 && old < _filter.ItemCount) _filter.Select(old);
    }
    private bool MatchesType(string name)
    {
        var selected = _filter.Selected;
        if (selected == _filter.ItemCount - 1 || _filters.Length == 0) return true;
        if (_filters.Length > 1 && selected == 0) return _filters.Any(filter => MatchesPatterns(name, filter));
        var index = selected - (_filters.Length > 1 ? 1 : 0); return index < 0 || index >= _filters.Length || MatchesPatterns(name, _filters[index]);
    }
    private static bool MatchesPatterns(string name, string filter)
    { foreach (var pattern in filter.Split(';')[0].Split(',')) if (FileSystemName.MatchesSimpleExpression(pattern.Trim(), name, ignoreCase: true)) return true; return false; }
    private void RefreshBrowser()
    {
        if (_refreshing || _disposing || _files == null || _directory == null) return;
        _refreshing = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                if (!_dirty) break; _dirty = false; _directory.IncludeHidden = _showHidden; _entries.Clear();
                foreach (var name in _directory.GetDirectories()) { var path = Join(CurrentDir, name); _entries.Add(new(name, path, true, Directory.GetLastWriteTimeUtc(ProjectSettings.GlobalizePath(path)))); }
                foreach (var name in _directory.GetFiles()) if (MatchesType(name) && name.Contains(_filenameFilterText, StringComparison.OrdinalIgnoreCase)) { var path = Join(CurrentDir, name); _entries.Add(new(name, path, false, File.GetLastWriteTimeUtc(ProjectSettings.GlobalizePath(path)))); }
                _entries.Sort(CompareEntries); _files.Clear(); ApplyFileTheme();
                foreach (var entry in _entries.ToArray())
                {
                    if (IsDisposed) return; Texture? icon = null;
                    try { if (!entry.Directory) icon = (_display == DisplayModeType.List ? _iconCallback : _thumbnailCallback)?.Invoke(entry.Path); } catch (Exception e) { CollectException(ref errors, e); }
                    if (IsDisposed) return;
                    icon = icon is { IsDisposed: false } ? icon : GetThemeIcon(entry.Directory ? (_display == DisplayModeType.List ? "folder" : "folder_thumbnail") : _display == DisplayModeType.List ? "file" : "file_thumbnail");
                    var i = _files.AddItem(entry.Name, icon); _files.SetItemMetadata(i, entry); _files.SetItemTooltip(i, entry.Path);
                    _files.SetItemIconModulate(i, GetThemeColor(entry.Directory ? "folder_icon_color" : "file_icon_color"));
                    var disabled = !entry.Directory && _mode == FileDialogMode.OpenDirectory;
                    _files.SetItemDisabled(i, disabled); if (disabled) _files.SetItemCustomFGColor(i, GetThemeColor("file_disabled_color"));
                }
                _directoryEdit.Text = _root.Length == 0 ? CurrentDir : IOPath.GetRelativePath(ProjectSettings.GlobalizePath(_root), ProjectSettings.GlobalizePath(CurrentDir));
                RefreshPlaces(); RefreshHistory(); RefreshOK();
                if (!_dirty) break; if (pass == 63) throw new InvalidOperationException("File browser refresh did not settle.");
            }
        }
        finally { _refreshing = false; }
        ThrowCollected("File browser callbacks failed.", errors);
    }
    private int CompareEntries(Entry a, Entry b)
    {
        if (a.Directory != b.Directory) return a.Directory ? -1 : 1;
        var result = _sortIndex is 4 or 5 ? b.Modified.CompareTo(a.Modified) : _sortIndex is 2 or 3 && !a.Directory ? FileNames.Compare(IOPath.GetExtension(a.Name), IOPath.GetExtension(b.Name)) : FileNames.Compare(a.Name, b.Name);
        if (result == 0) result = FileNames.Compare(a.Name, b.Name); return (_sortIndex & 1) == 0 ? result : -result;
    }
    private void FileSelectedIndex(int index) { if ((uint)index >= _files.ItemCount) return; var entry = _files.GetItemMetadata<Entry>(index); if (!entry.Directory) CurrentFile = entry.Name; RefreshOK(); }
    private void ActivateEntry(int index) { var entry = _files.GetItemMetadata<Entry>(index); if (entry.Directory) TryOperation(() => { ChangeDirectory(entry.Path, true); if (_mode != FileDialogMode.SaveFile) CurrentFile = ""; }); else { CurrentFile = entry.Name; AcceptSelection(); } }
    private void FileClicked(int index, Vector2 point, MouseButton button)
    {
        if (button != MouseButton.Right) return; _context.Clear();
        if (FolderCreationEnabled && _mode is FileDialogMode.OpenDirectory or FileDialogMode.OpenAny or FileDialogMode.SaveFile) _context.AddIconItem(GetThemeIcon("menu_new_folder"), "New Folder", 1);
        if (DeletingEnabled && index >= 0) _context.AddIconItem(GetThemeIcon("menu_delete"), "Move to Trash", 2);
        if (index >= 0) { _context.AddIconItem(GetThemeIcon("menu_open_bundle"), "Open", 3); _context.AddIconItem(GetThemeIcon("menu_copy_path"), "Copy Path", 4); _context.AddIconItem(GetThemeIcon("menu_show_in_file_manager"), "Show in File Manager", 5); _files.Select(index, single: true); }
        _context.AddIconItem(GetThemeIcon("menu_refresh"), "Refresh", 6);
        if (_context.ItemCount == 0) return; var at = _files.GetGlobalTransformWithCanvas() * point; if (Embedder != null && _context.Embedder != this) at += (Vector2)Position; _context.Popup(new((Vector2i)at, new(1, 1)));
    }
    private void ContextAction(int id) { if (id == 1) _mkdir.PopupCentered(new(320, 130)); else if (id == 2) _delete.PopupCentered(new(320, 130)); else if (id == 3 && _files.GetSelectedItems().Length > 0) ActivateEntry(_files.GetSelectedItems()[0]); else if (id == 4 && _files.GetSelectedItems().Length > 0) DisplayServer.ClipboardSet(_files.GetItemMetadata<Entry>(_files.GetSelectedItems()[0]).Path); else if (id == 5 && _files.GetSelectedItems().Length > 0) TryOperation(() => OS.ShellShowInFileManager(ProjectSettings.GlobalizePath(_files.GetItemMetadata<Entry>(_files.GetSelectedItems()[0]).Path))); else if (id == 6) Invalidate(); }
    private void CreateFolder() => TryOperation(() => { var name = _folderName.Text.Trim(); if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(['/', '\\', '\0']) >= 0) throw new ArgumentException("Expected one folder name."); _directory.MakeDir(name); ChangeDirectory(name, true); _folderName.Text = ""; });
    private void TrashSelected() => TryOperation(() => { var paths = _files.GetSelectedItems().Select(i => _files.GetItemMetadata<Entry>(i).Path).ToArray(); try { foreach (var path in paths) OS.MoveToTrash(path); } finally { if (!IsDisposed) Invalidate(); } });
    private void SelectDrive(int index) => TryOperation(() => ChangeDirectory(DirAccess.GetDriveName(index), true));
    private bool AccessMatches(string path) => _access == FileDialogAccess.Resources ? path.StartsWith("res://", StringComparison.Ordinal) : _access == FileDialogAccess.UserData ? path.StartsWith("user://", StringComparison.Ordinal) : !path.StartsWith("res://", StringComparison.Ordinal) && !path.StartsWith("user://", StringComparison.Ordinal);
    private void RefreshPlaces()
    {
        _favorites.Clear(); _recents.Clear();
        foreach (var pair in new[] { (GetFavoriteList(), _favorites), (GetRecentList(), _recents) })
        {
            var invalid = new List<string>();
            foreach (var path in pair.Item1)
            {
                if (!AccessMatches(path) || _root.Length != 0 && !WithinRoot(path)) continue;
                if (!_directory.DirExists(path)) { invalid.Add(path); continue; }
                var caption = path is "res://" or "user://" or "/" ? "/" : IOPath.GetFileName(path.TrimEnd('/'));
                var i = pair.Item2.AddItem(caption, GetThemeIcon("folder")); pair.Item2.SetItemMetadata(i, path); pair.Item2.SetItemTooltip(i, path); pair.Item2.SetItemIconModulate(i, GetThemeColor("folder_icon_color"));
            }
            if (invalid.Count != 0) lock (SharedGate) { if (ReferenceEquals(pair.Item2, _favorites)) _sharedFavorites = _sharedFavorites.Where(path => !invalid.Contains(path)).ToArray(); else _sharedRecents = _sharedRecents.Where(path => !invalid.Contains(path)).ToArray(); }
        }
        _favorite.SetPressedNoSignal(GetFavoriteList().Any(path => path.TrimEnd('/') == CurrentDir.TrimEnd('/')));
    }
    private void PlaceActivated(ItemList list, int index) => TryOperation(() => ChangeDirectory(list.GetItemMetadata<string>(index), true));
    private void ToggleFavorite() { var path = CurrentDir.EndsWith('/') ? CurrentDir : CurrentDir + "/"; lock (SharedGate) { _sharedFavorites = _sharedFavorites.Contains(path) ? _sharedFavorites.Where(p => p != path).ToArray() : [.. _sharedFavorites, path]; } RefreshPlaces(); }
    private void MoveFavorite(int direction)
    {
        var index = _favorites.Current; if (index < 0) return; var path = _favorites.GetItemMetadata<string>(index);
        lock (SharedGate) { var position = Array.IndexOf(_sharedFavorites, path); var next = position + direction; if (position < 0 || next < 0 || next >= _sharedFavorites.Length) return; (_sharedFavorites[position], _sharedFavorites[next]) = (_sharedFavorites[next], _sharedFavorites[position]); }
        RefreshPlaces(); if (index + direction >= 0 && index + direction < _favorites.ItemCount) _favorites.Select(index + direction);
    }
    private void SaveRecent()
    {
        var path = CurrentDir.EndsWith('/') ? CurrentDir : CurrentDir + "/";
        lock (SharedGate)
        {
            var recents = new List<string> { path }; var count = 1;
            foreach (var recent in _sharedRecents) if (recent != path && (!AccessMatches(recent) || count++ < 20)) recents.Add(recent);
            _sharedRecents = recents.ToArray();
        }
    }
    private void RefreshOK()
    {
        if (_files == null || _filenameEdit == null || IsDisposed) return;
        GetOKButton().Disabled = _mode switch { FileDialogMode.SaveFile => _filenameEdit.Text.Trim().Length == 0, FileDialogMode.OpenDirectory => false, FileDialogMode.OpenFiles => !_files.GetSelectedItems().Any(i => !_files.GetItemMetadata<Entry>(i).Directory), _ => _filenameEdit.Text.Length == 0 && _files.GetSelectedItems().Length == 0 };
        if (_mode == FileDialogMode.OpenDirectory) SetDefaultOKText(_files.GetSelectedItems().Length == 0 ? "Select Current Folder" : "Select This Folder");
    }
    private void AcceptSelection()
    {
        TryOperation(() =>
        {
            if (_mode == FileDialogMode.OpenFiles) { var paths = _files.GetSelectedItems().Select(i => _files.GetItemMetadata<Entry>(i)).Where(e => !e.Directory).Select(e => e.Path).ToArray(); if (paths.Length != 0) FinishFiles(paths); return; }
            var name = CurrentFile.Trim(); var path = IOPath.IsPathRooted(name) || name.StartsWith("res://", StringComparison.Ordinal) || name.StartsWith("user://", StringComparison.Ordinal) ? name : Join(CurrentDir, name);
            ValidateSelectionScope(path);
            if (_mode is FileDialogMode.OpenFile or FileDialogMode.OpenAny && _directory.FileExists(path)) { FinishFile(path); return; }
            if (_mode is FileDialogMode.OpenDirectory or FileDialogMode.OpenAny) { var selected = _files.GetSelectedItems(); var directory = selected.Length > 0 && _files.GetItemMetadata<Entry>(selected[0]).Directory ? _files.GetItemMetadata<Entry>(selected[0]).Path : CurrentDir; FinishDirectory(directory); return; }
            if (_mode != FileDialogMode.SaveFile) { ShowError("Select an existing file."); return; }
            if (name.Length == 0 || name.EndsWith('/') || name.EndsWith('\\')) { ShowError("Enter a filename."); return; }
            if (!MatchesType(IOPath.GetFileName(path)))
            {
                var index = _filter.Selected - (_filters.Length > 1 ? 1 : 0);
                if (index < 0 || index >= _filters.Length) { ShowError("Filename does not match the selected filters."); return; }
                var pattern = _filters[index].Split(';')[0].Split(',')[0].Trim(); if (!pattern.StartsWith("*.", StringComparison.Ordinal) || pattern.AsSpan(2).ContainsAny('*', '?')) { ShowError("Enter a filename matching the selected filter."); return; }
                path += pattern[1..]; CurrentFile = IOPath.GetFileName(path);
            }
            if (!_directory.DirExists(ParentPath(path))) { ShowError("The destination directory does not exist."); return; }
            if (OverwriteWarningEnabled && _directory.FileExists(path)) { _pendingSave = path; _overwrite.PopupCentered(new(380, 150)); return; }
            FinishFile(path);
        });
    }
    private void ValidateSelectionScope(string path)
    {
        if (!AccessMatches(path) && _access != FileDialogAccess.FileSystem) throw new UnauthorizedAccessException("Selection cannot switch access scope.");
        if (_root.Length != 0 && !WithinRoot(path)) throw new UnauthorizedAccessException("Selection cannot leave RootSubfolder.");
    }
    private void FinishPendingSave() { var path = _pendingSave; _pendingSave = ""; if (path.Length != 0) TryOperation(() => { ValidateSelectionScope(path); FinishFile(path); }); }
    private void FinishFile(string path) { SaveRecent(); HideAndNotify(() => FileSelected?.Invoke(path)); }
    private void FinishFiles(string[] paths) { SaveRecent(); HideAndNotify(() => FilesSelected?.Invoke(Array.AsReadOnly(paths))); }
    private void FinishDirectory(string path) { SaveRecent(); HideAndNotify(() => DirSelected?.Invoke(path)); }
    private void HideAndNotify(Action notify)
    { List<Exception>? errors = null; try { Hide(); } catch (Exception e) { CollectException(ref errors, e); } try { if (!IsDisposed) notify(); } catch (Exception e) { CollectException(ref errors, e); } ThrowCollected("File selection callbacks failed.", errors); }
    private void TryOperation(Action action) { try { action(); } catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { if (!IsDisposed) ShowError(e.Message); } }
    private void ShowError(string message) { _error.DialogText = message; if (IsInsideTree) _error.PopupCentered(new(380, 150)); }
    /// <inheritdoc />
    protected override void OnShortcutInput(InputEvent input)
    {
        base.OnShortcutInput(input); if (!Visible || !input.IsPressed() || input.IsEcho()) return;
        if (input.IsActionPressed("ui_filedialog_delete", false, true) && DeletingEnabled && _files.GetSelectedItems().Length != 0) _delete.PopupCentered(new(320, 130));
        else if (input.IsActionPressed("ui_filedialog_up_one_level", false, true)) GoUp();
        else if (input.IsActionPressed("ui_filedialog_refresh", false, true)) Invalidate();
        else if (input.IsActionPressed("ui_filedialog_show_hidden", false, true) && HiddenFilesToggleEnabled) ShowHiddenFiles = !ShowHiddenFiles;
        else if (input.IsActionPressed("ui_filedialog_find", false, true) && FileFilterToggleEnabled) { SetFilterVisible(true); _filenameFilter.SelectAll(); }
        else if (input.IsActionPressed("ui_filedialog_focus_path", false, true)) { _directoryEdit.GrabFocus(); _directoryEdit.SelectAll(); }
        else return;
        SetInputAsHandled();
    }
    /// <inheritdoc />
    protected override void OnOKPressed() => AcceptSelection();
    /// <inheritdoc />
    protected override void OnCancelPressed() { CurrentFile = ""; Hide(); }
    internal override void PreparePopup() { RefreshBrowser(); RefreshOptions(); base.PreparePopup(); }
    internal override void AfterVisibilityChanged(bool visible) { if (_vbox != null && visible) { _dirty = true; RefreshBrowser(); RefreshOptions(); } base.AfterVisibilityChanged(visible); }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (_files != null && !IsDisposed && what is NotificationThemeChanged or Control.NotificationLayoutDirectionChanged) { ApplyFeatures(); Invalidate(); } }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(FileDialog) ? CreateFileDialog : base.CreateSceneInstanceFactory();
    private static Node CreateFileDialog() => new FileDialog();
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_nativePending) throw new InvalidOperationException("A native chooser is still pending; wait for its result."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; _directory?.Dispose(); FileSelected = null; FilesSelected = null; DirSelected = null; FilenameFilterChanged = null; } base.Dispose(disposing); }
}
