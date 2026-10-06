# NumericExpression.Number

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/NumericExpression.cs). **Component:** [Numeric input](../components/numeric-input.md).

Private readonly integer/double evaluation value. The integer identity preserves division, bit operations and comparisons before projection to Range double; it is not an engine-wide data container.

The connected SpinBox expression/locale/fresh-scene checks exercise its owner. It has no public construction or independently shipped backend. Foreign native/semantic services remain separate prerequisites.
