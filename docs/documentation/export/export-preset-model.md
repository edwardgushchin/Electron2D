# Export preset model и toolchain validation

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
- `target`: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64` или `IosArm64`;
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

`Electron2DExportPresetStore.Save(...)`:

- проверяет документ перед записью;
- создаёт родительскую папку, если она нужна;
- записывает stable JSON;
- не выполняет сборку, signing, deploy или публикацию.

`Electron2DExportPresetStore.Load(...)`:

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
- `E2D-EXPORT-IOS-0001` - Xcode недоступен;
- `E2D-EXPORT-SIGNING-0001` - signing identity недоступна;
- `E2D-EXPORT-SIGNING-0002` - signing credential reference недоступна.

## Ограничения

- Public export API не добавлен.
- Windows package planner и локальный verifier добавлены отдельной задачей.
- Linux/macOS/Android/iOS export commands остаются отдельными задачами.
- Signing, deploy, публикация и GitHub Release не выполняются этой реализацией.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ExportPresetTests" --no-restore -m:1
```
