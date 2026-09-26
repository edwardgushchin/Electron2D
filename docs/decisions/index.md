# Electron2D architectural decision index

Last updated: 2026-09-26

This file routes architecture work to bounded domain decision documents. Read this index, the affected document, and only cross-domain documents explicitly referenced by relevant ADRs. Class, component, and domain documents remain authoritative for implemented behavior.

| Decision domain | Canonical document | ADRs |
| --- | --- | --- |
| Product architecture | [product.md](product.md) | 0001, 0002, 0004, 0012, 0017, 0021, 0027, 0030, 0045, 0051 |
| Core object and runtime | [core-object-runtime.md](core-object-runtime.md) | 0003, 0005, 0009, 0010, 0015, 0016, 0050 |
| Core configuration, data, and I/O | [core-data-io.md](core-data-io.md) | 0018, 0019, 0020, 0022, 0048, 0049 |
| Core math | [core-math.md](core-math.md) | 0024, 0025, 0026, 0029, 0032, 0033, 0034, 0035 |
| Scene | [scene.md](scene.md) | 0006, 0008, 0011, 0023, 0031, 0036, 0037 |
| Resources | [resources.md](resources.md) | 0013, 0014, 0039 |
| Localization | [localization.md](localization.md) | 0007 |
| Rendering | [rendering.md](rendering.md) | 0028, 0046 |
| Navigation | [navigation.md](navigation.md) | 0052, 0053 |
| Physics | [physics.md](physics.md) | 0054, 0056, 0057, 0059, 0060, 0061, 0062, 0063, 0064, 0065, 0066, 0067, 0068, 0069, 0070, 0071, 0072 |
| Physics mass | [physics-mass.md](physics-mass.md) | 0073 |
| Physics monitoring | [physics-monitoring.md](physics-monitoring.md) | 0055, 0058 |
| Audio | [audio.md](audio.md) | 0047 |
| Input | [input.md](input.md) | 0038 |
| Display | [display.md](display.md) | 0040, 0041, 0042, 0043, 0044 |

These documents contain current decisions, not an append-only history. Revise an active ADR in place, remove obsolete records, keep the anchors of retained ADRs stable, and update this table. Add a new record only for a distinct decision; split a document by cohesive subdomain before it exceeds 500 lines. See [ADR 0030](product.md#adr-0030).
