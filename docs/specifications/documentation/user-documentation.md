# Пользовательская документация 0.1.0 Preview

Статус: целевая спецификация для `T-0097`, `T-0099` и `T-0101`.
Обновлено: 2026-06-21.

## Назначение

Пользовательская документация должна дать разработчику проверенный путь от установки до запуска первого проекта Electron2D `0.1.0 Preview`. Документация не должна обещать готовые editor workflows, mobile export или другие возможности, которые ещё не имеют проверки в текущем baseline.

## Обязательные разделы

Документация должна покрывать:

- установку и требования окружения;
- создание первого проекта;
- структуру первой сцены;
- C# scripting baseline;
- ресурсы, импорт и сериализацию;
- physics baseline;
- UI baseline;
- animation baseline;
- Input Map и ввод;
- renderer profiles, feature flags и fallback policy;
- export baseline, включая общую export guide, desktop verifier commands и mobile status gaps;
- troubleshooting;
- release checklist.

## Проверяемость

Команды документации должны ссылаться на существующие локальные verifier scripts:

- `tools\Verify-ProjectTemplate.ps1` для чистого проекта;
- `tools\Run-Tests.ps1` для полного тестового набора;
- platform-specific export verifiers для desktop export.
- `tools\Verify-ExportDocumentation.ps1` для полноты export guide, platform pages, secret policy и mobile limitations.

Документация может показывать ручные команды `dotnet restore`, `dotnet build` и `dotnet run`, но должна явно отделять release-package команды от локального repository verification path.

## Screenshots

До завершения Editor UI задач пользовательская документация не должна включать screenshots. Если screenshots появятся позже, verifier обязан проверять, что все image links указывают на существующие файлы.

## Запреты

- Не описывать непроверенное поведение как готовое.
- Не публиковать secrets, real credentials, private keys или signing payloads.
- Не использовать публичные объяснения через сторонние API-style labels, кроме README.
- Не добавлять устаревшие screenshot placeholders.

## Критерии приёмки

- User guide существует в `docs/documentation/documentation/user-guide.md`.
- Guide содержит все обязательные разделы.
- Guide ссылается на проверяемые команды, текущие verifier scripts и export guide.
- Guide не содержит запрещённых публичных формулировок.
- Screenshot links отсутствуют или указывают на существующие файлы.
- `tools\Verify-UserDocumentation.ps1` проходит локально и в CI.
