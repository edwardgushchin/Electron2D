using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private Framebuffer? _drawTarget;
    private Entry? _graphicsPipeline;
    private VertexArray? _drawVertices;
    private IndexArray? _drawIndices;
    private Rect2 _drawRegion;
    private void DrawList(long id) { Access(); if (_drawPass == 0 || id != _activeList) throw new ArgumentException("The draw list is not active.", nameof(id)); }
    private static void FiniteColor(Color color) { if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A)) throw new ArgumentOutOfRangeException(nameof(color)); }
    private static Rect2 DrawRegion(Rect2 region, Vector2i size)
    {
        if (region == default) return new(0, 0, size.X, size.Y);
        var p = region.Position; var s = region.Size;
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(s.X) || !float.IsFinite(s.Y) || p.X < 0 || p.Y < 0 || s.X <= 0 || s.Y <= 0 || p.X + s.X > size.X || p.Y + s.Y > size.Y) throw new ArgumentOutOfRangeException(nameof(region));
        return region;
    }
    /// <summary>Begins an ordered color and depth/stencil render pass on borrowed framebuffer images.</summary>
    /// <param name="framebuffer">A live framebuffer with matching raster sample counts and optional resolve destinations.</param>
    /// <param name="drawFlags">Per-target clear or discard load flags; default loads existing pixels.</param>
    /// <param name="clearColorValues">One clear value per color render slot when a color clear flag is set; finite float components or raw integer lane bits as in TextureClear.</param>
    /// <param name="region">Viewport and initial scissor; zero selects the whole framebuffer.</param>
    /// <param name="clearDepthValue">Finite depth clear from zero through one; defaults to one.</param>
    /// <param name="clearStencilValue">Eight-bit stencil clear; defaults to zero.</param>
    /// <returns>The active owner-thread draw-list identity.</returns>
    /// <remarks>Clears affect the complete attachment. The region restricts draws. Results become available to subsequent compute, copy and canvas passes after DrawListEnd.</remarks>
    public long DrawListBegin(RID framebuffer, DrawFlags drawFlags = 0, ReadOnlySpan<Color> clearColorValues = default, Rect2 region = default, float clearDepthValue = 1, int clearStencilValue = 0)
    {
        IdleList(); var target = GetFramebuffer(framebuffer); var actual = DrawRegion(region, target.Size);
        var layout = FramebufferFormat(target.Format); var depthIndex = layout.DepthIndex;
        if (!float.IsFinite(clearDepthValue) || clearDepthValue is < 0 or > 1 || clearStencilValue is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(clearDepthValue));
        if ((drawFlags & DrawFlags.ClearDepth) != 0 && (drawFlags & DrawFlags.IgnoreDepth) != 0 || (drawFlags & DrawFlags.ClearStencil) != 0 && (drawFlags & DrawFlags.IgnoreStencil) != 0) throw new ArgumentException("An aspect cannot both clear and discard.", nameof(drawFlags));

        if (((uint)drawFlags & ~1048575u) != 0) throw new ArgumentOutOfRangeException(nameof(drawFlags));
        var clears = (uint)drawFlags & 255; var ignores = ((uint)drawFlags >> 8) & 255;
        if ((clears & ignores) != 0 || clears != 0 && clearColorValues.Length != layout.Colors.Length || clears == 0 && !clearColorValues.IsEmpty && clearColorValues.Length != layout.Colors.Length) throw new ArgumentException("Clear colors and flags do not match the attachments.");
        for (var i = 0; i < clearColorValues.Length; i++)
        {
            var format = Get(target.Textures[layout.Colors[i]], Kind.Texture).Texture!.Format;
            if (PixelType(format) == 0) FiniteColor(clearColorValues[i]);
            else { if (TextureDriver != "vulkan") throw new NotSupportedException("Integer clear lanes require verification of this native backend."); ValidateSignedClear(format, clearColorValues[i]); }
        }
        var colors = stackalloc SDL.GPUColorTargetInfo[8];
        for (var i = 0; i < layout.Colors.Length; i++)
        {
            var color = clearColorValues.IsEmpty ? default : clearColorValues[i];
            colors[i] = new()
            {
                Texture = Get(target.Textures[layout.Colors[i]], Kind.Texture).Handle!.DangerousGetHandle(),
                ClearColor = new() { R = color.R, G = color.G, B = color.B, A = color.A },
                LoadOp = (clears & (1u << i)) != 0 ? SDL.GPULoadOp.Clear : (ignores & (1u << i)) != 0 ? SDL.GPULoadOp.DontCare : SDL.GPULoadOp.Load,
                StoreOp = layout.Resolves[i] >= 0 ? SDL.GPUStoreOp.ResolveAndStore : SDL.GPUStoreOp.Store,
                ResolveTexture = layout.Resolves[i] >= 0 ? Get(target.Textures[layout.Resolves[i]], Kind.Texture).Handle!.DangerousGetHandle() : 0
            };
        }
        var depth = new SDL.GPUDepthStencilTargetInfo();
        if (depthIndex >= 0)
        {
            depth.Texture = Get(target.Textures[depthIndex], Kind.Texture).Handle!.DangerousGetHandle(); depth.ClearDepth = clearDepthValue; depth.ClearStencil = (byte)clearStencilValue;
            depth.LoadOp = (drawFlags & DrawFlags.ClearDepth) != 0 ? SDL.GPULoadOp.Clear : (drawFlags & DrawFlags.IgnoreDepth) != 0 ? SDL.GPULoadOp.DontCare : SDL.GPULoadOp.Load;
            depth.StencilLoadOp = (drawFlags & DrawFlags.ClearStencil) != 0 ? SDL.GPULoadOp.Clear : (drawFlags & DrawFlags.IgnoreStencil) != 0 ? SDL.GPULoadOp.DontCare : SDL.GPULoadOp.Load;
            depth.StoreOp = SDL.GPUStoreOp.Store; depth.StencilStoreOp = SDL.GPUStoreOp.Store;
        }
        var pass = SDL.BeginGPURenderPass(Commands(), (nint)colors, (uint)layout.Colors.Length, depthIndex >= 0 ? (nint)(&depth) : 0);
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin device graphics pass");
        _drawPass = pass; _drawTarget = target; _drawRegion = actual; _graphicsPipeline = null; _drawVertices = null; _drawIndices = null; _sets.Clear();
        SDL.SetGPUViewport(pass, new() { X = actual.Position.X, Y = actual.Position.Y, W = actual.Size.X, H = actual.Size.Y, MinDepth = 0, MaxDepth = 1 });
        SetScissor(actual);
        return _activeList = ++_nextList;
    }
    /// <summary>Binds a graphics pipeline matching the active framebuffer.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="renderPipeline">A live matching pipeline of this device.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListBindRenderPipeline(long drawList, RID renderPipeline)
    {
        DrawList(drawList); var entry = Get(renderPipeline, Kind.GraphicsPipeline); Get(entry.Shader, Kind.Shader);
        if (!SameRasterLayout(FramebufferFormat(entry.Graphics!.FramebufferFormat), FramebufferFormat(_drawTarget!.Format))) throw new ArgumentException("The pipeline framebuffer layout does not match.", nameof(renderPipeline));
        _graphicsPipeline = entry; SDL.BindGPUGraphicsPipeline(_drawPass, entry.Handle!.DangerousGetHandle()); SetBlendConstants(entry.Graphics.Constant); SDL.SetGPUStencilReference(_drawPass, entry.Graphics.StencilReference);
    }
    /// <summary>Binds a live borrowed vertex array for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="vertexArray">A live borrowed array with all source buffers intact.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListBindVertexArray(long drawList, RID vertexArray)
    { DrawList(drawList); var vertices = Get(vertexArray, Kind.VertexArray).Vertices!; foreach (var buffer in vertices.Buffers) Get(buffer, Kind.Vertex); _drawVertices = vertices; }
    /// <summary>Binds a live borrowed index range for subsequent indexed draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="indexArray">A live borrowed range with its source buffer intact.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListBindIndexArray(long drawList, RID indexArray)
    { DrawList(drawList); var indices = Get(indexArray, Kind.IndexArray).Indices!; Get(indices.Buffer, Kind.Index); _drawIndices = indices; }
    /// <summary>Binds a complete resource set at its logical shader set index.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="uniformSet">A complete live set belonging to the selected shader.</param>
    /// <param name="setIndex">The set logical descriptor index.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListBindUniformSet(long drawList, RID uniformSet, uint setIndex)
    {
        DrawList(drawList); var set = Get(uniformSet, Kind.Set);
        if (set.Set != setIndex || !UniformSetIsValid(uniformSet)) throw new ArgumentException("Invalid set or index.", nameof(uniformSet));
        _sets[setIndex] = set;
    }
    /// <summary>Sets the finite blend constant for subsequent draws.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="color">Finite components used by enabled constant blend factors.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListSetBlendConstants(long drawList, Color color)
    { DrawList(drawList); FiniteColor(color); if (_graphicsPipeline is null || (_graphicsPipeline.Graphics!.Dynamic & PipelineDynamicStateFlags.BlendConstants) == 0) throw new InvalidOperationException("Bind a pipeline with dynamic blend constants first."); SetBlendConstants(color); }
    private void SetBlendConstants(Color color) { SDL.SetGPUBlendConstants(_drawPass, new() { R = color.R, G = color.G, B = color.B, A = color.A }); }
    private void SetScissor(Rect2 rect)
    {
        var left = (int)MathF.Floor(rect.Position.X); var top = (int)MathF.Floor(rect.Position.Y);
        SDL.SetGPUScissor(_drawPass, new() { X = left, Y = top, W = (int)MathF.Ceiling(rect.End.X) - left, H = (int)MathF.Ceiling(rect.End.Y) - top });
    }
    /// <summary>Clips subsequent draws to the rectangle intersected with the framebuffer.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <param name="rect">A finite nonnegative rectangle, intersected with the framebuffer; zero clips all pixels.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListEnableScissor(long drawList, Rect2 rect = default)
    {
        DrawList(drawList); var p = rect.Position; var end = rect.End;
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(end.X) || !float.IsFinite(end.Y) || rect.Size.X < 0 || rect.Size.Y < 0) throw new ArgumentOutOfRangeException(nameof(rect));
        var size = _drawTarget!.Size; var left = Math.Clamp(p.X, 0, size.X); var top = Math.Clamp(p.Y, 0, size.Y); var right = Math.Clamp(end.X, left, size.X); var bottom = Math.Clamp(end.Y, top, size.Y);
        SetScissor(new(left, top, right - left, bottom - top));
    }
    /// <summary>Disables custom scissoring by restoring the complete framebuffer.</summary>
    /// <param name="drawList">The active draw-list identity.</param>
    /// <exception cref="ArgumentException">A list, resource identity, descriptor index or value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The creating thread, selected pipeline or resource dependencies are invalid.</exception>
    public void DrawListDisableScissor(long drawList) { DrawList(drawList); SetScissor(new(0, 0, _drawTarget!.Size.X, _drawTarget.Size.Y)); }
    private void BindDrawResources()
    {
        var program = Get(_graphicsPipeline!.Shader, Kind.Shader);
        Span<SDL.GPUTextureSamplerBinding> samplerBinding = stackalloc SDL.GPUTextureSamplerBinding[1];
        Span<nint> storageBinding = stackalloc nint[1];
        foreach (var descriptor in program.Layout)
        {
            if (!_sets.TryGetValue(descriptor.Set, out var set) || !ReferenceEquals(Get(set.Shader), program)) throw new InvalidOperationException("Bind every graphics resource set first.");
            var ids = set.Bindings[descriptor.Slot]; var resource = Get(ids[0]); var fragment = descriptor.Stage == ShaderStage.Fragment;
            if (descriptor.Type == UniformType.UniformBuffer)
            {
                if (fragment) SDL.PushGPUFragmentUniformData(_command, (uint)descriptor.Native, resource.Data.AsSpan(), resource.Size);
                else SDL.PushGPUVertexUniformData(_command, (uint)descriptor.Native, resource.Data.AsSpan(), resource.Size);
            }
            else if (descriptor.Type == UniformType.SamplerWithTexture)
            {
                var image = Get(ids[1]); foreach (var target in _drawTarget!.Textures) if (target == ids[1]) throw new InvalidOperationException("An attachment cannot be sampled during its render pass.");
                var binding = samplerBinding; binding[0] = new() { Sampler = resource.Handle!.DangerousGetHandle(), Texture = image.Handle!.DangerousGetHandle() };
                if (fragment) SDL.BindGPUFragmentSamplers(_drawPass, (uint)descriptor.Native, binding, 1); else SDL.BindGPUVertexSamplers(_drawPass, (uint)descriptor.Native, binding, 1);
            }
            else
            {
                var binding = storageBinding; binding[0] = resource.Handle!.DangerousGetHandle();
                if (descriptor.Type == UniformType.Image)
                {
                    foreach (var target in _drawTarget!.Textures) if (target == ids[0]) throw new InvalidOperationException("An attachment cannot be read as storage during its render pass.");
                    if (fragment) SDL.BindGPUFragmentStorageTextures(_drawPass, (uint)descriptor.Native, binding, 1); else SDL.BindGPUVertexStorageTextures(_drawPass, (uint)descriptor.Native, binding, 1);
                }
                else { if (fragment) SDL.BindGPUFragmentStorageBuffers(_drawPass, (uint)descriptor.Native, binding, 1); else SDL.BindGPUVertexStorageBuffers(_drawPass, (uint)descriptor.Native, binding, 1); }
            }
        }
    }
    /// <summary>Records a procedural, vertex-array or indexed draw with validated instance capacity.</summary>
    /// <param name="drawList">The active draw list.</param>
    /// <param name="useIndices">Draw the bound index range.</param>
    /// <param name="instances">Positive instance count.</param>
    /// <param name="proceduralVertexCount">Positive count when drawing without a vertex layout; otherwise zero.</param>
    /// <remarks>Index values produced or changed by GPU work must refer to vertices in the bound array. Index contents are not synchronously read back.</remarks>
    public void DrawListDraw(long drawList, bool useIndices, uint instances, uint proceduralVertexCount = 0)
    {
        DrawList(drawList); if (instances == 0) throw new ArgumentOutOfRangeException(nameof(instances));
        if (_graphicsPipeline is null) throw new InvalidOperationException("Bind a graphics pipeline first.");
        var count = PrepareDraw(useIndices, instances, proceduralVertexCount);
        if (useIndices) SDL.DrawGPUIndexedPrimitives(_drawPass, count, instances, 0, 0, 0); else SDL.DrawGPUPrimitives(_drawPass, count, instances, 0, 0);
    }
    private uint PrepareDraw(bool useIndices, uint instances, uint proceduralVertexCount)
    {
        Get(_graphicsPipeline!.Shader, Kind.Shader);
        uint count;
        if (_graphicsPipeline.Graphics!.VertexFormat == -1)
        { if (proceduralVertexCount == 0 && !useIndices) throw new ArgumentOutOfRangeException(nameof(proceduralVertexCount)); count = proceduralVertexCount; }
        else
        {
            if (_drawVertices is null || _drawVertices.Format != _graphicsPipeline.Graphics.VertexFormat || proceduralVertexCount != 0) throw new InvalidOperationException("Bind a matching vertex array without a procedural count.");
            var layout = VertexFormat(_drawVertices.Format); var bindings = stackalloc SDL.GPUBufferBinding[16];
            for (var i = 0; i < _drawVertices.Buffers.Length; i++)
            {
                var buffer = Get(_drawVertices.Buffers[i], Kind.Vertex); var n = layout.Buffers[i].InputRate == SDL.GPUVertexInputRate.Instance ? instances : _drawVertices.Count;
                if ((ulong)_drawVertices.Offsets[i] + (ulong)(n - 1) * layout.Buffers[i].Pitch + layout.Ends[i] > buffer.Size) throw new ArgumentException("Instance count exceeds its vertex buffer.");
                bindings[i] = new() { Buffer = buffer.Handle!.DangerousGetHandle(), Offset = _drawVertices.Offsets[i] };
            }
            if (_drawVertices.Buffers.Length != 0) SDL.BindGPUVertexBuffers(_drawPass, 0, (nint)bindings, (uint)_drawVertices.Buffers.Length);
            count = _drawVertices.Count;
        }
        Entry? indices = null;
        if (useIndices)
        {
            if (_drawIndices is null) throw new InvalidOperationException("Bind an index array first.");
            indices = Get(_drawIndices.Buffer, Kind.Index); count = _drawIndices.Count;
            var primitive = _graphicsPipeline.Graphics.Primitive;
            if (indices.RestartIndices && primitive is not (RenderPrimitive.LineStrips or RenderPrimitive.TriangleStripsWithRestartIndex) || !indices.RestartIndices && primitive == RenderPrimitive.TriangleStripsWithRestartIndex) throw new ArgumentException("Index restart role and strip topology must match.");
            SDL.BindGPUIndexBuffer(_drawPass, new() { Buffer = indices.Handle!.DangerousGetHandle(), Offset = _drawIndices.Offset }, indices.IndexFormat == IndexBufferFormat.UInt16 ? SDL.GPUIndexElementSize.IndexElementSize16Bit : SDL.GPUIndexElementSize.IndexElementSize32Bit);
        }
        BindDrawResources();
        return count;
    }
    /// <summary>Records GPU-directed draws from validated indirect command ranges.</summary>
    /// <param name="drawList">The active draw list.</param>
    /// <param name="useIndices">Use the bound index range.</param>
    /// <param name="buffer">A storage buffer created with DispatchIndirect usage.</param>
    /// <param name="offset">Four-byte-aligned first command byte offset.</param>
    /// <param name="drawCount">Command count; zero validates without drawing.</param>
    /// <param name="stride">Four-byte-aligned command spacing, or zero for tightly packed commands.</param>
    /// <remarks>Nonindexed commands contain four unsigned words: vertex count, instance count, first vertex and first instance.
    /// Indexed commands contain five words: index count, instance count, first index, signed vertex offset and first instance.
    /// The GPU producer must keep counts and offsets within bound vertex, instance and index capacities. Contents stay on the GPU.
    /// Keep first vertex/instance zero when shaders depend on portable built-in vertex/instance identifiers.</remarks>
    public void DrawListDrawIndirect(long drawList, bool useIndices, RID buffer, uint offset = 0, uint drawCount = 1, uint stride = 0)
    {
        DrawList(drawList); var entry = Get(buffer, Kind.Storage); var commandSize = useIndices ? 20u : 16u;
        if ((entry.Usage & StorageBufferUsage.DispatchIndirect) == 0) throw new ArgumentException("An indirect-capable storage buffer is required.", nameof(buffer));
        var step = stride == 0 ? commandSize : stride;
        if (offset % 4 != 0 || step % 4 != 0 || step < commandSize || offset > entry.Size || drawCount != 0 && (ulong)offset + (ulong)(drawCount - 1) * step + commandSize > entry.Size) throw new ArgumentOutOfRangeException(nameof(offset));
        if (_graphicsPipeline is null) throw new InvalidOperationException("Bind a graphics pipeline first.");
        PrepareDraw(useIndices, 1, _graphicsPipeline.Graphics!.VertexFormat == -1 ? 1u : 0u);
        if (drawCount == 0) return;
        if (step == commandSize)
        { if (useIndices) SDL.DrawGPUIndexedPrimitivesIndirect(_drawPass, entry.Handle!.DangerousGetHandle(), offset, drawCount); else SDL.DrawGPUPrimitivesIndirect(_drawPass, entry.Handle!.DangerousGetHandle(), offset, drawCount); }
        else for (uint i = 0; i < drawCount; i++)
            { if (useIndices) SDL.DrawGPUIndexedPrimitivesIndirect(_drawPass, entry.Handle!.DangerousGetHandle(), offset + i * step, 1); else SDL.DrawGPUPrimitivesIndirect(_drawPass, entry.Handle!.DangerousGetHandle(), offset + i * step, 1); }
    }
    /// <summary>Binds copied buffer identities and offsets directly for a vertex layout.</summary>
    /// <param name="drawList">The active draw list.</param>
    /// <param name="vertexFormat">A device-local vertex layout.</param>
    /// <param name="vertexCount">Available vertex count.</param>
    /// <param name="vertexBuffers">One live buffer per layout binding.</param>
    /// <param name="offsets">Optional byte offsets copied before returning.</param>
    /// <remarks>This convenience binding copies arrays. Use a prepared VertexArray for allocation-sensitive draws.</remarks>
    public void DrawListBindVertexBuffersFormat(long drawList, int vertexFormat, uint vertexCount, ReadOnlySpan<RID> vertexBuffers, ReadOnlySpan<uint> offsets = default)
    {
        DrawList(drawList); var layout = VertexFormat(vertexFormat);
        if (vertexCount == 0 || vertexBuffers.Length != layout.Buffers.Length || !offsets.IsEmpty && offsets.Length != vertexBuffers.Length) throw new ArgumentException("Vertex dimensions do not match the format.");
        var copiedOffsets = offsets.IsEmpty ? new uint[vertexBuffers.Length] : offsets.ToArray();
        for (var i = 0; i < vertexBuffers.Length; i++)
        {
            var buffer = Get(vertexBuffers[i], Kind.Vertex); var count = layout.Buffers[i].InputRate == SDL.GPUVertexInputRate.Instance ? 1 : vertexCount;
            if ((ulong)copiedOffsets[i] + (ulong)(count - 1) * layout.Buffers[i].Pitch + layout.Ends[i] > buffer.Size) throw new ArgumentException("The vertex range exceeds its buffer.");
        }
        _drawVertices = new(vertexCount, vertexFormat, vertexBuffers.ToArray(), copiedOffsets);
    }
    /// <summary>Ends the active draw list, preserving its results for subsequent device work.</summary>
    public void DrawListEnd()
    {
        DrawList(_activeList); SDL.EndGPURenderPass(_drawPass); _drawPass = 0; _activeList = 0; _drawTarget = null; _graphicsPipeline = null; _drawVertices = null; _drawIndices = null; _sets.Clear();
    }
}
