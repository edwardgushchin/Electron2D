namespace Electron2D;

internal enum RuntimeUserCodeFailureKind
{
    LifecycleCallback,
    GroupCall,
    DeferredCall,
    SignalEmission
}
