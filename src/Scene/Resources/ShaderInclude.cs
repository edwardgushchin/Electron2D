using System.Text;

namespace Electron2D;

/// <summary>Owns reusable HLSL or GLSL source consumed by the shader build importer.</summary>
/// <remarks>Source edits emit Changed after commitment. Save the source and rebuild dependent shaders to replace
/// their compiled programs; a runtime source edit does not compile or mutate an already loaded Shader.</remarks>
public sealed class ShaderInclude : Resource
{
    internal const int MaximumSourceBytes = 8 * 1024 * 1024;
    internal static readonly UTF8Encoding SourceEncoding = new(false, true);
    private readonly object _codeGate = new();
    private string _code = "";

    /// <summary>Creates an empty shader include source.</summary>
    public ShaderInclude() { }

    /// <summary>Gets or replaces the complete include source.</summary>
    /// <value>Empty initially; strict UTF-8 source is bounded to eight MiB.</value>
    /// <remarks>Every successful assignment emits Changed, including assignment of equal text. Source syntax
    /// and nested includes are evaluated by the selected HLSL or GLSL compiler during import/build.</remarks>
    /// <exception cref="ArgumentNullException">Text is null.</exception>
    /// <exception cref="ArgumentException">Text has invalid UTF-16 or exceeds its encoded byte budget.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string Code
    {
        get { lock (_codeGate) { ThrowIfDisposed(); return _code; } }
        set
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value);
            if (SourceEncoding.GetByteCount(value) > MaximumSourceBytes) throw new ArgumentException("Shader include source exceeds its byte budget.", nameof(value));
            lock (_codeGate) { ThrowIfDisposed(); _code = value; }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<ShaderInclude, string>(nameof(Code), source => source.Code, (source, value) => source.Code = value, _ => "", stored: true)]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ShaderInclude();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((ShaderInclude)target).Code = Code;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_codeGate) _code = ""; base.Dispose(disposing); }
}
