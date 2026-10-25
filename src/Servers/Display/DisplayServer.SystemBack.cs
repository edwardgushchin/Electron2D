namespace Electron2D;

public sealed partial class DisplayServer
{
    private long _pendingSystemGoBack;
    internal event Action? GoBackRequestedCore { add => _publisherGoBackRequestedCore.Add(value); remove => _publisherGoBackRequestedCore.Remove(value); }
    private EventPublisher<Action> _publisherGoBackRequestedCore;

    internal static bool TryQueueSystemGoBack()
    {
        lock (InstanceGate)
        {
            if (_instance is not { IsDisposed: false } server || server._retiringNativeWindow) return false;
            checked { server._pendingSystemGoBack++; }
            return true;
        }
    }

    private void DrainSystemGoBack(ref List<Exception>? failures)
    {
        long count;
        lock (InstanceGate) { count = _pendingSystemGoBack; _pendingSystemGoBack = 0; }
        for (long i = 0; i < count; i++)
            try { PublishWindow(BeginEventEmission(_publisherGoBackRequestedCore.Handlers), _windowEvents?.BackHandlers); }
            catch (Exception error) { (failures ??= []).Add(error); }
    }
}
