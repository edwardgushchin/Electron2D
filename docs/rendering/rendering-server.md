# `RenderingServer` и renderer profiles

Обновлено: 2026-07-10.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Назначение

`RenderingServer` должен стать внутренней границей Electron2D для всего, что будет отображаться в Electron2D. В `0.1-preview` задача `T-0022` не создаёт настоящий SDL device и не рисует кадр. Она вводит минимальную проверяемую границу:

- публичный singleton-style facade `RenderingServer`;
- два renderer profile: `Compatibility` и `Standard`;
- feature flags через `HasFeature()`;
- internal backend abstraction, скрытую от public node API.

## Источники поведения

- [Godot RenderingServer](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html);
- `docs/releases/0.1-preview.md`, разделы «Два профиля рендеринга» и Android fallback;
- `docs/architecture/engine-platform-stack.md`, раздел «Ключевая архитектурная граница».

Godot `RenderingServer` описывается как непрозрачный backend для всего видимого. Resources создаются server methods и возвращают `Rid`, а nodes не должны знать, какой concrete backend активен.

## Public API

Минимальная публичная поверхность `T-0022`:

```csharp
namespace Electron2D;

public static class RenderingServer
{
    public enum RenderingProfile
    {
        Compatibility,
        Standard
    }

    public enum RenderingFeature
    {
        Sprites,
        Animation,
        TileMap,
        Ui,
        Text,
        Primitives,
        Camera,
        Clipping,
        StandardBlendModes,
        RenderTargets,
        CustomShaders,
        ShaderMaterial,
        MultiPass,
        AdvancedBlending,
        PostProcessing
    }

    public static RenderingProfile CurrentProfile { get; }

    public static bool HasFeature(RenderingFeature feature);
}
```

Enums вложены в `RenderingServer`, чтобы не разбрасывать renderer-specific имена по корневому namespace и сохранить Electron2D singleton/server shape.

`CurrentProfile`, `HasFeature`, `RenderingProfile` и `RenderingFeature` являются отдельными owner-approved Electron2D extensions. Они не сопоставляются с устаревшим Godot `RenderingServer.Features`, не заменяют Godot `has_os_feature` и не получают статус Godot parity. Manual profile обязан классифицировать каждый такой экспортированный member/value как `electronExtension`; generated manifest публикует для него `parity = not_applicable`. Сам `RenderingServer` остаётся публичной 2D-границей, а Godot members вне утверждённого 2D/HLSL subset получают `Deferred` или `Unsupported`.

## Internal backend abstraction

Минимальная internal граница:

```csharp
internal interface IRenderingBackend
{
    string Name { get; }
    RenderingServer.RenderingProfile Profile { get; }
    bool HasFeature(RenderingServer.RenderingFeature feature);
}
```

Internal backends `StandardRenderingBackend` и `CompatibilityRenderingBackend` должны реализовывать эту границу. Тесты могут переключать backend через internal test hook, но public API не должен раскрывать concrete backend types.

## Feature policy

`Compatibility` гарантирует:

- `Sprites`;
- `Animation`;
- `TileMap`;
- `Ui`;
- `Text`;
- `Primitives`;
- `Camera`;
- `Clipping`;
- `StandardBlendModes`.

`Compatibility` не гарантирует:

- `RenderTargets`;
- `CustomShaders`;
- `ShaderMaterial`;
- `MultiPass`;
- `AdvancedBlending`;
- `PostProcessing`.

Текущий production `Standard` включает тот же подтверждённый набор features, что и `Compatibility`. Профиль `Standard` означает выбор SDL GPU presenter, но сам по себе не является доказательством подключения дополнительных consumer paths. Поэтому `RenderTargets`, `CustomShaders`, `ShaderMaterial`, `MultiPass`, `AdvancedBlending` и `PostProcessing` возвращают `false` и могут стать доступными только после отдельной реализации, production-подключения и проверки соответствующего пути.

## Интерактивный reusable path

Для интерактивного runtime `RenderingServer` является общей границей между сценой и показом кадра. `RuntimeHost` не должен иметь отдельную ручную систему программной растеризации для обычной игры: он собирает `CanvasItemRenderPlan`, передаёт его активному внутреннему модулю отрисовки и использует долгоживущий объект показа кадра (`presenter`), который живёт вместе с окном.

