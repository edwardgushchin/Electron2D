using System.Buffers.Binary;
using System.Security.Cryptography;
using Electron2D;

internal static partial class ScriptTests
{
    private static void ArchiveSourceEdges(string path, ScriptPaintEffect cached, string sourcePath)
    {
        var original = File.ReadAllBytes(path);
        var definitions = ResourceArchive.Inspect(path, out var endian);
        var source = cached.GetScript();
        try
        {
            foreach (var reference in new[] { -2, 0, definitions.Length })
            {
                var malformed = (ArchiveDefinition[])definitions.Clone();
                malformed[0] = malformed[0] with { Script = reference };
                WriteDefinitions(path, malformed, endian, original, 9);
                Reject<InvalidDataException>(() => ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.ReplaceDeep));
                Check(!cached.IsDisposed && cached.GetScript() == source && source is { IsDisposed: false }, "invalid source references preserve the cached owner and its borrowed source");
            }
            var wrongType = (ArchiveDefinition[])definitions.Clone();
            wrongType[0] = wrongType[0] with { Type = ResourceFileTypes.Get(typeof(Image)) };
            WriteDefinitions(path, wrongType, endian, original, 9);
            Reject<InvalidDataException>(() => ResourceLoader.Load<Resource>(path, ResourceLoader.CacheMode.IgnoreDeep));
        }
        finally { File.WriteAllBytes(path, original); }

        var legacyPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "legacy-script.e2dres");
        var legacy = ResourceArchive.Inspect(sourcePath, out var legacyEndian);
        WriteDefinitions(legacyPath, legacy, legacyEndian, File.ReadAllBytes(sourcePath), 8);
        using var restored = ResourceLoader.Load<Script>(legacyPath, ResourceLoader.CacheMode.IgnoreDeep);
        using var actor = restored.New<ScriptActor>();
        Check(actor.GetScript() == restored && actor.ConstructorScript == restored, "version-eight source assets retain their original schema and construct associated owners");
        Console.WriteLine("Archive source references, failed replacement preservation and version-eight decoding passed.");
    }

    private static void WriteDefinitions(string path, ArchiveDefinition[] definitions, bool endian, byte[] header, int version)
    {
        using var output = new StreamPeerBuffer { BigEndian = endian };
        output.Put32(definitions.Length);
        foreach (var definition in definitions)
        {
            ResourceArchiveStrings.Write(output, definition.Type.ID); ResourceArchive.WriteRole(output, definition, version);
            ResourceArchiveStrings.Write(output, definition.Name); output.PutU8(definition.Local ? (byte)1 : (byte)0);
            ResourceArchiveStrings.Write(output, definition.SceneID); ResourceArchiveStrings.Write(output, definition.Path);
            output.Put64(definition.UID); if (version >= 9) output.Put32(definition.Script);
            output.Put32(definition.Payload.Length); output.PutData(definition.Payload);
        }
        var payload = output.DataArray;
        var file = new byte[64 + payload.Length];
        header.AsSpan(0, 64).CopyTo(file); payload.CopyTo(file, 64);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(8), version);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(12), endian ? (int)SaverFlags.SaveBigEndian : 0);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), payload.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), payload.Length);
        SHA256.HashData(payload).CopyTo(file, 32); File.WriteAllBytes(path, file);
    }
}
