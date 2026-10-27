# NavigationDebugCanvas

Last updated: 2026-10-27

Constructor-owned internal Entity drawing world-space region/link/avoidance diagnostics. It borrows one typed snapshot and reuses retained geometry commands; source disposal owns it. Its internal-child role excludes it from ordinary enumeration, duplication and authored files.

Part of [navigation controls and diagnostics](../components/navigation-diagnostics.md), under [ADR 0097](../decisions/navigation.md#global-simulation-and-scene-diagnostics).
