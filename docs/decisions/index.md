# Electron2D architectural decision index

Last updated: 2026-09-21

This file routes architecture work to bounded domain decision logs. Do not load every log by default: read this index, the affected log, and only cross-domain logs explicitly referenced by relevant ADRs. Current-state class, component, and domain documents remain authoritative for implemented behavior.

| Decision domain | Canonical log | ADRs |
| --- | --- | --- |
| Product architecture | [product.md](product.md) | 0001, 0002, 0004, 0012, 0017, 0021, 0027, 0030 |
| Core object and runtime | [core-object-runtime.md](core-object-runtime.md) | 0003, 0005, 0009, 0010, 0015, 0016 |
| Core configuration, data, and I/O | [core-data-io.md](core-data-io.md) | 0018, 0019, 0020, 0022 |
| Core math | [core-math.md](core-math.md) | 0024, 0025, 0026, 0029, 0032, 0033, 0034, 0035 |
| Scene | [scene.md](scene.md) | 0006, 0008, 0011, 0023, 0031 |
| Resources | [resources.md](resources.md) | 0013, 0014 |
| Localization | [localization.md](localization.md) | 0007 |
| Rendering | [rendering.md](rendering.md) | 0028 |

ADR numbers and anchors are permanent. Add a new record to the narrowest owning log, update this table, and split a log by cohesive subdomain before it exceeds 500 lines. See [ADR 0030](product.md#adr-0030).
