namespace Electron2D;

public sealed partial class OS
{
    private int _fileSaveAndSwap;
    internal static bool UseFileAccessSaveAndSwap => Volatile.Read(ref Service._fileSaveAndSwap) != 0;
    /// <summary>Enables or disables staged replacement for subsequently opened write-only FileAccess files.</summary>
    /// <param name="enabled">True to write a unique sibling and replace its target on close; false for direct writing, initially.</param>
    /// <remarks>Thread-safe process policy; existing streams retain their selected behavior. Only the exact Write mode
    /// participates. ReadWrite/WriteRead, process pipes and existing atomic transformed/configuration persistence retain
    /// their contracts. Flush persists staging data without publishing it. Close or disposal replaces the destination;
    /// an ordinary caller exception does not cancel a successful write. Underlying write/resize/flush failures abort it.
    /// Symbolic links retain their identity and select their resolved target at open. AbsolutePath identifies staging.
    /// Failed final replacement leaves completed staging data available at that path for caller recovery. This is blocking
    /// filesystem work and does not guarantee survival of filesystem/power failure or concurrent external path changes.</remarks>
    public static void SetUseFileAccessSaveAndSwap(bool enabled) => Volatile.Write(ref Service._fileSaveAndSwap, enabled ? 1 : 0);
}
