namespace Electron2D;

/// <summary>Specifies the operations permitted by an opened <see cref="FileAccess"/>.</summary>
[Flags]
public enum FileAccessModeFlags
{
    /// <summary>Opens an existing file for reading from its beginning.</summary>
    Read = 1,

    /// <summary>Creates or truncates a file and opens it for writing from its beginning.</summary>
    /// <remarks>The default-disabled OS save-and-swap policy delays destination replacement until close for this exact mode.</remarks>
    Write = 2,

    /// <summary>Opens an existing file for reading and writing without truncating it.</summary>
    ReadWrite = Read | Write,

    /// <summary>Creates or truncates a file and opens it for reading and writing.</summary>
    WriteRead = 7
}
