using System.Text.RegularExpressions;

namespace Electron2D;

/// <summary>Compiles a regular expression and searches or replaces text.</summary>
/// <remarks>Patterns use PCRE2 and code-point positions. One owner serializes operations; results survive program retirement.
/// Inputs/output are bounded to 16777216 code points, with a 32 MiB native match-heap cap, native work/depth limits
/// and a five-second callout deadline. Compiled native storage retires on Clear or Dispose; per-call storage retires before return.
/// This is blocking/allocating text work outside frame hot paths.</remarks>
public sealed class RegEx : ElectronObject
{
    private readonly object _gate = new();
    private RegExProgram? _compiled;
    private string _pattern = string.Empty;

    /// <summary>Creates an uncompiled regular expression.</summary>
    public RegEx() { }

    /// <summary>Creates and compiles a regular expression.</summary>
    /// <param name="pattern">The search pattern.</param>
    /// <exception cref="ArgumentException">The pattern is invalid.</exception>
    /// <exception cref="OutOfMemoryException">Native pattern storage cannot be allocated.</exception>
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
    public int GroupCount { get { lock (_gate) { ThrowIfDisposed(); return _compiled?.GroupCount ?? 0; } } }

    /// <summary>Gets the distinct names of named capturing groups.</summary>
    /// <returns>A fresh array, or an empty array when uncompiled.</returns>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public string[] GetNames()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _compiled?.Names.Select(item => item.Name).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        }
    }

    /// <summary>Removes the compiled program, retaining the last attempted pattern text.</summary>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public void Clear() { lock (_gate) { ThrowIfDisposed(); _compiled?.Dispose(); _compiled = null; } }

    /// <summary>Compiles a pattern, replacing any previous pattern even when compilation fails.</summary>
    /// <param name="pattern">The PCRE2 regular-expression pattern.</param>
    /// <exception cref="ArgumentNullException">The pattern is null.</exception>
    /// <exception cref="ArgumentException">The pattern cannot be compiled; the exception contains diagnostics.</exception>
    /// <exception cref="OutOfMemoryException">Native pattern storage cannot be allocated.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public void Compile(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        lock (_gate)
        {
            ThrowIfDisposed();
            _compiled?.Dispose(); _compiled = null;
            _pattern = pattern;
            _compiled = RegExProgram.Compile(pattern);
        }
    }

    /// <summary>Creates and compiles a regular expression.</summary>
    /// <param name="pattern">The PCRE2 regular-expression pattern.</param>
    /// <returns>A compiled expression owned by the caller.</returns>
    /// <exception cref="ArgumentNullException">The pattern is null.</exception>
    /// <exception cref="ArgumentException">The pattern is invalid.</exception>
    /// <exception cref="OutOfMemoryException">Native pattern storage cannot be allocated.</exception>
    public static RegEx CreateFromString(string pattern) => new(pattern);

    /// <summary>Finds the first match within the specified region.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="offset">Inclusive code-point starting position.</param>
    /// <param name="end">Exclusive code-point ending position; a negative value means the full text.</param>
    /// <returns>A match owned by the caller, or null when none exists.</returns>
    /// <exception cref="ArgumentNullException">The subject is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="InvalidOperationException">A native work/depth/heap or output budget is exhausted.</exception>
    /// <exception cref="OutOfMemoryException">Native matching storage cannot be allocated.</exception>
    /// <exception cref="RegexMatchTimeoutException">The match exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public RegExMatch? Search(string subject, int offset = 0, int end = -1)
    {
        var matches = SearchCore(subject, offset, end, all: false);
        return matches.Length == 0 ? null : matches[0];
    }

    /// <summary>Finds all nonoverlapping matches within the specified region.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="offset">Inclusive code-point starting position.</param>
    /// <param name="end">Exclusive code-point ending position; a negative value means the full text.</param>
    /// <returns>New match objects in source order.</returns>
    /// <exception cref="ArgumentNullException">The subject is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="InvalidOperationException">A native work/depth/heap or output budget is exhausted.</exception>
    /// <exception cref="OutOfMemoryException">Native matching storage cannot be allocated.</exception>
    /// <exception cref="RegexMatchTimeoutException">The match exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public RegExMatch[] SearchAll(string subject, int offset = 0, int end = -1) => SearchCore(subject, offset, end, all: true);

    /// <summary>Replaces the first or every match in the region, leaving the suffix after <paramref name="end"/> intact.</summary>
    /// <param name="subject">The original text.</param>
    /// <param name="replacement">Native capture tokens such as $1 or ${name}; unset/unknown groups become empty.</param>
    /// <param name="all">Replace every nonoverlapping match when true.</param>
    /// <param name="offset">Inclusive code-point starting position.</param>
    /// <param name="end">Exclusive code-point ending position; a negative value means the full text.</param>
    /// <returns>The replaced text.</returns>
    /// <exception cref="ArgumentNullException">The subject or replacement is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="InvalidOperationException">No pattern is compiled.</exception>
    /// <exception cref="InvalidOperationException">A native work/depth/heap or output budget is exhausted.</exception>
    /// <exception cref="OutOfMemoryException">Native matching storage cannot be allocated.</exception>
    /// <exception cref="RegexMatchTimeoutException">Matching exceeds the five-second limit.</exception>
    /// <exception cref="ObjectDisposedException">The expression has been disposed.</exception>
    public string Sub(string subject, string replacement, bool all = false, int offset = 0, int end = -1)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(replacement);
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        lock (_gate)
        {
            ThrowIfDisposed(); var compiled = _compiled ?? throw new InvalidOperationException("Compile a pattern before replacing text.");
            var text = new RegExProgram.Text(subject); var limit = end < 0 ? text.Count : Math.Min(end, text.Count);
            if (offset > limit) throw new ArgumentOutOfRangeException(nameof(offset));
            return compiled.Substitute(text, offset, limit, subject, replacement, all, _pattern);
        }
    }

    private RegExMatch[] SearchCore(string subject, int offset, int end, bool all)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        lock (_gate)
        {
            ThrowIfDisposed(); var compiled = _compiled ?? throw new InvalidOperationException("Compile a pattern before searching text.");
            var text = new RegExProgram.Text(subject); var limit = end < 0 ? text.Count : Math.Min(end, text.Count);
            if (offset > limit) return [];
            var positions = new nuint[checked((compiled.GroupCount + 1) * 2)]; var result = new List<RegExMatch>();
            try
            {
                while (compiled.Search(text, offset, limit, positions, _pattern, subject))
                {
                    var match = new RegExMatch(subject, compiled, positions, text.Boundaries); result.Add(match);
                    if (!all) break;
                    offset = match.GetEnd();
                    if (match.GetStart() == offset) offset++;
                    if (offset > limit) break;
                }
                return [.. result];
            }
            catch { foreach (var match in result) match.Dispose(); throw; }
        }
    }
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try { lock (_gate) { _compiled?.Dispose(); _compiled = null; } }
        finally { base.Dispose(disposing); }
    }
}
