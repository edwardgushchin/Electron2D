using System.Text.Json;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private void TraceCCDInputs(int pairs, float delta)
    {
        if (pairs == 0) return;
        var prefix = $"bin/diagnostics/ccd-{CCDQueryCount:D4}";
        File.WriteAllText(prefix + "-settings.json", JsonSerializer.Serialize(new
        {
            pairs, shapes = _shapeHighWater, bodies = _highWater, delta,
            corrections = _hasPositionCorrections, historyCount = _oneWayHistoryCount,
            historyCapacity = OneWayHistoryTableSize, episodeMargin = _contactMargin,
            tolerance = CCDTolerance, degeneracy = 0.5f, axisTolerance = 0.00001f
        }));
        Copy(_bodies!, _highWater, sizeof(Body), "bodies");
        Copy(_shapesGPU!, _shapeHighWater, sizeof(ShapeData), "shapes");
        Copy(_geometryGPU!, _geometryEntries.Count, sizeof(GeometryData), "geometries");
        Copy(_verticesGPU!, _vertexHighWater, sizeof(Vector2), "vertices");
        Copy(_pairsGPU!, pairs, sizeof(PairData), "pairs");
        Copy(_centers!, _highWater, sizeof(Vector2), "centers");
        if (_hasPositionCorrections) Copy(_positionCorrectionsGPU!, _highWater, 16, "corrections");

        void Copy(RenderHandle source, int count, int stride, string name)
        {
            if (count == 0) return;
            var bytes = checked(count * stride);
            DownloadSpatial(source, 0, (uint)bytes);
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map diagnostic CCD inputs");
            try { File.WriteAllBytes(prefix + "-" + name + ".bin", new ReadOnlySpan<byte>((void*)mapped, bytes)); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        }
    }
}
