# RenderingDevice.DataFormat

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.DataFormat`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Local compute](../components/local-compute.md).

## Description

Encoded pixel and block-format numeric vocabulary for capability queries, allocations and views. Numeric identity is preserved. Presence of a value does not promise native allocation support; query TextureIsFormatSupportedForUsage first. The native local profile maps color formats supported by the installed backend, including BC/ASTC blocks; depth transfer/attachment roles remain unavailable. Max is a nonselectable bound and the no-override view sentinel.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
var selection = RenderingDevice.DataFormat.A1R5G5B5UNORMPACK16;
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.DataFormat A1R5G5B5UNORMPACK16 = 7` | A1R5G5B5UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SINTPACK32 = 68` | A2B10G10R10SINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SNORMPACK32 = 64` | A2B10G10R10SNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SSCALEDPACK32 = 66` | A2B10G10R10SSCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10UINTPACK32 = 67` | A2B10G10R10UINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10UNORMPACK32 = 63` | A2B10G10R10UNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2B10G10R10USCALEDPACK32 = 65` | A2B10G10R10USCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SINTPACK32 = 62` | A2R10G10B10SINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SNORMPACK32 = 58` | A2R10G10B10SNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SSCALEDPACK32 = 60` | A2R10G10B10SSCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10UINTPACK32 = 61` | A2R10G10B10UINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10UNORMPACK32 = 57` | A2R10G10B10UNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A2R10G10B10USCALEDPACK32 = 59` | A2R10G10B10USCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SINTPACK32 = 55` | A8B8G8R8SINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SNORMPACK32 = 51` | A8B8G8R8SNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SRGBPACK32 = 56` | A8B8G8R8SRGBPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SSCALEDPACK32 = 53` | A8B8G8R8SSCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8UINTPACK32 = 54` | A8B8G8R8UINTPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8UNORMPACK32 = 50` | A8B8G8R8UNORMPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat A8B8G8R8USCALEDPACK32 = 52` | A8B8G8R8USCALEDPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X10SFLOATBLOCK = 229` | ASTC10X10SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X10SRGBBLOCK = 179` | ASTC10X10SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X10UNORMBLOCK = 178` | ASTC10X10UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X5SFLOATBLOCK = 226` | ASTC10X5SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X5SRGBBLOCK = 173` | ASTC10X5SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X5UNORMBLOCK = 172` | ASTC10X5UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X6SFLOATBLOCK = 227` | ASTC10X6SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X6SRGBBLOCK = 175` | ASTC10X6SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X6UNORMBLOCK = 174` | ASTC10X6UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X8SFLOATBLOCK = 228` | ASTC10X8SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X8SRGBBLOCK = 177` | ASTC10X8SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC10X8UNORMBLOCK = 176` | ASTC10X8UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X10SFLOATBLOCK = 230` | ASTC12X10SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X10SRGBBLOCK = 181` | ASTC12X10SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X10UNORMBLOCK = 180` | ASTC12X10UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X12SFLOATBLOCK = 231` | ASTC12X12SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X12SRGBBLOCK = 183` | ASTC12X12SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC12X12UNORMBLOCK = 182` | ASTC12X12UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC4X4SFLOATBLOCK = 218` | ASTC4X4SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC4X4SRGBBLOCK = 157` | ASTC4X4SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC4X4UNORMBLOCK = 156` | ASTC4X4UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X4SFLOATBLOCK = 219` | ASTC5X4SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X4SRGBBLOCK = 159` | ASTC5X4SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X4UNORMBLOCK = 158` | ASTC5X4UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X5SFLOATBLOCK = 220` | ASTC5X5SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X5SRGBBLOCK = 161` | ASTC5X5SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC5X5UNORMBLOCK = 160` | ASTC5X5UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X5SFLOATBLOCK = 221` | ASTC6X5SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X5SRGBBLOCK = 163` | ASTC6X5SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X5UNORMBLOCK = 162` | ASTC6X5UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X6SFLOATBLOCK = 222` | ASTC6X6SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X6SRGBBLOCK = 165` | ASTC6X6SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC6X6UNORMBLOCK = 164` | ASTC6X6UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X5SFLOATBLOCK = 223` | ASTC8X5SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X5SRGBBLOCK = 167` | ASTC8X5SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X5UNORMBLOCK = 166` | ASTC8X5UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X6SFLOATBLOCK = 224` | ASTC8X6SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X6SRGBBLOCK = 169` | ASTC8X6SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X6UNORMBLOCK = 168` | ASTC8X6UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X8SFLOATBLOCK = 225` | ASTC8X8SFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X8SRGBBLOCK = 171` | ASTC8X8SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ASTC8X8UNORMBLOCK = 170` | ASTC8X8UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B10G11R11UFLOATPACK32 = 121` | B10G11R11UFLOATPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B10X6G10X6R10X6G10X6422UNORM4PACK16 = 195` | B10X6G10X6R10X6G10X6422UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B12X4G12X4R12X4G12X4422UNORM4PACK16 = 205` | B12X4G12X4R12X4G12X4422UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B16G16R16G16422UNORM = 212` | B16G16R16G16422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B4G4R4A4UNORMPACK16 = 2` | B4G4R4A4UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B5G5R5A1UNORMPACK16 = 6` | B5G5R5A1UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B5G6R5UNORMPACK16 = 4` | B5G6R5UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SINT = 48` | B8G8R8A8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SNORM = 44` | B8G8R8A8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SRGB = 49` | B8G8R8A8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SSCALED = 46` | B8G8R8A8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8UINT = 47` | B8G8R8A8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8UNORM = 43` | B8G8R8A8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8A8USCALED = 45` | B8G8R8A8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8G8422UNORM = 185` | B8G8R8G8422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8SINT = 34` | B8G8R8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8SNORM = 30` | B8G8R8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8SRGB = 35` | B8G8R8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8SSCALED = 32` | B8G8R8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8UINT = 33` | B8G8R8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8UNORM = 29` | B8G8R8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat B8G8R8USCALED = 31` | B8G8R8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC1RGBASRGBBLOCK = 133` | BC1RGBASRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC1RGBAUNORMBLOCK = 132` | BC1RGBAUNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC1RGBSRGBBLOCK = 131` | BC1RGBSRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC1RGBUNORMBLOCK = 130` | BC1RGBUNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC2SRGBBLOCK = 135` | BC2SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC2UNORMBLOCK = 134` | BC2UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC3SRGBBLOCK = 137` | BC3SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC3UNORMBLOCK = 136` | BC3UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC4SNORMBLOCK = 139` | BC4SNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC4UNORMBLOCK = 138` | BC4UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC5SNORMBLOCK = 141` | BC5SNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC5UNORMBLOCK = 140` | BC5UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC6HSFLOATBLOCK = 143` | BC6HSFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC6HUFLOATBLOCK = 142` | BC6HUFLOATBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC7SRGBBLOCK = 145` | BC7SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat BC7UNORMBLOCK = 144` | BC7UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat D16UNORM = 123` | D16UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat D16UNORMS8UINT = 127` | D16UNORMS8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat D24UNORMS8UINT = 128` | D24UNORMS8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat D32SFLOAT = 125` | D32SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat D32SFLOATS8UINT = 129` | D32SFLOATS8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat E5B9G9R9UFLOATPACK32 = 122` | E5B9G9R9UFLOATPACK32 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat EACR11G11SNORMBLOCK = 155` | EACR11G11SNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat EACR11G11UNORMBLOCK = 154` | EACR11G11UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat EACR11SNORMBLOCK = 153` | EACR11SNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat EACR11UNORMBLOCK = 152` | EACR11UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A1SRGBBLOCK = 149` | ETC2R8G8B8A1SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A1UNORMBLOCK = 148` | ETC2R8G8B8A1UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A8SRGBBLOCK = 151` | ETC2R8G8B8A8SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A8UNORMBLOCK = 150` | ETC2R8G8B8A8UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8SRGBBLOCK = 147` | ETC2R8G8B8SRGBBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8UNORMBLOCK = 146` | ETC2R8G8B8UNORMBLOCK encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6G10X6R10X6422UNORM4PACK16 = 194` | G10X6B10X6G10X6R10X6422UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X62PLANE420UNORM3PACK16 = 197` | G10X6B10X6R10X62PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X62PLANE422UNORM3PACK16 = 199` | G10X6B10X6R10X62PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE420UNORM3PACK16 = 196` | G10X6B10X6R10X63PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE422UNORM3PACK16 = 198` | G10X6B10X6R10X63PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE444UNORM3PACK16 = 200` | G10X6B10X6R10X63PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4G12X4R12X4422UNORM4PACK16 = 204` | G12X4B12X4G12X4R12X4422UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X42PLANE420UNORM3PACK16 = 207` | G12X4B12X4R12X42PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X42PLANE422UNORM3PACK16 = 209` | G12X4B12X4R12X42PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE420UNORM3PACK16 = 206` | G12X4B12X4R12X43PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE422UNORM3PACK16 = 208` | G12X4B12X4R12X43PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE444UNORM3PACK16 = 210` | G12X4B12X4R12X43PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16G16R16422UNORM = 211` | G16B16G16R16422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16R162PLANE420UNORM = 214` | G16B16R162PLANE420UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16R162PLANE422UNORM = 216` | G16B16R162PLANE422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE420UNORM = 213` | G16B16R163PLANE420UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE422UNORM = 215` | G16B16R163PLANE422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE444UNORM = 217` | G16B16R163PLANE444UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8G8R8422UNORM = 184` | G8B8G8R8422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8R82PLANE420UNORM = 187` | G8B8R82PLANE420UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8R82PLANE422UNORM = 189` | G8B8R82PLANE422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE420UNORM = 186` | G8B8R83PLANE420UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE422UNORM = 188` | G8B8R83PLANE422UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE444UNORM = 190` | G8B8R83PLANE444UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat Max = 232` | Nonselectable upper bound; DataFormat.Max also means no view override. |
| `public const Electron2D.RenderingDevice.DataFormat R10X6G10X6B10X6A10X6UNORM4PACK16 = 193` | R10X6G10X6B10X6A10X6UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R10X6G10X6UNORM2PACK16 = 192` | R10X6G10X6UNORM2PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R10X6UNORMPACK16 = 191` | R10X6UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R12X4G12X4B12X4A12X4UNORM4PACK16 = 203` | R12X4G12X4B12X4A12X4UNORM4PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R12X4G12X4UNORM2PACK16 = 202` | R12X4G12X4UNORM2PACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R12X4UNORMPACK16 = 201` | R12X4UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SFLOAT = 96` | R16G16B16A16SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SINT = 95` | R16G16B16A16SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SNORM = 91` | R16G16B16A16SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SSCALED = 93` | R16G16B16A16SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16UINT = 94` | R16G16B16A16UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16UNORM = 90` | R16G16B16A16UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16A16USCALED = 92` | R16G16B16A16USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16SFLOAT = 89` | R16G16B16SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16SINT = 88` | R16G16B16SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16SNORM = 84` | R16G16B16SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16SSCALED = 86` | R16G16B16SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16UINT = 87` | R16G16B16UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16UNORM = 83` | R16G16B16UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16B16USCALED = 85` | R16G16B16USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16SFLOAT = 82` | R16G16SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16SINT = 81` | R16G16SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16SNORM = 77` | R16G16SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16SSCALED = 79` | R16G16SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16UINT = 80` | R16G16UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16UNORM = 76` | R16G16UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16G16USCALED = 78` | R16G16USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16SFLOAT = 75` | R16SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16SINT = 74` | R16SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16SNORM = 70` | R16SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16SSCALED = 72` | R16SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16UINT = 73` | R16UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16UNORM = 69` | R16UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R16USCALED = 71` | R16USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32A32SFLOAT = 108` | R32G32B32A32SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32A32SINT = 107` | R32G32B32A32SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32A32UINT = 106` | R32G32B32A32UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32SFLOAT = 105` | R32G32B32SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32SINT = 104` | R32G32B32SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32B32UINT = 103` | R32G32B32UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32SFLOAT = 102` | R32G32SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32SINT = 101` | R32G32SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32G32UINT = 100` | R32G32UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32SFLOAT = 99` | R32SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32SINT = 98` | R32SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R32UINT = 97` | R32UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R4G4B4A4UNORMPACK16 = 1` | R4G4B4A4UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R4G4UNORMPACK8 = 0` | R4G4UNORMPACK8 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R5G5B5A1UNORMPACK16 = 5` | R5G5B5A1UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R5G6B5UNORMPACK16 = 3` | R5G6B5UNORMPACK16 encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64A64SFLOAT = 120` | R64G64B64A64SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64A64SINT = 119` | R64G64B64A64SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64A64UINT = 118` | R64G64B64A64UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64SFLOAT = 117` | R64G64B64SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64SINT = 116` | R64G64B64SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64B64UINT = 115` | R64G64B64UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64SFLOAT = 114` | R64G64SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64SINT = 113` | R64G64SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64G64UINT = 112` | R64G64UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64SFLOAT = 111` | R64SFLOAT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64SINT = 110` | R64SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R64UINT = 109` | R64UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SINT = 41` | R8G8B8A8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SNORM = 37` | R8G8B8A8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SRGB = 42` | R8G8B8A8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SSCALED = 39` | R8G8B8A8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8UINT = 40` | R8G8B8A8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8UNORM = 36` | R8G8B8A8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8A8USCALED = 38` | R8G8B8A8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8SINT = 27` | R8G8B8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8SNORM = 23` | R8G8B8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8SRGB = 28` | R8G8B8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8SSCALED = 25` | R8G8B8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8UINT = 26` | R8G8B8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8UNORM = 22` | R8G8B8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8B8USCALED = 24` | R8G8B8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8SINT = 20` | R8G8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8SNORM = 16` | R8G8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8SRGB = 21` | R8G8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8SSCALED = 18` | R8G8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8UINT = 19` | R8G8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8UNORM = 15` | R8G8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8G8USCALED = 17` | R8G8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8SINT = 13` | R8SINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8SNORM = 9` | R8SNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8SRGB = 14` | R8SRGB encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8SSCALED = 11` | R8SSCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8UINT = 12` | R8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8UNORM = 8` | R8UNORM encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat R8USCALED = 10` | R8USCALED encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat S8UINT = 126` | S8UINT encoded pixel/block format; query support before creation. |
| `public const Electron2D.RenderingDevice.DataFormat X8D24UNORMPACK32 = 124` | X8D24UNORMPACK32 encoded pixel/block format; query support before creation. |

