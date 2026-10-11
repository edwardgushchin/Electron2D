using Electron2D;
using static TextureArrayTests;

internal static class RenderingDeviceCanvasTests
{
    private sealed class Patch(Texture? texture = null, RID rid = default) : Entity
    {
        protected override void OnDraw()
        {
            if (rid.IsValid()) RenderingServer.CanvasItemAddTextureRect(GetCanvasItem(), new(0, 0, 16, 16), rid);
            else if (texture is not null) DrawTextureRect(texture, new(0, 0, 16, 16), false);
            else DrawRect(new(0, 0, 16, 16), Colors.White);
        }
    }
    private const RenderingDevice.TextureUsageBits Roles = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CPURead;
    internal static void Run()
    {
        Check(RenderingServer.GetRenderingDevice() is null, "No retained device outside a rendering session.");
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); using var layers = new TextureArrayRD();
        Check(texture.GetWidth() == 0 && texture.GetImage() is null && !texture.HasAlpha && layers.GetLayers() == 0, "Unbound metadata and snapshot.");
        Reject<ArgumentOutOfRangeException>(() => layers.GetLayerData(0));
        var root = new Window { Size = new(160, 32) };
        if (backend == "compatibility")
        {
            root.Ready += _ => { Check(RenderingServer.GetRenderingDevice() is null, "Compatibility exposes no compute device."); Reject<InvalidOperationException>(() => texture.TextureRDRID = RID.Allocate()); Reject<InvalidOperationException>(() => RenderingServer.TextureRDCreate(default)); root.Tree!.Quit(); };
            Engine.Run(root); Check(root.IsDisposed && RenderingServer.Service is null, "Compatibility session cleanup."); Console.WriteLine("Device canvas: unavailable renderer and unbound resource guards passed."); return;
        }
        using var glsl = Load("ArrayGLSL"); using var hlsl = Load("ArrayHLSL");
        using var material = new ShaderMaterial { Shader = glsl }; using var otherMaterial = new ShaderMaterial { Shader = hlsl };
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); white.Fill(Colors.White); using var ordinary = ImageTexture.CreateFromImage(white);
        glsl.SetDefaultTextureParameter("detailMap", ordinary); hlsl.SetDefaultTextureParameter("detailMap", ordinary);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 4, 4) };
        RenderingDevice device = null!; RID source = default, output = default, array = default, pipeline = default, sourceSet = default, targetSet = default, countSet = default, owned = default, arrayOwned = default, proxy = default, arrayProxy = default, innerProxy = default;
        var frames = 0; var callbacks = 0; long before = 0, allocated = 0;
        var snapshot = new byte[4 * 4 * 16]; Action completed = () => callbacks++;
        var released = false;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; Check(ReferenceEquals(device, RenderingServer.GetRenderingDevice()), "Retained device identity.");
            Reject<InvalidOperationException>(device.Submit); Reject<InvalidOperationException>(device.Sync); Reject<InvalidOperationException>(device.Dispose);
            RenderingServer.SetDefaultClearColor(Colors.Black);
            using var format = new RDTextureFormat { Width = 4, Height = 4, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = Roles }; using var view = new RDTextureView();
            source = device.TextureCreate(format, view);
            format.Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT; format.UsageBits = Roles | RenderingDevice.TextureUsageBits.Storage; output = device.TextureCreate(format, view); texture.TextureRDRID = output;
            Check(texture.GetWidth() == 4 && texture.PixelFormat == Image.Format.Rgbaf && !texture.HasMipmaps, "Device resource allocation metadata.");
            using (var local = RenderingServer.CreateLocalRenderingDevice()) { var localTexture = local.TextureCreate(format, view); Reject<ArgumentException>(() => texture.TextureRDRID = localTexture); Reject<ArgumentException>(() => RenderingServer.TextureRDCreate(localTexture)); }
            var count = device.StorageBufferCreate(4); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
            using var module = new RDShaderSPIRV(); using var stream = typeof(RenderingDeviceCanvasTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalTexture.comp.spv")!; using var bytes = new MemoryStream(); stream.CopyTo(bytes); module.BytecodeCompute = bytes.ToArray();
            var shader = device.ShaderCreateFromSPIRV(module); pipeline = device.ComputePipelineCreate(shader);
            using var sampled = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; sampled.AddID(sampler); sampled.AddID(source); sourceSet = device.UniformSetCreate([sampled], shader, 3);
            using var target = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; target.AddID(output); targetSet = device.UniformSetCreate([target], shader, 5);
            using var counter = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.StorageBuffer }; counter.AddID(count); countSet = device.UniformSetCreate([counter], shader, 9);
            format.Format = RenderingDevice.DataFormat.R8G8B8A8UNORM; format.UsageBits = Roles; format.ArrayLayers = 2; format.Mipmaps = 3; format.TextureType = RenderingDevice.TextureType.TextureArray;
            array = device.TextureCreate(format, view); device.TextureClear(array, Colors.Blue, 0, 3, 0, 1); device.TextureClear(array, Colors.Green, 0, 3, 1, 1); layers.TextureRDRID = array;
            Check(layers.GetWidth() == 4 && layers.GetLayers() == 2 && layers.HasMipmaps(), "Device array metadata.");
            Reject<ArgumentException>(() => texture.TextureRDRID = array); Reject<ArgumentException>(() => layers.TextureRDRID = output);
            owned = RenderingServer.TextureRDCreate(output); var replacing = RenderingServer.TextureRDCreate(output); innerProxy = RenderingServer.TextureProxyCreate(replacing); proxy = RenderingServer.TextureProxyCreate(innerProxy);
            RenderingServer.TextureSetPath(replacing, "device-source"); RenderingServer.TextureReplace(owned, replacing);
            Check(RenderingServer.TextureGetPath(owned) == "device-source" && RenderingServer.TextureGetRDTexture(proxy) == output, "Stable replacement redirects aliases and metadata."); Reject<ArgumentException>(() => RenderingServer.TextureGetRDTexture(replacing));
            arrayOwned = RenderingServer.TextureRDCreate(array); replacing = RenderingServer.TextureRDCreate(array); arrayProxy = RenderingServer.TextureProxyCreate(replacing); RenderingServer.TextureReplace(arrayOwned, replacing);
            Check(RenderingServer.TextureGetRDTexture(arrayProxy) == array, "Layered alias inverse device identity.");
            glsl.SetDefaultLayeredTextureParameter("layers", layers); otherMaterial.SetShaderLayeredParameter("layers", (TextureLayered)RenderingTextureRegistry.ResolveResource(arrayProxy));
            root.AddChild(new Patch(texture) { Name = "device", Position = new(4, 4) }); root.AddChild(new Patch(rid: proxy) { Name = "proxy", Position = new(36, 4) }); root.AddChild(new Patch(atlas) { Name = "atlas", Position = new(68, 4) });
            root.AddChild(new Patch { Name = "glsl", Position = new(100, 4), Material = material }); root.AddChild(new Patch { Name = "hlsl", Position = new(132, 4), Material = otherMaterial });
            RenderingServer.TextureSetSizeOverride(owned, 16, 8); Check(RenderingTextureRegistry.Resolve(owned).GetSize() == new Vector2(16, 8), "Device wrapper logical size override.");
            using (var actual = RenderingServer.Texture2DGet(owned)!) Check(actual.Width == 4 && actual.Height == 4, "Size override never changes borrowed image allocation/readback.");
            ColdChecks(device, texture, layers, format, view);
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePreDraw += () =>
            {
                if (released) return;
                device.TextureClear(source, frames % 2 == 0 ? new(0, .5f, .25f, 1) : new(.5f, .25f, 0, 1), 0, 1, 0, 1);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sourceSet, 3); device.ComputeListBindUniformSet(list, targetSet, 5); device.ComputeListBindUniformSet(list, countSet, 9); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
                material.SetShaderParameter("selectedLayer", (float)(frames % 2)); otherMaterial.SetShaderParameter("selectedLayer", (float)((frames + 1) % 2));
                device.TextureGetDataAsync(output, 0, snapshot.AsMemory(), completed);
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (!released && frames >= 24) allocated += bytes;
                if (released)
                {
                    using var cleared = root.GetTexture().GetImage()!; Pixel(cleared, 12, Colors.Black); Pixel(cleared, 44, Colors.Black); Pixel(cleared, 76, Colors.Black);
                    RenderingServer.FreeRID(proxy); RenderingServer.FreeRID(innerProxy); RenderingServer.FreeRID(owned); RenderingServer.FreeRID(arrayProxy); RenderingServer.FreeRID(arrayOwned); root.Tree!.Quit(); return;
                }
                Check(callbacks == frames + 1, "Renderer delivers queued snapshots before FramePostDraw.");
                if (frames < 3 || frames == 55)
                {
                    using var image = root.GetTexture().GetImage()!; var expected = frames % 2 == 0 ? new Color(.25f, .5f, .25f, 1) : new Color(.75f, .25f, 0, 1);
                    Pixel(image, 12, expected); Pixel(image, 44, expected); Pixel(image, 76, expected); Pixel(image, 108, frames % 2 == 0 ? Colors.Blue : Colors.Green); Pixel(image, 140, frames % 2 == 0 ? Colors.Green : Colors.Blue);
                    Check(Math.Abs(BitConverter.ToSingle(snapshot) - expected.R) < .003f, "Rendered canvas and queued compute snapshot agree.");
                    if (frames == 55 && Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_CANVAS_SNAPSHOT") is { } path) image.SavePNG(path);
                }
                frames++; if (frames != 56) return;
                Check(allocated == 0, $"Prepared compute/canvas frames allocated {allocated} managed bytes.");
                using (var image = texture.GetImage()!) Check(image.PixelFormat == Image.Format.Rgbaf && image.GetPixel(0, 0).R > .7f, "Explicit snapshot retains floating storage.");
                device.FreeRID(output); Check(!RenderingServer.TextureGetRDTexture(owned).IsValid() && texture.GetWidth() == 0 && texture.GetImage() is null, "Freeing borrowed pixels empties resource without dangling native access.");
                texture.TextureRDRID = default; device.FreeRID(array); Check(layers.GetLayers() == 0, "Array source free clears live metadata.");
                released = true;
            };
        };
        Engine.Run(root); Check(root.IsDisposed && device.IsDisposed && RenderingServer.GetRenderingDevice() is null && frames == 56, "Global device shutdown.");
        var reopened = new Window { Size = new(32, 32) }; var reopeningFrames = 0; var hostCallbacks = 0;
        reopened.Ready += _ =>
        {
            var next = RenderingServer.GetRenderingDevice()!; Check(!ReferenceEquals(next, device), "Reopening creates a new retained device."); Reject<ArgumentException>(() => texture.TextureRDRID = source);
            using var format = new RDTextureFormat { UsageBits = Roles }; using var view = new RDTextureView(); next.TextureCreate(format, view);
            var uniform = next.UniformBufferCreate(16, new byte[16]);
            RenderingServer.FramePostDraw += () =>
            {
                reopeningFrames++;
                if (reopeningFrames == 1) next.BufferGetDataAsync(uniform, _ => { hostCallbacks++; next.BufferGetDataAsync(uniform, _ => hostCallbacks++); });
                if (reopeningFrames == 2) Check(hostCallbacks == 1, "Host-only readback submits after a prior GPU fence; callback recording waits for next frame.");
                if (reopeningFrames == 3) { Check(hostCallbacks == 2, "Reentrant host request completes on the next frame."); reopened.Tree!.Quit(); }
            };
        }; Engine.Run(reopened);
        var failing = new Window { Size = new(32, 32) }; RenderingDevice failingDevice = null!; var delivered = false;
        failing.Ready += _ => { failingDevice = RenderingServer.GetRenderingDevice()!; var buffer = failingDevice.UniformBufferCreate(16); failingDevice.BufferGetDataAsync(buffer, _ => throw new ApplicationException("readback fixture")); failingDevice.BufferGetDataAsync(buffer, _ => delivered = true); };
        Reject<AggregateException>(() => Engine.Run(failing)); Check(delivered && failing.IsDisposed && failingDevice.IsDisposed && RenderingServer.Service is null, "Readback errors propagate after other callbacks and clean the renderer.");
        var openList = new Window { Size = new(32, 32) }; RenderingDevice abandoned = null!; openList.Ready += _ => { abandoned = RenderingServer.GetRenderingDevice()!; abandoned.ComputeListBegin(); };
        Reject<InvalidOperationException>(() => Engine.Run(openList)); Check(abandoned.IsDisposed && openList.IsDisposed && RenderingServer.Service is null, "Renderer failure cancels an unfinished compute list and releases the retained device.");
        Console.WriteLine($"Device canvas: compute to direct/atlas/proxy pixels, GLSL/HLSL arrays, snapshots, format conversion, ownership/lifecycle; 24 warm + 32 active frames, {allocated} owner-thread managed bytes.");
    }
    private static void ColdChecks(RenderingDevice device, TextureRD texture, TextureArrayRD array, RDTextureFormat format, RDTextureView view)
    {
        var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => texture.TextureRDRID = default); device.ComputeListEnd();
        var rejection = Task.Run(() => { try { texture.TextureRDRID = default; return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(); Check(rejection, "Binding reset obeys device owner thread.");
        var identity = texture.GetRID(); var original = texture.TextureRDRID;
        using var duplicate = (TextureRD)texture.Duplicate(true); Check(duplicate.TextureRDRID == texture.TextureRDRID, "Duplicate borrows the same device image.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "device-canvas-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(texture, path); using var loaded = ResourceLoader.Load<TextureRD>(path, ResourceLoader.CacheMode.Ignore); Check(!loaded.TextureRDRID.IsValid() && loaded.GetWidth() == 0, "Resource archive omits transient device identity."); }
        finally { System.IO.File.Delete(path); }
        using (var layer = array.GetLayerData(1)!) { Check(layer.HasMipmaps && layer.GetPixel(0, 0) == Colors.Green, "Array snapshot reads selected layer and all mips."); layer.Fill(Colors.Red); }
        using (var layer = array.GetLayerData(1)!) Check(layer.GetPixel(0, 0) == Colors.Green, "Readback images never mutate native source.");
        Reject<ArgumentOutOfRangeException>(() => array.GetLayerData(-1)); Reject<ArgumentOutOfRangeException>(() => array.GetLayerData(2));
        format.ArrayLayers = 1; format.TextureType = RenderingDevice.TextureType.Texture; format.Mipmaps = 3; format.Format = RenderingDevice.DataFormat.B8G8R8A8UNORM;
        var converted = device.TextureCreate(format, view); device.TextureClear(converted, new(.25f, .5f, .75f, 1), 0, 3, 0, 1); using var convertedTexture = new TextureRD { TextureRDRID = converted };
        texture.TextureRDRID = converted; Check(texture.GetRID() == identity, "Rebinding keeps the canvas resource identity."); texture.TextureRDRID = original;
        using (var image = convertedTexture.GetImage()!) Check(image.PixelFormat == Image.Format.Rgbaf && image.HasMipmaps && Math.Abs(image.GetPixel(0, 0).R - .25f) < .003f && Math.Abs(image.GetPixel(0, 0).B - .75f) < .003f, "Non-Image device format converts to floating RGBA with every mip."); device.FreeRID(converted);
        format.Format = RenderingDevice.DataFormat.R8G8B8A8SNORM; var signed = device.TextureCreate(format, view); device.TextureClear(signed, new(-.5f, .25f, .75f, 1), 0, 3, 0, 1); using var signedTexture = new TextureRD { TextureRDRID = signed };
        using (var image = signedTexture.GetImage()!) Check(image.HasMipmaps && Math.Abs(image.GetPixel(0, 0).R + .5f) < .005f, "Signed normalized readback preserves negative values through native conversion."); device.FreeRID(signed);
        format.Format = RenderingDevice.DataFormat.B8G8R8A8UNORM; format.ArrayLayers = 2; format.TextureType = RenderingDevice.TextureType.TextureArray;
        var convertedArray = device.TextureCreate(format, view); device.TextureClear(convertedArray, Colors.Red, 0, 3, 0, 1); device.TextureClear(convertedArray, Colors.Blue, 0, 3, 1, 1); using var arrayTexture = new TextureArrayRD { TextureRDRID = convertedArray };
        using (var image = arrayTexture.GetLayerData(1)!) Check(image.HasMipmaps && image.GetPixel(0, 0) == Colors.Blue, "Native conversion selects the requested array layer and its mip chain."); device.FreeRID(convertedArray);
        format.ArrayLayers = 1; format.TextureType = RenderingDevice.TextureType.Texture;
        format.Format = RenderingDevice.DataFormat.R8G8B8A8SRGB; var srgb = device.TextureCreate(format, view); using var srgbTexture = new TextureRD { TextureRDRID = srgb }; Check(RenderingServer.TextureGetRDTexture(srgbTexture.GetRID(), true) == srgb, "Existing sRGB storage needs no alternate view."); device.FreeRID(srgb);
        Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(texture.GetRID(), true));
        format.Format = RenderingDevice.DataFormat.R8G8B8A8UINT; var integer = device.TextureCreate(format, view); Reject<NotSupportedException>(() => texture.TextureRDRID = integer); device.FreeRID(integer);
        format.Format = RenderingDevice.DataFormat.R8G8B8A8UNORM; format.UsageBits = RenderingDevice.TextureUsageBits.Sampling; var unreadable = device.TextureCreate(format, view); using var unreadableTexture = new TextureRD { TextureRDRID = unreadable }; Check(unreadableTexture.GetImage() is null, "No readable role returns no snapshot."); device.FreeRID(unreadable);
    }
    private static void Pixel(Image image, int x, Color expected) { var actual = image.GetPixel(x, 12); Check(Math.Abs(actual.R - expected.R) < .03 && Math.Abs(actual.G - expected.G) < .03 && Math.Abs(actual.B - expected.B) < .03, $"Canvas pixel {x}: {actual} != {expected}."); }
}
