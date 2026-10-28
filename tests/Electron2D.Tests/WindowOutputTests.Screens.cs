using Electron2D;

internal static partial class WindowOutputTests
{
    private static void Screens()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        _rootNative = 0; _nativeHDR = true; _white = 2; _headroom = 4;
        GPUCanvasBackend.OutputForTesting = NativeMetadata;
        var window = new Window { Size = new(96, 64), HDROutputRequested = true };
        window.AddChild(new ColorRect { Size = new(96, 64), Color = Colors.Red });
        var frames = 0;
        try
        {
            window.Ready += _ =>
            {
                var device = RenderingServer.GetRenderingDevice()!;
                Check(device.HasFeature(RenderingDevice.Features.SupportsHDROutput), "Device HDR capability follows the integrated native main output.");
                var hdrFormat = device.ScreenGetFramebufferFormat();
                var list = device.DrawListBeginForScreen(clearColor: new(.25f, 1, 2.5f)); device.DrawListEnd();
                Check(list != RenderingDevice.InvalidID, "Real native screen draw list.");
                window.HDROutputRequested = false;
                Check(device.ScreenGetFramebufferFormat() != hdrFormat, "Native HDR output has a linear half-float public screen stage.");
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    using var pixels = RenderingServer.Service!.Readback(); var color = pixels.GetPixel(4, 4);
                    if (frames == 1)
                    {
                        Check(pixels.PixelFormat == Image.Format.Rgba8 && MathF.Abs(color.R - Encode(.25f)) < .006f && color.G > .99f && color.B > .99f,
                            $"Pending linear screen content transfers to SDR: {color}.");
                        device.DrawListBeginForScreen(clearColor: new(.5f, .25f, .125f)); device.DrawListEnd();
                        window.HDROutputRequested = true;
                        Check(device.ScreenGetFramebufferFormat() == hdrFormat, "The device layout identity is reused after native output restoration.");
                    }
                    else
                    {
                        Check(pixels.PixelFormat == Image.Format.Rgbah && MathF.Abs(color.R - Decode(.5f)) < .002f && MathF.Abs(color.G - Decode(.25f)) < .002f && MathF.Abs(color.B - Decode(.125f)) < .002f,
                            $"Pending encoded screen content transfers to HDR: {color}.");
                        window.Tree!.Quit();
                    }
                };
            };
            Check(Engine.Run(window) == 0 && frames == 2, "Native screen/output transition host.");
            Console.WriteLine("Pending native screen content preserves its source color space across HDR/SDR output changes, without a CPU image copy.");
        }
        finally { GPUCanvasBackend.OutputForTesting = null; if (!window.IsDisposed) window.Dispose(); }
    }

    private static float Encode(float v) => v <= .0031308f ? 12.92f * v : 1.055f * MathF.Pow(v, 1 / 2.4f) - .055f;
    private static float Decode(float v) => v <= .04045f ? v / 12.92f : MathF.Pow((v + .055f) / 1.055f, 2.4f);
}
