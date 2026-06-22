# Reproducibility lock и `e2d doctor`

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
