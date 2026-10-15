using System.Globalization;

namespace Electron2D;

public partial class Node
{
    private static long _generatedChildName = 1;

    private static string NormalizeNodeName(string name) => name.AsSpan().IndexOfAny(".:@/\"%") < 0 ? name :
        string.Create(name.Length, name, static (target, source) =>
        {
            for (var i = 0; i < source.Length; i++)
                target[i] = source[i] is '.' or ':' or '@' or '/' or '"' or '%' ? '_' : source[i];
        });

    internal void RestoreSceneName(string name)
    {
        EnsureMutable();
        if (!IsValidStoredNodeName(name)) throw new InvalidDataException("Invalid stored scene node name.");
        // Stored internal names are identities, not fresh authoring inputs.
        _name = name;
        InvalidatePathCache();
    }

    private string ResolveChildName(Node child, string name, bool readable)
    {
        if (!ChildNameExists(name, child)) return name;
        if (!readable)
        {
            do { name = "@" + child.ClassName + "@" + Interlocked.Increment(ref _generatedChildName).ToString(CultureInfo.InvariantCulture); }
            while (ChildNameExists(name, child));
            return name;
        }

        var separator = ProjectSettings.GetWithOverride(ProjectSettings.NodeNameNumberSeparator) switch { 0 => "", 1 => " ", 2 => "_", _ => "-" };
        var firstDigit = name.Length;
        while (firstDigit > 0 && char.IsAsciiDigit(name[firstDigit - 1])) firstDigit--;
        var number = "";
        var prefix = name;
        if (firstDigit < name.Length && name.AsSpan(0, firstDigit).EndsWith(separator, StringComparison.Ordinal))
        {
            number = name[firstDigit..]; prefix = name[..firstDigit];
        }
        while (true)
        {
            if (number.Length == 0) { number = "2"; prefix += separator; }
            else number = IncreaseNameNumber(number);
            name = prefix + number;
            if (!ChildNameExists(name, child)) return name;
        }
    }

    private bool ChildNameExists(string name, Node except)
    {
        foreach (var child in _children)
            if (!ReferenceEquals(child, except) && string.Equals(child._name, name, StringComparison.Ordinal)) return true;
        return false;
    }

    private static string IncreaseNameNumber(string number)
    {
        var digits = number.ToCharArray();
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            if (digits[i] == '9') { digits[i] = '0'; continue; }
            digits[i]++; return new string(digits);
        }
        return "1" + new string(digits);
    }
}
