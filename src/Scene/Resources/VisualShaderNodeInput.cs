namespace Electron2D;

/// <summary>Reads a named typed canvas input in an imported visual shader stage.</summary>
/// <remarks>Unknown input names retain the scalar-zero source role. Known inputs without a current canvas
/// ABI producer reject import rather than silently producing a replacement value.</remarks>
public sealed class VisualShaderNodeInput : VisualShaderNode
{
    private string _inputName = "[None]";
    /// <summary>Creates a scalar-zero input operation.</summary>
    public VisualShaderNodeInput() { }
    /// <summary>Gets or replaces the case-sensitive canvas input name.</summary>
    /// <value>Initially [None]; examples are uv, color, vertex and time.</value>
    /// <remarks>Changed is emitted after commitment. InputTypeChanged then reports a changed value role.</remarks>
    /// <exception cref="ArgumentException">The name exceeds 128 characters or contains a control character.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string InputName
    {
        get { ThrowIfDisposed(); return _inputName; }
        set
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 128 || value.Any(char.IsControl)) throw new ArgumentException("Invalid graph input name.", nameof(value));
            if (value is "canvas_matrix" or "model_matrix" or "screen_matrix")
                throw new NotSupportedException("This matrix graph input requires the renderer's canvas matrix ABI producer.");
            if (value is "region_rect" or "specular_shininess" or "light" or "light_color" or "light_direction" or "light_energy" or "light_is_directional")
                throw new NotSupportedException("This graph input requires its typed matrix, sampled-image or lighting integration.");
            var previous = InputKind; _inputName = value;
            Exception? changedError = null;
            try { EmitChanged(); } catch (Exception error) { changedError = error; }
            try { if (previous != InputKind) InputTypeChanged?.Invoke(); }
            catch (Exception error) { if (changedError is not null) throw new AggregateException(changedError, error); throw; }
            if (changedError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(changedError).Throw();
        }
    }
    /// <summary>Occurs after a name edit changes the output's value role.</summary>
    public event Action? InputTypeChanged;
    /// <summary>Returns the source symbol corresponding to the selected input.</summary>
    /// <returns>The uppercase canvas symbol, or an empty string for an unknown input.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string GetInputRealName()
    { ThrowIfDisposed(); return IsKnown ? _inputName.ToUpperInvariant() : ""; }

    private bool IsKnown => _inputName is "color" or "uv" or "vertex" or "time" or "fragcoord" or "instance_custom" or "custom0" or "custom1" or
        "vertex_id" or "instance_id" or "at_light_pass" or "point_size" or "point_coord" or "screen_pixel_size" or "screen_uv" or "texture_pixel_size" or "texture" or "normal_texture" or "specular_shininess_texture";
    private PortType InputKind => _inputName switch
    {
        "texture" or "normal_texture" or "specular_shininess_texture" => PortType.Sampler,
        "color" or "fragcoord" or "instance_custom" or "custom0" or "custom1" => PortType.Vector4,
        "uv" or "vertex" or "point_coord" or "screen_pixel_size" or "screen_uv" or "texture_pixel_size" => PortType.Vector2,
        "vertex_id" or "instance_id" => PortType.ScalarInt,
        "at_light_pass" => PortType.Boolean,
        _ => PortType.Scalar
    };
    internal override PortType[] Inputs => [];
    internal override PortType[] Outputs => [InputKind];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => IsKnown ? GetInputRealName() : "0.0";
    internal string Expression(VisualShader.Type stage) => _inputName switch
    {
        "texture" or "normal_texture" or "specular_shininess_texture" when stage == VisualShader.Type.Fragment => GetInputRealName(),
        "color" => "COLOR",
        "uv" => "UV",
        "vertex" => stage == VisualShader.Type.Vertex ? "VERTEX" : "gl_FragCoord.xy",
        "time" => "TIME",
        "instance_custom" => "INSTANCE_CUSTOM",
        "fragcoord" when stage == VisualShader.Type.Fragment => "gl_FragCoord",
        "screen_pixel_size" when stage == VisualShader.Type.Fragment => "SCREEN_PIXEL_SIZE",
        "screen_uv" when stage == VisualShader.Type.Fragment => "(gl_FragCoord.xy * SCREEN_PIXEL_SIZE)",
        "texture_pixel_size" => throw new NotSupportedException("Graph texture-pixel size requires the renderer's logical command-texture size producer."),
        "vertex_id" when stage == VisualShader.Type.Vertex => "gl_VertexIndex",
        _ when !IsKnown => "0.0",
        _ => throw new NotSupportedException($"Canvas input '{_inputName}' has no executing producer in this graph stage.")
    };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeInput)target)._inputName = _inputName;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeInput();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) InputTypeChanged = null; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<VisualShaderNodeInput, string>(nameof(InputName), n => n.InputName, (n, v) => n.InputName = v, stored: true)]);
}
