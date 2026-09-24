using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Electron2D;

internal sealed class JSONDocumentParser
{
    private static readonly double[] PowersOf10 = [10d, 100d, 1e4d, 1e8d, 1e16d, 1e32d, 1e64d, 1e128d, 1e256d];
    private readonly string _source;
    private int _position;
    private int _line;

    internal JSONDocumentParser(string source) => _source = source;

    internal int ErrorLine => _line;
    internal string ErrorMessage { get; private set; } = string.Empty;

    internal bool TryParse(out JsonNode? value)
    {
        value = null;
        if (!ReadToken(out var token) || !ReadValue(token, 0, out value)) { value = null; return false; }
        if (_position < _source.Length && (!ReadToken(out token) || token.Kind != TokenKind.End))
        {
            value = null;
            return Fail("Expected 'EOF'");
        }
        return true;
    }

    private bool ReadValue(Token token, int depth, out JsonNode? value)
    {
        value = null;
        if (depth > 1024) return Fail("JSON structure is too deep");
        switch (token.Kind)
        {
            case TokenKind.OpenObject: return ReadObject(depth + 1, out value);
            case TokenKind.OpenArray: return ReadArray(depth + 1, out value);
            case TokenKind.Identifier:
                if (token.Text == "true") value = JsonValue.Create(true);
                else if (token.Text == "false") value = JsonValue.Create(false);
                else if (token.Text != "null")
                    return Fail($"Expected 'true', 'false', or 'null', got '{token.Text}'");
                return true;
            case TokenKind.Number:
                value = double.IsFinite(token.Number)
                    ? JsonNode.Parse(token.Number.ToString("R", CultureInfo.InvariantCulture))
                    : JsonValue.Create(token.Number);
                return true;
            case TokenKind.String: value = JsonValue.Create(token.Text); return true;
            default: return Fail($"Expected value, got '{TokenName(token.Kind)}'");
        }
    }

    private bool ReadArray(int depth, out JsonNode? value)
    {
        value = null;
        var array = new JsonArray();
        var needComma = false;
        while (_position < _source.Length)
        {
            if (!ReadToken(out var token)) return false;
            if (token.Kind == TokenKind.CloseArray) { value = array; return true; }
            if (needComma)
            {
                if (token.Kind != TokenKind.Comma) return Fail("Expected ','");
                needComma = false;
                continue;
            }
            if (!ReadValue(token, depth, out var item)) return false;
            array.Add(item);
            needComma = true;
        }
        return Fail("Expected ']'");
    }

    private bool ReadObject(int depth, out JsonNode? value)
    {
        value = null;
        var result = new JsonObject();
        var atKey = true;
        var needComma = false;
        var key = string.Empty;
        while (_position < _source.Length)
        {
            if (!ReadToken(out var token)) return false;
            if (atKey)
            {
                if (token.Kind == TokenKind.CloseObject) { value = result; return true; }
                if (needComma)
                {
                    if (token.Kind != TokenKind.Comma) return Fail("Expected '}' or ','");
                    needComma = false;
                    continue;
                }
                if (token.Kind != TokenKind.String) return Fail("Expected key");
                key = token.Text;
                if (!ReadToken(out token)) return false;
                if (token.Kind != TokenKind.Colon) return Fail("Expected ':'");
                atKey = false;
            }
            else
            {
                if (!ReadValue(token, depth, out var item)) return false;
                result[key] = item;
                needComma = true;
                atKey = true;
            }
        }
        return Fail("Expected '}'");
    }

    private bool ReadToken(out Token token)
    {
        token = default;
        if (_source.Length == 0) return Fail("Unknown error getting token");
        while (_position < _source.Length)
        {
            var character = _source[_position];
            if (character == '\n') { _line++; _position++; continue; }
            if (character == '\0') { token = new(TokenKind.End); return true; }
            var kind = character switch
            {
                '{' => TokenKind.OpenObject,
                '}' => TokenKind.CloseObject,
                '[' => TokenKind.OpenArray,
                ']' => TokenKind.CloseArray,
                ':' => TokenKind.Colon,
                ',' => TokenKind.Comma,
                _ => TokenKind.End,
            };
            if (kind != TokenKind.End) { _position++; token = new(kind); return true; }
            if (character == '"') return ReadString(out token);
            if (character <= 32) { _position++; continue; }
            if (character == '-' || IsDigit(character)) return ReadNumber(out token);
            if (IsLetter(character))
            {
                var start = _position;
                while (_position < _source.Length && IsLetter(_source[_position])) _position++;
                token = new(TokenKind.Identifier, _source[start.._position]);
                return true;
            }
            return Fail("Unexpected character");
        }
        token = new(TokenKind.End);
        return true;
    }

