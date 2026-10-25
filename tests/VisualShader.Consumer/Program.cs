using Electron2D;
using IOPath = System.IO.Path;

try
{
    CustomNodes.Register();
    if (args.Length > 0 && args[0] == "light-graphs-cpu") GraphChecks.LightGraphsCPU();
    else if (args.Length > 0 && args[0] == "light-graphs-author") GraphChecks.AuthorLightGraphs(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "light-graphs-gpu") GraphChecks.RenderLightGraphs(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "matrix-inputs-cpu") GraphChecks.MatrixInputsCPU();
    else if (args.Length > 0 && args[0] == "matrix-inputs-author") GraphChecks.AuthorMatrixInputs(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "matrix-inputs-gpu") GraphChecks.RenderMatrixInputs(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "curves-cpu") GraphChecks.CurvesCPU();
    else if (args.Length > 0 && args[0] == "curves-author") GraphChecks.AuthorCurves(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "curves-gpu") GraphChecks.RenderCurves(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "mesh-inputs-cpu") GraphChecks.MeshInputsCPU();
    else if (args.Length > 0 && args[0] == "mesh-inputs-author") GraphChecks.AuthorMeshInputs(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "mesh-inputs-gpu") GraphChecks.RenderMeshInputs(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "varyings-cpu") GraphChecks.VaryingsCPU();
    else if (args.Length > 0 && args[0] == "varyings-author") GraphChecks.AuthorVaryings(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "varyings-metadata") GraphChecks.VaryingsMetadata(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "varyings-gpu") GraphChecks.RenderVaryings(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "size-cpu") GraphChecks.TextureSizeCPU();
    else if (args.Length > 0 && args[0] == "size-author") GraphChecks.AuthorTextureSize(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "size-gpu") GraphChecks.RenderTextureSize(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "fresnel-cpu") GraphChecks.FresnelCPU();
    else if (args.Length > 0 && args[0] == "fresnel-gpu") GraphChecks.RenderFresnel(IOPath.GetFullPath(args[1]), args);
    else if (args.Length > 0 && args[0] == "fresnel-author") GraphChecks.AuthorFresnel(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "reroute-cpu") GraphChecks.RerouteCPU();
    else if (args.Length > 0 && args[0] == "reroute-author") GraphChecks.AuthorReroutes(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "orientation-cpu") GraphChecks.OrientationCPU();
    else if (args.Length > 0 && args[0] == "orientation-author") GraphChecks.AuthorOrientation(IOPath.GetFullPath(args[1]));
    else if (args.Length > 0 && args[0] == "random-cpu") GraphChecks.RandomRangeCPU();
    else if (args.Length > 0 && args[0] == "random-author") GraphChecks.AuthorRandomRange(System.IO.Path.GetFullPath(args[1]));
    else if (args.Length == 0 || args[0] == "cpu") GraphChecks.CPU();
    else if (args[0] == "domains-gpu") GraphChecks.RenderDomains(IOPath.GetFullPath(args[1]), args);
    else if (args[0] == "domains-cpu") GraphChecks.DomainCPU();
    else if (args[0] == "domains-author") GraphChecks.AuthorDomains(IOPath.GetFullPath(args[1]));
    else if (args[0] == "author") GraphChecks.Author(IOPath.GetFullPath(args[1]));
    else if (args[0] == "edit")
    {
        using var graph = ResourceLoader.Load<VisualShader>(args[1], ResourceLoader.CacheMode.IgnoreDeep);
        ((VisualShaderNodeColorConstant)graph.GetNode(VisualShader.Type.Fragment, 2)).Constant = new(float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture), .5f, .75f, 1);
        ResourceSaver.Save(graph, args[1], SaverFlags.BundleResources);
    }
    else if (args[0] == "gpu") GraphChecks.Render(IOPath.GetFullPath(args[1]), args);
    else if (args[0] == "images-metadata") GraphChecks.ImageMetadataChecks(IOPath.GetFullPath(args[1]));
    else if (args[0] == "expressions-author") GraphChecks.AuthorExpressions(IOPath.GetFullPath(args[1]));
    else if (args[0] == "custom-author") GraphChecks.AuthorCustom(IOPath.GetFullPath(args[1]));
    else if (args[0] == "custom-cpu") GraphChecks.CustomCPU();
    else if (args[0] == "expressions-metadata") GraphChecks.ExpressionMetadata(IOPath.GetFullPath(args[1]));
    else if (args[0] == "sdf-author") GraphChecks.AuthorSDF(IOPath.GetFullPath(args[1]));
    else if (args[0] == "sdf-metadata") GraphChecks.SDFMetadata(IOPath.GetFullPath(args[1]));
    else if (args[0] == "sdf-gpu") GraphChecks.RenderSDF(IOPath.GetFullPath(args[1]), args);
    else if (args[0] == "effects-author") GraphChecks.AuthorEffects(IOPath.GetFullPath(args[1]));
    else if (args[0] == "images-author") GraphChecks.AuthorImages(IOPath.GetFullPath(args[1]));
    else if (args[0] == "derivatives-metadata") GraphChecks.DerivativeBytecodeChecks(IOPath.GetFullPath(args[1]));
    else if (args[0] == "derivatives-author") GraphChecks.AuthorDerivatives(IOPath.GetFullPath(args[1]));
    else if (args[0] == "parameters-author") GraphChecks.AuthorParameters(IOPath.GetFullPath(args[1]));
    else if (args[0] == "parameters-metadata") GraphChecks.ParameterMetadataChecks(IOPath.GetFullPath(args[1]));
    else if (args[0] == "read") GraphChecks.Read(IOPath.GetFullPath(args[1]));
    else throw new ArgumentException("Unknown graph check.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

internal static partial class GraphChecks
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void CPU()
    {
        LightGraphsCPU(); CurvesCPU(); MatrixInputsCPU();
        MeshInputsCPU();
        VaryingsCPU();
        RerouteCPU();
        UnaryCPU();
        DataflowCPU();
        RangesCPU();
        IntegersCPU();
        ConditionsCPU();
        MatricesCPU();
        ParametersCPU();
        DerivativesCPU();
        ImagesCPU();
        EffectsCPU();
        SDFCPU();
        ExpressionsCPU();
        CustomCPU();
        using var graph = new VisualShader();
        using var scalar = new VisualShaderNodeFloatConstant { Constant = .375f };
        using var operation = new VisualShaderNodeFloatOp();
        var scalarChanges = 0; scalar.Changed += _ => scalarChanges++;
        scalar.Constant = MathF.BitIncrement(.375f);
        Check(scalar.Constant == .375f && scalarChanges == 0, "Approximately equal constants preserve the stored value and suppress change events.");
        var operationChanges = 0; operation.Changed += _ => operationChanges++;
        operation.Operator = VisualShaderNodeFloatOp.OperatorEnum.Add;
        Check(operationChanges == 0, "An unchanged arithmetic selection emits no change event.");
        var storedConnections = (PropertyDescriptor<VisualShader, int[]>)graph.GetPropertyList().Single(p => p.Name == "GraphConnections");
        Reject<InvalidDataException>(() => storedConnections.SetValue(graph, new int[(4096 * 8 + 1) * 5]));
        Check(graph.GetNodeConnections(VisualShader.Type.Vertex).Length == 0, "Stored edges enforce the per-stage budget before commitment.");
        using (var input = new VisualShaderNodeInput())
        {
            var inputChanges = 0;
            input.Changed += _ => throw new InvalidOperationException("Observer failure");
            input.InputTypeChanged += () => inputChanges++;
            Reject<InvalidOperationException>(() => input.InputName = "uv");
            Check(input.InputName == "uv" && inputChanges == 1, "A committed type change publishes both channels despite an observer failure.");
        }
        Check(graph.GetNodeList(VisualShader.Type.Vertex).SequenceEqual(new[] { 0 }) && graph.GetValidNodeID(VisualShader.Type.Fragment) == 2, "Initial stage IDs.");
        graph.AddNode(VisualShader.Type.Fragment, scalar, new(4, 8), 2);
        graph.AddNode(VisualShader.Type.Fragment, operation, new(12, 16), 3);
        graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
        graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0);
        Check(!graph.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 3, 1), "Self cycle rejected.");
        Check(!graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0), "Duplicate rejected.");
        Reject<ArgumentException>(() => graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 3, 1));
        Reject<ArgumentException>(() => graph.AddNode(VisualShader.Type.Fragment, scalar, new(float.NaN, 0), 4));
        Reject<ArgumentException>(() => graph.AddNode(VisualShader.Type.Fragment, scalar, default, 2));
        Reject<ArgumentException>(() => graph.RemoveNode(VisualShader.Type.Fragment, 0));
        Reject<ArgumentOutOfRangeException>(() => graph.GetNode((VisualShader.Type)9, 0));
        var edges = graph.GetNodeConnections(VisualShader.Type.Fragment); edges[0] = default;
        Check(graph.IsNodeConnection(VisualShader.Type.Fragment, 2, 0, 3, 0), "Connection snapshots own their arrays.");
        operation.SetInputPortDefaultValue(1, .25f);
        var snapshot = operation.GetDefaultInputValues();
        operation.SetInputPortDefaultValue(1, .75f);
        Check(((VisualShaderPortDefault<float>)snapshot.Single(x => x.Port == 1)).Value == .25f, "Default snapshots are immutable.");
        Reject<ArgumentException>(() => operation.SetInputPortDefaultValue(1, float.NaN));
        Reject<ArgumentException>(() => operation.SetInputPortDefaultValue(1, 1));
        Reject<ArgumentException>(() => operation.SetDefaultInputValues(new VisualShaderPortDefault[] { new VisualShaderPortDefault<float>(0, 2), new VisualShaderPortDefault<float>(0, 3) }));
        Check(operation.GetInputPortDefaultValue<float>(1) == .75f, "Rejected replacements preserve defaults.");
        operation.SetInputPortDefaultValue(1, 0f, new Vector3(.125f, .5f, .75f));
        Check(operation.GetInputPortDefaultValue<float>(1) == .125f, "Previous vectors convert by first component.");
        operation.RemoveInputPortDefaultValue(1); Check(operation.GetInputPortDefaultValue<float>(1) is null, "Removed defaults return null.");
        using var vec4 = new VisualShaderNodeVec4Constant();
        Check(vec4.Constant == new Vector4(0, 0, 0, 1), "Four-component defaults retain their numeric homogeneous component.");
        using var vectorOperation = new VisualShaderNodeVectorOp();
        vectorOperation.SetInputPortDefaultValue(0, new Vector3(.125f, .25f, .5f));
        vectorOperation.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        Check(vectorOperation.GetInputPortDefaultValue<Vector4>(0) == new Vector4(.125f, .25f, .5f, .5f), "Changing vector width repeats the previous last component.");
        vectorOperation.ClearDefaultInputValues(); vectorOperation.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2;
        Check(vectorOperation.GetInputPortDefaultValue<Vector2>(0) == Vector2.Zero, "A width change recreates a missing default.");
        Reject<NotSupportedException>(() => vectorOperation.Operator = VisualShaderNodeVectorOp.OperatorEnum.Cross);
        using var shallow = (VisualShader)graph.Duplicate();
        using var deep = (VisualShader)graph.Duplicate(true);
        Check(ReferenceEquals(shallow.GetNode(VisualShader.Type.Fragment, 2), scalar), "Shallow graph borrows operation resources.");
        Check(!ReferenceEquals(deep.GetNode(VisualShader.Type.Fragment, 2), scalar), "Deep graph copies operation resources.");
        Check(((VisualShaderNodeFloatConstant)deep.GetNode(VisualShader.Type.Fragment, 2)).Constant == .375f, "Deep graph copies authored constant values.");
        using var copied = new VisualShader(); copied.CopyFromResource(graph);
        Check(!ReferenceEquals(copied.GetNode(VisualShader.Type.Vertex, 0), graph.GetNode(VisualShader.Type.Vertex, 0)), "CopyFromResource preserves independent reserved outputs.");
        Check(!ReferenceEquals(shallow.GetNode(VisualShader.Type.Vertex, 0), graph.GetNode(VisualShader.Type.Vertex, 0)), "Output definitions remain independently owned.");
        graph.RemoveNode(VisualShader.Type.Fragment, 2);
        Check(!scalar.IsDisposed && graph.GetNodeConnections(VisualShader.Type.Fragment).Length == 1, "Removal borrows the resource and removes incident edges.");
        using (var replacements = new VisualShader())
        using (var borrowed = new VisualShaderNodeFloatConstant())
        {
            replacements.AddNode(VisualShader.Type.Fragment, borrowed, new(3, 4), 2);
            replacements.ReplaceNode(VisualShader.Type.Fragment, 2, "VisualShaderNodeVec3Constant");
            var owned = (VisualShaderNodeVec3Constant)replacements.GetNode(VisualShader.Type.Fragment, 2);
            owned.Constant = new(.1f, .2f, .3f);
            replacements.AddNode(VisualShader.Type.Vertex, owned, default, 2);
            using var aliasCopy = (VisualShader)replacements.Duplicate();
            Check(!ReferenceEquals(aliasCopy.GetNode(VisualShader.Type.Fragment, 2), owned) && ReferenceEquals(aliasCopy.GetNode(VisualShader.Type.Fragment, 2), aliasCopy.GetNode(VisualShader.Type.Vertex, 2)), "Owned replacements duplicate independently while retaining graph aliases.");
            replacements.RemoveNode(VisualShader.Type.Fragment, 2); Check(!owned.IsDisposed, "A replacement lives while another graph entry refers to it.");
            replacements.RemoveNode(VisualShader.Type.Vertex, 2); Check(owned.IsDisposed && !borrowed.IsDisposed, "The last graph alias retires an owned replacement, preserving borrowed originals.");
            Reject<ArgumentException>(() => aliasCopy.ReplaceNode(VisualShader.Type.Fragment, 2, "Resource"));
            Check(aliasCopy.GetNodePosition(VisualShader.Type.Fragment, 2) == new Vector2(3, 4), "Replacement preserves authoring position.");
        }
        var folder = IOPath.Combine(IOPath.GetTempPath(), "electron2d-visual-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            Author(folder);
            var expressionFolder = IOPath.Combine(folder, "expressions"); AuthorExpressions(expressionFolder); ExpressionMetadata(expressionFolder, false);
            var sdfFolder = IOPath.Combine(folder, "sdf"); AuthorSDF(sdfFolder); SDFMetadata(sdfFolder, false);
            foreach (var source in Directory.GetFiles(folder, "fx-*.e2dres"))
            { using var effect = ResourceLoader.Load<VisualShader>(source, ResourceLoader.CacheMode.IgnoreDeep); VerifyEffectArchive(IOPath.GetFileNameWithoutExtension(source), effect); }
            using var loaded = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "color.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            Check(loaded.GetNodeList(VisualShader.Type.Fragment).Length == 3, "Fresh resource graph node table.");
            Check(((VisualShaderNodeColorConstant)loaded.GetNode(VisualShader.Type.Fragment, 2)).Constant == new Color(.25f, .5f, .75f, 1), "Typed constants restore.");
            Check(loaded.GetNodeConnections(VisualShader.Type.Fragment).Length == 2, "Typed connections restore.");
            using var cleared = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "cleared.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            Check(cleared.GetNode(VisualShader.Type.Fragment, 2).GetDefaultInputValues().Count == 0, "Removed constructor defaults stay removed after restore.");
            using var hidden = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "flow-hidden-compose.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            var compose = (VisualShaderNodeVectorCompose)hidden.GetNode(VisualShader.Type.Fragment, 2);
            Check(compose.OpType == VisualShaderNodeVectorBase.OpTypeEnum.Vector2 && compose.GetInputPortDefaultValue<float>(3) == .5f, "Fresh archives retain a reserved scalar default while its component is inactive.");
            compose.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
            Check(compose.GetInputPortDefaultValue<float>(2) == .75f && compose.GetInputPortDefaultValue<float>(3) == .5f, "Restored dormant components survive widening.");
            Reject<InvalidDataException>(() => ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "invalid/flow-missing-output.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        Console.WriteLine("Visual graph ownership, numeric defaults, invalid requests, snapshots, duplication and archives passed.");
    }
    internal static void Author(string folder)
    {
        Directory.CreateDirectory(folder);
        AuthorFunctions(folder);
        AuthorDataflow(folder);
        AuthorRanges(folder);
        AuthorIntegers(folder);
        AuthorConditions(folder);
        AuthorMatrices(folder);
        AuthorParameters(folder);
        AuthorDerivatives(folder);
        AuthorImages(folder);
        AuthorEffects(folder);
        using (var graph = new VisualShader())
        using (var color = new VisualShaderNodeColorConstant { Constant = new(.25f, .5f, .75f, 1) })
        using (var alpha = new VisualShaderNodeFloatConstant { Constant = 1 })
        {
            graph.AddNode(VisualShader.Type.Fragment, color, new(10, 20), 2);
            graph.AddNode(VisualShader.Type.Fragment, alpha, new(20, 40), 3);
            graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 1);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "color.e2dres"), SaverFlags.BundleResources);
        }
        using (var graph = new VisualShader())
        using (var operation = new VisualShaderNodeFloatOp())
        {
            operation.ClearDefaultInputValues(); graph.AddNode(VisualShader.Type.Fragment, operation, default, 2);
            graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "cleared.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        }
        using (var graph = new VisualShader())
        using (var uv = new VisualShaderNodeInput { InputName = "uv" })
        {
            graph.AddNode(VisualShader.Type.Fragment, uv, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "uv.e2dres"), SaverFlags.BundleResources);
        }
        using (var graph = new VisualShader())
        using (var operation = new VisualShaderNodeFloatOp())
        {
            graph.AddNode(VisualShader.Type.Fragment, operation, default, 2);
            graph.ConnectNodesForced(VisualShader.Type.Fragment, 2, 0, 2, 0);
            graph.ConnectNodesForced(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "cycle.e2dres"), SaverFlags.BundleResources);
        }
        using (var graph = new VisualShader())
        using (var uv = new VisualShaderNodeVec2Constant { Constant = new(.25f, .75f) })
        using (var input = new VisualShaderNodeInput { InputName = "uv" })
        {
            graph.AddNode(VisualShader.Type.Vertex, uv, default, 2); graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 0, 1);
            graph.AddNode(VisualShader.Type.Fragment, input, default, 2); graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "vertex-uv.e2dres"), SaverFlags.BundleResources);
        }
        AuthorNumeric(folder);
        Console.WriteLine("Authored typed visual shader fixtures.");
    }
    internal static void Read(string file)
    {
        using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep);
        if (IOPath.GetFileName(file) == "range-zero-input-range.e2dres")
        {
            var remap = (VisualShaderNodeRemap)graph.GetNode(VisualShader.Type.Fragment, 2);
            Check(remap.GetInputPortDefaultValue<float>(0) == 1 && remap.GetInputPortDefaultValue<float>(1) == 0 && remap.GetInputPortDefaultValue<float>(2) == 0,
                "A zero-width native arithmetic boundary imports and reloads without fabricating a finite value.");
        }
        ReadRanges(graph);
        if (IOPath.GetFileNameWithoutExtension(file).StartsWith("integer-", StringComparison.Ordinal)) VerifyIntegerArchive(IOPath.GetFileNameWithoutExtension(file), graph);
        if (IOPath.GetFileNameWithoutExtension(file).StartsWith("condition-", StringComparison.Ordinal)) VerifyConditionArchive(IOPath.GetFileNameWithoutExtension(file), graph);
        if (IOPath.GetFileNameWithoutExtension(file).StartsWith("matrix-", StringComparison.Ordinal)) VerifyMatrixArchive(IOPath.GetFileNameWithoutExtension(file), graph);
        if (IOPath.GetFileNameWithoutExtension(file).StartsWith("parameter-", StringComparison.Ordinal)) VerifyParameterArchive(IOPath.GetFileNameWithoutExtension(file), graph);
        if (IOPath.GetFileNameWithoutExtension(file).StartsWith("derivative-", StringComparison.Ordinal)) VerifyDerivativeArchive(IOPath.GetFileNameWithoutExtension(file), graph);
        Check(graph.GetMode() == Shader.Mode.CanvasItem, "Imported graph domain.");
        Check(graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length > 20 && graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment).Length > 20, "Both canvas stages are compiled.");
        Console.WriteLine("Imported visual shader loaded with both native program stages.");
    }
}