## Member descriptions

<a id="member-0e92ae678870"></a>
### A1R5G5B5UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat A1R5G5B5UNORMPACK16 = 7
```

A1R5G5B5UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-d7d10003e84b"></a>
### A2B10G10R10SINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SINTPACK32 = 68
```

A2B10G10R10SINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-38a6555be107"></a>
### A2B10G10R10SNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SNORMPACK32 = 64
```

A2B10G10R10SNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-88adfd1d4965"></a>
### A2B10G10R10SSCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10SSCALEDPACK32 = 66
```

A2B10G10R10SSCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-96f054eb3dce"></a>
### A2B10G10R10UINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10UINTPACK32 = 67
```

A2B10G10R10UINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-74499140f4b9"></a>
### A2B10G10R10UNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10UNORMPACK32 = 63
```

A2B10G10R10UNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-99f35fff67b9"></a>
### A2B10G10R10USCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2B10G10R10USCALEDPACK32 = 65
```

A2B10G10R10USCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-6635bb1480ba"></a>
### A2R10G10B10SINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SINTPACK32 = 62
```

A2R10G10B10SINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-52f51ce424e1"></a>
### A2R10G10B10SNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SNORMPACK32 = 58
```

A2R10G10B10SNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-c73b90173b17"></a>
### A2R10G10B10SSCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10SSCALEDPACK32 = 60
```