    private bool ReadString(out Token token)
    {
        token = default;
        _position++;
        var text = new StringBuilder();
        while (_position < _source.Length)
        {
            var character = _source[_position++];
            if (character == '\0') return Fail("Unterminated string");
            if (character == '"') { token = new(TokenKind.String, text.ToString()); return true; }
            if (character != '\\')
            {
                if (character == '\n') _line++;
                text.Append(character);
                continue;
            }
            if (_position >= _source.Length || _source[_position] == '\0') return Fail("Unterminated string");
            character = _source[_position++];
            switch (character)
            {
                case 'b': text.Append('\b'); break;
                case 't': text.Append('\t'); break;
                case 'n': text.Append('\n'); break;
                case 'f': text.Append('\f'); break;
                case 'r': text.Append('\r'); break;
                case '"': case '\\': case '/': text.Append(character); break;
                case 'u':
                    if (!ReadHex(out var codePoint)) return false;
                    if (char.IsHighSurrogate((char)codePoint))
                    {
                        if (_position + 1 >= _source.Length || _source[_position] != '\\' || _source[_position + 1] != 'u')
                            return Fail("Invalid UTF-16 sequence in string, unpaired lead surrogate");
                        _position += 2;
                        if (!ReadHex(out var trail)) return false;
                        if (!char.IsLowSurrogate((char)trail))
                            return Fail("Invalid UTF-16 sequence in string, unpaired lead surrogate");
                        text.Append((char)codePoint);
                        text.Append((char)trail);
                    }
                    else if (char.IsLowSurrogate((char)codePoint))
                        return Fail("Invalid UTF-16 sequence in string, unpaired trail surrogate");
                    else text.Append((char)codePoint);
                    break;
                default: return Fail("Invalid escape sequence");
            }
        }
        return Fail("Unterminated string");
    }

    private bool ReadHex(out int value)
    {
        value = 0;
        for (var digit = 0; digit < 4; digit++)
        {
            if (_position >= _source.Length || _source[_position] == '\0') return Fail("Unterminated string");
            var character = _source[_position++];
            var hex = character switch
            {
                >= '0' and <= '9' => character - '0',
                >= 'a' and <= 'f' => character - 'a' + 10,
                >= 'A' and <= 'F' => character - 'A' + 10,
                _ => -1,
            };
            if (hex < 0) return Fail("Malformed hex constant in string");
            value = value * 16 + hex;
        }
        return true;
    }

    private bool ReadNumber(out Token token)
    {
        var start = _position;
        if (_source[_position] == '-') _position++;
        var digits = 0;
        while (_position < _source.Length && IsDigit(_source[_position])) { _position++; digits++; }
        if (_position < _source.Length && _source[_position] == '.')
        {
            _position++;
            while (_position < _source.Length && IsDigit(_source[_position])) { _position++; digits++; }
        }
        if (digits == 0) _position = start;
        else if (_position < _source.Length && (_source[_position] == 'e' || _source[_position] == 'E'))
        {
            var exponentAt = _position++;
            if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-')) _position++;
            var exponentStart = _position;
            while (_position < _source.Length && IsDigit(_source[_position])) _position++;
            if (_position == exponentStart) _position = exponentAt;
        }
        var number = digits == 0 ? 0 : ConvertNumber(_source.AsSpan(start, _position - start));
        token = new(TokenKind.Number, Number: number);
        return true;
    }

    private static double ConvertNumber(ReadOnlySpan<char> source)
    {
        var negative = source[0] == '-';
        if (negative) source = source[1..];
        var mantissaEnd = source.IndexOfAny('e', 'E');
        if (mantissaEnd < 0) mantissaEnd = source.Length;
        var mantissa = source[..mantissaEnd];
        var point = mantissa.IndexOf('.');
        var integerDigits = point < 0 ? mantissa.Length : point;
        var count = mantissa.Length - (point < 0 ? 0 : 1);
        var kept = Math.Min(count, 18);
        var fractionExponent = integerDigits - kept;
        var cursor = 0;
        var first = 0;
        for (var digit = 0; digit < Math.Max(0, kept - 9); digit++)
        {
            if (mantissa[cursor] == '.') cursor++;
            first = first * 10 + mantissa[cursor++] - '0';
        }
        var second = 0;
        for (var digit = Math.Max(0, kept - 9); digit < kept; digit++)
        {
            if (mantissa[cursor] == '.') cursor++;
            second = second * 10 + mantissa[cursor++] - '0';
        }
        var fraction = 1.0e9 * first + second;

        var exponent = (long)fractionExponent;
        if (mantissaEnd < source.Length)
        {
            var expAt = mantissaEnd + 1;
            var negativeExponent = expAt < source.Length && source[expAt] == '-';
            if (expAt < source.Length && source[expAt] is '+' or '-') expAt++;
            long explicitExponent = 0;
            while (expAt < source.Length)
            {
                explicitExponent = Math.Min(int.MaxValue, explicitExponent * 10 + source[expAt++] - '0');
            }
            exponent += negativeExponent ? -explicitExponent : explicitExponent;
        }
        var negativePower = exponent < 0;
        var remaining = Math.Min(Math.Abs(exponent), 511L);
        var power = 1d;
        for (var bit = 0; remaining != 0; remaining >>= 1, bit++)
            if ((remaining & 1) != 0) power *= PowersOf10[bit];
        fraction = negativePower ? fraction / power : fraction * power;
        return negative ? -fraction : fraction;
    }

    private bool Fail(string message) { ErrorMessage = message; return false; }
    private static bool IsDigit(char value) => value is >= '0' and <= '9';
    private static bool IsLetter(char value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    private static string TokenName(TokenKind kind) => kind switch
    {
        TokenKind.OpenObject => "'{'",
        TokenKind.CloseObject => "'}'",
        TokenKind.OpenArray => "'['",
        TokenKind.CloseArray => "']'",
        TokenKind.Identifier => "identifier",
        TokenKind.String => "string",
        TokenKind.Number => "number",
        TokenKind.Colon => "':'",
        TokenKind.Comma => "','",
        _ => "EOF",
    };

    private readonly record struct Token(TokenKind Kind, string Text = "", double Number = 0);
    private enum TokenKind { OpenObject, CloseObject, OpenArray, CloseArray, Identifier, String, Number, Colon, Comma, End }
}
