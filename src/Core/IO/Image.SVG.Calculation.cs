using System.Globalization;
using System.Text.RegularExpressions;

namespace Electron2D;

public sealed partial class Image
{
    private static bool IsSVGCalculation(string? value) => value?.Contains('(') == true;

    private sealed class SVGCalculation(string expression, double percentBase)
    {
        private const int MaximumDepth = 64;
        private static readonly Regex NumberToken = new(
            @"\G(?<number>[+-]?(?:[0-9]*\.[0-9]+|[0-9]+)(?:[eE][+-]?[0-9]+)?)(?<unit>%|[a-zA-Z]*)",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        private int _offset;

        // Percentages resolve to lengths on the current axis. Keep the length exponent
        // so multiplication and division can cancel units before the final type check.
        private readonly record struct Term(double Value, int LengthPower);

        internal double Read()
        {
            var result = ReadFunction(0);
            SkipCSSTrivia(expression, ref _offset);
            if (_offset != expression.Length || result.LengthPower != 1 || result.Value <= 0)
                throw InvalidCalculation();
            return result.Value;
        }

        private Term ReadFunction(int depth)
        {
            var nameStart = _offset;
            while (char.IsAsciiLetter(Current))
                _offset++;
            var name = expression.AsSpan(nameStart, _offset - nameStart);
            Require('(');
            var first = ReadSum(depth + 1);
            var result = first;
            if (name.Equals("clamp", StringComparison.OrdinalIgnoreCase))
            {
                Require(',');
                var preferred = ReadSum(depth + 1);
                Require(',');
                var upper = ReadSum(depth + 1);
                RequireSameType(first, preferred);
                RequireSameType(first, upper);
                result = new Term(Math.Max(first.Value, Math.Min(preferred.Value, upper.Value)), first.LengthPower);
            }
            else if (name.Equals("min", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("max", StringComparison.OrdinalIgnoreCase))
            {
                var minimum = name.Equals("min", StringComparison.OrdinalIgnoreCase);
                while (Current == ',')
                {
                    _offset++;
                    var next = ReadSum(depth + 1);
                    RequireSameType(result, next);
                    if (minimum ? next.Value < result.Value : next.Value > result.Value)
                        result = next;
                }
            }
            else if (!name.Equals("calc", StringComparison.OrdinalIgnoreCase))
                throw InvalidCalculation();

            Require(')');
            return result;
        }

        private Term ReadSum(int depth)
        {
            if (depth > MaximumDepth)
                throw InvalidCalculation();

            var left = ReadProduct(depth);
            while (true)
            {
                var hasWhitespace = SkipCSSTrivia(expression, ref _offset);
                var operation = Current;
                if (operation is not ('+' or '-'))
                    return left;

                _offset++;
                if (!hasWhitespace || !SkipCSSTrivia(expression, ref _offset))
                    throw InvalidCalculation();
                var right = ReadProduct(depth);
                RequireSameType(left, right);
                left = FiniteTerm(operation == '+' ? left.Value + right.Value : left.Value - right.Value, left.LengthPower);
            }
        }

        private Term ReadProduct(int depth)
        {
            var left = ReadTerm(depth);
            while (true)
            {
                var endOfTerm = _offset;
                SkipCSSTrivia(expression, ref _offset);
                var operation = Current;
                if (operation is not ('*' or '/'))
                {
                    _offset = endOfTerm;
                    return left;
                }

                _offset++;
                var right = ReadTerm(depth);
                if (operation == '*')
                    left = FiniteTerm(left.Value * right.Value, left.LengthPower + right.LengthPower);
                else
                {
                    if (right.Value == 0)
                        throw InvalidCalculation();
                    left = FiniteTerm(left.Value / right.Value, left.LengthPower - right.LengthPower);
                }
            }
        }

        private Term ReadTerm(int depth)
        {
            SkipCSSTrivia(expression, ref _offset);
            if (Current == '(')
            {
                _offset++;
                var result = ReadSum(depth + 1);
                Require(')');
                return result;
            }
            if (char.IsAsciiLetter(Current))
                return ReadFunction(depth);

            var token = NumberToken.Match(expression, _offset);
            if (!token.Success ||
                !double.TryParse(token.Groups["number"].ValueSpan, SVGNumberStyle, CultureInfo.InvariantCulture, out var number) ||
                !double.IsFinite(number))
                throw InvalidCalculation();

            _offset += token.Length;
            var unit = token.Groups["unit"].ValueSpan;
            if (unit.IsEmpty)
                return new Term(number, 0);
            if (unit.SequenceEqual("%"))
                return FiniteTerm(number * (percentBase / 100), 1);
            foreach (var (name, factor) in SVGPhysicalUnits)
                if (unit.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return FiniteTerm(number * factor, 1);
            throw InvalidCalculation();
        }

        private char Current => _offset < expression.Length ? expression[_offset] : '\0';

        private void Require(char expected)
        {
            if (Current != expected)
                throw InvalidCalculation();
            _offset++;
        }

        private static void RequireSameType(Term left, Term right)
        {
            if (left.LengthPower != right.LengthPower)
                throw InvalidCalculation();
        }

        private static Term FiniteTerm(double value, int lengthPower)
        {
            if (!double.IsFinite(value))
                throw InvalidCalculation();
            return new Term(value, lengthPower);
        }

        private static InvalidDataException InvalidCalculation() =>
            new("SVG dimension calculations must evaluate to a positive finite length with compatible units.");
    }
}
