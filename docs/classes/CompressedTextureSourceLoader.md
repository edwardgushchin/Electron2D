# CompressedTextureSourceLoader

Last updated: 2026-10-16

Internal format extension in [CompressedTextureSourceLoader.cs](../../src/Core/IO/CompressedTextureSourceLoader.cs).
Recognizes ctex and constructs CompressedTexture through its public Load method.
Type discovery accepts applicable Texture/Resource base roles, independently of
the array loader. Failure disposes the temporary resource. ResourceLoader owns
weak caching, replacement, threaded preparation and path publication; this loader
stores no cache or native handle. See [ordinary imports](../components/compressed-textures.md).
