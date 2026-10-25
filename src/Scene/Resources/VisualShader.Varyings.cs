using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    /// <summary>The canvas stages sharing an authored varying.</summary>
    public enum VaryingMode
    {
        /// <summary>Vertex values are available to fragments and their automatic scene lighting.</summary>
        VertexToFragmentLight = 0,
        /// <summary>The nonselectable upper bound, including the separate fragment-to-light graph role.</summary>
        Max = 2,
    }
    private readonly Dictionary<string, VisualShaderVaryingType> _varyings = new(StringComparer.Ordinal);
    private static bool VaryingIdentifier(string name) => name.Length != 0 && (char.IsAsciiLetter(name[0]) || name[0] == '_') && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    /// <summary>Adds a named numeric value shared between canvas stages.</summary>
    /// <param name="name">A unique ASCII shader identifier.</param>
    /// <param name="mode">The vertex-to-fragment transport role.</param>
    /// <param name="type">The complete numeric value role.</param>
    /// <remarks>Declarations and node accesses are stored independently. Import validates names and roles of
    /// reachable accesses. Four native custom locations are available; a matrix consumes four.
    /// Unused declarations consume no native locations. Source edits retain the previous compiled program.</remarks>
    /// <exception cref="ArgumentException">The name is invalid, duplicated or declaration capacity is exhausted.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A selector is undefined or nonselectable.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void AddVarying(string name, VaryingMode mode, VisualShaderVaryingType type)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name);
        if (mode != VaryingMode.VertexToFragmentLight) throw new ArgumentOutOfRangeException(nameof(mode));
        if ((uint)type >= (uint)VisualShaderVaryingType.Max) throw new ArgumentOutOfRangeException(nameof(type));
        if (!VaryingIdentifier(name) || _varyings.Count == MaximumNodes || !_varyings.TryAdd(name, type)) throw new ArgumentException("A varying requires a unique ASCII identifier within declaration capacity.", nameof(name));
        EmitChanged();
    }
    /// <summary>Removes a declaration while retaining node references for subsequent source edits.</summary>
    /// <param name="name">The existing declaration name.</param>
    /// <exception cref="ArgumentException">The declaration does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void RemoveVarying(string name)
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (!_varyings.Remove(name)) throw new ArgumentException("The varying declaration does not exist.", nameof(name)); EmitChanged(); }
    /// <summary>Tests whether this graph declares a varying.</summary>
    /// <param name="name">The exact case-sensitive name.</param>
    /// <returns>True for a current declaration, independently of node references.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public bool HasVarying(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _varyings.ContainsKey(name); }

    private sealed record VaryingWire(VisualShaderVaryingType Type, int Location, string[] Names);
    private Dictionary<string, VaryingWire> PrepareVaryings(Dictionary<string, VisualShaderNodeParameter> parameters, CustomPlan? custom)
    {
        var active = new HashSet<string>(StringComparer.Ordinal); var names = parameters.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var stage in new[] { Type.Vertex, Type.Fragment })
        {
            var plan = PlanStage(stage); var reachable = plan.Order.ToHashSet();
            foreach (var source in ExpressionSources(stage, reachable, custom))
                names.UnionWith(SourceIdentifiers(source).Select(token => source.Substring(token.Start, token.Length)));
            foreach (var id in reachable)
                if (_graphs[(int)stage].Nodes[id].Node is VisualShaderNodeVarying node && node.VaryingName != "[None]")
                { ValidateVaryingAccess(node, stage); active.Add(node.VaryingName); }
        }
        var result = new Dictionary<string, VaryingWire>(StringComparer.Ordinal); var location = 12; var serial = 0;
        foreach (var name in active.Order(StringComparer.Ordinal))
        {
            var type = _varyings[name]; var count = type == VisualShaderVaryingType.Transform ? 4 : 1;
            if (location + count > 16) throw new NotSupportedException("Canvas varyings exceed four custom native locations; each numeric matrix consumes four.");
            var wires = new string[count];
            for (var i = 0; i < count; i++)
                do { wires[i] = "e2d_varying_" + (serial++).ToString(CultureInfo.InvariantCulture); } while (!names.Add(wires[i]));
            result.Add(name, new(type, location, wires)); location += count;
        }
        return result;
    }
    private void ValidateVaryingAccess(VisualShaderNodeVarying node, Type stage)
    {
        if (_mode != Mode.CanvasItem || stage is not (Type.Vertex or Type.Fragment) || node is VisualShaderNodeVaryingSetter && stage != Type.Vertex)
            throw new NotSupportedException("Named varyings require canvas vertex writes and canvas vertex/fragment reads; fragment writes require a light-graph producer.");
        if (!_varyings.TryGetValue(node.VaryingName, out var type) || type != node.VaryingType)
            throw new InvalidDataException("A varying access requires an existing declaration with the same numeric role.");
    }
    private static string VaryingHeader(Type stage, Dictionary<string, VaryingWire> varyings, string header)
    {
        if (varyings.Count == 0) return header;
        var declarations = new StringBuilder(); var initial = new StringBuilder();
        foreach (var wire in varyings.Values)
            for (var i = 0; i < wire.Names.Length; i++)
            {
                var flat = wire.Type is VisualShaderVaryingType.Int or VisualShaderVaryingType.UInt or VisualShaderVaryingType.Boolean;
                var type = wire.Type == VisualShaderVaryingType.Transform ? "vec4" : wire.Type == VisualShaderVaryingType.Boolean ? "uint" : VisualShaderValues.Name((VisualShaderNode.PortType)wire.Type);
                declarations.Append("layout(location=").Append(wire.Location + i).Append(") ").Append(flat ? "flat " : "").Append(stage == Type.Vertex ? "out " : "in ").Append(type).Append(' ').Append(wire.Names[i]).AppendLine(";");
                if (stage == Type.Vertex)
                {
                    var value = wire.Type == VisualShaderVaryingType.Boolean ? "0u" : wire.Type == VisualShaderVaryingType.Transform ? "vec4(" + string.Join(", ", Enumerable.Range(0, 4).Select(c => c == i ? "1.0" : "0.0")) + ")" : VisualShaderValues.Literal(VisualShaderValues.Zero((VisualShaderNode.PortType)wire.Type));
                    initial.Append("    ").Append(wire.Names[i]).Append(" = ").Append(value).AppendLine(";");
                }
            }
        return header.Replace("void main() {", declarations + "void main() {\n" + initial, StringComparison.Ordinal);
    }
    private string VaryingRead(VisualShaderNodeVaryingGetter node, Type stage, Dictionary<string, VaryingWire>? varyings)
    {
        if (node.VaryingName == "[None]") return VisualShaderValues.Literal(VisualShaderValues.Zero(node.ValueType));
        ValidateVaryingAccess(node, stage); var wire = varyings![node.VaryingName];
        return wire.Type == VisualShaderVaryingType.Boolean ? "(" + wire.Names[0] + " != 0u)" : wire.Names.Length == 1 ? wire.Names[0] : "mat4(" + string.Join(", ", wire.Names) + ")";
    }
    private void VaryingWrite(VisualShaderNodeVaryingSetter node, Type stage, string input, Dictionary<string, VaryingWire>? varyings, StringBuilder source)
    {
        if (node.VaryingName == "[None]") return;
        ValidateVaryingAccess(node, stage); var wire = varyings![node.VaryingName];
        for (var i = 0; i < wire.Names.Length; i++)
            source.Append("    ").Append(wire.Names[i]).Append(" = ").Append(wire.Type == VisualShaderVaryingType.Boolean ? "(" + input + " ? 1u : 0u)" : wire.Names.Length == 1 ? input : "(" + input + ")[" + i.ToString(CultureInfo.InvariantCulture) + "]").AppendLine(";");
    }
    private string[] FileVaryings() => _varyings.Select(p => "0/" + ((int)p.Value).ToString(CultureInfo.InvariantCulture) + "/" + p.Key).ToArray();
    private void RestoreVaryings(string[] values)
    {
        ArgumentNullException.ThrowIfNull(values); if (values.Length > MaximumNodes) throw new InvalidDataException("Varying declaration capacity exceeded.");
        var restored = new Dictionary<string, VisualShaderVaryingType>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var parts = value?.Split('/');
            if (parts is not { Length: 3 } || parts[0] != "0" || parts[1].Length != 1 || parts[1][0] is < '0' or > '7' || !VaryingIdentifier(parts[2]) || !restored.TryAdd(parts[2], (VisualShaderVaryingType)(parts[1][0] - '0')))
                throw new InvalidDataException("Invalid varying declaration table.");
        }
        _varyings.Clear(); foreach (var pair in restored) _varyings.Add(pair.Key, pair.Value); EmitChanged();
    }
}
