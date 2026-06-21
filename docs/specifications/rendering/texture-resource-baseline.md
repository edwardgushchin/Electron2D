# Texture2D resource baseline

## Назначение

`T-0025` вводит public Electron2D texture resource baseline и internal texture lifetime registry для `0.1.0 Preview`.

Задача закрывает:

- public `Texture2D` resource API для размера, прозрачности и mipmaps;
- public `AtlasTexture` для atlas regions, margin и filter clipping;
- internal upload/reload/release registry с leak tracking;
- internal sampling descriptor для texture filter/repeat, который future `CanvasItem` nodes будут передавать в renderer;
- runtime smoke test, который доказывает отсутствие остаточных active texture handles после release.

## Источники поведения

- [Godot `Texture2D`](https://docs.godotengine.org/en/stable/classes/class_texture2d.html): public resource exposes width, height, size, alpha, mipmaps, pixel opacity and draw methods.
- [Godot `AtlasTexture`](https://docs.godotengine.org/en/stable/classes/class_atlastexture.html): region wrapper over another `Texture2D` with `atlas`, `filter_clip`, `margin` and `region`.
- [Godot `CanvasItem`](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html): `TextureFilter` и `TextureRepeat` принадлежат CanvasItem sampling state.
- [SDL GPU](https://wiki.libsdl.org/SDL3/CategoryGPU): textures are created/uploaded/released on the GPU timeline, while samplers describe how textures are read.

## Public API

`Texture2D`:

```csharp
public abstract class Texture2D : Resource
{
    public abstract int GetWidth();
    public abstract int GetHeight();
    public Vector2 GetSize();
    public abstract bool HasAlpha();
    public virtual bool HasMipmaps();
    public virtual int GetMipmapCount();
    public virtual bool IsPixelOpaque(int x, int y);
}
```

`AtlasTexture`:

```csharp
public sealed class AtlasTexture : Texture2D
{
    public Texture2D? Atlas { get; set; }
    public bool FilterClip { get; set; }
    public Rect2 Margin { get; set; }
    public Rect2 Region { get; set; }
}
```

Все public members должны иметь XML documentation в SDL-like C# стиле: `summary`, `remarks` при необходимости, `param`, `returns`, `threadsafety`, `since` и `seealso` для связанных API.

## Atlas behavior

`AtlasTexture.GetWidth()` и `GetHeight()` возвращают:

- `0`, если `Atlas == null`;
- размер `Region.Size`, если соответствующая ось region больше `0`;
- размер `Atlas`, если соответствующая ось region равна `0`.

Размер округляется вниз до `int`, потому что Godot documentation указывает integer image size.

`AtlasTexture.HasAlpha()`, `HasMipmaps()`, `GetMipmapCount()` и `IsPixelOpaque()` делегируют данные atlas texture. `IsPixelOpaque(x, y)` переводит координаты в `Region.Position + (x, y)` и возвращает `false` за пределами видимого atlas region.

## Internal texture lifetime

Минимальная internal surface:

```csharp
internal sealed class TextureResourceRegistry
{
    int ActiveTextureCount { get; }
    int LeakCount { get; }
    IReadOnlyList<TextureResourceEvent> Events { get; }

    TextureResourceHandle Upload(Texture2D texture, TextureSamplingOptions sampling);
    void Reload(TextureResourceHandle handle, Texture2D texture);
    bool Release(TextureResourceHandle handle);
}
```

Native/backend calls проходят через adapter:

```csharp
internal interface ITextureGpuApi
{
    bool Upload(Rid texture, TextureUploadDescriptor descriptor, out string? error);
    bool Reload(Rid texture, TextureUploadDescriptor descriptor, out string? error);
    bool Release(Rid texture, out string? error);
}
```

`TextureUploadDescriptor` содержит width, height, alpha, mipmaps, source region, margin, filter clip и sampling options.

`TextureSamplingOptions` содержит internal filter/repeat values. Public `CanvasItem.TextureFilter` и `CanvasItem.TextureRepeat` появятся в `T-0026`; T-0025 не должен добавлять отдельные public non-Godot enums.

## Diagnostics

Registry пишет события:

- `Uploaded`;
- `Reloaded`;
- `Released`;
- `Error`.

Ошибки upload/reload/release не должны оставлять active handle в registry. `LeakCount` равен количеству unreleased active texture handles.

## Ограничения `T-0025`

- PNG/JPEG import metadata реализуется отдельной спецификацией `resources/texture-image-import.md`; public `ImageTexture` остаётся будущей Electron2D API задачей.
- Public `CanvasItem` sampling properties остаются `T-0026`.
- Реальный SDL_GPU transfer-buffer upload может быть реализован отдельным backend step; T-0025 фиксирует проверяемый registry contract и adapter boundary.
- `DrawTexture()` реализуется immediate drawing baseline `T-0028`; `DrawTextureRect()` и `DrawTextureRectRegion()` остаются future drawing задачами.
