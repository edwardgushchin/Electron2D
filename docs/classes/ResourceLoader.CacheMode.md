# ResourceLoader.CacheMode

Last updated: 2026-10-05

**Owner:** [ResourceLoader](ResourceLoader.md) · **Source:** [ResourceLoader.cs](../../src/Core/IO/ResourceLoader.cs)

| Value | Integer | Synchronous resource behavior |
| --- | ---: | --- |
| `Ignore` | 0 | Decode an independent root without registering its visible path |
| `Reuse` | 1 | Return a compatible live cached resource or decode and register one; default |
| `Replace` | 2 | Decode, then refresh an existing exact-type resource in place or register a new one |
| `IgnoreDeep` | 3 | Ignore root and external dependencies recursively |
| `ReplaceDeep` | 4 | Replace root and external dependencies recursively |

The numeric identities are pinned. Ordinary Ignore/Replace reuses external dependencies; Deep modes propagate. Newly decoded file graphs have root ownership and scene-instance leases; reused external resources remain borrowed. Further concrete payload schemas remain Partial on the general load/discovery contract. See [resource files](../components/resource-files.md).
