using Electron2D;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class VisualShaderSourceRegressionTests
{
    public static void Write(string folder, string output)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(folder, "*.e2dres").Order(StringComparer.Ordinal))
        {
            using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var code = graph.GenerateSources();
            var metadata = JsonSerializer.Serialize(new
            {
                Images = code.Images.Select(i => new
                {
                    i.Stage,
                    i.Name,
                    i.IsArray,
                    i.Policy,
                    i.PublicName,
                    DefaultType = i.Default?.ClassName,
                    DefaultPath = i.Default?.ResourcePath,
                    DefaultName = i.Default?.ResourceName
                }),
                code.Blocks
            });
            var payload = code.Vertex + "\0" + code.Fragment + "\0" + metadata;
            result.Add(System.IO.Path.GetFileName(file), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))));
        }
        if (result.Count == 0) throw new InvalidOperationException("No graph fixtures were found.");
        File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Generated source/interface metadata hashes for {result.Count} archived graphs.");
    }
}
