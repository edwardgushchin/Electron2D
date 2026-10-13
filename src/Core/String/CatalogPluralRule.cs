using System.Globalization;

namespace Electron2D;

internal sealed class CatalogPluralRule
{
    private readonly Expression _root;
    internal int Forms { get; }
    private CatalogPluralRule(int forms, Expression root) { Forms = forms; _root = root; }
    internal int Select(long count)
    {
        var result = _root.Evaluate((ulong)count);
        if (result >= (ulong)Forms) throw new InvalidOperationException("Catalog plural expression returned an invalid form index.");
        return (int)result;
    }
    internal static CatalogPluralRule Parse(string definition)
    {
        if (definition.Length > 4096) throw new InvalidDataException("Plural definition exceeds its size budget.");
        var parts = definition.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts.Length > 3 || (parts.Length == 3 && parts[2].Length != 0)) throw new InvalidDataException("Invalid plural definition.");
        static string Value(string field, string name)
        {
            var equal = field.IndexOf('=');
            if (equal < 0 || field[..equal].Trim() != name) throw new InvalidDataException("Invalid plural definition field.");
            return field[(equal + 1)..].Trim();
        }
        if (!int.TryParse(Value(parts[0], "nplurals"), NumberStyles.None, CultureInfo.InvariantCulture, out var forms) || forms < 1 || forms > 64)
            throw new InvalidDataException("Plural form count must be between 1 and 64.");
        var parser = new Parser(Value(parts[1], "plural"));
        var root = parser.Read(0);
        parser.RequireEnd();
        return new CatalogPluralRule(forms, root);
    }
    private sealed record Expression(string Operator, ulong Value = 0, Expression? Left = null, Expression? Right = null, Expression? Third = null)
    {
        internal ulong Evaluate(ulong n)
        {
            if (Operator == "n") return n;
            if (Operator == "number") return Value;
            var left = Left!.Evaluate(n);
            return Operator switch
            {
                "!" => left == 0 ? 1UL : 0,
                "?:" => left != 0 ? Right!.Evaluate(n) : Third!.Evaluate(n),
                "&&" => left != 0 && Right!.Evaluate(n) != 0 ? 1UL : 0,
                "||" => left != 0 || Right!.Evaluate(n) != 0 ? 1UL : 0,
                "+" => unchecked(left + Right!.Evaluate(n)),
                "-" => unchecked(left - Right!.Evaluate(n)),
                "*" => unchecked(left * Right!.Evaluate(n)),
                "/" => Divide(left, Right!.Evaluate(n), false),
                "%" => Divide(left, Right!.Evaluate(n), true),
                "==" => left == Right!.Evaluate(n) ? 1UL : 0,
                "!=" => left != Right!.Evaluate(n) ? 1UL : 0,
                "<" => left < Right!.Evaluate(n) ? 1UL : 0,
                "<=" => left <= Right!.Evaluate(n) ? 1UL : 0,
                ">" => left > Right!.Evaluate(n) ? 1UL : 0,
                ">=" => left >= Right!.Evaluate(n) ? 1UL : 0,
                _ => throw new InvalidOperationException("Invalid plural expression operator.")
            };
        }
        private static ulong Divide(ulong left, ulong right, bool remainder) => right == 0 ? throw new InvalidOperationException("Catalog plural expression divides by zero.") : remainder ? left % right : left / right;
    }
    private sealed class Parser(string text)
    {
        private int _offset;
        private int _nodes;
        private int _depth;
        internal Expression Read(int precedence)
        {
            if (++_depth > 64) throw new InvalidDataException("Plural expression nesting exceeds its budget.");
            try
            {
                Expression left;
                if (Take("!")) left = Node("!", left: Read(7));
                else if (Take("(")) { left = Read(0); Require(")"); }
                else if (Take("n")) left = Node("n");
                else
                {
                    Space(); var start = _offset; while (_offset < text.Length && char.IsAsciiDigit(text[_offset])) _offset++;
                    if (start == _offset || !ulong.TryParse(text.AsSpan(start, _offset - start), NumberStyles.None, CultureInfo.InvariantCulture, out var value)) throw new InvalidDataException("Invalid plural expression operand.");
                    left = Node("number", value);
                }
                while (true)
                {
                    Space(); var (op, level) = Operator(); if (level <= precedence) break;
                    _offset += op.Length; left = Node(op, left: left, right: Read(level));
                }
                if (precedence == 0 && Take("?")) { var yes = Read(0); Require(":"); left = Node("?:", left: left, right: yes, third: Read(0)); }
                return left;
            }
            finally { _depth--; }
        }
        private (string, int) Operator()
        {
            foreach (var op in Operators) if (text.AsSpan(_offset).StartsWith(op.Item1, StringComparison.Ordinal)) return op;
            return ("", 0);
        }
        private static readonly (string, int)[] Operators = [("||", 1), ("&&", 2), ("==", 3), ("!=", 3), ("<=", 4), (">=", 4), ("<", 4), (">", 4), ("+", 5), ("-", 5), ("*", 6), ("/", 6), ("%", 6)];
        private Expression Node(string op, ulong value = 0, Expression? left = null, Expression? right = null, Expression? third = null)
        {
            if (++_nodes > 256) throw new InvalidDataException("Plural expression node budget exceeded.");
            return new Expression(op, value, left, right, third);
        }
        private void Space() { while (_offset < text.Length && char.IsWhiteSpace(text[_offset])) _offset++; }
        private bool Take(string token) { Space(); if (!text.AsSpan(_offset).StartsWith(token, StringComparison.Ordinal)) return false; _offset += token.Length; return true; }
        private void Require(string token) { if (!Take(token)) throw new InvalidDataException("Missing plural expression delimiter."); }
        internal void RequireEnd() { Space(); if (_offset != text.Length) throw new InvalidDataException("Unexpected plural expression suffix."); }
    }
}
