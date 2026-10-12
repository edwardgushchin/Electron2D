# SurfaceTool.SkinWeightCount

Last updated: 2026-10-12

Selects influence width in [SurfaceTool](SurfaceTool.md). This is a mode enum; its values are not literal influence counts.

| Complete signature | Contract |
| --- | --- |
| `public const SurfaceTool.SkinWeightCount Four = 0` | Four retained influences per vertex; the default. |
| `public const SurfaceTool.SkinWeightCount Eight = 1` | Eight retained influences per vertex. |

Begin preserves the chosen mode; Clear resets Four, bulk import infers actual storage and changing captured width rejects. The [mesh component](../components/meshes.md#typed-surface-authoring) records real storage/consumer checks and limits.
