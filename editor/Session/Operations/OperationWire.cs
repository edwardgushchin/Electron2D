using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D.Editor;

internal sealed record OperationEndpoint(int Version, Guid OwnerID, string ProjectRoot, string Socket, string Token);
internal sealed record OperationHello(int Version, Guid OwnerID, string Token, string ClientName);
internal sealed record OperationCall(string Method, JsonElement Arguments);
internal sealed record OperationFailure(string Code, string Message, Guid DocumentID = default, Guid ObjectID = default, ulong ExpectedRevision = 0, ulong ActualRevision = 0);
internal sealed record OperationReply(JsonElement? Value = null, OperationFailure? Error = null);

internal static class OperationWire
{
    internal const int Limit = 8 * 1024 * 1024;
    internal static readonly JsonSerializerOptions JSON = new()
    {
        PropertyNamingPolicy = new OperationNamingPolicy(),
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        IncludeFields = true,
        IgnoreReadOnlyProperties = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() }
    };
    private sealed class OperationNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name == "SHA256" ? "sha256" : CamelCase.ConvertName(name);
    }
    internal static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, JSON);
    internal static T Model<T>(JsonElement value) => value.Deserialize<T>(JSON) ?? throw new InvalidDataException("Operation arguments cannot be null.");
    internal static async Task Write<T>(Stream stream, T value, CancellationToken cancellation)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JSON);
        if (bytes.Length > Limit) throw new InvalidDataException("Operation messages are limited to eight MiB.");
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        await stream.WriteAsync(header, cancellation).ConfigureAwait(false);
        await stream.WriteAsync(bytes, cancellation).ConfigureAwait(false);
        await stream.FlushAsync(cancellation).ConfigureAwait(false);
    }
    internal static async Task<T> Read<T>(Stream stream, CancellationToken cancellation)
    {
        var header = new byte[4]; await stream.ReadExactlyAsync(header, cancellation).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 2 or > Limit) throw new InvalidDataException("Invalid operation frame size.");
        var bytes = new byte[length]; await stream.ReadExactlyAsync(bytes, cancellation).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(bytes, JSON) ?? throw new InvalidDataException("Operation frame cannot be null.");
    }
    internal static string DiscoveryPath(string root) => JobProtocol.ChildPath(IOPath.GetFullPath(root), ".electron2d/session/operations.json");
    internal static OperationEndpoint Discover(string root)
    {
        var path = DiscoveryPath(root);
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Local operation transport currently requires Linux Unix sockets.");
        if ((File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new IOException("Operation discovery must be private to its owner.");
        if (new FileInfo(path).Length > 16384) throw new InvalidDataException("Invalid operation discovery file.");
        var endpoint = JsonSerializer.Deserialize<OperationEndpoint>(File.ReadAllBytes(path), JSON) ?? throw new InvalidDataException("Missing operation endpoint.");
        if (endpoint.Version != 1 || endpoint.OwnerID == Guid.Empty || endpoint.ProjectRoot != IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(root)) || endpoint.Token is not { Length: 64 } || string.IsNullOrWhiteSpace(endpoint.Socket) || !IOPath.IsPathFullyQualified(endpoint.Socket))
            throw new InvalidDataException("Operation endpoint belongs to a different owner/project or protocol.");
        return endpoint;
    }
    internal static OperationFailure Failure(Exception error)
    {
        while (error is TypeInitializationException or System.Reflection.TargetInvocationException && error.InnerException is not null) error = error.InnerException;
        return error is SessionConflictException conflict
        ? new("CONFLICT", conflict.Message, conflict.DocumentID, conflict.ObjectID, conflict.ExpectedRevision, conflict.ActualRevision)
        : new(error is UnauthorizedAccessException ? "PERMISSION_DENIED" : error is OperationCanceledException ? "CANCELLED" : error is JsonException or ArgumentException or InvalidDataException ? "INVALID_ARGUMENT" : "OPERATION_FAILED", error.Message[..Math.Min(error.Message.Length, 4096)]);
    }
}
