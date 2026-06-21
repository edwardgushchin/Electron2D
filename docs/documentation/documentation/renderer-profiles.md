# Renderer profiles

Эта страница описывает текущий пользовательский контракт renderer profiles в Electron2D `0.1.0 Preview`.

## Быстрый выбор

Используйте `Compatibility`, если нужна минимальная 2D-сцена с sprites, UI, text, primitives, camera и стандартным blending. Это профиль по умолчанию в текущем runtime baseline.

Используйте `Standard`, если проекту нужны расширенные возможности renderer feature flags: render targets, custom shaders, `ShaderMaterial`, multipass, advanced blending или post-processing. В `0.1.0 Preview` часть этих возможностей уже присутствует как resource/import/model layer, но не все они привязаны к реальному window rendering path.

## Проверка профиля из кода

Текущий профиль доступен через:

```csharp
var profile = RenderingServer.CurrentProfile;
```

Перед использованием возможности проверяйте feature flag:

```csharp
if (RenderingServer.HasFeature(RenderingServer.RenderingFeature.CustomShaders))
{
    // Enable shader-specific scene setup.
}
```

Такой код остаётся переносимым между `Compatibility`, `Standard` и будущим automatic fallback.

## Feature flags

`Compatibility` поддерживает:

- `Sprites`;
- `Animation`;
- `TileMap`;
- `Ui`;
- `Text`;
- `Primitives`;
- `Camera`;
- `Clipping`;
- `StandardBlendModes`.

`Standard` включает все flags `Compatibility` и добавляет:

- `RenderTargets`;
- `CustomShaders`;
- `ShaderMaterial`;
- `MultiPass`;
- `AdvancedBlending`;
- `PostProcessing`.

Если feature flag выключен, проект должен скрывать зависящий от него путь или использовать более простой fallback. Не полагайтесь на имя платформы вместо `RenderingServer.HasFeature(...)`: на Android automatic fallback может выбрать другой профиль.

## Export preset

В export preset поле `rendererProfile` может хранить:

- `Automatic` - попытаться использовать расширенный профиль и перейти на минимальный профиль, если проверка устройства не проходит;
- `Compatibility` - сразу использовать минимальный профиль;
- `Standard` - использовать расширенный профиль;
- `FailIfUnavailable` - требовать расширенный профиль и завершать запуск понятной ошибкой, если устройство не проходит проверку.

Для пользовательских настроек это же поведение может отображаться как `automatic`, `compatibility`, `standard` и `fail_if_unavailable`.

## Android fallback

На Android профиль `Automatic` должен проверять базовый graphics startup path перед запуском сцены. Если устройство или драйвер не проходит smoke-check, runtime выбирает `Compatibility` и сохраняет причину fallback для будущего editor/CLI output.

`FailIfUnavailable` нужен для проектов, где визуальный результат зависит от standard-only features. В этом режиме runtime не должен молча переходить на `Compatibility`: он должен остановить запуск и вывести диагностическое сообщение.

## Ограничения `0.1.0 Preview`

- Real-window rendering path ещё не является production-ready.
- `Compatibility` уже строит внутренний command plan, но не гарантирует pixel-perfect parity со `Standard`.
- `TileMap` feature flag зарезервирован за renderer path, но public `TileMap` node ещё не реализован.
- Shader import и `ShaderMaterial` metadata уже проверяются, но фактическое выполнение custom shader в сцене остаётся задачей renderer integration.
- Android fallback policy проверяется deterministic tests; real-device Android smoke остаётся отдельной export-задачей.

## Проверки

Проверить актуальность этой страницы:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UserDocumentation.ps1
```
