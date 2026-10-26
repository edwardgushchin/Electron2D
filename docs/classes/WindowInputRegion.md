# WindowInputRegion

**Declaration:** `internal sealed partial class WindowInputRegion`

**Source:** `src/Servers/Display/WindowInputRegion*.cs`

Borrows one live native window/display identity and publishes its prepared mouse
input region for DisplayServer. It validates finite vertices, retains polygon and
geometry preparation, and owns no scene state. Native X11/Windows regions have
bounded temporary ownership. macOS polling runs on the display owner thread.

On Windows, an owner-thread native subclass implements transparent hit testing.
Its GC handle belongs to that subclass and is freed on successful removal or
native destruction. Renderer-created native identity replacement retires the old
adapter before preparing the new one. Unsupported video drivers reject use.

See [native input regions](../components/window-input-region.md) for coordinates,
platform behavior, allocation boundaries and verification limits. There is no
public type or native handle ownership transfer to consumers.

Private XPoint/Point/NativeRect records describe the platform ABI only. They have
no public identity or scene storage.
