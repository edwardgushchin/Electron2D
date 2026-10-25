namespace Electron2D;

/// <summary>Reads a named typed input in an imported visual shader stage.</summary>
/// <remarks>Unknown or out-of-context names retain the scalar-zero role. A borrowing graph resolves the port type
/// independently for its stage; changing one borrower does not bind this definition to it. InputTypeChanged
/// reports the named semantic role. Canvas texture_pixel_size reads the inverse logical dimensions of the
/// current command's resolved diffuse texture in vertex and fragment stages, or one for a missing image.
/// Atlas regions retain the backing image size; material image overrides do not change this input.
/// Canvas vertex custom0/custom1 read the first two decoded mesh channels, independently of instance_custom.
/// Canvas vertex instance_id reads the logical instance or particle slot, or zero for ordinary geometry.
/// Canvas vertex model_matrix includes the item, draw command, mesh and current instance pose.
/// Canvas_matrix maps canvas coordinates to framebuffer pixels; screen_matrix maps those pixels to clip
/// coordinates with X from -1 to 1 and Y from 1 to -1. All are numeric four-by-four matrices.
/// The mesh/instance and matrix names have the scalar-zero role outside the canvas vertex stage.
/// Known inputs in their applicable context require a real renderer producer.</remarks>
public sealed class VisualShaderNodeInput : VisualShaderNode
{
    private string _inputName = "[None]";
    /// <summary>Creates a scalar-zero input operation.</summary>
    public VisualShaderNodeInput() { }
    /// <summary>Gets or replaces the case-sensitive input name.</summary>
    /// <value>Initially [None]; examples are uv, color, velocity, number and time.</value>
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
            if (value is "attractor_force" or "region_rect" or "specular_shininess" or "light" or "light_color" or "light_direction" or "light_energy" or "light_is_directional")
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
    /// <returns>The uppercase named symbol, or an empty string for an unknown input; this query has no borrowing-stage context.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string GetInputRealName()
    { ThrowIfDisposed(); return IsKnown ? _inputName.ToUpperInvariant() : ""; }

