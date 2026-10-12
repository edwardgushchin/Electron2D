using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly byte[] _pushData = new byte[MaximumPushBytes];
    private RID _pushPipeline;
    private bool _pushSupplied;
    private void ResetPush() { _pushPipeline = default; _pushSupplied = false; }
    private void SelectPushPipeline(RID pipeline) { if (_pushPipeline != pipeline) { _pushPipeline = pipeline; _pushSupplied = false; } }
    private static uint PushSize(Entry shader) { uint size = 0; foreach (var stage in shader.Push) size = Math.Max(size, stage.Size); return size; }
    private void RequirePush(Entry shader) { if (shader.Push.Length != 0 && !_pushSupplied) throw new InvalidOperationException("Set push constants for the selected pipeline before drawing or dispatching."); }
    private void SetPush(Entry shader, ReadOnlySpan<byte> buffer, uint sizeBytes)
    {
        var size = PushSize(shader);
        if (sizeBytes != size || sizeBytes > buffer.Length) throw new ArgumentException("Push byte count must match the selected shader block and fit the source buffer.", nameof(sizeBytes));
        var aligned = (int)((size + 15) & ~15u); _pushData.AsSpan(0, aligned).Clear(); buffer[..(int)size].CopyTo(_pushData); _pushSupplied = true;
    }
    private void PushUniforms(Entry shader)
    {
        foreach (var stage in shader.Push)
        {
            var bytes = _pushData.AsSpan(0, (int)((stage.Size + 15) & ~15u));
            if (stage.Stage == ShaderStage.Compute) SDL.PushGPUComputeUniformData(_command, (uint)stage.Native, bytes, (uint)bytes.Length);
            else if (stage.Stage == ShaderStage.Vertex) SDL.PushGPUVertexUniformData(_command, (uint)stage.Native, bytes, (uint)bytes.Length);
            else SDL.PushGPUFragmentUniformData(_command, (uint)stage.Native, bytes, (uint)bytes.Length);
        }
    }
    /// <summary>Copies binary push data for subsequent dispatches in the selected compute pipeline.</summary>
    /// <param name="computeList">The active owner-thread compute list.</param>
    /// <param name="buffer">At least sizeBytes source bytes; the selected prefix is copied before returning.</param>
    /// <param name="sizeBytes">Exact size of the shader's reflected push block, or zero for a shader without one.</param>
    /// <remarks>The original SPIR-V member offsets and array/matrix strides determine the byte layout. Native uniform translation preserves those bytes. Blocks are limited to 128 bytes and the executing 32-bit scalar shader profile. Bind a pipeline before setting data; changing pipeline or beginning another list requires another set. Recorded dispatches retain their own snapshot.</remarks>
    /// <exception cref="ArgumentException">The list, byte count or source capacity is invalid.</exception>
    /// <exception cref="InvalidOperationException">No pipeline is selected or the creating thread/device state is invalid.</exception>
    public void ComputeListSetPushConstant(long computeList, ReadOnlySpan<byte> buffer, uint sizeBytes)
    { List(computeList); if (_pipeline is null) throw new InvalidOperationException("Bind a compute pipeline first."); SetPush(_pipeline, buffer, sizeBytes); }
    /// <summary>Copies shared binary push data for subsequent vertex and fragment work in the selected graphics pipeline.</summary>
    /// <param name="drawList">The active owner-thread draw list.</param>
    /// <param name="buffer">At least sizeBytes source bytes; the selected prefix is copied before returning.</param>
    /// <param name="sizeBytes">Maximum reflected push-block extent of the paired stages, or zero when neither has a block.</param>
    /// <remarks>Both stages consume the same source bytes through their own original SPIR-V layouts, within the 128-byte portable limit. Data remains valid until pipeline changes or the list ends; previously recorded draws retain their own snapshot.</remarks>
    /// <exception cref="ArgumentException">The list, byte count or source capacity is invalid.</exception>
    /// <exception cref="InvalidOperationException">No pipeline is selected or the creating thread/device state is invalid.</exception>
    public void DrawListSetPushConstant(long drawList, ReadOnlySpan<byte> buffer, uint sizeBytes)
    { DrawList(drawList); if (_graphicsPipeline is null) throw new InvalidOperationException("Bind a graphics pipeline first."); SetPush(Get(_graphicsPipeline.Shader, Kind.Shader), buffer, sizeBytes); }
}
