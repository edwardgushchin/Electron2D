# Named ZIP archives

Last updated: 2026-10-14

Owns [ZIPPacker](../classes/ZIPPacker.md), [ZIPReader](../classes/ZIPReader.md), [ZIPAppend](../classes/ZIPAppend.md) and [ZIPCompressionLevel](../classes/ZIPCompressionLevel.md). They are synchronous asset-preparation utilities under ADR 0003/0020/0051, backed by standard ZIP structures and the existing runtime compression library. File paths use the existing project path service; logical entry names are never extracted automatically.

## Executable workflow

Create a writer, Open a destination, StartFile, WriteFile one or more spans and CloseFile/Close. ZIP64 stores large offsets and sizes without loading whole files. Reader Open/GetFiles/ReadFile returns named caller-owned snapshots and validates length/CRC. Packed resources and scenes can be saved as bytes, packaged, read, restored through ResourceLoader and rendered through the public runtime, as exercised by ZIPTests. Pack mounts and automatic import are independent capabilities.

## Contract boundaries

UTF-8 names, ordered duplicates, portable Unix attributes, local DOS timestamps and append/prefix semantics are documented on the owning classes. AddInZIP retains old compressed payloads and central metadata; it appends a new final directory and preserves the global comment. Reader replacement is prepared before publication; writer file changes are not transactional. Split archives, encryption and unsupported compression fail explicitly. Archive preparation blocks/allocates and is unsuitable for frame callbacks. Disposal owns file cleanup; no process-wide instance accessor is exposed.

## Verification

ZIPTests runs standard .NET interoperability, Unicode/empty/duplicate entries, all -1..9 compression levels, permissions/timestamps, append/global comments, prefixed files, CRC and malformed headers, lifecycle and res:// paths. Sparse ZIP64 checks run on Linux/macOS. Actual Linux Wayland GPU and compatibility pixel checks verify a bundled scene/resource round trip through ZIP. Broad performance, other hosts and owner acceptance remain unverified.