    private bool IsKnown => _inputName is "model_matrix" or "canvas_matrix" or "screen_matrix" or "active" or "custom" or "delta" or "emission_transform" or "index" or "lifetime" or "number" or "random_seed" or "restart" or "transform" or "velocity" or "collision_depth" or "collision_normal" or "modulate" or "source_texture0" or "source_texture1" or "source_texture2" or "source_texture3" or "color" or "uv" or "vertex" or "time" or "fragcoord" or "instance_custom" or "custom0" or "custom1" or
        "vertex_id" or "instance_id" or "at_light_pass" or "point_size" or "point_coord" or "screen_pixel_size" or "screen_uv" or "texture_pixel_size" or "texture" or "normal_texture" or "specular_shininess_texture";
    private bool IsKnownInStage(VisualShader.Type stage) => stage switch
    {
        VisualShader.Type.TextureBlit => _inputName is "modulate" or "uv" or "fragcoord" or "source_texture0" or "source_texture1" or "source_texture2" or "source_texture3",
        >= VisualShader.Type.Start and <= VisualShader.Type.ProcessCustom => _inputName is "active" or "color" or "custom" or "delta" or "emission_transform" or "index" or "lifetime" or "number" or "random_seed" or "time" or "transform" or "velocity" ||
            _inputName == "restart" && stage is VisualShader.Type.Process or VisualShader.Type.ProcessCustom or VisualShader.Type.Collide ||
            _inputName is "collision_depth" or "collision_normal" && stage == VisualShader.Type.Collide,
        _ => _inputName is "color" or "uv" or "vertex" or "time" or "fragcoord" or "instance_custom" or "vertex_id" or "at_light_pass" or "point_size" or "point_coord" or "screen_pixel_size" or "screen_uv" or "texture_pixel_size" or "texture" or "normal_texture" or "specular_shininess_texture" ||
            stage == VisualShader.Type.Vertex && _inputName is "custom0" or "custom1" or "instance_id" or "model_matrix" or "canvas_matrix" or "screen_matrix"
    };
    internal PortType[] OutputsFor(VisualShader.Type stage) => [IsKnownInStage(stage) ? InputKind : PortType.Scalar];
    private PortType InputKind => _inputName switch
    {
        "source_texture0" or "source_texture1" or "source_texture2" or "source_texture3" or "texture" or "normal_texture" or "specular_shininess_texture" => PortType.Sampler,
        "color" or "modulate" or "custom" or "fragcoord" or "instance_custom" or "custom0" or "custom1" => PortType.Vector4,
        "uv" or "vertex" or "point_coord" or "screen_pixel_size" or "screen_uv" or "texture_pixel_size" => PortType.Vector2,
        "velocity" or "collision_normal" => PortType.Vector2,
        "transform" or "emission_transform" or "model_matrix" or "canvas_matrix" or "screen_matrix" => PortType.Matrix,
        "index" or "number" or "random_seed" => PortType.ScalarUInt,
        "vertex_id" or "instance_id" => PortType.ScalarInt,
        "active" or "restart" or "at_light_pass" => PortType.Boolean,
        _ => PortType.Scalar
    };
    internal override PortType[] Inputs => [];
    internal override PortType[] Outputs => [InputKind];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => IsKnown ? GetInputRealName() : "0.0";
    internal string GenerateExpression(VisualShader.Type stage) => !IsKnownInStage(stage) ? "0.0" : stage == VisualShader.Type.TextureBlit ? _inputName switch
    {
        "modulate" => "MODULATE",
        "uv" => "UV",
        "fragcoord" => "gl_FragCoord",
        "source_texture0" or "source_texture1" or "source_texture2" or "source_texture3" => GetInputRealName(),
        _ when !IsKnownInStage(stage) => "0.0",
        _ => throw new NotSupportedException($"Input '{_inputName}' has no writable-texture producer.")
    } : stage is >= VisualShader.Type.Start and <= VisualShader.Type.ProcessCustom ? _inputName switch
    {
        "color" => "COLOR",
        "custom" => "CUSTOM",
        "velocity" => "VELOCITY.xy",
        "active" or "delta" or "emission_transform" or "index" or "lifetime" or "number" or "random_seed" or "restart" or "time" or "transform" => GetInputRealName(),
        "collision_depth" when stage == VisualShader.Type.Collide => "COLLISION_DEPTH",
        "collision_normal" when stage == VisualShader.Type.Collide => "COLLISION_NORMAL.xy",
        _ when !IsKnownInStage(stage) => "0.0",
        _ => throw new NotSupportedException($"Input '{_inputName}' has no particle lifecycle producer in this stage.")
    } : _inputName switch
    {
        "texture" or "normal_texture" or "specular_shininess_texture" when stage == VisualShader.Type.Fragment => GetInputRealName(),
        "color" => "COLOR",
        "uv" => "UV",
        "vertex" => stage == VisualShader.Type.Vertex ? "VERTEX" : "gl_FragCoord.xy",
        "time" => "TIME",
        "model_matrix" or "canvas_matrix" or "screen_matrix" when stage == VisualShader.Type.Vertex => GetInputRealName(),
        "instance_custom" => "INSTANCE_CUSTOM",
        "fragcoord" when stage == VisualShader.Type.Fragment => "gl_FragCoord",
        "screen_pixel_size" when stage == VisualShader.Type.Fragment => "SCREEN_PIXEL_SIZE",
        "screen_uv" when stage == VisualShader.Type.Fragment => "(gl_FragCoord.xy * SCREEN_PIXEL_SIZE)",
        "texture_pixel_size" when stage is VisualShader.Type.Vertex or VisualShader.Type.Fragment => "TEXTURE_PIXEL_SIZE",
        "vertex_id" when stage == VisualShader.Type.Vertex => "gl_VertexIndex",
        "instance_id" when stage == VisualShader.Type.Vertex => "gl_InstanceIndex",
        "custom0" when stage == VisualShader.Type.Vertex => "inMeshCustom0",
        "custom1" when stage == VisualShader.Type.Vertex => "inMeshCustom1",
        _ when !IsKnownInStage(stage) => "0.0",
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
