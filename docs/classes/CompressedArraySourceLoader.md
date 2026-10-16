# CompressedArraySourceLoader

Last updated: 2026-10-16

Internal format extension in [CompressedArraySourceLoader.cs](../../src/Core/IO/CompressedArraySourceLoader.cs).
Recognizes ctexarray and constructs CompressedTextureArray through its public Load
operation. Type discovery accepts applicable Resource/TextureLayered base roles.
Construction failure disposes the temporary resource. ResourceLoader owns cache,
replacement, threaded preparation and path publication; the loader stores no
additional cache or native handle. See [file-backed arrays](../components/compressed-arrays.md).
