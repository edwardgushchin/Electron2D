namespace Electron2D;

internal sealed partial class TextLayout
{
    private void DecodeSimpleBoundaries()
    {
        _breaks.AsSpan(0, _count + 1).Clear(); _wordBoundaries.AsSpan(0, _count + 1).Clear(); _wordEnds.AsSpan(0, _count + 1).Clear();
        for (var i = 0; i < _count; i++)
        {
            _grapheme[i] = true;
            _nonprinting[i] = TextCase.SimpleNonprinting(_scalars[i]);
            var scalar = _scalars[i]; var space = NativeTextBreak.StringWhitespace(scalar);
            var punctuation = TextCase.SimplePunctuation(_scalars[i]);
            _breaks[i + 1] = space && scalar is not (0xa0 or 0x202f or 0x2060 or 0x2007) || IsHardBreak(scalar) || scalar == 0xad;
            _wordBoundaries[i] = i == 0 || space || punctuation || IsSpace(_scalars[i - 1]);
            _wordEnds[i + 1] = i + 1 == _count || space || punctuation || IsSpace(_scalars[i + 1]);
        }
        _grapheme[_count] = true;
    }
}
