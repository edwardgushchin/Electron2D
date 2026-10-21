# PCKPacker

Last updated: 2026-10-15

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/IO/PCKPacker.cs`](../../src/Core/IO/PCKPacker.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PCKPacker : ElectronObject`

> Creates aligned resource packages with optional entry/directory encryption and patch removals.

## Description

PCKStart validates a positive alignment and 64-character hexadecimal AES key, finishes the previous session and opens the destination for exact Write access. AddFile reads its source immediately; AddFileFromBuffer consumes borrowed bytes synchronously. Paths normalize relative to res://, preserve ordinal case and cannot escape the root. Data is stored immediately, with MD5 and aligned offsets. Empty files and duplicate keys are retained in directory order. Flush writes the directory, optionally prints each entry, closes the file and clears the key; deterministic disposal also flushes. A failed write faults the session and disposal releases it without publishing a directory. Operations serialize on one instance gate; calls allocate and block.

The writer emits ordinary PCK version 4. ProjectSettings.LoadResourcePack reads ordinary versions 2–4 and ZIP overlays. The PCK envelope uses AES-256-CFB128, random IV, zero-padded blocks, plaintext length and MD5. This is a separate format from FileAccess.OpenEncrypted and does not authenticate a downloaded pack. Source files and byte buffers use the managed array size range; pack offsets are 64-bit. Directory content is bounded to 64 MiB. Mounted pack files must remain immutable.

## API

- `public PCKPacker()`
- `public void PCKStart(string pckPath, int alignment = 32, string key = "0000000000000000000000000000000000000000000000000000000000000000", bool encryptDirectory = false)`
- `public void AddFile(string targetPath, string sourcePath, bool encrypt = false)`
- `public void AddFileFromBuffer(string targetPath, ReadOnlySpan<byte> data, bool encrypt = false)`
- `public void AddFileRemoval(string targetPath)`
- `public void Flush(bool verbose = false)`
- `protected override void Dispose(bool disposing)` (flushes and releases the owned session)

## Related

[Resource packs](../components/resource-packs.md), [ProjectSettings](ProjectSettings.md), [FileAccess](FileAccess.md), [DirAccess](DirAccess.md), [ResourceLoader](ResourceLoader.md).

With [save and swap](../components/file-save-and-swap.md) enabled, the destination remains unchanged until Flush/disposal completes its directory. Header/data/directory failures cancel staged output; the default direct policy can still leave an incomplete destination.
