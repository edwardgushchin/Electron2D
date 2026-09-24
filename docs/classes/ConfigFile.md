# ConfigFile

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/IO/ConfigFile.cs`](../../src/Core/IO/ConfigFile.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class ConfigFile : ElectronObject`

> Stores strongly typed values in a sectioned text configuration and loads or saves them as one document.

## Description

Stores strongly typed values in a sectioned text configuration and loads or saves them as one document.

`ConfigFile` owns one in-memory, insertion-ordered, case-sensitive configuration document. Values are addressed only through [`ConfigKey<T>`](ConfigKey.Generic.md) and are stored immediately as independent compact JSON snapshots. It owns no live object references and exposes no universal value container. [`Color`](Color.md), [`Vector2`](Vector2.md), [`Vector2I`](Vector2I.md), [`Vector3`](Vector3.md), [`Vector3I`](Vector3I.md), [`Vector4`](Vector4.md), [`Vector4I`](Vector4I.md), [`Rect`](Rect.md), [`RectI`](RectI.md), and [`Transform`](Transform.md) use stable exact schemas rather than incidental public-member serialization.

The text format is section-oriented. Sectionless assignments precede named sections, named headers use `[section]`, assignments use `key=json`, and unsafe identifiers are JSON-quoted. Blank lines and full comment lines beginning with `;` are accepted. Comments are not retained when encoding.

