using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private enum NativeNameKind : uint
    {
        Buffer = 1, Image, ImageView, Sampler, Shader, ComputeShader, ComputePipeline, GraphicsPipeline
    }

    /// <summary>Labels a live GPU resource for native graphics diagnostics.</summary>
    /// <param name="id">An owned resource identity on this device.</param>
    /// <param name="name">The UTF-8 debug name. An empty string clears the name.</param>
    /// <remarks>Labels apply to existing native objects, including aliases of a compute pipeline and both physical views of a viewport.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">The identity is invalid or the name contains a null character.</exception>
    /// <exception cref="NotSupportedException">The backend or resource does not expose a nameable native object.</exception>
    /// <exception cref="InvalidOperationException">The operation is off the owner thread or the driver rejects the name.</exception>
    public void SetResourceName(RID id, string name)
    {
        Access();
        ArgumentNullException.ThrowIfNull(name);
        if (name.Contains('\0'))
            throw new ArgumentException("A debug name cannot contain null characters.", nameof(name));
        if (!_entries.TryGetValue(id, out var entry) || !entry.Alive)
            throw new ArgumentException("The RID is not a live resource of this device.", nameof(id));

        var kind = entry.Kind switch
        {
            Kind.Storage or Kind.Texel or Kind.Uniform or Kind.Vertex or Kind.Index => NativeNameKind.Buffer,
            Kind.Texture when entry.Borrowed is null => entry.TextureSource is null ? NativeNameKind.Image : NativeNameKind.ImageView,
            Kind.Sampler => NativeNameKind.Sampler,
            Kind.Shader when entry.Stage == ShaderStage.Compute => NativeNameKind.ComputeShader,
            Kind.Shader => NativeNameKind.Shader,
            Kind.Pipeline => NativeNameKind.ComputePipeline,
            Kind.GraphicsPipeline => NativeNameKind.GraphicsPipeline,
            _ => throw new NotSupportedException("This resource has no independently nameable native object."),
        };
        var handle = entry.Kind == Kind.Pipeline ? PipelineProgram(entry).Handle : entry.TextureAllocation ?? entry.Handle;
        LabelResource(handle, kind, name);
        if (entry.SecondTextureView is { } second)
            LabelResource(second, kind, name);
        if (entry.FragmentHandle is { } fragment)
            LabelResource(fragment, NativeNameKind.Shader, name);
        foreach (var viewPipeline in entry.ViewPipelines)
            if (!ReferenceEquals(viewPipeline, handle))
                LabelResource(viewPipeline, NativeNameKind.GraphicsPipeline, name);
    }

    private void LabelResource(RenderHandle? handle, NativeNameKind kind, string name)
    {
        if (handle is null)
            throw new NotSupportedException("This resource has no instantiated native object.");
        int result;
        try { result = NameNativeResource(Device, 1, kind, handle.DangerousGetHandle(), name); }
        catch (EntryPointNotFoundException) { result = 0; }
        if (result == 0)
            throw new NotSupportedException("The active GPU does not expose resource debug labels.");
        if (result != 1)
            throw new InvalidOperationException("The native GPU rejected the resource debug label.");
    }

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_resource_name",
        StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NameNativeResource(nint device, uint abi, NativeNameKind kind, nint resource, string name);
}
