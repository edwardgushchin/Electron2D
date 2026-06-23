# Export preset model and toolchain validation

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0087`.
Обновлено: 2026-06-23.

## Назначение

Electron2D `0.1.0 Preview` должен иметь общий export preset model для платформенных export-задач. Generic preset layer сам не собирает пакеты: он фиксирует переносимый preset format и fail-closed validation. Фактические package builders подключаются отдельными platform-specific командами, включая WebAssembly browser `plan-web`, `build-web` и `run-web`.

## Preset document

Export presets сохраняются в UTF-8 JSON без BOM и с LF line endings. Базовое имя файла - `export_presets.e2export.json`.

Root object содержит:

- `format`: `Electron2D.ExportPresets`;
- `formatVersion`: `1`;
- `presets`: массив preset objects.

Каждый preset содержит:

- `name`: уникальное имя preset внутри файла;
- `target`: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` или `WebAssemblyBrowser`;
- `configuration`: `Debug` или `Release`;
- `runtimeIdentifier`: runtime identifier, который будет передан будущему package builder;
- `selfContained`: boolean;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `outputDirectory`: путь output directory относительно project root или абсолютный путь;
- `includeDebugSymbols`: boolean;
- `signing`: signing references без секретов.

`signing` содержит:

- `required`: boolean;
- `identity`: имя signing identity, profile или certificate alias;
- `credentialReference`: ссылка на секрет во внешнем хранилище, но не сам секрет.

Файл не должен хранить passwords, tokens, private keys или содержимое certificates.

## Validation

Toolchain validation принимает preset и описание окружения. Окружение задаёт только факты, обнаруженные будущим editor/CLI/CI layer:

- доступен ли .NET SDK;
- путь к Android SDK;
- путь к Android NDK;
- путь к Xcode;
- доступны ли .NET WebAssembly build tools;
- доступна ли signing identity;
- доступна ли signing credential reference.

Validator не запускает сборку, signing, deploy, публикацию и не читает секретные значения.

## Fail-closed diagnostics

Validation result содержит:

- `Succeeded`;
- список diagnostics с `Code`, `Message`, `Severity` и `PresetName`.

Если обязательный SDK/toolchain/signing reference отсутствует, result должен быть failure. Минимальные стабильные коды:

- `E2D-EXPORT-DOTNET-0001` - .NET SDK недоступен;
- `E2D-EXPORT-ANDROID-0001` - Android SDK недоступен;
- `E2D-EXPORT-ANDROID-0002` - Android NDK недоступен;
- `E2D-EXPORT-IOS-0013` - Xcode недоступен;
- `E2D-EXPORT-WEB-0001` - .NET WebAssembly build tools недоступны;
- `E2D-EXPORT-SIGNING-0001` - signing identity недоступна;
- `E2D-EXPORT-SIGNING-0002` - signing credential reference недоступна.

Ошибки должны быть понятны человеку и AI-агенту: сообщение объясняет, что отсутствует и для какого preset.

## Критерии приёмки

- Integration tests подтверждают deterministic round-trip нескольких presets.
- Integration tests подтверждают, что duplicate preset names fail closed при загрузке.
- Integration tests подтверждают, что desktop preset с доступным .NET SDK проходит validation.
- Integration tests подтверждают, что Android Release preset без SDK/NDK/signing references возвращает diagnostics и не выполняет side effects.
- Integration tests подтверждают, что WebAssembly browser preset без WebAssembly build tools возвращает diagnostics и не выполняет side effects.
- Implementation documentation описывает фактический JSON format, validation input и команды проверки.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний export preset layer. Внутренний layer означает код движка и будущих инструментов, доступный тестам и tooling через assembly internals, но не являющийся public runtime API для игр.

## Файл presets

Export presets сохраняются в `export_presets.e2export.json`. Это UTF-8 JSON-файл с переносами строк LF.

Root object содержит:

- `format`: `Electron2D.ExportPresets`;
- `formatVersion`: `1`;
- `presets`: массив preset objects.

При сохранении presets сортируются по `name` в ordinal-порядке. Это делает файл стабильным для повторного сохранения, code review и будущих автоматических правок.

## Preset object

Каждый preset содержит:

- `name`: уникальное имя preset внутри файла;
- `target`: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` или `WebAssemblyBrowser`;
- `configuration`: `Debug` или `Release`;
- `runtimeIdentifier`: runtime identifier для будущего package builder;
- `selfContained`: будет ли будущая сборка включать runtime;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `outputDirectory`: путь выходной папки;
- `includeDebugSymbols`: включает ли будущая сборка debug symbols;
- `signing`: ссылки на signing-конфигурацию без секретов.

`signing` содержит:

- `required`: требуется ли signing;
- `identity`: имя signing identity, profile или certificate alias;
- `credentialReference`: ссылка на секрет во внешнем хранилище.

Файл не должен хранить passwords, tokens, private keys, certificates или содержимое keystore. `credentialReference` должен быть только ссылкой, например на environment variable или секрет CI.

## Load/save behavior

`ExportPresetStore.Save(...)`:

- проверяет документ перед записью;
- создаёт родительскую папку, если она нужна;
- записывает stable JSON;
- не выполняет сборку, signing, deploy или публикацию.

`ExportPresetStore.Load(...)`:

- возвращает `Electron2DExportPresetLoadResult`;
- при успехе содержит проверенный `Document` и пустой список diagnostics;
- при ошибке возвращает `Document == null` и diagnostic;
- не меняет runtime state.

Duplicate `name` fail closed при загрузке с кодом `E2D-EXPORT-PRESET-0001`.

## Toolchain validation

`Electron2DExportToolchainValidator.Validate(...)` принимает preset и описание окружения. Описание окружения передаёт уже обнаруженные факты:

- доступен ли .NET SDK;
- путь к Android SDK;
- путь к Android NDK;
- путь к Xcode;
- доступны ли .NET WebAssembly build tools;
- доступна ли signing identity;
- доступна ли signing credential reference.

Validator только проверяет эти факты и возвращает diagnostics. Он не запускает build, signing, deploy, публикацию, не читает секреты и не раскрывает `credentialReference` в сообщениях ошибок.

## Diagnostics

Текущие stable codes:

- `E2D-EXPORT-PRESET-0001` - duplicate preset name;
- `E2D-EXPORT-PRESET-0002` - invalid preset value;
- `E2D-EXPORT-PRESET-JSON-0001` - malformed JSON;
- `E2D-EXPORT-PRESET-IO-0001` - I/O или access error;
- `E2D-EXPORT-DOTNET-0001` - .NET SDK недоступен;
- `E2D-EXPORT-ANDROID-0001` - Android SDK недоступен;
- `E2D-EXPORT-ANDROID-0002` - Android NDK недоступен;
- `E2D-EXPORT-IOS-0013` - Xcode недоступен;
- `E2D-EXPORT-WEB-0001` - .NET WebAssembly build tools недоступны;
- `E2D-EXPORT-SIGNING-0001` - signing identity недоступна;
- `E2D-EXPORT-SIGNING-0002` - signing credential reference недоступна.

## Ограничения

- Public export API не добавлен.
- Windows, Linux и macOS package planners с локальными verifiers добавлены отдельными задачами.
- Android/iOS export commands остаются отдельными задачами.
- WebAssembly browser package generation and local smoke artifact are implemented by the web-specific CLI commands, not by the generic preset load/save layer.
- Signing, deploy, публикация и GitHub Release не выполняются этой реализацией.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ExportPresetTests" --no-restore -m:1
```
