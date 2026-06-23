# Texture2D resource baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0025` вводит public Electron2D texture resource baseline и internal texture lifetime registry для `0.1.0 Preview`.

Задача закрывает:

- public `Texture2D` resource API для размера, прозрачности и mipmaps;
- public `ImageTexture` для загрузки PNG asset-файлов в immutable image-backed texture;
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

`ImageTexture`:

```csharp
public sealed class ImageTexture : Texture2D
{
    public static ImageTexture LoadFromFile(string path);
}
```

`ImageTexture.LoadFromFile(path)` загружает PNG-файл из filesystem path, читает размеры, alpha channel и RGBA pixels. В `0.1.0 Preview` обязательна поддержка 8-bit PNG color types 2, 4 и 6, indexed palette PNG color type 3 с bit depth 1, 2, 4 или 8, `tRNS` transparency, palette chunks, non-interlaced images и PNG filter types 0-4. JPEG остаётся import-metadata path и не обязан быть runtime texture source для этой задачи.

Публичные методы inherited от `Texture2D` должны возвращать реальные данные изображения:

- `GetWidth()` и `GetHeight()` возвращают размеры PNG;
- `HasAlpha()` возвращает `true`, если PNG имеет alpha channel или transparency metadata;
- `IsPixelOpaque(x, y)` возвращает `true` только когда координаты внутри bounds и alpha пикселя равна `255`;
- запросы за пределами bounds возвращают `false`.

`ImageTexture` immutable: после загрузки пиксели не меняются. Невалидный path, отсутствующий файл, unsupported PNG feature или malformed image fail closed через исключение до создания texture.

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

## Runtime preview presentation

Project runtime host preview rasterizer должен рисовать `ImageTexture` и `AtlasTexture` поверх `ImageTexture` реальными пикселями, а не fallback rectangle. Это нужно для reference games и screenshot acceptance:

- `Sprite2D`, `AnimatedSprite2D`, `TextureRect`, `TextureButton`, `NinePatchRect`, `TileMapLayer` и `CanvasItem.DrawTexture()` должны доходить до texture-backed render commands;
- nearest-neighbor scaling достаточно для preview host;
- alpha pixels должны blend-иться поверх уже нарисованного frame;
- `modulate`, `self_modulate` и command modulate применяются к RGB/A;
- `AtlasTexture.Region` ограничивает source sampling.

## Ограничения `T-0025`

- JPEG runtime texture loading остаётся вне этого среза; PNG import metadata продолжает жить в `resources/texture-image-import.md`.
- Public `CanvasItem` sampling properties остаются `T-0026`.
- Реальный SDL_GPU transfer-buffer upload может быть реализован отдельным backend step; T-0025 фиксирует проверяемый registry contract и adapter boundary.
- `DrawTexture()` реализуется immediate drawing baseline `T-0028`; `DrawTextureRect()` и `DrawTextureRectRegion()` остаются future drawing задачами.

## Фактическое состояние, ограничения и проверки

Статус: реализовано.
Задача: `T-0025`, обновлено в `T-0030` и `T-0037`.
Обновлено: 2026-06-23.

## Public API

В runtime добавлены Electron2D public ресурсы:

- `Texture2D`;
- `AtlasTexture`;
- `ImageTexture`;
- `ViewportTexture` через отдельный baseline [Offscreen render target и восстановление GPU resources](offscreen-render-target-recovery-baseline.md).

`Texture2D` является abstract base resource. Он предоставляет:

- `GetWidth()`;
- `GetHeight()`;
- `GetSize()`;
- `HasAlpha()`;
- `HasMipmaps()`;
- `GetMipmapCount()`;
- `IsPixelOpaque(int x, int y)`.

`AtlasTexture` наследуется от `Texture2D` и добавляет:

- `Atlas`;
- `Region`;
- `Margin`;
- `FilterClip`.

Если у `Region.Size.X` или `Region.Size.Y` значение `0`, для соответствующей оси используется размер atlas texture. Размер региона округляется вниз до `int`, как integer image size.

`ImageTexture.LoadFromFile(path)` загружает PNG из filesystem path или `res://` resource path и возвращает immutable texture с реальными RGBA pixels. Поддерживаются non-interlaced PNG:

- 8-bit RGB, grayscale-alpha и RGBA;
- indexed palette color type 3 с bit depth 1, 2, 4 или 8;
- `tRNS` transparency metadata;
- PNG filter types 0-4.

Unsupported PNG mode, malformed image или отсутствующий файл завершаются исключением до создания texture.

## Transparency

`Texture2D.IsPixelOpaque()` возвращает `true` только для пикселей внутри texture bounds. Базовая реализация считает texture без alpha полностью непрозрачной внутри bounds, а alpha texture требует concrete override.

`AtlasTexture.IsPixelOpaque()` переводит координаты в `Region.Position + (x, y)` и делегирует проверку atlas texture. Запросы за пределами region или atlas возвращают `false`.

## Internal Lifetime Registry

Internal `TextureResourceRegistry` управляет upload/reload/release lifecycle через `ITextureGpuApi`. В этом документе `internal` означает код движка, доступный тестам и будущему runtime/editor host, но не пользовательский public API.

Registry хранит active texture handles, пишет события:

- `Uploaded`;
- `Reloaded`;
- `Released`;
- `RenderTargetCreated`;
- `Restored`;
- `Error`.

`LeakCount` равен количеству active handles, которые ещё не были release. Runtime smoke tests проверяют циклы upload -> reload -> release и upload -> render target -> restore -> release, ожидая `LeakCount == 0`.

Начиная с `T-0030`, registry также создаёт offscreen render targets через `CreateRenderTarget()` и восстанавливает active texture resources после пересоздания device через `RestoreAfterDeviceLoss()`. Подробности описаны в [Offscreen render target и восстановление GPU resources](offscreen-render-target-recovery-baseline.md).

## Sampling Descriptor

Filter/repeat сейчас представлены internal `TextureSamplingOptions`. Public `CanvasItem` уже существует, но GPU sampling ещё не вынесен в public API, потому что настоящий texture drawing pipeline остаётся следующим шагом.

## Ограничения

- PNG/JPEG import metadata реализован в `T-0037` как internal import cache importer. JPEG пока не является runtime texture source.
- Real rendering-backend transfer-buffer upload ещё не реализован; T-0025 фиксирует registry contract и backend adapter boundary.
- `DrawTexture()` реализован в immediate drawing baseline `T-0028`; `DrawTextureRect()` и `DrawTextureRectRegion()` остаются будущими drawing задачами.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
dotnet test tests\Electron2D.Tests.RuntimeSmoke\Electron2D.Tests.RuntimeSmoke.csproj --no-restore
```

Они проверяют public metadata/atlas/viewport/image texture behavior, PNG RGBA и indexed palette loading, internal upload/reload/release registry, atlas upload descriptor, failed upload cleanup, unknown handle rejection, render target descriptor, restore path и no-leak smoke cycles.
