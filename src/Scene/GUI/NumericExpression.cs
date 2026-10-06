using System.Globalization;
using System.Reflection;

namespace Electron2D;

// Numeric field expressions have no object instance, member invocation or runtime scripting access.
internal static class NumericExpression
{
    private readonly record struct Number(double Real, long Integer, bool IsInteger)
    {
        internal static Number From(long value) => new(value, value, true);
        internal static Number From(double value) => new(value, 0, false);
        internal bool Truth => Real != 0;
    }
    private static readonly Lazy<RandomNumberGenerator> NumericRandom = new(static () => new RandomNumberGenerator());
    private static readonly string[] MathNames = ["sin", "cos", "tan", "sinh", "cosh", "tanh", "asin", "acos", "atan", "atan2", "asinh", "acosh", "atanh", "sqrt", "fmod", "fposmod", "posmod", "floor", "floorf", "floori", "ceil", "ceilf", "ceili", "round", "roundf", "roundi", "abs", "absf", "absi", "sign", "signf", "signi", "snapped", "snappedf", "snappedi", "pow", "log", "exp", "is_nan", "is_inf", "is_equal_approx", "is_zero_approx", "is_finite", "ease", "step_decimals", "lerp", "lerpf", "cubic_interpolate", "cubic_interpolate_angle", "cubic_interpolate_in_time", "cubic_interpolate_angle_in_time", "bezier_interpolate", "bezier_derivative", "angle_difference", "lerp_angle", "inverse_lerp", "remap", "smoothstep", "move_toward", "rotate_toward", "deg_to_rad", "rad_to_deg", "linear_to_db", "db_to_linear", "wrap", "wrapi", "wrapf", "max", "maxi", "maxf", "min", "mini", "minf", "clamp", "clampi", "clampf", "nearest_po2", "pingpong"];
    private static readonly Dictionary<string, MethodInfo[]> Functions = BuildFunctions();
    private static Dictionary<string, MethodInfo[]> BuildFunctions()
    {
        var methods = typeof(Mathf).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => !m.ContainsGenericParameters && Numeric(m.ReturnType) && m.GetParameters().All(p => Numeric(p.ParameterType))).ToArray();
        var result = new Dictionary<string, MethodInfo[]>(StringComparer.Ordinal);
        foreach (var name in MathNames)
        {
            var canonical = name is "fposmod" ? "posmod" : name is "lerpf" ? "lerp" : MathNames.Contains(name[..^1]) && (name.EndsWith('f') || name.EndsWith('i')) ? name[..^1] : name;
            var selected = methods.Where(m => m.Name.Replace("_", "").Equals(canonical.Replace("_", ""), StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.GetParameters().Count(p => p.ParameterType == typeof(double))).ToArray();
            if (selected.Length != 0) result[name] = selected;
        }
        return result;
    }
    private static bool Numeric(Type type) => type == typeof(double) || type == typeof(float) || type == typeof(long) || type == typeof(int) || type == typeof(uint) || type == typeof(ulong) || type == typeof(bool);
    internal static bool TryEvaluate(string text, out double result)
    {
        result = 0; if (text.Length > 65536) return false;
        try { var parser = new Parser(text); var value = parser.Expression(0); parser.Space(); if (!parser.End) return false; result = value.Real; return true; }
        catch (Exception error) when (error is FormatException or ArithmeticException or ArgumentException or TargetInvocationException) { return false; }
    }
    private ref struct Parser
    {
        private readonly ReadOnlySpan<char> _text;
        private int _position, _depth, _operations;
        internal Parser(string text) { _text = text; _position = _depth = _operations = 0; }
        internal bool End => _position == _text.Length;
        internal void Space() { while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++; }
        private bool Take(string value) { Space(); if (!_text[_position..].StartsWith(value, StringComparison.Ordinal)) return false; _position += value.Length; return true; }
        private bool Word(string word) { Space(); if (!_text[_position..].StartsWith(word, StringComparison.Ordinal) || _position + word.Length < _text.Length && (char.IsLetterOrDigit(_text[_position + word.Length]) || _text[_position + word.Length] == '_')) return false; _position += word.Length; return true; }
        internal Number Expression(int minimum)
        {
            if (++_depth > 128 || ++_operations > 16384) throw new FormatException("Expression limit exceeded.");
            try
            {
                var left = Primary();
                while (true)
                {
                    Space(); var before = _position; var op = Operator(out var precedence);
                    if (precedence < minimum) { _position = before; break; }
                    var right = Expression(precedence + 1); left = Apply(op, left, right);
                }
                return left;
            }
            finally { _depth--; }
        }
        private string Operator(out int precedence)
        {
            foreach (var entry in Operators) if (entry.Word ? Word(entry.Text) : Take(entry.Text)) { precedence = entry.Precedence; return entry.Text; }
            precedence = -1; return "";
        }
        private Number Primary()
        {
            if (++_depth > 128 || ++_operations > 16384) throw new FormatException("Expression limit exceeded.");
            try { return PrimaryValue(); } finally { _depth--; }
        }
        private Number PrimaryValue()
        {
            Space(); if (End) throw new FormatException("Expected a value.");
            if (Take("-")) { var n = Expression(9); return n.IsInteger ? Number.From(unchecked(-n.Integer)) : Number.From(-n.Real); }
            if (Take("+")) return Expression(9);
            if (Take("~")) { var n = Expression(10); if (!n.IsInteger) throw new FormatException("Expected an integer."); return Number.From(~n.Integer); }
            if (Take("!") || Word("not")) return Number.From(Expression(2).Truth ? 0L : 1L);
            if (Take("(")) { var n = Expression(0); if (!Take(")")) throw new FormatException("Expected a closing parenthesis."); return n; }
            if (char.IsAsciiDigit(_text[_position]) || _text[_position] == '.') return Literal();
            var start = _position; while (_position < _text.Length && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] == '_')) _position++;
            if (start == _position) throw new FormatException("Expected a number or function.");
            var name = _text[start.._position].ToString();
            if (!Take("(")) return name switch { "PI" => Number.From(Math.PI), "TAU" => Number.From(Math.Tau), "INF" => Number.From(double.PositiveInfinity), "NAN" => Number.From(double.NaN), "true" => Number.From(1L), "false" => Number.From(0L), _ => throw new FormatException("Unknown numeric constant.") };
            var args = new List<Number>(); if (!Take(")")) { do { if (args.Count == 256) throw new FormatException("Too many arguments."); args.Add(Expression(0)); } while (Take(",")); if (!Take(")")) throw new FormatException("Expected a closing parenthesis."); }
            return Call(name, args);
        }
        private Number Literal()
        {
            var start = _position;
            if (_text[_position..].StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { _position += 2; var digits = _position; while (_position < _text.Length && char.IsAsciiHexDigit(_text[_position])) _position++; return Number.From(unchecked((long)ulong.Parse(_text[digits.._position], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture))); }
            if (_text[_position..].StartsWith("0b", StringComparison.OrdinalIgnoreCase)) { _position += 2; var digits = _position; long value = 0; while (_position < _text.Length && _text[_position] is '0' or '1') value = unchecked(value * 2 + _text[_position++] - '0'); if (_position == digits) throw new FormatException("Expected binary digits."); return Number.From(value); }
            while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++;
            var real = false;
            if (_position < _text.Length && _text[_position] == '.') { real = true; _position++; while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++; }
            if (_position < _text.Length && _text[_position] is 'e' or 'E') { real = true; _position++; if (_position < _text.Length && _text[_position] is '+' or '-') _position++; while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++; }
            var text = _text[start.._position]; return real ? Number.From(double.Parse(text, CultureInfo.InvariantCulture)) : Number.From(long.Parse(text, CultureInfo.InvariantCulture));
        }
    }
    private static readonly (string Text, int Precedence, bool Word)[] Operators = [("**", 11, false), ("||", 0, false), ("or", 0, true), ("&&", 1, false), ("and", 1, true), ("==", 2, false), ("!=", 2, false), ("<=", 2, false), (">=", 2, false), ("<<", 6, false), (">>", 6, false), ("<", 2, false), (">", 2, false), ("|", 3, false), ("^", 4, false), ("&", 5, false), ("+", 7, false), ("-", 7, false), ("*", 8, false), ("/", 8, false), ("%", 8, false)];
    private static Number Apply(string op, Number a, Number b)
    {
        var integer = a.IsInteger && b.IsInteger;
        return op switch
        {
            "+" => integer ? Number.From(unchecked(a.Integer + b.Integer)) : Number.From(a.Real + b.Real),
            "-" => integer ? Number.From(unchecked(a.Integer - b.Integer)) : Number.From(a.Real - b.Real),
            "**" => integer ? Number.From(unchecked((long)Math.Pow(a.Integer, b.Integer))) : Number.From(Math.Pow(a.Real, b.Real)),
            "*" => integer ? Number.From(unchecked(a.Integer * b.Integer)) : Number.From(a.Real * b.Real),
            "/" => integer ? Number.From(a.Integer / b.Integer) : b.Real == 0 ? throw new DivideByZeroException() : Number.From(a.Real / b.Real),
            "%" when integer => Number.From(a.Integer % b.Integer),
            "<<" when integer => Number.From(a.Integer << (int)b.Integer),
            ">>" when integer => Number.From(a.Integer >> (int)b.Integer),
            "|" when integer => Number.From(a.Integer | b.Integer),
            "^" when integer => Number.From(a.Integer ^ b.Integer),
            "&" when integer => Number.From(a.Integer & b.Integer),
            "==" => Number.From((integer ? a.Integer == b.Integer : a.Real == b.Real) ? 1L : 0L),
            "!=" => Number.From((integer ? a.Integer != b.Integer : a.Real != b.Real) ? 1L : 0L),
            "<" => Number.From((integer ? a.Integer < b.Integer : a.Real < b.Real) ? 1L : 0L),
            ">" => Number.From((integer ? a.Integer > b.Integer : a.Real > b.Real) ? 1L : 0L),
            "<=" => Number.From((integer ? a.Integer <= b.Integer : a.Real <= b.Real) ? 1L : 0L),
            ">=" => Number.From((integer ? a.Integer >= b.Integer : a.Real >= b.Real) ? 1L : 0L),
            "and" or "&&" => Number.From(a.Truth && b.Truth ? 1L : 0L),
            "or" or "||" => Number.From(a.Truth || b.Truth ? 1L : 0L),
            _ => throw new FormatException("Invalid numeric operator.")
        };
    }
    private static object ConvertArgument(Number value, Type type) => type == typeof(double) ? value.Real : type == typeof(float) ? (float)value.Real : type == typeof(long) ? value.IsInteger ? value.Integer : unchecked((long)value.Real) : type == typeof(int) ? unchecked((int)(value.IsInteger ? value.Integer : (long)value.Real)) : type == typeof(uint) ? unchecked((uint)(value.IsInteger ? value.Integer : (long)value.Real)) : type == typeof(ulong) ? unchecked((ulong)(value.IsInteger ? value.Integer : (long)value.Real)) : value.Truth;
    private static Number Call(string name, List<Number> values)
    {
        if (name == "randi" && values.Count == 0) return Number.From((long)NumericRandom.Value.Randi());
        if (name == "randf" && values.Count == 0) return Number.From((double)NumericRandom.Value.Randf());
        if (name == "randi_range" && values.Count == 2) return Number.From((long)NumericRandom.Value.RandiRange((int)values[0].Real, (int)values[1].Real));
        if (name == "randf_range" && values.Count == 2) return Number.From((double)NumericRandom.Value.RandfRange((float)values[0].Real, (float)values[1].Real));
        if (name == "randfn" && values.Count == 2) return Number.From((double)NumericRandom.Value.Randfn((float)values[0].Real, (float)values[1].Real));
        if (name == "seed" && values.Count == 1) { NumericRandom.Value.Seed = unchecked((ulong)(values[0].IsInteger ? values[0].Integer : (long)values[0].Real)); throw new FormatException("The function has no numeric result."); }
        if (name == "randomize" && values.Count == 0) { NumericRandom.Value.Randomize(); throw new FormatException("The function has no numeric result."); }
        if (name is "min" or "max" or "mini" or "maxi" or "minf" or "maxf") { if (values.Count < 2 || name is not ("min" or "max") && values.Count != 2) throw new FormatException("Invalid minimum/maximum arguments."); var result = values[0]; foreach (var value in values.Skip(1)) if (name.StartsWith("min", StringComparison.Ordinal) ? value.IsInteger && result.IsInteger ? value.Integer < result.Integer : value.Real < result.Real : value.IsInteger && result.IsInteger ? value.Integer > result.Integer : value.Real > result.Real) result = value; return name.EndsWith('f') ? Number.From(result.Real) : name.EndsWith('i') ? Number.From((long)result.Real) : result; }
        if (name is "float" or "int" or "bool") { if (values.Count != 1) throw new FormatException("Expected one argument."); return name == "int" ? Number.From((long)values[0].Real) : name == "bool" ? Number.From(values[0].Truth ? 1L : 0L) : Number.From(values[0].Real); }
        if (name is "clamp" or "clampi" or "clampf") { if (values.Count != 3) throw new FormatException("Expected three arguments."); var n = values[0]; if (name == "clampi" || name == "clamp" && values.All(v => v.IsInteger)) return Number.From(Math.Clamp(n.IsInteger ? n.Integer : (long)n.Real, values[1].IsInteger ? values[1].Integer : (long)values[1].Real, values[2].IsInteger ? values[2].Integer : (long)values[2].Real)); var value = Math.Clamp(n.Real, values[1].Real, values[2].Real); return name == "clampf" ? Number.From(value) : name == "clampi" || n.IsInteger && values[1].IsInteger && values[2].IsInteger ? Number.From((long)value) : Number.From(value); }
        if (name is "abs" or "absi" && values.Count == 1 && (values[0].IsInteger || name == "absi")) { var n = values[0].IsInteger ? values[0].Integer : (long)values[0].Real; return Number.From(n < 0 ? unchecked(-n) : n); }
        if (name is "floor" or "ceil" or "round" && values.Count == 1 && values[0].IsInteger) return values[0];
        if (name == "fmod" && values.Count == 2) return Number.From(values[0].Real % values[1].Real);
        if (!Functions.TryGetValue(name, out var methods)) throw new FormatException("Unknown numeric function.");
        var selected = methods.FirstOrDefault(m => m.GetParameters().Length == values.Count && m.GetParameters().All(p => p.ParameterType != typeof(bool))) ?? throw new FormatException("Invalid numeric arguments.");
        var parameters = selected.GetParameters(); var args = new object[values.Count]; for (var i = 0; i < values.Count; i++) args[i] = ConvertArgument(values[i], parameters[i].ParameterType);
        var resultValue = selected.Invoke(null, args)!; if (name.EndsWith('i')) return Number.From(unchecked((long)Convert.ToDouble(resultValue, CultureInfo.InvariantCulture))); if (name.EndsWith('f')) return Number.From(Convert.ToDouble(resultValue, CultureInfo.InvariantCulture)); return selected.ReturnType == typeof(double) || selected.ReturnType == typeof(float) ? Number.From(Convert.ToDouble(resultValue, CultureInfo.InvariantCulture)) : Number.From(Convert.ToInt64(resultValue, CultureInfo.InvariantCulture));
    }
}
