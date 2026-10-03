namespace Electron2D;

public abstract partial class CanvasItem
{
    private List<CanvasMesh>? _meshes;
    private int _meshCount;
    /// <summary>Records a borrowed mesh with live surface geometry during canvas recording.</summary>
    /// <param name="mesh">Borrowed live two-dimensional mesh.</param>
    /// <param name="texture">Optional borrowed texture; atlas views use their full backing texture.</param>
    /// <param name="transform">Local mesh transform, identity when null.</param>
    /// <param name="modulate">Mesh color multiplier, white when null.</param>
    /// <remarks>Retained draws observe surface edits without rerecording. Per-surface materials override this item's
    /// material; transforms, clip, order, sampling and modulation remain inherited canvas policies.</remarks>
    /// <exception cref="ArgumentNullException">The mesh is null.</exception>
    /// <exception cref="ArgumentException">Transform or color is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">A borrowed resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope.</exception>
    public void DrawMesh(Mesh mesh, Texture? texture = null, Transform? transform = null, Color? modulate = null)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(mesh); ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
        var pose = transform ?? Transform.Identity; var tint = modulate ?? Colors.White;
        if (!pose.IsFinite() || !tint.IsFinite()) throw new ArgumentException("Mesh transform and color must be finite.");
        if (texture is AtlasTexture atlas) { var rid = atlas.GetRID(); texture = rid.IsValid() ? RenderingTextureRegistry.Resolve(rid) : null; }
        EnsureDrawing(); if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        _meshes ??= [];
        if (_meshCount == _meshes.Count) _meshes.Add(new(mesh, pose, tint));
        else _meshes[_meshCount].Set(mesh, pose, tint);
        (_canvasCommands ??= []).Add(new CanvasCommand(false, default, default, Colors.White, 0, false, Transform.Identity, texture, Mesh: _meshes[_meshCount++]));
    }

    /// <summary>Records a borrowed mesh identity through the same typed canvas path.</summary>
    /// <param name="mesh">Logical live mesh RID.</param>
    /// <param name="texture">Optional live texture RID, empty for no texture.</param>
    /// <param name="transform">Local transform or identity.</param>
    /// <param name="modulate">Color multiplier or white.</param>
    /// <exception cref="ArgumentException">A supplied identity is absent or disposed.</exception>
    public void DrawMesh(RID mesh, RID texture = default, Transform? transform = null, Color? modulate = null) =>
        DrawMesh(RenderingMeshRegistry.Resolve(mesh), texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null, transform, modulate);
}

public abstract partial class CanvasItem
{
    private List<CanvasMultiMesh>? _multiMeshes;
    private int _multiMeshCount;
    /// <summary>Records a borrowed instance resource with live packed data and mesh surfaces.</summary>
    /// <param name="multiMesh">Borrowed live instance storage.</param>
    /// <param name="texture">Optional borrowed surface texture.</param>
    /// <remarks>Replays the visible prefix in surface/instance order; transforms, instance colors, shader data
    /// and eligible physics interpolation are read during replay rather than copied into drawing commands.</remarks>
    /// <exception cref="InvalidOperationException">Called outside the drawing scope.</exception>
    /// <exception cref="ObjectDisposedException">A supplied resource is disposed.</exception>
    public void DrawMultiMesh(MultiMesh multiMesh, Texture? texture = null)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(multiMesh); multiMesh.GetRID();
        if (texture is AtlasTexture atlas) { var rid = atlas.GetRID(); texture = rid.IsValid() ? RenderingTextureRegistry.Resolve(rid) : null; }
        EnsureDrawing(); if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        _multiMeshes ??= [];
        if (_multiMeshCount == _multiMeshes.Count) _multiMeshes.Add(new(multiMesh)); else _multiMeshes[_multiMeshCount].Set(multiMesh);
        (_canvasCommands ??= []).Add(new(false, default, default, Colors.White, 0, false, Transform.Identity, texture, MultiMesh: _multiMeshes[_multiMeshCount++]));
    }
    /// <summary>Records live instance and texture identities through the same retained path.</summary>
    /// <param name="multiMesh">Borrowed or owned logical instance identity.</param>
    /// <param name="texture">Optional live texture identity.</param>
    /// <exception cref="ArgumentException">An identity is missing or disposed.</exception>
    public void DrawMultiMesh(RID multiMesh, RID texture = default) => DrawMultiMesh(RenderingMultiMeshRegistry.Resolve(multiMesh), texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null);
}
