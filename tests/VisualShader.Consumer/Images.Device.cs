using Electron2D;

internal static partial class GraphChecks
{
    private sealed partial class GraphScene
    {
        private RenderingDevice? _imageDevice;
        private readonly RID[] _imageDeviceRIDs = new RID[2];
        private TextureRD? _imageDeviceOrdinary;
        private TextureArrayRD? _imageDeviceArray;
        private readonly List<(GraphPainter Painter, string Name, Vector2 Position)> _deviceImageCases = [];
        private Resource ImageDeviceTexture(bool array)
        {
            if (_imageDevice is null)
            {
                _imageDevice = RenderingServer.GetRenderingDevice() ?? throw new InvalidOperationException("Image view checks require the retained rendering device.");
                using var format = new RDTextureFormat
                {
                    Width = 4,
                    Height = 4,
                    MIPMaps = 3,
                    Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
                    UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo
                };
                using var view = new RDTextureView();
                _imageDeviceRIDs[0] = _imageDevice.TextureCreate(format, view);
                format.ArrayLayers = 2; format.TextureType = RenderingDevice.TextureType.TextureArray;
                _imageDeviceRIDs[1] = _imageDevice.TextureCreate(format, view);
                _imageDeviceOrdinary = Own(new TextureRD { TextureRDRID = _imageDeviceRIDs[0] });
                _imageDeviceArray = Own(new TextureArrayRD { TextureRDRID = _imageDeviceRIDs[1] });
                ClearImageDeviceTextures(0);
            }
            return array ? _imageDeviceArray! : _imageDeviceOrdinary!;
        }
        private void ClearImageDeviceTextures(int state)
        {
            if (_imageDevice is null) return;
            for (var shape = 0; shape < 2; shape++) for (uint layer = 0; layer <= shape; layer++) for (uint mip = 0; mip < 3; mip++)
                        _imageDevice.TextureClear(_imageDeviceRIDs[shape], DeviceImageColor((int)layer, (int)mip, state), mip, 1, layer, 1);
        }
        private void ProcessDeviceImages()
        {
            if (_frame is 4 or 6 or 95 || _frame is > 30 and <= 94) ClearImageDeviceTextures(_frame is 6 or 95 || _frame > 30 && _frame % 2 == 0 ? 0 : 1);
        }
        private void CheckDeviceImageFrames()
        {
            if (_deviceImageCases.Count == 0 || _frame is not (5 or 7 or 94 or 174)) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _deviceImageCases)
            {
                var expected = DeviceImageExpected(item.Name, GetWindow()!.UseHDR2D, _frame == 5 ? 1 : 0);
                var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                Check(MathF.Abs(actual.R - expected.R) < .0021f && MathF.Abs(actual.G - expected.G) < .0021f && MathF.Abs(actual.B - expected.B) < .0021f,
                    $"Fresh native image {item.Name} frame {_frame}: actual {actual}, expected {expected}.");
            }
            Console.WriteLine($"{_deviceImageCases.Count} native image views refreshed after GPU clears at frame {_frame}.");
        }
        private void ReleaseDeviceImages()
        {
            if (_imageDevice is null) return;
            foreach (var rid in _imageDeviceRIDs) if (rid.IsValid()) _imageDevice.FreeRID(rid);
        }
    }
    private static Color DeviceImageColor(int layer, int mip, int state) => new((32 + 64 * state + 16 * mip) / 255f,
        (64 + 64 * layer + 16 * state) / 255f, (192 - 32 * mip - 16 * layer) / 255f, (224 - 32 * layer - 16 * mip - 32 * state) / 255f);
    private static Color DeviceImageExpected(string name, bool hdr, int state)
    {
        var p = name.Split('-'); var layer = int.Parse(p[3]); var mip = int.Parse(p[4]); var type = int.Parse(p[5]); var alpha = int.Parse(p[6]);
        var value = DeviceImageColor(layer, mip, state);
        return alpha != 0 ? new(value.A, value.A, value.A, 1) : type == 1 && hdr ? LinearImage(value) : value;
    }
}
