using Electron2D;
using CanvasShape = Electron2D.RenderingServer.TextureType;
using ResourceKind = Electron2D.RenderingDevice.DriverResource;

internal static unsafe partial class VulkanTextureImportTests
{
    private static void Server(nint library)
    {
        const uint rgba16Float = 97; // VK_FORMAT_R16G16B16A16_SFLOAT
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(48, 20), Title = "Vulkan server texture import" };
        using var output = root.GetTexture();
        using var source = Image.CreateEmpty((int)Width, (int)Height, false, Image.Format.Rgba8);
        source.Fill(Colors.Yellow);
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        white.Fill(Colors.White);
        using var detail = ImageTexture.CreateFromImage(white);
        using var shader = TextureArrayTests.Load("ArrayGLSL");
        var complete = false;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            var context = GetContext(device);
            ProducerImage ordinary = default, array = default, floatImage = default;
            var owned = new List<RID>();
            try
            {
                ordinary = Create(library, context, 1, levels: 1);
                array = Create(library, context, 2, levels: 1);
                floatImage = Create(library, context, 1, levels: 1, format: rgba16Float);
                Reject<ArgumentOutOfRangeException>(() => ImportServer(ordinary.Image, layers: 2));
                Reject<ArgumentOutOfRangeException>(() => ImportServer(array.Image, CanvasShape.Layered, layers: 0));
                Reject<ArgumentOutOfRangeException>(() => ImportServer(ordinary.Image, (CanvasShape)2));
                Reject<NotSupportedException>(() => ImportServer(ordinary.Image, format: Image.Format.Rgb8));
                Reject<InvalidOperationException>(() => Task.Run(() => ImportServer(ordinary.Image)).GetAwaiter().GetResult());
                device.ComputeListBegin();
                try { Reject<InvalidOperationException>(() => ImportServer(ordinary.Image)); }
                finally { device.ComputeListEnd(); }

                var imageRID = Track(ImportServer(ordinary.Image));
                var layersRID = Track(ImportServer(array.Image, CanvasShape.Layered, 2));
                var floatRID = Track(ImportServer(floatImage.Image, CanvasShape.Layered, format: Image.Format.Rgbah));
                using (var half = RenderingServer.Texture2DLayerGet(floatRID, 0))
                    Check(half is not null && half.PixelFormat == Image.Format.Rgbah && half.GetPixel(0, 0) == Colors.Red, "One-layer arrays retain their shape and half-float encoding.");
                Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DLayerGet(floatRID, 1));
                Release(floatRID);
                var imageProxy = Track(RenderingServer.TextureProxyCreate(imageRID));
                var arrayProxy = Track(RenderingServer.TextureProxyCreate(layersRID));
                var imageDevice = RenderingServer.TextureGetRDTexture(imageProxy);
                var arrayDevice = RenderingServer.TextureGetRDTexture(arrayProxy);
                Check(RenderingServer.TextureGetNativeHandle(imageProxy) == ordinary.Image &&
                    RenderingServer.TextureGetNativeHandle(arrayProxy) == array.Image, "Both proxies expose their exact producer allocation.");
                Reject<ArgumentException>(() => ImportServer(ordinary.Image));
                Reject<ArgumentException>(() => Import(device, array.Image, 2, mipmaps: 1));
                Reject<InvalidOperationException>(() => device.FreeRID(imageDevice));
                Reject<InvalidOperationException>(() => device.FreeRID(arrayDevice));