A2R10G10B10SSCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-d9178f12d434"></a>
### A2R10G10B10UINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10UINTPACK32 = 61
```

A2R10G10B10UINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-72cc879ab140"></a>
### A2R10G10B10UNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10UNORMPACK32 = 57
```

A2R10G10B10UNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-3f70913faa63"></a>
### A2R10G10B10USCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A2R10G10B10USCALEDPACK32 = 59
```

A2R10G10B10USCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-c05a6ae393f3"></a>
### A8B8G8R8SINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SINTPACK32 = 55
```

A8B8G8R8SINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-095ea35efa95"></a>
### A8B8G8R8SNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SNORMPACK32 = 51
```

A8B8G8R8SNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-7ceb878b370c"></a>
### A8B8G8R8SRGBPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SRGBPACK32 = 56
```

A8B8G8R8SRGBPACK32 encoded pixel/block format; query support before creation.

<a id="member-ec3c5eb68dbf"></a>
### A8B8G8R8SSCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8SSCALEDPACK32 = 53
```

A8B8G8R8SSCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-3c1e002f9c39"></a>
### A8B8G8R8UINTPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8UINTPACK32 = 54
```

A8B8G8R8UINTPACK32 encoded pixel/block format; query support before creation.

<a id="member-b3c966422cd8"></a>
### A8B8G8R8UNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8UNORMPACK32 = 50
```

A8B8G8R8UNORMPACK32 encoded pixel/block format; query support before creation.

<a id="member-b25b0409a285"></a>
### A8B8G8R8USCALEDPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat A8B8G8R8USCALEDPACK32 = 52
```

A8B8G8R8USCALEDPACK32 encoded pixel/block format; query support before creation.

<a id="member-493dded0e70d"></a>
### ASTC10X10SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X10SFLOATBLOCK = 229
```

ASTC10X10SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-899f744dcf03"></a>
### ASTC10X10SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X10SRGBBLOCK = 179
```

ASTC10X10SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-7f2a480cd6d2"></a>
### ASTC10X10UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X10UNORMBLOCK = 178
```

ASTC10X10UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-ed92f78f55f1"></a>
### ASTC10X5SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X5SFLOATBLOCK = 226
```

ASTC10X5SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-703d7078cfe2"></a>
### ASTC10X5SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X5SRGBBLOCK = 173
```

ASTC10X5SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-f7d2aa3a092b"></a>
### ASTC10X5UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X5UNORMBLOCK = 172
```

ASTC10X5UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-52dfb5ea826a"></a>
### ASTC10X6SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X6SFLOATBLOCK = 227
```

ASTC10X6SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-6541a0f97efa"></a>
### ASTC10X6SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X6SRGBBLOCK = 175
```

ASTC10X6SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-85a20feed7fb"></a>
### ASTC10X6UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X6UNORMBLOCK = 174
```

ASTC10X6UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-f23e8f4b3e37"></a>
### ASTC10X8SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X8SFLOATBLOCK = 228
```

ASTC10X8SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-8070fe97ff59"></a>
### ASTC10X8SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X8SRGBBLOCK = 177
```

ASTC10X8SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-8719a27d8978"></a>
### ASTC10X8UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC10X8UNORMBLOCK = 176
```

ASTC10X8UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-3af07acdc299"></a>
### ASTC12X10SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X10SFLOATBLOCK = 230
```

ASTC12X10SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-4987e9733156"></a>
### ASTC12X10SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X10SRGBBLOCK = 181
```

