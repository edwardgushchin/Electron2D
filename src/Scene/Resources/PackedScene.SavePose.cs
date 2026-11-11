using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class PackedScene
{
    /// <summary>Captures a save-ready scene with enabled mixers' RESET property poses.</summary>
    /// <param name="root">The live scene root whose owned hierarchy is saved.</param>
    /// <remarks>Allocating authoring operation on the owner thread. Ordinary Pack captures the current pose.
    /// A temporary detached instance receives RESET value and Bézier tracks at zero in hierarchy order,
    /// including coupled setter effects. Factories and stored setters execute; scene lifecycle, local-resource
    /// setup and method/audio/nested event tracks do not. Custom authoring state must be stored in descriptors.
    /// Preview values, controller clocks and queues are unchanged. A failure preserves the previous packed data;
    /// application callback effects outside the temporary hierarchy remain application-owned.</remarks>
    /// <exception cref="InvalidOperationException">Capture reenters, a target root is absent or evaluation is invalidated.</exception>
    /// <exception cref="Exception">A factory, descriptor, RESET callback or cleanup operation fails.</exception>
    public void PackForSave(Node root)
    {
        ArgumentNullException.ThrowIfNull(root); ObjectDisposedException.ThrowIf(root.IsDisposed, root);
        PackedSceneData? prepared = null; Resource[] owned = []; Exception? error = null; var isolated = false;
        lock (_gate)
        {
            ThrowIfDisposed(); if (_packing) throw new InvalidOperationException("Packed-scene capture cannot be re-entered.");
            _packing = true; Node[]? sourceGuard = null; Node? temporary = null;
            try
            {
                sourceGuard = root.BeginSceneCapture();
                var selected = GetOwnedSceneNodes(root);
                var source = Capture(root);
                if (!selected.Any(node => node is AnimationMixer mixer && mixer.ResetOnSave && mixer.HasAnimation("RESET")))
                    prepared = source;
                else
                {
                    isolated = true;
                    using var staging = new PackedScene(); staging.LoadFileData(source);
                    temporary = staging.InstantiateCore(PackedSceneEditState.Main, false);
                    prepared = CaptureResetPose(temporary, source);
                    owned = temporary.ReleaseSceneResourcesForSave();
                }
                _ = PrepareTemplates(this, prepared);
            }
            catch (Exception caught) { error = caught; }
            finally
            {
                try { temporary?.Dispose(); } catch (Exception cleanup) { error = error is null ? cleanup : new AggregateException(error, cleanup); }
                if (sourceGuard is not null) Node.EndSceneCapture(sourceGuard); _packing = false;
            }
            if (error is null) { ReplaceDataUnderLock(prepared!); if (isolated) AdoptFileResources(owned); }
            else foreach (var resource in owned) try { resource.Dispose(); } catch (Exception cleanup) { error = new AggregateException(error, cleanup); }
        }
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
        EmitChanged();
    }

    private static PackedSceneData CaptureResetPose(Node temporary, PackedSceneData source)
    {
        var before = GetOwnedSceneNodes(temporary).Select(node => (Node: node, node.Parent, node.Owner)).ToArray();
        var factories = source.Nodes.ToDictionary(record => temporary.GetNode(record.Path).InstanceID, record => record.Factory);
        foreach (var entry in before)
            if (entry.Node is AnimationMixer mixer) mixer.ApplyResetForSave();

        var guard = temporary.BeginSceneCapture();
        try
        {
            var after = GetOwnedSceneNodes(temporary);
            if (after.Count != before.Length) throw new InvalidOperationException("RESET changed the saved hierarchy.");
            for (var i = 0; i < after.Count; i++)
            {
                var node = after[i];
                if (node != before[i].Node || node.Parent != before[i].Parent || node.Owner != before[i].Owner)
                    throw new InvalidOperationException("RESET changed the saved hierarchy.");
            }
            var result = Capture(temporary);
            var nodes = result.Nodes.Select(node => factories.TryGetValue(node.Factory.SourceInstanceID, out var factory)
                ? node with { Factory = factory } : node).ToArray();
            return new(nodes, result.Connections);
        }
        finally { Node.EndSceneCapture(guard); }
    }
}
