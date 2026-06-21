# Документация renderer profiles

Статус: целевая спецификация для `T-0099`.
Обновлено: 2026-06-21.
Связанные документы: [Пользовательская документация 0.1.0 Preview](user-documentation.md), [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [`RenderingServer` и renderer profiles](../rendering/rendering-server.md).

## Назначение

Пользовательская документация должна объяснить, как в `0.1.0 Preview` выбирать renderer profile, как читать feature flags и как работает fallback policy на Android. Документ не должен обещать реальный window presentation, shader execution или mobile export smoke раньше соответствующих задач.

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
- ограничения `0.1.0 Preview`, включая незавершённый real-window rendering path и отсутствие гарантии visual parity между профилями.

## Проверяемость

`tools\Verify-UserDocumentation.ps1` должен проверять:

- наличие страницы `docs/documentation/documentation/renderer-profiles.md`;
- наличие раздела `user-doc:renderer-profiles` в `user-guide.md`;
- упоминание `RenderingServer.CurrentProfile`, `RenderingServer.HasFeature`, `Compatibility`, `Standard`, `Automatic`, `FailIfUnavailable`, `fail_if_unavailable`, `Android fallback` и `feature flags`;
- отсутствие запрещённых публичных формулировок.

## Критерии приёмки

- User guide ссылается на отдельную renderer-памятку.
- Renderer-памятка описывает feature flags, fallback policy, `fail_if_unavailable` и ограничения.
- Внутренние backend details не выдаются за публичный API.
- `tools\Verify-UserDocumentation.ps1` проходит локально и в CI.
