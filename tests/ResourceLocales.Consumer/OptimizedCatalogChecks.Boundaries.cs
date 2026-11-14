using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Electron2D;
using IOFile = System.IO.File;

internal static partial class OptimizedCatalogChecks
{
    private const int MiB = 1024 * 1024;
    private const int ExpandedBudget = 32 * MiB;
    private static readonly string FullValue = new('é', MiB / 2);

    private static void VerifyInheritedBehavior(Translation catalog)
    {
        catalog.AddMessage("hidden-base-key", "hidden-base-value");
        catalog.AddMessage(Caption, "shadowed base value");
        catalog.AddMessage("context", "hidden context value", "menu");
        catalog.AddPluralMessage("plural", ["shadowed first", "shadowed second"]);
        Check(catalog.GetMessageCount() == 0 && catalog.GetMessageList().Length == 0,
            "Inherited source enumeration stays empty after AddMessage and AddPluralMessage.");
        Check(catalog.GetMessage("hidden-base-key") == "" && catalog.GetMessage(Caption, "another context") == "MMMMM"
            && catalog.GetMessage("context", "menu") == "", "Optimized singular lookup ignores inherited storage and context.");
        foreach (var count in new[] { long.MinValue, -1L, 0L, 1L, 2L, long.MaxValue })
        {
            foreach (var context in new[] { "", "menu" })
                Check(catalog.GetPluralMessage("plural", "different source plural", count, context) == "primary"
                    && catalog.GetPluralMessage("missing", "plural", count, context) == "",
                    "Optimized plural lookup uses the first form by singular key for every count and context.");
        }
        Console.WriteLine("Inherited API: AddMessage/AddPluralMessage, hidden source enumeration and 12 count/context combinations passed.");
    }

    private static void VerifyGenerationBudget()
    {
        using var source = new Translation { Locale = "fr-CA" };
        using var optimized = new OptimizedTranslation();
        for (var i = 0; i < ExpandedBudget / MiB; i++) source.AddMessage("budget-" + i, FullValue);
        Check(optimized.Generate(source) && optimized.Locale == "fr-CA" && optimized.GetMessage("budget-31") == FullValue,
            "Generation accepts exactly 32 MiB of expanded UTF-8.");
        var changes = 0;
        optimized.Changed += _ => changes++;
        source.Locale = "en-US";
        source.AddMessage("one-byte-over", "x");
        Reject<InvalidDataException>(() => optimized.Generate(source));
        Check(changes == 0 && optimized.Locale == "fr-CA" && optimized.GetMessage("budget-31") == FullValue
            && optimized.GetMessage("one-byte-over") == "", "Generation at 32 MiB + 1 preserves the previous state.");
        Console.WriteLine("Generation: 33,554,432 UTF-8 bytes accepted, 33,554,433 refused.");
    }

    private static void VerifyArchiveBoundaries(string path, OptimizedTranslation cached)
    {
        var physical = ProjectSettings.GlobalizePath(path);
        var validFile = IOFile.ReadAllBytes(physical);
        var changes = 0;
        cached.Changed += Observe;
        try
        {
            IOFile.WriteAllBytes(physical, BuildArchive(validFile, null));
            foreach (var mode in new[] { ResourceLoader.CacheMode.Ignore, ResourceLoader.CacheMode.Replace })
            {
                var error = Reject<InvalidDataException>(() => ResourceLoader.Load<OptimizedTranslation>(path, mode));
                Check(error.Message == "Missing optimized catalog payload.", $"Required-field validation rejected {mode}.");
                VerifyCached();
            }

            IOFile.WriteAllBytes(physical, BuildArchive(validFile, BuildBudgetPayload(false)));
            using (var exact = ResourceLoader.Load<OptimizedTranslation>(path, ResourceLoader.CacheMode.Ignore))
                Check(!ReferenceEquals(exact, cached) && exact.Locale == "ja" && exact.GetMessage("budget-31") == FullValue,
                    "An independently encoded archive with exactly 32 MiB of expanded UTF-8 loads.");
            VerifyCached();

            IOFile.WriteAllBytes(physical, BuildArchive(validFile, BuildBudgetPayload(true)));
            foreach (var mode in new[] { ResourceLoader.CacheMode.Ignore, ResourceLoader.CacheMode.Replace })
            {
                Reject<InvalidDataException>(() => ResourceLoader.Load<OptimizedTranslation>(path, mode));
                VerifyCached();
            }
        }
        finally
        {
            cached.Changed -= Observe;
            IOFile.WriteAllBytes(physical, validFile);
        }
        Check(ReferenceEquals(cached, ResourceLoader.Load<OptimizedTranslation>(path, ResourceLoader.CacheMode.Replace)),
            "Restoring the valid archive permits replacement of the original identity.");
        Console.WriteLine("Independent archive: absent field and 33,554,433 expanded bytes refused in Ignore/Replace; 33,554,432 accepted; cached identity preserved.");

        void Observe(Resource _) => changes++;

        void VerifyCached()
        {
            Check(changes == 0 && ReferenceEquals(cached, ResourceLoader.GetCachedRef<OptimizedTranslation>(path))
                && cached.ResourceName == "" && !cached.ResourceLocalToScene && cached.ResourcePath == path,
                "Rejected candidate cannot replace or mutate cached identity or metadata.");
            Verify(cached, "fr-CA", "MMMMM");
        }
    }

