# WorldEnvironment

`WorldEnvironment : Node` selects a borrowed [CanvasEnvironment](CanvasEnvironment.md) through its stored `Environment` property. The property defaults to null. The node belongs to the world of its nearest viewport. Viewports that share that world use the same selection.

If several nodes supply live environments, the first in depth-first scene order controls the world. Clearing, disposing or removing its environment lets the next eligible node take over. The node's configuration warnings report a missing environment or a later selection. The selector has no drawable geometry.

See [canvas glow](../components/canvas-glow.md) for the effect and the active backend profile.
