namespace Electron2D;

public abstract partial class CanvasItem
{
    private InstanceUniformState? _instanceUniforms;
    internal InstanceUniformState InstanceUniforms => ServerState?.TileOwner?.InstanceUniforms ?? (_instanceUniforms ??= new());
    internal T? GetStoredInstanceParameter<T>(string name) where T : unmanaged
    { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (ServerState?.TileOwner?.InstanceUniforms ?? _instanceUniforms)?.GetStored<T>(name, GetInstanceProgram(name)); }
    internal void RestoreInstanceParameter<T>(string name, T? value) where T : unmanaged
    { EnsureMutable(); if (InstanceUniforms.Set(name, value, null)) NotifyPropertyListChanged(); }
    internal ShaderProgram? InstanceProgram => CanvasMaterial?.GetCanvasState()?.Program;
    internal ShaderProgram? GetInstanceProgram(string name)
    {
        var program = InstanceProgram;
        if (program?.InstanceUniforms.ContainsKey(name) == true) return program;
        var mesh = this is MeshInstance single ? single.Mesh : this is MultiMeshInstance many ? many.MultiMesh?.Mesh : null;
        if (FindMeshInstanceProgram(mesh, name) is { } surface) return surface;
        if (this is TileMapLayer tiles && tiles.FindTileInstanceProgram(name) is { } tile) return tile;
        var commands = ServerState?.Commands?._canvasCommands ?? _canvasCommands;
        if (commands is not null) foreach (var command in commands)
                if (FindMeshInstanceProgram(command.Mesh?.Mesh ?? command.MultiMesh?.Mesh, name) is { } recorded) return recorded;
        return program;
    }
    private static ShaderProgram? FindMeshInstanceProgram(Mesh? mesh, string name)
    {
        if (mesh is null) return null;
        for (var i = 0; i < mesh.GetSurfaceCount(); i++)
            if (mesh.SurfaceGetMaterial(i)?.GetCanvasState()?.Program is { } program && program.InstanceUniforms.ContainsKey(name)) return program;
        return null;
    }
    internal IEnumerable<PropertyDescriptor> InstanceParameterDescriptors(bool storedNames = false)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var program in InstancePrograms()) foreach (var descriptor in storedNames ? program.CanvasInstanceDescriptors : program.InstanceDescriptors)
                if (names.Add(descriptor.Name)) yield return descriptor;
    }
    private IEnumerable<ShaderProgram> InstancePrograms()
    {
        if (InstanceProgram is { } program) yield return program;
        if (this is TileMapLayer tiles) foreach (var tile in tiles.TileInstancePrograms()) yield return tile;
        var mesh = this is MeshInstance single ? single.Mesh : this is MultiMeshInstance many ? many.MultiMesh?.Mesh : null;
        if (mesh is not null) for (var i = 0; i < mesh.GetSurfaceCount(); i++)
                if (mesh.SurfaceGetMaterial(i)?.GetCanvasState()?.Program is { } surface) yield return surface;
        var commands = ServerState?.Commands?._canvasCommands ?? _canvasCommands;
        if (commands is not null) foreach (var command in commands)
            {
                mesh = command.Mesh?.Mesh ?? command.MultiMesh?.Mesh;
                if (mesh is not null) for (var i = 0; i < mesh.GetSurfaceCount(); i++)
                        if (mesh.SurfaceGetMaterial(i)?.GetCanvasState()?.Program is { } surface) yield return surface;
            }
    }
    /// <summary>Assigns one scalar or vector shader value to this canvas item.</summary>
    /// <typeparam name="T">The supported scalar/vector carrier matching the declared Instance member.</typeparam>
    /// <param name="name">Case-sensitive member name.</param>
    /// <param name="value">Copied value, or null to reset to the shader default.</param>
    /// <remarks>Values may be authored before a material is assigned. Live matching Instance members use the
    /// override independently of shared materials, without rerecording geometry. Floating values must be finite.
    /// Textures, arrays and matrices are not per-item parameters. Unset parameters follow the compiled shader default,
    /// with implicit zero when absent; explicit values survive compatible reloads. Prepared names retain their typed
    /// storage and descriptor so repeated set/clear cycles allocate no managed memory. Each MultiMesh node owns
    /// one parameter set shared by its hardware instances. Compatible surface materials also consume it.
    /// Reads and writes permit the owner/current processing group; the getter may prepare retained typed storage.</remarks>
    /// <exception cref="ArgumentException">The name/type is invalid or the value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is outside the owner/current processing group, or a capture is active.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void SetInstanceShaderParameter<T>(string name, T? value) where T : unmanaged
    {
        EnsureCanvasMetadataMutable();
        if (InstanceUniforms.Set(name, value, GetInstanceProgram(name))) NotifyPropertyListChanged();
    }
    /// <summary>Reads this item's override or the current shader's default value.</summary>
    /// <typeparam name="T">The matching scalar/vector carrier.</typeparam>
    /// <param name="name">Case-sensitive parameter name.</param>
    /// <returns>The effective value, or null when neither an override nor a declaration exists.</returns>
    /// <exception cref="ArgumentException">The name or requested type is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public T? GetInstanceShaderParameter<T>(string name) where T : unmanaged
    { ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return InstanceUniforms.Get<T>(name, GetInstanceProgram(name)); }
}
