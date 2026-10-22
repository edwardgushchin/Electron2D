namespace Electron2D;

/// <summary>Transforms three numeric color components in an imported canvas shader graph.</summary>
/// <remarks>RGB and HSV use Vector3 ports. Conversions do not clamp signed or HDR output.
/// Grayscale replicates the largest input component. Edits require reimport.</remarks>
public sealed class VisualShaderNodeColorFunc : VisualShaderNode
{
    /// <summary>The numeric color transform.</summary>
    public enum FunctionEnum
    {
        /// <summary>Replicates the maximum RGB component.</summary>
        Grayscale = 0,
        /// <summary>Converts hue, saturation and value to RGB.</summary>
        HSVToRGB = 1,
        /// <summary>Converts RGB to hue, saturation and value with a 1e-10 denominator stabilizer.</summary>
        RGBToHSV = 2,
        /// <summary>Applies the three sepia coefficient rows without clamping.</summary>
        Sepia = 3,
        /// <summary>Converts linear RGB to sRGB using the piecewise transfer function.</summary>
        LinearToSRGB = 4,
        /// <summary>Converts sRGB to linear RGB using the piecewise transfer function.</summary>
        SRGBToLinear = 5,
        /// <summary>The nonselectable function count.</summary>
        Max = 6,
    }
    private FunctionEnum _function;
    /// <summary>Creates a grayscale transform with a zero Vector3 default.</summary>
    public VisualShaderNodeColorFunc() => InitializeDefault(0, Vector3.Zero);
    /// <summary>Gets or selects the color transform.</summary>
    /// <value>Grayscale initially. Existing input values and default absence survive selection changes.</value>
    /// <remarks>An unchanged selection is quiet.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined or Max.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public FunctionEnum Function
    {
        get { ThrowIfDisposed(); return _function; }
        set { ThrowIfDisposed(); if (!Enum.IsDefined(value) || value == FunctionEnum.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_function == value) return; _function = value; EmitChanged(); }
    }
    internal override PortType[] Inputs => [PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3];
    internal override string Expression(int output, IReadOnlyList<string> inputs)
    {
        var c = "(" + inputs[0] + ")";
        return _function switch
        {
            FunctionEnum.Grayscale => "vec3(max(max(" + c + ".r, " + c + ".g), " + c + ".b))",
            FunctionEnum.HSVToRGB => c + ".z * mix(vec3(1.0), clamp(abs(fract(" + c + ".xxx + vec3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - vec3(3.0)) - vec3(1.0), 0.0, 1.0), " + c + ".y)",
            FunctionEnum.RGBToHSV => "e2d_rgb_to_hsv(" + c + ")",
            FunctionEnum.Sepia => "vec3(dot(" + c + ", vec3(.393, .769, .189)), dot(" + c + ", vec3(.349, .686, .168)), dot(" + c + ", vec3(.272, .534, .131)))",
            FunctionEnum.LinearToSRGB => "mix(vec3(1.055) * pow(" + c + ", vec3(1.0 / 2.4)) - vec3(.055), " + c + " * 12.92, lessThan(" + c + ", vec3(.0031308)))",
            _ => "mix(pow((" + c + " + vec3(.055)) * (1.0 / 1.055), vec3(2.4)), " + c + " * (1.0 / 12.92), lessThan(" + c + ", vec3(.04045)))"
        };
    }
    internal static string RGBToHSVDefinition(string name) => "vec3 " + name + "(vec3 rgb) {\n" +
        "    vec4 sorted = mix(vec4(rgb.bg, -1.0, 2.0 / 3.0), vec4(rgb.gb, 0.0, -1.0 / 3.0), step(rgb.b, rgb.g));\n" +
        "    vec4 dominant = mix(vec4(sorted.xyw, rgb.r), vec4(rgb.r, sorted.yzx), step(sorted.x, rgb.r));\n" +
        "    float chroma = dominant.x - min(dominant.w, dominant.y);\n" +
        "    return vec3(abs(dominant.z + (dominant.w - dominant.y) / (6.0 * chroma + 1.0e-10)), chroma / (dominant.x + 1.0e-10), dominant.x);\n}\n";
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeColorFunc)target)._function = _function;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeColorFunc();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeColorFunc, FunctionEnum>(nameof(Function), n => n.Function, (n, v) => n.Function = v, stored: true)]);
}
