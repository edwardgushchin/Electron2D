# ADR 0018: Typed configuration files

Last updated: 2026-09-21

## Status

Accepted; current file-access integration is added by ADR 0020 without changing the public `ConfigFile` path boundary.

## Context

The reference `ConfigFile` is a reference-counted section/key map whose values are `Variant`. It parses and emits a Variant-specific INI-like syntax, returns numeric error values for file operations, and delegates encryption and virtual paths to its file-access layer.

At the time of this decision Electron2D had accepted managed deterministic lifetime, typed C# without `Variant`/`object`/`dynamic`, ordinary exceptions, one owned assembly, and no fictional API for absent domains. It did not yet have `FileAccess`, `ProjectSettings`, or a virtual resource/user path resolver. A useful configuration layer therefore had to preserve section/key, parse/encode, merge, ordering, removal, persistence, and encryption semantics without recreating a universal value container or claiming unavailable path services. ADR 0019 subsequently added the project-settings/path layer above this unchanged boundary.

## Decision

`ConfigFile` inherits `ElectronObject` directly; managed memory replaces reference-counted lifetime. `ConfigKey<T>` binds each section/name pair to a compile-time type. The public boundary rejects `object`, JSON DOM nodes, delegates, and engine objects. Values are serialized immediately as compact JSON tokens with public fields enabled, allowing typed scalars, 2D numerics, collections, and stable user models without storing CLR type names or live aliases.

The text container remains section-oriented and human-readable. Safe identifiers are bare; unsafe identifiers are JSON-quoted. Values are one-line JSON. Full semicolon comment lines and a leading BOM are accepted, comments are discarded, names are ordinal/case-sensitive, and first-insertion order is preserved. Null means removal. Parse/load fully validate before one locked merge; existing unmentioned values survive, matching the reference merge behavior while removing its partial-mutation-on-parse-error risk.

File methods accept only ordinary operating-system paths. C# exceptions replace numeric errors. Saves use a unique same-directory temporary file, flush it, and replace the target. Virtual path resolution is deferred until the project/path domain exists.

Encrypted files use a small versioned Electron2D envelope. Raw mode requires a 32-byte key. Password mode uses a fresh 16-byte salt and 600,000-round PBKDF2-HMAC-SHA-256 to derive 32 bytes. Both use a fresh 12-byte nonce and AES-256-GCM with a 16-byte tag, authenticating the complete header. The format deliberately does not claim compatibility with the reference file-access encryption format.

All public operations are safe for concurrent callers through locked state transitions. JSON, cryptography, and I/O remain explicitly non-realtime work.

## API coverage

| Reference member | Electron2D status |
| --- | --- |
| `clear` | `Clear()` implemented |
| `encode_to_text` | `EncodeToText()` implemented |
| `erase_section` | `EraseSection(string)` implemented with exceptions |
| `erase_section_key` | `EraseSectionKey<T>(ConfigKey<T>)` implemented |
| `get_section_keys` | `GetSectionKeys(string)` implemented |
| `get_sections` | `GetSections()` implemented |
| `get_value` | Required/fallback `GetValue<T>` and `TryGetValue<T>` implemented |
| `has_section` | `HasSection(string)` implemented |
| `has_section_key` | `HasSectionKey<T>(ConfigKey<T>)` implemented |
| `load` | `Load(string)` implemented for strict UTF-8 OS paths |
| `load_encrypted` | `LoadEncrypted(string, ReadOnlySpan<byte>)` implemented |
| `load_encrypted_pass` | `LoadEncryptedPass(string, string)` implemented |
| `parse` | `Parse(string)` implemented transactionally |
| `save` | `Save(string)` implemented with atomic replacement |
| `save_encrypted` | `SaveEncrypted(string, ReadOnlySpan<byte>)` implemented |
| `save_encrypted_pass` | `SaveEncryptedPass(string, string)` implemented |
| `set_value` | `SetValue<T>(ConfigKey<T>, T?)` implemented; null removes |
| `RefCounted` inheritance | Permanently adapted to `ElectronObject` and managed memory under ADR 0003 |
| `Variant` values | Permanently adapted to typed keys and JSON serialization under ADR 0001 |
| Numeric `Error` returns | Permanently adapted to standard C# exceptions |
| Virtual paths/file-access encryption compatibility | Direct handling remains outside `ConfigFile`; ADR 0019 adds directory-backed resolution in `ProjectSettings`, while pack/file-access compatibility remains deferred |

## Consequences

- Callers receive compile-time value types, fresh decoded values, and normal exception diagnostics.
- The same section/name can still be misdeclared through a second incompatible key; decoding detects this at runtime because portable files do not embed CLR type identity.
- Arbitrary external edits remain possible using JSON tokens, while comments are not round-tripped.
- Strong authenticated encryption and atomic replacement are available without an external package.
- Configuration work allocates and may block, so gameplay hot paths must cache values rather than read files per frame.
- `ProjectSettings` now builds typed defaults, validation, overrides, and paths above this component instead of duplicating parsing and persistence (ADR 0019).
- `FileAccess` now shares the internal atomic-replacement implementation while `ConfigFile` deliberately keeps its ordinary-path public contract (ADR 0020).

## Rejected alternatives

- `Dictionary<string, object>`, `dynamic`, or a public JSON DOM: each recreates the rejected universal-value boundary.
- A new public `Variant`-like discriminated union: it would duplicate serialization and expand every time a value type is added.
- One overload per primitive: it excludes stable typed models and creates repetitive API/code.
- Reflection-driven storage of live engine objects: it creates hidden ownership, cycles, and unstable files.
- Plain AES or CBC without authentication: it cannot reliably detect wrong keys or tampering.
- Direct overwrite of the destination: interruption could destroy the last valid configuration.
- Empty virtual-path support or external-format compatibility flags: the required domains do not exist.

## Verification boundary

Executable checks cover the public matrix, validation, stable ordering, typed round trips and isolation, transactional failures, strict UTF-8, merge and replacement behavior, concurrency, disposal, raw/password encryption, randomization, wrong credentials/modes, tampering, malformed envelopes, and temporary-file cleanup. They do not simulate process termination between flush and rename, every filesystem's durability semantics, OS-level hostile races, compromised process memory, or third-party format interoperability.

## References

- [Reference ConfigFile documentation](https://docs.godotengine.org/en/stable/classes/class_configfile.html)
- [Reference ConfigFile header](https://github.com/godotengine/godot/blob/master/core/io/config_file.h)
- [Reference ConfigFile implementation](https://github.com/godotengine/godot/blob/master/core/io/config_file.cpp)
- [OWASP password-storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [.NET AES-GCM authenticated-encryption contract](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt)
