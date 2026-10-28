using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private const int MaximumPushBytes = 128;
    private sealed record PushStage(uint Variable, uint Size, ShaderStage Stage, int Native = -1);
    private sealed class PushLowering
    {
        private readonly List<uint[]> _instructions = [];
        private readonly Dictionary<uint, uint[]> _types = [];
        private readonly Dictionary<uint, uint> _literals = [];
        private readonly Dictionary<(uint, uint), uint> _decorations = [];
        private readonly Dictionary<(uint, uint, uint), uint> _members = [];
        private readonly Dictionary<uint, uint> _valueTypes = [];
        private readonly Dictionary<uint, uint> _uintConstants = [];
        private readonly List<uint> _constants = [], _functions = [];
        private uint _next, _uint, _vector, _wordPair, _halfPair, _scalarPointer, _raw, _root, _block;
        private readonly uint[] _header;
        private int _depth;
        private bool _integer16, _integer8;
        private uint Fresh() => _next < 1_048_576 ? _next++ : throw new ArgumentException("Translated shader identities exceed the module limit.");
        private static uint Op(uint[] instruction) => instruction[0] & 65535;
        private static void Emit(List<uint> output, uint op, params uint[] operands) { output.Add((uint)(operands.Length + 1) << 16 | op); output.AddRange(operands); }
        private uint Constant(uint value)
        {
            if (_uintConstants.TryGetValue(value, out var id)) return id;
            id = Fresh(); _uintConstants.Add(value, id); Emit(_constants, 43, _uint, id, value); return id;
        }
        private uint Instruction(uint op, uint type, params uint[] operands)
        { var id = Fresh(); _functions.Add((uint)(operands.Length + 3) << 16 | op); _functions.Add(type); _functions.Add(id); _functions.AddRange(operands); return id; }
        private uint Part(uint pointer, uint index) => Instruction(81, _uint, pointer, index);
        private uint Add(uint value, uint bytes) => bytes == 0 ? value : Instruction(128, _uint, value, Constant(bytes));
        private uint Offset(uint value, uint index, uint stride) => Instruction(128, _uint, value, Instruction(132, _uint, index, stride));
        private uint[] Type(uint id) => _types.TryGetValue(id, out var type) ? type : throw new ArgumentException("A push block refers to an unknown type.");
        private uint Literal(uint id) => _literals.TryGetValue(id, out var n) ? n : throw new NotSupportedException("Push block array lengths and struct selectors require literal integers; specialization needs its module adapter.");
        private uint Decoration(uint type, uint kind) => _decorations.TryGetValue((type, kind), out var n) ? n : throw new ArgumentException("A push block is missing its array layout.");
        private uint Member(uint type, uint index, uint kind) => _members.TryGetValue((type, index, kind), out var n) ? n : throw new ArgumentException("A push block is missing its member layout.");
        private bool PushPointer(uint type) => _types.TryGetValue(type, out var t) && Op(t) == 32 && t[2] == 9;
        private uint NativeType(uint type) => PushPointer(type) ? _vector : type;
        private uint CellBytes(uint type)
        {
            for (var depth = 0; depth < 32; depth++)
            {
                var t = Type(type);
                if (Op(t) is 23 or 24 or 28) { type = t[2]; continue; }
                return Op(t) == 32 && t[2] == 5349 ? 8u : Op(t) is 21 or 22 ? t[2] / 8 : 4u;
            }
            throw new ArgumentException("Push element nesting exceeds the module depth.");
        }
        private uint Alignment(uint type)
        {
            var t = Type(type);
            return Op(t) switch { 32 when t[2] == 5349 => 8, 21 when t[2] == 8 => 1, 21 or 22 when t[2] == 16 => 2, 23 or 24 or 28 => Alignment(t[2]), 30 => t.Skip(2).Select(Alignment).DefaultIfEmpty(4u).Max(), _ => 4 };
        }
        private (uint Column, uint Row) Matrix(uint type, uint member)
        {
            if (!_members.TryGetValue((type, member, 7), out var stride)) return (0, 0);
            if (stride == 0 || stride % CellBytes(Type(type)[member + 2]) != 0 || _members.ContainsKey((type, member, 4)) == _members.ContainsKey((type, member, 5))) throw new ArgumentException("Invalid push matrix layout.");
            var cell = CellBytes(Type(type)[member + 2]);
            return _members.ContainsKey((type, member, 4)) ? (cell, stride) : (stride, cell);
        }
        private uint Size(uint id, uint column = 0, uint row = 0)
        {
            if (++_depth > 32) throw new ArgumentException("Push type nesting is cyclic or exceeds the supported module depth.");
            try
            {
                var t = Type(id); uint size;
                switch (Op(t))
                {
                    case 20: size = 4; break;
                    case 32 when t[2] == 5349: size = 8; break;
                    case 21:
                        if (t[2] is not (8 or 16 or 32 or 64)) throw new NotSupportedException("Push integer precision requires its native shader adapter."); size = t[2] / 8; break;
                    case 22:
                        if (t[2] is not (16 or 32 or 64)) throw new NotSupportedException("Push floating-point precision requires its native shader adapter."); size = t[2] / 8; break;
                    case 23: if (t[3] is < 2 or > 4) throw new ArgumentException("Invalid push vector width."); size = checked(Size(t[2]) * t[3]); break;
                    case 24:
                        var vector = Type(t[2]); if (Op(vector) != 23 || vector.Length != 4 || vector[3] is < 2 or > 4 || t[3] is < 2 or > 4) throw new ArgumentException("Invalid push matrix type.");
                        var cell = Size(vector[2]); if (column == 0 || row == 0 || column != cell && row != cell) throw new ArgumentException("Invalid push matrix stride.");
                        if (column == cell ? row < t[3] * cell : column < vector[3] * cell) throw new ArgumentException("Push matrix storage overlaps.");
                        size = checked(column == cell ? row * vector[3] : column * t[3]); break;
                    case 28:
                        var count = Literal(t[3]); var stride = Decoration(id, 6);
                        if (count == 0 || stride % Alignment(t[2]) != 0 || stride < Size(t[2], column, row)) throw new ArgumentException("Invalid push array layout."); size = checked(stride * count); break;
                    case 30:
                        size = 0; var spans = new List<(uint Start, uint End)>();
                        for (uint i = 0; i < t.Length - 2; i++)
                        {
                            var offset = Member(id, i, 35); var matrix = Matrix(id, i); var end = checked(offset + Size(t[i + 2], matrix.Column, matrix.Row));
                            if (offset % Alignment(t[i + 2]) != 0 || spans.Any(s => offset < s.End && s.Start < end)) throw new ArgumentException("Push members overlap or violate scalar alignment.");
                            spans.Add((offset, end)); size = Math.Max(size, end);
                        }
                        break;
                    default: throw new NotSupportedException("Push values require fixed scalar, vector, matrix, array or struct data.");
                }
                if (size == 0 || size > MaximumPushBytes) throw new NotSupportedException("Push data exceeds the portable push-data capacity.");
                return size;
            }
            finally { _depth--; }
        }
        private uint LoadWord(uint address)
        {
            var word = Instruction(194, _uint, address, Constant(2));
            var pointer = Instruction(65, _scalarPointer, _raw, Constant(0), Instruction(194, _uint, word, Constant(2)), Instruction(199, _uint, word, Constant(3)));
            return Instruction(61, _uint, pointer);
        }
        private uint Load(uint type, uint address, uint column, uint row, uint vectorStride)
        {
            var t = Type(type);
            if (Op(t) is 20 or 21 or 22 || Op(t) == 32 && t[2] == 5349)
            {
                var value = LoadWord(address);
                if (Op(t) == 21 && t[2] is 8 or 16)
                {
                    if (t[2] == 8) _integer8 = true; else _integer16 = true;
                    var shift = Instruction(132, _uint, Instruction(199, _uint, address, Constant(t[2] == 8 ? 3u : 2u)), Constant(8));
                    return Instruction(t[3] == 1 ? 114u : 113u, type, Instruction(194, _uint, value, shift));
                }
                if (Op(t) == 22 && t[2] == 16)
                {
                    if (_halfPair == 0)
                    {
                        _halfPair = _types.Values.FirstOrDefault(v => Op(v) == 23 && v.Length == 4 && v[2] == type && v[3] == 2)?[1] ?? 0;
                        if (_halfPair == 0) { _halfPair = Fresh(); Emit(_constants, 23, _halfPair, type, 2); }
                    }
                    var pair = Instruction(124, _halfPair, value);
                    return Instruction(77, type, pair, Instruction(199, _uint, Instruction(194, _uint, address, Constant(1)), Constant(1)));
                }
                if (Op(t) is 21 or 22 && t[2] == 64 || Op(t) == 32 && t[2] == 5349)
                {
                    if (_wordPair == 0)
                    {
                        _wordPair = _types.Values.FirstOrDefault(v => Op(v) == 23 && v.Length == 4 && v[2] == _uint && v[3] == 2)?[1] ?? 0;
                        if (_wordPair == 0) { _wordPair = Fresh(); Emit(_constants, 23, _wordPair, _uint, 2); }
                    }
                    return Instruction(124, type, Instruction(80, _wordPair, value, LoadWord(Add(address, 4))));
                }
                return type == _uint ? value : Op(t) == 20 ? Instruction(171, type, value, Constant(0)) : Instruction(124, type, value);
            }
            var parts = new List<uint>();
            if (Op(t) == 23)
                for (uint i = 0; i < t[3]; i++) parts.Add(Load(t[2], Offset(address, Constant(i), vectorStride), column, row, vectorStride));
            else if (Op(t) == 24)
                for (uint i = 0; i < t[3]; i++) parts.Add(Load(t[2], Offset(address, Constant(i), column), column, row, row));
            else if (Op(t) == 28)
                for (uint i = 0; i < Literal(t[3]); i++) parts.Add(Load(t[2], Add(address, checked(i * Decoration(type, 6))), column, row, vectorStride));
            else if (Op(t) == 30)
                for (uint i = 0; i < t.Length - 2; i++) { var matrix = Matrix(type, i); parts.Add(Load(t[i + 2], Add(address, Member(type, i, 35)), Constant(matrix.Column), Constant(matrix.Row), Constant(CellBytes(t[i + 2])))); }
            else throw new NotSupportedException("The push load has no fixed value layout.");
            return Instruction(80, type, parts.ToArray());
        }
        private void AccessChain(uint[] ins)
        {
            if (!_valueTypes.TryGetValue(ins[3], out var pointerType)) throw new ArgumentException("A push access chain has an unknown base.");
            var type = Type(pointerType)[3]; var address = Part(ins[3], 0); var column = Part(ins[3], 1); var row = Part(ins[3], 2); var vectorStride = Part(ins[3], 3);
            for (var i = 4; i < ins.Length; i++)
            {
                var t = Type(type); var index = ins[i];
                if (Op(t) == 30)
                {
                    var member = Literal(index); if (member >= t.Length - 2) throw new ArgumentException("A push member index is out of bounds.");
                    address = Add(address, Member(type, member, 35)); var matrix = Matrix(type, member); column = Constant(matrix.Column); row = Constant(matrix.Row); type = t[member + 2]; vectorStride = Constant(CellBytes(type));
                }
                else
                {
                    if (!_valueTypes.TryGetValue(index, out var indexType)) throw new ArgumentException("A push array index has an unknown type.");
                    var scalar = Type(indexType); if (Op(scalar) != 21 || scalar[2] is not (8 or 16 or 32 or 64)) throw new NotSupportedException("Push indices require supported integers.");
                    if (scalar[2] != 32) index = Instruction(113, _uint, index);
                    else if (indexType != _uint) index = Instruction(124, _uint, index);
                    if (Op(t) == 28) { address = Offset(address, index, Constant(Decoration(type, 6))); type = t[2]; }
                    else if (Op(t) == 24) { address = Offset(address, index, column); vectorStride = row; type = t[2]; }
                    else if (Op(t) == 23) { address = Offset(address, index, vectorStride); type = t[2]; }
                    else throw new ArgumentException("A push access chain traverses a scalar or unsupported type.");
                }
            }
            if (type != Type(ins[1])[3]) throw new ArgumentException("Push access-chain result type does not match its value.");
            Emit(_functions, 80, _vector, ins[2], address, column, row, vectorStride);
        }
        internal PushLowering(byte[] code)
        {
            var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
            if (words.Length < 5 || words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid SPIR-V header.", nameof(code));
            _header = words[..5].ToArray(); _next = words[3];
            for (var i = 5; i < words.Length;)
            {
                var count = (int)(words[i] >> 16); if (count == 0 || count > words.Length - i) throw new ArgumentException("Truncated SPIR-V instruction.", nameof(code));
                var ins = words.Slice(i, count).ToArray(); _instructions.Add(ins); var op = Op(ins);
                var minimum = op switch { 19 or 20 => 2, 21 => 4, 22 => 3, 23 or 24 or 28 or 32 => 4, 25 => 9, 26 or 30 => 2, 27 or 29 or 31 or 33 => 3, 59 => 4, 61 => 4, 63 => 3, 65 or 66 => 4, _ => 1 };
                if (count < minimum) throw new ArgumentException("A shader instruction is missing required operands.", nameof(code));
                if (op is >= 19 and <= 33)
                { if (ins[1] == 0 || ins[1] >= _header[3]) throw new ArgumentException("A shader type identity exceeds its header bound.", nameof(code)); _types.Add(ins[1], ins); }
                if (op == 71 && count == 4) _decorations.TryAdd((ins[1], ins[2]), ins[3]);
                if (op == 72 && count >= 4) _members.TryAdd((ins[1], ins[2], ins[3]), count == 5 ? ins[4] : 1);
                i += count;
            }
            foreach (var ins in _instructions)
            {
                var op = Op(ins);
                if (ins.Length >= 3 && _types.ContainsKey(ins[1]) && (op == 12 || op >= 41 && op is not (71 or 72)))
                { if (ins[2] == 0 || ins[2] >= _header[3]) throw new ArgumentException("A shader value identity exceeds its header bound.", nameof(code)); _valueTypes.TryAdd(ins[2], ins[1]); }
                if (op == 43 && ins.Length == 4 && _types.TryGetValue(ins[1], out var scalar) && Op(scalar) == 21 && scalar[2] == 32) _literals.Add(ins[2], ins[3]);
                if (op == 59 && ins[3] == 9) { if (ins.Length != 4) throw new ArgumentException("Push storage cannot have an initializer.", nameof(code)); if (_root != 0) throw new ArgumentException("One push block per module is required."); _root = ins[2]; _block = Type(ins[1])[3]; }
            }
        }
        internal (byte[] Code, uint Variable, uint Size) Translate()
        {
            if (_root == 0) return (Bytes(_instructions.SelectMany(i => i)), 0, 0);
            var size = Size(_block); if (Op(Type(_block)) != 30) throw new ArgumentException("A push block requires struct storage.");
            if (_decorations.ContainsKey((_root, 33)) || _decorations.ContainsKey((_root, 34))) throw new ArgumentException("Push blocks do not have descriptor bindings.");
            var head = new List<uint>();
            _uint = _types.Values.FirstOrDefault(t => Op(t) == 21 && t.Length == 4 && t[2] == 32 && t[3] == 0)?[1] ?? 0;
            if (_uint == 0) { _uint = Fresh(); Emit(head, 21, _uint, 32, 0); }
            _vector = _types.Values.FirstOrDefault(t => Op(t) == 23 && t.Length == 4 && t[2] == _uint && t[3] == 4)?[1] ?? 0;
            if (_vector == 0) { _vector = Fresh(); Emit(head, 23, _vector, _uint, 4); }
            _scalarPointer = _types.Values.FirstOrDefault(t => Op(t) == 32 && t.Length == 4 && t[2] == 2 && t[3] == _uint)?[1] ?? 0;
            var newScalarPointer = _scalarPointer == 0; if (newScalarPointer) _scalarPointer = Fresh();
            var array = Fresh(); var structure = Fresh(); var pointer = Fresh(); _raw = Fresh();
            var zero = Constant(0); var four = Constant(4); var length = Constant((size + 15) / 16);
            foreach (var ins in _instructions)
            {
                var op = Op(ins);
                if (op == 65 || op == 66)
                { if (PushPointer(ins[1])) { AccessChain(ins); continue; } }
                if (op == 61 && PushPointer(_valueTypes.GetValueOrDefault(ins[3])))
                {
                    var p = ins[3]; var value = Load(ins[1], Part(p, 0), Part(p, 1), Part(p, 2), Part(p, 3)); Emit(_functions, 83, ins[1], ins[2], value); continue;
                }
                if (op == 63 && PushPointer(_valueTypes.GetValueOrDefault(ins[2])))
                {
                    var p = ins[2]; var type = Type(_valueTypes[p])[3]; var value = Load(type, Part(p, 0), Part(p, 1), Part(p, 2), Part(p, 3)); Emit(_functions, 62, ins[1], value); continue;
                }
                if (op is 62 or 63 or 64 && PushPointer(_valueTypes.GetValueOrDefault(ins[1]))) throw new ArgumentException("Push constant storage is read-only.");
                if ((op == 12 || op >= 41 && op is not (71 or 72)) && ins.Length >= 3 && PushPointer(ins[1]))
                {
                    if (op is not (54 or 55 or 57 or 83 or 169 or 245 or 59)) throw new NotSupportedException("Push pointer address/arithmetic requires its shader capability adapter.");
                    var changed = ins.ToArray(); changed[1] = _vector; _functions.AddRange(changed); continue;
                }
                _functions.AddRange(ins);
            }
            // Global source push variables become virtual byte addresses; raw words use a separate std140 block.
            var globals = new List<uint>(head); globals.AddRange(_constants);
            Emit(globals, 28, array, _vector, length); Emit(globals, 30, structure, array); Emit(globals, 32, pointer, 2, structure);
            if (newScalarPointer) Emit(globals, 32, _scalarPointer, 2, _uint);
            Emit(globals, 59, pointer, _raw, 2); Emit(globals, 44, _vector, _root, zero, zero, zero, four);
            var usedSets = _decorations.Where(d => d.Key.Item2 == 34).Select(d => d.Value).ToHashSet(); var privateSet = uint.MaxValue; while (usedSets.Contains(privateSet)) privateSet--;
            var annotations = new List<uint>(); Emit(annotations, 71, array, 6, 16); Emit(annotations, 71, structure, 2); Emit(annotations, 72, structure, 0, 35, 0); Emit(annotations, 71, _raw, 34, privateSet); Emit(annotations, 71, _raw, 33, 0);
            var translated = new List<uint>();
            if (_integer8 && !_instructions.Any(i => Op(i) == 17 && i[1] == 39)) Emit(translated, 17, 39);
            if (_integer16 && !_instructions.Any(i => Op(i) == 17 && i[1] == 22)) Emit(translated, 17, 22);
            if (_halfPair != 0 && !_instructions.Any(i => Op(i) == 17 && i[1] == 9)) Emit(translated, 17, 9);
            var addedAnnotations = false; var addedGlobals = false;
            for (var i = 0; i < _functions.Count;)
            {
                var count = (int)(_functions[i] >> 16); var ins = _functions.GetRange(i, count).ToArray(); var op = Op(ins); i += count;
                if (op == 59 && ins[2] == _root || op == 71 && ins[1] == _root) continue;
                if (!addedAnnotations && op is >= 19 and <= 39) { translated.AddRange(annotations); addedAnnotations = true; }
                if (!addedGlobals && op == 54) { translated.AddRange(globals); addedGlobals = true; }
                if (op == 15)
                {
                    var nameEnd = 3; while (nameEnd < ins.Length && (ins[nameEnd++] & 0xff000000) != 0) { }
                    var entry = ins.Take(nameEnd).Concat(ins.Skip(nameEnd).Where(id => id != _root)).ToList(); if (_header[1] >= 0x10400) entry.Add(_raw); entry[0] = (uint)entry.Count << 16 | op; translated.AddRange(entry); continue;
                }
                if (op == 33) for (var j = 2; j < ins.Length; j++) ins[j] = NativeType(ins[j]);
                translated.AddRange(ins);
            }
            if (!addedAnnotations || !addedGlobals) throw new ArgumentException("Push translation requires ordinary type and function sections.");
            return (Bytes(translated), _raw, size);
        }
        private byte[] Bytes(IEnumerable<uint> instructions)
        { var words = _header.Concat(instructions).ToArray(); words[3] = _next; return MemoryMarshal.AsBytes(words.AsSpan()).ToArray(); }
    }
}
