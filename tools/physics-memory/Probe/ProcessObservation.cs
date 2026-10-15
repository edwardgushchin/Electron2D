using System.Globalization;

internal static class ProcessObservation
{
    internal static object Read(int pid)
    {
        var status = File.ReadAllText($"/proc/{pid}/status");
        return new
        {
            StatusBytes = Parse(status, "VmPeak", "VmSize", "VmHWM", "VmRSS", "VmSwap"),
            SmapsRollupBytes = Parse(File.ReadAllText($"/proc/{pid}/smaps_rollup"), "Rss", "Pss", "Anonymous", "Swap"),
            Threads = int.Parse(status.Split('\n').Single(line => line.StartsWith("Threads:")).Split(':')[1], CultureInfo.InvariantCulture)
        };
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
        Console.WriteLine("Process memory: procfs units, missing/invalid/duplicate/overflow fields and live self-read passed.");
    }
}
