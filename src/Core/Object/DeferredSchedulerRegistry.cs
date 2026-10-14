using System.Runtime.CompilerServices;

namespace Electron2D;

internal interface IDeferredScheduler
{
    Action<Action> Callback { get; }
    void Prepare<T>();
    void Release<T>();
    void Enqueue<T>(Action<T> callback, T value);
}

internal interface IDeferredInvocation
{
    void Discard();
}

internal static class DeferredSchedulerRegistry
{
    private static readonly ConditionalWeakTable<object, IDeferredScheduler> Providers = new();

    internal static void Register(IDeferredScheduler provider) => Providers.Add(provider.Callback.Target!, provider);

    internal static IDeferredScheduler? Find(Action<Action>? callback) =>
        callback?.Target is { } owner && Providers.TryGetValue(owner, out var provider) && provider.Callback == callback ? provider : null;
}
