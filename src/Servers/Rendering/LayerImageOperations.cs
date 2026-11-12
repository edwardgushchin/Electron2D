using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    internal static uint EmitLayerImageOperation(uint[] operation, uint coordinateType, Dictionary<uint, uint[]> types,
        Func<uint, uint[], uint> type, Func<uint, uint> zero, List<uint[]> body, ref uint next)
    {
        var op = Op(operation);
        var result = op == 99 ? 0 : operation[2];
        if (op is 103 or 104)
        {
            var original = operation[1];
            operation[1] = type(23, [types[original][2], 3]);
            operation[2] = next++;
            body.Add(operation);
            body.Add(Instruction(79, original, result, operation[2], operation[2], 0, 1));
        }
        else
        {
            if (op is >= 87 and <= 99 or 60)
            {
                var index = op == 99 ? 2 : 4;
                var vector = types[coordinateType];
                if (Op(vector) != 23 || vector[3] < 2) throw new ArgumentException("A layer view needs a two-component image coordinate.");
                var scalar = vector[2];
                var x = next++;
                var y = next++;
                body.Add(Instruction(81, scalar, x, operation[index], 0));
                body.Add(Instruction(81, scalar, y, operation[index], 1));
                if (op is >= 91 and <= 94)
                {
                    var q = next++;
                    body.Add(Instruction(81, scalar, q, operation[index], 2));
                    body.Add(Instruction(136, scalar, next, x, q));
                    x = next++;
                    body.Add(Instruction(136, scalar, next, y, q));
                    y = next++;
                    operation[0] = (operation[0] & 0xffff0000u) | (op - 4);
                }
                operation[index] = next++;
                body.Add(Instruction(80, type(23, [scalar, 3]), operation[index], x, y, zero(scalar)));
            }
            body.Add(operation);
        }
        return result;
    }
}
