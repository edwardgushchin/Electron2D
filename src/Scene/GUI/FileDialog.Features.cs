namespace Electron2D;

public partial class FileDialog
{
    /// <summary>Reports whether one browser customization feature is enabled.</summary><param name="flag">A defined feature.</param><returns>The retained feature state.</returns>
    public bool IsCustomizationFlagEnabled(Customization flag) { CheckFileDialog(); if ((uint)flag >= _custom.Length) throw new ArgumentOutOfRangeException(nameof(flag)); return _custom[(int)flag]; }
    /// <summary>Changes one feature's actual control/context-menu visibility or validation behavior.</summary><param name="flag">A defined feature.</param><param name="enabled">The new state.</param>
    public void SetCustomizationFlagEnabled(Customization flag, bool enabled) { EnsureMutable(); if ((uint)flag >= _custom.Length) throw new ArgumentOutOfRangeException(nameof(flag)); _custom[(int)flag] = enabled; ApplyFeatures(); }
    /// <summary>Gets or sets visibility of the hidden-file toggle.</summary><value>True initially.</value>
    public bool HiddenFilesToggleEnabled { get => IsCustomizationFlagEnabled(Customization.HiddenFiles); set => SetCustomizationFlagEnabled(Customization.HiddenFiles, value); }
    /// <summary>Gets or sets folder creation commands in applicable modes.</summary><value>True initially.</value>
    public bool FolderCreationEnabled { get => IsCustomizationFlagEnabled(Customization.CreateFolder); set => SetCustomizationFlagEnabled(Customization.CreateFolder, value); }
    /// <summary>Gets or sets visibility of the filename-filter toggle.</summary><value>True initially.</value>
    public bool FileFilterToggleEnabled { get => IsCustomizationFlagEnabled(Customization.FileFilter); set => SetCustomizationFlagEnabled(Customization.FileFilter, value); }
    /// <summary>Gets or sets visibility of the file ordering menu.</summary><value>True initially.</value>
    public bool FileSortOptionsEnabled { get => IsCustomizationFlagEnabled(Customization.FileSort); set => SetCustomizationFlagEnabled(Customization.FileSort, value); }
    /// <summary>Gets or sets visibility of favorite-directory controls.</summary><value>True initially.</value>
    public bool FavoritesEnabled { get => IsCustomizationFlagEnabled(Customization.Favorites); set => SetCustomizationFlagEnabled(Customization.Favorites, value); }
    /// <summary>Gets or sets visibility of recent directories.</summary><value>True initially.</value>
    public bool RecentListEnabled { get => IsCustomizationFlagEnabled(Customization.Recent); set => SetCustomizationFlagEnabled(Customization.Recent, value); }
    /// <summary>Gets or sets visibility of the thumbnail/list switch.</summary><value>True initially.</value>
    public bool LayoutToggleEnabled { get => IsCustomizationFlagEnabled(Customization.Layout); set => SetCustomizationFlagEnabled(Customization.Layout, value); }
    /// <summary>Gets or sets explicit confirmation for existing save destinations.</summary><value>True initially.</value>
    public bool OverwriteWarningEnabled { get => IsCustomizationFlagEnabled(Customization.OverwriteWarning); set => SetCustomizationFlagEnabled(Customization.OverwriteWarning, value); }
    /// <summary>Gets or sets the recoverable trash context command.</summary><value>True initially; native trash capability may reject an operation.</value>
    public bool DeletingEnabled { get => IsCustomizationFlagEnabled(Customization.Delete); set => SetCustomizationFlagEnabled(Customization.Delete, value); }
    private void ApplyFeatures()
    {
        if (_files == null || _disposing) return;
        _hidden.Visible = HiddenFilesToggleEnabled; _hidden.SetPressedNoSignal(_showHidden);
        _newFolder.Visible = FolderCreationEnabled && _mode is FileDialogMode.OpenDirectory or FileDialogMode.OpenAny or FileDialogMode.SaveFile;
        _filenameToggle.Visible = FileFilterToggleEnabled; _filterRow.Visible = _filterVisible; _filenameToggle.SetPressedNoSignal(_filterVisible); _sort.Visible = FileSortOptionsEnabled; _layout.Visible = LayoutToggleEnabled; _layout.SetPressedNoSignal(_display == DisplayModeType.List);
        _favorites.Visible = _favoritesLabel.Visible = _favorite.Visible = _favoriteUp.Visible = _favoriteDown.Visible = FavoritesEnabled;
        _recents.Visible = _recentsLabel.Visible = RecentListEnabled; _sidebar.Visible = FavoritesEnabled || RecentListEnabled;
        _filenameEdit.Visible = _filter.Visible = _mode != FileDialogMode.OpenDirectory; _filenameEdit.Editable = _mode != FileDialogMode.OpenDirectory;
        _files.SelectionMode = _mode == FileDialogMode.OpenFiles ? ItemList.SelectMode.Multi : ItemList.SelectMode.Single; _files.AllowRMBSelect = true;
        _files.MaxColumns = _display == DisplayModeType.List ? 1 : 0; _files.IconDisplayMode = _display == DisplayModeType.List ? ItemList.IconMode.Left : ItemList.IconMode.Top;
        _files.FixedIconSize = _display == DisplayModeType.List ? Vector2i.Zero : new(Math.Max(1, GetThemeConstant("thumbnail_size")), Math.Max(1, GetThemeConstant("thumbnail_size")));
        if (_drives.ItemCount == 0 && _access == FileDialogAccess.FileSystem) for (var i = 0; i < DirAccess.GetDriveCount(); i++) _drives.GetPopup().AddItem(DirAccess.GetDriveName(i));
        _drives.Visible = _access == FileDialogAccess.FileSystem && _root.Length == 0;
        for (var i = 0; i < _sort.GetPopup().ItemCount; i++) _sort.GetPopup().SetItemChecked(i, i == _sortIndex); ApplyFileTheme(); RefreshOK();
    }
    private void ApplyFileTheme()
    {
        if (_files == null) return;
        foreach (var entry in new[] { (_previous, "back_folder"), (_next, "forward_folder"), (_up, "parent_folder"), (_refresh, "reload"), (_newFolder, "create_folder"), (_favorite, "favorite"), (_favoriteUp, "favorite_up"), (_favoriteDown, "favorite_down"), (_sort as Button, "sort"), (_drives as Button, "folder") })
        {
            entry.Item1.Icon = GetThemeIcon(entry.Item2); entry.Item1.TooltipText = entry.Item1.Text.Length == 0 ? entry.Item1.TooltipText : entry.Item1.Text; entry.Item1.Text = "";
        }
        _layout.Icon = GetThemeIcon(_display == DisplayModeType.List ? "list_mode" : "thumbnail_mode"); _layout.TooltipText = "Switch file layout"; _layout.Text = "";
        _hidden.Icon = GetThemeIcon("toggle_hidden"); _hidden.TooltipText = "Show hidden files"; _hidden.Text = "";
        _filenameToggle.Icon = GetThemeIcon("toggle_filename_filter");
    }
}
