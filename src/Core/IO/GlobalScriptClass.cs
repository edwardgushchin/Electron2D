namespace Electron2D;

/// <summary>Describes a registered global C# script class in the project.</summary>
public sealed class GlobalScriptClass
{
    internal GlobalScriptClass(string name, Type? baseType, string iconPath, string path, bool isAbstract, bool isTool)
    {
        Name = name;
        BaseType = baseType;
        IconPath = iconPath;
        Path = path;
        IsAbstract = isAbstract;
        IsTool = isTool;
    }

    /// <summary>Gets the declared global class name.</summary>
    public string Name { get; }
    /// <summary>Gets the nearest global project base, engine class or registered extension base, when present.</summary>
    public Type? BaseType { get; }
    /// <summary>Gets the script language name.</summary>
    public string Language => "C#";
    /// <summary>Gets the project-relative source path accepted by ResourceLoader.</summary>
    public string Path { get; }
    /// <summary>Gets the project-relative icon path, or an empty string when no project icon is declared.</summary>
    public string IconPath { get; }
    /// <summary>Gets whether the compiled class is abstract.</summary>
    public bool IsAbstract { get; }
    /// <summary>Gets whether the class carries a direct or inherited tool declaration.</summary>
    public bool IsTool { get; }
}

/// <summary>Declares the icon path shown for a global C# class in project tools.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IconAttribute : Attribute
{
    /// <summary>Declares an icon path relative to the script or to the project resource root.</summary>
    /// <param name="path">The icon path.</param>
    public IconAttribute(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.Contains('\0')) throw new ArgumentException("An icon path cannot contain a null character.", nameof(path));
        Path = path;
    }

    /// <summary>Gets the declared icon path.</summary>
    public string Path { get; }
}
