namespace Electron2D;

/// <summary>Executes a GLSL statement block with typed, named graph ports during offline canvas import.</summary>
/// <remarks>Reachable blocks run after their inputs, in a local scope. Numeric outputs begin at zero,
/// except matrices which begin at identity. Input sampler names refer to connected image symbols;
/// samplers cannot be local output variables. Global definitions use VisualShaderNodeGlobalExpression.
/// Statements can contain loops, branches and fragment discard. The shader compiler validates stage
/// legality. Graph edits never compile or replace the last imported runtime program.</remarks>
public class VisualShaderNodeExpression : VisualShaderNodeGroupBase
{
    private string _expression = "";
    /// <summary>Creates empty source with no ports and zero layout size.</summary>
    public VisualShaderNodeExpression() { }
    /// <summary>Gets or sets GLSL source consumed by the offline graph importer.</summary>
    /// <value>Empty initially; strict UTF-8 source is bounded to eight MiB.</value>
    /// <remarks>Every successful assignment publishes Changed, including equal text. Port identifier
    /// references are substituted without rewriting comments, member selectors or longer identifiers.</remarks>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    /// <exception cref="ArgumentException">The text has invalid UTF-16 or exceeds its UTF-8 byte budget.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public string Expression
    {
        get { ThrowIfDisposed(); return _expression; }
        set
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value);
            if (ShaderInclude.SourceEncoding.GetByteCount(value) > ShaderInclude.MaximumSourceBytes) throw new ArgumentException("Expression source exceeds its byte budget.", nameof(value));
            _expression = value; EmitChanged();
        }
    }
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Expression blocks require shared statement lowering.");
    internal override void CopyDefinitionTo(VisualShaderNode target)
    { base.CopyDefinitionTo(target); ((VisualShaderNodeExpression)target)._expression = _expression; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeExpression();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeExpression, string>(nameof(Expression), n => n.Expression, (n, v) => n.Expression = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
