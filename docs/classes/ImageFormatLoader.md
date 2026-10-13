# ImageFormatLoader

Last updated: 2026-10-13

`public abstract class ImageFormatLoader : ElectronObject`

The common managed identity for image decoders. Create a subclass of [ImageFormatLoaderExtension](ImageFormatLoaderExtension.md) to implement a format. Its base construction is assembly-restricted. The inherited object lifetime uses deterministic disposal; registration borrows the decoder until removal.

[LoaderFlags](ImageFormatLoader.LoaderFlags.md) carries format-specific linear/color conversion requests. Ordinary `Image.Load` passes `None` and scale one. This does not extend the built-in codecs' color metadata profile.

See [images](../components/images.md), [resources](../domains/resources.md) and the separate [coverage register](../coverage/classes/ImageFormatLoader.md). Custom decoding and discovery allocate during preparation; no per-frame decoder loop is introduced.
