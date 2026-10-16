using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

namespace Electron2D;

/// <summary>Temporarily overrides global shader values while this node belongs to the rendered scene tree.</summary>
/// <remarks>The first node in tree order is active; other nodes retain their authored values until promoted.
/// Membership is observable through shader_overrides_group and shader_overrides_group_active.
/// Values can be authored and saved without a renderer. Removing the active node clears its temporary
/// values and activates the next node, or restores current base values. Textures are borrowed.</remarks>
public class ShaderGlobalsOverride : Node
{
    internal readonly ShaderGlobalParameters Parameters = new();
    internal SceneTree? RegisteredTree;

    /// <summary>Creates a detached node without authored overrides.</summary>
    public ShaderGlobalsOverride() { }

    /// <summary>Stores or clears one typed scalar, vector, rectangle, color or planar transform override.</summary>
    /// <typeparam name="T">The carrier required by the logical role.</typeparam>
    /// <param name="name">Exact nonblank global name.</param>
    /// <param name="type">The logical role, retained in portable scene storage.</param>
    /// <param name="value">A finite copied value; null clears the override.</param>
    /// <remarks>An existing authored entry must be removed before changing its role. Unknown global names
    /// remain authored until registered in the rendering session. An active incompatible role rejects.</remarks>
    /// <exception cref="ArgumentException">The name, role, carrier or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during capture/submission.</exception>
    /// <exception cref="AggregateException">Publication or property-list callbacks fail after the authored value commits.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetShaderParameter<T>(string name, Kind type, T? value) where T : unmanaged
    {
        Changing(name, type, value is null); var previous = Parameters.GetType(name);
        Parameters.SceneSet(name, type, value); Changed(name, previous);
    }

    /// <summary>Stores or clears a numeric matrix override.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <param name="type">Mat3 or Mat4.</param>
    /// <param name="value">Nine or sixteen finite column-major elements; an empty span clears the override.</param>
    /// <exception cref="ArgumentException">The name, role, shape or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during capture/submission.</exception>
    /// <exception cref="AggregateException">Publication or property-list callbacks fail after the authored value commits.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetShaderParameter(string name, Kind type, ReadOnlySpan<float> value)
    {
        Changing(name, type, value.IsEmpty); var previous = Parameters.GetType(name);
        Parameters.SceneSetMatrix(name, type, value); Changed(name, previous);
    }

    /// <summary>Stores a borrowed ordinary texture override, or clears it with null.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <param name="value">The borrowed live texture, or null to restore the base value.</param>
    /// <exception cref="ArgumentException">The name or logical role is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during capture/submission.</exception>
    /// <exception cref="AggregateException">Publication or property-list callbacks fail after the authored value commits.</exception>
    /// <exception cref="ObjectDisposedException">The node or supplied texture is disposed.</exception>
    public void SetShaderParameter(string name, Texture? value)
    {
        Changing(name, Kind.Sampler2D, value is null); var previous = Parameters.GetType(name);
        Parameters.SceneSetImage(name, value, false); Changed(name, previous);
    }

    /// <summary>Stores a borrowed image-array override, or clears it with null.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <param name="value">The borrowed live image array, or null to restore the base value.</param>
    /// <exception cref="ArgumentException">The name or logical role is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during capture/submission.</exception>
    /// <exception cref="AggregateException">Publication or property-list callbacks fail after the authored value commits.</exception>
    /// <exception cref="ObjectDisposedException">The node or supplied texture is disposed.</exception>
    public void SetShaderParameter(string name, TextureLayered? value)
    {
        Changing(name, Kind.Sampler2DArray, value is null); var previous = Parameters.GetType(name);
        Parameters.SceneSetImage(name, value, true); Changed(name, previous);
    }

