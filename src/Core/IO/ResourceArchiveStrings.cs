namespace Electron2D;

internal static class ResourceArchiveStrings
{
    internal static void Write(StreamPeerBuffer stream, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > ResourceArchive.Limit / 2) throw new InvalidDataException("String exceeds archive budget.");
        stream.Put32(value.Length);
        foreach (var character in value) stream.PutU16(character);
    }
    internal static string Read(StreamPeerBuffer stream)
    {
        var count = stream.Get32();
        if (count < 0 || count > ResourceArchive.Limit / 2 || count > stream.GetAvailableBytes() / 2) throw new InvalidDataException("Invalid archive string length.");
        var characters = new char[count];
        for (var i = 0; i < count; i++) characters[i] = (char)stream.GetU16();
        return new string(characters);
    }
}
