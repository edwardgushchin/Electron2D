namespace Electron2D;

public partial class AnimationMixer
{
    internal void ApplyResetForSave()
    {
        if (!ResetOnSave || !HasAnimation("RESET")) return;
        EnsureAnimationMutable(); var animation = GetAnimation("RESET"); ObjectDisposedException.ThrowIf(animation.IsDisposed, animation);
        var root = (RootNode.Length == 0 ? this : GetNodeOrNull(RootNode)) ?? throw new InvalidOperationException("The RESET target root is unavailable.");
        var active = _active; var previous = _cachedAnimation; _active = true; _cachedAnimation = animation; _evaluationDepth++;
        try
        {
            var generation = _bindingGeneration; var revision = animation.ChangeRevision;
            for (var i = 0; i < animation.GetTrackCount(); i++)
            {
                var track = animation.Get(i);
                if (!track.Enabled || track.Kind is not (Animation.TrackType.Value or Animation.TrackType.Bezier)) continue;
                var binding = track.Bind(root); binding?.Apply(animation, i, 0, false, null, this, generation);
                if (IsDisposed || animation.IsDisposed || animation.ChangeRevision != revision || _bindingGeneration != generation)
                    throw new InvalidOperationException("RESET save preparation was invalidated during evaluation.");
            }
        }
        finally { _evaluationDepth--; _cachedAnimation = previous; _active = active; }
    }
}
