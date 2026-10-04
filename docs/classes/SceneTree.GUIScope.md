# SceneTree.GUIScope

Last updated: 2026-10-04

Private readonly value scope in [SceneTree.GUIState.cs](../../src/Scene/Main/SceneTree.GUIState.cs). Captures the previous ViewportGUIState and restores it on Dispose, including after callback exceptions. It owns no nodes/handles and introduces no per-use managed allocation. Synchronous direct-child dispatch uses the existing SceneTree execution guard and separate prepared snapshots. Public input reentry remains rejected. Verified through [SubViewportContainerTests](../../tests/Electron2D.Tests/SubViewportContainerTests.cs).
