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
                var source = Capture(root);
                if (!source.Nodes.Any(n => root.GetNode(n.Path) is AnimationMixer mixer && mixer.ResetOnSave && mixer.HasAnimation("RESET")))
                    prepared = source;
                else
                {
                    isolated = true;
                    using var staging = new PackedScene(); staging.LoadFileData(source);
                    temporary = staging.InstantiateCore(PackedSceneEditState.Disabled, false);
                    var temporaryNodes = source.Nodes.Select(n => temporary.GetNode(n.Path)).ToArray();
                    var identities = temporaryNodes.Select(n => n.InstanceID).ToArray();
                    foreach (var node in temporaryNodes)
                        if (node is AnimationMixer mixer) mixer.ApplyResetForSave();
                    staging.Pack(temporary); var result = staging.FileData;
                    if (result.Nodes.Length != source.Nodes.Length) throw new InvalidOperationException("RESET changed the saved hierarchy.");
                    var nodes = new SceneNodeData[source.Nodes.Length];
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        if (result.Nodes[i].Factory.SourceInstanceID != identities[i] || result.Nodes[i].ParentIndex != source.Nodes[i].ParentIndex || result.Nodes[i].OwnerIndex != source.Nodes[i].OwnerIndex) throw new InvalidOperationException("RESET changed the saved hierarchy.");
                        nodes[i] = result.Nodes[i] with { Factory = source.Nodes[i].Factory };
                    }
                    prepared = new(nodes, result.Connections); owned = temporary.ReleaseSceneResourcesForSave();
                }
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
}
