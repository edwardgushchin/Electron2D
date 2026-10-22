using System.Globalization;
using Matrix4x4 = System.Numerics.Matrix4x4;
using P = Electron2D.VisualShaderNode.PortType;

namespace Electron2D;

internal static class VisualShaderValues
{
    internal static P Kind(object value) => value switch
    {
        float => P.Scalar,
        int => P.ScalarInt,
        uint => P.ScalarUInt,
        bool => P.Boolean,
        Vector2 => P.Vector2,
        Vector3 => P.Vector3,
        Vector4 or Color => P.Vector4,
        Matrix4x4 => P.Matrix,
        _ => throw new ArgumentException("Unsupported graph value type.")
    };
    internal static void Validate(object value)
    {
        _ = Kind(value);
        var finite = value switch
        {
            float f => float.IsFinite(f),
            Vector2 v => float.IsFinite(v.X) && float.IsFinite(v.Y),
            Vector3 v => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z),
            Vector4 v => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) && float.IsFinite(v.W),
            Color c => float.IsFinite(c.R) && float.IsFinite(c.G) && float.IsFinite(c.B) && float.IsFinite(c.A),
            Matrix4x4 m => MatrixComponents(m).All(float.IsFinite),
            _ => true
        };
        if (!finite) throw new ArgumentException("Graph values must be finite.");
    }
    internal static void Require(P kind, object value)
    { Validate(value); if (kind != Kind(value)) throw new ArgumentException("The value type does not match the graph port."); }
    internal static bool SameConstant(object left, object right) => (left, right) switch
    {
        (float a, float b) => Mathf.IsEqualApprox(a, b),
        (Vector2 a, Vector2 b) => Mathf.IsEqualApprox(a.X, b.X) && Mathf.IsEqualApprox(a.Y, b.Y),
        (Vector3 a, Vector3 b) => Mathf.IsEqualApprox(a.X, b.X) && Mathf.IsEqualApprox(a.Y, b.Y) && Mathf.IsEqualApprox(a.Z, b.Z),
        (Vector4 a, Vector4 b) => Mathf.IsEqualApprox(a.X, b.X) && Mathf.IsEqualApprox(a.Y, b.Y) && Mathf.IsEqualApprox(a.Z, b.Z) && Mathf.IsEqualApprox(a.W, b.W),
        (Color a, Color b) => Mathf.IsEqualApprox(a.R, b.R) && Mathf.IsEqualApprox(a.G, b.G) && Mathf.IsEqualApprox(a.B, b.B) && Mathf.IsEqualApprox(a.A, b.A),
        (Matrix4x4 a, Matrix4x4 b) => MatrixComponents(a).Zip(MatrixComponents(b), (x, y) => Mathf.IsEqualApprox(x, y)).All(v => v),
        _ => left.Equals(right)
    };
    internal static VisualShaderPortDefault Entry(int port, object value) => value switch
    {
        float v => new VisualShaderPortDefault<float>(port, v),
        int v => new VisualShaderPortDefault<int>(port, v),
        uint v => new VisualShaderPortDefault<uint>(port, v),
        bool v => new VisualShaderPortDefault<bool>(port, v),
        Vector2 v => new VisualShaderPortDefault<Vector2>(port, v),
        Vector3 v => new VisualShaderPortDefault<Vector3>(port, v),
        Vector4 v => new VisualShaderPortDefault<Vector4>(port, v),
        Color v => new VisualShaderPortDefault<Vector4>(port, new(v.R, v.G, v.B, v.A)),
        Matrix4x4 v => new VisualShaderPortDefault<Matrix4x4>(port, v),
        _ => throw new ArgumentException("Unsupported graph value type.")
    };
    internal static object Zero(P kind) => kind switch
    {
        P.Scalar => 0f,
        P.ScalarInt => 0,
        P.ScalarUInt => 0u,
        P.Boolean => false,
        P.Vector2 => Vector2.Zero,
        P.Vector3 => Vector3.Zero,
        P.Vector4 => Vector4.Zero,
        P.Matrix => Matrix4x4.Identity,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    internal static T Cast<T>(object value) where T : unmanaged
    {
        if (value is T typed) return typed;
        if (value is Color c && typeof(T) == typeof(Vector4)) return (T)(object)new Vector4(c.R, c.G, c.B, c.A);
        if (value is Vector4 v && typeof(T) == typeof(Color)) return (T)(object)new Color(v.X, v.Y, v.Z, v.W);
        throw new ArgumentException("The value type does not match the graph port.");
    }
    internal static T ConvertDefault<T, TPrevious>(T fallback, TPrevious previous) where T : unmanaged where TPrevious : unmanaged
        => (T)ConvertDefaultValue(fallback, previous);
    internal static object ConvertDefaultValue(object fallback, object previous)
    {
        if (previous is bool || fallback is bool) return fallback;
        if (previous.GetType() == fallback.GetType()) return previous;
        if (previous is Matrix4x4 || fallback is Matrix4x4) return fallback;
        if (fallback is int && previous is uint unsigned) return checked((int)unsigned);
        if (fallback is uint && previous is int signed) return checked((uint)signed);
        var v = previous switch
        {
            float f => new Vector4(f, f, f, f),
            int i => new Vector4(i, i, i, i),
            uint u => new Vector4(u, u, u, u),
            Vector2 x => new Vector4(x.X, x.Y, x.Y, x.Y),
            Vector3 x => new Vector4(x.X, x.Y, x.Z, x.Z),
            Vector4 x => x,
            Color c => new Vector4(c.R, c.G, c.B, c.A),
            _ => throw new ArgumentException("Unsupported graph value type.")
        };
        object result = fallback switch
        {
            float => v.X,
            int => checked((int)v.X),
            uint => checked((uint)v.X),
            Vector2 => new Vector2(v.X, v.Y),
            Vector3 => new Vector3(v.X, v.Y, v.Z),
            Vector4 => v,
            Color => new Color(v.X, v.Y, v.Z, v.W),
            _ => fallback
        };
        return result;
    }
    internal static P RangeKind(VisualShaderNumericOpType type) => type switch
    {
        VisualShaderNumericOpType.Scalar => P.Scalar,
        VisualShaderNumericOpType.Vector2 or VisualShaderNumericOpType.Vector2Scalar => P.Vector2,
        VisualShaderNumericOpType.Vector3 or VisualShaderNumericOpType.Vector3Scalar => P.Vector3,
        _ => P.Vector4
    };
    internal static bool HasScalarControls(VisualShaderNumericOpType type) => type is
        VisualShaderNumericOpType.Vector2Scalar or VisualShaderNumericOpType.Vector3Scalar or VisualShaderNumericOpType.Vector4Scalar;
    internal static string Name(P kind) => kind switch
    { P.Scalar => "float", P.ScalarInt => "int", P.ScalarUInt => "uint", P.Boolean => "bool", P.Vector2 => "vec2", P.Vector3 => "vec3", P.Vector4 => "vec4", P.Matrix => "mat4", _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
    private static float[] MatrixComponents(Matrix4x4 matrix) => Enumerable.Range(0, 16).Select(i => matrix[i / 4, i % 4]).ToArray();
    private static string MatrixLiteral(Matrix4x4 matrix) => "mat4(" + string.Join(", ", Enumerable.Range(0, 16).Select(i => Float(matrix[i % 4, i / 4]))) + ")";
    private static string Float(float value)
    { var text = value.ToString("R", CultureInfo.InvariantCulture); return text.Contains('.') || text.Contains('E') ? text : text + ".0"; }
    internal static string Literal(object value) => value switch
    {
        float f => Float(f),
        int i when i == int.MinValue => "(-2147483647 - 1)",
        int i => i.ToString(CultureInfo.InvariantCulture),
        uint u => u.ToString(CultureInfo.InvariantCulture) + "u",
        bool b => b ? "true" : "false",
        Vector2 v => $"vec2({Float(v.X)}, {Float(v.Y)})",
        Vector3 v => $"vec3({Float(v.X)}, {Float(v.Y)}, {Float(v.Z)})",
        Vector4 v => $"vec4({Float(v.X)}, {Float(v.Y)}, {Float(v.Z)}, {Float(v.W)})",
        Color c => $"vec4({Float(c.R)}, {Float(c.G)}, {Float(c.B)}, {Float(c.A)})",
        Matrix4x4 m => MatrixLiteral(m),
        _ => throw new ArgumentException("Unsupported graph value type.")
    };
    internal static string ConvertExpression(string expression, P from, P to)
    {
        if (from == to) return expression;
        if (from is P.Matrix or P.Sampler || to is P.Matrix or P.Sampler) throw new InvalidOperationException("Matrix graph ports require a matrix connection.");
        var vector = from is P.Vector2 or P.Vector3 or P.Vector4;
        var source = "(" + expression + ")";
        if (to is P.Scalar or P.ScalarInt or P.ScalarUInt)
        {
            if (vector) source += ".x";
            if (from == P.Boolean) return "(" + source + " ? " + (to == P.Scalar ? "1.0 : 0.0" : to == P.ScalarUInt ? "1u : 0u" : "1 : 0") + ")";
            return Name(to) + "(" + source + ")";
        }
        if (to == P.Boolean) return vector ? "all(bvec" + (from == P.Vector2 ? 2 : from == P.Vector3 ? 3 : 4) + "(" + source + "))" : "(" + source + " > " + (from == P.Scalar ? "0.0" : from == P.ScalarUInt ? "0u" : "0") + ")";
        if (from == P.Boolean) return Name(to) + "(" + source + " ? 1.0 : 0.0)";
        if (!vector) return Name(to) + "(" + source + ")";
        return (from, to) switch
        {
            (P.Vector2, P.Vector3) => "vec3(" + source + ", 0.0)",
            (P.Vector2, P.Vector4) => "vec4(" + source + ", 0.0, 0.0)",
            (P.Vector3, P.Vector4) => "vec4(" + source + ", 0.0)",
            (_, P.Vector2) => source + ".xy",
            (_, P.Vector3) => source + ".xyz",
            _ => throw new InvalidOperationException("Invalid numeric conversion.")
        };
    }
}
