using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static class EngineMetadataChecks
{
    internal static string Run(string? sourceRoot = null)
    {
        Parallel.For(0, 32, _ =>
        {
            Require(Engine.GetLicenseText().Contains("2026 Eduard Gushchin", StringComparison.Ordinal), "Concurrent first read");
            Require(Engine.GetLicenseInfo().Count == 97, "Distribution document count");
        });
        var authors = Engine.GetAuthorInfo();
        Require(authors.Keys.SequenceEqual(new[] { "lead_developers", "founders", "project_managers", "developers" }), "Author categories");
        Require(authors["developers"].SequenceEqual(new[] { "Eduard Gushchin" }), "Package author identity");
        Require(authors.Where(pair => pair.Key != "developers").All(pair => pair.Value.Count == 0), "No invented author roles");
        var donors = Engine.GetDonorInfo();
        Require(donors.Keys.SequenceEqual(new[] { "platinum_sponsors", "gold_sponsors", "silver_sponsors", "bronze_sponsors", "mini_sponsors", "gold_donors", "silver_donors", "bronze_donors" }), "Donor categories");
        Require(donors.Values.All(value => value.Count == 0), "No invented donors");
        Reject(() => ((IDictionary<string, IReadOnlyList<string>>)authors).Clear());
        Reject(() => ((IList<string>)authors["developers"])[0] = "changed");
        Reject(() => ((IList<string>)authors["founders"]).Add("changed"));
        Reject(() => ((IDictionary<string, IReadOnlyList<string>>)donors).Clear());
        foreach (var names in donors.Values)
            Reject(() => ((IList<string>)names).Add("changed"));
        var licenses = Engine.GetLicenseInfo();
        Reject(() => ((IDictionary<string, string>)licenses).Clear());
        Require(!licenses.ContainsKey("ReferenceData-LICENSE.txt") && !licenses.ContainsKey("THIRD_PARTY_NOTICES.md"), "Non-runtime catalog exclusions");
        Require(Engine.GetLicenseText() == licenses["Electron2D-LICENSE.txt"], "Engine license consistency");
        var components = Engine.GetCopyrightInfo();
        Reject(() => ((IList<EngineCopyrightInfo>)components).Clear());
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in components)
        {
            Require(component.Name.Length > 0 && component.Parts.Count > 0, "Named nonempty components");
            Reject(() => ((IList<EngineCopyrightPart>)component.Parts).Clear());
            foreach (var part in component.Parts)
            {
                Require(seen.Add(part.License) && licenses.ContainsKey(part.License), "Unique resolvable license document");
                Require(part.Files.Count > 0, "Distribution source scope");
                Reject(() => ((IList<string>)part.Files).Clear());
                Reject(() => ((IList<string>)part.Copyright).Add("changed"));
            }
        }
        Require(seen.SetEquals(licenses.Keys), "Every document has an attributed component");
        if (sourceRoot is not null)
        {
            string directory = Path.Combine(sourceRoot, "licence");
            string[] documents = Directory.GetFiles(directory).Where(path =>
                (Path.GetExtension(path) is ".txt" or ".md") &&
                Path.GetFileName(path) is not ("ReferenceData-LICENSE.txt" or "THIRD_PARTY_NOTICES.md")).ToArray();
            Require(documents.Select(path => Path.GetFileName(path)).ToHashSet(StringComparer.Ordinal).SetEquals(licenses.Keys), "Shipped source document closure");
            foreach (string path in documents)
                Require(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) == licenses[Path.GetFileName(path)], "Full document text: " + path);
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(sourceRoot, "tools/licenses/engine_metadata.json")));
            var recorded = catalog.RootElement.GetProperty("components").EnumerateArray().ToArray();
            Require(recorded.Length == components.Count, "Catalog component count");
            for (int i = 0; i < recorded.Length; i++)
            {
                Require(recorded[i].GetProperty("name").GetString() == components[i].Name, "Catalog component identity");
                var parts = recorded[i].GetProperty("parts").EnumerateArray().ToArray();
                Require(parts.Length == components[i].Parts.Count, "Catalog parts count");
                for (int j = 0; j < parts.Length; j++)
                {
                    var actual = components[i].Parts[j];
                    Require(parts[j].GetProperty("license").GetString() == actual.License, "Catalog license identity");
                    Require(parts[j].GetProperty("files").EnumerateArray().Select(value => value.GetString()).SequenceEqual(actual.Files), "Catalog source scope");
                    Require(parts[j].GetProperty("copyright").EnumerateArray().Select(value => value.GetString()).SequenceEqual(actual.Copyright), "Catalog attribution");
                    var attributionTexts = new List<string> { licenses[actual.License] };
                    if (parts[j].TryGetProperty("attributionSources", out var sources))
                        foreach (var source in sources.EnumerateObject())
                            attributionTexts.Add(File.ReadAllText(Path.Combine(sourceRoot, source.Name)).Replace("\r\n", "\n", StringComparison.Ordinal));
                    Require(actual.Copyright.All(value => attributionTexts.Any(text => text.Contains(value, StringComparison.Ordinal))), "Verbatim legal/source attribution");
                }
            }
        }
        using var data = new MemoryStream();
        using (var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var category in authors.Concat(donors))
            {
                writer.Write(category.Key);
                writer.Write(category.Value.Count);
                foreach (string value in category.Value)
                    writer.Write(value);
            }
            foreach (var license in licenses.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.Write(license.Key);
                writer.Write(license.Value);
            }
            foreach (var component in components)
            {
                writer.Write(component.Name);
                writer.Write(component.Parts.Count);
                foreach (var part in component.Parts)
                {
                    writer.Write(part.License);
                    writer.Write(part.Files.Count);
                    foreach (string value in part.Files)
                        writer.Write(value);
                    writer.Write(part.Copyright.Count);
                    foreach (string value in part.Copyright)
                        writer.Write(value);
                }
            }
        }
        string digest = Convert.ToHexString(SHA256.HashData(data.ToArray()));
        Console.WriteLine($"Engine metadata: {authors["developers"].Count} author, {donors.Count} empty donor categories, {components.Count} components, {licenses.Count} documents; SHA256={digest}");
        return digest;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Reject(Action mutation)
    {
        try { mutation(); }
        catch (NotSupportedException) { return; }
        throw new InvalidOperationException("Returned metadata accepted a mutation.");
    }
}
