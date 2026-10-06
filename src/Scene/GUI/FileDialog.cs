using IOPath = System.IO.Path;

namespace Electron2D;

/// <summary>An embedded file browser supporting file/directory selection, destination validation and typed custom options.</summary>
/// <remarks>Uses the existing dialog, menu, list and directory services. Browsing is blocking I/O on the scene owner.
/// Returned controls and icon/thumbnail results are borrowed. Native requests use the active display service when supported.</remarks>
public partial class FileDialog : ConfirmationDialog
{
    /// <summary>Selects the visual arrangement of files.</summary>
    public enum DisplayModeType
    { /// <summary>Shows icons above file names.</summary>
        Thumbnails = 0, /// <summary>Shows one row per file with a leading icon.</summary>
        List = 1
    }
    /// <summary>Identifies individually configurable browser features.</summary>
    public enum Customization
    { /// <summary>Hidden-file toggle.</summary>
        HiddenFiles = 0, /// <summary>Folder creation.</summary>
        CreateFolder = 1, /// <summary>Filename substring filtering.</summary>
        FileFilter = 2, /// <summary>File ordering.</summary>
        FileSort = 3, /// <summary>Shared favorite directories.</summary>
        Favorites = 4, /// <summary>Shared recent directories.</summary>
        Recent = 5, /// <summary>Thumbnail/list layout toggle.</summary>
        Layout = 6, /// <summary>Existing destination confirmation.</summary>
        OverwriteWarning = 7, /// <summary>Recoverable trash command.</summary>
        Delete = 8
    }
    private readonly VBoxContainer _vbox;
    private readonly HBoxContainer _toolbar, _bottom, _optionsRow, _filterRow;
    private readonly HSplitContainer _split;
    private readonly VBoxContainer _sidebar;
    private readonly ItemList _files, _favorites, _recents;
    private readonly LineEdit _directoryEdit, _filenameEdit, _filenameFilter;
    private readonly OptionButton _filter;
    private readonly MenuButton _sort, _drives;
    private readonly Button _previous, _next, _up, _refresh, _newFolder, _favorite, _favoriteUp, _favoriteDown, _filenameToggle;
    private readonly CheckButton _hidden, _layout;
    private readonly Label _favoritesLabel, _recentsLabel;
    private readonly PopupMenu _context;
    private readonly ConfirmationDialog _overwrite, _mkdir, _delete;
    private readonly AcceptDialog _error;
    private readonly LineEdit _folderName;
    private readonly bool[] _custom = [true, true, true, true, true, true, true, true, true];
    private readonly List<string> _history = [];
    private readonly List<Entry> _entries = [];
    private int _historyIndex = -1, _sortIndex;
    private DirAccess _directory;
    private FileDialogAccess _access;
    private FileDialogMode _mode = FileDialogMode.SaveFile;
    private DisplayModeType _display;
    private string _rootSubfolderText = "";
    private string _root = "", _filenameFilterText = "", _pendingSave = "";
    private string[] _filters = [];
    private bool _modeTitles = true, _showHidden, _native, _nativePending, _filterVisible, _dirty = true, _refreshing, _disposing;
    private static readonly object SharedGate = new();
    private static string[] _sharedFavorites = [], _sharedRecents = [];
    private static Func<string, Texture?>? _iconCallback, _thumbnailCallback;
    private readonly record struct Entry(string Name, string Path, bool Directory, DateTime Modified);
    /// <summary>Creates a hidden 640 by 360 resource browser in save-file mode.</summary>
    public FileDialog()
    {
        CheckSharedOwner(); Title = "Save a File"; DialogHideOnOK = false; Size = new(640, 360); SetDefaultOKText("Save"); GetLabel().Hide(); ShortcutInputEnabled = true;
        _directory = DirAccess.Open("res://");
        _vbox = new VBoxContainer { Name = "_file_browser" }; AddChild(_vbox, InternalMode.Front);
        _toolbar = new HBoxContainer { Name = "Toolbar" }; _vbox.AddChild(_toolbar);
        _previous = Tool("Previous", GoBack); _next = Tool("Next", GoForward); _up = Tool("Up", GoUp);
        _drives = new MenuButton("Drives") { Name = "Drives" }; _toolbar.AddChild(_drives); _drives.GetPopup().IndexPressed += SelectDrive;
        _directoryEdit = new LineEdit { Name = "Directory", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _toolbar.AddChild(_directoryEdit); _directoryEdit.TextSubmitted += SubmitDirectory;
        _refresh = Tool("Refresh", Invalidate); _favorite = Tool("Favorite", ToggleFavorite); _favorite.ToggleMode = true;
        _newFolder = Tool("New Folder", BeginCreateFolder);
        _hidden = new CheckButton("Hidden") { Name = "Hidden" }; _toolbar.AddChild(_hidden); _hidden.Toggled += value => ShowHiddenFiles = value;
        _split = new HSplitContainer { Name = "Browser", SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _vbox.AddChild(_split);
        _sidebar = new VBoxContainer { Name = "Places", CustomMinimumSize = new(140, 0) }; _split.AddChild(_sidebar);
        _favoritesLabel = new Label { Name = "FavoritesLabel", Text = "Favorites" }; _sidebar.AddChild(_favoritesLabel);
        _favorites = new ItemList { Name = "Favorites", CustomMinimumSize = new(130, 65), SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _sidebar.AddChild(_favorites); _favorites.ItemActivated += i => PlaceActivated(_favorites, i);
        var favoriteRow = new HBoxContainer { Name = "FavoriteOrder" }; _sidebar.AddChild(favoriteRow); _favoriteUp = new Button("Up") { Name = "Up" }; _favoriteDown = new Button("Down") { Name = "Down" }; favoriteRow.AddChild(_favoriteUp); favoriteRow.AddChild(_favoriteDown); _favoriteUp.Pressed += () => MoveFavorite(-1); _favoriteDown.Pressed += () => MoveFavorite(1);
        _recentsLabel = new Label { Name = "RecentsLabel", Text = "Recent" }; _sidebar.AddChild(_recentsLabel);
        _recents = new ItemList { Name = "Recents", CustomMinimumSize = new(130, 65), SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _sidebar.AddChild(_recents); _recents.ItemActivated += i => PlaceActivated(_recents, i);
        _files = new ItemList { Name = "Files", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill }; _split.AddChild(_files); _files.ItemActivated += ActivateEntry; _files.ItemSelected += FileSelectedIndex; _files.MultiSelected += (i, selected) => { if (selected) FileSelectedIndex(i); else RefreshOK(); }; _files.ItemClicked += FileClicked;
        _filterRow = new HBoxContainer { Name = "FilenameFilterRow" }; _vbox.AddChild(_filterRow); _filenameFilter = new LineEdit { Name = "FilenameFilter", PlaceholderText = "Filter file names", ClearButtonEnabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _filterRow.AddChild(_filenameFilter); _filenameFilter.TextChanged += UserFilenameFilter;
        _bottom = new HBoxContainer { Name = "FilenameRow" }; _vbox.AddChild(_bottom); _filenameEdit = new LineEdit { Name = "Filename", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _bottom.AddChild(_filenameEdit); _filenameEdit.TextChanged += _ => RefreshOK(); RegisterTextEnter(_filenameEdit);
        _filter = new OptionButton { Name = "FileTypes", CustomMinimumSize = new(130, 0) }; _bottom.AddChild(_filter); _filter.ItemSelected += _ => { UpdateFilenameExtension(); Invalidate(); };
        _layout = new CheckButton("List") { Name = "Layout" }; _bottom.AddChild(_layout); _layout.Toggled += value => DisplayMode = value ? DisplayModeType.List : DisplayModeType.Thumbnails;
        _filenameToggle = new Button { Name = "FilterToggle", ToggleMode = true, TooltipText = "Filter file names" }; _bottom.AddChild(_filenameToggle); _filenameToggle.Toggled += SetFilterVisible;
        _sort = new MenuButton("Sort") { Name = "Sort" }; _bottom.AddChild(_sort); foreach (var name in new[] { "Name", "Name descending", "Type", "Type descending", "Modified", "Modified descending" }) _sort.GetPopup().AddRadioCheckItem(name); _sort.GetPopup().IndexPressed += i => { _sortIndex = i; ApplyFeatures(); Invalidate(); };
        _optionsRow = new HBoxContainer { Name = "Options" }; _vbox.AddChild(_optionsRow);
        _context = new PopupMenu { Name = "_file_actions" }; AddChild(_context, InternalMode.Back); _context.IDPressed += ContextAction;
        _overwrite = new ConfirmationDialog { Name = "_overwrite", DialogText = "The destination already exists. Replace it?" }; AddChild(_overwrite, InternalMode.Back); _overwrite.Confirmed += FinishPendingSave;
        _mkdir = new ConfirmationDialog { Name = "_create_folder", Title = "Create Folder" }; AddChild(_mkdir, InternalMode.Back); _folderName = new LineEdit { Name = "FolderName" }; _mkdir.AddChild(_folderName); _mkdir.RegisterTextEnter(_folderName); _mkdir.Confirmed += CreateFolder;
        _delete = new ConfirmationDialog { Name = "_trash", Title = "Move to Trash", DialogText = "Move the selected entry to the trash?" }; AddChild(_delete, InternalMode.Back); _delete.Confirmed += TrashSelected;
        _error = new AcceptDialog { Name = "_file_error", Title = "File operation failed" }; AddChild(_error, InternalMode.Back);
        RebuildFilters(); ApplyFeatures(); PushHistory();
    }
    private Button Tool(string caption, Action action) { var button = new Button(caption) { Name = caption.Replace(" ", "") }; _toolbar.AddChild(button); button.Pressed += action; return button; }
    /// <summary>Occurs after a valid file is accepted, with the path in the selected access scope.</summary>
    public event Action<string>? FileSelected;
    /// <summary>Occurs after a directory is accepted, with the scoped path.</summary>
    public event Action<string>? DirSelected;
    /// <summary>Occurs after multiple files are accepted, preserving current selected index order.</summary>
    public event Action<IReadOnlyList<string>>? FilesSelected;
    /// <summary>Occurs after the filename substring filter changes, with the committed new text.</summary>
    public event Action<string>? FilenameFilterChanged;
    /// <summary>Gets or sets the directory access scope, resetting to the virtual root or filesystem working directory.</summary>
    /// <value>Resources initially.</value>
    public FileDialogAccess Access { get { CheckFileDialog(); return _access; } set { EnsureMutable(); if (value is < FileDialogAccess.Resources or > FileDialogAccess.FileSystem) throw new ArgumentOutOfRangeException(nameof(value)); if (_access == value) return; var next = DirAccess.Open(value == FileDialogAccess.Resources ? "res://" : value == FileDialogAccess.UserData ? "user://" : IOPath.GetFullPath(".")); _directory.Dispose(); _directory = next; _access = value; _root = ""; _rootSubfolderText = ""; _history.Clear(); _historyIndex = -1; CurrentFile = ""; PushHistory(); Invalidate(); } }
    /// <summary>Gets or sets the file/directory selection mode.</summary>
    /// <value>SaveFile initially.</value>
    public FileDialogMode FileMode { get { CheckFileDialog(); return _mode; } set { EnsureMutable(); if (value is < FileDialogMode.OpenFile or > FileDialogMode.SaveFile) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; if (_modeTitles) UpdateModeTitle(); ApplyFeatures(); Invalidate(); } }
    /// <summary>Gets or sets the file display arrangement.</summary>
    /// <value>Thumbnails initially.</value>
    public DisplayModeType DisplayMode { get { CheckFileDialog(); return _display; } set { EnsureMutable(); if (value is < DisplayModeType.Thumbnails or > DisplayModeType.List) throw new ArgumentOutOfRangeException(nameof(value)); _display = value; ApplyFeatures(); Invalidate(); } }
    /// <summary>Gets or sets whether mode changes update the title and default OK caption.</summary>
    /// <value>True initially.</value>
    public bool ModeOverridesTitle { get { CheckFileDialog(); return _modeTitles; } set { EnsureMutable(); _modeTitles = value; if (value) UpdateModeTitle(); } }
    /// <summary>Gets or sets the current scoped directory; relative paths resolve from the current directory.</summary>
    /// <value>The selected access root initially.</value>
    public string CurrentDir { get { CheckFileDialog(); return _directory.GetCurrentDir(); } set { EnsureMutable(); ChangeDirectory(value, true); } }
    /// <summary>Gets or sets the file name shown in the borrowed filename field.</summary>
    /// <value>Empty initially.</value>
    public string CurrentFile { get { CheckFileDialog(); return _filenameEdit.Text; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _filenameEdit.Text = value; RefreshOK(); } }
    /// <summary>Gets or sets the scoped path formed from the current directory and filename.</summary>
    /// <value>The current directory with a trailing separator when filename is empty.</value>
    public string CurrentPath { get { CheckFileDialog(); return Join(CurrentDir, CurrentFile); } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var slash = value.Replace('\\', '/').LastIndexOf('/'); if (slash >= 0) CurrentDir = value[..(slash + 1)]; CurrentFile = slash < 0 ? value : value[(slash + 1)..]; } }
    /// <summary>Gets or sets a directory navigation boundary within the selected scope.</summary>
    /// <value>Empty initially. Requests with this boundary use the custom browser until native-file-extra is available.</value>
    public string RootSubfolder { get { CheckFileDialog(); return _rootSubfolderText; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var old = _root; _root = ""; try { if (value.Length != 0) { ChangeDirectory(value, false); _root = CurrentDir; } } catch { _root = old; throw; } _rootSubfolderText = value; _history.Clear(); _historyIndex = -1; PushHistory(); Invalidate(); } }
    /// <summary>Gets or sets whether hidden entries are listed.</summary>
    /// <value>False initially.</value>
    public bool ShowHiddenFiles { get { CheckFileDialog(); return _showHidden; } set { EnsureMutable(); _showHidden = value; _directory.IncludeHidden = value; ApplyFeatures(); Invalidate(); } }
    /// <summary>Gets or sets the case-insensitive filename substring filter.</summary>
    /// <value>Empty initially; changed writes emit FilenameFilterChanged with the new text.</value>
    public string FilenameFilter { get { CheckFileDialog(); return _filenameFilterText; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); SetFilenameFilter(value); } }
    /// <summary>Gets or sets copied type filters in pattern;description;MIME syntax.</summary>
    /// <value>An empty ordered list initially; All Files remains available.</value>
    public string[] Filters { get { CheckFileDialog(); return (string[])_filters.Clone(); } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var copy = value.ToArray(); foreach (var filter in copy) ValidateFilter(filter); _filters = copy; RebuildFilters(); Invalidate(); } }
    /// <summary>Gets or sets preference for a supported OS-native chooser.</summary>
    /// <value>False initially. Scoped/options/root-boundary requests fall back to this browser when native-file-extra is unavailable.</value>
    public bool UseNativeDialog { get { CheckFileDialog(); return _native; } set { EnsureMutable(); _native = value; if (Visible && CanUseNative()) { Hide(); PresentNative(); } } }
    /// <summary>Returns the borrowed main content column for caller-authored controls.</summary>
    /// <returns>The stable owned VBoxContainer.</returns>
    public VBoxContainer GetVBox() { CheckFileDialog(); return _vbox; }
    /// <summary>Returns the borrowed filename input used by acceptance and validation.</summary>
    /// <returns>The stable owned LineEdit.</returns>
    public LineEdit GetLineEdit() { CheckFileDialog(); return _filenameEdit; }
    /// <summary>Clears the filename substring filter.</summary>
    public void ClearFilenameFilter() => FilenameFilter = "";
    /// <summary>Clears file-type filters.</summary>
    public void ClearFilters() => Filters = Array.Empty<string>();
    /// <summary>Adds one file-type filter.</summary>
    /// <param name="filter">Comma-separated glob patterns.</param><param name="description">Optional display description.</param><param name="mimeType">Optional comma-separated native MIME hints.</param>
    public void AddFilter(string filter, string description = "", string mimeType = "") { EnsureMutable(); ArgumentNullException.ThrowIfNull(description); ArgumentNullException.ThrowIfNull(mimeType); ValidateFilter(filter); if (filter.StartsWith('.')) throw new ArgumentException("Filter must include a filename pattern before its extension.", nameof(filter)); Filters = [.. _filters, mimeType.Length != 0 ? filter + " ; " + description + " ; " + mimeType : description.Length != 0 ? filter + " ; " + description : filter]; }
    /// <summary>Clears selection without changing file records.</summary>
    public void DeselectAll() { EnsureMutable(); _files.DeselectAll(); RefreshOK(); }
    /// <summary>Marks listing and appearance for refresh; visible custom dialogs refresh immediately.</summary>
    public void Invalidate() { EnsureMutable(); _dirty = true; if (Visible && !_nativePending) RefreshBrowser(); }
    /// <summary>Shows this dialog centered using its configured size, selecting the editable filename.</summary>
    public void PopupFileDialog() { EnsureMutable(); if (CanUseNative()) PresentNative(); else { PopupCentered(); _filenameEdit.SelectAll(); if (_mode == FileDialogMode.SaveFile) _filenameEdit.GrabFocus(); else _files.GrabFocus(); } }
    private void CheckFileDialog() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static string Join(string directory, string name) => directory.EndsWith("://", StringComparison.Ordinal) ? directory + name : directory.TrimEnd('/', '\\') + "/" + name;
    private static string ParentPath(string path) { var slash = path.Replace('\\', '/').LastIndexOf('/'); if (slash < 0) return "."; if (slash == 0) return "/"; if (path[..slash].EndsWith(":/", StringComparison.Ordinal)) return path[..(slash + 1)]; return path[..slash]; }
    private void BeginCreateFolder() { _mkdir.PopupCentered(new(320, 130)); _folderName.GrabFocus(); }
    private static void ValidateFilter(string filter) { ArgumentNullException.ThrowIfNull(filter); if (filter.Contains('\0')) throw new ArgumentException("Filter contains a null character.", nameof(filter)); }
    private void UpdateModeTitle() { Title = _mode switch { FileDialogMode.OpenFile => "Open a File", FileDialogMode.OpenFiles => "Open Files", FileDialogMode.OpenDirectory => "Open a Directory", FileDialogMode.OpenAny => "Open a File or Directory", _ => "Save a File" }; SetDefaultOKText(_mode == FileDialogMode.SaveFile ? "Save" : _mode == FileDialogMode.OpenDirectory ? "Select Current Folder" : "Open"); }
}
