# SpinBox.SpinBoxLineEdit

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/SpinBox.cs). **Component:** [Numeric input](../components/numeric-input.md).

Private LineEdit subclass retains numeric-parent ownership. Its GUI handler consumes field input during parent dragging; disposal remains owned by SpinBox. Normal editing/input/shaping stays in LineEdit.

The connected SpinBox expression/locale/fresh-scene checks exercise its owner. It has no public construction or independently shipped backend. Foreign native/semantic services remain separate prerequisites.
