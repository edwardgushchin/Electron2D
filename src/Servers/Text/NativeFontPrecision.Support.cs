using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    private HashSet<uint> ReadSupportedScripts()
    {
        var result = new HashSet<uint>();
        foreach (var table in new uint[] { 0x47535542, 0x47504F53 })
        {
            uint count = 0; var total = HBOTLayoutTableGetScriptTags(_hbFace, table, 0, ref count, null); if (total == 0) continue;
            if (total > 65535) throw new InvalidDataException("The font script table exceeds the OpenType count range.");
            var tags = new uint[total]; count = total; fixed (uint* values = tags) HBOTLayoutTableGetScriptTags(_hbFace, table, 0, ref count, values);
            if (count > total) throw new InvalidDataException("The font script table changed during metadata capture.");
            for (var i = 0; i < count; i++) { var script = HBOTTagToScript(tags[i]); if (script != 0) result.Add(script); }
        }
        return result;
    }
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_layout_table_get_script_tags"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint HBOTLayoutTableGetScriptTags(nint face, uint table, uint startOffset, ref uint count, uint* tags);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_tag_to_script"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint HBOTTagToScript(uint tag);
}
