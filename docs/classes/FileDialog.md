# FileDialog

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.FileDialog`. **Inherits:** [ConfirmationDialog](ConfirmationDialog.md). **Inherited By:** —. **Source:** [source](../../src/Scene/GUI/FileDialog.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Provides real scoped file/directory selection and save destinations through an owned embedded browser. See [file dialogs](../components/file-dialogs.md) for filtering, options, menus, defaults, lifecycle and precise native/platform limits. CurrentDir resolves relative paths; RootSubfolder blocks lexical navigation outside its canonical directory. Selection events run after custom hiding; cancel clears the filename. Save emits a destination without writing contents. Returned controls and icon/thumbnail textures are borrowed.

`GetSelectedOptions()` returns copied typed checkbox/index results. Shared favorites/recents and providers belong to the first authoring thread; attached operations require the scene owner. Invalid enums/indexes/null strings and scope escapes throw; user-triggered filesystem errors open the owned error dialog. Failed observers remain aggregated while required refresh/hiding continues. Native pending requests prevent disposal.

```csharp
var root = new Window { Size = new(900, 600), GUIEmbedSubwindows = true };
var picker = new FileDialog { FileMode = FileDialogMode.OpenFile };
picker.AddFilter("*.e2dscene", "Scenes");
picker.FileSelected += path => Console.WriteLine(path);
root.AddChild(picker);
root.Ready += _ => picker.PopupFileDialog();
Engine.Run(root);
```

Own enum identities: [Customization](FileDialog.Customization.md) and [DisplayModeType](FileDialog.DisplayModeType.md). Shared modes use [FileDialogMode](FileDialogMode.md); access uses [FileDialogAccess](FileDialogAccess.md).

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public FileDialog()` | Creates a hidden 640 by 360 resource browser in save-file mode. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public Electron2D.FileDialogAccess Access { get; set; }` | Gets or sets the directory access scope, resetting to the virtual root or filesystem working directory. value: Resources initially. |
| `public System.String CurrentDir { get; set; }` | Gets or sets the current scoped directory; relative paths resolve from the current directory. value: The selected access root initially. |
| `public System.String CurrentFile { get; set; }` | Gets or sets the file name shown in the borrowed filename field. value: Empty initially. |
| `public System.String CurrentPath { get; set; }` | Gets or sets the scoped path formed from the current directory and filename. value: The current directory with a trailing separator when filename is empty. |
| `public System.Boolean DeletingEnabled { get; set; }` | Gets or sets the recoverable trash context command. value: True initially; native trash capability may reject an operation. |
| `public Electron2D.FileDialog.DisplayModeType DisplayMode { get; set; }` | Gets or sets the file display arrangement. value: Thumbnails initially. |
| `public System.Boolean FavoritesEnabled { get; set; }` | Gets or sets visibility of favorite-directory controls. value: True initially. |
| `public System.Boolean FileFilterToggleEnabled { get; set; }` | Gets or sets visibility of the filename-filter toggle. value: True initially. |
| `public Electron2D.FileDialogMode FileMode { get; set; }` | Gets or sets the file/directory selection mode. value: SaveFile initially. |
| `public System.Boolean FileSortOptionsEnabled { get; set; }` | Gets or sets visibility of the file ordering menu. value: True initially. |
| `public System.String FilenameFilter { get; set; }` | Gets or sets the case-insensitive filename substring filter. value: Empty initially; changed writes emit FilenameFilterChanged with the new text. |
| `public System.String[] Filters { get; set; }` | Gets or sets copied type filters in pattern;description;MIME syntax. value: An empty ordered list initially; All Files remains available. |
| `public System.Boolean FolderCreationEnabled { get; set; }` | Gets or sets folder creation commands in applicable modes. value: True initially. |
| `public System.Boolean HiddenFilesToggleEnabled { get; set; }` | Gets or sets visibility of the hidden-file toggle. value: True initially. |
| `public System.Boolean LayoutToggleEnabled { get; set; }` | Gets or sets visibility of the thumbnail/list switch. value: True initially. |
| `public System.Boolean ModeOverridesTitle { get; set; }` | Gets or sets whether mode changes update the title and default OK caption. value: True initially. |
| `public System.Int32 OptionCount { get; set; }` | Gets or sets the number of additional checkbox/choice controls. value: Zero initially; range 0..65536. |
| `public System.Boolean OverwriteWarningEnabled { get; set; }` | Gets or sets explicit confirmation for existing save destinations. value: True initially. |
| `public System.Boolean RecentListEnabled { get; set; }` | Gets or sets visibility of recent directories. value: True initially. |
| `public System.String RootSubfolder { get; set; }` | Gets or sets a directory navigation boundary within the selected scope. value: Empty initially. Requests with this boundary use the custom browser until native-file-extra is available. |
| `public System.Boolean ShowHiddenFiles { get; set; }` | Gets or sets whether hidden entries are listed. value: False initially. |
| `public System.Boolean UseNativeDialog { get; set; }` | Gets or sets preference for a supported OS-native chooser. value: False initially. Scoped/options/root-boundary requests fall back to this browser when native-file-extra is unavailable. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<System.String> DirSelected` | Occurs after a directory is accepted, with the scoped path. |
| `public event System.Action<System.String> FileSelected` | Occurs after a valid file is accepted, with the path in the selected access scope. |
| `public event System.Action<System.String> FilenameFilterChanged` | Occurs after the filename substring filter changes, with the committed new text. |
| `public event System.Action<System.Collections.Generic.IReadOnlyList<System.String>> FilesSelected` | Occurs after multiple files are accepted, preserving current selected index order. |

