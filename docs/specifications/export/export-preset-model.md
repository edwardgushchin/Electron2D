# Export preset model and toolchain validation

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
