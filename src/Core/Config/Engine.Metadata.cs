using System.Collections.ObjectModel;
using System.Text;

namespace Electron2D;

public sealed partial class Engine
{
    /// <summary>Returns the recorded Electron2D authors grouped by contributor role.</summary>
    /// <returns>Immutable ordinal categories lead_developers, founders, project_managers and developers.</returns>
    /// <remarks>Developers come from the package authors. Unrecorded roles are empty. Available before runtime startup on any thread.</remarks>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetAuthorInfo() => DistributionMetadata.Authors;

    /// <summary>Returns the recorded source and distribution components with their legal attribution.</summary>
    /// <returns>A deeply immutable component list with source scopes and exact license-document references.</returns>
    /// <remarks>The build's distribution catalog includes optional platform dependencies and retained notices.
    /// It does not inspect installed or loaded libraries. Comparison-only source data is excluded.</remarks>
    public static IReadOnlyList<EngineCopyrightInfo> GetCopyrightInfo() => DistributionMetadata.Components;

    /// <summary>Returns the recorded Electron2D sponsors and donors grouped by contribution category.</summary>
    /// <returns>Immutable categories platinum_sponsors, gold_sponsors, silver_sponsors, bronze_sponsors,
    /// mini_sponsors, gold_donors, silver_donors and bronze_donors; unrecorded categories are empty.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetDonorInfo() => DistributionMetadata.Donors;

    /// <summary>Returns the full license and notice documents retained by this Electron2D distribution.</summary>
    /// <returns>An immutable ordinal map from unique license-document filenames to their complete UTF8 text.</returns>
    /// <remarks>Line endings are normalized to LF. Document keys preserve distinct provider versions and composite
    /// terms rather than merging documents that share a license family. No external files or native services are required.</remarks>
    public static IReadOnlyDictionary<string, string> GetLicenseInfo() => DistributionMetadata.Licenses;

    /// <summary>Returns the full MIT license and copyright notice for Electron2D-authored code.</summary>
    /// <returns>The embedded Electron2D-LICENSE.txt with LF line endings.</returns>
    public static string GetLicenseText() => DistributionMetadata.Licenses["Electron2D-LICENSE.txt"];

    private static class DistributionMetadata
    {
        internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Authors;
        internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Donors;
        internal static readonly IReadOnlyDictionary<string, string> Licenses;
        internal static readonly IReadOnlyList<EngineCopyrightInfo> Components;

        static DistributionMetadata()
        {
            using Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("Electron2D.EngineMetadata")
                ?? throw new InvalidDataException("The engine distribution metadata is missing.");
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadInt32() != 1)
                throw new InvalidDataException("The engine distribution metadata version is invalid.");
            Authors = ReadCategories(reader);
            Donors = ReadCategories(reader);
            var licenses = new Dictionary<string, string>(StringComparer.Ordinal);
            int licenseCount = reader.ReadInt32();
            for (int i = 0; i < licenseCount; i++)
                licenses.Add(ReadText(reader), ReadText(reader));
            Licenses = new ReadOnlyDictionary<string, string>(licenses);
            var components = new EngineCopyrightInfo[reader.ReadInt32()];
            for (int i = 0; i < components.Length; i++)
            {
                string name = ReadText(reader);
                var parts = new EngineCopyrightPart[reader.ReadInt32()];
                for (int j = 0; j < parts.Length; j++)
                    parts[j] = new EngineCopyrightPart(ReadStrings(reader), ReadStrings(reader), ReadText(reader));
                components[i] = new EngineCopyrightInfo(name, parts);
            }
            Components = Array.AsReadOnly(components);
        }

        private static string ReadText(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException("The engine distribution metadata is incomplete.");
            return Encoding.UTF8.GetString(bytes);
        }

        private static string[] ReadStrings(BinaryReader reader)
        {
            var values = new string[reader.ReadInt32()];
            for (int i = 0; i < values.Length; i++)
                values[i] = ReadText(reader);
            return values;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadCategories(BinaryReader reader)
        {
            var values = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                values.Add(ReadText(reader), Array.AsReadOnly(ReadStrings(reader)));
            return new ReadOnlyDictionary<string, IReadOnlyList<string>>(values);
        }
    }
}
