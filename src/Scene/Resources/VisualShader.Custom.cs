using System.Globalization;
using System.Text;
using P = Electron2D.VisualShaderNode.PortType;

namespace Electron2D;

public sealed partial class VisualShader
{
    private sealed record CustomBlock(string Source, string[] Inputs, string[] Outputs);
    private sealed class CustomPlan
    {
        internal readonly Dictionary<(Type Stage, int ID), CustomBlock> Blocks = [];
        internal readonly List<string> Globals = [];
        internal readonly Dictionary<Type, List<string>> Functions = new() { [Type.Vertex] = [], [Type.Fragment] = [] };
        internal IEnumerable<string> Sources(Type stage) => Globals.Concat(Functions[stage]).Concat(Blocks.Where(p => p.Key.Stage == stage).Select(p => p.Value.Source));
    }
    private CustomPlan? PrepareCustomCode()
    {
        if (!_graphs.Any(g => g.Nodes.Values.Any(e => e.Node is VisualShaderNodeCustom))) return null;
        var entries = new List<(Type Stage, int ID, VisualShaderNodeCustom Node, HashSet<int> Connected)>();
        foreach (var stage in new[] { Type.Vertex, Type.Fragment })
        {
            var graph = GetGraph(stage); var incoming = new Dictionary<(int Node, int Port), VisualShaderConnection>();
            foreach (var edge in graph.Connections)
            {
                if (!PortsExist(graph, edge) || !PortsCompatible(graph, edge)) throw new InvalidDataException("Custom graph connections require live compatible ports.");
                incoming[(edge.ToNode, edge.ToPort)] = edge;
            }
            var visited = new HashSet<int>(); var visiting = new HashSet<int>(); var stack = new Stack<(int ID, bool Exit)>(); stack.Push((0, false));
            while (stack.TryPop(out var step))
            {
                if (visited.Contains(step.ID)) continue;
                var node = graph.Nodes[step.ID].Node;
                if (step.Exit)
                {
                    visiting.Remove(step.ID); visited.Add(step.ID);
                    if (node is VisualShaderNodeCustom custom)
                        entries.Add((stage, step.ID, custom, Enumerable.Range(0, node.Inputs.Length).Where(p => incoming.ContainsKey((step.ID, p))).ToHashSet()));
                    continue;
                }
                if (!visiting.Add(step.ID)) throw new InvalidDataException("The reachable visual shader graph contains a cycle.");
                stack.Push((step.ID, true));
                for (var port = node.Inputs.Length - 1; port >= 0; port--)
                    if (incoming.TryGetValue((step.ID, port), out var edge)) stack.Push((edge.FromNode, false));
            }
        }
        if (entries.Count == 0) return null;
        var plan = new CustomPlan(); var providers = new HashSet<System.Type>(); var bytes = new long[2];
        void Budget(Type stage, string source)
        {
            if ((bytes[(int)stage] += ShaderInclude.SourceEncoding.GetByteCount(source)) > ShaderInclude.MaximumSourceBytes)
                throw new InvalidDataException("Combined custom shader source exceeds the import budget.");
        }
        foreach (var entry in entries)
        {
            var node = entry.Node;
            string Evaluate(Func<string> callback)
            {
                try { return node.EvaluateCode(callback); }
                catch (Exception error) { throw new InvalidDataException($"Custom provider {node.GetType().FullName}, {entry.Stage} node {entry.ID}: {error.Message}", error); }
            }
            if (providers.Add(node.GetType()))
            {
                var global = Evaluate(() => node.GetGlobalCode(GetMode())); plan.Globals.Add(global);
                foreach (var stage in new[] { Type.Vertex, Type.Fragment })
                {
                    Budget(stage, global); var function = Evaluate(() => node.GetFuncCode(GetMode(), stage)); Budget(stage, function); plan.Functions[stage].Add(function);
                }
            }
            var defaults = node.GetDefaultInputValues();
            var stem = "e2d_custom_" + (int)entry.Stage + "_" + entry.ID.ToString(CultureInfo.InvariantCulture);
            var inputs = Enumerable.Range(0, node.Inputs.Length).Select(p => entry.Connected.Contains(p) || defaults.Any(d => d.Port == p) ? stem + "_in_" + p.ToString(CultureInfo.InvariantCulture) : "").ToArray();
            if (node.Outputs.Contains(P.Sampler)) throw new NotSupportedException("Custom GLSL outputs cannot declare local samplers.");
            var outputs = Enumerable.Range(0, node.Outputs.Length).Select(p => stem + "_out_" + p.ToString(CultureInfo.InvariantCulture)).ToArray();
            var code = Evaluate(() => node.GetCode(Array.AsReadOnly(inputs), Array.AsReadOnly(outputs), GetMode(), entry.Stage));
            Budget(entry.Stage, code);
            plan.Blocks.Add((entry.Stage, entry.ID), new(code, inputs, outputs));
        }
        return plan;
    }
    private static string CustomGlobals(CustomPlan? plan, Type stage, Dictionary<string, string> aliases)
    {
        if (plan is null) return "";
        var source = new StringBuilder(); var replacements = ExpressionBuiltins(stage, aliases, true);
        foreach (var code in plan.Globals) source.Append(RewriteExpression(code, replacements)).AppendLine();
        CheckExpressionBudget(source); return source.ToString();
    }
    private static void EmitCustomFunctions(CustomPlan? plan, Type stage, Dictionary<string, string> aliases, StringBuilder source)
    {
        if (plan is null) return;
        var replacements = ExpressionBuiltins(stage, aliases);
        foreach (var code in plan.Functions[stage]) source.Append(RewriteExpression(code, replacements)).AppendLine();
        CheckExpressionBudget(source);
    }
    private static void EmitCustom(VisualShaderNodeCustom node, CustomBlock block, int id, IReadOnlyList<string> inputs,
        HashSet<int> connected, Type stage, Dictionary<string, string> aliases, HashSet<string> names,
        Dictionary<(int Node, int Port), string> values, StringBuilder source)
    {
        var replacements = ExpressionBuiltins(stage, aliases);
        string Local(string stem)
        {
            var name = stem;
            for (var suffix = 2; !names.Add(name); suffix++) name = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            return name;
        }
        for (var port = 0; port < block.Inputs.Length; port++)
        {
            if (block.Inputs[port].Length == 0) continue;
            var input = inputs[port];
            if (!connected.Contains(port))
            {
                input = Local("n_in" + id.ToString(CultureInfo.InvariantCulture) + "p" + port.ToString(CultureInfo.InvariantCulture));
                source.Append("    ").Append(VisualShaderValues.Name(node.Inputs[port])).Append(' ').Append(input).Append(" = ").Append(inputs[port]).AppendLine(";");
            }
            replacements[block.Inputs[port]] = node.Inputs[port] == P.Sampler ? input : "(" + input + ")";
        }
        for (var port = 0; port < block.Outputs.Length; port++)
        {
            var role = node.Outputs[port]; var name = Local("n" + id.ToString(CultureInfo.InvariantCulture) + "p" + port.ToString(CultureInfo.InvariantCulture));
            source.Append("    ").Append(VisualShaderValues.Name(role)).Append(' ').Append(name).Append(" = ").Append(VisualShaderValues.Literal(VisualShaderValues.Zero(role))).AppendLine(";");
            values.Add((id, port), name); replacements[block.Outputs[port]] = name;
        }
        source.AppendLine("    {").Append(RewriteExpression(block.Source, replacements)).AppendLine().AppendLine("    }"); CheckExpressionBudget(source);
    }
}