Основной интерактивный `presenter` использует SDL GPU: ресурсы окна создаются один раз, draw-команды проходят через графические конвейеры (`graphics pipelines`), проход отрисовки (`render pass`) и вызовы draw. Если SDL GPU недоступен или отвергает создание ресурсов на конкретной платформе/драйвере, runtime выбирает запасной `SDL_Renderer` `presenter`. Запасной путь остаётся частью общего пути `RenderingServer`: он получает тот же `CanvasItemRenderPlan`, сохраняет порядок и пакеты, кеширует текстуры и не использует программный `RuntimePixelCanvas` как окно.

PNG-снимок создаётся из активного `presenter`, а не из отдельной программной растеризации: основной путь после той же отрисовки читает GPU-текстуру через буфер чтения, запасной путь читает текущую цель `SDL_Renderer`. Поэтому PNG отражает тот же порядок команд, пакеты и выбранный путь показа кадра, что и окно.

Запасной путь в Preview может оставаться ограниченным по возможностям, но он не должен превращать draw-команды в визуально другие примитивы. Текст должен оставаться читаемыми glyphs, texture flip должен применяться через UV, круг и непрямоугольный полигон должны оставаться соответствующей геометрией. Для polygon-команд основной и запасной presenter-ы используют цвета вершин, умноженные на итоговый modulate. Если возможность не поддерживается, `presenter` обязан вернуть явную диагностируемую ошибку, а не молча заменить команду прямоугольником.

Ресурсы запасного пути должны быть долгоживущими для неизменного размера окна: ресурс показа кадра, кеш текстур и буферы отправки кадра не создаются заново каждый кадр. При изменении размера старые ресурсы показа кадра освобождаются, новые создаются один раз для нового размера. При завершении `presenter` освобождает все свои ресурсы и делает это идемпотентно.

Кеш загрузки текстур должен загружать конкретный `Texture2D` только при первом использовании или после изменения версии содержимого ресурса. Повторная отправка той же неизменной текстуры в устойчивом состоянии должна считаться повторным использованием, а не новой загрузкой. GPU upload проходит через staged state и попадает в committed cache только после успешной отправки command buffer; ошибка до submit освобождает staged texture и не меняет счётчики успешно зафиксированных загрузок или попаданий в кеш. Диагностика runtime должна сообщать draw commands, плановые пакеты, фактические draw-вызовы, выбранный модуль показа кадра, переход на запасной путь, причину перехода, переключения текстуры и pipeline state, загрузки текстуры, попадания в кеш текстур, создания ресурсов показа кадра, пересоздания принадлежащих Electron2D ресурсов, наблюдённые изменения размера окна, перенастройки backend/swapchain и фактические управляемые выделения памяти после прогрева на границе `presenter`. Эта метрика не включает построение `CanvasItemRenderPlan`; полный бюджет кадра проверяет отдельный performance gate.

## Acceptance tests

- Default backend сообщает `Compatibility` profile и только compatibility features.
- Production-выбор SDL GPU presenter меняет `CurrentProfile` на `Standard`, но не включает неподключённые `RenderTargets`, `CustomShaders`, `ShaderMaterial`, `MultiPass`, `AdvancedBlending` и `PostProcessing`.
- Production-выбор или runtime fallback на SDL Renderer устанавливает `Compatibility`; оба профиля сообщают `false` для неподключённых features.
- Public exported types не содержат internal backend interfaces/classes.
- Public node types не имеют public members, которые раскрывают concrete rendering backend.
- API compatibility table отражает новые public types.

## Ограничения `T-0022`

- SDL3-CS device creation, GPU smoke test, fallback logging и resource restore не реализуются в этой задаче.
- Public `Node2D`, `CanvasItem`, `Sprite2D`, shaders, texture upload и отправка canvas-команд не входили в `T-0022`; базовый интерактивный путь с загрузкой текстур и draw-вызовами подключён позже в `T-0219`.
- OpenGL ES backend не добавляется.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline, дополнен интерактивным путём `T-0219`.
Задачи: `T-0022`, `T-0023`, `T-0024`, `T-0025`, `T-0031`, `T-0032`, `T-0033`, `T-0034`, `T-0219`.
Обновлено: 2026-06-25.

## Public API

Текущий runtime экспортирует:

- `Electron2D.RenderingServer`;
- `Electron2D.RenderingServer.RenderingProfile`;
- `Electron2D.RenderingServer.RenderingFeature`.

`RenderingServer` - Electron2D singleton-style facade для запроса активного renderer profile и feature flags. Concrete backend classes не являются public API.

## Профили

До создания production `RuntimeFramePresenter` публичный `RenderingServer` использует исходный профиль `Compatibility`. Успешный выбор `SDL_GPU` переключает публичный профиль на `Standard`. Выбор или runtime-переход на `SDL_Renderer` устанавливает `Compatibility`.