                using var view = new RDTextureView();
                var imageView = device.TextureCreateShared(view, imageDevice);
                var arrayView = device.TextureCreateSharedFromSlice(view, arrayDevice, 1, 0);
                var dependent = Track(RenderingServer.TextureRDCreate(imageView));
                Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(imageRID, dependent));
                Check(RenderingServer.TextureGetRDTexture(dependent) == imageView, "Replacing an owner with its own view rejects before mutation.");
                Release(dependent);

                var canvas = Track(RenderingServer.CanvasCreate());
                var imageItem = Track(RenderingServer.CanvasItemCreate());
                var arrayItem = Track(RenderingServer.CanvasItemCreate());
                var shaderRID = Track(RenderingServer.ShaderCreate());
                var material = Track(RenderingServer.MaterialCreate());
                RenderingServer.ShaderSetSPIRV(shaderRID, shader.GetSPIRV());
                RenderingServer.ShaderSetDefaultTextureParameter(shaderRID, "detailMap", detail.GetRID());
                RenderingServer.MaterialSetShader(material, shaderRID);
                RenderingServer.MaterialSetParam(material, "layers", arrayProxy);
                RenderingServer.MaterialSetParam(material, "selectedLayer", 1f);
                RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
                RenderingServer.CanvasItemSetParent(imageItem, canvas);
                RenderingServer.CanvasItemSetParent(arrayItem, canvas);
                RenderingServer.CanvasItemSetDefaultTextureFilter(imageItem, TextureFilter.Nearest);
                RenderingServer.CanvasItemSetMaterial(arrayItem, material);
                RenderingServer.CanvasItemAddTextureRect(imageItem, new(0, 0, 20, 12), imageProxy);
                RenderingServer.CanvasItemAddRect(arrayItem, new(24, 0, 20, 12), Colors.White);

                Draw(Colors.Red, Colors.Blue);
                using (var layer = RenderingServer.Texture2DLayerGet(arrayProxy, 1))
                    Check(layer?.GetPixel(0, 0) == Colors.Blue, "Native array readback uses the requested layer.");
                using (var pixels = RenderingServer.Texture2DGet(imageProxy))
                    Check(pixels?.GetPixel(0, 0) == Colors.Red, "Ordinary native readback preserves the producer pixels.");
                RenderingServer.Texture2DUpdate(imageRID, source);
                RenderingServer.Texture2DUpdate(layersRID, source, 1);
                Draw(Colors.Yellow, Colors.Yellow);

                var imageDestination = Track(RenderingServer.Texture2DCreate(source));
                var arrayDestination = Track(RenderingServer.Texture2DLayeredCreate([source], TextureLayered.LayeredType.Array));
                device.ComputeListBegin();
                try
                {
                    Reject<InvalidOperationException>(() => RenderingServer.FreeRID(imageRID));
                    Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(arrayDestination, layersRID));
                }
                finally { device.ComputeListEnd(); }
                Check(device.TextureIsValid(imageDevice) && device.TextureIsValid(arrayDevice), "A rejected change preserves both roots.");
                Replace(imageDestination, imageRID);
                Replace(arrayDestination, layersRID);
                Check(RenderingServer.TextureGetRDTexture(imageProxy) == imageDevice &&
                    RenderingServer.TextureGetRDTexture(arrayProxy) == arrayDevice, "Both proxies follow transferred import ownership.");
                Reject<ArgumentException>(() => RenderingServer.TextureGetFormat(imageRID));
                Reject<ArgumentException>(() => RenderingServer.TextureGetFormat(layersRID));
                Reject<InvalidOperationException>(() => device.FreeRID(imageDevice));

                for (var i = 0; i < 24; i++) RenderingServer.ForceDraw(swapBuffers: false);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(swapBuffers: false);
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(allocated == 0, $"64 prepared native-import frames allocated {allocated} managed owner bytes.");

                Release(imageDestination);
                Check(!device.TextureIsValid(imageDevice) && !device.TextureIsValid(imageView), "Free retires the ordinary root and its shared view.");
                var replacement = Track(RenderingServer.Texture2DLayeredCreate([source], TextureLayered.LayeredType.Array));
                var callbackRan = false;
                device.TextureGetDataAsync(arrayDevice, 1, bytes =>
                {
                    Check(bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 0, "Retirement completes the submitted layer readback.");
                    Check(!device.TextureIsValid(arrayDevice) && RenderingServer.TextureGetFormat(arrayDestination) == Image.Format.Rgba8,
                        "The callback observes committed replacement and a retired root.");
                    Reject<ArgumentException>(() => RenderingServer.TextureGetFormat(replacement));
                    callbackRan = true;
                    throw new InvalidOperationException("Test readback callback failure.");
                });
                Reject<AggregateException>(() => RenderingServer.TextureReplace(arrayDestination, replacement));
                owned.Remove(replacement);
                Check(callbackRan && !device.TextureIsValid(arrayView), "The replaced array and its slice expire even if a callback fails.");
                using (var pixels = RenderingServer.Texture2DLayerGet(arrayProxy, 0))
                    Check(pixels?.GetPixel(0, 0) == Colors.Yellow, "The stable proxy reads its new CPU-backed array.");

                Clear(library, context, ordinary, 1, 1, levels: 1);
                Clear(library, context, array, 2, 1, levels: 1);
                var returnedImage = Track(ImportServer(ordinary.Image));
                var returnedArray = Track(ImportServer(array.Image, CanvasShape.Layered, 2));
                using (var pixels = RenderingServer.Texture2DGet(returnedImage))
                    Check(pixels?.GetPixel(0, 0) == Colors.Green, "The producer can update and return the same ordinary allocation.");
                using (var pixels = RenderingServer.Texture2DLayerGet(returnedArray, 1))
                    Check(pixels?.GetPixel(0, 0) == Colors.Yellow, "The producer can update and return the same array allocation.");
                Check(device.GetDriverResource(ResourceKind.Texture, RenderingServer.TextureGetRDTexture(returnedArray), 0) == array.Image,
                    "Reimport retains the original VkImage after native retirement.");
            }
            finally
            {
                for (var i = owned.Count - 1; i >= 0; i--) RenderingServer.FreeRID(owned[i]);
                Destroy(library, context, ref floatImage);
                Destroy(library, context, ref array);
                Destroy(library, context, ref ordinary);
            }
            complete = true;
            root.Tree!.Quit();

            RID Track(RID rid) { owned.Add(rid); return rid; }
            void Release(RID rid) { RenderingServer.FreeRID(rid); owned.Remove(rid); }
            void Replace(RID target, RID incoming) { RenderingServer.TextureReplace(target, incoming); owned.Remove(incoming); }
            void Draw(Color image, Color layer)
            {
                RenderingServer.ForceDraw();
                using var frame = output.GetImage();
                Check(frame is not null && frame.GetPixel(3, 3) == image && frame.GetPixel(27, 3) == layer,
                    "The canvas draws the imported ordinary image and the selected array layer.");
            }
        };

        Check(Engine.Run(root) == 0 && complete, "The direct Vulkan canvas import scene completed.");
    }

    private static RID ImportServer(ulong image, CanvasShape shape = CanvasShape.Texture, int layers = 1, Image.Format format = Image.Format.Rgba8) =>
        RenderingServer.TextureCreateFromNativeHandle(shape, format, image, (int)Width, (int)Height, 1, layers);
}
