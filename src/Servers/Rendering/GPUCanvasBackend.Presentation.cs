using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    internal override void Present(ReadOnlySpan<CanvasPresentation> viewports)
        => PresentToWindow(_window, PreparePresentation(), viewports, root: true);
    private void PresentToWindow(nint window, CanvasRenderTarget presentation, ReadOnlySpan<CanvasPresentation> viewports, bool root)
    {
        _presentationPipeline ??= CreatePipeline(BuiltInShaders.ColorFragment, BlendMode.Mix, overwrite: true);
        _ = Sampler(TextureFilter.NearestWithMipmaps, TextureRepeat.Disabled);
        var bytes = checked(viewports.Length * 6 * sizeof(CanvasVertex)); EnsureBuffers(bytes);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire presentation command buffer");
        nint pass = 0; var acquired = false;
        try
        {
            if (bytes != 0)
            {
                var mapped = SDL.MapGPUTransferBuffer(Device, _transfer!.DangerousGetHandle(), true);
                if (mapped == 0) throw Failure("map presentation vertices");
                try
                {
                    var vertices = new Span<CanvasVertex>((void*)mapped, viewports.Length * 6);
                    for (var i = 0; i < viewports.Length; i++)
                    {
                        var quad = vertices.Slice(i * 6, 6);
                        if (!PresentationRegion(viewports[i].Rect, presentation.Size, out var rect, out var uv)) { quad.Clear(); continue; }
                        rect = new(rect.Position / presentation.Size, rect.Size / presentation.Size);
                        var a = new CanvasVertex(rect.Position, Colors.White, uv.Position);
                        var b = new CanvasVertex(new(rect.End.X, rect.Position.Y), Colors.White, new(uv.End.X, uv.Position.Y));
                        var c = new CanvasVertex(rect.End, Colors.White, uv.End);
                        var d = new CanvasVertex(new(rect.Position.X, rect.End.Y), Colors.White, new(uv.Position.X, uv.End.Y));
                        quad[0] = a; quad[1] = b; quad[2] = c; quad[3] = a; quad[4] = c; quad[5] = d;
                    }
                }
                finally { SDL.UnmapGPUTransferBuffer(Device, _transfer.DangerousGetHandle()); }
                var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin presentation upload");
                SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _transfer.DangerousGetHandle() },
                    new SDL.GPUBufferRegion { Buffer = _vertexBuffer!.DangerousGetHandle(), Size = (uint)bytes }, true);
                SDL.EndGPUCopyPass(copy);
            }
            pass = BeginPass(command, presentation.Next.DangerousGetHandle(), SDL.GPULoadOp.Clear, Colors.Black);
            for (var i = 0; i < viewports.Length; i++)
                if (PresentationRegion(viewports[i].Rect, presentation.Size, out _, out _))
                    BindColorCopy(command, pass, _presentationPipeline, viewports[i].Target.Current.DangerousGetHandle(), new(0, 0, viewports[i].Target.HDR ? 1 : 0, viewports[i].Target.HDR && viewports[i].Debanding ? 1 : 0), 0, i * 6);
            SDL.EndGPURenderPass(pass); pass = 0;
            Check(SDL.WaitAndAcquireGPUSwapchainTexture(command, window, out var swapchain, out var width, out var height), "acquire swapchain texture");
            if (swapchain != 0)
            {
                acquired = true;
                if (root) _hasPresented = true;
                var blit = new SDL.GPUBlitInfo { Source = new() { Texture = presentation.Next.DangerousGetHandle(), W = (uint)presentation.Size.X, H = (uint)presentation.Size.Y }, Destination = new() { Texture = swapchain, W = width, H = height }, LoadOp = SDL.GPULoadOp.DontCare, Filter = SDL.GPUFilter.Nearest };
                SDL.BlitGPUTexture(command, in blit);
            }
            var submitted = command; command = 0; Check(SDL.SubmitGPUCommandBuffer(submitted), "submit presentation");
            presentation.Commit();
        }
        finally
        {
            if (pass != 0) SDL.EndGPURenderPass(pass);
            if (command != 0) { if (acquired) SDL.SubmitGPUCommandBuffer(command); else SDL.CancelGPUCommandBuffer(command); }
        }
    }
}