## Methods

| Complete declaration | Contract |
| --- | --- |
| `public System.Void AddFilter(System.String filter, System.String description = "", System.String mimeType = "")` | Adds one file-type filter. filter: Comma-separated glob patterns. description: Optional display description. mimeType: Optional comma-separated native MIME hints. |
| `public System.Void AddOption(System.String name, System.Collections.Generic.IReadOnlyList<System.String> values, System.Int32 defaultValueIndex)` | Adds a checkbox when values are empty, or a choice control otherwise. name: The option label/result key. values: Copied choice captions, or empty for a checkbox. defaultValueIndex: Initial index, clamped to the choice or checkbox range. |
| `public System.Void ClearFilenameFilter()` | Clears the filename substring filter. |
| `public System.Void ClearFilters()` | Clears file-type filters. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `public System.Void DeselectAll()` | Clears selection without changing file records. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `public static System.Collections.Generic.IReadOnlyList<System.String> GetFavoriteList()` | Returns a copied shared favorite-directory list on the authoring thread. returns: Ordered scoped directory paths normalized with trailing separators. |
| `public Electron2D.LineEdit GetLineEdit()` | Returns the borrowed filename input used by acceptance and validation. returns: The stable owned LineEdit. |
| `public System.Int32 GetOptionDefault(System.Int32 option)` | Gets a checkbox/choice's default value index. option: A nonnegative index. returns: The stored clamped index. |
| `public System.String GetOptionName(System.Int32 option)` | Gets the option label/result key. option: A nonnegative index. returns: The raw option name. |
| `public System.String[] GetOptionValues(System.Int32 option)` | Returns an independent copy of choice captions. option: A nonnegative index. returns: An empty list for a checkbox. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `public static System.Collections.Generic.IReadOnlyList<System.String> GetRecentList()` | Returns a copied shared recent-directory list on the authoring thread. returns: Ordered scoped directory paths normalized with trailing separators. |
| `public System.Collections.Generic.IReadOnlyDictionary<System.String, Electron2D.FileDialogOptionValue> GetSelectedOptions()` | Returns an owned read-only snapshot of current checkbox/choice results keyed by option name. returns: Typed results, populated when controls are prepared; duplicate keys use the last configured option. |
| `public Electron2D.VBoxContainer GetVBox()` | Returns the borrowed main content column for caller-authored controls. returns: The stable owned VBoxContainer. |
| `public System.Void Invalidate()` | Marks listing and appearance for refresh; visible custom dialogs refresh immediately. |
| `public System.Boolean IsCustomizationFlagEnabled(Electron2D.FileDialog.Customization flag)` | Reports whether one browser customization feature is enabled. flag: A defined feature. returns: The retained feature state. |
| `protected override System.Void OnCancelPressed()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnOKPressed()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnShortcutInput(Electron2D.InputEvent input)` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |
| `public System.Void PopupFileDialog()` | Shows this dialog centered using its configured size, selecting the editable filename. |
| `public System.Void SetCustomizationFlagEnabled(Electron2D.FileDialog.Customization flag, System.Boolean enabled)` | Changes one feature's actual control/context-menu visibility or validation behavior. flag: A defined feature. enabled: The new state. |
| `public static System.Void SetFavoriteList(System.Collections.Generic.IReadOnlyList<System.String> favorites)` | Replaces copied shared favorite directories on the authoring thread. favorites: Ordered scoped paths. |
| `public static System.Void SetGetIconCallback(System.Func<System.String, Electron2D.Texture> callback)` | Sets the shared file icon provider used by list layout. callback: A path-to-borrowed-texture provider, or null to use the theme. |
| `public static System.Void SetGetThumbnailCallback(System.Func<System.String, Electron2D.Texture> callback)` | Sets the shared thumbnail provider used by thumbnail layout. callback: A path-to-borrowed-texture provider, or null to use the theme. |
| `public System.Void SetOptionDefault(System.Int32 option, System.Int32 defaultValueIndex)` | Changes an option's clamped default and rebuilds visible controls. option: An index, optionally counted from the end. defaultValueIndex: The requested default. |
| `public System.Void SetOptionName(System.Int32 option, System.String name)` | Changes an option label/result key and rebuilds visible controls. option: An index, optionally counted from the end. name: The nonnull new key. |
| `public System.Void SetOptionValues(System.Int32 option, System.Collections.Generic.IReadOnlyList<System.String> values)` | Changes copied choice values and clamps the retained default. option: An index, optionally counted from the end. values: Captions or empty for a checkbox. |
| `public static System.Void SetRecentList(System.Collections.Generic.IReadOnlyList<System.String> recents)` | Replaces copied shared recent directories on the authoring thread. recents: Ordered scoped paths. |
| `protected override System.Void ValidateDisposal()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
