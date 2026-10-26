namespace Electron2D;

public abstract partial class MainLoop
{
    private readonly object _permissionResultsGate = new();
    private Queue<(string Permission, bool Granted)>? _permissionResults;
    private bool _permissionResultsClosed;

    internal void PostPermissionResults(ReadOnlySpan<string> permissions, ReadOnlySpan<bool> granted)
    {
        if (permissions.Length != granted.Length) throw new ArgumentException("Permission result counts differ.");
        foreach (var permission in permissions) ArgumentException.ThrowIfNullOrEmpty(permission);
        lock (_permissionResultsGate)
        {
            if (_permissionResultsClosed || IsDisposed || permissions.IsEmpty) return;
            _permissionResults ??= new();
            for (var i = 0; i < permissions.Length; i++) _permissionResults.Enqueue((permissions[i], granted[i]));
        }
    }

    private void DispatchPermissionResults()
    {
        int count;
        lock (_permissionResultsGate) count = _permissionResults?.Count ?? 0;
        for (var i = 0; i < count; i++)
        {
            (string Permission, bool Granted) result;
            lock (_permissionResultsGate)
            {
                if (_permissionResults is null || _permissionResults.Count == 0) break;
                result = _permissionResults.Dequeue();
            }
            NotifyRequestPermissionsResult(result.Permission, result.Granted);
        }
    }

    private void ClosePermissionResults()
    {
        lock (_permissionResultsGate)
        {
            _permissionResultsClosed = true;
            _permissionResults = null;
        }
    }
}