    /// <summary>Reads an authored scalar/vector value, independently of active selection and base values.</summary>
    /// <typeparam name="T">The exact stored carrier.</typeparam>
    /// <param name="name">Exact nonblank global name.</param>
    /// <returns>The copied authored value, or null when absent.</returns>
    /// <exception cref="ArgumentException">The name or requested carrier is invalid.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public T? GetShaderParameter<T>(string name) where T : unmanaged { Reading(); return Parameters.SceneValue<T>(name); }

    /// <summary>Copies an authored numeric matrix.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <returns>An independent column-major array, or null when absent.</returns>
    /// <exception cref="ArgumentException">The name or requested role is invalid.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public float[]? GetShaderParameterMatrix(string name) { Reading(); return Parameters.SceneMatrix(name); }

    /// <summary>Reads the borrowed authored ordinary texture.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <returns>The borrowed resource, or null when absent.</returns>
    /// <exception cref="ArgumentException">The name or requested role is invalid.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Texture? GetShaderParameterTexture(string name) { Reading(); return (Texture?)Parameters.SceneImage(name, false); }

    /// <summary>Reads the borrowed authored image array.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <returns>The borrowed resource, or null when absent.</returns>
    /// <exception cref="ArgumentException">The name or requested role is invalid.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public TextureLayered? GetShaderParameterLayeredTexture(string name) { Reading(); return (TextureLayered?)Parameters.SceneImage(name, true); }

    /// <summary>Copies authored names in ordinal order.</summary>
    /// <returns>An independent array; empty for a node without overrides.</returns>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string[] GetShaderParameterList() { Reading(); return Parameters.Names(); }

    /// <summary>Reads an authored logical role.</summary>
    /// <param name="name">Exact nonblank global name.</param>
    /// <returns>The stored role, or Max when absent.</returns>
    /// <exception cref="ArgumentException">The name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Kind GetShaderParameterType(string name) { Reading(); return Parameters.GetType(name); }

    /// <inheritdoc />
    /// <remarks>Completes inherited scene cleanup before releasing authored values and borrowed image references.</remarks>
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) Parameters.Clear(); }
    }
    private void Reading() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Changing(string name, Kind kind, bool clear)
    { EnsureMutable(); ShaderGlobalParameters.ValidateName(name); RenderingServer.ValidateSceneGlobal(this, name, clear ? Kind.Max : kind); }
    private void Changed(string name, Kind previous)
    {
        List<Exception>? errors = null;
        try { RenderingServer.RefreshSceneGlobal(this, name); } catch (Exception error) { CollectException(ref errors, error); }
        if (previous != Parameters.GetType(name))
            try { NotifyPropertyListChanged(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Scene shader parameter publication or notifications failed.", errors);
    }
    internal sealed override void OnTreeMembershipChanged(bool entering)
    {
        List<Exception>? errors = null;
        try { base.OnTreeMembershipChanged(entering); } catch (Exception error) { CollectException(ref errors, error); }
        if (entering)
        {
            RegisteredTree = Tree;
            SetLifecycleGroup("shader_overrides_group", true);
            try { Tree!.RegisterShaderGlobals(this); } catch (Exception error) { CollectException(ref errors, error); }
        }
        else
        {
            var previous = RegisteredTree; RegisteredTree = null;
            SetLifecycleGroup("shader_overrides_group", false); SetLifecycleGroup("shader_overrides_group_active", false);
            try { previous?.UnregisterShaderGlobals(this); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Scene shader override lifecycle failed.", errors);
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return RegisteredTree is { } tree && !ReferenceEquals(tree.SelectedShaderGlobals, this)
            ? [.. warnings, "Another ShaderGlobalsOverride precedes this node in tree order; this node is inactive."] : warnings;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Parameters.SceneDescriptors).Concat(RenderingServer.SceneGlobalDescriptors(Parameters));
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ShaderGlobalsOverride) ? CreateOverride : base.CreateSceneInstanceFactory();
    private static Node CreateOverride() => new ShaderGlobalsOverride();
}
