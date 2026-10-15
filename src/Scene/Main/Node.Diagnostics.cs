namespace Electron2D;

public partial class Node
{
#if DEBUG
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Node, object> DiagnosticNodes = new();
    private static readonly object DiagnosticNodeMarker = new();

    private static List<Node> CaptureOrphanNodes()
    {
        var nodes = new List<Node>();
        foreach (var entry in DiagnosticNodes)
            if (!entry.Key.IsDisposed && !entry.Key.IsInsideTree) nodes.Add(entry.Key);
        nodes.Sort(static (first, second) => first.InstanceID.CompareTo(second.InstanceID));
        return nodes;
    }
#endif

    /// <summary>Returns the instance identifiers of live nodes outside an active scene tree.</summary>
    /// <returns>A caller-owned array sorted by instance identifier; an empty array in Release builds.</returns>
    /// <remarks>
    /// Debug builds include detached roots and descendants, including internal children, independently of ownership
    /// and physics attachment. Disposing and collected nodes are omitted. The diagnostic registry does not keep
    /// nodes alive. Concurrent construction, tree transitions and disposal may occur between observations;
    /// this cold diagnostic is not an atomic scene snapshot and does not inspect numerical state.
    /// </remarks>
    public static ulong[] GetOrphanNodeIDs()
    {
#if DEBUG
        var nodes = CaptureOrphanNodes();
        var ids = new ulong[nodes.Count];
        for (var i = 0; i < ids.Length; i++) ids[i] = nodes[i].InstanceID;
        return ids;
#else
        return [];
#endif
    }

    /// <summary>Writes live detached nodes to the normal runtime diagnostic output.</summary>
    /// <remarks>
    /// Debug builds report each instance identifier, root-relative hierarchy name and compiled class name.
    /// Rows are captured before logging callbacks run and are ordered by instance identifier. Release builds
    /// emit nothing. Like <see cref="GetOrphanNodeIDs"/>, this is a cold nearby observation, not an atomic snapshot.
    /// </remarks>
    public static void PrintOrphanNodes()
    {
#if DEBUG
        var nodes = CaptureOrphanNodes();
        var rows = new string[nodes.Count];
        for (var i = 0; i < rows.Length; i++)
        {
            var node = nodes[i];
            var names = new Stack<string>();
            for (Node? current = node; current is not null; current = current.Parent) names.Push(current._name);
            rows[i] = $"{node.InstanceID} - Stray Node: {string.Join('/', names)} (Type: {node.ClassName})";
        }
        foreach (var row in rows) OS.LogMessage(row);
#endif
    }
}