ASTC12X10SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-ecd845f837f0"></a>
### ASTC12X10UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X10UNORMBLOCK = 180
```

ASTC12X10UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-8c92c83ad73d"></a>
### ASTC12X12SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X12SFLOATBLOCK = 231
```

ASTC12X12SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-7b0e6e4b76fe"></a>
### ASTC12X12SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X12SRGBBLOCK = 183
```

ASTC12X12SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-1d5c2a2a2558"></a>
### ASTC12X12UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC12X12UNORMBLOCK = 182
```

ASTC12X12UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-4bb9a75dc18d"></a>
### ASTC4X4SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC4X4SFLOATBLOCK = 218
```

ASTC4X4SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-f5c5403e4630"></a>
### ASTC4X4SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC4X4SRGBBLOCK = 157
```

ASTC4X4SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-893c70f562df"></a>
### ASTC4X4UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC4X4UNORMBLOCK = 156
```

ASTC4X4UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-7a79dac51f9f"></a>
### ASTC5X4SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X4SFLOATBLOCK = 219
```

ASTC5X4SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-afc5115059fe"></a>
### ASTC5X4SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X4SRGBBLOCK = 159
```

ASTC5X4SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-91dae1a50e5a"></a>
### ASTC5X4UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X4UNORMBLOCK = 158
```

ASTC5X4UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-31151f9e14cd"></a>
### ASTC5X5SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X5SFLOATBLOCK = 220
```

ASTC5X5SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-2200637f66a0"></a>
### ASTC5X5SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X5SRGBBLOCK = 161
```

ASTC5X5SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-0a13c8d6ad0a"></a>
### ASTC5X5UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC5X5UNORMBLOCK = 160
```

ASTC5X5UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-882cd933d640"></a>
### ASTC6X5SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X5SFLOATBLOCK = 221
```

ASTC6X5SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-28e52801b0f2"></a>
### ASTC6X5SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X5SRGBBLOCK = 163
```

ASTC6X5SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-70428d042f9e"></a>
### ASTC6X5UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X5UNORMBLOCK = 162
```

ASTC6X5UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-18d76f0f2555"></a>
### ASTC6X6SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X6SFLOATBLOCK = 222
```

ASTC6X6SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-def8d64f69cf"></a>
### ASTC6X6SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X6SRGBBLOCK = 165
```

ASTC6X6SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-91eeac15e5d2"></a>
### ASTC6X6UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC6X6UNORMBLOCK = 164
```

ASTC6X6UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-5ca4eb372738"></a>
### ASTC8X5SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X5SFLOATBLOCK = 223
```

ASTC8X5SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-e2291aca3185"></a>
### ASTC8X5SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X5SRGBBLOCK = 167
```

ASTC8X5SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-ee84ab811dac"></a>
### ASTC8X5UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X5UNORMBLOCK = 166
```

ASTC8X5UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-fc2e9bd19178"></a>
### ASTC8X6SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X6SFLOATBLOCK = 224
```

ASTC8X6SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-f47652cf78fb"></a>
### ASTC8X6SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X6SRGBBLOCK = 169
```

ASTC8X6SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-c388c2c0dd69"></a>
### ASTC8X6UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X6UNORMBLOCK = 168
```

ASTC8X6UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-4424aef61005"></a>
### ASTC8X8SFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X8SFLOATBLOCK = 225
```

ASTC8X8SFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-38a8c6adac17"></a>
### ASTC8X8SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X8SRGBBLOCK = 171
```

ASTC8X8SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-c257699e4437"></a>
### ASTC8X8UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ASTC8X8UNORMBLOCK = 170
```

ASTC8X8UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-e64014113937"></a>
### B10G11R11UFLOATPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat B10G11R11UFLOATPACK32 = 121
```

B10G11R11UFLOATPACK32 encoded pixel/block format; query support before creation.

<a id="member-0e0015f132d9"></a>
### B10X6G10X6R10X6G10X6422UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat B10X6G10X6R10X6G10X6422UNORM4PACK16 = 195
```

B10X6G10X6R10X6G10X6422UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-8a2e8986f408"></a>
### B12X4G12X4R12X4G12X4422UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat B12X4G12X4R12X4G12X4422UNORM4PACK16 = 205
```

B12X4G12X4R12X4G12X4422UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-83e75605039a"></a>
### B16G16R16G16422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B16G16R16G16422UNORM = 212
```

B16G16R16G16422UNORM encoded pixel/block format; query support before creation.

<a id="member-d88b57ae84a8"></a>
### B4G4R4A4UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat B4G4R4A4UNORMPACK16 = 2
```

B4G4R4A4UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-a1ebd5c8f1ec"></a>
### B5G5R5A1UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat B5G5R5A1UNORMPACK16 = 6
```

B5G5R5A1UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-bf6355bb0f97"></a>
### B5G6R5UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat B5G6R5UNORMPACK16 = 4
```

B5G6R5UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-a65dbb4996ce"></a>
### B8G8R8A8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SINT = 48
```

B8G8R8A8SINT encoded pixel/block format; query support before creation.

<a id="member-844a3c76b2ed"></a>
### B8G8R8A8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SNORM = 44
```

B8G8R8A8SNORM encoded pixel/block format; query support before creation.

<a id="member-b9bd22f515fb"></a>
### B8G8R8A8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SRGB = 49
```

B8G8R8A8SRGB encoded pixel/block format; query support before creation.

<a id="member-0ac0d96e140f"></a>
### B8G8R8A8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8SSCALED = 46
```

B8G8R8A8SSCALED encoded pixel/block format; query support before creation.

<a id="member-7067d7cb1396"></a>
### B8G8R8A8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8UINT = 47
```

B8G8R8A8UINT encoded pixel/block format; query support before creation.

<a id="member-bc030da3f456"></a>
### B8G8R8A8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8UNORM = 43
```

B8G8R8A8UNORM encoded pixel/block format; query support before creation.

<a id="member-382ef283e0bd"></a>
### B8G8R8A8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8A8USCALED = 45
```

B8G8R8A8USCALED encoded pixel/block format; query support before creation.

<a id="member-df19466b2f05"></a>
### B8G8R8G8422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8G8422UNORM = 185
```

B8G8R8G8422UNORM encoded pixel/block format; query support before creation.

<a id="member-6b6c4a6a4282"></a>
### B8G8R8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8SINT = 34
```

B8G8R8SINT encoded pixel/block format; query support before creation.

