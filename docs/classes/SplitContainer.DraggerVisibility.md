# SplitContainer.DraggerVisibility

Last updated: 2026-10-01

**Declaration:** `public enum SplitContainer.DraggerVisibility` · **Source:** [SplitContainer.cs](../../src/Scene/GUI/SplitContainer.cs). Consumed by [DraggerVisibilityMode](SplitContainer.md#draggervisibilitymode).

## Enumeration descriptions

| Value | Contract |
| --- | --- |
| `Visible = 0` | Ordinary icon follows autohide/hover/drag; icon extent can enlarge separation. Default. |
| `Hidden = 1` | Ordinary icon hidden; its separation contribution and pointer dragging remain. |
| `HiddenCollapsed = 2` | Ordinary icon hidden and gap zero; minimum_grab_thickness retains a hit target. |

DraggingEnabled controls interaction independently. TouchDraggerEnabled suppresses the ordinary icon and uses its overlapping image regardless of this enum's ordinary-icon visibility. Undefined values reject before mutation. See [verification and limits](SplitContainer.md#verification-and-limitations).
