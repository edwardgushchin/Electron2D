# ImageFormatLoader.LoaderFlags

Last updated: 2026-10-13

`[Flags] public enum ImageFormatLoader.LoaderFlags`

| Member | Value | Meaning |
| --- | ---: | --- |
| None | 0 | Ordinary decoder interpretation |
| ForceLinear | 1 | Request linear pixels when the format supports color interpretation |
| ConvertColors | 2 | Request format-specific color conversion |

The flags may be combined. `Image.Load` and the texture resource loader use `None`; scale is one. Extension decoders own interpretation of the options. This enum does not add a built-in color-management backend. See [ImageFormatLoaderExtension](ImageFormatLoaderExtension.md).
