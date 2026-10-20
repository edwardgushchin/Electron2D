namespace Electron2D;

/// <summary>A copied shaped glyph record for custom text rendering and source inspection.</summary>
/// <param name="Start">Inclusive Unicode scalar position; minus one for ellipsis records.</param>
/// <param name="End">Exclusive scalar position; minus one for ellipsis records.</param>
/// <param name="Repeat">Number of repetitions at the same advance; zero for an inactive justification opportunity.</param>
/// <param name="Count">Cluster record count on its first glyph, zero on continuations.</param>
/// <param name="Flags">Cluster classification and native joining flags.</param>
/// <param name="Offset">Baseline-relative logical pixel offset, with positive Y downwards.</param>
/// <param name="Advance">Logical pixel advance per repetition along the buffer axis.</param>
/// <param name="FontRID">Borrowed actual font source identity; empty for objects, replacement boxes and synthetic advance-only records.</param>
/// <param name="FontSize">Logical font size.</param>
/// <param name="Index">Native glyph index, or replacement scalar for a missing glyph.</param>
/// <param name="SpanIndex">Original authored span index, or minus one for ellipsis.</param>
/// <remarks>The snapshot owns no font or native memory. Source identities may retire after the query.</remarks>
public readonly record struct TextGlyph(int Start, int End, int Repeat, int Count, TextGraphemeFlags Flags,
    Vector2 Offset, float Advance, RID FontRID, int FontSize, int Index, int SpanIndex);
