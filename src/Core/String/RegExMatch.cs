using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using DotNetRegex = System.Text.RegularExpressions.Regex;

namespace Electron2D;

/// <summary>Stores one immutable regular-expression match and its capturing groups.</summary>
public sealed class RegExMatch : ElectronObject
{
    private readonly (int Start, int End, string Value)[] _groups;
    private readonly IReadOnlyDictionary<string, int> _names;
    private readonly string _subject;

    /// <summary>Creates an empty match result.</summary>
    public RegExMatch()
    {
        _subject = string.Empty;
        _groups = [];
        _names = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(StringComparer.Ordinal));
    }

    internal RegExMatch(string subject, DotNetRegex expression, Match match)
    {
        _subject = subject;
        _groups = new (int, int, string)[match.Groups.Count];
        for (var index = 0; index < _groups.Length; index++)
        {
            var group = match.Groups[index];
            _groups[index] = group.Success ? (group.Index, group.Index + group.Length, group.Value) : (-1, -1, string.Empty);
        }
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var name in expression.GetGroupNames())
        {
            if (int.TryParse(name, out _)) continue;
            var index = expression.GroupNumberFromName(name);
            if (index >= 0 && index < _groups.Length && _groups[index].Start >= 0)
                names[name] = index;
        }
        _names = new ReadOnlyDictionary<string, int>(names);
    }

    /// <summary>Gets the complete original text searched by the expression.</summary>
    /// <value>The text supplied to the search operation.</value>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public string Subject { get { ThrowIfDisposed(); return _subject; } }

    /// <summary>Gets matched named groups and their numeric group indices.</summary>
    /// <value>Only successfully matched names, in a read-only snapshot.</value>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public IReadOnlyDictionary<string, int> Names { get { ThrowIfDisposed(); return _names; } }

    /// <summary>Gets the whole match followed by each capturing group's text.</summary>
    /// <value>A fresh array; unmatched groups are empty strings.</value>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public string[] Strings { get { ThrowIfDisposed(); return _groups.Select(group => group.Value).ToArray(); } }

    /// <summary>Gets the number of capturing groups, excluding the whole match.</summary>
    /// <value>The fixed group count of this result.</value>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GroupCount { get { ThrowIfDisposed(); return Math.Max(0, _groups.Length - 1); } }

    /// <summary>Gets the inclusive start of a numbered group, or -1 when absent.</summary>
    /// <param name="group">Group zero is the whole match.</param>
    /// <returns>The start position in <see cref="Subject"/>, or -1.</returns>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetStart(int group = 0) { ThrowIfDisposed(); return group >= 0 && group < _groups.Length ? _groups[group].Start : -1; }

    /// <summary>Gets the inclusive start of a named group, or -1 when absent.</summary>
    /// <param name="name">The group name.</param>
    /// <returns>The start position in <see cref="Subject"/>, or -1.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetStart(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _names.TryGetValue(name, out var index) ? GetStart(index) : -1; }

    /// <summary>Gets the exclusive end of a numbered group, or -1 when absent.</summary>
    /// <param name="group">Group zero is the whole match.</param>
    /// <returns>The end position in <see cref="Subject"/>, or -1.</returns>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetEnd(int group = 0) { ThrowIfDisposed(); return group >= 0 && group < _groups.Length ? _groups[group].End : -1; }

    /// <summary>Gets the exclusive end of a named group, or -1 when absent.</summary>
    /// <param name="name">The group name.</param>
    /// <returns>The end position in <see cref="Subject"/>, or -1.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetEnd(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _names.TryGetValue(name, out var index) ? GetEnd(index) : -1; }

    /// <summary>Gets the text of a numbered group, or an empty string when absent.</summary>
    /// <param name="group">Group zero is the whole match.</param>
    /// <returns>The captured text.</returns>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public string GetString(int group = 0) { ThrowIfDisposed(); return group >= 0 && group < _groups.Length ? _groups[group].Value : string.Empty; }

    /// <summary>Gets the text of a named group, or an empty string when absent.</summary>
    /// <param name="name">The group name.</param>
    /// <returns>The captured text.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public string GetString(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _names.TryGetValue(name, out var index) ? GetString(index) : string.Empty; }
}
