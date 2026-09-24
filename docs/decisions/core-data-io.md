# Electron2D core configuration, data, and i/o decisions

Last updated: 2026-09-24

This bounded log owns the complete architectural records for core configuration, data, and i/o. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0018](#adr-0018), [0019](#adr-0019), [0020](#adr-0020), [0022](#adr-0022), [0048](#adr-0048).

<a id="adr-0018"></a>
## ADR 0018: Typed configuration files

Last updated: 2026-09-24

### Status

Accepted; current file-access integration is added by ADR 0020 without changing the public `ConfigFile` path boundary.

### Context

The reference `ConfigFile` is a reference-counted section/key map whose values are `Variant`. It parses and emits a Variant-specific INI-like syntax, returns numeric error values for file operations, and delegates encryption and virtual paths to its file-access layer.

At the time of this decision Electron2D had accepted managed deterministic lifetime, typed C# without `Variant`/`object`/`dynamic`, ordinary exceptions, one owned assembly, and no fictional API for absent domains. It did not yet have `FileAccess`, `ProjectSettings`, or a virtual resource/user path resolver. A useful configuration layer therefore had to preserve section/key, parse/encode, merge, ordering, removal, persistence, and encryption semantics without recreating a universal value container or claiming unavailable path services. ADR 0019 subsequently added the project-settings/path layer above this unchanged boundary.

### Decision

`ConfigFile` inherits `ElectronObject` directly; managed memory replaces reference-counted lifetime. `ConfigKey<T>` binds each section/name pair to a compile-time type; section and entry names may be empty. The public boundary rejects `object`, JSON DOM nodes, delegates, and engine objects. Values are serialized immediately as compact JSON tokens with public fields enabled, allowing typed scalars, 2D numerics, collections, and stable user models without storing CLR type names or live aliases.

The text container remains section-oriented and human-readable. Safe identifiers are bare; unsafe identifiers are JSON-quoted. Values are one-line JSON. Full semicolon comment lines and a leading BOM are accepted, comments are discarded, names are ordinal/case-sensitive, and first-insertion order is preserved. Null means removal. Parse/load fully validate before one locked merge; existing unmentioned values survive, matching the reference merge behavior while removing its partial-mutation-on-parse-error risk.

File methods accept only ordinary operating-system paths. C# exceptions replace numeric errors. Saves use a unique same-directory temporary file, flush it, and replace the target. Virtual path resolution is deferred until the project/path domain exists.

Encrypted files use a small versioned Electron2D envelope. Raw mode requires a 32-byte key. Password mode uses a fresh 16-byte salt and 600,000-round PBKDF2-HMAC-SHA-256 to derive 32 bytes. Both use a fresh 12-byte nonce and AES-256-GCM with a 16-byte tag, authenticating the complete header. The format deliberately does not claim compatibility with the reference file-access encryption format.

All public operations are safe for concurrent callers through locked state transitions. JSON, cryptography, and I/O remain explicitly non-realtime work.

### API coverage

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

### Consequences

- Callers receive compile-time value types, fresh decoded values, and normal exception diagnostics. Empty entry names are encoded as quoted identifiers and round-trip through the text format.
- The same section/name can still be misdeclared through a second incompatible key; decoding detects this at runtime because portable files do not embed CLR type identity.
- Arbitrary external edits remain possible using JSON tokens, while comments are not round-tripped.
- Strong authenticated encryption and atomic replacement are available without an external package.
- Configuration work allocates and may block, so gameplay hot paths must cache values rather than read files per frame.
- `ProjectSettings` now builds typed defaults, validation, overrides, and paths above this component instead of duplicating parsing and persistence (ADR 0019).
- `FileAccess` now shares the internal atomic-replacement implementation while `ConfigFile` deliberately keeps its ordinary-path public contract (ADR 0020).

### Rejected alternatives

- `Dictionary<string, object>`, `dynamic`, or a public JSON DOM **as the configuration value store**: each recreates the rejected universal-value boundary in `ConfigFile`. This does not exclude a separate JSON document API (ADR 0048).
- A new public `Variant`-like discriminated union: it would duplicate serialization and expand every time a value type is added.
- One overload per primitive: it excludes stable typed models and creates repetitive API/code.
- Reflection-driven storage of live engine objects: it creates hidden ownership, cycles, and unstable files.
- Plain AES or CBC without authentication: it cannot reliably detect wrong keys or tampering.
- Direct overwrite of the destination: interruption could destroy the last valid configuration.
- Empty virtual-path support or external-format compatibility flags: the required domains do not exist.

### Verification boundary

Executable checks cover the public matrix, validation, stable ordering, typed round trips and isolation, transactional failures, strict UTF-8, merge and replacement behavior, concurrency, disposal, raw/password encryption, randomization, wrong credentials/modes, tampering, malformed envelopes, and temporary-file cleanup. They do not simulate process termination between flush and rename, every filesystem's durability semantics, OS-level hostile races, compromised process memory, or third-party format interoperability.

### References

- [Reference ConfigFile documentation](https://docs.godotengine.org/en/stable/classes/class_configfile.html)
- [Reference ConfigFile header](https://github.com/godotengine/godot/blob/master/core/io/config_file.h)
- [Reference ConfigFile implementation](https://github.com/godotengine/godot/blob/master/core/io/config_file.cpp)
- [OWASP password-storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [.NET AES-GCM authenticated-encryption contract](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt)

<a id="adr-0019"></a>
## ADR 0019: Typed project settings and directory-backed virtual paths

Last updated: 2026-09-24

### Status

Accepted.

### Context

The reference project-settings singleton combines a universal-value registry, defaults and editor metadata, feature-tag overrides, dirty/change notification, project/custom persistence, project discovery, resource/user path conversion, script-class discovery, and resource-pack mounting. Electron2D has already rejected `Variant`, `object`, and `dynamic` as public value boundaries, has no editor/scripting/resource-pack domains, and must not introduce inert settings for systems that do not exist. It does have a production `ConfigFile`, typed property descriptors, process Engine, and directory-backed development projects.

### Decision

`ProjectSetting<T>` is the immutable public setting identity: full name, JSON-snapshotted default, and optional typed validator. `ProjectSettings` accepts only exact registered definitions for value access. Internally heterogeneous definitions are type-erased behind private generic entries; no untyped value crosses the public boundary.

`ProjectSettings.Instance` is the non-disposable runtime registry and an Engine built-in singleton. Public constructors provide isolated disposable registries. Every registry starts with implemented application/timing, rendering, six GUI-focus input, two locale-selection, and nine pseudolocalization definitions; other domains add definitions only with executable consumers. Engine timing properties use active feature overrides from the process registry, so the settings layer is the single source rather than a duplicate bag. The typed `input/<action>` schema and atomic `InputMap` reload are owned by ADR 0038; ProjectSettings supplies exact-definition registration and a private typed group snapshot without exposing an untyped value store. The startup sampling, catalog fallback and transformation reload of localization settings are owned by ADR 0007.

Values persist through `ConfigFile`. Unknown entries survive loading. Registration validates preloaded data. The current per-registry initial value is implicit: changing it preserves the observable current value, reset removes explicit base storage, and save omits a registered base value equal to its initial value. Main load replaces the document only after complete parsing and validation; custom load merges transactionally and preserves unsaved names that existed before the merge. Re-entrant mutation from a validator is rejected during load; validators must otherwise be pure and thread-safe. Saves reorder known entries using setting order, atomically replace the destination, and clear internal unsaved tracking only on success. The public changed-setting list is instead the current coalesced notification batch and is consumed only after event delivery. Standard C# exceptions replace numeric error codes.

Feature overrides are explicit typed operations. Tags normalize to lowercase; the first matching stored override wins. Built-in active tags describe managed runtime, release/debug build, current OS, and process architecture. Custom feature changes queue one coalesced typed event for affected override owners. Engine flushes one pending event after a successful process callback; standalone hosts can call `FlushChanges()` directly.

`res://` and `user://` map to configured directories. Resolution blocks lexical `..` escape, supports reverse localization and upward project discovery, and explicitly does not claim symbolic-link or packed-filesystem security/semantics.

Script global classes, pack mounting, editor-only metadata/UI, and settings for absent domains remain documented deferrals. Three-dimensional settings are permanently excluded.

### Consequences

- Callers get compile-time value types, reusable definitions, validator-backed writes, mutable snapshot isolation, and deterministic override selection.
- Unknown custom settings can be loaded before the owning game/domain registers their type.
- Process timing configuration is persistable and feature-aware without adding allocation to warmed empty frames.
- File I/O, serializers, validators, metadata snapshots, and path conversion remain non-realtime operations.
- Directory virtual paths are useful now without pretending exported resource packs exist.
- `project.e2d` and `override.cfg` use Electron2D's `ConfigFile` format; third-party project-file compatibility is not promised.

### Rejected alternatives

- `Dictionary<string, object>`, `dynamic`, JSON DOM **as the project-setting value store**, or string-based generic conversion: these recreate the rejected universal-value boundary in `ProjectSettings`. A separate JSON document API remains allowed (ADR 0048).
- One static property per possible setting: it cannot support game-defined settings and would add inert absent-domain configuration.
- Reflection discovery of arbitrary fields/properties: it hides registration, validation, persistence ownership, and trimming behavior.
- Immediate event delivery from every setter: it permits event storms and does not match end-of-frame change observation.
- Silent empty methods for global classes or packs: their required domains do not exist.
- Treating lexical normalization as a secure filesystem sandbox: symlinks require a different threat model and OS-specific handle validation.

### Verification boundary

Executable tests cover the typed registry, exact identity, mutable snapshots, validators/rollback/re-entry, metadata/property discovery, feature precedence/normalization, unsaved/version/event semantics, persistence/overrides/order/unknown entries, path traversal and discovery, concurrency, disposal, and Engine integration/allocation. They do not verify editor behavior, archive mounts, hostile symlink races, unavailable domain settings, or every filesystem's crash durability.

### References

- [Reference ProjectSettings documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html)
- [Reference ProjectSettings header](https://github.com/godotengine/godot/blob/master/core/config/project_settings.h)
- [Reference ProjectSettings implementation](https://github.com/godotengine/godot/blob/master/core/config/project_settings.cpp)
- [ADR 0018: Typed configuration files](core-data-io.md#adr-0018)
- [ADR 0001: Typed C# without Variant](product.md#adr-0001)

<a id="adr-0020"></a>
## ADR 0020: Typed file access and transformed-file containers

Last updated: 2026-09-21

### Status

Accepted.

### Context

The reference `FileAccess` combines a reference-counted stream, numeric error state, `Variant` serialization, raw and virtual paths, platform attributes, compression, and encryption. Electron2D already requires managed deterministic lifetime, typed C# without `Variant`, standard exceptions, one owned assembly, directory-backed `res://`/`user://`, and no fictional methods for absent domains. It also requires real-time hot paths to avoid blocking/allocating filesystem work.

The BCL supplies robust streams, hashes, DEFLATE/GZip/Brotli, PBKDF2, and AES-GCM, but not FastLZ or Zstandard. The project has no pack mount, resource loader, SDL platform service, or accepted codec dependency.

### Decision

`FileAccess` derives directly from `ElectronObject` and owns one stream until idempotent `Close` or disposal. Exact reference enum numbers are preserved in `FileAccessModeFlags`, `FileCompressionMode`, and `UnixPermissionFlags`. Public members use typed PascalCase C# and standard exceptions; acronyms in method names stay fully uppercase under [ADR 0045](product.md#adr-0045). No process-global last error exists.

Ordinary, `res://`, and `user://` paths resolve through `ProjectSettings`; the latter two remain directory-backed. Raw access uses `FileStream`. The cursor, EOF state, endian state, and individual operations are serialized by one instance lock. Compound caller sequences are explicitly not transactional.

Fixed-width binary values and Pascal strings require complete reads. Buffer reads return the available prefix. `ReadReal`/`WriteReal` use binary32 because Electron2D's 2D numeric types are single-precision; explicit double methods remain available. Text is strict UTF-8; CSV supports quoting and multiline fields. `get_var`/`store_var` have no replacement because ADR 0001 excludes a universal value boundary.

Transformed access is whole-file and seekable in memory. It validates/decrypts before publishing an instance. Dirty data is encoded and atomically persisted with the same flushed same-directory replacement helper used by `ConfigFile`. DEFLATE, GZip, and Brotli use BCL providers. FastLZ and Zstandard are recognized but rejected before side effects until a dependency is separately accepted.

Encrypted containers are Electron2D-specific and authenticated. Raw mode requires 32 bytes. Password mode uses a random 16-byte salt, 600,000 PBKDF2-HMAC-SHA-256 iterations, and a 32-byte key. AES-256-GCM uses a new 12-byte nonce and 16-byte tag per commit and authenticates the header. A public caller-supplied IV is omitted because nonce reuse would destroy GCM security.

Filesystem attributes are exposed through native BCL behavior where the reference contract defines them: hidden/read-only attributes on Apple, BSD, and Windows platforms, and all 12 Unix mode bits on non-Windows platforms. Unsupported attribute platforms throw rather than return a plausible no-op result. Extended attributes present platform-neutral names: Linux adds/removes the `user.` namespace prefix internally, macOS uses libSystem xattrs, and Windows maps names to alternate data streams.

### Consequences

- The normal C# boundary is typed, exception-based, deterministic, and integrates current virtual paths.
- Compression/encryption files are seekable but allocate proportional to decoded size and are unsuitable for real-time callbacks or very large streaming assets.
- Authentication prevents wrong credentials or modified ciphertext from exposing plaintext.
- The transformed envelope is versioned and private to Electron2D; external/reference binary compatibility is not promised.
- FastLZ/Zstandard remain visible, explicit capability gaps without inert stubs; macOS/Windows attribute backends still require native-host testing.
- Packed `res://`, `uid://`, and `pipe://` need future pack, resource-UID, and platform-pipe backends; no current API claims that directory resolution can read them.

### Reference coverage classification

Implemented: raw open modes, temporary files, close/flush/seek/resize, position/length/EOF/path state, endian numeric I/O, buffers, strict text, lines, CSV, Pascal strings, full-text snapshots, hashes, timestamps, size/existence, hidden/read-only/Unix permissions, Linux extended attributes, DEFLATE/GZip/Brotli containers, and key/password encryption.

Adapted: reference counting to managed memory and `IDisposable`; numeric error returns and static open error to exceptions; encryption to authenticated private containers; snake_case to typed PascalCase.

Dependency-blocked: FastLZ, Zstandard, packed/exported archive paths, `uid://` resource identities, and `pipe://` streams.

Permanently excluded: `Variant` read/write and a shared mutable numeric error slot.

### Rejected alternatives

- A new `Variant` or `object` serializer: contradicts ADR 0001.
- Empty methods for absent codecs/platforms: falsely claim support and delay failures.
- Vendoring codec implementations now: creates maintenance and security scope before a real asset-format requirement.
- Unaunthenticated AES/CBC compatibility: cannot reliably reject tampering or wrong credentials.
- Caller-controlled GCM nonce: makes catastrophic nonce reuse easy.
- Direct overwrite for transformed files: can destroy the previous valid destination on failure.
- Async-only or hidden worker threads: changes ownership and cancellation semantics without a scheduler requirement.

### Verification boundary

The executable harness verifies the implemented API on Linux, including native xattrs and Unix permissions. It checks invalid modes/paths, temporary prefixes/extensions/read-only ownership, every available data encoding, EOF/cursor/lifecycle behavior, virtual paths, hashing, concurrency, compression corruption, authenticated encryption failures/tampering, and atomic-commit failure cleanup. The macOS xattr and Windows alternate-stream code compiles but is not exercised on the Linux host. The harness does not prove crash durability on every filesystem, pack integration, large-file performance, or secrecy in a compromised process.

### References

- [Reference FileAccess documentation](https://docs.godotengine.org/en/stable/classes/class_fileaccess.html)
- [Reference FileAccess header](https://github.com/godotengine/godot/blob/master/core/io/file_access.h)
- [Reference FileAccess implementation](https://github.com/godotengine/godot/blob/master/core/io/file_access.cpp)
- [.NET AES-GCM](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [.NET compression streams](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression)

<a id="adr-0022"></a>
## ADR 0022: Typed directory access and scoped filesystem mutation

Last updated: 2026-09-22

### Status

Accepted.

### Context

Electron2D already has typed file streams, directory-backed virtual paths, managed lifetime, and standard-exception error reporting, but no directory cursor or mutation API. The reference `DirAccess` surface combines current-directory state, streaming enumeration, static absolute helpers, temporary ownership, drive queries, links, and platform-specific filesystem identity. Reproducing its numeric errors, `RefCounted` protocol, thread-local last-open error, or packed-resource behavior would contradict accepted decisions or advertise absent domains.

The runtime target matrix also requires platform gaps to fail explicitly instead of returning plausible empty values.

### Decision

`DirAccess` is a sealed `ElectronObject` in the existing Core file-access component and `Electron2D.dll`. It has a private constructor and non-null `Open`/`CreateTemp` factories. Managed memory remains runtime-owned; `IDisposable` closes listing state and deterministically deletes non-kept temporary directories.

Standard exceptions replace numeric `Error` results. There is no `GetOpenError`, last-error slot, or nullable factory result. Sorted string collections are immutable `IReadOnlyList<string>` snapshots.

Each instance captures one of three scopes at open time: physical filesystem, project resources, or user data. Virtual instances capture their concrete root and cannot switch scope or escape it lexically, even if process project roots are later reconfigured. Two-path static operations capture both current virtual roots under one `ProjectSettings` lock before resolving either argument.

One private lock serializes current directory, include flags, listing snapshot/cursor, last-entry kind, temporary ownership, and disposal. `ListDirBegin` replaces an active listing, `GetNext` auto-closes at EOF, `ListDirEnd` is idempotent, and `ChangeDir` closes the listing after a successful transition. Streaming order remains filesystem-defined; snapshot helpers sort ordinally.

Public removal is deliberately nonrecursive and permanent. It removes one file, link/reparse point, or empty directory. Recursive deletion exists only during disposal of the exact canonical root created and owned by `CreateTemp`; directory links are removed rather than traversed.

Link creation/detection/reading is enabled only on Linux, Windows, and macOS until mobile native-host behavior is verified. Relative link sources retain native relative-link semantics and may be dangling; virtual and absolute sources are stored as physical absolute targets. Windows can require Developer Mode or elevation; detection recognizes every reparse point while reading is limited to symbolic-link and mount-point targets exposed by the runtime. `IsEquivalent` compares native file identity so symbolic and hard links are handled, using Win32 handle metadata or Unix device/inode data. `IsCaseSensitive` uses platform metadata rather than mutating the queried directory.

Drive enumeration uses a private platform snapshot: Windows logical drives with native labels, visible macOS mounts, and Linux `/dev` mounts plus home, desktop, and GTK 3 file bookmarks. Android's StorageVolume/permission semantics and iOS enumeration remain explicit host-integration gaps and throw rather than return misleading zero/empty results. Current-drive, available-space, and filesystem-type queries select the most specific visible mount root containing the current path; Windows space queries accept UNC paths directly and UNC filesystem type is reported as `Network Share`.

The current `res://` and `user://` backends remain directories. Packed/exported resource contents, import remapping, and resource-loader views are not simulated.

### Consequences

- Directory operations compose with existing file and project-path APIs without another package or abstraction layer.
- Blocking enumeration and mutation stay out of real-time hot paths.
- Individual instance calls are thread-safe, but compound caller sequences and external filesystem races remain non-transactional.
- Copy is not atomic and rename does not simulate cross-volume copy/delete.
- Native platform behavior requires native-host tests in addition to compilation.
- Future pack/resource-loader integration can provide another backend without changing current physical-directory semantics.

### Rejected alternatives

- Return numeric errors and retain a last-open error: rejected because standard exceptions already define the Core I/O contract.
- Add a public reference-count protocol: rejected because managed lifetime and deterministic disposal already own this boundary.
- Treat `Directory.Exists`/`File.Exists` as complete validation: rejected because they can hide invalid or inaccessible-path failures.
- Implement recursive public erase: rejected because it is destructive and absent from the audited stable surface.
- Compare canonical path strings for equivalence: rejected because hard links can name the same filesystem object.
- Infer case sensitivity only from the operating system: rejected because policies can vary by filesystem and directory.
- Return empty drive results on mobile: rejected because that would make a missing host/storage backend appear successful.
- Add packed-resource placeholders: rejected because no pack-mount or resource-loader domain exists.

### Verification boundary

The executable harness verifies ordinary and virtual scope behavior, listing state/filtering/concurrency, mutations, nonrecursive deletion, temporary ownership, relative/dangling link safety, Linux hard-link/symlink identity and case metadata, drive/filesystem queries, and disposal on Linux. It does not establish Windows, macOS, Android, or iOS native behavior, exported-pack behavior, crash atomicity, cross-volume rename, or hostile-filesystem confinement.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0003: ElectronObject lifetime](core-object-runtime.md#adr-0003)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](product.md#adr-0021)

<a id="adr-0048"></a>
## ADR 0048: Dedicated JSON documents with typed native conversion

Last updated: 2026-09-24

### Status

Accepted.

### Context

ADR 0001 excludes a universal engine value container. ADRs 0018 and 0019 separately exclude JSON trees as `ConfigFile` and `ProjectSettings` value stores. Neither decision excludes a document-specific JSON API. The reference JSON resource exposes parsing, diagnostics, source retention, formatting, data, and native-value conversion; its `Variant` and optional engine-object reconstruction do not fit Electron2D's typed boundary.

### Decision

`JSON : Resource` owns one `System.Text.Json.Nodes.JsonNode` tree. `Parse` accepts JSON documents, reports success as `bool`, and retains diagnostics and optional source text. `ParseString` returns a JSON tree or null. `Stringify` accepts a JSON tree with optional key ordering, indentation and floating-point precision. `FromNative<T>` and `ToNative<T>` convert explicitly selected C# types using the established typed value schemas; untyped `object` roots and engine objects are rejected. A null JSON root converts to the selected type's default, including value types. The tree is confined to this document API and is never a universal engine property or settings value. Data assignment and resource duplication copy the tree; a returned tree is live, mutable, and caller-synchronized.

The document parser follows the reference token grammar for trailing commas, raw string line breaks, whitespace, escaped Unicode pairs, structural diagnostics and 1024-level nesting. `Parse` and `ParseString` use the same path. Numeric tokens use the reference 18-digit mantissa and power-of-ten conversion before entering the document tree. Exponent text beyond the native signed-integer range is safely capped instead of relying on overflow; this is the accepted managed boundary for exceptional inputs. Parsing replaces the document and diagnostics together; source text is retained only when requested and is cleared by a parse without retention or a `Data` assignment. This deterministic state reset is the managed document adaptation. A valid JSON null and a parse failure both yield null from `ParseString`; use `Parse` when diagnostics matter. `Stringify` uses the matching fixed and Grisu2 floating formats, key ordering and document escapes; values deeper than 1024 levels emit an ellipsis and report through managed trace listeners. JSON work is allocating and stays outside real-time callbacks.

### Consequences and boundaries

- `ConfigFile` and `ProjectSettings` continue to use compile-time typed keys and never store JSON DOM nodes.
- Generic conversion never constructs an engine object from a document or interprets a serialized runtime type name.
- The resource requires no new dependency or assembly: it uses the .NET JSON library already present in the runtime.
- The applicable JSON document API is implemented under these typed and managed diagnostic adaptations; full object reconstruction remains excluded.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0018: Typed configuration files](core-data-io.md#adr-0018)
- [0019: Typed project settings](core-data-io.md#adr-0019)
- [0014: Managed Resource lifetime](resources.md#adr-0014)

<a id="adr-0049"></a>
## ADR 0049: Managed XML token cursor and character boundary

Last updated: 2026-09-24

### Status

Accepted.

### Context

The XML token reader operates on UTF-8 bytes and exposes offsets, permissive markup tokens and integer error results. Electron2D exposes this reader directly to typed C# callers, shares directory-backed path resolution with `FileAccess`, and cannot place a numeric entity above the Unicode maximum into a managed string.

### Decision

`XMLParser : ElectronObject` owns a copied byte buffer. `Open` uses the existing `FileAccess` path resolver; `OpenBuffer` copies caller bytes. Both reset the byte cursor and line counter but leave the prior token visible immediately after a successful reopen. `Read` and `Seek` return `bool` for success/EOF, while invalid calls and I/O failures use C# exceptions instead of numeric error codes. `SkipSection` traverses the token cursor until the matching closing level or EOF.

Token identities, source-order attributes, retained attributes on non-element tokens, byte offsets, line counting, permissive malformed markup and predefined/numeric entity decoding follow the pinned implementation. Getter error results remain empty strings and report through managed trace listeners. Numeric entities with a value outside the representable Unicode range remain literal text; this is the accepted managed string boundary for invalid source data. No DTD validation, external-entity resolution, document tree or streaming input is introduced.

### Consequences and verification limits

- Whole-buffer allocation and text decoding remain outside frame-critical callbacks.
- A short whitespace scan near EOF can return success without replacing the prior token; the next call reports EOF.
- Malformed UTF-8 is decoded with replacement characters while byte offsets still refer to the original input.
- The managed executable harness verifies tokens, malformed cases, offsets, seeking and section skipping. Native hosts and other platforms remain separate acceptance gates under ADR 0021.

### Related decisions

- [0003: Managed object lifetime](core-object-runtime.md#adr-0003)
- [0020: Typed file access](core-data-io.md#adr-0020)
- [0021: Current platform gate](product.md#adr-0021)
