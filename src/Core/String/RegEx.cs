using System.Text.RegularExpressions;
using DotNetRegex = System.Text.RegularExpressions.Regex;

namespace Electron2D;

/// <summary>Compiles a regular expression and searches or replaces text.</summary>
/// <remarks>Patterns use the .NET regular-expression dialect. Matches retain their own result after this object is cleared.</remarks>
public sealed class RegEx : ElectronObject
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly object _gate = new();
    private DotNetRegex? _compiled;
    private string _pattern = string.Empty;

    /// <summary>Creates an uncompiled regular expression.</summary>
    public RegEx() { }

    /// <summary>Creates and compiles a regular expression.</summary>
    /// <param name="pattern">The search pattern.</param>
    /// <exception cref="ArgumentException">The pattern is invalid.</exception>
    public RegEx(string pattern) => Compile(pattern);

    /// <summary>Gets the last pattern supplied to <see cref="Compile"/>.</summary>
    /// <value>The pattern, or an empty string before compilation.</value>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public string Pattern { get { lock (_gate) { ThrowIfDisposed(); return _pattern; } } }

    /// <summary>Gets whether a compiled pattern is available.</summary>
    /// <value>True after successful compilation and before <see cref="Clear"/>.</value>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public bool IsValid { get { lock (_gate) { ThrowIfDisposed(); return _compiled is not null; } } }

    /// <summary>Gets the number of capturing groups, excluding the whole match.</summary>
    /// <value>Zero when uncompiled.</value>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public int GroupCount { get { lock (_gate) { ThrowIfDisposed(); return _compiled?.GetGroupNumbers().Length - 1 ?? 0; } } }

    /// <summary>Gets the distinct names of named capturing groups.</summary>
    /// <returns>A fresh array, or an empty array when uncompiled.</returns>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public string[] GetNames()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _compiled?.GetGroupNames().Where(name => !int.TryParse(name, out _)).ToArray() ?? [];
        }
    }

    /// <summary>Removes the compiled pattern and resets its text.</summary>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public void Clear() { lock (_gate) { ThrowIfDisposed(); _compiled = null; _pattern = string.Empty; } }

    /// <summary>Compiles a pattern, replacing any previous pattern even when compilation fails.</summary>
    /// <param name="pattern">The .NET regular-expression pattern.</param>
    /// <exception cref="ArgumentNullException">The pattern is null.</exception>
    /// <exception cref="ArgumentException">The pattern cannot be compiled; the exception contains diagnostics.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public void Compile(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        lock (_gate)
        {
            ThrowIfDisposed();
            _compiled = null;
            _pattern = pattern;
            _compiled = new DotNetRegex(pattern, RegexOptions.CultureInvariant, Timeout);
        }
    }

    /// <summary>Creates and compiles a regular expression.</summary>
    /// <param name="pattern">The .NET regular-expression pattern.</param>
    /// <returns>A compiled expression owned by the caller.</returns>
    /// <exception cref="ArgumentNullException">The pattern is null.</exception>
    /// <exception cref="ArgumentException">The pattern is invalid.</exception>
    public static RegEx CreateFromString(string pattern) => new(pattern);

    /// <summary>Finds the first match within the specified region.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="offset">Inclusive starting position.</param>
    /// <param name="end">Exclusive ending position; a negative value means the full text.</param>
    /// <returns>A match owned by the caller, or null when none exists.</returns>
    /// <exception cref="ArgumentNullException">The subject is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="RegexMatchTimeoutException">The match exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public RegExMatch? Search(string subject, int offset = 0, int end = -1)
    {
        var matches = SearchCore(subject, offset, end, all: false);
        return matches.Length == 0 ? null : matches[0];
    }

    /// <summary>Finds all nonoverlapping matches within the specified region.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="offset">Inclusive starting position.</param>
    /// <param name="end">Exclusive ending position; a negative value means the full text.</param>
    /// <returns>New match objects in source order.</returns>
    /// <exception cref="ArgumentNullException">The subject is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="RegexMatchTimeoutException">The match exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public RegExMatch[] SearchAll(string subject, int offset = 0, int end = -1) => SearchCore(subject, offset, end, all: true);

    /// <summary>Replaces the first or every match in the region, leaving the suffix after <paramref name="end"/> intact.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="replacement">Replacement text using .NET capture syntax such as <c>$1</c> or <c>${name}</c>.</param>
    /// <param name="all">Replace every nonoverlapping match when true.</param>
    /// <param name="offset">Inclusive starting position.</param>
    /// <param name="end">Exclusive ending position; a negative value means the full text.</param>
    /// <returns>The replaced text.</returns>
    /// <exception cref="ArgumentNullException">The subject or replacement is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="RegexMatchTimeoutException">Matching exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public string Sub(string subject, string replacement, bool all = false, int offset = 0, int end = -1)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(replacement);
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        DotNetRegex compiled;
        lock (_gate) { ThrowIfDisposed(); compiled = _compiled ?? throw new InvalidOperationException("Compile a pattern before replacing text."); }
        var limit = end < 0 ? subject.Length : Math.Min(end, subject.Length);
        if (offset > limit) return subject;
        var prefix = subject[..limit];
        return compiled.Replace(prefix, replacement, all ? int.MaxValue : 1, offset) + subject[limit..];
    }

    private RegExMatch[] SearchCore(string subject, int offset, int end, bool all)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        DotNetRegex compiled;
        lock (_gate) { ThrowIfDisposed(); compiled = _compiled ?? throw new InvalidOperationException("Compile a pattern before searching text."); }
        var limit = end < 0 ? subject.Length : Math.Min(end, subject.Length);
        if (offset > limit) return [];
        var result = new List<RegExMatch>();
        try
        {
            var match = compiled.Match(subject[..limit], offset);
            while (match.Success)
            {
                result.Add(new RegExMatch(subject, compiled, match));
                if (!all) break;
                match = match.NextMatch();
            }
            return [.. result];
        }
        catch
        {
            foreach (var item in result) item.Dispose();
            throw;
        }
    }
}
