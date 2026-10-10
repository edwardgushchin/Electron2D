# TileSetAtlasSource.TileAnimationMode

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileSetAtlasSource.Animation.cs](../../src/Scene/Resources/TileSetAtlasSource.Animation.cs).

## Overview

Selects Default synchronized renderer time or stable cell-specific RandomStartTimes phases. Max is the exclusive sentinel; it cannot be assigned.

## Syntax

```csharp
public enum TileSetAtlasSource.TileAnimationMode { Default = 0, RandomStartTimes = 1, Max = 2 }
```

## Behavior and verification

See [atlas animation](../components/tiles.md#atlas-frame-animation-and-padding) for retained timing, phase stability, resource storage, errors and actual checks.
