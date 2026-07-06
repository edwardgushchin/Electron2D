# Texture2D resource baseline

Обновлено: 2026-06-25.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`T-0025` вводит Electron2D texture resource behavior baseline и internal texture lifetime registry для `0.1-preview`.

Задача закрывает:

- resource-level metadata для размера, прозрачности и mipmaps;
- загрузку PNG asset-файлов в immutable image-backed texture;
- atlas regions, margin и filter clipping behavior;
- internal upload/reload/release registry с leak tracking;
- internal sampling descriptor для texture filter/repeat, который future `CanvasItem` nodes будут передавать в renderer;
- runtime smoke test, который доказывает отсутствие остаточных active texture handles после release.

## Источники поведения

- [Godot `Texture2D`](https://docs.godotengine.org/en/stable/classes/class_texture2d.html): публичный ресурс описывает размер, формат, копию изображения, прозрачность, mipmap-уровни, проверку прозрачности пикселя, placeholder и методы отрисовки.
- [Godot `AtlasTexture`](https://docs.godotengine.org/en/stable/classes/class_atlastexture.html): представление области другого `Texture2D` с `atlas`, `filter_clip`, `margin` и `region`.
- [Godot `CanvasItem`](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html): `TextureFilter` и `TextureRepeat` принадлежат CanvasItem sampling state.
- [SDL GPU](https://wiki.libsdl.org/SDL3/CategoryGPU): textures are created/uploaded/released on the GPU timeline, while samplers describe how textures are read.

## Уточнение совместимости Godot 4.7

`T-0025` реализовала только минимальный слой `Texture2D`: размер, прозрачность, mipmap-данные и проверку прозрачности пикселя. Это не полное соответствие Godot 4.7 public API contract.

`T-0226` должен добавить недостающий базовый texture contract: доступ к формату и копии изображения, placeholder-resource behavior, draw-entry behavior и внутренний переопределяемый контракт отрисовки. Точный список public members берётся из generated API manifest и Wiki compatibility table.

`T-0227` должен после `T-0226` привести atlas resource к тому же контракту: рекурсивный atlas source, запрет прямой ссылки на себя, region/margin/filter clipping behavior, image copy и обрезку области при отрисовке. Точные public signatures остаются generated source of truth.

## Контракт публичной поверхности

Публичная texture-domain поверхность задаётся `data/api/electron2d-api-manifest.json`, GitHub Wiki `API-Compatibility.md` и `verify api-compatibility --wiki-path .github/wiki`. Этот документ описывает поведение и ограничения texture resources, но не повторяет полный список public types, properties или methods.

`ImageTexture.LoadFromFile(path)` загружает PNG-файл из filesystem path, читает размеры, alpha channel и RGBA pixels. В `0.1-preview` обязательна поддержка 8-bit PNG color types 2, 4 и 6, indexed palette PNG color type 3 с bit depth 1, 2, 4 или 8, `tRNS` transparency, palette chunks, non-interlaced images и PNG filter types 0-4. JPEG остаётся import-metadata path и не обязан быть runtime texture source для этой задачи.

Публичные texture queries должны возвращать реальные данные изображения:

- `GetWidth()` и `GetHeight()` возвращают размеры PNG;
- `HasAlpha()` возвращает `true`, если PNG имеет alpha channel или transparency metadata;
- `IsPixelOpaque(x, y)` возвращает `true` только когда координаты внутри bounds и alpha пикселя равна `255`;
- запросы за пределами bounds возвращают `false`.

`ImageTexture` immutable: после загрузки пиксели не меняются. Невалидный path, отсутствующий файл, unsupported PNG feature или malformed image fail closed через исключение до создания texture.

Все public members, перечисленные в generated manifest, должны иметь XML documentation в SDL-like C# стиле: `summary`, `remarks` при необходимости, `param`, `returns`, `threadsafety`, `since` и `seealso` для связанных API.

## Atlas behavior

`AtlasTexture.GetWidth()` и `GetHeight()` возвращают:

- размер соответствующей оси `Atlas`, если округлённая вниз ось `Region.Size` равна `0` и `Atlas` задан;
- поведение отсутствующего `Atlas` при нулевой оси фиксируется по исходному коду Godot в `T-0227`;
- округлённый вниз размер `Region.Size` плюс `Margin.Size`, если ось region больше `0`.

Размер округляется вниз до `int`, потому что контракт Godot работает с целочисленным размером изображения.

`AtlasTexture.HasAlpha()`, `HasMipmaps()` и `GetMipmapCount()` делегируют данные atlas texture. `IsPixelOpaque(x, y)` переводит координаты с учётом `Region.Position - Margin.Position` и возвращает `false`, если результат попадает за пределы atlas texture.

`AtlasTexture.GetImage()` возвращает копию вырезанного региона исходной texture. `AtlasTexture.Draw(...)`, `DrawRect(...)` и `DrawRectRegion(...)` используют контракт отрисовки базового `Texture2D`: обрезка исходной области должна пропорционально корректировать destination rectangle, включая destination rectangle с разворотом.

`FilterClip` должен влиять на выборку пикселей и предотвращать чтение соседних пикселей за пределами atlas region. `Atlas` может быть любым `Texture2D`, включая другой `AtlasTexture`; прямая ссылка на самого себя запрещена.

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

Project runtime host preview rasterizer должен рисовать image-backed и atlas-backed texture resources реальными пикселями, а не fallback rectangle. Это нужно для reference games и screenshot acceptance:

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

Статус: минимальный слой `T-0025` реализован; полная совместимость с Godot 4.7 public API contract открыта задачами `T-0226` и `T-0227`.
Задача: `T-0025`, обновлено в `T-0030`, `T-0037`, `T-0226` и `T-0227`.
Обновлено: 2026-06-25.

## Runtime behavior

Runtime texture resources и offscreen texture resources перечисляются в generated API manifest; `ViewportTexture` описан в отдельном baseline [Offscreen render target и восстановление GPU resources](offscreen-render-target-recovery-baseline.md).

Base texture resource behavior покрывает размер, alpha, mipmap metadata и opaque-pixel checks. Atlas behavior добавляет source texture, region, margin и filter clipping semantics без ручного списка public members в этом документе.

Текущее состояние до `T-0227`: если у `Region.Size.X` или `Region.Size.Y` значение `0`, для соответствующей оси используется размер atlas texture; размер региона округляется вниз до `int`, как целочисленный размер изображения. `Margin` и `FilterClip` сохраняются как свойства, но ещё не выполняют полный контракт отрисовки и выборки пикселей Godot 4.7. Внутренний механизм показа кадра из `T-0219` уже разрешает вложенные atlas-backed resources для поддержанного image-backed subset, но полный контракт atlas resource, включая margin, filter clipping, image copy и drawing behavior, остаётся задачей `T-0227`.

`ImageTexture.LoadFromFile(path)` загружает PNG из filesystem path или `res://` resource path и возвращает immutable texture с реальными RGBA pixels. Поддерживаются non-interlaced PNG:

- 8-bit RGB, grayscale-alpha и RGBA;
- indexed palette color type 3 с bit depth 1, 2, 4 или 8;
- `tRNS` transparency metadata;
- PNG filter types 0-4.

Unsupported PNG mode, malformed image или отсутствующий файл завершаются исключением до создания texture.

## Transparency

`Texture2D.IsPixelOpaque()` возвращает `true` только для пикселей внутри texture bounds. Базовая реализация считает texture без alpha полностью непрозрачной внутри bounds, а alpha texture требует concrete override.

Текущее состояние до `T-0227`: `AtlasTexture.IsPixelOpaque()` переводит координаты в `Region.Position + (x, y)` и делегирует проверку atlas texture. Это не учитывает `Margin.Position`; исправление закреплено в `T-0227`.

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
- Полный registry contract остаётся границей `T-0025`, а интерактивный runtime presenter из `T-0219` уже использует транзакционную загрузку через staged state: texture resource попадает в committed cache и в счётчик `TextureUploads` только после успешной отправки кадра.
- `DrawTexture()` реализован в immediate drawing baseline `T-0028`; `DrawTextureRect()` и `DrawTextureRectRegion()` остаются будущими drawing задачами.

## Проверки

Целевые наборы:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --no-restore
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --no-restore
dotnet test tests\Electron2D.Tests.RuntimeSmoke\Electron2D.Tests.RuntimeSmoke.csproj --no-restore
```

Они проверяют public metadata/atlas/viewport/image texture behavior, PNG RGBA и indexed palette loading, internal upload/reload/release registry, atlas upload descriptor, failed upload cleanup, unknown handle rejection, render target descriptor, restore path и no-leak smoke cycles. В runtime presenter path `T-0219` дополнительно проверяется staged GPU upload: texture resource не попадает в committed cache, `TextureUploads` не увеличивается и native texture/transfer buffer освобождаются ровно один раз, если кадр падает до успешной отправки command buffer.
