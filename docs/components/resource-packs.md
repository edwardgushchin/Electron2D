# Resource packs

Last updated: 2026-10-15

PCKPacker creates resource packages and ProjectSettings.LoadResourcePack publishes them into res://. FileAccess, DirAccess and ResourceLoader use the same mounted namespace, including typed scene archives, relative external dependencies, images and bitmap-font pages. Runtime code consumes loaded resources through the existing scene/render lifecycle.

## Behavior

- Ordinary PCK versions 2, 3 and 4, positive writer alignment, 64-bit file bases/offsets, explicit mount offsets and appended PCK trailers are supported. ZIP mounts reuse the existing bounded directory/CRC reader, including prefixed archives. Unsupported ZIP encryption flags reject before publication. Sparse bundles, delta patch flags and native executable section discovery remain explicit export dependencies.
- Mounting parses and validates a complete directory before replacing state. Invalid headers, flags, paths, lengths, directory keys, namespace collisions and truncation return false without partial publication. Invalid caller arguments still throw. Encrypted file integrity/key failures throw when opening that entry.
- Keys and names use copied mount ownership. The default decryption key is 32 zero bytes; the explicit-key overload takes exactly 32 bytes. The writer consumes source content immediately. Encrypted directories/entries use PCK AES-256-CFB128/MD5 envelopes; FileAccess's authenticated encrypted container remains a separate format.
- New mounts replace existing packed keys by default; replaceFiles=false preserves them. Removal records remove the previous packed overlay and reveal any physical fallback. Already open streams retain their selected entry and independent cursor. Cached resources retain normal Reuse/Replace/Ignore behavior. Physical absolute paths and user:// bypass the resource overlay.
- DirAccess merges physical and mounted names and navigates synthetic directories. Mounted entries are read-only; remove/rename and metadata mutation reject. Copying a file out uses its packed bytes. FileAccess supplies packed lengths, zero timestamps and read-only permissions, and rejects OS extended-attribute access. Unencrypted entries use bounded file streams; encrypted entries decode and checksum before exposure. Mounted files must not be modified in place.
- Operations allocate and may block; directories are bounded to 64 MiB and decoded entries use managed array capacity. Directory parsing, checksum validation and warmed rendering are separate verification concerns. No hostile symlink sandbox, network authenticity or crash durability is promised.

## Public workflow

```csharp
using (var packer = new PCKPacker())
{
    packer.PCKStart("game.pck", encryptDirectory: true);
    packer.AddFile("game/scene.e2dscene", "res://game/scene.e2dscene", encrypt: true);
    packer.AddFile("game/pixel.png", "res://game/pixel.png", encrypt: true);
}
if (!ProjectSettings.LoadResourcePack("game.pck"))
    throw new IOException("Cannot mount game resources.");
using var scene = ResourceLoader.Load<PackedScene>("res://game/scene.e2dscene");
var root = scene.Instantiate();
```

A production distributor supplies its own pack authentication and key provisioning. Sparse/delta exporters require their own decoder and producer before acceptance. Implementation is governed by [ADR 0019/0020](../decisions/core-data-io.md#adr-0020), resource loading by [ADR 0013](../decisions/resources.md#adr-0013), scene persistence by [ADR 0023](../decisions/scene.md#adr-0023), and host verification by [ADR 0090](../decisions/agent-native.md#adr-0090).

## Verification

Linux x64 source builds pass 119 ResourcePackTests assertions: immediate bytes, normalization/escape, empty files, alignment, copied keys, block-edge encrypted sizes, checksums, partial/invalid directories, atomic rejection, overlay replacement/removal, directory/file collisions, scope snapshots, independent cursors, retained read-only metadata, transformed compressed/raw-key/password streams, v2–v4 reads, appended/explicit offsets, a sparse physical file with an offset beyond 4 GiB, lifecycle/verbose output and typed resource instantiation. The sparse physical-offset fixture is an ordinary PCK; sparse-bundle flags explicitly reject. Existing physical FileAccess/DirAccess/ProjectSettings, ZIP, resource archive and resource loader regressions pass.

`python3 -B tools/resource-packs/check.py create <folder>` produces eight independently encrypted OpenSSL fixtures, read by the runtime's oracle profile. Its `check <game.pck>` command independently decrypts the writer's directory and two entries and verifies MD5/length/alignment/PNG/archive identity.

The test host's `--resource-pack author|headless|oracle|gpu|compatibility <folder>` profiles author an encrypted scene with an external PNG, remove the physical source directory, mount and load on a threaded request in fresh processes, instantiate the scene, and verify the rendered lime pixel on GPU and compatibility backends. The same public-only flow and 119 API assertions pass through an independent NuGet/NativeAOT consumer using matching managed and Linux native packages, launched from `/tmp` with LD_LIBRARY_PATH unset and no IL warnings. Release builds, both format verifiers, compiled API/coverage checks, wiki tests and generation/check pass. Foreign host, editor, sparse/delta export and native section discovery acceptance remain separate.
