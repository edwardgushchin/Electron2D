namespace Electron2D;

internal sealed class MovieRecording : IDisposable
{
    private readonly MovieWriter _writer;
    private readonly Vector2i _size;
    private readonly int _fps, _rate, _channels;
    private readonly int[] _pcm;
    private int _remainder;
    private bool _begun, _disposed;
    internal MovieRecording(Vector2i size, int fps, string path)
    {
        _writer = MovieWriter.Acquire(path); _size = size; _fps = fps;
        try
        {
            (_rate, _channels) = _writer.GetAudioFormat();
            _pcm = new int[checked((int)Math.Ceiling(_rate / (double)_fps) * _channels)];
            AudioServer.Service.PrepareMovie(_rate, _channels);
            _begun = true; _writer.Begin(size, fps, path);
        }
        catch (Exception error)
        {
            try { Dispose(); } catch (Exception cleanup) { throw new AggregateException("Movie preparation and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    internal void Write(Window window)
    {
        using var captured = window.GetTexture().GetImage() ?? throw new InvalidOperationException("Movie viewport has no completed image.");
        Image? cropped = null;
        try
        {
            var image = captured;
            if (image.Size != _size)
            {
                var ratio = _size.X / (double)_size.Y;
                var width = image.Width; var height = image.Height;
                if (width / (double)height > ratio) width = Math.Max(1, (int)(height * ratio));
                else height = Math.Max(1, (int)(width / ratio));
                cropped = image.GetRegion(new Rect2i((image.Width - width) / 2, (image.Height - height) / 2, width, height));
                image = cropped; image.Resize(_size.X, _size.Y);
            }
            image.Convert(Image.Format.Rgb8);
            var frames = Math.DivRem((long)_remainder + _rate, _fps, out var rest); _remainder = (int)rest;
            var pcm = _pcm.AsSpan(0, checked((int)frames * _channels));
            AudioServer.Service.ReadMoviePCM(pcm); AudioServer.ReportEffectErrors();
            _writer.Write(image, pcm);
        }
        finally { cropped?.Dispose(); }
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        try { if (_begun) _writer.End(); } finally { _writer.Release(); }
    }
}
