# Reproducibility lock и `e2d doctor`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

Статус: реализованная минимальная диагностика воспроизводимости.
Задача: `T-0126`.
Обновлено: 2026-06-22.

## Назначение

Новый проект Electron2D фиксирует воспроизводимый baseline в двух файлах:

- `global.json` — закрепляет .NET SDK через `sdk.version` и `sdk.rollForward`;
- `electron2d.lock.json` — закрепляет версию Electron2D, целевой framework, package references, native runtime package metadata, версии import metadata, renderer profile, physics backend marker, schema version проекта, export template version и signing policy.

`e2d doctor` читает эти файлы и локальное окружение как read-only diagnostic report. Команда не открывает `ProjectWorkspace`, не подключается к active Editor, не создаёт dirty documents и не пишет project files.

## Lock file

Шаблон `data/templates/electron2d-empty/` содержит:

```text
global.json
electron2d.lock.json
```

`global.json` сейчас использует:

```json
{
  "sdk": {
    "version": "10.0.101",
    "rollForward": "latestFeature"
  }
}
```

`electron2d.lock.json` использует schema URI:

```text
https://electron2d.dev/schemas/project-system/electron2d-lock.schema.json
```

Файл имеет стабильный top-level порядок:

```text
$schema
format
schemaVersion
engine
dotnet
nuget
nativeRuntime
assetImporters
project
exportTemplates
signing
```

`signing.mode` равен `referencesOnly`. Lock file не хранит passwords, tokens, private keys, certificates, keystore payloads, credential values или machine-local absolute paths.

Опубликованная JSON Schema находится здесь:

```text
schemas/project-system/electron2d-lock.schema.json
```

## Verifier

`ProjectReproducibilityLockVerifier` — внутренний проверяющий механизм project-system layer. Он доступен тестам, CLI и будущему Editor/tooling коду, но не является public runtime API для игр.

Verifier проверяет:

- наличие `global.json` и `electron2d.lock.json`;
- JSON shape и обязательные поля lock-файла;
- стабильный top-level порядок lock-файла;
- совпадение `global.json` SDK metadata и `dotnet` metadata в lock-файле;
- совпадение `engine.version` с `PackageReference Include="Electron2D"` в `.csproj`;
- отсутствие секретных полей и локальных абсолютных путей в lock-файле.

При missing, malformed или inconsistent reproducibility files verifier возвращает structured diagnostic:

### `E2D-DOCTOR-0001`

`E2D-DOCTOR-0001` означает, что воспроизводимый baseline проекта отсутствует, повреждён или противоречит `.csproj`/`global.json`. Diagnostic содержит project-relative location, если проблема привязана к конкретному файлу.

## `e2d doctor`

Команда:

```powershell
e2d doctor --project <path> --format json
```

Возвращает общий CLI envelope:

- `command = "doctor"`;
- `route = "none"`;
- `changedFiles = []`;
- `dirtyDocuments = []`;
- `data.mode = "doctor.environment"`.

`route = "none"` означает, что команда не открывала workspace и не пыталась стать вторым владельцем проекта рядом с открытым Editor.

`data.checks[]` сейчас содержит checks:

- `dotnetSdk`;
- `electron2d`;
- `nativeRuntime`;
- `androidSdk`;
- `androidNdk`;
- `xcode`;
- `exportTemplates`;
- `graphicsCapabilities`;
- `signing`.

Statuses:

| Status | Meaning |
| --- | --- |
| `ok` | Проверка прошла или lock содержит нужную декларацию. |
| `warning` | Диагностика завершилась, но есть ограничение, не блокирующее desktop baseline. |
| `missing` | Optional platform toolchain не найден локально. |
| `blocked` | Project reproducibility files противоречивы или небезопасны. |

Отсутствующие Android SDK, Android NDK и Xcode возвращаются как `missing` и не делают сам diagnostic report failed. Missing или malformed lock file делает summary `blocked` и добавляет `E2D-DOCTOR-0001`.

## Signing safety

Doctor может читать только signing references из `export_presets.e2export.json`. Он не читает значения environment variables, certificate files, keystore files или keychain entries.

Если preset содержит `credentialReference = "env:NAME"`, output может показать строку `env:NAME`, но не значение переменной `NAME`. Любые non-env credential references выводятся как `external-reference`, чтобы не раскрывать private file paths.

## Проверка

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ReproducibilityLockDoctorTests
```

Она покрывает template files, schema required fields, verifier success/failure, `doctor` JSON shape, read-only поведение и redaction signing secret values.
