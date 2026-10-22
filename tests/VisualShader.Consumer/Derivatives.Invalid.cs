using System.Runtime.InteropServices;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void DerivativeBytecodeChecks(string folder)
    {
        using var fine = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "derivative-0-0-2-2-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var basic = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "derivative-0-0-0-2-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        var vertex = fine.GetSPIRV(RenderingDevice.ShaderStage.Vertex);
        var fragment = fine.GetSPIRV(RenderingDevice.ShaderStage.Fragment);
        using var target = new Shader(); target.SetSPIRV(vertex, fragment);
        static List<uint[]> Instructions(byte[] code)
        {
            var words = MemoryMarshal.Cast<byte, uint>(code); var result = new List<uint[]> { words[..5].ToArray() };
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) result.Add(words.Slice(at, (int)(words[at] >> 16)).ToArray());
            return result;
        }
        static byte[] Bytes(List<uint[]> instructions) => MemoryMarshal.AsBytes(instructions.SelectMany(w => w).ToArray().AsSpan()).ToArray();
        var checks = 0;
        void RejectModule(byte[] code, bool isVertex, string message)
        {
            try { target.SetSPIRV(isVertex ? code : vertex, isVertex ? fragment : code); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException)
            {
                Check(error.Message.Contains(message, StringComparison.Ordinal), "Derivative bytecode reaches its specific validation gate: " + error.Message);
                Check(target.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex) && target.GetSPIRV(RenderingDevice.ShaderStage.Fragment).SequenceEqual(fragment), "Invalid derivative bytecode preserves both previously published stages.");
                checks++; return;
            }
            throw new InvalidOperationException("Expected derivative bytecode rejection.");
        }
        var missing = Instructions(fragment); Check(missing.RemoveAll(i => i.Length == 2 && (i[0] & 0xffff) == 17 && i[1] == 51) == 1, "Fine module declares derivative control.");
        RejectModule(Bytes(missing), false, "require the derivative-control capability");
        var unsupported = Instructions(fragment); unsupported.First(i => i.Length == 2 && (i[0] & 0xffff) == 17 && i[1] == 51)[1] = 52;
        RejectModule(Bytes(unsupported), false, "require supported graphics");
        var malformed = Instructions(fragment); var index = malformed.FindIndex(i => (i[0] & 0xffff) == 212); Check(index > 0, "Fine fwidth instruction exists.");
        malformed[index] = malformed[index][..3]; malformed[index][0] = (3u << 16) | 212;
        RejectModule(Bytes(malformed), false, "Invalid derivative instruction");
        var wrongCapability = Instructions(vertex); wrongCapability.Insert(1, [(2u << 16) | 17u, 51u]);
        RejectModule(Bytes(wrongCapability), true, "require supported graphics");
        var wrongStage = Instructions(basic.GetSPIRV(RenderingDevice.ShaderStage.Fragment)); wrongStage.First(i => (i[0] & 0xffff) == 15)[1] = 0;
        RejectModule(Bytes(wrongStage), true, "derivatives require the fragment stage");
        Check(checks == 5, "Every malformed/stage derivative case was exercised.");
        Console.WriteLine("Five derivative capability/instruction/stage failures preserved both shader stages.");
    }
}