Values use compact JSON tokens inside an INI-style section layout. Keys remain strongly typed through
[`ConfigKey`1`](ConfigKey.Generic.md); no untyped value getter or setter is exposed. All public operations are safe to invoke
concurrently and are serialized at document mutation boundaries. File operations use ordinary operating-system
paths and are unsuitable for a real-time frame callback.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var config = new ConfigFile();
var volume = new ConfigKey<float>("audio", "volume");
config.SetValue(volume, 0.75f);
```

## Constructors

| Member | Description |
| --- | --- |
| [`public ConfigFile()`](#m-electron2d-configfile-ctor) | Initializes an empty configuration document. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Clear()`](#m-electron2d-configfile-clear) | Removes every section and entry from memory. |
| [`public string EncodeToText()`](#m-electron2d-configfile-encodetotext) | Encodes the current document as sectioned UTF-8-compatible text. |
| [`public void EraseSection(string section)`](#m-electron2d-configfile-erasesection-system-string) | Removes an existing section and all entries that it contains. |
| [`public void EraseSectionKey<T>(ConfigKey<T> key)`](#m-electron2d-configfile-erasesectionkey-1-electron2d-configkey-0) | Removes an existing typed entry and removes its section when it becomes empty. |
| [`public IReadOnlyList<string> GetSectionKeys(string section)`](#m-electron2d-configfile-getsectionkeys-system-string) | Gets the entry names currently stored in a section. |
| [`public IReadOnlyList<string> GetSections()`](#m-electron2d-configfile-getsections) | Gets all section names currently present in the document. |
| [`public T GetValue<T>(ConfigKey<T> key)`](#m-electron2d-configfile-getvalue-1-electron2d-configkey-0) | Gets a required typed entry. |
| [`public T GetValue<T>(ConfigKey<T> key, T defaultValue)`](#m-electron2d-configfile-getvalue-1-electron2d-configkey-0-0) | Gets a typed entry or a caller-provided fallback when the entry is absent. |
| [`public bool HasSection(string section)`](#m-electron2d-configfile-hassection-system-string) | Determines whether a section exists. |
| [`public bool HasSectionKey<T>(ConfigKey<T> key)`](#m-electron2d-configfile-hassectionkey-1-electron2d-configkey-0) | Determines whether a typed entry exists. |
| [`public void Load(string path)`](#m-electron2d-configfile-load-system-string) | Loads and merges an unencrypted configuration document from an operating-system path. |
| [`public void LoadEncrypted(string path, ReadOnlySpan<byte> key)`](#m-electron2d-configfile-loadencrypted-system-string-system-readonlyspan-system-byte) | Loads and merges a configuration document encrypted with a 256-bit key. |
| [`public void LoadEncryptedPass(string path, string password)`](#m-electron2d-configfile-loadencryptedpass-system-string-system-string) | Loads and merges a configuration document encrypted with a password-derived key. |
| [`public void Parse(string data)`](#m-electron2d-configfile-parse-system-string) | Parses and merges an in-memory configuration document. |
| [`public void Save(string path)`](#m-electron2d-configfile-save-system-string) | Saves the current document to an unencrypted operating-system path. |
| [`public void SaveEncrypted(string path, ReadOnlySpan<byte> key)`](#m-electron2d-configfile-saveencrypted-system-string-system-readonlyspan-system-byte) | Saves the current document using authenticated AES-256-GCM encryption. |
| [`public void SaveEncryptedPass(string path, string password)`](#m-electron2d-configfile-saveencryptedpass-system-string-system-string) | Saves the current document using password-derived authenticated encryption. |
| [`public void SetValue<T>(ConfigKey<T> key, T value)`](#m-electron2d-configfile-setvalue-1-electron2d-configkey-0-0) | Assigns or removes a typed entry. |
| [`public bool TryGetValue<T>(ConfigKey<T> key, out T value)`](#m-electron2d-configfile-trygetvalue-1-electron2d-configkey-0-0-byref) | Attempts to get a typed entry. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-configfile-dispose-system-boolean) | Releases resources owned by a derived class. |

## Constructor Descriptions

<a id="m-electron2d-configfile-ctor"></a>
### `public ConfigFile()`

Initializes an empty configuration document.

## Method Descriptions

<a id="m-electron2d-configfile-clear"></a>
### `public void Clear()`

Removes every section and entry from memory.

**Exceptions**

- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** This method does not modify any file previously loaded or saved.

<a id="m-electron2d-configfile-encodetotext"></a>
### `public string EncodeToText()`

Encodes the current document as sectioned UTF-8-compatible text.

**Returns:** A snapshot using LF line endings. Values are compact JSON tokens; unsafe section and key names are JSON-quoted.
The empty document is encoded as an empty string.

**Exceptions**

- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** Comments read by [`ConfigFile.Parse(String)`](ConfigFile.md#m-electron2d-configfile-parse-system-string) are intentionally not retained.

<a id="m-electron2d-configfile-erasesection-system-string"></a>
### `public void EraseSection(string section)`

Removes an existing section and all entries that it contains.

**Parameters**

- `section`: The case-sensitive section name. An empty string identifies sectionless entries.

**Exceptions**

- `ArgumentNullException`: `section` is `null`.
- `Collections.Generic.KeyNotFoundException`: The section does not exist.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-erasesectionkey-1-electron2d-configkey-0"></a>
### `public void EraseSectionKey<T>(ConfigKey<T> key)`

Removes an existing typed entry and removes its section when it becomes empty.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `Collections.Generic.KeyNotFoundException`: The section or entry does not exist.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-getsectionkeys-system-string"></a>
### `public IReadOnlyList<string> GetSectionKeys(string section)`

Gets the entry names currently stored in a section.

**Parameters**

- `section`: The case-sensitive section name. An empty string identifies sectionless entries.

**Returns:** A read-only insertion-order snapshot of the entry names.

**Exceptions**

- `ArgumentNullException`: `section` is `null`.
- `Collections.Generic.KeyNotFoundException`: The section does not exist.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-getsections"></a>
### `public IReadOnlyList<string> GetSections()`

Gets all section names currently present in the document.

**Returns:** A read-only insertion-order snapshot. The empty section, when present, is always the first item.

**Exceptions**

- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-getvalue-1-electron2d-configkey-0"></a>
### `public T GetValue<T>(ConfigKey<T> key)`

Gets a required typed entry.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.

**Returns:** A newly deserialized value.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `Collections.Generic.KeyNotFoundException`: The entry does not exist.
- `IO.InvalidDataException`: The stored JSON token cannot be decoded as `T`.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-getvalue-1-electron2d-configkey-0-0"></a>
### `public T GetValue<T>(ConfigKey<T> key, T defaultValue)`

Gets a typed entry or a caller-provided fallback when the entry is absent.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.
- `defaultValue`: The value returned without serialization when the entry is absent.

**Returns:** The deserialized stored value, or `defaultValue`.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `IO.InvalidDataException`: The stored JSON token exists but cannot be decoded as `T`.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-hassection-system-string"></a>
### `public bool HasSection(string section)`

Determines whether a section exists.

**Parameters**

- `section`: The case-sensitive section name. An empty string identifies sectionless entries.

**Returns:** `true` when the section contains at least one entry; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `section` is `null`.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-hassectionkey-1-electron2d-configkey-0"></a>
### `public bool HasSectionKey<T>(ConfigKey<T> key)`

Determines whether a typed entry exists.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.

**Returns:** `true` when the entry exists; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** The stored token is not deserialized by this method.

<a id="m-electron2d-configfile-load-system-string"></a>
### `public void Load(string path)`

Loads and merges an unencrypted configuration document from an operating-system path.

**Parameters**

- `path`: The nonempty file path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller cannot read the file.
- `Text.DecoderFallbackException`: The file is not valid UTF-8.
- `FormatException`: The document is malformed.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** Parsing is completed before mutation. Existing entries not present in the loaded document are retained, and
matching entries are replaced atomically as one merge. Virtual resource paths are not resolved.

<a id="m-electron2d-configfile-loadencrypted-system-string-system-readonlyspan-system-byte"></a>
### `public void LoadEncrypted(string path, ReadOnlySpan<byte> key)`

Loads and merges a configuration document encrypted with a 256-bit key.

**Parameters**

- `path`: The nonempty encrypted file path.
- `key`: Exactly 32 key bytes.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid, or `key` is not 32 bytes.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller cannot read the file.
- `IO.InvalidDataException`: The encryption envelope or decrypted UTF-8 document is malformed.
- `Security.Cryptography.CryptographicException`: Authentication fails because the key is wrong or the file was modified.
- `PlatformNotSupportedException`: AES-GCM is unavailable on the current platform.
- `FormatException`: The decrypted configuration document is malformed.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** The authenticated encryption envelope must have been produced by [`ConfigFile.SaveEncrypted(String,ReadOnlySpan{Byte})`](ConfigFile.md#m-electron2d-configfile-saveencrypted-system-string-system-readonlyspan-system-byte).

<a id="m-electron2d-configfile-loadencryptedpass-system-string-system-string"></a>
### `public void LoadEncryptedPass(string path, string password)`

Loads and merges a configuration document encrypted with a password-derived key.

**Parameters**

- `path`: The nonempty encrypted file path.
- `password`: The nonempty password.

**Exceptions**

- `ArgumentNullException`: `path` or `password` is `null`.
- `ArgumentException`: `path` or `password` is empty, or the path is invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller cannot read the file.
- `IO.InvalidDataException`: The encryption envelope or decrypted UTF-8 document is malformed.
- `Security.Cryptography.CryptographicException`: Authentication fails because the password is wrong or the file was modified.
- `PlatformNotSupportedException`: AES-GCM is unavailable on the current platform.
- `FormatException`: The decrypted configuration document is malformed.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** The authenticated envelope must have been produced by [`ConfigFile.SaveEncryptedPass(String,String)`](ConfigFile.md#m-electron2d-configfile-saveencryptedpass-system-string-system-string). A per-file random salt
and PBKDF2-HMAC-SHA-256 are used before AES-256-GCM authentication and decryption.

<a id="m-electron2d-configfile-parse-system-string"></a>
### `public void Parse(string data)`

Parses and merges an in-memory configuration document.

**Parameters**

- `data`: The complete sectioned document.

**Exceptions**

- `ArgumentNullException`: `data` is `null`.
- `FormatException`: A section, assignment, identifier, or JSON value is malformed.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** Blank lines and lines whose first non-whitespace character is a semicolon are ignored. Parsing is transactional:
malformed input leaves the current document unchanged. Existing entries not mentioned by the input are retained.

<a id="m-electron2d-configfile-save-system-string"></a>
### `public void Save(string path)`

Saves the current document to an unencrypted operating-system path.

**Parameters**

- `path`: The nonempty destination path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The temporary or destination file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the destination.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** A snapshot is written to a uniquely named file in the destination directory, flushed, and moved over the target.
The destination directory must already exist. Virtual resource paths are not resolved.

<a id="m-electron2d-configfile-saveencrypted-system-string-system-readonlyspan-system-byte"></a>
### `public void SaveEncrypted(string path, ReadOnlySpan<byte> key)`

Saves the current document using authenticated AES-256-GCM encryption.

**Parameters**

- `path`: The nonempty destination path.
- `key`: Exactly 32 key bytes.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid, or `key` is not 32 bytes.
- `IO.IOException`: The temporary or destination file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the destination.
- `Security.Cryptography.CryptographicException`: Encryption cannot be completed.
- `PlatformNotSupportedException`: AES-GCM is unavailable on the current platform.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** A fresh random nonce is generated for every save. The binary envelope is Electron2D-specific.

<a id="m-electron2d-configfile-saveencryptedpass-system-string-system-string"></a>
### `public void SaveEncryptedPass(string path, string password)`

Saves the current document using password-derived authenticated encryption.

**Parameters**

- `path`: The nonempty destination path.
- `password`: The nonempty password.

**Exceptions**

- `ArgumentNullException`: `path` or `password` is `null`.
- `ArgumentException`: `path` or `password` is empty, or the path is invalid.
- `IO.IOException`: The temporary or destination file cannot be written or replaced.
- `UnauthorizedAccessException`: The caller cannot write the destination.
- `Security.Cryptography.CryptographicException`: Key derivation or encryption cannot be completed.
- `PlatformNotSupportedException`: AES-GCM is unavailable on the current platform.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** A fresh random salt and nonce are generated for every save. PBKDF2-HMAC-SHA-256 derives a 256-bit key before
AES-256-GCM encryption. The binary envelope is Electron2D-specific.

<a id="m-electron2d-configfile-setvalue-1-electron2d-configkey-0-0"></a>
### `public void SetValue<T>(ConfigKey<T> key, T value)`

Assigns or removes a typed entry.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.
- `value`: The value to serialize immediately. A `null` reference removes the entry without error when absent.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `Text.Json.JsonException`: `value` cannot be serialized as `T`.
- `NotSupportedException`: `T` has no supported JSON representation.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

**Remarks:** Serialization uses compact, case-sensitive `Text.Json` semantics and includes public fields.
The serialized snapshot is independent of later mutations to `value`.

<a id="m-electron2d-configfile-trygetvalue-1-electron2d-configkey-0-0-byref"></a>
### `public bool TryGetValue<T>(ConfigKey<T> key, out T value)`

Attempts to get a typed entry.

**Type parameters**

- `T`: The declared entry value type.

**Parameters**

- `key`: The typed section and entry identifier.
- `value`: The newly deserialized value when found; otherwise the default value of `T`.

**Returns:** `true` when the entry exists; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `key` is `null`.
- `IO.InvalidDataException`: The stored JSON token exists but cannot be decoded as `T`.
- `ObjectDisposedException`: The configuration file is disposing or disposed.

<a id="m-electron2d-configfile-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

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
- `Vector2` serialization accepts only finite components and writes `X` then `Y`; `Vector2I` writes exact 32-bit integer `X` and `Y` fields. Their typed decoders reject missing, duplicate, unknown, nonnumeric, out-of-range, or non-finite fields as applicable.
- `Vector3` serialization accepts only finite components and writes `X`, `Y`, then `Z`; `Vector3I` writes exact 32-bit integer fields in the same order. Their typed decoders reject missing, duplicate, unknown, nonnumeric, out-of-range, or non-finite fields as applicable.
- `Vector4` serialization accepts only finite components and writes `X`, `Y`, `Z`, then `W`; `Vector4I` writes exact 32-bit integer fields in the same order. Their typed decoders reject missing, duplicate, unknown, nonnumeric, out-of-range, or non-finite fields as applicable.
- Rectangle serialization accepts only finite position and size components and writes `Position` then `Size`, each with `X` then `Y`. Typed decoding rejects missing, duplicate, unknown, nonnumeric, or non-finite fields as `InvalidDataException`; computed `End` and `Area` are excluded.
- Integer rectangle serialization writes exact 32-bit `Position` then `Size` components, each with `X` then `Y`. Typed decoding rejects missing, duplicate, unknown, non-integer, or out-of-range fields as `InvalidDataException`; computed `End` and `Area` are excluded.
- Transform serialization accepts only finite basis and origin components and writes `X`, `Y`, then `Origin`, each with coordinate fields `X` then `Y`. Typed decoding rejects missing, duplicate, unknown, nonnumeric, or non-finite fields as `InvalidDataException`.
- File/path/permission errors use standard `System.IO` exceptions.
- The class does not translate failures into a separate numeric error enum.

## Threading

Public operations may be invoked concurrently. Document reads and mutations are protected by one private monitor; snapshots and complete merges are linearized. Parsing, JSON serialization, cryptography, and disk I/O allocate and may be expensive, so the type makes no real-time or allocation-free guarantee and should not be used from frame-critical callbacks.

## Dependencies and interactions

The class depends on `ElectronObject`, `System.Text.Json`, UTF-8/file primitives, `System.Security.Cryptography.RandomNumberGenerator`, PBKDF2, and `AesGcm`. It does not depend on Scene, SDL, a resource loader, rendering, input, physics, scripting, or an editor. `ProjectSettings` reuses this component without changing its typed file contract.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies defaults, parameter/type rejection, scalar/vector/collection/color/floating-rectangle/integer-rectangle/transform round trips, exact schemas for all six vector types and both rectangle types, malformed-field failures, copy isolation, missing/default/try-get behavior, insertion order, null deletion, section cleanup, incompatible types, failed serialization rollback, comments/BOM/quoted identifiers, stable encoding, transactional parse failure, concurrent writes and disposal, strict UTF-8, merge behavior, atomic overwrite, temporary cleanup, raw-key and password encryption, random salt/nonce behavior, wrong keys/passwords/modes, tampering, malformed envelopes, and access after disposal.

The in-memory state audit also checks empty entry names, quoted-text round trips, replacement without reordering, stable enumeration snapshots, last-key section removal, sectionless priority, reinsertion order and caller-owned fallback values. Those nine own state members are Implemented in [coverage](../coverage/classes/ConfigFile.md); the class aggregate and text/file members retain their separate Partial audits.

There is no comment preservation, direct virtual path resolution, asynchronous or streaming I/O, external binary-envelope compatibility, or custom public serializer registry. Feature overrides and virtual paths belong to `ProjectSettings`. JSON models must be supported by the built-in serializer and should be stable data contracts rather than live engine types.

## Decisions

- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
