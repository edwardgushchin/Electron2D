using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    /// <summary>The canvas stages sharing an authored varying.</summary>
    public enum VaryingMode
    {
        /// <summary>Vertex values are available to fragments and their per-light evaluations.</summary>
        VertexToFragmentLight = 0,
        /// <summary>Fragment values are available to the same fragment's per-light evaluations.</summary>
        FragmentToLight = 1,
        /// <summary>The nonselectable upper bound.</summary>
        Max = 2,
    }
    private readonly record struct VaryingDeclaration(VaryingMode Mode, VisualShaderVaryingType Type);
    private readonly Dictionary<string, VaryingDeclaration> _varyings = new(StringComparer.Ordinal);
    private static bool VaryingIdentifier(string name) => name.Length != 0 && (char.IsAsciiLetter(name[0]) || name[0] == '_') && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    /// <summary>Adds a named numeric value shared between canvas stages.</summary>
    /// <param name="name">A unique ASCII shader identifier.</param>
    /// <param name="mode">The vertex-to-fragment/light or fragment-to-light transport role.</param>
    /// <param name="type">The complete numeric value role.</param>
    /// <remarks>Declarations and node accesses are stored independently. Import validates names and roles of
    /// reachable accesses. Vertex transport has four native custom locations; a matrix consumes four.
    /// Fragment-to-light values use private per-fragment storage and no native locations. Unwritten values
    /// start at typed zero or matrix identity. Unused declarations consume no native locations.
    /// Source edits retain the previous compiled program.</remarks>
    /// <exception cref="ArgumentException">The name is invalid, duplicated or declaration capacity is exhausted.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A selector is undefined or nonselectable.</exception>
    /// <exception cref="ObjectDisposedException">This graph is disposed.</exception>
    public void AddVarying(string name, VaryingMode mode, VisualShaderVaryingType type)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name);
        if ((uint)mode >= (uint)VaryingMode.Max) throw new ArgumentOutOfRangeException(nameof(mode));
        if ((uint)type >= (uint)VisualShaderVaryingType.Max) throw new ArgumentOutOfRangeException(nameof(type));
        if (!VaryingIdentifier(name) || _varyings.Count == MaximumNodes || !_varyings.TryAdd(name, new(mode, type))) throw new ArgumentException("A varying requires a unique ASCII identifier within declaration capacity.", nameof(name));
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

    private sealed record VaryingWire(VaryingMode Mode, VisualShaderVaryingType Type, int Location, string[] Names);
    private Dictionary<string, VaryingWire> PrepareVaryings(Dictionary<string, VisualShaderNodeParameter> parameters, CustomPlan? custom)
    {
        var active = new HashSet<string>(StringComparer.Ordinal); var names = parameters.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var stage in new[] { Type.Vertex, Type.Fragment, Type.Light })
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
            var declaration = _varyings[name]; var type = declaration.Type; var count = type == VisualShaderVaryingType.Transform ? 4 : 1;
            var transport = declaration.Mode == VaryingMode.VertexToFragmentLight;
            if (transport && location + count > 16) throw new NotSupportedException("Canvas varyings exceed four custom native locations; each numeric matrix consumes four.");
            var wires = new string[count];
            for (var i = 0; i < count; i++)
                do { wires[i] = "e2d_varying_" + (serial++).ToString(CultureInfo.InvariantCulture); } while (!names.Add(wires[i]));
            result.Add(name, new(declaration.Mode, type, transport ? location : -1, wires)); if (transport) location += count;
        }
        return result;
    }
    private void ValidateVaryingAccess(VisualShaderNodeVarying node, Type stage)
    {
        if (!_varyings.TryGetValue(node.VaryingName, out var declaration) || declaration.Type != node.VaryingType)
            throw new InvalidDataException("A varying access requires an existing declaration with the same numeric role.");
        if (_mode != Mode.CanvasItem || stage is not (Type.Vertex or Type.Fragment or Type.Light) ||
            declaration.Mode == VaryingMode.FragmentToLight && stage == Type.Vertex ||
            node is VisualShaderNodeVaryingSetter && stage != (declaration.Mode == VaryingMode.FragmentToLight ? Type.Fragment : Type.Vertex))
            throw new NotSupportedException("A varying write requires its declared producer stage; reads require that stage or a later canvas stage.");
    }
    private static string VaryingHeader(Type stage, Dictionary<string, VaryingWire> varyings, string header)
    {
        if (varyings.Count == 0) return header;
        var declarations = new StringBuilder(); var initial = new StringBuilder();
        foreach (var wire in varyings.Values)
        {
            if (wire.Mode == VaryingMode.FragmentToLight && stage == Type.Vertex) continue;
            for (var i = 0; i < wire.Names.Length; i++)
            {
                var flat = wire.Type is VisualShaderVaryingType.Int or VisualShaderVaryingType.UInt or VisualShaderVaryingType.Boolean;
                var type = wire.Type == VisualShaderVaryingType.Transform ? "vec4" : wire.Type == VisualShaderVaryingType.Boolean ? "uint" : VisualShaderValues.Name((VisualShaderNode.PortType)wire.Type);
                var value = wire.Type == VisualShaderVaryingType.Boolean ? "0u" : wire.Type == VisualShaderVaryingType.Transform ? "vec4(" + string.Join(", ", Enumerable.Range(0, 4).Select(c => c == i ? "1.0" : "0.0")) + ")" : VisualShaderValues.Literal(VisualShaderValues.Zero((VisualShaderNode.PortType)wire.Type));
                if (wire.Mode == VaryingMode.VertexToFragmentLight)
                    declarations.Append("layout(location=").Append(wire.Location + i).Append(") ").Append(flat ? "flat " : "").Append(stage == Type.Vertex ? "out " : "in ");
                declarations.Append(type).Append(' ').Append(wire.Names[i]);
                if (wire.Mode == VaryingMode.FragmentToLight)
                    declarations.Append(" = ").Append(value);
                declarations.AppendLine(";");
                if (stage == Type.Vertex)
                {
                    initial.Append("    ").Append(wire.Names[i]).Append(" = ").Append(value).AppendLine(";");
                }
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
    private string[] FileVaryings() => _varyings.Select(p => ((int)p.Value.Mode).ToString(CultureInfo.InvariantCulture) + "/" + ((int)p.Value.Type).ToString(CultureInfo.InvariantCulture) + "/" + p.Key).ToArray();
    private void RestoreVaryings(string[] values)
    {
        ArgumentNullException.ThrowIfNull(values); if (values.Length > MaximumNodes) throw new InvalidDataException("Varying declaration capacity exceeded.");
        var restored = new Dictionary<string, VaryingDeclaration>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var parts = value?.Split('/');
            if (parts is not { Length: 3 } || parts[0] is not ("0" or "1") || parts[1].Length != 1 || parts[1][0] is < '0' or > '7' || !VaryingIdentifier(parts[2]) || !restored.TryAdd(parts[2], new((VaryingMode)(parts[0][0] - '0'), (VisualShaderVaryingType)(parts[1][0] - '0'))))
                throw new InvalidDataException("Invalid varying declaration table.");
        }
        _varyings.Clear(); foreach (var pair in restored) _varyings.Add(pair.Key, pair.Value); EmitChanged();
    }
}