<a id="member-c6665facae9f"></a>
### B8G8R8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8SNORM = 30
```

B8G8R8SNORM encoded pixel/block format; query support before creation.

<a id="member-7600b2f7b351"></a>
### B8G8R8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8SRGB = 35
```

B8G8R8SRGB encoded pixel/block format; query support before creation.

<a id="member-9aaddc342396"></a>
### B8G8R8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8SSCALED = 32
```

B8G8R8SSCALED encoded pixel/block format; query support before creation.

<a id="member-61b36366e44e"></a>
### B8G8R8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8UINT = 33
```

B8G8R8UINT encoded pixel/block format; query support before creation.

<a id="member-5de8cf91020d"></a>
### B8G8R8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8UNORM = 29
```

B8G8R8UNORM encoded pixel/block format; query support before creation.

<a id="member-ddcf589ff2e5"></a>
### B8G8R8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat B8G8R8USCALED = 31
```

B8G8R8USCALED encoded pixel/block format; query support before creation.

<a id="member-8e197d736d9e"></a>
### BC1RGBASRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC1RGBASRGBBLOCK = 133
```

BC1RGBASRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-7c7b9cbd2139"></a>
### BC1RGBAUNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC1RGBAUNORMBLOCK = 132
```

BC1RGBAUNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-7160d29be72d"></a>
### BC1RGBSRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC1RGBSRGBBLOCK = 131
```

BC1RGBSRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-2c5da6abe5f1"></a>
### BC1RGBUNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC1RGBUNORMBLOCK = 130
```

BC1RGBUNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-6f53afdf087d"></a>
### BC2SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC2SRGBBLOCK = 135
```

BC2SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-481fa6bc210e"></a>
### BC2UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC2UNORMBLOCK = 134
```

BC2UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-8ef35ff070bb"></a>
### BC3SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC3SRGBBLOCK = 137
```

BC3SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-fe56579f5889"></a>
### BC3UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC3UNORMBLOCK = 136
```

BC3UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-53e491206603"></a>
### BC4SNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC4SNORMBLOCK = 139
```

BC4SNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-8df1a078fc77"></a>
### BC4UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC4UNORMBLOCK = 138
```

BC4UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-fb946621caa3"></a>
### BC5SNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC5SNORMBLOCK = 141
```

BC5SNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-3821bfa63614"></a>
### BC5UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC5UNORMBLOCK = 140
```

BC5UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-46b2aa303de2"></a>
### BC6HSFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC6HSFLOATBLOCK = 143
```

BC6HSFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-791842721b47"></a>
### BC6HUFLOATBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC6HUFLOATBLOCK = 142
```

BC6HUFLOATBLOCK encoded pixel/block format; query support before creation.

<a id="member-ddb4883918e1"></a>
### BC7SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC7SRGBBLOCK = 145
```

BC7SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-008ff9dd9a7e"></a>
### BC7UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat BC7UNORMBLOCK = 144
```

BC7UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-060215fb56ba"></a>
### D16UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat D16UNORM = 123
```

D16UNORM encoded pixel/block format; query support before creation.

<a id="member-a94ae5f9ca6a"></a>
### D16UNORMS8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat D16UNORMS8UINT = 127
```

D16UNORMS8UINT encoded pixel/block format; query support before creation.

<a id="member-2b97de15409c"></a>
### D24UNORMS8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat D24UNORMS8UINT = 128
```

D24UNORMS8UINT encoded pixel/block format; query support before creation.

<a id="member-e15e83832814"></a>
### D32SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat D32SFLOAT = 125
```

D32SFLOAT encoded pixel/block format; query support before creation.

<a id="member-6568119eaaff"></a>
### D32SFLOATS8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat D32SFLOATS8UINT = 129
```

D32SFLOATS8UINT encoded pixel/block format; query support before creation.

<a id="member-22f35707830d"></a>
### E5B9G9R9UFLOATPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat E5B9G9R9UFLOATPACK32 = 122
```

E5B9G9R9UFLOATPACK32 encoded pixel/block format; query support before creation.

<a id="member-713e494c0d43"></a>
### EACR11G11SNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat EACR11G11SNORMBLOCK = 155
```

EACR11G11SNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-3a7c65658468"></a>
### EACR11G11UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat EACR11G11UNORMBLOCK = 154
```

EACR11G11UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-e15aa87fc3d9"></a>
### EACR11SNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat EACR11SNORMBLOCK = 153
```

EACR11SNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-dc825424ebca"></a>
### EACR11UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat EACR11UNORMBLOCK = 152
```

EACR11UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-3bc3907419c5"></a>
### ETC2R8G8B8A1SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A1SRGBBLOCK = 149
```

ETC2R8G8B8A1SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-bd338d203e88"></a>
### ETC2R8G8B8A1UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A1UNORMBLOCK = 148
```

ETC2R8G8B8A1UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-a56e27ac9e3d"></a>
### ETC2R8G8B8A8SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A8SRGBBLOCK = 151
```

ETC2R8G8B8A8SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-e5dc8bac2b49"></a>
### ETC2R8G8B8A8UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8A8UNORMBLOCK = 150
```

ETC2R8G8B8A8UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-b3d18b74c2da"></a>
### ETC2R8G8B8SRGBBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8SRGBBLOCK = 147
```

ETC2R8G8B8SRGBBLOCK encoded pixel/block format; query support before creation.

<a id="member-2559aa409068"></a>
### ETC2R8G8B8UNORMBLOCK

```csharp
public const Electron2D.RenderingDevice.DataFormat ETC2R8G8B8UNORMBLOCK = 146
```

ETC2R8G8B8UNORMBLOCK encoded pixel/block format; query support before creation.

<a id="member-6d6e390c70c3"></a>
### G10X6B10X6G10X6R10X6422UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6G10X6R10X6422UNORM4PACK16 = 194
```

G10X6B10X6G10X6R10X6422UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-7c9307da93c5"></a>
### G10X6B10X6R10X62PLANE420UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X62PLANE420UNORM3PACK16 = 197
```

G10X6B10X6R10X62PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-1e92098f6ede"></a>
### G10X6B10X6R10X62PLANE422UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X62PLANE422UNORM3PACK16 = 199
```

G10X6B10X6R10X62PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-78ca9673b235"></a>
### G10X6B10X6R10X63PLANE420UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE420UNORM3PACK16 = 196
```

G10X6B10X6R10X63PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-b568590a2b95"></a>
### G10X6B10X6R10X63PLANE422UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE422UNORM3PACK16 = 198
```

