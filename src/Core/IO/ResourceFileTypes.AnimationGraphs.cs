namespace Electron2D;

public static partial class ResourceFileTypes
{
    private static void RegisterAnimationGraphFiles()
    {
        RegisterResource("AnimationNode", CreateAnimationNodeFileResource);
        RegisterResource("AnimationRootNode", CreateAnimationRootNodeFileResource);
        RegisterResource("AnimationNodeSync", CreateAnimationNodeSyncFileResource);
        RegisterResource("AnimationNodeAnimation", CreateAnimationNodeAnimationFileResource);
        RegisterResource("AnimationNodeBlendSpace1D", CreateAnimationNodeBlendSpace1DFileResource);
        RegisterResource("AnimationNodeBlendSpace2D", CreateAnimationNodeBlendSpace2DFileResource);
        RegisterResource("AnimationNodeBlendTree", CreateAnimationNodeBlendTreeFileResource);
        RegisterResource("AnimationNodeOutput", CreateAnimationNodeOutputFileResource);
        RegisterResource("AnimationNodeOneShot", CreateAnimationNodeOneShotFileResource);
        RegisterResource("AnimationNodeBlend2", CreateAnimationNodeBlend2FileResource);
        RegisterResource("AnimationNodeBlend3", CreateAnimationNodeBlend3FileResource);
        RegisterResource("AnimationNodeAdd2", CreateAnimationNodeAdd2FileResource);
        RegisterResource("AnimationNodeAdd3", CreateAnimationNodeAdd3FileResource);
        RegisterResource("AnimationNodeSub2", CreateAnimationNodeSub2FileResource);
        RegisterResource("AnimationNodeTimeScale", CreateAnimationNodeTimeScaleFileResource);
        RegisterResource("AnimationNodeTimeSeek", CreateAnimationNodeTimeSeekFileResource);
        RegisterResource("AnimationNodeStateMachine", CreateAnimationNodeStateMachineFileResource);
        RegisterResource("AnimationNodeStateMachineTransition", CreateAnimationNodeStateMachineTransitionFileResource);
        RegisterResource("AnimationNodeTransition", CreateAnimationNodeTransitionFileResource);
        RegisterAnimationParameter("engine.AnimationNodeBlendSpace1D.BlendPosition", AnimationNodeBlendSpace1D.BlendPosition);
        RegisterAnimationParameter("engine.AnimationNodeBlendSpace2D.BlendPosition", AnimationNodeBlendSpace2D.BlendPosition);
        RegisterAnimationParameter("engine.AnimationNodeOneShot.Request", AnimationNodeOneShot.Request);
        RegisterAnimationParameter("engine.AnimationNodeOneShot.FadeInRemaining", AnimationNodeOneShot.FadeInRemaining);
        RegisterAnimationParameter("engine.AnimationNodeOneShot.FadeOutRemaining", AnimationNodeOneShot.FadeOutRemaining);
        RegisterAnimationParameter("engine.AnimationNodeOneShot.TimeToRestart", AnimationNodeOneShot.TimeToRestart);
        RegisterAnimationParameter("engine.AnimationNodeBlend2.BlendAmount", AnimationNodeBlend2.BlendAmount);
        RegisterAnimationParameter("engine.AnimationNodeBlend3.BlendAmount", AnimationNodeBlend3.BlendAmount);
        RegisterAnimationParameter("engine.AnimationNodeAdd2.AddAmount", AnimationNodeAdd2.AddAmount);
        RegisterAnimationParameter("engine.AnimationNodeAdd3.AddAmount", AnimationNodeAdd3.AddAmount);
        RegisterAnimationParameter("engine.AnimationNodeSub2.SubAmount", AnimationNodeSub2.SubAmount);
        RegisterAnimationParameter("engine.AnimationNodeTimeScale.Scale", AnimationNodeTimeScale.Scale);
        RegisterAnimationParameter("engine.AnimationNodeTimeSeek.SeekRequest", AnimationNodeTimeSeek.SeekRequest);
        RegisterAnimationParameter("engine.AnimationNodeTransition.TransitionRequest", AnimationNodeTransition.TransitionRequest);
        RegisterAnimationParameter("engine.AnimationNodeTransition.PreviousIndex", AnimationNodeTransition.PreviousIndex);
        RegisterAnimationParameter("engine.AnimationNodeTransition.PreviousXFading", AnimationNodeTransition.PreviousXFading);
    }
    private static AnimationNode CreateAnimationNodeFileResource() => new();
    private static AnimationRootNode CreateAnimationRootNodeFileResource() => new();
    private static AnimationNodeSync CreateAnimationNodeSyncFileResource() => new();
    private static AnimationNodeAnimation CreateAnimationNodeAnimationFileResource() => new();
    private static AnimationNodeBlendSpace1D CreateAnimationNodeBlendSpace1DFileResource() => new();
    private static AnimationNodeBlendSpace2D CreateAnimationNodeBlendSpace2DFileResource() => new();
    private static AnimationNodeBlendTree CreateAnimationNodeBlendTreeFileResource() => new();
    private static AnimationNodeOutput CreateAnimationNodeOutputFileResource() => new();
    private static AnimationNodeOneShot CreateAnimationNodeOneShotFileResource() => new();
    private static AnimationNodeBlend2 CreateAnimationNodeBlend2FileResource() => new();
    private static AnimationNodeBlend3 CreateAnimationNodeBlend3FileResource() => new();
    private static AnimationNodeAdd2 CreateAnimationNodeAdd2FileResource() => new();
    private static AnimationNodeAdd3 CreateAnimationNodeAdd3FileResource() => new();
    private static AnimationNodeSub2 CreateAnimationNodeSub2FileResource() => new();
    private static AnimationNodeTimeScale CreateAnimationNodeTimeScaleFileResource() => new();
    private static AnimationNodeTimeSeek CreateAnimationNodeTimeSeekFileResource() => new();
    private static AnimationNodeStateMachine CreateAnimationNodeStateMachineFileResource() => new();
    private static AnimationNodeStateMachineTransition CreateAnimationNodeStateMachineTransitionFileResource() => new();
    private static AnimationNodeTransition CreateAnimationNodeTransitionFileResource() => new();

