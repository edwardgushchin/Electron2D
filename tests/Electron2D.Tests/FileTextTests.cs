using System.Diagnostics;
using System.Text;
using Electron2D;
using static TextureArrayTests;
using EngineFile = Electron2D.FileAccess;
using Path = System.IO.Path;

internal static class FileTextTests
{
    private const string Unicode = "é😀";
    private static byte[] Expected => Convert.FromHexString(
        "C3A9F09F98800A780D790A2CC3A92C22612C62222C2271222272222C22610D62222C22610D0A62222C22610A62222C0A0A06000000C3A9F09F988000000006C3A9F09F988000000000");

    internal static void Run()
    {
        var directory = Path.GetFullPath(Path.Combine("obj", "file-text-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "ordinary.bin");
            using (var file = EngineFile.Open(path, FileAccessModeFlags.Write))
                Write(file);
            var actual = File.ReadAllBytes(path);
            Check(actual.SequenceEqual(Expected), $"Exact text wire bytes (actual {Convert.ToHexString(actual)}).");
            using (var reader = Process.Start(ChildStartInfo(path)))
            {
                Check(reader is not null, "The fresh reader process started.");
                if (!reader!.WaitForExit(15000))
                {
                    reader.Kill();
                    throw new InvalidOperationException("The fresh FileAccess reader timed out.");
                }
                Check(reader.ExitCode == 0, "The fresh reader verified the file.");
            }

            LinesAndCSV(path);
            Errors(path);
            var priorRoot = ProjectSettings.ProjectRoot;
            var priorUser = ProjectSettings.UserDataRoot;
            var userRoot = Directory.CreateDirectory(Path.Combine(directory, "user")).FullName;
            ProjectSettings.ConfigurePaths(directory, userRoot);
            try
            {
                using (var file = EngineFile.Open("res://virtual.bin", FileAccessModeFlags.Write))
                    Write(file);
                Check(File.ReadAllBytes(Path.Combine(directory, "virtual.bin")).SequenceEqual(Expected), "Virtual text wire bytes.");
                using var virtualFile = EngineFile.Open("res://virtual.bin", FileAccessModeFlags.Read);
                Read(virtualFile);
                using (var file = EngineFile.Open("user://virtual.bin", FileAccessModeFlags.Write))
                    Write(file);
                Check(File.ReadAllBytes(Path.Combine(userRoot, "virtual.bin")).SequenceEqual(Expected), "User text wire bytes.");
                using var userFile = EngineFile.Open("user://virtual.bin", FileAccessModeFlags.Read);
                Read(userFile);
            }
            finally
            {
                ProjectSettings.ConfigurePaths(priorRoot, priorUser);
            }

            var transformed = Path.Combine(directory, "transformed.bin");
            using (var file = EngineFile.OpenCompressed(transformed, FileAccessModeFlags.Write, FileCompressionMode.Gzip))
                Write(file);
            using var decoded = EngineFile.OpenCompressed(transformed, FileAccessModeFlags.Read, FileCompressionMode.Gzip);
            Check(decoded.ReadBytes(Expected.Length).SequenceEqual(Expected), "Transformed text wire bytes.");
            decoded.Seek(0);
            Read(decoded);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        Console.WriteLine("FileAccess text: wire bytes, fresh process, ordinary, virtual, gzip, strict errors and stream state passed.");
    }

    internal static void Child(string path)
    {
        using var file = EngineFile.Open(path, FileAccessModeFlags.Read);
        Read(file);
        Console.WriteLine("Fresh public FileAccess reader passed.");
    }

    private static ProcessStartInfo ChildStartInfo(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(FileTextTests).Assembly.Location);
        start.ArgumentList.Add("--file-text-child");
        start.ArgumentList.Add(path);
        return start;
    }

    private static void Write(EngineFile file)
    {
        file.WriteString(Unicode);
        file.WriteString("");
        file.WriteLine("");
        file.WriteLine("x\ry");
        file.WriteCSVLine(["", "é", "a,b", "q\"r", "a\rb", "a\r\nb", "a\nb", ""]);
        file.WriteCSVLine([]);
        file.WritePascalString(Unicode);
        file.BigEndian = true;
        file.WritePascalString(Unicode);
        file.WritePascalString("");
    }

    private static void Read(EngineFile file)
    {
        Check(file.ReadLine() == Unicode && file.ReadLine() == "x" && file.ReadLine() == "y", "UTF-8 line values.");
        Check(file.ReadCSVLine().SequenceEqual(["", "é", "a,b", "q\"r", "a\nb", "a\nb", "a\nb", ""]) &&
              file.ReadCSVLine().SequenceEqual([""]), "Quoted values, normalized multiline and empty CSV record.");
        Check(file.ReadPascalString() == Unicode, "Little-endian Pascal string.");
        file.BigEndian = true;
        Check(file.ReadPascalString() == Unicode && file.ReadPascalString() == "" &&
              file.Position == Expected.Length && !file.EOFReached, "Big-endian Pascal strings and EOF boundary.");
        Check(file.ReadCSVLine().Length == 0 && file.EOFReached, "CSV exhaustion.");
    }

    private static void LinesAndCSV(string path)
    {
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes("\ufeffA\rB\r\nC\0D\r"));
        using (var file = EngineFile.Open(path, FileAccessModeFlags.Read))
        {
            file.Seek(2);
            Check(file.ReadAllText() == "\ufeffA\rB\r\nC\0D\r" &&
                  file.ReadAllText(true) == "\ufeffAB\nC\0D" && file.Position == 2 && !file.EOFReached,
                "Snapshot preserves BOM, NUL, cursor and EOF state.");
            file.Seek(0);
            foreach (var (line, position) in new[] { ("\ufeffA", 5), ("B", 8), ("C", 10), ("D", 12) })
                Check(file.ReadLine() == line && file.Position == position && !file.EOFReached, "Physical line framing.");
            Check(file.ReadLine() == "" && file.EOFReached && file.ReadAllText().Length == 10 &&
                  file.Position == 12 && file.EOFReached, "Snapshot preserves latched EOF.");
        }

        File.WriteAllBytes(path, Encoding.UTF8.GetBytes("\"A\0B\"§Z\r\nlast§"));
        using var csv = EngineFile.Open(path, FileAccessModeFlags.Read);
        Check(csv.ReadCSVLine('§').SequenceEqual(["A\nB", "Z"]) && csv.Position == 10 && !csv.EOFReached,
            "Quoted NUL, Unicode delimiter and CRLF.");
        Check(csv.ReadCSVLine('§').SequenceEqual(["last", ""]) && csv.Position == 16 && csv.EOFReached,
            "Unterminated physical line with a complete CSV record.");
    }

