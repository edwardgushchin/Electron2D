namespace Electron2D;

/// <summary>A graph definition with portable canvas-layout size metadata.</summary>
/// <remarks>Size belongs to source authoring and has no effect on rendered geometry or compiled code.</remarks>
public abstract class VisualShaderNodeResizableBase : VisualShaderNode
{
    private Vector2 _size;
    /// <summary>Initializes zero layout size.</summary>
    protected VisualShaderNodeResizableBase() { }
    /// <summary>Gets or sets the finite graph-layout size without publishing Changed.</summary>
    /// <value>Zero initially. Signed components are preserved as source metadata.</value>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The definition is disposed.</exception>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentException("Graph layout size must be finite.", nameof(value)); _size = value; }
    }
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeResizableBase)target)._size = _size;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeResizableBase, Vector2>(nameof(Size), n => n.Size, (n, v) => n.Size = v, stored: true) }.Concat(base.GetPropertyDescriptors());
}