G10X6B10X6R10X63PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-6fd1507ddbc1"></a>
### G10X6B10X6R10X63PLANE444UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G10X6B10X6R10X63PLANE444UNORM3PACK16 = 200
```

G10X6B10X6R10X63PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-502b880f0407"></a>
### G12X4B12X4G12X4R12X4422UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4G12X4R12X4422UNORM4PACK16 = 204
```

G12X4B12X4G12X4R12X4422UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-bd13713a2025"></a>
### G12X4B12X4R12X42PLANE420UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X42PLANE420UNORM3PACK16 = 207
```

G12X4B12X4R12X42PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-65ac6705f05e"></a>
### G12X4B12X4R12X42PLANE422UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X42PLANE422UNORM3PACK16 = 209
```

G12X4B12X4R12X42PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-7731f8cfeeb8"></a>
### G12X4B12X4R12X43PLANE420UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE420UNORM3PACK16 = 206
```

G12X4B12X4R12X43PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-dbbc66a35cf7"></a>
### G12X4B12X4R12X43PLANE422UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE422UNORM3PACK16 = 208
```

G12X4B12X4R12X43PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-76ce62512edc"></a>
### G12X4B12X4R12X43PLANE444UNORM3PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat G12X4B12X4R12X43PLANE444UNORM3PACK16 = 210
```

G12X4B12X4R12X43PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation.

<a id="member-f27d332475e7"></a>
### G16B16G16R16422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16G16R16422UNORM = 211
```

G16B16G16R16422UNORM encoded pixel/block format; query support before creation.

<a id="member-0720a6510cbb"></a>
### G16B16R162PLANE420UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16R162PLANE420UNORM = 214
```

G16B16R162PLANE420UNORM encoded pixel/block format; query support before creation.

<a id="member-ae49308ffd53"></a>
### G16B16R162PLANE422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16R162PLANE422UNORM = 216
```

G16B16R162PLANE422UNORM encoded pixel/block format; query support before creation.

<a id="member-15952cdb8336"></a>
### G16B16R163PLANE420UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE420UNORM = 213
```

G16B16R163PLANE420UNORM encoded pixel/block format; query support before creation.

<a id="member-b3ef1c268961"></a>
### G16B16R163PLANE422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE422UNORM = 215
```

G16B16R163PLANE422UNORM encoded pixel/block format; query support before creation.

<a id="member-6b4f6dd6de1a"></a>
### G16B16R163PLANE444UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G16B16R163PLANE444UNORM = 217
```

G16B16R163PLANE444UNORM encoded pixel/block format; query support before creation.

<a id="member-0595d9d4ea2a"></a>
### G8B8G8R8422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8G8R8422UNORM = 184
```

G8B8G8R8422UNORM encoded pixel/block format; query support before creation.

<a id="member-75b30eef0475"></a>
### G8B8R82PLANE420UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8R82PLANE420UNORM = 187
```

G8B8R82PLANE420UNORM encoded pixel/block format; query support before creation.

<a id="member-fb4ed75c7f7a"></a>
### G8B8R82PLANE422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8R82PLANE422UNORM = 189
```

G8B8R82PLANE422UNORM encoded pixel/block format; query support before creation.

<a id="member-55d016425a87"></a>
### G8B8R83PLANE420UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE420UNORM = 186
```

G8B8R83PLANE420UNORM encoded pixel/block format; query support before creation.

<a id="member-f99139e0a933"></a>
### G8B8R83PLANE422UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE422UNORM = 188
```

G8B8R83PLANE422UNORM encoded pixel/block format; query support before creation.

<a id="member-c2e6f2fcb8ae"></a>
### G8B8R83PLANE444UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat G8B8R83PLANE444UNORM = 190
```

G8B8R83PLANE444UNORM encoded pixel/block format; query support before creation.

<a id="member-ceb6361a1f72"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.DataFormat Max = 232
```

Nonselectable upper bound; DataFormat.Max also means no view override.

<a id="member-7337ac3c97ee"></a>
### R10X6G10X6B10X6A10X6UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R10X6G10X6B10X6A10X6UNORM4PACK16 = 193
```

R10X6G10X6B10X6A10X6UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-3c42ea10d48d"></a>
### R10X6G10X6UNORM2PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R10X6G10X6UNORM2PACK16 = 192
```

R10X6G10X6UNORM2PACK16 encoded pixel/block format; query support before creation.

<a id="member-8502f692b732"></a>
### R10X6UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R10X6UNORMPACK16 = 191
```

R10X6UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-5c654e9581b5"></a>
### R12X4G12X4B12X4A12X4UNORM4PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R12X4G12X4B12X4A12X4UNORM4PACK16 = 203
```

R12X4G12X4B12X4A12X4UNORM4PACK16 encoded pixel/block format; query support before creation.

<a id="member-e1b1e7f00e3f"></a>
### R12X4G12X4UNORM2PACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R12X4G12X4UNORM2PACK16 = 202
```

R12X4G12X4UNORM2PACK16 encoded pixel/block format; query support before creation.

<a id="member-da301233bfce"></a>
### R12X4UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R12X4UNORMPACK16 = 201
```

R12X4UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-4813bb17ac41"></a>
### R16G16B16A16SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SFLOAT = 96
```

R16G16B16A16SFLOAT encoded pixel/block format; query support before creation.

<a id="member-682139448b80"></a>
### R16G16B16A16SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SINT = 95
```

R16G16B16A16SINT encoded pixel/block format; query support before creation.

<a id="member-6a6b4e7510c8"></a>
### R16G16B16A16SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SNORM = 91
```

R16G16B16A16SNORM encoded pixel/block format; query support before creation.

<a id="member-b65d04baa607"></a>
### R16G16B16A16SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16SSCALED = 93
```

R16G16B16A16SSCALED encoded pixel/block format; query support before creation.

<a id="member-cf938397af65"></a>
### R16G16B16A16UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16UINT = 94
```

R16G16B16A16UINT encoded pixel/block format; query support before creation.

<a id="member-5e380616c153"></a>
### R16G16B16A16UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16UNORM = 90
```

R16G16B16A16UNORM encoded pixel/block format; query support before creation.

<a id="member-c050f5fe83c2"></a>
### R16G16B16A16USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16A16USCALED = 92
```

R16G16B16A16USCALED encoded pixel/block format; query support before creation.

<a id="member-576c1852d67d"></a>
### R16G16B16SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16SFLOAT = 89
```

R16G16B16SFLOAT encoded pixel/block format; query support before creation.

