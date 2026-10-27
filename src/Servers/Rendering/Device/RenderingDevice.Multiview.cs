using System.Runtime.InteropServices;
using System.Text;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static byte[] LowerViewIndex(byte[] code, uint view, ShaderStage stage)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        var instructions = new List<uint[]>();
        var types = new Dictionary<uint, uint[]>();
        var builtins = new HashSet<uint>();
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16);
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated view-index instruction.", nameof(code));
            var item = words.Slice(at, count).ToArray(); var op = item[0] & 65535;
            instructions.Add(item);
            if (op is >= 19 and <= 33) types.Add(item[1], item);
            if (op == 71 && count == 4 && item[2] == 11 && item[3] == 4440) builtins.Add(item[1]);
            if (op == 72 && count == 5 && item[3] == 11 && item[4] == 4440) return LowerViewIndex(FlattenViewBlocks(code), view, stage);
            at += count;
        }
        if (builtins.Count == 0) return code;
        if (stage == ShaderStage.Compute) throw new ArgumentException("A compute shader cannot consume a view index.", nameof(code));
        var declarations = new Dictionary<uint, uint[][]>(); var replacements = new Dictionary<uint, uint[]>(); var next = words[3];
        foreach (var item in instructions)
        {
            if ((item[0] & 65535) != 59 || !builtins.Contains(item[2])) continue;
            if (item.Length != 4 || item[3] != 1 || !types.TryGetValue(item[1], out var pointer) || pointer.Length != 4 || pointer[2] != 1 || !types.TryGetValue(pointer[3], out var scalar) || (scalar[0] & 65535) != 21 || scalar.Length != 4 || scalar[2] != 32)
                throw new ArgumentException("A view index requires an input 32-bit scalar.", nameof(code));
            var type = next++; var value = next++;
            declarations.Add(item[2], [[(4u << 16) | 32, type, 6, pointer[3]], [(4u << 16) | 43, pointer[3], value, view]]);
            replacements.Add(item[2], [(5u << 16) | 59, type, item[2], 6, value]);
        }
        if (replacements.Count != builtins.Count || next > 1_048_576) throw new ArgumentException("Invalid view-index declarations.", nameof(code));
        var output = new List<uint>(words.Length + declarations.Count * 5); output.AddRange(words[..5].ToArray()); output[3] = next;
        foreach (var item in instructions)
        {
            var op = item[0] & 65535;
            if (op == 17 && item.Length == 2 && item[1] == 4439 || op == 10 && Encoding.UTF8.GetString(MemoryMarshal.AsBytes(item.AsSpan(1))).TrimEnd('\0') == "SPV_KHR_multiview") continue;
            if (op == 71 && builtins.Contains(item[1]) && item[2] is 11 or 13 or 14 or 16 or 17 or 18) continue;
            if (op == 59 && replacements.TryGetValue(item[2], out var replacement)) { foreach (var declaration in declarations[item[2]]) output.AddRange(declaration); output.AddRange(replacement); continue; }
            if (op == 15 && words[1] < 0x00010400)
            {
                var end = 3; while (end < item.Length && (item[end++] & 0xff000000) != 0) { }
                var entry = item.Take(end).Concat(item.Skip(end).Where(id => !builtins.Contains(id))).ToArray(); entry[0] = (uint)(entry.Length << 16) | 15; output.AddRange(entry); continue;
            }
            output.AddRange(item);
        }
        return MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray();
    }
    private void BindGraphicsState()
    {
        var entry = _graphicsPipeline!;
        SDL.BindGPUGraphicsPipeline(_drawPass, GraphicsHandle(entry));
        SetBlendConstants(_drawBlend); SDL.SetGPUStencilReference(_drawPass, entry.Graphics!.StencilReference);
    }
    private void SelectDrawView(int view)
    {
        if (_drawView == view) return;
        EndDrawPass(); _drawView = view;
        try { BeginDrawPass(false); }
        catch { _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _sets.Clear(); ResetPush(); throw; }
    }
    private void FinishDrawViews()
    {
        var target = _drawTarget!;
        for (var view = 0; view < target.Visited.Length; view++) if (!target.Visited[view]) SelectDrawView(view);
    }
    private static void ReleaseViews(Entry entry)
    {
        foreach (var pipeline in entry.ViewPipelines) pipeline.Dispose();
        foreach (var program in entry.ViewPrograms) ReleaseProgram(program);
    }
    private nint GraphicsHandle(Entry entry) => _drawView == 0 ? entry.Handle!.DangerousGetHandle() : entry.ViewPipelines[_drawView - 1].DangerousGetHandle();
}
