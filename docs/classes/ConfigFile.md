# ConfigFile

Last updated: 2026-09-21

## Declaration

- Source: [`ConfigFile.cs`](../../src/Core/IO/ConfigFile.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class ConfigFile : ElectronObject`
- Domain: [Core](../domains/core.md)
- Component: [Configuration files](../components/config-files.md)

## Responsibility and ownership

`ConfigFile` owns one in-memory, insertion-ordered, case-sensitive configuration document. Values are addressed only through [`ConfigKey<T>`](ConfigKey.Generic.md) and are stored immediately as independent compact JSON snapshots. It owns no live object references and exposes no universal value container. [`Color`](Color.md) uses a stable exact `R/G/B/A` schema, [`Rect2`](Rect2.md) uses stable nested `Position.X/Y` and `Size.X/Y`, and [`Transform2D`](Transform2D.md) uses stable nested `X`, `Y`, and `Origin` vectors rather than incidental public-member serialization.

The text format is section-oriented. Sectionless assignments precede named sections, named headers use `[section]`, assignments use `key=json`, and unsafe identifiers are JSON-quoted. Blank lines and full comment lines beginning with `;` are accepted. Comments are not retained when encoding.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `ConfigFile()` | Creates an empty live document |
| `Clear()` | Removes every in-memory section and entry without touching disk |
| `EncodeToText()` | Returns a deterministic LF-terminated snapshot; empty state returns an empty string |
| `EraseSection(string)` | Removes a required section and all entries; the empty name addresses sectionless entries |
| `EraseSectionKey<T>(ConfigKey<T>)` | Removes a required entry and removes its section when empty |
| `GetSectionKeys(string)` | Returns a read-only insertion-order snapshot; missing section throws |
| `GetSections()` | Returns a read-only insertion-order snapshot with the empty section first |
| `GetValue<T>(ConfigKey<T>)` | Returns a newly deserialized required value |
| `GetValue<T>(ConfigKey<T>, T)` | Returns a newly deserialized value or the supplied fallback only when absent |
| `HasSection(string)` | Reports whether a nonempty section record currently exists |
| `HasSectionKey<T>(ConfigKey<T>)` | Reports entry presence without decoding the stored token |
| `Load(string)` | Reads strict UTF-8 from an operating-system path, parses completely, then merges atomically |
| `LoadEncrypted(string, ReadOnlySpan<byte>)` | Authenticates/decrypts a raw-key envelope, parses completely, then merges |
| `LoadEncryptedPass(string, string)` | Derives a key from the password, authenticates/decrypts, parses completely, then merges |
| `Parse(string)` | Parses a complete in-memory document transactionally and merges it |
| `Save(string)` | Atomically replaces a path with the current UTF-8 snapshot without a BOM |
| `SaveEncrypted(string, ReadOnlySpan<byte>)` | Atomically writes an AES-256-GCM envelope using an exact 32-byte key and a new nonce |
| `SaveEncryptedPass(string, string)` | Uses a new 16-byte salt, 600,000-round PBKDF2-HMAC-SHA-256 key derivation, and a new AES-GCM nonce |
| `SetValue<T>(ConfigKey<T>, T?)` | Serializes a value immediately; a null reference removes the entry idempotently |
| `TryGetValue<T>(ConfigKey<T>, out T)` | Distinguishes absence from successful typed decoding |

Inherited identity, notification, translation, typed-property, diagnostic, and disposal APIs are documented in [`ElectronObject`](ElectronObject.md). `ConfigFile` exposes no dynamic configuration entries through `ElectronObject.GetPropertyList()`.

## Parsing and persistence

Parsing first builds a complete validated operation list. A malformed section, assignment, quoted identifier, or JSON token throws `FormatException` and leaves all existing state unchanged. Successful input is merged: matching entries are replaced, JSON `null` removes an entry, new entries append in encounter order, and existing entries not mentioned remain. Duplicate assignments resolve to the final value without moving the entry.

Normal loads require strict UTF-8. Saves encode UTF-8 without a BOM. All save variants create a unique temporary file in the destination directory, write and flush it, then move it over the target. The directory must exist. A failed write attempts to delete its temporary file while preserving the original exception.

Only ordinary operating-system paths are accepted. [`ProjectSettings`](ProjectSettings.md) resolves `res://`/`user://` before delegating persistence to this class; virtual-path policy does not leak into the generic document type.

## Encryption

Raw-key encryption requires exactly 32 bytes. Password encryption rejects empty passwords and derives a 32-byte key with PBKDF2-HMAC-SHA-256, a fresh 16-byte random salt, and 600,000 iterations. Both modes use a fresh 12-byte nonce, a 16-byte AES-GCM tag, and authenticate the complete envelope header before parsing plaintext. Derived keys, salts, and plaintext byte buffers are zeroed after use.

The versioned binary envelope belongs to Electron2D and is intentionally not claimed compatible with any external configuration implementation. Wrong modes and malformed/truncated headers throw `InvalidDataException`; wrong keys/passwords and modified authenticated data throw `CryptographicException` before configuration mutation. A platform without AES-GCM support rejects encrypted operations with `PlatformNotSupportedException` before file access or key derivation.

## Lifecycle and state transitions

```text
empty/live --SetValue or successful Parse/Load--> populated/live
populated/live --Clear or final removal--> empty/live
live --Dispose()--> disposed and cleared
```

Disposal clears all sections and entries, then completes inherited deterministic cleanup. Disposal is idempotent. Every public operation rejects a call beginning after disposal. An operation that already captured an immutable snapshot may finish file I/O while a concurrent disposal completes; no internal live references escape.

## Invariants and error behavior

- Sections and keys use ordinal, case-sensitive identity.
- The empty section is first; all other sections and entries preserve first-insertion order.
- Stored values are non-null valid JSON tokens. Null assignment/removal never leaves an empty section.
- `GetValue` returns a newly decoded value; mutable values cannot mutate stored state by alias.
- Missing required sections/entries throw `KeyNotFoundException`; fallback and try-get APIs do not.
- Incompatible stored tokens throw `InvalidDataException` and remain stored unchanged.
- Serializer failures during `SetValue` occur before mutation.
- Color serialization accepts only finite components and always writes `R`, `G`, `B`, and `A` in that order. Typed decoding rejects missing, duplicate, unknown, nonnumeric, or non-finite fields as `InvalidDataException` without changing the stored token.
- Rectangle serialization accepts only finite position and size components and writes `Position` then `Size`, each with `X` then `Y`. Typed decoding rejects missing, duplicate, unknown, nonnumeric, or non-finite fields as `InvalidDataException`; computed `End` and `Area` are excluded.
- Transform serialization accepts only finite basis and origin components and writes `X`, `Y`, then `Origin`, each with coordinate fields `X` then `Y`. Typed decoding rejects missing, duplicate, unknown, nonnumeric, or non-finite fields as `InvalidDataException`.
- File/path/permission errors use standard `System.IO` exceptions.
- The class does not translate failures into a separate numeric error enum.

## Threading

Public operations may be invoked concurrently. Document reads and mutations are protected by one private monitor; snapshots and complete merges are linearized. Parsing, JSON serialization, cryptography, and disk I/O allocate and may be expensive, so the type makes no real-time or allocation-free guarantee and should not be used from frame-critical callbacks.

## Dependencies and interactions

The class depends on `ElectronObject`, `System.Text.Json`, UTF-8/file primitives, `RandomNumberGenerator`, PBKDF2, and `AesGcm`. It does not depend on Scene, SDL, a resource loader, rendering, input, physics, scripting, or an editor. `ProjectSettings` reuses this component without changing its typed file contract.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies defaults, parameter/type rejection, scalar/vector/collection/color/rectangle/transform round trips, exact finite schemas and malformed-field failures, copy isolation, missing/default/try-get behavior, insertion order, null deletion, section cleanup, incompatible types, failed serialization rollback, comments/BOM/quoted identifiers, stable encoding, transactional parse failure, concurrent writes and disposal, strict UTF-8, merge behavior, atomic overwrite, temporary cleanup, raw-key and password encryption, random salt/nonce behavior, wrong keys/passwords/modes, tampering, malformed envelopes, and access after disposal.

There is no comment preservation, direct virtual path resolution, asynchronous or streaming I/O, external binary-envelope compatibility, or custom public serializer registry. Feature overrides and virtual paths belong to `ProjectSettings`. JSON models must be supported by the built-in serializer and should be stable data contracts rather than live engine types.

## Decisions

- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