    private static void Errors(string path)
    {
        File.WriteAllBytes(path, [0xff, 0x0a, 0xc3, 0x0d, 0xa9, 0x0a]);
        using (var file = EngineFile.Open(path, FileAccessModeFlags.Read))
        {
            Reject<DecoderFallbackException>(() => file.ReadLine());
            Check(file.Position == 2 && !file.EOFReached, "Invalid line consumes only its framing.");
            Reject<DecoderFallbackException>(() => file.ReadAllText(true));
            Check(file.Position == 2 && !file.EOFReached, "Invalid snapshot restores cursor and preserves EOF.");
            Reject<InvalidOperationException>(() => file.WriteLine("read only"));
            file.Close();
            file.Close();
            Reject<InvalidOperationException>(() => file.ReadLine());
            file.Dispose();
            Reject<ObjectDisposedException>(() => file.ReadLine());
        }

        File.WriteAllBytes(path, [0xc3, 0x0d, 0xa9]);
        using (var file = EngineFile.Open(path, FileAccessModeFlags.Read))
            Reject<DecoderFallbackException>(() => file.ReadAllText(true));

        foreach (var (bytes, position) in new (byte[], int)[]
                 { ([1, 2], 2), ([0, 0, 0, 64, 0xc3, 0xa9], 6) })
        {
            File.WriteAllBytes(path, bytes);
            using var file = EngineFile.Open(path, FileAccessModeFlags.Read);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Reject<EndOfStreamException>(() => file.ReadPascalString());
            Check(GC.GetAllocatedBytesForCurrentThread() - before < 65536 && file.Position == position && file.EOFReached,
                "Truncated Pascal frame consumes available bytes with bounded allocation.");
        }

        File.WriteAllBytes(path, [0xff, 0xff, 0xff, 0xff, 1, 0, 0, 0, 0xff]);
        using (var file = EngineFile.Open(path, FileAccessModeFlags.Read))
        {
            Reject<IOException>(() => file.ReadPascalString());
            Check(file.Position == 4 && !file.EOFReached, "Unrepresentable Pascal length stops after the prefix.");
            Reject<DecoderFallbackException>(() => file.ReadPascalString());
            Check(file.Position == 9 && !file.EOFReached, "Invalid Pascal payload consumes its declared bytes.");
        }

        File.WriteAllBytes(path, "\"a\"junk\n\"unterminated"u8.ToArray());
        using (var file = EngineFile.Open(path, FileAccessModeFlags.Read))
        {
            Reject<FormatException>(() => file.ReadCSVLine());
            Check(file.Position == 8 && !file.EOFReached, "Misplaced CSV quote consumes the framed record.");
            Reject<FormatException>(() => file.ReadCSVLine());
            Check(file.EOFReached, "Unterminated quote ends at EOF.");
        }

        using var writer = EngineFile.Open(path, FileAccessModeFlags.Write);
        Reject<InvalidOperationException>(() => writer.ReadPascalString());
        Reject<ArgumentException>(() => writer.WriteCSVLine(["x"], '\0'));
        Reject<ArgumentException>(() => writer.WriteCSVLine(["x"], '\ud800'));
        Reject<EncoderFallbackException>(() => writer.WriteString("\ud800"));
        Reject<EncoderFallbackException>(() => writer.WriteLine("\ud800"));
        Reject<EncoderFallbackException>(() => writer.WriteCSVLine(["good", "\ud800"]));
        Reject<EncoderFallbackException>(() => writer.WritePascalString("\ud800"));
        Check(writer.Position == 0 && writer.Length == 0, "Typed write errors emit no bytes.");
    }
}
