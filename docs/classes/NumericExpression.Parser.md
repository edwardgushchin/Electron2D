# NumericExpression.Parser

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/NumericExpression.cs). **Component:** [Numeric input](../components/numeric-input.md).

Private span-based bounded expression parser. It validates syntax, precedence, literal/constant/function names, arity, depth and operation budgets; failures restore the owner numeric field.

The connected SpinBox expression/locale/fresh-scene checks exercise its owner. It has no public construction or independently shipped backend. Foreign native/semantic services remain separate prerequisites.
