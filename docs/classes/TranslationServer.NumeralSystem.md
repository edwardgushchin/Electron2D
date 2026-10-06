# TranslationServer.NumeralSystem

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Core/String/TranslationServer.Numbers.cs). **Component:** [Numeric input](../components/numeric-input.md).

Private immutable numeric locale data: eleven rune strings for digits/decimal marker, percent sign and two exponent markers. Nine systems index exactly 92 pinned locale spellings; supplementary-plane runes stay intact.

The connected SpinBox expression/locale/fresh-scene checks exercise its owner. It has no public construction or independently shipped backend. Foreign native/semantic services remain separate prerequisites.
