using System.Text.Json;
using Electron2D;

if (args.Length > 2)
    throw new ArgumentException("Expected the optional source root and version expectation file. Use - to omit the source root.");

if (args.Length == 2)
{
    using var expectation = JsonDocument.Parse(File.ReadAllText(args[1]));
    var expected = expectation.RootElement;
    string architecture = expected.GetProperty("architecture").GetString()!;
    var assemblyVersion = Version.Parse(expected.GetProperty("assemblyVersion").GetString()!);
    string informationalVersion = expected.GetProperty("informationalVersion").GetString()!;
    int major = expected.GetProperty("major").GetInt32();
    int minor = expected.GetProperty("minor").GetInt32();
    int patch = expected.GetProperty("patch").GetInt32();
    long hex = expected.GetProperty("hex").GetInt64();
    string status = expected.GetProperty("status").GetString()!;
    string build = expected.GetProperty("build").GetString()!;
    string sourceRevision = expected.GetProperty("sourceRevision").GetString()!;
    long sourceTimestamp = expected.GetProperty("sourceTimestamp").GetInt64();

    EngineVersionInfo? first = null;
    Parallel.For(0, 64, _ =>
    {
        var version = Engine.VersionInfo;
        var previous = Interlocked.CompareExchange(ref first, version, null);
        Require(previous is null || ReferenceEquals(previous, version), nameof(Engine.VersionInfo));
        Require(Engine.ArchitectureName == architecture, nameof(Engine.ArchitectureName));
        Require(version.AssemblyVersion == assemblyVersion, nameof(version.AssemblyVersion));
        Require(version.InformationalVersion == informationalVersion, nameof(version.InformationalVersion));
        Require(version.ToString() == informationalVersion, nameof(version.ToString));
        Require(version.Major == major, nameof(version.Major));
        Require(version.Minor == minor, nameof(version.Minor));
        Require(version.Patch == patch, nameof(version.Patch));
        Require(version.Hex == hex, nameof(version.Hex));
        Require(version.Status == status, nameof(version.Status));
        Require(version.Build == build, nameof(version.Build));
        Require(version.SourceRevision == sourceRevision, nameof(version.SourceRevision));
        Require(version.SourceTimestamp == sourceTimestamp, nameof(version.SourceTimestamp));
    });
    Require(ReferenceEquals(first, Engine.VersionInfo), nameof(Engine.VersionInfo));
    Console.WriteLine($"Engine version verified across 64 reads: {architecture}, {first}, source={sourceRevision}, timestamp={sourceTimestamp}.");
}

EngineMetadataChecks.Run(args.Length == 0 || args[0] == "-" ? null : args[0]);

static void Require(bool condition, string property)
{
    if (!condition)
        throw new InvalidOperationException("Engine metadata expectation failed for " + property + ".");
}
