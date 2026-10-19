namespace Electron2D;

/// <summary>Provides process-wide discovery of the retained native text implementation.</summary>
/// <remarks>Interface lookup does not initialize native fonts. The manager and interface cannot be disposed.</remarks>
public sealed class TextServerManager : ElectronObject
{
    internal static TextServerManager Service { get; } = new();
    private readonly TextServer _primary = new TextServer.Builtin();
    private TextServerManager() { }
    /// <summary>Returns the primary text implementation.</summary><returns>The retained interface.</returns>
    public static TextServer GetPrimaryInterface() => Service._primary;
    /// <summary>Returns the number of registered interfaces.</summary><returns>One built-in implementation.</returns>
    public static int GetInterfaceCount() => 1;
    /// <summary>Returns an interface by its current index.</summary><param name="index">Zero-based index.</param><returns>The retained implementation.</returns>
    public static TextServer GetInterface(int index) => index == 0 ? Service._primary : throw new ArgumentOutOfRangeException(nameof(index));
    /// <summary>Returns registered interfaces in current index order.</summary><returns>A caller-owned array of retained implementation objects; each supplies its name.</returns>
    public static TextServer[] GetInterfaces() => [Service._primary];
    /// <summary>Finds an interface by its exact name.</summary><param name="name">Case-sensitive name.</param><returns>The retained implementation, or null.</returns>
    public static TextServer? FindInterface(string name) { ArgumentNullException.ThrowIfNull(name); return name == Service._primary.GetName() ? Service._primary : null; }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-wide TextServerManager cannot be disposed.");
}
