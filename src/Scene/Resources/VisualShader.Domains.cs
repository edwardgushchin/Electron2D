using System.Globalization;
using System.Text;
using P = Electron2D.VisualShaderNode.PortType;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

namespace Electron2D;

internal sealed class VisualShaderDomainSources(Shader.Mode mode, string? vertex, string fragmentOrCompute,
    IReadOnlyList<VisualShaderImage> images, IReadOnlyList<VisualShaderBlockName> blocks, List<Resource> owned) : IDisposable
{
    internal readonly Shader.Mode Mode = mode;
    internal readonly string? Vertex = vertex;
    internal readonly string Code = fragmentOrCompute;
    internal readonly IReadOnlyList<VisualShaderImage> Images = images;
    internal readonly IReadOnlyList<VisualShaderBlockName> Blocks = blocks;
    internal CanvasStageSource? Light { get; init; }
    internal CanvasStageSource? Surface { get; init; }
    public void Dispose()
    {
        List<Exception>? errors = null;
        foreach (var resource in owned) try { resource.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        owned.Clear(); if (errors is not null) throw new AggregateException(errors);
    }
}

public sealed partial class VisualShader
{
    private static readonly string[] DomainBuiltins = ["UV", "MODULATE", "COLOR", "CUSTOM", "VELOCITY", "TRANSFORM", "EMISSION_TRANSFORM", "ACTIVE", "RESTART", "RESTART_POSITION", "RESTART_ROT_SCALE", "RESTART_VELOCITY", "RESTART_COLOR", "RESTART_CUSTOM", "DELTA", "LIFETIME", "NUMBER", "INDEX", "RANDOM_SEED", "COLLIDED", "COLLISION_NORMAL", "COLLISION_DEPTH"];
    private static readonly string[] DomainSymbols = ["ParticleFrame", "ParticleState", "ParticleSubemission", "ParticleBirthNumbers", "BIRTH_NUMBERS", "Electron2D_Particle", "PARTICLES", "SUBPARTICLES", "Electron2D_EmitSubparticle", "ELECTRON2D_PARTICLE_EMIT_FLAGS_SHIFT", "ELECTRON2D_PARTICLE_EMIT_FLAGS_MASK", "e2d_seed", "e2d_range", "e2d_random", "e2d_hash", "e2d_rotation", "e2d_circle", "e2d_ring", "e2d_box", "e2d_cone", "e2d_accelerate", "e2d_emit", "e2d_collision", "e2d_particle", "e2d_color0", "e2d_color1", "e2d_color2", "e2d_color3", "e2d_blit_output0", "e2d_blit_output1", "e2d_blit_output2", "e2d_blit_output3"];
    private sealed class DomainCompilation
    {
        internal readonly Dictionary<Type, StagePlan> Plans = [];
        internal readonly Dictionary<Type, (List<VisualShaderImage> Images, Dictionary<int, string> Nodes)> ImagePlans = [];
        internal readonly Dictionary<(Type Stage, int ID), MeshEmissionBake> Meshes = [];
        internal Dictionary<string, string> Symbols = [];
        internal int BlitOutputs = 1;
        internal bool Canvas;
        internal SurfacePlan? Surface;
        internal string StageHeader(Type stage) => "#undef ELECTRON2D_SHADER_STAGE\n#define ELECTRON2D_SHADER_STAGE " + (int)stage + "\nvoid " + Symbols["e2d_stage_" + (int)stage] + "() {\n";
    }
    internal VisualShaderDomainSources GenerateDomainSources()
    {
        ThrowIfDisposed();
        if (_mode == Mode.CanvasItem)
        {
            var canvas = GenerateSources(); return new(_mode, canvas.Vertex, canvas.Fragment, canvas.Images, canvas.Blocks, []) { Light = canvas.Light, Surface = canvas.Surface };
        }
        var owned = new List<Resource>();
        try
        {
            var domain = new DomainCompilation(); var parameters = ParameterSymbols();
            if (parameters.Values.Any(p => p.Qualifier is Q.Instance or Q.InstanceIndex))
                throw new NotSupportedException("Particle and writable-texture graphs have no scene-instance parameter producer.");
            foreach (var stage in ActiveStages) domain.Plans.Add(stage, PlanStage(stage));
            var custom = PrepareCustomCode();
            var sourceIdentifiers = domain.Plans.SelectMany(p => ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom)).SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))).Concat(parameters.Keys).ToHashSet(StringComparer.Ordinal);
            foreach (var (stage, plan) in domain.Plans)
            {
                var reachable = plan.Order.ToHashSet(); var images = ImagePlan(stage, reachable, parameters, custom);
                foreach (var id in plan.Order)
                    if (GetNode(stage, id) is VisualShaderNodeParticleMeshEmitter mesh)
                    {
                        var ports = plan.Incoming.Values.Where(e => e.FromNode == id && reachable.Contains(e.ToNode)).Select(e => e.FromPort).ToHashSet();
                        var bake = BakeMeshEmission(mesh, stage, id, ports, images.Images, owned, sourceIdentifiers); domain.Meshes.Add((stage, id), bake);
                    }
                domain.ImagePlans.Add(stage, images);
            }
            var primary = _mode == Mode.Particles ? Type.Process : Type.TextureBlit;
            var imagesAll = new List<VisualShaderImage>();
            foreach (var image in domain.ImagePlans.Values.SelectMany(p => p.Images))
            {
                var canonical = image with { Stage = primary }; var previous = imagesAll.FirstOrDefault(i => i.Name == image.Name);
                if (previous is null) imagesAll.Add(canonical);
                else if (previous != canonical) throw new InvalidDataException("Conflicting graph image declarations across lifecycle stages.");
            }
            var collide = _mode == Mode.Particles && HasStageEffect(domain, Type.Collide, custom);
            var collision = _mode == Mode.Particles && (collide || domain.Plans.Any(p => ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom).Any(s => SourceIdentifiers(s).Any(t => !t.Member && s.Substring(t.Start, t.Length) is "COLLIDED" or "COLLISION_NORMAL" or "COLLISION_DEPTH"))));
            if (collision) imagesAll.Add(new(primary, "SDF_TEXTURE", false));
            if (imagesAll.Count > 16) throw new NotSupportedException("A shader stage supports at most sixteen sampled-image descriptors.");
            var names = domain.Plans.SelectMany(p => ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom))
                .SelectMany(s => SourceIdentifiers(s).Select(t => s.Substring(t.Start, t.Length))).Concat(parameters.Keys).ToHashSet(StringComparer.Ordinal);
            domain.Symbols = StageSymbols(imagesAll, names); names.UnionWith(domain.Symbols.Values);
            foreach (var role in DomainBuiltins.Concat(DomainSymbols).Concat(GraphStages.Select(s => "e2d_stage_" + (int)s)))
            {
                if (domain.Symbols.ContainsKey(role)) continue;
                var symbol = role;
                for (var suffix = 2; !names.Add(symbol); suffix++) symbol = role + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                domain.Symbols.Add(role, symbol);
            }
            if (_mode == Mode.TextureBlit)
            {
                var root = GetNode(Type.TextureBlit, 0);
                domain.BlitOutputs = Enumerable.Range(0, 4).Where(p => domain.Plans[Type.TextureBlit].Incoming.ContainsKey((0, p)) || root.GetDefaultInputValues().Any(d => d.Port == p)).DefaultIfEmpty(0).Max() + 1;
            }
            var blocks = new List<VisualShaderBlockName>();
            var header = DomainParameterHeader(domain, parameters, imagesAll, blocks, custom, primary);
            var source = new StringBuilder(header);
            foreach (var stage in ActiveStages) source.Append(GenerateStage(stage, parameters, [], [], [], custom, domain));
            source.Append(_mode == Mode.Particles ? ParticleEntry(domain, collision, collide) : BlitEntry(domain));
            var code = source.ToString();
            if (ShaderInclude.SourceEncoding.GetByteCount(code) > ShaderInclude.MaximumSourceBytes) throw new InvalidDataException("Generated shader source exceeds the import budget.");
            return new(_mode, null, code, imagesAll, blocks, owned);
        }
        catch (Exception failure)
        {
            List<Exception>? cleanup = null;
            foreach (var resource in owned) try { resource.Dispose(); } catch (Exception error) { (cleanup ??= []).Add(error); }
            if (cleanup is not null) throw new AggregateException(new[] { failure }.Concat(cleanup));
            throw;
        }
    }
    private bool HasStageEffect(DomainCompilation domain, Type stage, CustomPlan? custom) => domain.Plans[stage].Order.Count > 1 || GetNode(stage, 0).GetDefaultInputValues().Count != 0 || custom?.Functions[stage].Any(source => source.Length != 0) == true;
    private string DomainParameterHeader(DomainCompilation domain, Dictionary<string, VisualShaderNodeParameter> parameters,
        List<VisualShaderImage> images, List<VisualShaderBlockName> blocks, CustomPlan? custom, Type primary)
    {
        var aliases = domain.Symbols; var result = new StringBuilder("#version 450\n#extension GL_GOOGLE_include_directive : require\n#define ELECTRON2D_SHADER_STAGE " + (int)primary + "\n");
        var used = domain.Plans.SelectMany(p => p.Value.Order.Select(id => GetNode(p.Key, id))).ToArray();
        var usedNames = used.OfType<VisualShaderNodeParameter>().Select(p => p.ParameterName).Concat(used.OfType<VisualShaderNodeParameterRef>().Select(p => p.ParameterName)).Where(n => n != "[None]").ToHashSet(StringComparer.Ordinal);
        var selected = usedNames.Order(StringComparer.Ordinal).Select(n => parameters.TryGetValue(n, out var p) ? p : throw new InvalidDataException($"Graph parameter '{n}' has no declaration.")).Where(p => p is not VisualShaderNodeTextureParameter).ToArray();
        var renderer = selected.Any(p => p.Qualifier == Q.None) || used.OfType<VisualShaderNodeInput>().Any(p => p.InputName == "time") || domain.Plans.Any(p => ReadsRendererValues(ExpressionSources(p.Key, p.Value.Order.ToHashSet(), custom)));
        var particle = _mode == Mode.Particles;
        if (particle) result.Append(ParticleInterface(domain, blocks));
        else
        {
            result.AppendLine(RewriteExpression("layout(location=0) in vec4 inColor;\nlayout(location=1) in vec2 inUV;", aliases));
            result.AppendLine(RewriteExpression("vec4 MODULATE; vec2 UV; vec4 e2d_color0, e2d_color1, e2d_color2, e2d_color3;", aliases));
            for (var i = 0; i < domain.BlitOutputs; i++) result.Append("layout(location=").Append(i).Append(") out vec4 ").Append(aliases["e2d_blit_output" + i]).AppendLine(";");
        }
        var binding = particle ? 1 : 0; var set = particle ? 2 : 3;
        if (renderer)
        {
            result.Append("layout(set=").Append(set).Append(",binding=").Append(binding++).Append(",std140) uniform ").Append(aliases["RendererValues"]).AppendLine(" { float TIME;");
            foreach (var p in selected.Where(p => p.Qualifier == Q.None)) result.Append(VisualShaderValues.Name(p.Outputs[0])).Append(' ').Append(p.ParameterName).AppendLine(";");
            result.Append("} ").Append(aliases["e2d_values"]).AppendLine(";");
        }
        if (selected.Any(p => p.Qualifier == Q.Global))
        {
            result.Append("layout(set=").Append(set).Append(",binding=").Append(binding).Append(",std140) uniform ").Append(aliases["Globals"]).AppendLine(" {");
            foreach (var p in selected.Where(p => p.Qualifier == Q.Global)) result.Append(VisualShaderValues.Name(p.Outputs[0])).Append(' ').Append(p.ParameterName).AppendLine(";");
            result.Append("} ").Append(aliases["e2d_globals"]).AppendLine(";");
        }
        foreach (var role in new[] { "RendererValues", "Globals" })
            if (aliases[role] != role) blocks.Add(new(primary, aliases[role], role));
        for (var i = 0; i < images.Count; i++) result.Append("layout(set=").Append(particle ? 0 : 2).Append(",binding=").Append(i).Append(") uniform ").Append(images[i].IsArray ? "sampler2DArray " : "sampler2D ").Append(images[i].Name).AppendLine(";");
        if (used.Any(n => n is VisualShaderNodeColorFunc { Function: VisualShaderNodeColorFunc.FunctionEnum.RGBToHSV })) result.Append(VisualShaderNodeColorFunc.RGBToHSVDefinition(aliases["e2d_rgb_to_hsv"]));
        if (used.Any(n => n is VisualShaderNodeRandomRange)) result.Append(VisualShaderNodeRandomRange.HashDefinition(aliases["e2d_random_range"]));
        if (used.Any(n => n is VisualShaderNodeRotationByAxis)) result.Append(VisualShaderNodeRotationByAxis.RotationDefinition(aliases["e2d_axis_rotation"]));
        if (particle) result.AppendLine(RewriteExpression(ParticleMath, aliases));
        result.Append(CustomGlobals(custom, primary, aliases)).Append(GlobalExpressionHeader(primary, aliases)).AppendLine();
        return result.ToString();
    }
    private static string? DomainInputDefault(VisualShaderNode node, int port) => node switch
    {
        VisualShaderNodeParticleRandomness when port == 0 => "e2d_seed",
        VisualShaderNodeParticleEmit => port switch { 1 => "TRANSFORM", 2 => "VELOCITY.xy", 3 => "COLOR.rgb", 4 => "COLOR.a", 5 => "CUSTOM.rgb", 6 => "CUSTOM.a", _ => null },
        _ => null
    };
    private static void EmitDomainOutput(Type stage, VisualShaderNode root, Dictionary<(int Node, int Port), VisualShaderConnection> incoming,
        Func<int, int, string> input, StringBuilder source, Dictionary<string, string> aliases, DomainCompilation domain)
    {
        bool Has(int p) => incoming.ContainsKey((0, p)) || root.GetDefaultInputValues().Any(d => d.Port == p);
        void Emit(string text) => source.AppendLine(RewriteExpression(text, aliases));
        void Set(int p, string destination) { if (Has(p)) Emit(destination + " = " + input(0, p) + ";"); }
        if (domain.Canvas)
        {
            if (stage == Type.Light) { Set(0, "LIGHT.rgb"); Set(1, "LIGHT.a"); Set(2, "SHADOW_MODULATE.rgb"); }
            else
            {
                Set(0, "COLOR.rgb"); Set(1, "COLOR.a");
                if (domain.Surface is not null)
                {
                    Set(2, "NORMAL"); Set(3, "NORMAL_MAP"); Set(4, "NORMAL_MAP_DEPTH"); Set(5, "LIGHT_VERTEX"); Set(6, "SHADOW_VERTEX");
                }
                else for (var p = 2; p < root.Inputs.Length; p++) if (Has(p)) throw new NotSupportedException("This canvas fragment output requires its surface-state integration.");
            }
            return;
        }
        if (stage == Type.TextureBlit) { for (var p = 0; p < domain.BlitOutputs; p++) Set(p, "e2d_color" + p); return; }
        if (stage is Type.StartCustom or Type.ProcessCustom)
        { Set(0, "CUSTOM.rgb"); Set(1, "CUSTOM.a"); Set(2, "VELOCITY.xy"); Set(3, "COLOR.rgb"); Set(4, "COLOR.a"); Set(5, "TRANSFORM"); return; }
        if (Has(0)) { Set(0, "ACTIVE"); Emit("if (ACTIVE) {"); }
        Set(1, "VELOCITY.xy"); Set(2, "COLOR.rgb"); Set(3, "COLOR.a");
        if (stage == Type.Start)
        {
            Emit("if (RESTART_POSITION) {"); Emit("TRANSFORM = mat4(1.0);");
            if (Has(4)) Emit("TRANSFORM[3].xy = " + input(0, 4) + ";");
            Emit("if (RESTART_VELOCITY) VELOCITY = (EMISSION_TRANSFORM * vec4(VELOCITY, 0.0)).xyz;");
            Emit("TRANSFORM = EMISSION_TRANSFORM * TRANSFORM; }");
        }
        else if (stage == Type.Collide) Set(4, "TRANSFORM");
        if (stage is Type.Start or Type.Process)
        {
            var scale = stage == Type.Start ? 5 : 4; var angle = scale + 1; var op = stage == Type.Start ? "*=" : "=";
            if (Has(angle)) Emit("TRANSFORM " + op + " mat4(e2d_rotation(vec3(0,0,1), -(" + input(0, angle) + ")));");
            if (Has(scale)) Emit("TRANSFORM " + op + " mat4(vec4(" + input(0, scale) + ",0,0,0), vec4(0," + input(0, scale) + ",0,0), vec4(0,0,1,0), vec4(0,0,0,1));");
        }
        if (Has(0)) Emit("}");
    }
    private static bool EmitDomainNode(DomainCompilation domain, Type stage, VisualShaderNode node, int id, string[] inputs,
        Dictionary<(int Node, int Port), VisualShaderConnection> incoming, HashSet<string> names, Dictionary<(int Node, int Port), string> values, StringBuilder source)
    {
        if (node is VisualShaderNodeParticleEmit emit)
        {
            source.AppendLine(RewriteExpression("if (" + inputs[0] + ") e2d_emit(" + inputs[1] + ", vec3(" + inputs[2] + ",0.0), vec4(" + inputs[3] + "," + inputs[4] + "), vec4(" + inputs[5] + "," + inputs[6] + "), " + (int)emit.Flags + "u);", domain.Symbols)); return true;
        }
        if (node is VisualShaderNodeParticleMeshEmitter) { EmitMeshEmission(domain, stage, id, values, source, names); return true; }
        if (node is VisualShaderNodeParticleRandomness && !incoming.ContainsKey((id, 0)) && node.GetDefaultInputValues().Any(d => d.Port == 0))
        {
            var symbol = "n" + id + "seed"; while (!names.Add(symbol)) symbol += "_";
            source.Append("uint ").Append(symbol).Append(" = ").Append(inputs[0]).AppendLine(";"); inputs[0] = symbol;
        }
        return false;
    }
    private static string BlitEntry(DomainCompilation domain)
    {
        var text = new StringBuilder("void main() {\nMODULATE = inColor; UV = inUV; e2d_color0 = e2d_color1 = e2d_color2 = e2d_color3 = vec4(0,0,0,1);\n");
        text.Append(domain.Symbols["e2d_stage_10"]).AppendLine("();");
        for (var i = 0; i < domain.BlitOutputs; i++) text.Append(domain.Symbols["e2d_blit_output" + i]).Append(" = ").Append(domain.Symbols["e2d_color" + i]).AppendLine(";");
        return RewriteExpression(text.AppendLine("}").ToString(), domain.Symbols);
    }
}
