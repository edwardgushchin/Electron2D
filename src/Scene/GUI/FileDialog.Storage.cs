namespace Electron2D;

public partial class FileDialog
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(DialogText)) continue;
            if (property.Name == nameof(Title)) yield return new PropertyDescriptor<FileDialog, string>(nameof(Title), d => d.Title, (d, v) => d.Title = v, _ => "Save a File", stored: true);
            else if (property.Name == nameof(Size)) yield return new PropertyDescriptor<FileDialog, Vector2i>(nameof(Size), d => d.Size, (d, v) => d.Size = v, _ => new(640, 360), stored: true);
            else if (property.Name == nameof(DialogHideOnOK)) yield return new PropertyDescriptor<FileDialog, bool>(nameof(DialogHideOnOK), d => d.DialogHideOnOK, (d, v) => d.DialogHideOnOK = v, _ => false, stored: true);
            else if (property.Name == nameof(ShortcutInputEnabled)) yield return new PropertyDescriptor<FileDialog, bool>(nameof(ShortcutInputEnabled), d => d.ShortcutInputEnabled, (d, v) => d.ShortcutInputEnabled = v, _ => true, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<FileDialog, FileDialogAccess>(nameof(Access), d => d.Access, (d, v) => d.Access = v, _ => FileDialogAccess.Resources, stored: true);
        yield return new PropertyDescriptor<FileDialog, bool>(nameof(ModeOverridesTitle), d => d.ModeOverridesTitle, (d, v) => d.ModeOverridesTitle = v, _ => true, stored: true);
        yield return new PropertyDescriptor<FileDialog, FileDialogMode>(nameof(FileMode), d => d.FileMode, (d, v) => d.FileMode = v, _ => FileDialogMode.SaveFile, stored: true);
        yield return new PropertyDescriptor<FileDialog, DisplayModeType>(nameof(DisplayMode), d => d.DisplayMode, (d, v) => d.DisplayMode = v, _ => DisplayModeType.Thumbnails, stored: true);
        yield return new PropertyDescriptor<FileDialog, string>(nameof(RootSubfolder), d => d.RootSubfolder, (d, v) => d.RootSubfolder = v, _ => "", stored: true);
        yield return new PropertyDescriptor<FileDialog, bool>(nameof(ShowHiddenFiles), d => d.ShowHiddenFiles, (d, v) => d.ShowHiddenFiles = v, _ => false, stored: true);
        yield return new PropertyDescriptor<FileDialog, string>(nameof(FilenameFilter), d => d.FilenameFilter, (d, v) => d.FilenameFilter = v, _ => "", stored: true);
        yield return new PropertyDescriptor<FileDialog, string[]>(nameof(Filters), d => d.Filters, (d, v) => d.Filters = v, _ => Array.Empty<string>(), stored: true);
        yield return new PropertyDescriptor<FileDialog, bool>(nameof(UseNativeDialog), d => d.UseNativeDialog, (d, v) => d.UseNativeDialog = v, _ => false, stored: true);
        for (var i = 0; i < _custom.Length; i++)
        {
            var flag = (Customization)i;
            yield return new PropertyDescriptor<FileDialog, bool>(flag switch { Customization.HiddenFiles => nameof(HiddenFilesToggleEnabled), Customization.CreateFolder => nameof(FolderCreationEnabled), Customization.FileFilter => nameof(FileFilterToggleEnabled), Customization.FileSort => nameof(FileSortOptionsEnabled), Customization.Favorites => nameof(FavoritesEnabled), Customization.Recent => nameof(RecentListEnabled), Customization.Layout => nameof(LayoutToggleEnabled), Customization.OverwriteWarning => nameof(OverwriteWarningEnabled), _ => nameof(DeletingEnabled) }, d => d.IsCustomizationFlagEnabled(flag), (d, v) => d.SetCustomizationFlagEnabled(flag, v), _ => true, stored: true);
        }
        yield return new PropertyDescriptor<FileDialog, int>(nameof(OptionCount), d => d.OptionCount, (d, v) => d.OptionCount = v, _ => 0, stored: true);
        for (var index = 0; index < _options.Count; index++)
        {
            var i = index;
            yield return new PropertyDescriptor<FileDialog, string>($"option_{i}/name", d => d.GetOptionName(i), (d, v) => d.SetOptionName(i, v), _ => "", stored: true);
            yield return new PropertyDescriptor<FileDialog, string[]>($"option_{i}/values", d => d.GetOptionValues(i), (d, v) => d.SetOptionValues(i, v), _ => Array.Empty<string>(), stored: true);
            yield return new PropertyDescriptor<FileDialog, int>($"option_{i}/default", d => d.GetOptionDefault(i), (d, v) => d.SetOptionDefault(i, v), _ => 0, stored: true);
        }
    }
}
