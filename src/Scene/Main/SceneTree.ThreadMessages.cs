namespace Electron2D;

public partial class SceneTree
{
    private readonly Queue<Action> _processMessages = [];

    internal void EnqueueProcessMessage<T>(Action<T> callback, T value) =>
        _typedDeferredScheduler.Enqueue(callback, value, _processMessages);

    private void FlushProcessMessages(ref List<Exception>? errors)
    {
        while (true)
        {
            Action message;
            lock (_workGate) { if (!_processMessages.TryDequeue(out message!)) return; }
            try { message(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
    }
}
