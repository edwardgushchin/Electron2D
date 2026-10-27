namespace Electron2D;

/// <summary>Describes caller-owned vertex, fragment and compute sources for explicit shader authoring.</summary>
/// <remarks>Compilation uses the separately installed import tool. Pipelines consume the resulting RDShaderSPIRV through the ordinary validated device path. Native Half and Int8/UInt8/Int16/UInt16 data require independently enabled arithmetic/storage; compact push reconstruction also requires matching arithmetic.</remarks>
public sealed class RDShaderSource : ElectronObject
{
    /// <summary>Creates empty GLSL stage descriptions.</summary>
    public RDShaderSource() { }
    private RenderingDevice.ShaderLanguage _language;
    private string _vertex = "", _fragment = "", _compute = "";
    /// <summary>Gets or sets the source language shared by every stage.</summary>
    /// <value>GLSL initially. Unknown language values reject when consumed.</value>
    public RenderingDevice.ShaderLanguage Language { get { ThrowIfDisposed(); return _language; } set { ThrowIfDisposed(); _language = value; } }
    /// <summary>Gets or sets the vertex source.</summary>
    /// <value>Initially empty; null is invalid.</value>
    public string SourceVertex { get { ThrowIfDisposed(); return _vertex; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _vertex = value; } }
    /// <summary>Gets or sets the fragment source.</summary>
    /// <value>Initially empty; null is invalid.</value>
    public string SourceFragment { get { ThrowIfDisposed(); return _fragment; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _fragment = value; } }
    /// <summary>Gets or sets the compute source.</summary>
    /// <value>Initially empty; null is invalid.</value>
    public string SourceCompute { get { ThrowIfDisposed(); return _compute; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _compute = value; } }
    /// <summary>Returns the source of an executable stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <returns>The stage source, initially empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage has no executing source profile.</exception>
    public string GetStageSource(RenderingDevice.ShaderStage stage) => stage switch
    { RenderingDevice.ShaderStage.Vertex => SourceVertex, RenderingDevice.ShaderStage.Fragment => SourceFragment, RenderingDevice.ShaderStage.Compute => SourceCompute, _ => throw new ArgumentOutOfRangeException(nameof(stage)) };
    /// <summary>Replaces the source of an executable stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <param name="source">Non-null source; empty omits the stage.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage has no executing source profile.</exception>
    public void SetStageSource(RenderingDevice.ShaderStage stage, string source)
    { switch (stage) { case RenderingDevice.ShaderStage.Vertex: SourceVertex = source; break; case RenderingDevice.ShaderStage.Fragment: SourceFragment = source; break; case RenderingDevice.ShaderStage.Compute: SourceCompute = source; break; default: throw new ArgumentOutOfRangeException(nameof(stage)); } }
    /// <summary>Projects inherited metadata followed by copied source and language descriptions.</summary>
    /// <returns>Typed fields with empty source and GLSL defaults.</returns>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDShaderSource, RenderingDevice.ShaderLanguage>(nameof(Language), p => p.Language, (p, v) => p.Language = v, _ => RenderingDevice.ShaderLanguage.GLSL),
        new PropertyDescriptor<RDShaderSource, string>(nameof(SourceVertex), p => p.SourceVertex, (p, v) => p.SourceVertex = v, _ => ""),
        new PropertyDescriptor<RDShaderSource, string>(nameof(SourceFragment), p => p.SourceFragment, (p, v) => p.SourceFragment = v, _ => ""),
        new PropertyDescriptor<RDShaderSource, string>(nameof(SourceCompute), p => p.SourceCompute, (p, v) => p.SourceCompute = v, _ => ""),
    ]);
}
