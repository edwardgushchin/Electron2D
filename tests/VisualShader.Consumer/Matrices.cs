using Electron2D;
using IOPath = System.IO.Path;
using M4 = System.Numerics.Matrix4x4;

internal static partial class GraphChecks
{
    private static readonly M4 MatrixA = new(1, .25f, .125f, .5f, .5f, 1.25f, .25f, .75f, .125f, .375f, 1.5f, 1, .25f, .125f, .5f, 1);
    private static readonly M4 MatrixB = new(.5f, .125f, .25f, .375f, .25f, .75f, .375f, .5f, .375f, .25f, 1, .125f, .125f, .5f, .25f, 1.25f);
    private static readonly Vector3 MatrixProbe = new(.25f, .5f, .75f);
    private static readonly string[] MatrixRoots = ["constant", "op", "func", "compose", "outer", "switch"];
    private static int MatrixModes(string kind) => kind == "op" ? 9 : kind is "func" or "switch" ? 2 : 1;
    private static VisualShaderNode MatrixRoot(string kind, int selection, int authored = 1)
    {
        VisualShaderNode node = kind switch
        {
            "constant" => new VisualShaderNodeMatrixConstant(),
            "op" => new VisualShaderNodeMatrixOp { Operator = (VisualShaderNodeMatrixOp.OperatorEnum)selection },
            "func" => new VisualShaderNodeMatrixFunc { Function = (VisualShaderNodeMatrixFunc.FunctionEnum)selection },
            "compose" => new VisualShaderNodeMatrixCompose(),
            "outer" => new VisualShaderNodeOuterProduct(),
            _ => new VisualShaderNodeSwitch { OpType = VisualShaderValueType.Matrix }
        };
        if (authored == 1)
        {
            switch (node)
            {
                case VisualShaderNodeMatrixConstant n: n.Constant = MatrixA; break;
                case VisualShaderNodeMatrixCompose:
                    for (var column = 0; column < 4; column++) node.SetInputPortDefaultValue(column, new Vector3(MatrixA[0, column], MatrixA[1, column], MatrixA[2, column])); break;
                case VisualShaderNodeOuterProduct:
                    node.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f)); node.SetInputPortDefaultValue(1, new Vector3(.5f, .75f, 1)); break;
                case VisualShaderNodeSwitch:
                    node.SetInputPortDefaultValue(0, selection != 0); node.SetInputPortDefaultValue(1, MatrixA); node.SetInputPortDefaultValue(2, MatrixB); break;
                default:
                    node.SetInputPortDefaultValue(0, MatrixA);
                    if (kind == "op") node.SetInputPortDefaultValue(1, MatrixB); break;
            }
        }
        if (authored == 2) node.ClearDefaultInputValues();
        return node;
    }
    private static void MatricesCPU()
    {
        foreach (var kind in MatrixRoots)
            for (var selection = 0; selection < MatrixModes(kind); selection++)
            {
                using var node = MatrixRoot(kind, selection); using var copy = (VisualShaderNode)node.Duplicate(true); using var assigned = MatrixRoot(kind, 0); assigned.CopyFromResource(node);
                if (kind == "constant") Check(((VisualShaderNodeMatrixConstant)copy).Constant == MatrixA && ((VisualShaderNodeMatrixConstant)assigned).Constant == MatrixA, "Matrix constants copy all sixteen components.");
                else Check(node.GetDefaultInputValues().Select(x => x.ValueType).SequenceEqual(copy.GetDefaultInputValues().Select(x => x.ValueType)), "Matrix definitions duplicate their exact typed defaults.");
                node.ClearDefaultInputValues(); using var emptyCopy = (VisualShaderNode)node.Duplicate(); Check(emptyCopy.GetDefaultInputValues().Count == 0, "Matrix copies retain absent defaults.");
            }
        using var constant = new VisualShaderNodeMatrixConstant { Constant = MatrixA }; var changes = 0; constant.Changed += _ => changes++;
        var near = MatrixA; near.M11 = MathF.BitIncrement(near.M11); constant.Constant = near;
        Check(changes == 0 && constant.Constant == MatrixA, "Approximate matrix constant edits preserve the original value.");
        var invalid = MatrixA; invalid.M32 = float.NaN; Reject<ArgumentException>(() => constant.Constant = invalid);
        using var operation = new VisualShaderNodeMatrixOp(); var defaults = operation.GetDefaultInputValues();
        Reject<ArgumentException>(() => operation.SetInputPortDefaultValue(0, invalid));
        Check(defaults.SequenceEqual(operation.GetDefaultInputValues()), "Invalid matrix defaults preserve all inputs.");
        var snapshot = new VisualShaderPortDefault<M4>(0, MatrixA); var local = snapshot.Value; local.M44 = 123;
        Check(snapshot.Value == MatrixA && snapshot.ValueType == typeof(M4), "Matrix default snapshots have copied value semantics.");
        using var compare = new VisualShaderNodeCompare(); using var select = new VisualShaderNodeSwitch();
        for (var from = 0; from < 8; from++) for (var to = 0; to < 8; to++)
            {
                compare.Type = (VisualShaderValueType)from; select.OpType = (VisualShaderValueType)from;
                compare.SetInputPortDefaultValue(2, .125f); select.SetInputPortDefaultValue(0, true);
                if (from == 7) { compare.SetInputPortDefaultValue(0, MatrixA); select.SetInputPortDefaultValue(1, MatrixB); }
                compare.Type = (VisualShaderValueType)to; select.OpType = (VisualShaderValueType)to;
                Check(compare.GetInputPortDefaultValue<float>(2) == .125f && select.GetInputPortDefaultValue<bool>(0) == true, "Matrix transitions preserve unrelated tolerance and condition.");
                if (to == 7) Check(compare.GetInputPortDefaultValue<M4>(0) == (from == to ? MatrixA : M4.Identity) && select.GetInputPortDefaultValue<M4>(1) == (from == to ? MatrixB : M4.Identity), "Changing into matrix layout uses identity instead of scalar/vector conversion.");
            }
        using var func = new VisualShaderNodeMatrixFunc(); using var product = new VisualShaderNodeMatrixVecMult();
        foreach (var node in new VisualShaderNode[] { operation, func, product })
        {
            var count = 0; node.Changed += _ => count++;
            if (node is VisualShaderNodeMatrixOp op) { op.Operator = VisualShaderNodeMatrixOp.OperatorEnum.Add; op.Operator = VisualShaderNodeMatrixOp.OperatorEnum.Add; Reject<ArgumentOutOfRangeException>(() => op.Operator = VisualShaderNodeMatrixOp.OperatorEnum.Max); }
            else if (node is VisualShaderNodeMatrixFunc fn) { fn.Function = VisualShaderNodeMatrixFunc.FunctionEnum.Transpose; fn.Function = VisualShaderNodeMatrixFunc.FunctionEnum.Transpose; Reject<ArgumentOutOfRangeException>(() => fn.Function = VisualShaderNodeMatrixFunc.FunctionEnum.Max); }
            else { product.Operator = VisualShaderNodeMatrixVecMult.OperatorEnum.VectorTimesMatrix; product.Operator = VisualShaderNodeMatrixVecMult.OperatorEnum.VectorTimesMatrix; Reject<ArgumentOutOfRangeException>(() => product.Operator = VisualShaderNodeMatrixVecMult.OperatorEnum.Max); }
            Check(count == 1, "Matrix operation selection commits once and repeated selection is quiet.");
        }
        operation.Changed += _ => throw new InvalidOperationException("Matrix observer");
        Reject<InvalidOperationException>(() => operation.Operator = VisualShaderNodeMatrixOp.OperatorEnum.Subtract);
        Check(operation.Operator == VisualShaderNodeMatrixOp.OperatorEnum.Subtract, "Matrix selection commits before observer failure.");
        using var graph = new VisualShader(); using var scalar = new VisualShaderNodeFloatConstant();
        graph.AddNode(VisualShader.Type.Fragment, constant, default, 2); graph.AddNode(VisualShader.Type.Fragment, scalar, default, 3); graph.AddNode(VisualShader.Type.Fragment, product, default, 4);
        Check(graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 0, 4, 0) && !graph.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 4, 0) && !graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0), "Matrix ports connect only to matrix ports.");
        graph.ConnectNodesForced(VisualShader.Type.Fragment, 3, 0, 4, 0);
        Check(graph.IsNodeConnection(VisualShader.Type.Fragment, 3, 0, 4, 0), "Forced authoring retains incompatible edges for explicit import rejection.");
        constant.Dispose(); operation.Dispose(); func.Dispose(); product.Dispose();
        Reject<ObjectDisposedException>(() => constant.Constant = M4.Identity); Reject<ObjectDisposedException>(() => operation.Operator = 0);
        Reject<ObjectDisposedException>(() => func.Function = 0); Reject<ObjectDisposedException>(() => product.Operator = 0);
    }
    private static double[,] MatrixData(M4 m)
    { var result = new double[4, 4]; for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) result[row, column] = m[row, column]; return result; }
    private static double[,] MatrixResult(string kind, int selection, int authored)
    {
        var a = MatrixData(authored == 1 ? MatrixA : M4.Identity); var b = MatrixData(authored == 1 ? MatrixB : M4.Identity); var result = new double[4, 4];
        if (kind == "constant") return a;
        if (kind == "switch") return authored == 1 && selection == 0 ? b : a;
        if (kind == "compose") { if (authored == 1) for (var r = 0; r < 3; r++) for (var c = 0; c < 4; c++) result[r, c] = a[r, c]; result[3, 3] = 1; return result; }
        if (kind == "outer") { if (authored == 1) for (var r = 0; r < 3; r++) for (var c = 0; c < 3; c++) result[r, c] = (r + 1) * .25 * (c + 2) * .25; return result; }
        if (kind == "func")
        {
            if (selection == 1) { for (var r = 0; r < 4; r++) for (var c = 0; c < 4; c++) result[r, c] = a[c, r]; return result; }
            var augmented = new double[4, 8];
            for (var r = 0; r < 4; r++) { for (var c = 0; c < 4; c++) augmented[r, c] = a[r, c]; augmented[r, r + 4] = 1; }
            for (var pivot = 0; pivot < 4; pivot++)
            {
                var divisor = augmented[pivot, pivot]; Check(Math.Abs(divisor) > 1e-8, "Independent inverse oracle is nonsingular.");
                for (var c = 0; c < 8; c++) augmented[pivot, c] /= divisor;
                for (var r = 0; r < 4; r++) if (r != pivot) { var factor = augmented[r, pivot]; for (var c = 0; c < 8; c++) augmented[r, c] -= factor * augmented[pivot, c]; }
            }
            for (var r = 0; r < 4; r++) for (var c = 0; c < 4; c++) result[r, c] = augmented[r, c + 4]; return result;
        }
        for (var r = 0; r < 4; r++) for (var c = 0; c < 4; c++)
            {
                if (selection < 2) for (var k = 0; k < 4; k++) result[r, c] += selection == 0 ? a[r, k] * b[k, c] : b[r, k] * a[k, c];
                else result[r, c] = selection switch { 2 or 3 => a[r, c] * b[r, c], 4 => a[r, c] + b[r, c], 5 => a[r, c] - b[r, c], 6 => b[r, c] - a[r, c], 7 => a[r, c] / b[r, c], _ => b[r, c] / a[r, c] };
            }
        return result;
    }
    private static double MatrixDeterminant(double[,] data)
    {
        double result = 0;
        foreach (var a in Enumerable.Range(0, 4)) foreach (var b in Enumerable.Range(0, 4)) foreach (var c in Enumerable.Range(0, 4)) foreach (var d in Enumerable.Range(0, 4))
                    {
                        int[] p = [a, b, c, d]; if (p.Distinct().Count() != 4) continue; var inversions = 0;
                        for (var x = 0; x < 4; x++) for (var y = x + 1; y < 4; y++) if (p[x] > p[y]) inversions++;
                        result += (inversions % 2 == 0 ? 1 : -1) * data[0, a] * data[1, b] * data[2, c] * data[3, d];
                    }
        return result;
    }
    private static Color MatrixObserved(double[,] data, int observation)
    {
        var v = new double[3];
        if (observation < 4) for (var r = 0; r < 3; r++) v[r] = data[r, observation];
        else if (observation == 4) Array.Fill(v, MatrixDeterminant(data));
        else
        {
            var op = observation - 5; double[] input = [.25, .5, .75, op < 2 ? 1 : 0];
            for (var r = 0; r < 3; r++) for (var c = 0; c < 4; c++) v[r] += (op % 2 == 0 ? data[r, c] : data[c, r]) * input[c];
        }
        return new((float)(v[0] * .0625 + .25), (float)(v[1] * .0625 + .25), (float)(v[2] * .0625 + .25), 1);
    }
    private static void ObserveMatrix(VisualShader graph, VisualShader.Type stage, int root, int observation, List<VisualShaderNode> owned)
    {
        VisualShaderNode observer = observation < 4 ? new VisualShaderNodeMatrixDecompose() : observation == 4 ? new VisualShaderNodeDeterminant() : new VisualShaderNodeMatrixVecMult { Operator = (VisualShaderNodeMatrixVecMult.OperatorEnum)(observation - 5) };
        if (observation > 4) observer.SetInputPortDefaultValue(1, MatrixProbe);
        var scale = new VisualShaderNodeMultiplyAdd { OpType = VisualShaderNodeMultiplyAdd.OpTypeEnum.Vector3 };
        scale.SetInputPortDefaultValue(1, new Vector3(.0625f, .0625f, .0625f)); scale.SetInputPortDefaultValue(2, new Vector3(.25f, .25f, .25f));
        owned.AddRange([observer, scale]); graph.AddNode(stage, observer, default, 3); graph.AddNode(stage, scale, default, 4);
        graph.ConnectNodes(stage, root, 0, 3, 0); graph.ConnectNodes(stage, 3, observation < 4 ? observation : 0, 4, 0); graph.ConnectNodes(stage, 4, 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
    }
    private static M4 MatrixComparisonValue(int variant)
    { var b = variant == 0 ? MatrixA : variant == 1 ? MatrixB : MatrixA; if (variant >= 2) b[(variant - 2) / 4, (variant - 2) % 4] += .25f; return b; }
    private static void AuthorMatrices(string folder)
    {
        foreach (var kind in MatrixRoots) for (var selection = 0; selection < MatrixModes(kind); selection++)
                for (var observation = 0; observation < 9; observation++) foreach (var vertex in new[] { false, true }) AuthorMatrixRoot(folder, kind, selection, observation, 1, vertex);
        foreach (var kind in MatrixRoots) foreach (var authored in new[] { 0, 2 }) for (var observation = 0; observation < 5; observation++) foreach (var vertex in new[] { false, true }) AuthorMatrixRoot(folder, kind, 0, observation, authored, vertex);
        for (var function = 0; function < 6; function++) for (var condition = 0; condition < 2; condition++)
                for (var variant = 0; variant < (function < 2 ? 18 : 2); variant++) foreach (var vertex in new[] { false, true })
                    {
                        using var graph = new VisualShader(); using var compare = new VisualShaderNodeCompare { Type = VisualShaderValueType.Matrix, Function = (VisualShaderNodeCompare.FunctionEnum)function, Condition = (VisualShaderNodeCompare.ConditionEnum)condition };
                        compare.SetInputPortDefaultValue(0, MatrixA); compare.SetInputPortDefaultValue(1, MatrixComparisonValue(variant)); compare.SetInputPortDefaultValue(2, .125f);
                        var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment; graph.AddNode(stage, compare, default, 2); graph.ConnectNodes(stage, 2, 0, 0, vertex ? 2 : 0);
                        SaveMatrix(graph, folder, $"c-{function}-{condition}-{variant}" + (vertex ? "-vertex" : ""));
                    }
        AuthorMatrixProcedure(folder);
        var boundaries = IOPath.Combine(folder, "matrix-boundaries"); Directory.CreateDirectory(boundaries);
        using var inverseGraph = new VisualShader(); using var inverse = new VisualShaderNodeMatrixFunc(); inverse.SetInputPortDefaultValue(0, default(M4)); inverseGraph.AddNode(VisualShader.Type.Fragment, inverse, default, 2); var inverseOwned = new List<VisualShaderNode>();
        try { ObserveMatrix(inverseGraph, VisualShader.Type.Fragment, 2, 4, inverseOwned); SaveMatrix(inverseGraph, boundaries, "singular-inverse"); } finally { foreach (var node in inverseOwned) node.Dispose(); }
        using var divideGraph = new VisualShader(); using var divide = new VisualShaderNodeMatrixOp { Operator = VisualShaderNodeMatrixOp.OperatorEnum.Divide }; divideGraph.AddNode(VisualShader.Type.Fragment, divide, default, 2); var divideOwned = new List<VisualShaderNode>();
        try { ObserveMatrix(divideGraph, VisualShader.Type.Fragment, 2, 0, divideOwned); SaveMatrix(divideGraph, boundaries, "zero-component-divide"); } finally { foreach (var node in divideOwned) node.Dispose(); }
        var invalidFolder = IOPath.Combine(folder, "invalid-matrices"); Directory.CreateDirectory(invalidFolder);
        using var invalidGraph = new VisualShader(); using var matrix = new VisualShaderNodeMatrixConstant(); invalidGraph.AddNode(VisualShader.Type.Fragment, matrix, default, 2); invalidGraph.ConnectNodesForced(VisualShader.Type.Fragment, 2, 0, 0, 0); SaveMatrix(invalidGraph, invalidFolder, "incompatible");
    }
    private static void AuthorMatrixRoot(string folder, string kind, int selection, int observation, int authored, bool vertex)
    {
        using var graph = new VisualShader(); using var node = MatrixRoot(kind, selection, authored); var owned = new List<VisualShaderNode>(); var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        try { graph.AddNode(stage, node, default, 2); ObserveMatrix(graph, stage, 2, observation, owned); SaveMatrix(graph, folder, $"r-{kind}-{selection}-{observation}-{authored}" + (vertex ? "-vertex" : "")); }
        finally { foreach (var item in owned) item.Dispose(); }
    }
    private static void AuthorMatrixProcedure(string folder)
    {
        using var graph = new VisualShader(); using var uv = new VisualShaderNodeInput { InputName = "uv" }; using var decompose = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
        using var compare = new VisualShaderNodeCompare { Function = VisualShaderNodeCompare.FunctionEnum.GreaterThan }; compare.SetInputPortDefaultValue(1, .5f);
        using var select = new VisualShaderNodeSwitch { OpType = VisualShaderValueType.Matrix }; select.SetInputPortDefaultValue(1, MatrixA); select.SetInputPortDefaultValue(2, MatrixB);
        graph.AddNode(VisualShader.Type.Fragment, uv, default, 20); graph.AddNode(VisualShader.Type.Fragment, decompose, default, 21); graph.AddNode(VisualShader.Type.Fragment, compare, default, 22); graph.AddNode(VisualShader.Type.Fragment, select, default, 2);
        graph.ConnectNodes(VisualShader.Type.Fragment, 20, 0, 21, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 21, 0, 22, 0); graph.ConnectNodes(VisualShader.Type.Fragment, 22, 0, 2, 0);
        var owned = new List<VisualShaderNode>(); try { ObserveMatrix(graph, VisualShader.Type.Fragment, 2, 5, owned); SaveMatrix(graph, folder, "procedural"); } finally { foreach (var item in owned) item.Dispose(); }
    }
    private static void SaveMatrix(VisualShader graph, string folder, string name) => ResourceSaver.Save(graph, IOPath.Combine(folder, "matrix-" + name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    private static Color MatrixExpected(string name)
    {
        var parts = name.Split('-');
        if (parts[1] == "procedural") return MatrixObserved(MatrixData(MatrixA), 5);
        if (parts[1] == "c") { var function = int.Parse(parts[2]); var equal = int.Parse(parts[4]) == 0; return (function == 0 && equal || function == 1 && !equal) ? Colors.White : Colors.Black; }
        return MatrixObserved(MatrixResult(parts[2], int.Parse(parts[3]), int.Parse(parts[5])), int.Parse(parts[4]));
    }
    private static void VerifyMatrixArchive(string name, VisualShader graph)
    {
        var parts = name.Split('-'); var stage = parts.Contains("vertex") ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        if (parts[1] == "c")
        {
            var node = (VisualShaderNodeCompare)graph.GetNode(stage, 2);
            Check(node.Type == VisualShaderValueType.Matrix && (int)node.Function == int.Parse(parts[2]) && (int)node.Condition == int.Parse(parts[3]) && node.GetInputPortDefaultValue<M4>(0) == MatrixA && node.GetInputPortDefaultValue<M4>(1) == MatrixComparisonValue(int.Parse(parts[4])) && node.GetInputPortDefaultValue<float>(2) == .125f, "Fresh matrix comparisons retain selectors, every matrix component and dormant tolerance.");
        }
        else if (parts[1] == "r")
        {
            var kind = parts[2]; var authored = int.Parse(parts[5]); var node = graph.GetNode(stage, 2);
            if (authored == 2) Check(node.GetDefaultInputValues().Count == 0, "Fresh matrix archives preserve absent input defaults.");
            if (kind == "constant") Check(((VisualShaderNodeMatrixConstant)node).Constant == (authored == 1 ? MatrixA : M4.Identity), "Fresh matrix constants retain all sixteen values.");
            if (kind == "op") Check((int)((VisualShaderNodeMatrixOp)node).Operator == int.Parse(parts[3]), "Fresh matrix operators retain their selection.");
            if (kind == "func") Check((int)((VisualShaderNodeMatrixFunc)node).Function == int.Parse(parts[3]), "Fresh matrix functions retain their selection.");
            if (kind is "op" or "func" && authored != 2) Check(node.GetInputPortDefaultValue<M4>(0) == (authored == 1 ? MatrixA : M4.Identity), "Fresh matrix defaults retain their exact row/column words.");
        }
    }
}
