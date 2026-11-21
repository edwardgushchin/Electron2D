using System.Text;
using System.Text.RegularExpressions;

namespace Electron2D;

public sealed partial class Image
{
    private static string? SVGCustomPropertyName(string value)
    {
        var offset = 0;
        SkipCSSTrivia(value, ref offset);
        var token = SVGSelectorName.Match(value, offset);
        if (!token.Success || token.Length <= 2 || !token.ValueSpan.StartsWith("--", StringComparison.Ordinal))
            return null;

        offset += token.Length;
        SkipCSSTrivia(value, ref offset);
        return offset == value.Length ? token.Value : null;
    }

    private sealed class SVGVariables(Dictionary<string, SVGDeclaration> properties)
    {
        private const int MaximumDepth = 64;
        private const int MaximumValueLength = 16_384;
        private const int MaximumWork = 1_048_576;

        // Skip comments and quoted strings. An identifier suffix or a dimension unit
        // that happens to end in "var" cannot start a function.
        private static readonly Regex VariableToken = new(
            """/\*[\s\S]*?(?:\*/|\z)|"(?:[^"\\]|\\[\s\S])*(?:"|\z)|'(?:[^'\\]|\\[\s\S])*(?:'|\z)|(?<function>(?<![-_a-z0-9\u0080-\uFFFF\\#@])var\()""",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly Dictionary<(string Name, int Depth), string?> _computed = new();
        private readonly Dictionary<string, Dependency> _dependencies = new(StringComparer.Ordinal);
        private readonly Stack<Dependency> _pending = new();
        private int _remainingWork = MaximumWork;

        private sealed class Dependency(int index)
        {
            internal int Index { get; } = index;
            internal int LowestReachableIndex { get; set; } = index;
            internal Dependency? Component { get; set; }
            internal int Depth { get; set; }
            internal bool Invalid { get; set; }
        }

        internal string Resolve(string value)
        {
            var first = FindVariable(value, 0);
            if (!first.Success)
                return value;

            var resolved = Substitute(value, first, 0) ??
                throw new InvalidDataException("An SVG dimension contains an invalid or unresolved CSS variable.");
            var start = 0;
            SkipCSSTrivia(resolved, ref start);
            var end = start;
            var offset = start;
            while (offset < resolved.Length)
            {
                end = ++offset;
                SkipCSSTrivia(resolved, ref offset);
            }
            return resolved[start..end];
        }

        private string? Compute(string name, int depth)
        {
            Charge(0, depth);
            // An expansion that fits at one depth may exceed the limit at another.
            var key = (name, depth);
            if (_computed.TryGetValue(key, out var computed))
                return computed;
            if (!properties.TryGetValue(name, out var declaration) || declaration.Value is not { } value)
                return null;

            var node = _dependencies.TryGetValue(name, out var visited) ? visited : Visit(name, value, depth);
            Charge(0, depth + node.Component!.Depth - 1);
            computed = node.Invalid || IsUnset(value) ? null : Substitute(value, FindVariable(value, 0), depth);
            _computed[key] = computed;
            return computed;
        }

        private Dependency Visit(string name, string value, int depth)
        {
            Charge(value.Length, depth);
            var node = new Dependency(_dependencies.Count);
            _dependencies.Add(name, node);
            _pending.Push(node);

            // Tarjan's algorithm includes edges from unused fallbacks, so every
            // member of a cycle becomes invalid before any substitutions are made.
            for (var token = FindVariable(value, 0); token.Success; token = FindVariable(value, token.Index + token.Length))
            {
                var reference = ReadCall(value, token.Index + token.Length, out _, out _);
                if (reference is null || reference == name)
                {
                    node.Invalid = true;
                    continue;
                }
                if (_dependencies.TryGetValue(reference, out var neighbor))
                {
                    if (neighbor.Component is null)
                        node.LowestReachableIndex = Math.Min(node.LowestReachableIndex, neighbor.Index);
                }
                else if (properties.TryGetValue(reference, out var declaration) && declaration.Value is { } referenced)
                {
                    neighbor = Visit(reference, referenced, depth + 1);
                    node.LowestReachableIndex = Math.Min(node.LowestReachableIndex, neighbor.LowestReachableIndex);
                }

                if (neighbor?.Component is { } component)
                    node.Depth = Math.Max(node.Depth, component.Depth);
            }

            if (node.LowestReachableIndex == node.Index)
            {
                var cyclic = _pending.Peek() != node;
                var count = 0;
                var tailDepth = 0;
                Dependency member;
                do
                {
                    member = _pending.Pop();
                    member.Component = node;
                    member.Invalid |= cyclic;
                    tailDepth = Math.Max(tailDepth, member.Depth);
                    count++;
                }
                while (member != node);
                // Before a component closes, Depth is the longest outgoing chain.
                // Count each cycle member once so the bound is independent of its entry point.
                node.Depth = count + tailDepth;
            }
            return node;
        }

        private string? Substitute(string value, Match token, int depth)
        {
            if (!token.Success)
                return value;

            Charge(value.Length, depth);
            var result = new StringBuilder();
            var offset = 0;
            while (token.Success)
            {
                Append(value.AsSpan(offset, token.Index - offset));
                var name = ReadCall(value, token.Index + token.Length, out offset, out var fallback);
                if (name is null)
                    return null;

                var replacement = Compute(name, depth + 1);
                if (replacement is null && fallback is not null)
                    replacement = Substitute(fallback, FindVariable(fallback, 0), depth + 1);
                if (replacement is null)
                    return null;

                Append(replacement);
                token = FindVariable(value, offset);
            }
            Append(value.AsSpan(offset));
            return result.ToString();

            void Append(ReadOnlySpan<char> segment)
            {
                if (segment.IsEmpty)
                    return;
                // A comment preserves a token boundary without adding the whitespace
                // required by binary + or - in calc().
                var separate = result.Length > 0 && NeedsSeparator(result[^1], segment[0]);
                var length = segment.Length + (separate ? 4 : 0);
                Charge(length, depth);
                if (result.Length + length > MaximumValueLength)
                    throw LimitExceeded();
                if (separate)
                    result.Append("/**/");
                result.Append(segment);
            }
        }

        private string? ReadCall(string value, int start, out int end, out string? fallback)
        {
            end = start;
            // ReadCSSSegment may scan to the end for unbalanced input. Charge that
            // upper bound so repeated or nested argument scans also have a ceiling.
            Charge(value.Length - start, 0);
            var argument = ReadCSSSegment(value, ref end, ",)", out var delimiter);
            fallback = delimiter == ',' ? ReadCSSSegment(value, ref end, ")", out delimiter) : null;
            return delimiter == ')' ? SVGCustomPropertyName(argument) : null;
        }

        private static Match FindVariable(string value, int offset)
        {
            var token = VariableToken.Match(value, offset);
            while (token.Success && !token.Groups["function"].Success)
                token = token.NextMatch();
            return token;
        }

        private static bool NeedsSeparator(char left, char right) =>
            (IsWord(left) || left is '.' or '+' or '#' or '@' or '\\') &&
            (IsWord(right) || right is '.' or '%' or '(' or '\\') ||
            left == '/' && right == '*';

        private static bool IsWord(char value) => char.IsAsciiLetterOrDigit(value) || value is '-' or '_' || value >= '\u0080';

        private static bool IsUnset(string value) =>
            value.Equals("initial", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("inherit", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("unset", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("revert", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("revert-layer", StringComparison.OrdinalIgnoreCase);

        private void Charge(int length, int depth)
        {
            _remainingWork -= length;
            if (length > MaximumValueLength || depth > MaximumDepth || _remainingWork < 0)
                throw LimitExceeded();
        }

        private static InvalidDataException LimitExceeded() => new("SVG CSS variable substitution exceeds its limits.");
    }
}
