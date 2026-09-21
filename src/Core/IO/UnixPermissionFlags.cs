namespace Electron2D;

/// <summary>Represents Unix file permission and special-mode bits.</summary>
[Flags]
public enum UnixPermissionFlags
{
    /// <summary>No permission bits are set.</summary>
    None = 0,

    /// <summary>Other users may execute the file or search the directory.</summary>
    ExecuteOther = 1,

    /// <summary>Other users may write the file or directory.</summary>
    WriteOther = 2,

    /// <summary>Other users may read the file or directory.</summary>
    ReadOther = 4,

    /// <summary>Group members may execute the file or search the directory.</summary>
    ExecuteGroup = 8,

    /// <summary>Group members may write the file or directory.</summary>
    WriteGroup = 16,

    /// <summary>Group members may read the file or directory.</summary>
    ReadGroup = 32,

    /// <summary>The owner may execute the file or search the directory.</summary>
    ExecuteOwner = 64,

    /// <summary>The owner may write the file or directory.</summary>
    WriteOwner = 128,

    /// <summary>The owner may read the file or directory.</summary>
    ReadOwner = 256,

    /// <summary>Restricts deletion or renaming in a directory to owners and privileged users.</summary>
    RestrictedDelete = 512,

    /// <summary>Uses the directory group for new entries or applies the file group identity on execution.</summary>
    SetGroupId = 1024,

    /// <summary>Applies the file owner identity on execution.</summary>
    SetUserId = 2048
}
