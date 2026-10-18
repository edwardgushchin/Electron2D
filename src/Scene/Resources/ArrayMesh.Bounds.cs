namespace Electron2D;

public sealed partial class ArrayMesh
{
    private Rect2 _customAABB;

    /// <summary>Gets or sets a mesh-local visibility rectangle for shader-displaced geometry.</summary>
    /// <value>The zero rectangle selects automatic bounds; any other finite nonnegative rectangle overrides visibility.</value>
    /// <remarks>This affects retained mesh and instance culling, including deformed meshes. It does not change
    /// source vertices, surface bounds or GetAABB. Every successful assignment emits Changed after commitment.
    /// Authors must cover the complete shader output; bounds do not transform or clip geometry.</remarks>
    /// <exception cref="ArgumentException">The rectangle is nonfinite, negative-sized or has an unrepresentable end.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="Exception">A Changed listener throws after the new bounds have committed.</exception>
    public Rect2 CustomAABB
    {
        get { lock (Gate) { ThrowIfDisposed(); return _customAABB; } }
        set
        {
            lock (Gate)
            {
                ThrowIfDisposed();
                if (!value.IsFinite() || !value.End.IsFinite() || value.Size.X < 0 || value.Size.Y < 0)
                    throw new ArgumentException("Mesh visibility bounds must be finite nonnegative rectangles.", nameof(value));
                _customAABB = value;
            }
            EmitChanged();
        }
    }
}
