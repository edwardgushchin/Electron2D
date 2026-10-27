namespace Electron2D;

public static partial class ResourceFileTypes
{
    internal static void RegisterFactory(ResourceFileType type)
    {
        lock (Gate)
        {
            if (ByID.ContainsKey(type.ID) || ByType.ContainsKey(type.Type)) throw new InvalidOperationException("Type or ID is already registered.");
            ByID.Add(type.ID, type); ByType.Add(type.Type, type);
        }
    }
    internal static ExtensionClass? FindExtensionClass(Type type)
    {
        lock (Gate) return ByType.GetValueOrDefault(type)?.ExtensionClass;
    }
    internal static void RemoveExtensionClass(ExtensionClass definition)
    {
        lock (Gate)
        {
            if (ReferenceEquals(ByID.GetValueOrDefault(definition.FileType.ID), definition.FileType)) ByID.Remove(definition.FileType.ID);
            if (ReferenceEquals(ByType.GetValueOrDefault(definition.FileType.Type), definition.FileType)) ByType.Remove(definition.FileType.Type);
        }
    }
}
