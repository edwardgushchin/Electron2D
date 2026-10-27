using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly record struct Specialization(int Kind, ulong Bits);
    private static Dictionary<uint, Specialization> CaptureSpecialization(ReadOnlySpan<RDPipelineSpecializationConstant> constants)
    {
        var result = new Dictionary<uint, Specialization>();
        foreach (var constant in constants)
        {
            ArgumentNullException.ThrowIfNull(constant); var id = constant.ConstantID;
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(constants)); var value = constant.CaptureValue();
            if (!result.TryAdd((uint)id, new(value.Kind, value.Bits))) throw new ArgumentException("Specialization identifiers must be distinct.", nameof(constants));
        }
        return result;
    }
    private sealed class SpecializationModule
    {
        private sealed record Value(uint Type, ulong Bits, Value[]? Elements = null);
        private readonly List<uint[]> _instructions = [];
        private readonly Dictionary<uint, uint[]> _types = [], _constants = [];
        private readonly Dictionary<uint, uint> _specIDs = [];
        private readonly Dictionary<uint, Value> _values = [];
        private readonly HashSet<uint> _evaluating = [];
        private readonly Dictionary<uint, Specialization> _overrides;
        private readonly HashSet<uint> _used;
        private readonly uint[] _header;
        private uint _next;
        private uint _workgroup;
        private int _aggregateValues;
        private int _emittedValues;
        internal bool HasSpecialization { get; private set; }
        private static uint Op(uint[] words) => words[0] & 65535;
        internal SpecializationModule(byte[] code, Dictionary<uint, Specialization> overrides, HashSet<uint> used)
        {
            if (code.Length is < 20 or > 16 * 1024 * 1024 || code.Length % 4 != 0) throw new ArgumentException("A module requires bounded aligned SPIR-V words.", nameof(code));
            var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); if (words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid SPIR-V header.", nameof(code));
            _header = words[..5].ToArray(); _next = words[3]; _overrides = overrides; _used = used;
            for (var i = 5; i < words.Length;)
            {
                var length = (int)(words[i] >> 16); if (length == 0 || length > words.Length - i) throw new ArgumentException("Truncated SPIR-V instruction.", nameof(code));
                var ins = words.Slice(i, length).ToArray(); var op = Op(ins); _instructions.Add(ins);
                if (op is >= 19 and <= 33) { if (length < 2 || ins[1] == 0 || ins[1] >= words[3]) throw new ArgumentException("Invalid shader type identity."); _types.Add(ins[1], ins); }
                if (op is >= 41 and <= 52 && op != 45) { if (length < 3 || ins[2] == 0 || ins[2] >= words[3]) throw new ArgumentException("Invalid constant identity."); _constants.Add(ins[2], ins); }
                if (op == 71 && length == 4 && ins[2] == 1) _specIDs.Add(ins[1], ins[3]);
                if (op == 71 && length == 4 && ins[2] == 11 && ins[3] == 25) _workgroup = ins[1];
                if (op is >= 48 and <= 52) HasSpecialization = true;
                i += length;
            }
        }
        private uint[] Type(uint id) => _types.TryGetValue(id, out var type) ? type : throw new ArgumentException("Unknown specialization type.");
        private int Kind(uint type)
        {
            var t = Type(type); if (Op(t) == 20) return 0;
            if (Op(t) == 21 && t.Length == 4 && t[2] == 32 && t[3] <= 1) return t[3] == 1 ? 1 : 2;
            if (Op(t) == 22 && t.Length == 3 && t[2] == 32) return 3;
            if (Op(t) == 22 && t.Length == 3 && t[2] == 64) return 4;
            throw new NotSupportedException("Specialization scalar precision requires its native shader adapter.");
        }
        private Value Evaluate(uint id)
        {
            if (_values.TryGetValue(id, out var value)) return value;
            if (!_constants.TryGetValue(id, out var ins)) throw new ArgumentException("A specialization expression refers to a nonconstant value.");
            if (_evaluating.Count >= 128 || !_evaluating.Add(id)) throw new ArgumentException("Specialization dependencies are cyclic or exceed the module depth.");
            try
            {
                var op = Op(ins); var type = ins[1];
                if (op is 41 or 42 or 43 or 48 or 49 or 50)
                {
                    var kind = Kind(type); if (ins.Length != (kind == 0 ? 3 : kind == 4 ? 5 : 4)) throw new ArgumentException("A scalar constant has an invalid payload.");
                    ulong bits = op is 41 or 48 ? 1ul : op is 42 or 49 ? 0ul : ins[3] | (kind == 4 ? (ulong)ins[4] << 32 : 0);
                    if (op is 48 or 49 or 50 && _specIDs.TryGetValue(id, out var external) && _overrides.TryGetValue(external, out var replacement))
                    { if (replacement.Kind != kind) throw new ArgumentException("Specialization value type does not match its module declaration."); bits = replacement.Bits; _used.Add(external); }
                    value = new(type, bits);
                }
                else if (op is 44 or 51)
                {
                    var elementTypes = ElementTypes(type);
                    if (ins.Length - 3 != elementTypes.Length) throw new ArgumentException("A specialization composite has an invalid element count.");
                    var elements = ins.Skip(3).Select(Evaluate).ToArray();
                    for (var i = 0; i < elements.Length; i++) if (elements[i].Type != elementTypes[i]) throw new ArgumentException("A specialization composite has an invalid element type.");
                    value = new(type, 0, elements);
                }
                else if (op == 46) value = Zero(type);
                else if (op == 52 && ins.Length >= 5) value = Operation(type, ins[3], ins[4..]);
                else throw new NotSupportedException("This constant form requires its shader capability adapter.");
                _values.Add(id, value); return value;
            }
            finally { _evaluating.Remove(id); }
        }
        private uint[] ElementTypes(uint type)
        {
            var t = Type(type);
            if (Op(t) == 30)
            {
                if ((_aggregateValues += t.Length - 2) > 1_048_576) throw new ArgumentException("Specialization aggregates exceed the module value bound.");
                return t[2..];
            }
            if (Op(t) is not (23 or 24 or 28) || t.Length != 4) throw new ArgumentException("A specialization aggregate requires a bounded element type.");
            var count = Op(t) == 28 ? Word(Evaluate(t[3])) : t[3];
            if (count is 0 or > 1_048_576 || (_aggregateValues += checked((int)count)) > 1_048_576) throw new ArgumentException("Specialization aggregates exceed the module value bound.");
            return Enumerable.Repeat(t[2], (int)count).ToArray();
        }
        private Value Zero(uint type, int depth = 0)
        {
            if (depth >= 128) throw new ArgumentException("Null aggregate types are cyclic or exceed the module depth.");
            if (Op(Type(type)) is 20 or 21 or 22) { Kind(type); return new(type, 0); }
            return new(type, 0, ElementTypes(type).Select(t => Zero(t, depth + 1)).ToArray());
        }
        private Value Operation(uint type, uint op, uint[] args)
        {
            var required = op is 113 or 114 or 115 or 116 or 126 or 168 or 200 ? 1 : op == 169 ? 3 : 2;
            if (op is 79 or 81 or 82 ? args.Length < (op == 81 ? 2 : 3) : args.Length != required) throw new ArgumentException("Specialization operation has an invalid operand count.");
            if (op == 81)
            { var v = Evaluate(args[0]); foreach (var index in args.Skip(1)) v = v.Elements is { } elements && index < elements.Length ? elements[index] : throw new ArgumentException("A specialization composite index is out of bounds."); return v with { Type = type }; }
            if (op == 82)
            {
                Value Insert(Value composite, int depth)
                { if (depth == args.Length) return Evaluate(args[0]); var items = composite.Elements?.ToArray() ?? throw new ArgumentException("Composite insert requires aggregate data."); var index = args[depth]; if (index >= items.Length) throw new ArgumentException("Composite insert index is out of bounds."); items[index] = Insert(items[index], depth + 1); return composite with { Elements = items }; }
                return Insert(Evaluate(args[1]), 2) with { Type = type };
            }
            if (op == 79)
            { var values = (Evaluate(args[0]).Elements ?? []).Concat(Evaluate(args[1]).Elements ?? []).ToArray(); var elements = args.Skip(2).Select(i => i == uint.MaxValue ? Zero(Type(type)[2]) : i < values.Length ? values[i] : throw new ArgumentException("Specialization vector shuffle index is out of bounds.")).ToArray(); return new(type, 0, elements); }
            var operands = args.Select(Evaluate).ToArray(); var resultType = Type(type);
            if (Op(resultType) == 23)
            {
                if (resultType.Length != 4 || resultType[3] is < 2 or > 4 || operands.Any(v => v.Elements is { } e && e.Length != resultType[3])) throw new ArgumentException("Specialization vector operands must match the result width.");
                var count = resultType[3]; var items = new Value[count];
                for (var i = 0; i < count; i++) items[i] = Scalar(resultType[2], op, operands.Select(v => v.Elements is { } e ? e[i] : v).ToArray());
                return new(type, 0, items);
            }
            if (op == 169 && operands[0].Elements is null) return operands[operands[0].Bits != 0 ? 1 : 2] with { Type = type };
            return Scalar(type, op, operands);
        }
        private Value Scalar(uint type, uint op, Value[] operands)
        {
            if (op == 115)
            {
                var sourceKind = Kind(operands[0].Type); var resultKind = Kind(type); var sourceBits = operands[0].Bits;
                var converted = (sourceKind, resultKind) switch
                {
                    (3, 3) or (4, 4) => sourceBits,
                    (3, 4) => BitConverter.DoubleToUInt64Bits(BitConverter.UInt32BitsToSingle((uint)sourceBits)),
                    (4, 3) => BitConverter.SingleToUInt32Bits((float)BitConverter.UInt64BitsToDouble(sourceBits)),
                    _ => throw new NotSupportedException("Specialization conversion requires supported floating-point types.")
                };
                return new(type, converted);
            }
            if (Kind(type) == 4)
            {
                if (op == 169) return new(type, operands[0].Bits != 0 ? operands[1].Bits : operands[2].Bits);
                throw new NotSupportedException("This double specialization operation is outside the Shader profile.");
            }
            uint A(int n) => n < operands.Length && operands[n].Elements is null ? checked((uint)operands[n].Bits) : throw new ArgumentException("Invalid specialization scalar operands.");
            var a = A(0); uint B() => A(1); uint C() => A(2); uint Bool(bool x) => x ? 1u : 0u;
            uint Shift() { var n = B(); if (n >= 32) throw new ArgumentException("Specialization shift exceeds the scalar width."); return n; }
            uint Div(bool signed, bool remainder, bool modulo)
            {
                var b = B(); if (b == 0 || signed && a == 0x80000000 && b == uint.MaxValue) throw new ArgumentException("Specialization division is undefined.");
                if (!signed) return remainder ? a % b : a / b;
                var x = (int)a; var y = (int)b; var r = x % y; if (modulo && r != 0 && (r < 0) != (y < 0)) r += y; return unchecked((uint)(remainder ? r : x / y));
            }
            var bits = op switch
            {
                113 or 114 => a,
                116 => BitConverter.SingleToUInt32Bits((float)(Half)BitConverter.UInt32BitsToSingle(a)),
                126 => unchecked(0u - a),
                128 => unchecked(a + B()),
                130 => unchecked(a - B()),
                132 => unchecked(a * B()),
                134 => Div(false, false, false),
                135 => Div(true, false, false),
                137 => Div(false, true, false),
                138 => Div(true, true, false),
                139 => Div(true, true, true),
                164 or 170 => Bool(a == B()),
                165 or 171 => Bool(a != B()),
                166 => Bool(a != 0 || B() != 0),
                167 => Bool(a != 0 && B() != 0),
                168 => Bool(a == 0),
                169 => a != 0 ? B() : C(),
                172 => Bool(a > B()),
                173 => Bool((int)a > (int)B()),
                174 => Bool(a >= B()),
                175 => Bool((int)a >= (int)B()),
                176 => Bool(a < B()),
                177 => Bool((int)a < (int)B()),
                178 => Bool(a <= B()),
                179 => Bool((int)a <= (int)B()),
                194 => a >> (int)Shift(),
                195 => unchecked((uint)((int)a >> (int)Shift())),
                196 => a << (int)Shift(),
                197 => a | B(),
                198 => a ^ B(),
                199 => a & B(),
                200 => ~a,
                _ => throw new NotSupportedException("The specialization operation requires its shader capability adapter.")
            };
            Kind(type); return new(type, bits);
        }
        private uint Fresh() => _next < 1_048_576 ? _next++ : throw new ArgumentException("Specialized module identities exceed the payload bound.");
        private uint Word(Value value)
        {
            if (Kind(value.Type) is not (1 or 2) || value.Bits > uint.MaxValue) throw new ArgumentException("A module dimension requires a 32-bit integer constant.");
            return (uint)value.Bits;
        }
        private static void Emit(List<uint> output, uint op, params uint[] operands) { output.Add((uint)(operands.Length + 1) << 16 | op); output.AddRange(operands); }
        private void EmitValue(List<uint> output, uint id, Value value, int depth = 0)
        {
            if (depth >= 128 || ++_emittedValues > 1_048_576) throw new ArgumentException("Specialized constant output exceeds the module bounds.");
            if (value.Elements is { } elements)
            {
                var ids = new uint[elements.Length]; for (var i = 0; i < ids.Length; i++) { ids[i] = Fresh(); EmitValue(output, ids[i], elements[i], depth + 1); }
                Emit(output, 44, [value.Type, id, .. ids]);
            }
            else if (Kind(value.Type) == 0) Emit(output, value.Bits != 0 ? 41u : 42u, value.Type, id);
            else if (Kind(value.Type) == 4) Emit(output, 43, value.Type, id, (uint)value.Bits, (uint)(value.Bits >> 32));
            else Emit(output, 43, value.Type, id, (uint)value.Bits);
        }
        internal byte[] Freeze()
        {
            var output = new List<uint>();
            var hasSpecializedWorkgroup = _constants.TryGetValue(_workgroup, out var workgroupConstant) && Op(workgroupConstant) is >= 48 and <= 52;
            var workgroup = hasSpecializedWorkgroup ? Evaluate(_workgroup).Elements : null;
            if (hasSpecializedWorkgroup && workgroup is not { Length: 3 }) throw new ArgumentException("Workgroup size requires three scalar dimensions.");
            foreach (var ins in _instructions)
            {
                var op = Op(ins);
                if (op == 71 && ins.Length == 4 && ins[2] == 1) continue;
                if (op is >= 48 and <= 52) { EmitValue(output, ins[2], Evaluate(ins[2])); continue; }
                if (op == 16 && ins.Length == 6 && ins[2] == 17 && workgroup is not null) { Emit(output, 16, ins[1], 17, Word(workgroup[0]), Word(workgroup[1]), Word(workgroup[2])); continue; }
                if (op == 331 && ins.Length == 6 && ins[2] == 38) { Emit(output, 16, ins[1], 17, Word(Evaluate(ins[3])), Word(Evaluate(ins[4])), Word(Evaluate(ins[5]))); continue; }
                output.AddRange(ins);
            }
            var words = _header.Concat(output).ToArray(); words[3] = _next; return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
        }
    }
}
