using System.Globalization;

namespace Electron2D;

public partial class Translation
{
    internal override void ValidateFileState()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_pluralSelector is not null) throw new NotSupportedException("Catalog archives cannot store a caller delegate; use automatic locale rules or restore the selector explicitly after loading.");
        }
    }
    private string[] CaptureMessages()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var result = new List<string>();
            foreach (var entry in _messages.OrderBy(item => item.Key.Context, StringComparer.Ordinal).ThenBy(item => item.Key.Source, StringComparer.Ordinal))
            {
                result.Add(entry.Key.Context); result.Add(entry.Key.Source); result.Add(entry.Value.Length.ToString(CultureInfo.InvariantCulture)); result.AddRange(entry.Value);
            }
            return result.ToArray();
        }
    }
    private void RestoreMessages(string[] data)
    {
        ArgumentNullException.ThrowIfNull(data); ThrowIfDisposed(); data = (string[])data.Clone();
        var prepared = new Dictionary<(string Context, string Source), string[]>();
        for (var offset = 0; offset < data.Length;)
        {
            if (data.Length - offset < 3 || data[offset] is null || data[offset + 1] is null ||
                !int.TryParse(data[offset + 2], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count <= 0 || count > data.Length - offset - 3)
                throw new InvalidDataException("Catalog message record is invalid.");
            var forms = data.AsSpan(offset + 3, count).ToArray();
            if (forms.Any(form => form is null) || !prepared.TryAdd((data[offset], data[offset + 1]), forms)) throw new InvalidDataException("Catalog message forms or duplicate identity are invalid.");
            offset += count + 3;
        }
        lock (_gate) { ThrowIfDisposed(); _messages.Clear(); foreach (var entry in prepared) _messages.Add(entry.Key, entry.Value); }
        EmitChanged();
    }
}
