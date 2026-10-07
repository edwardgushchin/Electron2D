namespace Electron2D;

public sealed partial class NavigationServer
{
    /// <summary>Queries one committed map using a captured parameter snapshot and atomically replaces the supplied result.</summary>
    /// <param name="parameters">Caller-owned typed settings; copied filter arrays remain stable during the query.</param>
    /// <param name="result">Caller-owned result receiving selected arrays and length together.</param>
    /// <param name="callback">Optional zero-argument completion invoked on this calling thread after publication, outside gates.</param>
    /// <remarks>Callbacks may mutate settings/results and run nested queries. Callback exceptions propagate after publication.
    /// Required map identity resolves at query time; invalid input or disposed objects preserve the previous result.</remarks>
    /// <exception cref="ArgumentNullException">A required object is null.</exception>
    /// <exception cref="ArgumentException">The captured map is absent, stale or of another kind.</exception>
    /// <exception cref="ObjectDisposedException">A parameter/result object is disposed.</exception>
    public static void QueryPath(NavigationPathQueryParameters parameters, NavigationPathQueryResult result, Action? callback = null)
    {
        ArgumentNullException.ThrowIfNull(parameters); ArgumentNullException.ThrowIfNull(result);
        var settings = parameters.Snapshot(); result.EnsureQueryable(); NavigationMapIteration iteration;
        lock (Shared._gate) iteration = Shared.Map(settings.Map).Iteration;
        var data = iteration.Query(settings); result.Publish(data); callback?.Invoke();
    }
    /// <summary>Returns a copied path with points removed by iterative Ramer-Douglas-Peucker simplification.</summary>
    /// <param name="path">Finite world-space points; never mutated.</param><param name="epsilon">Finite world-distance tolerance; negative values clamp to zero.</param>
    /// <returns>Independent ordered points retaining endpoints, including empty and singleton paths.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An input point or epsilon is nonfinite.</exception>
    public static Vector2[] SimplifyPath(ReadOnlySpan<Vector2> path, float epsilon)
    {
        if (!float.IsFinite(epsilon)) throw new ArgumentOutOfRangeException(nameof(epsilon));
        foreach (var point in path) if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(path));
        var indices = NavigationMapIteration.SimplifyIndices(path, Math.Max(0, epsilon)); var result = new Vector2[indices.Length];
        for (var i = 0; i < indices.Length; i++) result[i] = path[indices[i]]; return result;
    }
}
