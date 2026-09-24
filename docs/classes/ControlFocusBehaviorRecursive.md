# ControlFocusBehaviorRecursive

Last updated: 2026-09-24

- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlFocusBehaviorRecursive`
- **Source:** [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs)
- **Used by:** [Control.FocusBehaviorRecursive](Control.md)

## Description

Selects the focus policy for a Control and its direct Control descendants. `Inherited` (0) follows the direct parent Control and permits focus at the root; `Disabled` (1) masks the stored `FocusMode` to `None`; `Enabled` (2) restores the stored mode even beneath a disabled ancestor. A non-Control parent ends inheritance. Changing the policy releases focus immediately when the current owner becomes ineligible. [ControlRecursiveBehaviorTests](../../tests/Electron2D.Tests/ControlRecursiveBehaviorTests.cs) covers these transitions and packed state. Full GUI routing has separate gaps in [Control coverage](../coverage/classes/Control.md).
