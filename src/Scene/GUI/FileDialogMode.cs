namespace Electron2D;

/// <summary>Selects the file/directory opening or saving contract.</summary>
public enum FileDialogMode
{
    /// <summary>Selects one existing file.</summary>
    OpenFile = 0,
    /// <summary>Selects multiple existing files.</summary>
    OpenFiles = 1,
    /// <summary>Selects one directory.</summary>
    OpenDirectory = 2,
    /// <summary>Selects either a file or a directory; native backend support is queried separately.</summary>
    OpenAny = 3,
    /// <summary>Selects a destination file, which need not already exist.</summary>
    SaveFile = 4,
}
