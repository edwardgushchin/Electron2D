# ResourceLoader.CacheMode

Last updated: 2026-09-24

**Owner:** [ResourceLoader](ResourceLoader.md) · **Source:** [ResourceLoader.cs](../../src/Core/IO/ResourceLoader.cs)

| Value | Integer | Synchronous image-texture behavior |
| --- | ---: | --- |
| `Ignore` | 0 | Decode an independent texture without registering its visible path |
| `Reuse` | 1 | Return a compatible live cached texture or decode and register one; default |
| `Replace` | 2 | Decode, then refresh an existing texture in place or register a new one |
| `IgnoreDeep` | 3 | Same as Ignore for this dependency-free image format |
| `ReplaceDeep` | 4 | Same as Replace for this dependency-free image format |

The numeric identities are pinned. Cache behavior for resource dependency trees awaits additional concrete file formats and remains Partial on the general [`Load<TResource>`](ResourceLoader.md#loadtresource) contract.
