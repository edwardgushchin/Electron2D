# Reproducibility lock и `e2d doctor`

Статус: целевая спецификация для `T-0126`.
Обновлено: 2026-06-22.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [Формат проекта и шаблон electron2d-empty](../release-management/project-template.md), [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md), [Export preset model and toolchain validation](../export/export-preset-model.md).

## Назначение

Reproducibility lock фиксирует версии и профили, от которых зависит воспроизводимая сборка проекта Electron2D. Он нужен, чтобы локальная машина разработчика, CI и AI-агент видели одинаковый baseline окружения до запуска build/run/export jobs.

`e2d doctor` проверяет этот baseline и локальное окружение. Команда является read-only диагностикой: она не создаёт, не меняет и не удаляет project files, не подключается к активной Editor-сессии как владелец workspace, не снимает session lock и не читает signing secrets.

## Files

Новый проект должен содержать:

- `global.json`;
- `electron2d.lock.json`.

`global.json` фиксирует .NET SDK через `sdk.version` и `sdk.rollForward`. Для `0.1.0 Preview` template использует SDK `10.0.101` и `rollForward: latestFeature`.

`electron2d.lock.json` должен быть UTF-8 JSON с LF line endings и стабильным порядком top-level fields:

1. `$schema`;
2. `format`;
3. `schemaVersion`;
4. `engine`;
5. `dotnet`;
6. `nuget`;
7. `nativeRuntime`;
8. `assetImporters`;
9. `project`;
10. `exportTemplates`;
11. `signing`.

## Lock schema

`electron2d.lock.json` использует:

- `$schema`: `https://electron2d.dev/schemas/project-system/electron2d-lock.schema.json`;
- `format`: `Electron2D.ReproducibilityLock`;
- `schemaVersion`: `1`.

Минимальные обязательные данные:

- `engine.version` — версия Electron2D;
- `dotnet.sdkVersion`, `dotnet.rollForward`, `dotnet.targetFramework`;
- `nuget.packages[]` с `id` и `version`;
- `nativeRuntime.packages[]` с `id` и `version`;
- `assetImporters` с версиями importer metadata для texture, font, audio и shader;
- `project.rendererProfile`, `project.physicsBackendVersion`, `project.serializationSchemaVersion`;
- `exportTemplates.version`;
- `signing.mode = referencesOnly`.

Lock file не должен хранить пароли, tokens, private keys, certificates, keystore payloads или абсолютные machine-local paths. Signing configuration хранит только политику ссылок на внешние секреты, а сами secret values остаются вне project files.

## Verifier

`ProjectReproducibilityLockVerifier` должен:

- проверять наличие `global.json` и `electron2d.lock.json`;
- проверять JSON shape lock-файла и обязательные поля;
- проверять соответствие `global.json` и lock `.NET` metadata;
- проверять, что project package reference `Electron2D` совпадает с `engine.version`;
- возвращать structured diagnostics с code `E2D-DOCTOR-0001` при missing, malformed или inconsistent reproducibility files;
- не писать файлы и не читать secret values.

## `e2d doctor`

Команда:

```powershell
e2d doctor --project <path> --format json
```

Должна возвращать общий CLI JSON envelope:

- `command = "doctor"`;
- `route = "none"`;
- `changedFiles = []`;
- `dirtyDocuments = []`;
- `data.mode = "doctor.environment"`;
- `data.summary.status`;
- `data.checks[]`.

`route = "none"` обязателен, потому что doctor не должен открывать `ProjectWorkspace`, подключаться к active Editor или становиться вторым владельцем проекта.

`data.checks[]` должен содержать проверки:

- `dotnetSdk`;
- `electron2d`;
- `nativeRuntime`;
- `androidSdk`;
- `androidNdk`;
- `xcode`;
- `exportTemplates`;
- `graphicsCapabilities`;
- `signing`.

Статусы check:

| Status | Значение |
| --- | --- |
| `ok` | Требование выполнено или конфигурация валидна. |
| `warning` | Проверка завершилась, но есть ограничение, не блокирующее текущий desktop baseline. |
| `missing` | Локальный toolchain или optional SDK не найден. |
| `blocked` | Конфигурация противоречива или небезопасна для продолжения. |

`succeeded` означает, что diagnostic report был создан безопасно. Отсутствующий Android SDK, Android NDK или Xcode может давать `missing` checks без ненулевого exit code, потому что mobile/export readiness подтверждается отдельными platform gates. Missing/malformed lock file является blocked-состоянием и должен возвращать diagnostic.

## Signing safety

Doctor может читать только non-secret signing references из `export_presets.e2export.json` и lock policy. Он не должен:

- читать environment variable values, certificates, keystore files или keychain entries;
- печатать raw credential values;
- раскрывать private paths к secret files;
- запускать signing, export, deploy или publish commands.

Output может сообщать количество signing presets, required/non-required состояние и форму ссылки вроде `env:NAME`, но не значение `NAME`.

## Acceptance criteria

- Template `electron2d-empty` содержит `global.json` и `electron2d.lock.json`.
- Опубликована schema `schemas/project-system/electron2d-lock.schema.json`, и focused tests проверяют её обязательные поля.
- `ProjectReproducibilityLockVerifier` проходит на template lock file и возвращает diagnostic на missing/malformed lock.
- `e2d doctor --format json` возвращает checks для .NET SDK, Electron2D, native runtime, Android SDK/NDK, Xcode, export templates, graphics capabilities и signing.
- `e2d doctor` не меняет project files, не открывает active Editor route и возвращает пустые `changedFiles`/`dirtyDocuments`.
- Tests подтверждают, что output не содержит secret value из environment variables, даже если signing reference указывает на такую variable.
- Implementation documentation описывает текущую read-only диагностику, проверочные команды и ограничения.