    /// <summary>Registers an immutable compiled animation parameter identity for graph scene files.</summary>
    /// <typeparam name="TValue">The exact portable value or resource-reference type.</typeparam>
    /// <param name="id">A stable ordinal ID, at most 256 characters.</param>
    /// <param name="parameter">The exact immutable key declared by a graph definition.</param>
    /// <remarks>Register application keys before authoring/saving and in every loading process. Built-in writable keys are registered automatically; read-only registered keys can identify conditions but are not stored as tree values.
    /// File data selects this compiled key; it does not create arbitrary CLR types or invoke reflected members.</remarks>
    /// <exception cref="ArgumentException">The ID is invalid.</exception>
    /// <exception cref="InvalidOperationException">The ID or exact key is already registered.</exception>
    public static void RegisterAnimationParameter<TValue>(string id, AnimationParameter<TValue> parameter)
    {
        ValidateAnimationGraphID(id); ArgumentNullException.ThrowIfNull(parameter);
        lock (Gate)
        {
            var bindings = ResourceLoader.Runtime.FileTypes.AnimationParameters;
            if (bindings.ContainsKey(id) || bindings.Values.Any(b => ReferenceEquals(b.Parameter, parameter)))
                throw new InvalidOperationException("Animation parameter or ID is already registered.");
            bindings.Add(id, new AnimationFileParameterBinding<TValue>(id, parameter));
        }
    }
    /// <summary>Registers a compiled state-machine predicate for typed graph resource files.</summary>
    /// <param name="id">An immutable ordinal ID, at most 256 characters.</param>
    /// <param name="predicate">The application callback receiving the evaluated tree.</param>
    /// <remarks>Register the same callback in every process. Captured state remains application-owned; no delegate is serialized.</remarks>
    /// <exception cref="ArgumentException">The ID is invalid.</exception>
    /// <exception cref="InvalidOperationException">The ID or callback is already registered.</exception>
    public static void RegisterAnimationPredicate(string id, Func<AnimationTree, bool> predicate)
    {
        ValidateAnimationGraphID(id); ArgumentNullException.ThrowIfNull(predicate);
        lock (Gate)
        {
            var bindings = ResourceLoader.Runtime.FileTypes.AnimationPredicates;
            if (bindings.ContainsKey(id) || bindings.Values.Any(p => Equals(p, predicate)))
                throw new InvalidOperationException("Animation predicate or ID is already registered.");
            bindings.Add(id, predicate);
        }
    }
    private static void ValidateAnimationGraphID(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id.Length > 256 || id.Any(char.IsControl)) throw new ArgumentException("Invalid graph binding ID.", nameof(id));
    }
    internal static AnimationFileParameterBinding AnimationParameterBinding(string id)
    {
        lock (Gate) return ResourceLoader.Runtime.FileTypes.AnimationParameters.GetValueOrDefault(id)
            ?? throw new InvalidDataException("Animation parameter ID is not registered.");
    }
    internal static AnimationFileParameterBinding AnimationParameterBinding(AnimationParameter parameter)
    {
        lock (Gate) return ResourceLoader.Runtime.FileTypes.AnimationParameters.Values.FirstOrDefault(b => ReferenceEquals(b.Parameter, parameter))
            ?? throw new NotSupportedException("Register the exact animation parameter before saving.");
    }
    internal static string AnimationPredicateID(Func<AnimationTree, bool>? predicate)
    {
        if (predicate is null) return "";
        lock (Gate) foreach (var p in ResourceLoader.Runtime.FileTypes.AnimationPredicates) if (Equals(p.Value, predicate)) return p.Key;
        throw new NotSupportedException("Register the exact animation predicate before saving.");
    }
    internal static Func<AnimationTree, bool>? AnimationPredicate(string id)
    {
        ArgumentNullException.ThrowIfNull(id); if (id.Length == 0) return null;
        lock (Gate) return ResourceLoader.Runtime.FileTypes.AnimationPredicates.GetValueOrDefault(id)
            ?? throw new InvalidDataException("Animation predicate ID is not registered.");
    }
}
internal abstract class AnimationFileParameterBinding(string id, AnimationParameter parameter)
{
    internal string ID => id;
    internal AnimationParameter Parameter => parameter;
    internal abstract PropertyDescriptor Property(string path, int index);
    internal abstract void Restore(AnimationTree tree, string path);
}
internal sealed class AnimationFileParameterBinding<T>(string id, AnimationParameter<T> parameter) : AnimationFileParameterBinding(id, parameter)
{
    internal override PropertyDescriptor Property(string path, int index) => new PropertyDescriptor<AnimationTree, T>($"Parameter[{index}]",
        n => n.GetFileParameter(path, parameter), (n, v) => n.SetFileParameter(path, parameter, v), stored: true);
    internal override void Restore(AnimationTree tree, string path) => tree.SetParameter(path, parameter, tree.GetFileParameter(path, parameter));
}
