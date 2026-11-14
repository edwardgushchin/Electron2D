namespace Electron2D;

/// <summary>Describes a component recorded in the Electron2D distribution catalog.</summary>
public sealed class EngineCopyrightInfo
{
    internal EngineCopyrightInfo(string name, EngineCopyrightPart[] parts)
    {
        Name = name;
        Parts = Array.AsReadOnly(parts);
    }

    /// <summary>Gets the distribution component name.</summary>
    public string Name { get; }

    /// <summary>Gets the immutable source scopes, attribution notices and license references.</summary>
    public IReadOnlyList<EngineCopyrightPart> Parts { get; }
}

/// <summary>Describes a distribution component's source scope and retained legal attribution.</summary>
public sealed class EngineCopyrightPart
{
    internal EngineCopyrightPart(string[] files, string[] copyright, string license)
    {
        Files = Array.AsReadOnly(files);
        Copyright = Array.AsReadOnly(copyright);
        License = license;
    }

    /// <summary>Gets immutable repository-relative file paths or glob patterns covered by this part.</summary>
    /// <remarks>Native/runtime notice files identify distributed dependency terms; they do not report loaded modules.</remarks>
    public IReadOnlyList<string> Files { get; }

    /// <summary>Gets immutable verbatim attribution blocks from the associated legal document or recorded source headers.</summary>
    /// <remarks>Empty when the recorded sources supply license terms without a specific attribution.</remarks>
    public IReadOnlyList<string> Copyright { get; }

    /// <summary>Gets the unique document key in <see cref="Engine.GetLicenseInfo"/>.</summary>
    /// <remarks>A document may retain several licenses, alternatives or additional component terms.</remarks>
    public string License { get; }
}