    private static byte[] BuildBudgetPayload(bool overBudget)
    {
        var raw = Encoding.UTF8.GetBytes(FullValue);
        Check(raw.Length == MiB, "Each expanded value meets the per-entry limit.");
        using var buffer = new MemoryStream();
        using (var compressor = new BrotliStream(buffer, CompressionLevel.Fastest, leaveOpen: true)) compressor.Write(raw);
        var compressed = buffer.ToArray();
        Check(compressed.Length < raw.Length, "The stored archive remains small.");

        using var payload = new BinaryWriter(new MemoryStream());
        const int entries = ExpandedBudget / MiB;
        payload.Write(1);
        WriteUTF16(payload, "ja");
        payload.Write(entries + (overBudget ? 1 : 0));
        for (var i = 0; i < entries; i++) WriteEntry("budget-" + i, raw.Length, compressed);
        if (overBudget) WriteEntry("one-byte-over", 1, "x"u8);
        return ((MemoryStream)payload.BaseStream).ToArray();

        void WriteEntry(string key, int expanded, ReadOnlySpan<byte> stored)
        {
            payload.Write(SHA256.HashData(Encoding.Unicode.GetBytes(key)).AsSpan(0, 16));
            payload.Write(expanded);
            payload.Write(stored.Length);
            payload.Write(stored);
        }
    }

    private static byte[] BuildArchive(byte[] template, byte[]? catalog)
    {
        using var body = new BinaryWriter(new MemoryStream());
        body.Write(1); // One generic embedded root. Metadata differs from the cached resource.
        WriteUTF16(body, "OptimizedTranslation");
        body.Write((ushort)0); // Generic role and no missing-payload marker.
        WriteUTF16(body, "replacement");
        body.Write(true);
        WriteUTF16(body, "");
        WriteUTF16(body, "");
        body.Write(-1L);
        body.Write(-1);
        using var fields = new BinaryWriter(new MemoryStream());
        fields.Write(catalog is null ? 0 : 1);
        if (catalog is not null)
        {
            WriteUTF16(fields, "_optimized_catalog");
            WriteUTF16(fields, "System.Byte[]");
            fields.Write((ushort)0); // Scalar field with little-endian bytes.
            fields.Write(sizeof(int) + catalog.Length);
            fields.Write(catalog.Length);
            fields.Write(catalog);
        }
        var fieldBytes = ((MemoryStream)fields.BaseStream).ToArray();
        body.Write(fieldBytes.Length);
        body.Write(fieldBytes);
        var archiveBody = ((MemoryStream)body.BaseStream).ToArray();
        var file = new byte[64 + archiveBody.Length];
        template.AsSpan(0, 64).CopyTo(file);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(24), archiveBody.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(28), archiveBody.Length);
        SHA256.HashData(archiveBody).CopyTo(file, 32);
        archiveBody.CopyTo(file, 64);
        return file;
    }

    private static void WriteUTF16(BinaryWriter writer, string value)
    {
        writer.Write(value.Length);
        writer.Write(Encoding.Unicode.GetBytes(value));
    }
}
