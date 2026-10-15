using System.Globalization;

internal static class ProcessObservation
{
    internal static object Read(int pid, bool mappings = false)
    {
        var status = File.ReadAllText($"/proc/{pid}/status");
        return new
        {
            StatusBytes = Parse(status, "VmPeak", "VmSize", "VmHWM", "VmRSS", "VmSwap"),
            SmapsRollupBytes = Parse(File.ReadAllText($"/proc/{pid}/smaps_rollup"), "Rss", "Pss", "Anonymous", "Swap"),
            Threads = int.Parse(status.Split('\n').Single(line => line.StartsWith("Threads:")).Split(':')[1], CultureInfo.InvariantCulture),
            Mappings = mappings ? ParseMappings(File.ReadAllText($"/proc/{pid}/smaps")) : null
        };
    }

    private sealed record MappedPages(string Name, int Count, long RSSBytes);
    private static MappedPages[] ParseMappings(string text)
    {
        var result = new Dictionary<string, MappedPages>(); string? name = null;
        foreach (var line in text.Split('\n'))
        {
            var header = line.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
            if (header.Length >= 5 && header[0].Contains('-') && header[1].Length == 4)
            {
                if (name is not null) throw new InvalidDataException("A mapping has no RSS field.");
                name = header.Length == 6 ? header[5] : "[anonymous]";
            }
            else if (line.StartsWith("Rss:", StringComparison.Ordinal))
            {
                if (name is null) throw new InvalidDataException("Mapping RSS precedes its header or is repeated.");
                var bytes = Parse(line, "Rss")["Rss"];
                var prior = result.GetValueOrDefault(name) ?? new(name, 0, 0);
                result[name] = prior with { Count = checked(prior.Count + 1), RSSBytes = checked(prior.RSSBytes + bytes) };
                name = null;
            }
        }
        if (result.Count == 0 || name is not null) throw new InvalidDataException("Mapping RSS is absent.");
        return result.Values.OrderByDescending(row => row.RSSBytes).ThenBy(row => row.Name, StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, long> Parse(string text, params string[] required)
    {
        var bytes = new Dictionary<string, long>();
        foreach (var line in text.Split('\n'))
        {
            var pair = line.Split(':', 2); if (pair.Length != 2) continue;
            var value = pair[1].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (value.Length != 2 || value[1] != "kB") continue;
            if (!long.TryParse(value[0], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || !bytes.TryAdd(pair[0], checked(count * 1024)))
                throw new InvalidDataException("Invalid or duplicate procfs memory field: " + line);
        }
        if (required.Any(key => !bytes.ContainsKey(key))) throw new InvalidDataException("Required procfs memory field is absent.");
        return bytes;
    }

    internal static void SelfTest()
    {
        var values = Parse("Rss: 2 kB\nPss: 1 kB\nThreads: 3\n", "Rss", "Pss");
        if (values.Count != 2 || values["Rss"] != 2048 || values["Pss"] != 1024) throw new InvalidDataException("procfs unit calibration failed.");
        foreach (var value in new[] { "-1 kB", "abc kB", "1 MB", "9223372036854775807 kB", "1 kB\nRss: 2 kB" })
        {
            var rejected = false;
            try { Parse("Rss: " + value, "Rss"); }
            catch (Exception error) when (error is InvalidDataException or OverflowException) { rejected = true; }
            if (!rejected) throw new InvalidDataException("Invalid procfs input was accepted.");
        }
        _ = Read(Environment.ProcessId);
        var maps = ParseMappings("1000-2000 rw-p 00000000 00:00 0\nRss: 2 kB\n2000-3000 r--p 00000000 00:00 0 /path with spaces\nRss: 1 kB\n3000-4000 rw-p 00000000 00:00 0\nRss: 3 kB\n");
        if (maps.Length != 2 || maps[0] != new MappedPages("[anonymous]", 2, 5120) || maps[1].Name != "/path with spaces") throw new InvalidDataException("Mapping grouping calibration failed.");
        foreach (var invalid in new[] { "Rss: 1 kB\n", "1000-2000 rw-p 00000000 00:00 0\n", "1000-2000 rw-p 00000000 00:00 0\n2000-3000 rw-p 00000000 00:00 0\nRss: 1 kB\n", "1000-2000 rw-p 00000000 00:00 0\nRss: 1 kB\nRss: 1 kB\n" })
        {
            var rejected = false;
            try { ParseMappings(invalid); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new InvalidDataException("Invalid mapping sequence was accepted.");
        }
        _ = Read(Environment.ProcessId, mappings: true);
        Console.WriteLine("Process memory: procfs units, missing/invalid/duplicate/overflow fields and live self-read passed.");
    }
}
