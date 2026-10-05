using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Electron2D;

public sealed partial class ConfigFile
{
    private static readonly Lazy<JsonSerializerOptions> NativeJSONOptions = new(CreateNativeJSONOptions);

    internal static JsonTypeInfo<T> GetJSONTypeInfo<T>(JsonTypeInfo<T>? typeInfo = null) =>
        typeInfo ?? (JsonTypeInfo<T>)NativeJSONOptions.Value.GetTypeInfo(typeof(T));

    internal static JsonTypeInfo<T> CheckJSONTypeInfo<T>(JsonTypeInfo<T> typeInfo)
    {
        var visited = new HashSet<Type>();
        Check(typeInfo);
        return typeInfo;

        void Check(JsonTypeInfo info)
        {
            if (!visited.Add(info.Type)) return;
            CheckNativeJSONType(info.Type);
            info.Options.MakeReadOnly();
            foreach (var property in info.Properties)
            {
                CheckNativeJSONType(property.PropertyType);
                Check(info.Options.GetTypeInfo(property.PropertyType));
            }
            if (info.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary)
            {
                if (info.Type.GetElementType() is { } element) Check(info.Options.GetTypeInfo(element));
                foreach (var argument in info.Type.GetGenericArguments()) Check(info.Options.GetTypeInfo(argument));
            }
            info.MakeReadOnly();
        }
    }

    private static void CheckNativeJSONType(Type type)
    {
        if (type == typeof(object) || typeof(ElectronObject).IsAssignableFrom(type))
            throw new NotSupportedException("Native JSON metadata requires concrete non-engine types.");
    }

    private static JsonSerializerOptions CreateNativeJSONOptions()
    {
        var options = new JsonSerializerOptions(ValueJsonOptions);
        var resolver = JsonSerializer.IsReflectionEnabledByDefault
            ? JsonTypeInfoResolver.Combine(ValueJSONContext.Default, CreateReflectionJSONResolver())
            : (IJsonTypeInfoResolver)ValueJSONContext.Default;
        options.TypeInfoResolver = resolver.WithAddedModifier(info => CheckNativeJSONType(info.Type));
        options.MakeReadOnly();
        return options;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The reflection fallback is removed by the JSON reflection feature switch in trimmed hosts; custom models supply compiled metadata.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The reflection fallback is used only by hosts that enable JSON reflection; AOT hosts use the generated context or explicit metadata.")]
    private static IJsonTypeInfoResolver CreateReflectionJSONResolver() => new DefaultJsonTypeInfoResolver();

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, IncludeFields = true)]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(byte))]
    [JsonSerializable(typeof(sbyte))]
    [JsonSerializable(typeof(short))]
    [JsonSerializable(typeof(ushort))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(char))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonNode))]
    [JsonSerializable(typeof(Color))]
    [JsonSerializable(typeof(Vector2))]
    [JsonSerializable(typeof(Vector2i))]
    [JsonSerializable(typeof(Vector3))]
    [JsonSerializable(typeof(Vector3i))]
    [JsonSerializable(typeof(Vector4))]
    [JsonSerializable(typeof(Vector4i))]
    [JsonSerializable(typeof(Rect2))]
    [JsonSerializable(typeof(Rect2i))]
    [JsonSerializable(typeof(Transform))]
    [JsonSerializable(typeof(AudioDefaultPlaybackType))]
    [JsonSerializable(typeof(InputActionSettings))]
    private sealed partial class ValueJSONContext : JsonSerializerContext;
}
