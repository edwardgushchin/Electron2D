using System.Text;
using System.Text.RegularExpressions;
using Electron2D;

internal static unsafe partial class ShaderSourceTypes
{
    internal static byte[] AddHLSLDefaults(byte[] code, string source, RenderingDevice.ShaderStage stage, string? includes, Shader.Mode mode)
    {
        var program = ShaderCompiler.ValidateInterface(code, stage == RenderingDevice.ShaderStage.Fragment, mode: mode);
        var active = program.Uniforms.Concat(program.InstanceUniforms).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        var reserved = program.GlobalUniforms.Keys.Concat(new[] { "TIME", "SCREEN_PIXEL_SIZE", "SDF_COORDS" }).ToHashSet(StringComparer.Ordinal);
        var bindings = BufferBindings(code, stage == RenderingDevice.ShaderStage.Fragment, mode == Shader.Mode.Particles);
        var text = Encoding.UTF8.GetString(CompileHLSL(source, stage, includes, preprocess: true)).TrimEnd('\0');
        var tokens = DefaultTokens(text); var replacements = new List<(int Start, int Length, string Text)>();
        var initialized = new Dictionary<string, (ShaderUniform Uniform, string Expression)>(StringComparer.Ordinal);
        var scopes = new Stack<string?>();
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Text == "cbuffer")
            {
                var opening = i + 1; while (opening < tokens.Count && tokens[opening].Text != "{") opening++;
                var closing = DefaultMatch(tokens, opening, "{", "}");
                var prefix = string.Join("::", scopes.Reverse().Where(s => !string.IsNullOrEmpty(s)));
                var bufferName = tokens[i + 1].Text;
                var qualified = prefix.Length == 0 ? bufferName : prefix + "::" + bufferName;
                var matches = bindings.Where(p => p.Key.Replace("::", ".", StringComparison.Ordinal) == qualified.Replace("::", ".", StringComparison.Ordinal)).ToArray();
                if (matches.Length == 0) matches = bindings.Where(p => p.Key == bufferName).ToArray();
                var binding = matches.Length == 1 ? matches[0].Value : -1;
                var header = i;
                while (header > 0 && tokens[header - 1].Text == "]") header = DefaultMatchBack(tokens, header - 1, "[", "]");
                replacements.Add((tokens[header].Start, tokens[opening].End - tokens[header].Start, "\n"));
                var exports = new List<string>();
                var statement = opening + 1; var nesting = 0;
                for (var member = statement; member < closing; member++)
                {
                    if (tokens[member].Text is "(" or "[" or "{") nesting++;
                    if (tokens[member].Text is ")" or "]" or "}") nesting--;
                    if (tokens[member].Text != ";" || nesting != 0) continue;
                    if (statement != member)
                    {
                        var segment = statement; var depth = 0; string? declarationType = null;
                        for (var part = statement; part <= member; part++)
                        {
                            if (tokens[part].Text is "(" or "[" or "{") depth++;
                            if (tokens[part].Text is ")" or "]" or "}") depth--;
                            if (depth != 0 || tokens[part].Text is not ("," or ";")) continue;
                            var equal = -1; var annotations = new HashSet<int>();
                            for (var token = segment; token < part; token++)
                            {
                                if (tokens[token].Text == "[")
                                {
                                    var end = DefaultMatch(tokens, token, "[", "]");
                                    if (token + 1 < part && tokens[token + 1].Text == "[")
                                    {
                                        replacements.Add((tokens[token].Start, tokens[end].End - tokens[token].Start, " "));
                                        for (var a = token; a <= end; a++) annotations.Add(a);
                                    }
                                    token = end; continue;
                                }
                                if (tokens[token].Text == "=" && equal < 0) { equal = token; break; }
                                if (tokens[token].Text is "const" or "uniform" or "static") replacements.Add((tokens[token].Start, tokens[token].End - tokens[token].Start, " "));
                                if (tokens[token].Text == ":" && token + 2 < part && tokens[token + 1].Text == "packoffset" && tokens[token + 2].Text == "(")
                                {
                                    var end = DefaultMatch(tokens, token + 2, "(", ")");
                                    replacements.Add((tokens[token].Start, tokens[end].End - tokens[token].Start, " "));
                                    for (var a = token; a <= end; a++) annotations.Add(a);
                                    token = end;
                                }
                            }
                            var declared = -1;
                            for (var name = segment; name < (equal < 0 ? part : equal); name++)
                            {
                                if (annotations.Contains(name)) continue;
                                if (tokens[name].Text == "[") { name = DefaultMatch(tokens, name, "[", "]"); continue; }
                                if (char.IsLetter(tokens[name].Text[0]) || tokens[name].Text[0] == '_') declared = name;
                            }
                            if (declared < 0) throw new ArgumentException("Cannot resolve a source uniform declaration.");
                            declarationType ??= string.Join(" ", Enumerable.Range(statement, declared - statement)
                                .Where(t => !annotations.Contains(t) && tokens[t].Text is not ("const" or "uniform" or "static")).Select(t => tokens[t].Text));
                            if (segment == statement)
                            { if (equal >= 0) replacements.Add((tokens[statement].Start, 0, "static ")); }
                            else replacements.Add((tokens[segment - 1].Start, 1, "; " + (equal >= 0 ? "static " : "") + declarationType + " "));
                            if (prefix.Length != 0) exports.Add(prefix + "::" + tokens[declared].Text);
                            if (equal >= 0)
                            {
                                var candidate = -1;
                                for (var name = segment; name < equal; name++)
                                {
                                    if (annotations.Contains(name)) continue;
                                    if (tokens[name].Text == "[") { name = DefaultMatch(tokens, name, "[", "]"); continue; }
                                    var field = tokens[name].Text;
                                    if (reserved.Contains(field) || binding == 0 && (stage == RenderingDevice.ShaderStage.Vertex || mode == Shader.Mode.Particles))
                                        throw new ArgumentException("Source defaults cannot initialize renderer-owned or global uniform buffers.");
                                    if (active.TryGetValue(field, out var value))
                                    {
                                        if (value.Buffer != binding) throw new ArgumentException($"Cannot resolve the source buffer for default '{field}'.");
                                        candidate = name;
                                    }
                                }
                                if (candidate >= 0)
                                {
                                    var name = tokens[candidate].Text;
                                    if (!initialized.TryAdd(name, (active[name], prefix.Length == 0 ? name : prefix + "::" + name)))
                                        throw new ArgumentException($"Ambiguous source default for '{name}'.");
                                }
                            }
                            segment = part + 1;
                        }
                    }
                    statement = member + 1;
                }
                var namespaces = scopes.Reverse().Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var ending = "\n";
                if (exports.Count != 0)
                    ending += new string('}', namespaces.Length) + "\n" + string.Join("\n", exports.Select(n => "using " + n + ";")) + "\n" + string.Concat(namespaces.Select(n => "namespace " + n + " {")) + "\n";
                replacements.Add((tokens[closing].Start, tokens[closing].End - tokens[closing].Start, ending));
                i = closing; continue;
            }
            if (tokens[i].Text == "{")
            {
                var start = i - 1; while (start >= 0 && tokens[start].Text is not (";" or "{" or "}")) start--;
                var declaration = tokens.Skip(start + 1).Take(i - start - 1).ToArray();
                scopes.Push(declaration.Length > 0 && declaration[0].Text == "namespace" ? string.Concat(declaration.Skip(1).Select(t => t.Text)) : null);
            }
            else if (tokens[i].Text == "}" && scopes.Count != 0) scopes.Pop();
        }
        if (initialized.Count == 0) return code;
        foreach (var replacement in replacements.OrderByDescending(r => r.Start).ThenByDescending(r => r.Length))
            text = text.Remove(replacement.Start, replacement.Length).Insert(replacement.Start, replacement.Text);
        var identifiers = tokens.Select(t => t.Text).ToHashSet(StringComparer.Ordinal);
        string Fresh(string seed) { while (!identifiers.Add(seed)) seed += "_"; return seed; }
        var original = Fresh("Electron2DOriginalMain"); var output = Fresh("Electron2DDefaults");
        var kernel = new StringBuilder("#define main " + original + "\n" + text + "\n#undef main\nRWStructuredBuffer<uint> " + output + " : register(u0,space1);\n");
        kernel.Append(stage == RenderingDevice.ShaderStage.Compute ? "[numthreads(1,1,1)] void main(){\n" : stage == RenderingDevice.ShaderStage.Vertex ? "float4 main():SV_Position{\n" : "float4 main():SV_Target{\n");
        var declarations = initialized.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value).ToArray(); var count = 0;
        foreach (var declaration in declarations)
            for (var element = 0; element < declaration.Uniform.Count; element++)
                for (var component = 0; component < declaration.Uniform.DefaultWordWidth; component++)
                {
                    var uniform = declaration.Uniform;
                    var expression = declaration.Expression + (uniform.ArrayLength == 0 ? "" : $"[{element}]");
                    // DXC represents each HLSL row as a SPIR-V matrix column.
                    if (uniform.DefaultMatrixColumns != 0) expression += $"[{component / uniform.DefaultMatrixRows}][{component % uniform.DefaultMatrixRows}]";
                    else if (uniform.DefaultWordWidth > 1) expression += $"[{component}]";
                    expression = uniform.BooleanWidth != 0 ? "uint(" + expression + ")" : uniform.Unsigned ? expression : "asuint(" + expression + ")";
                    kernel.Append(output).Append('[').Append(count++).Append("] = ").Append(expression).Append(";\n");
                }
        if (stage != RenderingDevice.ShaderStage.Compute) kernel.Append("return float4(0,0,0,0);\n");
        kernel.Append('}');
        var values = DefaultStores(CompileHLSL(kernel.ToString(), stage, null, defaults: true), output, count);
        var records = new List<string>(); var offset = 0;
        foreach (var declaration in declarations)
        {
            var length = declaration.Uniform.DefaultWordWidth * declaration.Uniform.Count;
            records.Add(DefaultRecord(declaration.Uniform, values.AsSpan(offset, length))); offset += length;
        }
        return AppendMetadata(code, records);
    }

    private static List<(string Text, int Start, int End)> DefaultTokens(string text) => Regex.Matches(text,
        "^[ \\t]*#[^\\r\\n]*|\\b(?:vector|matrix)\\s*<[^<>]+>|[\\p{L}_][\\p{L}\\p{Nd}_]*|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'|[^\\s]",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)
        .Where(m => !m.Value.TrimStart().StartsWith('#')).Select(m => (m.Value, m.Index, m.Index + m.Length)).ToList();

    private static int DefaultMatch(List<(string Text, int Start, int End)> tokens, int at, string open, string close)
    {
        if ((uint)at >= (uint)tokens.Count || tokens[at].Text != open) throw new ArgumentException("Malformed preprocessed default declaration.");
        var depth = 0;
        for (var i = at; i < tokens.Count; i++) { if (tokens[i].Text == open) depth++; if (tokens[i].Text == close && --depth == 0) return i; }
        throw new ArgumentException("Unterminated preprocessed default declaration.");
    }
    private static int DefaultMatchBack(List<(string Text, int Start, int End)> tokens, int at, string open, string close)
    {
        var depth = 0;
        for (var i = at; i >= 0; i--) { if (tokens[i].Text == close) depth++; if (tokens[i].Text == open && --depth == 0) return i; }
        throw new ArgumentException("Unterminated preprocessed default attribute.");
    }
    private static uint[] DefaultStores(byte[] code, string output, int count)
    {
        var instructions = MaterialLightingProgram.Module.Read(code); var constants = new Dictionary<uint, uint>(); var knownConstants = new HashSet<uint>();
        var pointers = new Dictionary<uint, int>(); var outputs = new HashSet<uint>(); uint target = 0; var function = false;
        foreach (var instruction in instructions)
        {
            var op = MaterialLightingProgram.Op(instruction);
            if (op == 5)
            {
                var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(instruction.AsSpan(2));
                if (Encoding.UTF8.GetString(bytes[..bytes.IndexOf((byte)0)]) == output) target = instruction[1];
            }
            if (op == 43 && instruction.Length == 4) constants[instruction[2]] = instruction[3];
            if (op is 41 or 42 or 43 or 44 or 46) knownConstants.Add(instruction[2]);
            if (op == 59 && instruction[3] == 3) outputs.Add(instruction[2]);
        }
        if (target == 0) throw new ArgumentException("The compiler lost constant default output.");
        var result = new uint[count]; var assigned = new bool[count];
        foreach (var instruction in instructions)
        {
            var op = MaterialLightingProgram.Op(instruction);
            if (op == 54) { if (function) throw new ArgumentException("Malformed constant default function."); function = true; continue; }
            if (!function) continue;
            if (op == 56) { function = false; continue; }
            if (op is 0 or 8 or 248 or 253 or 317) continue;
            if (op is 65 or 66 && instruction.Length == 6 && instruction[3] == target && constants.GetValueOrDefault(instruction[4], uint.MaxValue) == 0 && constants.TryGetValue(instruction[5], out var index) && index < count)
            { pointers.Add(instruction[2], checked((int)index)); continue; }
            if (op == 62 && instruction.Length == 3)
            {
                if (outputs.Contains(instruction[1]) && knownConstants.Contains(instruction[2])) continue;
                if (pointers.TryGetValue(instruction[1], out var slot) && constants.TryGetValue(instruction[2], out var value) && !assigned[slot])
                { result[slot] = value; assigned[slot] = true; continue; }
            }
            throw new NotSupportedException("Authored uniform defaults must fold to complete constant values without runtime input or control flow.");
        }
        if (assigned.Any(a => !a)) throw new NotSupportedException("The compiler did not resolve every uniform default component.");
        return result;
    }
}
