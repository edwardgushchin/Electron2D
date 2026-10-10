namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedShaderRIDs = [], _ownedMaterialRIDs = [];
    internal RID ShaderCreateCore() { EnsureTextureChange(); var shader = new Shader(); var rid = RenderingProgramRegistry<Shader>.Register(shader, this); shader.BindRenderingRID(rid); _ownedShaderRIDs.Add(rid); return rid; }
    internal RID MaterialCreateCore() { EnsureTextureChange(); var material = new ShaderMaterial(); var rid = RenderingProgramRegistry<Material>.Register(material, this); material.BindRenderingRID(rid); _ownedMaterialRIDs.Add(rid); return rid; }
    internal void ShaderSetSPIRVCore(RID rid, ReadOnlySpan<byte> code)
    {
        EnsureTextureChange(); var entry = RenderingProgramRegistry<Shader>.OwnedEntry(rid, this);
        try { entry.Retained!.SetSPIRV(code); }
        catch (ArgumentException error) when (entry.PathHint.Length != 0) { throw new ArgumentException($"Shader '{entry.PathHint}' rejected the compiled module: {error.Message}", nameof(code), error); }
        catch (NotSupportedException error) when (entry.PathHint.Length != 0) { throw new NotSupportedException($"Shader '{entry.PathHint}' rejected the compiled module: {error.Message}", error); }
    }
    internal byte[] ShaderGetSPIRVCore(RID rid) { EnsureOwner(); return RenderingProgramRegistry<Shader>.Resolve(rid).GetSPIRV(); }
    internal void ShaderSetPathHintCore(RID rid, string path) { EnsureTextureChange(); ArgumentNullException.ThrowIfNull(path); RenderingProgramRegistry<Shader>.OwnedEntry(rid, this).PathHint = path; }
    internal IReadOnlyList<PropertyDescriptor> GetShaderParameterListCore(RID rid) { EnsureOwner(); return RenderingProgramRegistry<Shader>.Resolve(rid).GetShaderUniformList(); }
    internal void ShaderSetDefaultTextureParameterCore(RID rid, string name, RID texture, int index) { EnsureTextureChange(); var shader = RenderingProgramRegistry<Shader>.Owned(rid, this); shader.SetDefaultSampledResource(name, texture.IsValid() ? RenderingTextureRegistry.ResolveResource(texture) : null, index); }
    internal RID ShaderGetDefaultTextureParameterCore(RID rid, string name, int index) { EnsureOwner(); return RenderingProgramRegistry<Shader>.Resolve(rid).GetDefaultSampledResource(name, index)?.GetRID() ?? default; }
    private ShaderMaterial MutableProgramMaterial(RID rid) { EnsureTextureChange(); return RenderingProgramRegistry<Material>.Owned(rid, this) as ShaderMaterial ?? throw new ArgumentException("Material is not programmable.", nameof(rid)); }
    private ShaderMaterial ReadProgramMaterial(RID rid) { EnsureOwner(); return RenderingProgramRegistry<Material>.Resolve(rid) as ShaderMaterial ?? throw new ArgumentException("Material is not programmable.", nameof(rid)); }
    internal void MaterialSetShaderCore(RID rid, RID shader) { var material = MutableProgramMaterial(rid); material.Shader = shader.IsValid() ? RenderingProgramRegistry<Shader>.Resolve(shader) : null; }
    internal void MaterialSetParamCore<T>(RID rid, string name, T value) where T : unmanaged => MutableProgramMaterial(rid).SetShaderParameter(name, value);
    internal void MaterialSetParamCore<T>(RID rid, string name, ReadOnlySpan<T> values) where T : unmanaged => MutableProgramMaterial(rid).SetShaderParameter(name, values);
    internal void MaterialSetParamTextureCore(RID rid, string name, RID texture) => MutableProgramMaterial(rid).SetSampledResource(name, texture.IsValid() ? RenderingTextureRegistry.ResolveResource(texture) : null);
    internal T MaterialGetParamCore<T>(RID rid, string name) where T : unmanaged => ReadProgramMaterial(rid).GetShaderParameter<T>(name);
    internal T[] MaterialGetParamArrayCore<T>(RID rid, string name) where T : unmanaged => ReadProgramMaterial(rid).GetShaderParameterArray<T>(name);
    internal void MaterialGetParamCore<T>(RID rid, string name, Span<T> destination) where T : unmanaged => ReadProgramMaterial(rid).CopyShaderParameterArray(name, destination);
    internal RID MaterialGetParamTextureCore(RID rid, string name) => ReadProgramMaterial(rid).GetSampledResource(name)?.GetRID() ?? default;
    internal void CanvasItemSetMaterialCore(RID rid, RID material)
    { var state = Change(rid, out _); if (material.IsValid()) RenderingProgramRegistry<Material>.Resolve(material); state.Material = material; state.MaterialAssigned = true; }
    private bool ReleaseProgramRID(RID rid)
    {
        if (RenderingProgramRegistry<Material>.Contains(rid)) { var material = RenderingProgramRegistry<Material>.Owned(rid, this); _ownedMaterialRIDs.Remove(rid); RenderingProgramRegistry<Material>.Remove(rid); material.Dispose(); return true; }
        if (!RenderingProgramRegistry<Shader>.Contains(rid)) return false;
        var shader = RenderingProgramRegistry<Shader>.Owned(rid, this);
        foreach (var materialRID in _ownedMaterialRIDs) if (RenderingProgramRegistry<Material>.Owned(materialRID, this) is ShaderMaterial material && ReferenceEquals(material.Shader, shader)) material.Shader = null;
        _ownedShaderRIDs.Remove(rid); RenderingProgramRegistry<Shader>.Remove(rid); shader.Dispose(); return true;
    }
    private void ReleaseOwnedPrograms()
    {
        foreach (var rid in _ownedMaterialRIDs) { var material = RenderingProgramRegistry<Material>.Owned(rid, this); RenderingProgramRegistry<Material>.Remove(rid); material.Dispose(); }
        _ownedMaterialRIDs.Clear();
        foreach (var rid in _ownedShaderRIDs) { var shader = RenderingProgramRegistry<Shader>.Owned(rid, this); RenderingProgramRegistry<Shader>.Remove(rid); shader.Dispose(); }
        _ownedShaderRIDs.Clear();
    }
}
