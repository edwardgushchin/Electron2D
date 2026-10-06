namespace Electron2D;

/// <summary>Selects the filesystem scope browsed by a file dialog.</summary>
public enum FileDialogAccess
{
    /// <summary>Browses the configured project resource root.</summary>
    Resources = 0,
    /// <summary>Browses the configured application user-data root.</summary>
    UserData = 1,
    /// <summary>Browses ordinary filesystem paths and drives.</summary>
    FileSystem = 2
}