Начиная с `T-0033` internal `CompatibilityRenderingBackend` строит compatibility frame plan из `CanvasItemRenderPlan`. Этот план описывает команды для sprites, UI/text, primitives и tile-like texture copies, но пока не создаёт real-window presentation.

`RenderingProfile.Standard` идентифицирует успешно выбранный SDL GPU backend. Начиная с `T-0023` internal standard backend ведёт state machine для создания graphics device, привязки окна, begin/end frame и shutdown. Этот lifecycle не является public API, а профиль сам по себе не обещает ещё не подключённые shader/material/post-processing возможности.

Начиная с `T-0034` internal startup policy умеет создать graphics device в Android mobile-compatible profile, выполнить smoke steps и выбрать `Compatibility` fallback либо structured failure по policy.

## Feature flags

Compatibility profile поддерживает:

- `Sprites`;
- `Animation`;
- `TileMap`;
- `Ui`;
- `Text`;
- `Primitives`;
- `Camera`;
- `Clipping`;
- `StandardBlendModes`.

Текущий production `Standard` presenter поддерживает тот же наблюдаемый набор feature flags, что и `Compatibility`. `RenderTargets`, `CustomShaders`, `ShaderMaterial`, `MultiPass`, `AdvancedBlending` и `PostProcessing` возвращают `false`, пока соответствующие consumer paths не подключены к `RuntimeHost`/SDL GPU draw pipeline.

## Internal backend boundary

Внутренняя граница включает renderer profile adapters, startup policy, smoke-test result, canvas command queue, texture registry, shader import boundary, material parameter snapshot и compatibility frame plan. Эти типы доступны runtime/tests, но не экспортируются из assembly. Публичные node-типы не раскрывают concrete rendering backend через public members.

## Ограничения

- Real-window graphics smoke test ещё не запускается в CI: проверяется deterministic fake adapter, а production adapter требует реальный native window handle.
- Startup fallback result уже содержит selected backend, GPU, driver и reasons; интеграция этого result в editor/CLI logging будет отдельной задачей.
- `RuntimeFramePresenter`, который создаётся production `RuntimeHost`, синхронизирует публичный `RenderingServer` с фактически выбранным presenter: успешный `SDL_GPU` публикует профиль `Standard`, создание или runtime-переключение на `SDL_Renderer` публикует `Compatibility`.
- `CompatibilityRenderingBackend` уже строит internal command plan. Начиная с `T-0219`, интерактивный runtime формирует общий `CanvasItemRenderPlan` и отправляет его через фактически выбранный presenter; PNG-снимок читается из активного пути показа кадра.
- Public `ShaderMaterial`, uniforms, samplers и reserved canvas built-ins реализованы как resource/model layer.
- Загрузка текстур и отправка canvas-команд реализованы для базовых интерактивных команд `T-0219`; пакеты с учётом материалов и расширенная привязка shader-ресурсов остаются отдельным развитием draw pipeline.
- Shader import/material baseline уже создаёт compiled artifacts и serializable material parameter snapshots, но не привязывает их к реальному draw pipeline.
- OpenGL ES backend не добавлен.

## Проверки

- Unit tests фиксируют default compatibility profile, feature flags, public API baseline и отсутствие public backend leaks.
- Integration tests переключают internal `StandardRenderingBackend`/`CompatibilityRenderingBackend` и проверяют `CurrentProfile`/`HasFeature()`.
- Integration tests проверяют internal standard graphics lifecycle: initialize, frame begin/submit, resize/high-DPI, fullscreen, device errors и недопустимый порядок frame calls.
- Integration tests проверяют Android mobile graphics create options, smoke steps texture/pipeline/command buffer/first submit, `Automatic` fallback, `FailIfUnavailable` и startup log.
- Integration tests проверяют internal canvas item render queue: stable sorting, y-sort, visibility, modulate и batching.
- Unit/integration/runtime smoke tests проверяют public texture metadata/atlas behavior и internal texture upload/reload/release leak tracking.
- Unit/integration/runtime smoke tests проверяют public `Shader`, import-time vertex/fragment compilation boundary, diagnostics file/line/column и iOS artifact без runtime compilation.
- Unit/integration/golden-data tests проверяют public `Material`/`ShaderMaterial`, supported uniforms, texture sampler snapshot, reserved built-ins и stable JSON material parameter snapshot.
- Integration/golden-data tests проверяют compatibility frame plan, documented limitations и stable reference scene command stream.
