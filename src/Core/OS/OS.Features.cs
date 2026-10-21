using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Reports a built-in host, active service or custom project feature.</summary>
    /// <param name="tagName">Feature tag; matching follows the project's case-insensitive feature policy.</param>
    /// <returns>True for a supported tag. Unknown, empty and malformed tags return false.</returns>
    /// <remarks>Host tags share ProjectSettings' built-in feature set. Movie and renderer tags follow the current
    /// session; querying never opens a renderer. The first system_fonts query may initialize the host font catalog.
    /// Feature availability is not a performance guarantee or a project-setting override.</remarks>
    /// <exception cref="ArgumentNullException">The tag is null.</exception>
    public static bool HasFeature(string tagName)
    {
        ArgumentNullException.ThrowIfNull(tagName);
        if (ProjectSettings.Service.HasFeatureQueryCore(tagName)) return true;
        if (tagName.Equals("movie", StringComparison.OrdinalIgnoreCase)) return Engine.GetWriteMoviePath().Length != 0;
        if (tagName.Equals("system_fonts", StringComparison.OrdinalIgnoreCase)) return FontCatalog.IsAvailable;
        return RenderingServer.QueryOSFeature(tagName);
    }
    internal static void AddHostFeatures(HashSet<string> features)
    {
        features.Add("dotnet"); features.Add("csharp"); features.Add("single"); features.Add("template");
        features.Add(IntPtr.Size == 8 ? "64" : "32");
        features.Add(OperatingSystem.IsBrowser() ? "nothreads" : "threads");
#if DEBUG
        features.Add("debug"); features.Add("template_debug");
#else
        features.Add("release"); features.Add("template_release");
#endif
        if (OperatingSystem.IsAndroid()) { features.Add("android"); features.Add("mobile"); }
        else if (OperatingSystem.IsIOS()) { features.Add("ios"); features.Add("mobile"); }
        else if (OperatingSystem.IsTvOS()) { features.Add("tvos"); features.Add("mobile"); }
        else if (OperatingSystem.IsBrowser()) features.Add("web");
        else if (OperatingSystem.IsLinux()) { features.Add("linux"); features.Add("linuxbsd"); features.Add("pc"); }
        else if (OperatingSystem.IsWindows()) { features.Add("windows"); features.Add("pc"); }
        else if (OperatingSystem.IsMacOS()) { features.Add("macos"); features.Add("pc"); }
        var architecture = RuntimeInformation.ProcessArchitecture;
        features.Add(architecture switch { Architecture.X64 => "x86_64", Architecture.X86 => "x86_32", Architecture.Arm64 => "arm64", Architecture.Arm => "arm32", _ => architecture.ToString().ToLowerInvariant() });
        if (architecture is Architecture.X64 or Architecture.X86) features.Add("x86");
        if (architecture is Architecture.Arm or Architecture.Arm64) features.Add("arm");
        if (architecture == Architecture.Wasm) features.Add("wasm");
    }
}
