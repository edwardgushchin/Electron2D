namespace Electron2D;

/// <summary>Provides the portable shader fragments shared by built-in rendering and project imports.</summary>
/// <remarks>Source is embedded in the runtime and available before renderer startup. The import tool makes
/// the same files available to native HLSL/GLSL preprocessing. This immutable portable library is independent
/// of the selected graphics driver; drawing a consuming shader retains its backend capability requirements.</remarks>
public sealed class ShaderIncludeDB : ElectronObject
{
    private static readonly ShaderIncludeDB SharedInstance = new();
    private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
    private readonly string[] _names = ["electron2d/color.glslinc", "electron2d/color.hlslinc"];
    private ShaderIncludeDB()
    {
        foreach (var name in _names)
        {
            using var stream = typeof(ShaderIncludeDB).Assembly.GetManifestResourceStream("Electron2D.ShaderIncludes." + name)
                ?? throw new InvalidOperationException("The built-in shader include payload is missing.");
            using var reader = new StreamReader(stream, ShaderInclude.SourceEncoding, detectEncodingFromByteOrderMarks: false);
            _sources.Add(name, reader.ReadToEnd());
        }
    }

    /// <summary>Returns the source of a registered built-in shader fragment.</summary>
    /// <param name="filename">Literal, case-sensitive include name using forward slashes.</param>
    /// <returns>The immutable source, or an empty string when the name is not registered.</returns>
    /// <remarks>Use the same name in a shader's include directive. Lookup never reads caller files or compiles source.</remarks>
    /// <exception cref="ArgumentNullException">The filename is null.</exception>
    /// <exception cref="ObjectDisposedException">The retained library has been disposed.</exception>
    public static string GetBuiltInIncludeFile(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename); SharedInstance.ThrowIfDisposed();
        return SharedInstance._sources.GetValueOrDefault(filename, "");
    }

    /// <summary>Determines whether the portable library contains an exact include name.</summary>
    /// <param name="filename">Literal, case-sensitive include name.</param>
    /// <returns>True for a registered name; false for an empty, unknown or differently cased name.</returns>
    /// <exception cref="ArgumentNullException">The filename is null.</exception>
    /// <exception cref="ObjectDisposedException">The retained library has been disposed.</exception>
    public static bool HasBuiltInIncludeFile(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename); SharedInstance.ThrowIfDisposed();
        return SharedInstance._sources.ContainsKey(filename);
    }

    /// <summary>Copies the registered names in ordinal order.</summary>
    /// <returns>An independent array of the available HLSL/GLSL include names.</returns>
    /// <exception cref="ObjectDisposedException">The retained library has been disposed.</exception>
    public static string[] ListBuiltInIncludeFiles()
    { SharedInstance.ThrowIfDisposed(); return (string[])SharedInstance._names.Clone(); }
}
