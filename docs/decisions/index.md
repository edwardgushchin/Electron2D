# Electron2D architectural decision index

Last updated: 2026-10-10

This file routes architecture work to bounded domain decision documents. Read this index, the affected document, and only cross-domain documents explicitly referenced by relevant ADRs. Class, component, and domain documents remain authoritative for implemented behavior.

| Decision domain | Canonical document | ADRs |
| --- | --- | --- |
| Product architecture | [product.md](product.md) | 0001, 0002, 0004, 0012, 0017, 0021, 0027, 0030, 0045 |
| Public enum identities | [enum-identities.md](enum-identities.md) | 0051 |
| Product versioning | [versioning.md](versioning.md) | 0096 |
| Agent-native development | [agent-native.md](agent-native.md) | 0090 |
| C# scripting | [scripting.md](scripting.md) | 0091 |
| Core object and runtime | [core-object-runtime.md](core-object-runtime.md) | 0003, 0005, 0009, 0010, 0015, 0016, 0050 |
| Process-wide service API | [singleton-services.md](singleton-services.md) | 0095 |
| Core configuration, data, and I/O | [core-data-io.md](core-data-io.md) | 0018, 0019, 0020, 0022, 0048, 0049 |
| Core math | [core-math.md](core-math.md) | 0024, 0025, 0026, 0029, 0032, 0033, 0034, 0035 |
| Scene | [scene.md](scene.md) | 0006, 0008, 0011, 0023, 0031, 0036, 0037 |
| Scene animation | [scene-animation.md](scene-animation.md) | 0093 |
| Resources | [resources.md](resources.md) | 0013, 0014, 0039 |
| Localization | [localization.md](localization.md) | 0007 |
| Rendering | [rendering.md](rendering.md) | 0028, 0046, 0078, 0079, 0080, 0081, 0082, 0083 |
| Typed 2D meshes and skeletal palettes | [mesh.md](mesh.md) | 0092 |
| Tile resources and layers | [tiles.md](tiles.md) | 0101 |
| Navigation | [navigation.md](navigation.md) | 0052, 0053, 0097 |
| Physics world backends | [physics-backends.md](physics-backends.md) | 0054 |
| Physics bodies, shapes and queries | [physics.md](physics.md) | 0059, 0060, 0061, 0062, 0063, 0064, 0065, 0066, 0067, 0068, 0069, 0070, 0071, 0072, 0075 |
| Physics backend extensions | [physics-extensions.md](physics-extensions.md) | 0103 |
| Physics collision filters | [physics-filters.md](physics-filters.md) | 0102 |
| Physics contact correction | [physics-contacts.md](physics-contacts.md) | 0098 |
| Physics world activity | [physics-activity.md](physics-activity.md) | 0089 |
| Physics indexed geometry | [physics-shape-slots.md](physics-shape-slots.md) | 0088 |
| Physics joints | [physics-joints.md](physics-joints.md) | 0084, 0085, 0086, 0087 |
| Physics fields | [physics-fields.md](physics-fields.md) | 0056 |
| Physics forces | [physics-forces.md](physics-forces.md) | 0057, 0074 |
| Body parameters | [physics-mass.md](physics-mass.md) | 0073, 0076 |
| Physics monitoring | [physics-monitoring.md](physics-monitoring.md) | 0055, 0058, 0077 |
| Audio | [audio.md](audio.md) | 0047 |
| Networking | [networking.md](networking.md) | 0094 |
| Input | [input.md](input.md) | 0038 |
| Display | [display.md](display.md) | 0040, 0041, 0042, 0043, 0044 |
| Physics canvas diagnostics | [physics-debug.md](physics-debug.md) | 0100 |
| Physics pointer delivery | [physics-picking.md](physics-picking.md) | 0099 |

These documents contain current decisions, not an append-only history. Revise an active ADR in place, remove obsolete records, keep the anchors of retained ADRs stable, and update this table. Add a new record only for a distinct decision; split a document by cohesive subdomain before it exceeds 500 lines. See [ADR 0030](product.md#adr-0030).
