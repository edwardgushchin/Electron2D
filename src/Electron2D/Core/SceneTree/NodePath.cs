namespace Electron2D;

public readonly struct NodePath : IEquatable<NodePath>
{
    private readonly string? _path;

    public NodePath(string path)
    {
        _path = path ?? string.Empty;
    }

    public int GetNameCount()
    {
        return GetNodeNames().Length;
    }

    public string GetName(int index)
    {
        var names = GetNodeNames();
        if (index < 0 || index >= names.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return names[index];
    }

    public int GetSubnameCount()
    {
        return GetSubnames().Length;
    }

    public string GetSubname(int index)
    {
        var subnames = GetSubnames();
        if (index < 0 || index >= subnames.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return subnames[index];
    }

    public bool IsAbsolute()
    {
        return Text.StartsWith("/", StringComparison.Ordinal);
    }

    public bool IsEmpty()
    {
        return Text.Length == 0;
    }

    public override string ToString()
    {
        return Text;
    }

    public bool Equals(NodePath other)
    {
        return string.Equals(Text, other.Text, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is NodePath other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Text);
    }

    public static bool operator ==(NodePath left, NodePath right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NodePath left, NodePath right)
    {
        return !left.Equals(right);
    }

    public static implicit operator NodePath(string path)
    {
        return new NodePath(path);
    }

    internal string[] GetNodeNames()
    {
        var nodePart = GetNodePart();
        if (IsAbsolute())
        {
            nodePart = nodePart.Length > 0 ? nodePart[1..] : string.Empty;
        }

        if (nodePart.Length == 0)
        {
            return Array.Empty<string>();
        }

        return nodePart.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private string[] GetSubnames()
    {
        var separatorIndex = Text.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex < 0 || separatorIndex == Text.Length - 1)
        {
            return Array.Empty<string>();
        }

        return Text[(separatorIndex + 1)..].Split(':', StringSplitOptions.RemoveEmptyEntries);
    }

    private string GetNodePart()
    {
        var separatorIndex = Text.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0 ? Text : Text[..separatorIndex];
    }

    private string Text => _path ?? string.Empty;
}
