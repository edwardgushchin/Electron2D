using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class OGVNativeMemory
{
    private static readonly object Gate = new();
    private static bool _ready;
    internal static void Prepare()
    {
        if (Volatile.Read(ref _ready)) return;
        lock (Gate)
        {
            if (_ready) return;
            var functions = NativeAllocation.GetFunctions();
            try
            {
                if (Install(in functions) != 0) throw new InvalidOperationException("The native video allocator has another owner.");
            }
            catch (EntryPointNotFoundException error)
            { throw new PlatformNotSupportedException("Video allocation accounting requires the matching source-built native bundle.", error); }
            Volatile.Write(ref _ready, true);
        }
    }
    [LibraryImport(NativeLibraries.OGVLibrary, EntryPoint = "e2d_ogv_memory"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Install(in NativeAllocation.Functions functions);
}
