using System.Text.Json;

namespace Electron2D.Editor;

internal sealed class JSONLines(Stream input)
{
    private readonly byte[] _buffer = new byte[8192];
    private readonly MemoryStream _line = new();
    private int _start, _end;
    internal async Task<JsonElement?> Read(CancellationToken cancellation)
    {
        while (true)
        {
            if (_start == _end)
            {
                _start = 0; _end = await input.ReadAsync(_buffer, cancellation).ConfigureAwait(false);
                if (_end == 0)
                {
                    if (_line.Length != 0) throw new InvalidDataException("JSON-RPC messages require a terminating newline.");
                    return null;
                }
            }
            var newline = Array.IndexOf(_buffer, (byte)'\n', _start, _end - _start);
            var count = (newline < 0 ? _end : newline) - _start;
            if (_line.Length + count > OperationWire.Limit) throw new InvalidDataException("JSON-RPC message exceeds eight MiB.");
            _line.Write(_buffer, _start, count); _start += count + (newline < 0 ? 0 : 1);
            if (newline < 0) continue;
            if (_line.Length == 0) continue;
            try { using var document = JsonDocument.Parse(_line.GetBuffer().AsMemory(0, checked((int)_line.Length))); return document.RootElement.Clone(); }
            finally { _line.SetLength(0); }
        }
    }
    internal static async Task Write(Stream output, System.Text.Json.Nodes.JsonObject value, CancellationToken cancellation)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        if (bytes.Length > OperationWire.Limit) throw new InvalidDataException("JSON-RPC response exceeds eight MiB; use scoped paging.");
        await output.WriteAsync(bytes, cancellation).ConfigureAwait(false); await output.WriteAsync("\n"u8.ToArray(), cancellation).ConfigureAwait(false); await output.FlushAsync(cancellation).ConfigureAwait(false);
    }
}
