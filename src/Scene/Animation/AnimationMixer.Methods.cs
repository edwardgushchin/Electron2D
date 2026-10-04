namespace Electron2D;

public partial class AnimationMixer
{
    /// <summary>Selects safe-point or synchronous typed method-key invocation.</summary>
    public enum AnimationCallbackModeMethod
    {
        /// <summary>Queue on the target SceneTree; detached targets invoke immediately.</summary>
        Deferred = 0,
        /// <summary>Invoke while the controller evaluates the key.</summary>
        Immediate = 1,
    }
    private AnimationCallbackModeMethod _methodMode;
    internal int MethodCallbackCapacity { get; private set; } = 16;
    internal bool MethodUpdateOnly, MethodExternalSeeking;
    /// <summary>Gets or sets method-key dispatch policy; defaults to Deferred.</summary>
    /// <value>The defined dispatch mode.</value>
    /// <remarks>Mutation requires the attached SceneTree owner thread. Deferred keys retain their payload until its safe point; disposed/deleting or moved targets are skipped.</remarks>
    /// <exception cref="ObjectDisposedException">This mixer has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs outside the owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is invalid.</exception>
    public AnimationCallbackModeMethod CallbackModeMethod { get { ThrowIfDisposed(); return _methodMode; } set { EnsureAnimationMutable(); Animation.Valid(value); _methodMode = value; } }
    /// <summary>Prepares a minimum pending deferred-call capacity for each method-track binding.</summary>
    /// <param name="pendingCallsPerTrack">A nonnegative capacity for the largest burst before a SceneTree flush.</param>
    /// <remarks>Bindings initially prepare max(16, key count) records. This explicit cold operation prepares bindings and reusable typed call records; capacity cannot shrink. Exceeding prepared pending capacity during dispatch throws InvalidOperationException while retaining already queued calls. The shared SceneTree queue warms separately. Successful hot dispatch never grows their capacity. User callback allocations remain outside engine control.</remarks>
    /// <exception cref="ObjectDisposedException">This mixer or a required animation resource has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs outside the owner thread or binding descriptors conflict.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The capacity is negative.</exception>
    public void PrepareMethodCallbacks(int pendingCallsPerTrack)
    { EnsureAnimationMutable(); if (pendingCallsPerTrack < 0) throw new ArgumentOutOfRangeException(nameof(pendingCallsPerTrack)); MethodCallbackCapacity = Math.Max(MethodCallbackCapacity, pendingCallsPerTrack); EnsureBlendCaches(); foreach (var cache in _blendCaches.Values) foreach (var binding in cache.Bindings) binding?.PrepareMethodCalls(MethodCallbackCapacity); foreach (var binding in _bindings) binding?.PrepareMethodCalls(MethodCallbackCapacity); }
    private static readonly PropertyDescriptor[] MethodProperties = [new PropertyDescriptor<AnimationMixer, AnimationCallbackModeMethod>(nameof(CallbackModeMethod), n => n.CallbackModeMethod, (n, v) => n.CallbackModeMethod = v, _ => AnimationCallbackModeMethod.Deferred)];
}
