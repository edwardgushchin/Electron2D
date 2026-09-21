# ADR 0020: Typed file access and transformed-file containers

Last updated: 2026-09-21

## Status

Accepted.

## Context

The reference `FileAccess` combines a reference-counted stream, numeric error state, `Variant` serialization, raw and virtual paths, platform attributes, compression, and encryption. Electron2D already requires managed deterministic lifetime, typed C# without `Variant`, standard exceptions, one owned assembly, directory-backed `res://`/`user://`, and no fictional methods for absent domains. It also requires real-time hot paths to avoid blocking/allocating filesystem work.

The BCL supplies robust streams, hashes, DEFLATE/GZip/Brotli, PBKDF2, and AES-GCM, but not FastLZ or Zstandard. The project has no pack mount, resource loader, SDL platform service, or accepted codec dependency.

## Decision

`FileAccess` derives directly from `ElectronObject` and owns one stream until idempotent `Close` or disposal. Exact reference enum numbers are preserved in `FileAccessMode`, `FileCompressionMode`, and `UnixPermissionFlags`. Public members use typed PascalCase C# and standard exceptions. No process-global last error exists.

Ordinary, `res://`, and `user://` paths resolve through `ProjectSettings`; the latter two remain directory-backed. Raw access uses `FileStream`. The cursor, EOF state, endian state, and individual operations are serialized by one instance lock. Compound caller sequences are explicitly not transactional.

Fixed-width binary values and Pascal strings require complete reads. Buffer reads return the available prefix. `ReadReal`/`WriteReal` use binary32 because Electron2D's 2D numeric types are single-precision; explicit double methods remain available. Text is strict UTF-8; CSV supports quoting and multiline fields. `get_var`/`store_var` have no replacement because ADR 0001 excludes a universal value boundary.

Transformed access is whole-file and seekable in memory. It validates/decrypts before publishing an instance. Dirty data is encoded and atomically persisted with the same flushed same-directory replacement helper used by `ConfigFile`. DEFLATE, GZip, and Brotli use BCL providers. FastLZ and Zstandard are recognized but rejected before side effects until a dependency is separately accepted.

Encrypted containers are Electron2D-specific and authenticated. Raw mode requires 32 bytes. Password mode uses a random 16-byte salt, 600,000 PBKDF2-HMAC-SHA-256 iterations, and a 32-byte key. AES-256-GCM uses a new 12-byte nonce and 16-byte tag per commit and authenticates the header. A public caller-supplied IV is omitted because nonce reuse would destroy GCM security.

Filesystem attributes are exposed through native BCL behavior where the reference contract defines them: hidden/read-only attributes on Apple, BSD, and Windows platforms, and all 12 Unix mode bits on non-Windows platforms. Unsupported attribute platforms throw rather than return a plausible no-op result. Extended attributes present platform-neutral names: Linux adds/removes the `user.` namespace prefix internally, macOS uses libSystem xattrs, and Windows maps names to alternate data streams.

## Consequences

- The normal C# boundary is typed, exception-based, deterministic, and integrates current virtual paths.
- Compression/encryption files are seekable but allocate proportional to decoded size and are unsuitable for real-time callbacks or very large streaming assets.
- Authentication prevents wrong credentials or modified ciphertext from exposing plaintext.
- The transformed envelope is versioned and private to Electron2D; external/reference binary compatibility is not promised.
- FastLZ/Zstandard remain visible, explicit capability gaps without inert stubs; macOS/Windows attribute backends still require native-host testing.
- Packed `res://`, `uid://`, and `pipe://` need future pack, resource-UID, and platform-pipe backends; no current API claims that directory resolution can read them.

## Reference coverage classification

Implemented: raw open modes, temporary files, close/flush/seek/resize, position/length/EOF/path state, endian numeric I/O, buffers, strict text, lines, CSV, Pascal strings, full-text snapshots, hashes, timestamps, size/existence, hidden/read-only/Unix permissions, Linux extended attributes, DEFLATE/GZip/Brotli containers, and key/password encryption.

Adapted: reference counting to managed memory and `IDisposable`; numeric error returns and static open error to exceptions; encryption to authenticated private containers; snake_case to typed PascalCase.

Dependency-blocked: FastLZ, Zstandard, packed/exported archive paths, `uid://` resource identities, and `pipe://` streams.

Permanently excluded: `Variant` read/write and a shared mutable numeric error slot.

## Rejected alternatives

- A new `Variant` or `object` serializer: contradicts ADR 0001.
- Empty methods for absent codecs/platforms: falsely claim support and delay failures.
- Vendoring codec implementations now: creates maintenance and security scope before a real asset-format requirement.
- Unaunthenticated AES/CBC compatibility: cannot reliably reject tampering or wrong credentials.
- Caller-controlled GCM nonce: makes catastrophic nonce reuse easy.
- Direct overwrite for transformed files: can destroy the previous valid destination on failure.
- Async-only or hidden worker threads: changes ownership and cancellation semantics without a scheduler requirement.

## Verification boundary

The executable harness verifies the implemented API on Linux, including native xattrs and Unix permissions. It checks invalid modes/paths, temporary prefixes/extensions/read-only ownership, every available data encoding, EOF/cursor/lifecycle behavior, virtual paths, hashing, concurrency, compression corruption, authenticated encryption failures/tampering, and atomic-commit failure cleanup. The macOS xattr and Windows alternate-stream code compiles but is not exercised on the Linux host. The harness does not prove crash durability on every filesystem, pack integration, large-file performance, or secrecy in a compromised process.

## References

- [Reference FileAccess documentation](https://docs.godotengine.org/en/stable/classes/class_fileaccess.html)
- [Reference FileAccess header](https://github.com/godotengine/godot/blob/master/core/io/file_access.h)
- [Reference FileAccess implementation](https://github.com/godotengine/godot/blob/master/core/io/file_access.cpp)
- [.NET AES-GCM](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [.NET compression streams](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression)
