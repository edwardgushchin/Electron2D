using System.Text.Json;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private readonly List<(RenderHandle Buffer, int BodyBytes, int CommandBytes, string Prefix)> _bodyStageReads = [];
    private int _bodyStageReadCount;

    private void TraceBodyStage(nint command, Settings settings)
    {
        if (_ccdBodyCount == 0 || _highWater > 16 || _bodyStageReadCount >= 24) return;
        var bodyBytes = checked(_highWater * sizeof(Body));
        var commandBytes = checked(_pendingCount * sizeof(Command));
        var target = Transfer(checked((uint)(bodyBytes + commandBytes)), SDL.GPUTransferBufferUsage.Download);
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin diagnostic body-stage readback");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _bodies!.DangerousGetHandle(), Size = (uint)bodyBytes }, new() { TransferBuffer = target.DangerousGetHandle() });
            if (commandBytes != 0)
                SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _commands!.DangerousGetHandle(), Size = (uint)commandBytes }, new() { TransferBuffer = target.DangerousGetHandle(), Offset = (uint)bodyBytes });
            SDL.EndGPUCopyPass(copy);
            var prefix = $"bin/diagnostics/body-stage-{_identity:D4}-{_bodyStageReadCount++:D4}";
            File.WriteAllText(prefix + ".json", JsonSerializer.Serialize(new
            {
                stage = settings.Stage, count = settings.Count, bodies = _highWater,
                pending = _pendingCount, ccdBodies = _ccdBodyCount,
                delta = settings.Step.Z, tick = settings.Padding, Driver, DeviceName
            }));
            _bodyStageReads.Add((target, bodyBytes, commandBytes, prefix));
        }
        catch { target.Dispose(); throw; }
    }

    private void CompleteBodyStageTrace()
    {
        try
        {
            foreach (var entry in _bodyStageReads)
            {
                var mapped = SDL.MapGPUTransferBuffer(Device, entry.Buffer.DangerousGetHandle(), false);
                if (mapped == 0) throw GPUPhysicsDevice.Failure("map diagnostic body-stage readback");
                try
                {
                    File.WriteAllBytes(entry.Prefix + "-bodies.bin", new ReadOnlySpan<byte>((void*)mapped, entry.BodyBytes));
                    if (entry.CommandBytes != 0)
                        File.WriteAllBytes(entry.Prefix + "-commands.bin", new ReadOnlySpan<byte>((byte*)mapped + entry.BodyBytes, entry.CommandBytes));
                }
                finally { SDL.UnmapGPUTransferBuffer(Device, entry.Buffer.DangerousGetHandle()); }
            }
        }
        finally
        {
            foreach (var entry in _bodyStageReads) entry.Buffer.Dispose();
            _bodyStageReads.Clear();
        }
    }

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
        Copy(_commands!, _highWater, sizeof(Command), "commands");
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
