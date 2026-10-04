# ViewportGUIState

Last updated: 2026-10-04

Internal sealed mutable owner record in [ViewportGUIState.cs](../../src/Scene/Main/ViewportGUIState.cs). SceneTree owns one prepared state per attached viewport. It stores borrowed viewport/control references, local focus/hover/mouse/touch capture, reusable input/hover snapshots and tooltip state. Section references share payload/target/preview/result between container-connected viewports; travel/attempt state remains local. Detachment clears buffers/capture and removes map membership. Root ownership, callback scopes and capability gates are described in [embedded integration](../components/canvas-rendering.md#embedded-viewport-containers-and-gui). This type is not public API and owns no native handles. Node disposal owns tooltip/preview nodes through the existing scene paths.