<a id="member-5b99cec4e5a7"></a>
### R16G16B16SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16SINT = 88
```

R16G16B16SINT encoded pixel/block format; query support before creation.

<a id="member-5d6f3a02afa5"></a>
### R16G16B16SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16SNORM = 84
```

R16G16B16SNORM encoded pixel/block format; query support before creation.

<a id="member-61c810cb1cca"></a>
### R16G16B16SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16SSCALED = 86
```

R16G16B16SSCALED encoded pixel/block format; query support before creation.

<a id="member-06b1226e30e0"></a>
### R16G16B16UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16UINT = 87
```

R16G16B16UINT encoded pixel/block format; query support before creation.

<a id="member-2cdda69bc4da"></a>
### R16G16B16UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16UNORM = 83
```

R16G16B16UNORM encoded pixel/block format; query support before creation.

<a id="member-fe8908bfbcc7"></a>
### R16G16B16USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16B16USCALED = 85
```

R16G16B16USCALED encoded pixel/block format; query support before creation.

<a id="member-a3f8ffd6f263"></a>
### R16G16SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16SFLOAT = 82
```

R16G16SFLOAT encoded pixel/block format; query support before creation.

<a id="member-b9375bb54112"></a>
### R16G16SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16SINT = 81
```

R16G16SINT encoded pixel/block format; query support before creation.

<a id="member-1d8db7976542"></a>
### R16G16SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16SNORM = 77
```

R16G16SNORM encoded pixel/block format; query support before creation.

<a id="member-8b1f9b8486bb"></a>
### R16G16SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16SSCALED = 79
```

R16G16SSCALED encoded pixel/block format; query support before creation.

<a id="member-e6b75a4161f0"></a>
### R16G16UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16UINT = 80
```

R16G16UINT encoded pixel/block format; query support before creation.

<a id="member-ea35b2c6e9f5"></a>
### R16G16UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16UNORM = 76
```

R16G16UNORM encoded pixel/block format; query support before creation.

<a id="member-a1afa29a9546"></a>
### R16G16USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16G16USCALED = 78
```

R16G16USCALED encoded pixel/block format; query support before creation.

<a id="member-a8a1e796963c"></a>
### R16SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16SFLOAT = 75
```

R16SFLOAT encoded pixel/block format; query support before creation.

<a id="member-2abf48e0c1af"></a>
### R16SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16SINT = 74
```

R16SINT encoded pixel/block format; query support before creation.

<a id="member-ccd304e516cb"></a>
### R16SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16SNORM = 70
```

R16SNORM encoded pixel/block format; query support before creation.

<a id="member-97b26612c316"></a>
### R16SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16SSCALED = 72
```

R16SSCALED encoded pixel/block format; query support before creation.

<a id="member-e7c9fcb343c1"></a>
### R16UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R16UINT = 73
```

R16UINT encoded pixel/block format; query support before creation.

<a id="member-f82524fcb548"></a>
### R16UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R16UNORM = 69
```

R16UNORM encoded pixel/block format; query support before creation.

<a id="member-e082401027dc"></a>
### R16USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R16USCALED = 71
```

R16USCALED encoded pixel/block format; query support before creation.

<a id="member-d034b445bce7"></a>
### R32G32B32A32SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32A32SFLOAT = 108
```

R32G32B32A32SFLOAT encoded pixel/block format; query support before creation.

<a id="member-eeaf2777a69f"></a>
### R32G32B32A32SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32A32SINT = 107
```

R32G32B32A32SINT encoded pixel/block format; query support before creation.

<a id="member-fa8001055f99"></a>
### R32G32B32A32UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32A32UINT = 106
```

R32G32B32A32UINT encoded pixel/block format; query support before creation.

<a id="member-6afc58c7d54a"></a>
### R32G32B32SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32SFLOAT = 105
```

R32G32B32SFLOAT encoded pixel/block format; query support before creation.

<a id="member-6c52c5dc3eb4"></a>
### R32G32B32SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32SINT = 104
```

R32G32B32SINT encoded pixel/block format; query support before creation.

<a id="member-3ba117f811ff"></a>
### R32G32B32UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32B32UINT = 103
```

R32G32B32UINT encoded pixel/block format; query support before creation.

<a id="member-b1077d5ec088"></a>
### R32G32SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32SFLOAT = 102
```

R32G32SFLOAT encoded pixel/block format; query support before creation.

<a id="member-43aca24bd3fd"></a>
### R32G32SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32SINT = 101
```

R32G32SINT encoded pixel/block format; query support before creation.

<a id="member-0a64ade0058b"></a>
### R32G32UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32G32UINT = 100
```

R32G32UINT encoded pixel/block format; query support before creation.

<a id="member-eec0b9f726a9"></a>
### R32SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32SFLOAT = 99
```

R32SFLOAT encoded pixel/block format; query support before creation.

<a id="member-c7bc82997b0d"></a>
### R32SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32SINT = 98
```

R32SINT encoded pixel/block format; query support before creation.

<a id="member-5fc438e9db12"></a>
### R32UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R32UINT = 97
```

R32UINT encoded pixel/block format; query support before creation.

<a id="member-4b490450d78e"></a>
### R4G4B4A4UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R4G4B4A4UNORMPACK16 = 1
```

R4G4B4A4UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-46a707a826ff"></a>
### R4G4UNORMPACK8

```csharp
public const Electron2D.RenderingDevice.DataFormat R4G4UNORMPACK8 = 0
```

R4G4UNORMPACK8 encoded pixel/block format; query support before creation.

<a id="member-80319e25532b"></a>
### R5G5B5A1UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R5G5B5A1UNORMPACK16 = 5
```

R5G5B5A1UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-2c89856f428c"></a>
### R5G6B5UNORMPACK16

```csharp
public const Electron2D.RenderingDevice.DataFormat R5G6B5UNORMPACK16 = 3
```

R5G6B5UNORMPACK16 encoded pixel/block format; query support before creation.

<a id="member-947e88258bab"></a>
### R64G64B64A64SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64A64SFLOAT = 120
```

R64G64B64A64SFLOAT encoded pixel/block format; query support before creation.

<a id="member-c414e33acd5c"></a>
### R64G64B64A64SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64A64SINT = 119
```

R64G64B64A64SINT encoded pixel/block format; query support before creation.

<a id="member-31e8428eeb1d"></a>
### R64G64B64A64UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64A64UINT = 118
```

R64G64B64A64UINT encoded pixel/block format; query support before creation.

<a id="member-eafdf7cf1e0b"></a>
### R64G64B64SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64SFLOAT = 117
```

