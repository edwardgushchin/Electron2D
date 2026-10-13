# ImageFormatLoaderExtension

Last updated: 2026-10-13

`public abstract class ImageFormatLoaderExtension : ImageFormatLoader`

Implement a decoder for additional image-file formats, register it during initialization, and retain it until pending calls finish. It is caller-owned; registration retains a borrowed reference. Disposal removes registration, but callers must finish in-flight work before disposing a decoder. Hooks run on the loading thread and may run concurrently, including on resource workers. They grant no off-owner scene or native-render access.

## Construction and extension hooks

`protected ImageFormatLoaderExtension()` creates an unregistered decoder.

`protected abstract string[] GetRecognizedExtensions()` returns suffixes without dots. The engine copies and lowercases queried suffixes, validates simple filename tokens and deduplicates resource discovery. Each decoder may expose at most 256 suffixes of at most 64 ASCII letters, digits, underscores or hyphens. Null or invalid results throw `InvalidDataException` during discovery/loading. Query results remain caller-owned.

`protected abstract bool LoadImage(Image image, FileAccess fileAccess, ImageFormatLoader.LoaderFlags flags, float scale)` receives an empty temporary image and a read-only file positioned at zero. Ordinary loads pass `None` and scale one. Do not retain or dispose either borrowed argument: both expire after the call. Return true after producing valid nonempty pixels; false means the contents are unrecognized and permits the next decoder. Throw a typed exception for malformed recognized data or another failure. A false result discards any temporary pixels and rewinds the file before the next decoder. Hooks may produce any valid Image layout, including their own mipmaps; renderer sampling retains its normal format limits.

## Registration

`public void AddFormatLoader()` appends after the built-in codecs. It is idempotent by object identity. At most 64 custom decoders may be registered; capacity exhaustion throws `InvalidOperationException`, disposed registration throws `ObjectDisposedException`.

`public void RemoveFormatLoader()` removes this decoder idempotently without disposing it or waiting for callbacks. Loading captures registration before invoking hooks outside the registry lock. Removal from a discovery/load callback affects later snapshots; captured decoders remain eligible in the current operation.

## File and resource integration

[Image.Load](Image.md) accepts custom suffixes through the same native, `res://`, `user://` and UID resolution. Encoded files are restricted to 1 byte through 64 MiB before decoding. A successful temporary image is copied and published once with one `Changed` notification. Failure or unrecognized contents preserve the destination. The file length is checked after success. Mutation observers may still throw after commitment under the Resource contract.

[ResourceLoader](ResourceLoader.md) includes registered suffixes in texture extension discovery, existence and directory listing, synchronous/threaded image-texture loads and cache replacement. Built-in file suffixes retain their built-in decoder precedence. Explicit PNG/JPEG/etc buffer APIs select their own codec and do not consult custom registrations. Registration is process-local; a fresh host must register its decoders before loading files.

## Verification and limits

`ImageLoaderExtensionTests` exercises order, decline fallback/rewind, exceptions and atomic destination/event preservation, invalid suffixes, empty/oversized/malformed input, weak cache replacement, worker execution, callback removal, disposal and registration capacity. A fresh child reconstructs the public registration and loads a custom texture. Native checks sample the decoded texture and verify teardown on GPU and compatibility. These checks establish the exercised Linux desktop profile; foreign hosts, arbitrary third-party decoder safety, import/export remapping and native allocation budgets remain separate.

See [images](../components/images.md), [resources](../domains/resources.md) and the separate [coverage register](../coverage/classes/ImageFormatLoaderExtension.md).
