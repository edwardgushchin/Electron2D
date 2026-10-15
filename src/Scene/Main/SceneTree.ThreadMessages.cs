namespace Electron2D;

public partial class SceneTree
{
    internal void EnqueueProcessMessage<T>(Node receiver, Action<T> callback, T value)
    {
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();
            if (!ReferenceEquals(receiver.Tree, this) || receiver.ActiveProcessingGroup is not { Removed: false } group)
                throw new InvalidOperationException("The receiver has no active processing group in this tree.");
            _typedDeferredScheduler.Enqueue(callback, value, group.Messages);
        }
    }

    private void FlushProcessMessages(ProcessingGroup group, ref List<Exception>? errors)
    {
        while (true)
        {
            Action message;
            lock (_workGate) { if (!group.Messages.TryDequeue(out message!)) return; }
            try { message(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
    }
}