R64G64B64SFLOAT encoded pixel/block format; query support before creation.

<a id="member-ff91e69b6d7d"></a>
### R64G64B64SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64SINT = 116
```

R64G64B64SINT encoded pixel/block format; query support before creation.

<a id="member-60cf25e03fc7"></a>
### R64G64B64UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64B64UINT = 115
```

R64G64B64UINT encoded pixel/block format; query support before creation.

<a id="member-856c0acab633"></a>
### R64G64SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64SFLOAT = 114
```

R64G64SFLOAT encoded pixel/block format; query support before creation.

<a id="member-0191aed136d6"></a>
### R64G64SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64SINT = 113
```

R64G64SINT encoded pixel/block format; query support before creation.

<a id="member-2ea633520f51"></a>
### R64G64UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64G64UINT = 112
```

R64G64UINT encoded pixel/block format; query support before creation.

<a id="member-cfa91b1df2f7"></a>
### R64SFLOAT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64SFLOAT = 111
```

R64SFLOAT encoded pixel/block format; query support before creation.

<a id="member-52d5e317cec9"></a>
### R64SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64SINT = 110
```

R64SINT encoded pixel/block format; query support before creation.

<a id="member-8daffb53e437"></a>
### R64UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R64UINT = 109
```

R64UINT encoded pixel/block format; query support before creation.

<a id="member-6ee7336f0f80"></a>
### R8G8B8A8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SINT = 41
```

R8G8B8A8SINT encoded pixel/block format; query support before creation.

<a id="member-2165d33c898a"></a>
### R8G8B8A8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SNORM = 37
```

R8G8B8A8SNORM encoded pixel/block format; query support before creation.

<a id="member-1d081c8b0b9f"></a>
### R8G8B8A8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SRGB = 42
```

R8G8B8A8SRGB encoded pixel/block format; query support before creation.

<a id="member-d48662428690"></a>
### R8G8B8A8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8SSCALED = 39
```

R8G8B8A8SSCALED encoded pixel/block format; query support before creation.

<a id="member-5d1edee02e89"></a>
### R8G8B8A8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8UINT = 40
```

R8G8B8A8UINT encoded pixel/block format; query support before creation.

<a id="member-85016b472f6d"></a>
### R8G8B8A8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8UNORM = 36
```

R8G8B8A8UNORM encoded pixel/block format; query support before creation.

<a id="member-8de5a1aa51a0"></a>
### R8G8B8A8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8A8USCALED = 38
```

R8G8B8A8USCALED encoded pixel/block format; query support before creation.

<a id="member-e4a7d3a70be2"></a>
### R8G8B8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8SINT = 27
```

R8G8B8SINT encoded pixel/block format; query support before creation.

<a id="member-979cb01bdbb0"></a>
### R8G8B8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8SNORM = 23
```

R8G8B8SNORM encoded pixel/block format; query support before creation.

<a id="member-e10a4eab2250"></a>
### R8G8B8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8SRGB = 28
```

R8G8B8SRGB encoded pixel/block format; query support before creation.

<a id="member-07068d78fa44"></a>
### R8G8B8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8SSCALED = 25
```

R8G8B8SSCALED encoded pixel/block format; query support before creation.

<a id="member-4e6505b7dec0"></a>
### R8G8B8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8UINT = 26
```

R8G8B8UINT encoded pixel/block format; query support before creation.

<a id="member-c4b9c8d86cee"></a>
### R8G8B8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8UNORM = 22
```

R8G8B8UNORM encoded pixel/block format; query support before creation.

<a id="member-c852e3e5c71b"></a>
### R8G8B8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8B8USCALED = 24
```

R8G8B8USCALED encoded pixel/block format; query support before creation.

<a id="member-56f380a0753f"></a>
### R8G8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8SINT = 20
```

R8G8SINT encoded pixel/block format; query support before creation.

<a id="member-7d7294a58bfc"></a>
### R8G8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8SNORM = 16
```

R8G8SNORM encoded pixel/block format; query support before creation.

<a id="member-edb3753af44d"></a>
### R8G8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8SRGB = 21
```

R8G8SRGB encoded pixel/block format; query support before creation.

<a id="member-88722196c049"></a>
### R8G8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8SSCALED = 18
```

R8G8SSCALED encoded pixel/block format; query support before creation.

<a id="member-8b020edf91a1"></a>
### R8G8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8UINT = 19
```

R8G8UINT encoded pixel/block format; query support before creation.

<a id="member-2bb856e887e6"></a>
### R8G8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8UNORM = 15
```

R8G8UNORM encoded pixel/block format; query support before creation.

<a id="member-45bddb078d0b"></a>
### R8G8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8G8USCALED = 17
```

R8G8USCALED encoded pixel/block format; query support before creation.

<a id="member-6b8952b7c6f3"></a>
### R8SINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8SINT = 13
```

R8SINT encoded pixel/block format; query support before creation.

<a id="member-5a28687f5738"></a>
### R8SNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8SNORM = 9
```

R8SNORM encoded pixel/block format; query support before creation.

<a id="member-adca3edcb021"></a>
### R8SRGB

```csharp
public const Electron2D.RenderingDevice.DataFormat R8SRGB = 14
```

R8SRGB encoded pixel/block format; query support before creation.

<a id="member-b31a15b3185f"></a>
### R8SSCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8SSCALED = 11
```

R8SSCALED encoded pixel/block format; query support before creation.

<a id="member-57fef4116d67"></a>
### R8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat R8UINT = 12
```

R8UINT encoded pixel/block format; query support before creation.

<a id="member-50c713559de4"></a>
### R8UNORM

```csharp
public const Electron2D.RenderingDevice.DataFormat R8UNORM = 8
```

R8UNORM encoded pixel/block format; query support before creation.

<a id="member-f6d43d6385b3"></a>
### R8USCALED

```csharp
public const Electron2D.RenderingDevice.DataFormat R8USCALED = 10
```

R8USCALED encoded pixel/block format; query support before creation.

<a id="member-ed0fcb187bb6"></a>
### S8UINT

```csharp
public const Electron2D.RenderingDevice.DataFormat S8UINT = 126
```

S8UINT encoded pixel/block format; query support before creation.

<a id="member-cc5bec60491b"></a>
### X8D24UNORMPACK32

```csharp
public const Electron2D.RenderingDevice.DataFormat X8D24UNORMPACK32 = 124
```

X8D24UNORMPACK32 encoded pixel/block format; query support before creation.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
