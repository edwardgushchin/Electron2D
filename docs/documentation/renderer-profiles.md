# Документация renderer profiles

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0099`.
Обновлено: 2026-06-21.
Связанные документы: [Пользовательская документация 0.1-preview](user-documentation.md), [Electron2D 0.1-preview](../releases/0.1-preview.md), [`RenderingServer` и renderer profiles](../rendering/rendering-server.md).

## Назначение

Пользовательская документация должна объяснить, как в `0.1-preview` выбирать renderer profile, как читать feature flags и как работает fallback policy на Android. Документ не должен обещать реальный window presentation, shader execution или mobile export smoke раньше соответствующих задач.

## Обязательный контент

Документация должна покрывать:

- `Compatibility` как минимальный профиль для базовой 2D-сцены;
- `Standard` как профиль с расширенными возможностями;
- `RenderingServer.CurrentProfile`;
- `RenderingServer.HasFeature(...)`;
- полный список feature flags, разделённый на базовые и standard-only;
- настройку export preset `rendererProfile`;
- поведение `Automatic`;
- поведение `FailIfUnavailable`;
- Android fallback policy;
- ограничения `0.1-preview`, включая незавершённый real-window rendering path и отсутствие гарантии visual parity между профилями.

## Проверяемость

`dotnet run --project eng/Electron2D.Build -- verify user-documentation` должен проверять:

- наличие страницы `docs/renderer-profiles.md`;
- наличие раздела `user-doc:renderer-profiles` в `user-guide.md`;
- упоминание `RenderingServer.CurrentProfile`, `RenderingServer.HasFeature`, `Compatibility`, `Standard`, `Automatic`, `FailIfUnavailable`, `fail_if_unavailable`, `Android fallback` и `feature flags`;
- отсутствие запрещённых публичных формулировок.

## Критерии приёмки

- User guide ссылается на отдельную renderer-памятку.
- Renderer-памятка описывает feature flags, fallback policy, `fail_if_unavailable` и ограничения.
- Внутренние backend details не выдаются за публичный API.
- `dotnet run --project eng/Electron2D.Build -- verify user-documentation` проходит локально и в CI.

## Фактическое состояние, ограничения и проверки

Эта страница описывает текущий пользовательский контракт renderer profiles в Electron2D `0.1-preview`.

## Быстрый выбор

Используйте `Compatibility`, если нужна минимальная 2D-сцена с sprites, UI, text, primitives, camera и стандартным blending. Это профиль по умолчанию в текущем runtime baseline.

Используйте `Standard`, если проекту нужны расширенные возможности renderer feature flags: render targets, custom shaders, `ShaderMaterial`, multipass, advanced blending или post-processing. В `0.1-preview` часть этих возможностей уже присутствует как resource/import/model layer, но не все они привязаны к реальному window rendering path.

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

## Ограничения `0.1-preview`

- Real-window rendering path ещё не является production-ready.
- `Compatibility` уже строит внутренний command plan, но не гарантирует pixel-perfect parity со `Standard`.
- `TileMap` feature flag зарезервирован за renderer path, но public `TileMap` node ещё не реализован.
- Shader import и `ShaderMaterial` metadata уже проверяются, но фактическое выполнение custom shader в сцене остаётся задачей renderer integration.
- Android fallback policy проверяется deterministic tests; real-device Android smoke остаётся отдельной export-задачей.

## Проверки

Проверить актуальность этой страницы:

```bash
dotnet run --project eng/Electron2D.Build -- verify user-documentation
```
