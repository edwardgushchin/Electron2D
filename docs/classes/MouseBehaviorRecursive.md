# MouseBehaviorRecursive

Last updated: 2026-09-25

- **Namespace:** `Electron2D`
- **Declaration:** `public enum MouseBehaviorRecursive`
- **Source:** [Control.Input.cs](../../src/Scene/GUI/Control.Input.cs)
- **Used by:** [Control.MouseBehaviorRecursive](Control.md)

## Description

Selects the pointer policy for a Control and its direct Control descendants. `Inherited` (0) follows the direct parent Control and permits pointer input at the root; `Disabled` (1) masks the stored `MouseFilter` to `Ignore`; `Enabled` (2) restores the stored filter even beneath a disabled ancestor. A non-Control parent ends inheritance. Changing the policy refreshes hover immediately and invalidates an ineligible mouse capture on the next pointer event. [ControlRecursiveBehaviorTests](../../tests/Electron2D.Tests/ControlRecursiveBehaviorTests.cs) covers these transitions and packed state. Full GUI routing has separate gaps in [Control coverage](../coverage/classes/Control.md).
