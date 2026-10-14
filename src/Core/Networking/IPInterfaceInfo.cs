namespace Electron2D;

/// <summary>Provides a caller-owned snapshot of one host network adapter.</summary>
/// <param name="Name">The host adapter identifier.</param>
/// <param name="Friendly">Its human-readable name, which may be empty.</param>
/// <param name="Index">The host interface index in decimal string form.</param>
/// <param name="Addresses">Caller-owned IPv4/IPv6 address strings, including IPv6 scope when supplied by the host.</param>
/// <remarks>Enumeration order is not a routing priority. Mutating the address array does not change the host or resolver.</remarks>
public readonly record struct IPInterfaceInfo(string Name, string Friendly, string Index, string[] Addresses);
