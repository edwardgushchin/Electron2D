using System.Globalization;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private struct BufferUploadStatistics
    {
        internal ulong GPU, Direct, Bytes;
    }

    private BufferUploadStatistics _frameUploads, _publishedUploads;
    private ulong _recordedBufferUploads, _recordedBufferBytes;

    /// <summary>Returns the buffer upload statistics of the last submitted frame.</summary>
    /// <returns>An invariant report with staged GPU uploads, direct host uniform writes, their total bytes, and lazily allocated bytes.</returns>
    /// <remarks>The renderer publishes after successful frame submission. Local devices publish on Submit.
    /// Reads and internal flushes do not advance the report. The current renderers allocate no lazy attachment memory.</remarks>
    /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller cannot access the device in the current thread or context.</exception>
    public string GetPerfReport()
    {
        Access();
        return string.Create(CultureInfo.InvariantCulture,
            $" gpu:{_publishedUploads.GPU} direct:{_publishedUploads.Direct} bytes:{_publishedUploads.Bytes} lazily alloc:0");
    }

    private void RecordBufferUpload(uint bytes)
    {
        _recordedBufferUploads++;
        _recordedBufferBytes += bytes;
    }

    private void RecordDirectBufferWrite(uint bytes)
    {
        if (bytes == 0) return;
        _frameUploads.Direct++;
        _frameUploads.Bytes += bytes;
    }

    private void FinishBufferUploads(bool submitted)
    {
        if (submitted)
        {
            _frameUploads.GPU += _recordedBufferUploads;
            _frameUploads.Bytes += _recordedBufferBytes;
        }
        _recordedBufferUploads = _recordedBufferBytes = 0;
    }

    internal void PublishPerfReport()
    {
        _publishedUploads = _frameUploads;
        _frameUploads = default;
    }
}
