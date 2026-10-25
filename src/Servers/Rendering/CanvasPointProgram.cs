using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static class CanvasPointProgram
{
    internal static void ValidateNative(byte[] code, bool fragment, string driver, bool sizedPoints)
    {
        if (fragment)
        {
            if (driver is "vulkan" or "metal") return;
            if (new Module(code).Instructions.Any(i => Op(i) == 71 && i.Length == 4 && i[2] == 11 && i[3] == 16 ||
                Op(i) == 72 && i.Length == 5 && i[3] == 11 && i[4] == 16))
                throw new NotSupportedException("Native point coordinates require a Vulkan or Metal graphics device.");
        }
        else if (!sizedPoints && WritesSize(code))
            throw new NotSupportedException("Authored point sizes require a graphics device with sized point support.");
    }

    internal static bool WritesSize(byte[] code)
    {
        var instructions = new Module(code).Instructions;
        var direct = instructions.Where(i => Op(i) == 71 && i.Length == 4 && i[2] == 11 && i[3] == 1).Select(i => i[1]).ToHashSet();
        var members = instructions.Where(i => Op(i) == 72 && i.Length == 5 && i[3] == 11 && i[4] == 1).ToDictionary(i => i[1], i => i[2]);
        if (direct.Count == 0 && members.Count == 0) return false;
        var types = instructions.Where(i => Op(i) == 32).ToDictionary(i => i[1]);
        var constants = instructions.Where(i => Op(i) == 43 && i.Length == 4).ToDictionary(i => i[2], i => i[3]);
        var floats = instructions.Where(i => Op(i) == 22 && i[2] == 32).Select(i => i[1]).ToHashSet();
        var one = instructions.Where(i => Op(i) == 43 && i.Length == 4 && floats.Contains(i[1]) && i[3] == 0x3f800000).Select(i => i[2]).ToHashSet();
        var blocks = instructions.Where(i => Op(i) == 59 && i[3] == 3 && members.ContainsKey(types[i[1]][3])).ToDictionary(i => i[2], i => members[types[i[1]][3]]);
        if (instructions.Any(i => Op(i) == 59 && i.Length == 5 && direct.Contains(i[2]) && !one.Contains(i[4]))) return true;
        foreach (var instruction in instructions)
        {
            var op = Op(instruction);
            if (op is 65 or 66 or 83)
            {
                var source = instruction[3];
                if (direct.Contains(source)) direct.Add(instruction[2]);
                else if (blocks.TryGetValue(source, out var member))
                {
                    if (op == 83) blocks[instruction[2]] = member;
                    else if (!constants.TryGetValue(instruction[4], out var index)) return true;
                    else if (index == member) direct.Add(instruction[2]);
                }
            }
            else if (op is 67 or 70 && (direct.Contains(instruction[3]) || blocks.ContainsKey(instruction[3]))) return true;
            else if (op is 63 or 64 && (direct.Contains(instruction[1]) || blocks.ContainsKey(instruction[1]))) return true;
            else if (op == 62 && (blocks.ContainsKey(instruction[1]) || direct.Contains(instruction[1]) && !one.Contains(instruction[2]))) return true;
            else if (op == 57) { foreach (var id in instruction.AsSpan(4)) if (direct.Contains(id) || blocks.ContainsKey(id)) return true; }
            else if (op is 169 or 245 && types.TryGetValue(instruction[1], out var pointer) && pointer[2] == 3) return true;
        }
        return false;
    }
}
