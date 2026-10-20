namespace Electron2D;

public sealed partial class PackedScene
{
    private static void ConfigurePlaceholder(InstancePlaceholder placeholder, SceneNodeData record, Resource.SceneDuplicationScope resources)
    {
        var descriptors = placeholder.GetPropertyList().Where(descriptor => !placeholder.PendingFields.Fields.ContainsKey(descriptor.Name))
            .ToDictionary(descriptor => descriptor.Name, StringComparer.Ordinal);
        var pending = new List<MissingResourceData.Field>(); var direct = new List<ScenePropertyData>();
        foreach (var field in record.DeferredProperties?.Fields.Values ?? Enumerable.Empty<MissingResourceData.Field>())
        {
            descriptors.TryGetValue(field.Name, out var descriptor);
            descriptor ??= ResourceFileTypes.FindValueType(field.Schema) is { } type ? Node.StoredEditorProperty(field.Name, type) : null;
            if (descriptor is { IsStored: true, IsReadOnly: false } && ResourceFileTypes.ValueID(descriptor.ValueType) == field.Schema)
            {
                if (field.Name != "__SceneInstanceOrigin") direct.Add(new(field.Name, descriptor.ReadDeferredValue(field)));
            }
            else pending.Add(field);
        }
        RestoreProperties(placeholder, direct.ToArray(), resources);
        var path = record.InstancePlaceholder.Length == 0 ? placeholder.GetInstancePath() : record.InstancePlaceholder;
        placeholder.InitializePending(path, pending);
        if (path.Length != 0) placeholder.SetSceneInstanceLoadPlaceholder(true);
    }

    private static bool[] DeferredSkippedNodes(PackedSceneData data)
    {
        var skipped = new bool[data.Nodes.Length];
        for (var i = 0; i < skipped.Length; i++)
        {
            var node = data.Nodes[i];
            skipped[i] = node.ParentIndex >= 0 && skipped[node.ParentIndex] || node.InstanceRootIndex >= 0 &&
                (skipped[node.InstanceRootIndex] || data.Nodes[node.InstanceRootIndex].InstancePlaceholder.Length != 0);
        }
        return skipped;
    }

    internal static void ApplyPlaceholderProperties(Node instance, MissingResourceData.Field[] fields, InstancePlaceholder placeholder)
    {
        var nodes = instance.EnumerateDepthFirst().ToArray();
        var resources = CreateSceneDuplicationScope();
        var topology = new SceneTopology();
        foreach (var node in nodes) { node.BeginSceneInstantiation(); topology.Track(node); }
        try
        {
            var record = new SceneNodeData(new SceneFactoryData(static () => new Node(), instance.GetType(), 0), instance.Name,
                -1, -1, -1, ".", ".", "", [], fields.Select(field => new ScenePropertyData(field.Name, new DeferredPropertyValue(field))).ToArray(), null, "");
            RestoreProperties(instance, record.Properties, resources, nodeReferences: null, localRoot: instance, externalReference: placeholder.GetNodeOrNull);
            foreach (var node in nodes) node.ValidateSceneInstance();
            resources.AssignLocalScene(instance); resources.SetupLocalResources();
            instance.AppendSceneResources(resources.ReleaseCreated()); topology.Validate();
        }
        catch (Exception error)
        {
            var cleanup = resources.DisposeCreated();
            if (cleanup.Count != 0) { cleanup.Insert(0, error); throw new AggregateException("Pending property restoration and cleanup failed.", cleanup); }
            throw;
        }
        finally { foreach (var node in nodes) node.EndSceneInstantiation(); }
    }
}
