# Пользовательская документация экспорта

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0098`.
Обновлено: 2026-06-23.

## Назначение

Пользовательская export-документация Electron2D `0.1.0 Preview` должна дать проверяемый путь для текущих export targets, описать WebAssembly browser package/smoke проверку, честно обозначить mobile/web execution gaps и не смешивать `runtimeTargets`, `editorTargets` и `releaseVerificationTargets`. Документация не должна выдавать Android/iOS export или remote WebAssembly hosting как готовый release path до закрытия соответствующих platform tasks и реальных smoke checks.

## Обязательные страницы

Документация должна включать:

- общий export guide в `docs/export/export-guide.md`;
- platform page для `WindowsX64`;
- platform page для `LinuxX64`;
- platform page для `MacOSArm64`;
- status page для `AndroidArm64`;
- status page для `IosArm64`.
- status page для `WebAssemblyBrowser`.

## Содержание

Общий export guide должен описывать:

- текущую runtime/export target matrix;
- отличие `runtimeTargets` от desktop-only `editorTargets` и отдельного release verification tier;
- различие между проверенным desktop export и заблокированным mobile export;
- различие между WebAssembly browser package/smoke проверкой, внешним `dotnet publish` и будущей browser automation, то есть автоматической проверкой в браузере;
- формат `export_presets.e2export.json`;
- SDK/toolchain requirements;
- signing и credential references без secret values;
- известные ограничения;
- команды проверки.

Каждая platform page должна описывать:

- target name и runtime identifier;
- host requirements;
- SDK/toolchain prerequisites;
- signing или отсутствие signing в текущем verifier;
- ограничения;
- локальную или CI-проверку.

Mobile status pages должны описывать SDK, signing, credentials и известные ограничения как требования будущей реализации, но не должны содержать команду, которая выглядит как готовый mobile release export.

WebAssembly status page должна описывать `browser-wasm`, static package layout, CLI commands `plan-web`, `build-web`, `run-web`, browser policies, smoke criteria, WebAssembly build tools, local smoke artifact и ограничения remote hosting/browser automation.

## Безопасность секретов

Документация не должна содержать реальные пароли, токены, приватные ключи, сертификаты, содержимое хранилищ ключей, профили подготовки приложений или секреты подписи. Разрешены только значения-заглушки и непрозрачные ссылки, например имя переменной окружения или имя секрета CI.

## Проверяемость

`eng\Electron2D.Build` проверяет релизные файлы настольных платформ без PowerShell как активного пути сборки. Проверка export-документации выполняется отдельными командами локальной документации, а релизные архивы настольных платформ создаются C#-командой:

- `dotnet run --project eng\Electron2D.Build -- package --rid win-x64`;
- `dotnet run --project eng\Electron2D.Build -- package --rid linux-x64`;
- `dotnet run --project eng\Electron2D.Build -- package --rid osx-arm64`;
- `dotnet run --project eng\Electron2D.Build -- release verify`;
- `dotnet run --project eng\Electron2D.Build -- update docs --check`;
- `dotnet run --project eng\Electron2D.Build -- verify docs`.

## Критерии приёмки

- Export guide и platform pages существуют.
- Страницы настольных платформ описывают SDK, набор инструментов, правила подписи и учётных данных, ограничения и C#-команды сборки и проверки релиза.
- Android/iOS pages описывают SDK/signing/status requirements и явно фиксируют, что mobile export не является готовым release path.
- WebAssembly page описывает `WebAssemblyBrowser`, `browser-wasm`, package layout, CLI plan/build/run commands, browser policies, smoke artifact и ограничения.
- User guide ссылается на export guide.
- `dotnet run --project eng\Electron2D.Build -- release verify` проходит после сборки релизных файлов настольных платформ.

## Фактическое состояние, ограничения и проверки

<!-- export-doc:overview -->
## Обзор

Electron2D `0.1.0 Preview` имеет локально проверенные механизмы экспорта для desktop, проверки структуры WebAssembly browser package и Android arm64 debug APK с запуском на эмуляторе. Эта страница является пользовательской входной точкой в export-документацию: она объясняет, какие targets сейчас имеют проверяемые механизмы, какие toolchains нужны, как представлены signing references и какие команды подтверждают текущее состояние репозитория. Это не означает, что финальная релизная проверка уже пройдена: полный проход остаётся за `T-0093` и `T-0104`.

Export layer намеренно работает по правилу fail closed: отсутствующий SDK, неподдерживаемый runtime identifier, отсутствующая signing identity или отсутствующая project setting должны дать диагностику, а не частичный package.

<!-- export-doc:target-matrix -->
## Матрица runtime/export targets

`runtimeTargets` - это платформы, для которых существуют export presets и где экспортированная игра должна работать. `editorTargets` для `0.1.0 Preview` ограничены Windows, Linux и macOS. `releaseVerificationTargets` задаются отдельно в [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md): для текущего релиза они включают все runtime targets, но blocked-environment artifact не считается успешным release gate.

| Target | Runtime identifier | Статус | Проверка |
| --- | --- | --- | --- |
| `WindowsX64` | `win-x64` | Локально проверен механизм настольного экспорта и релизный архив | `dotnet run --project eng\Electron2D.Build -- package --rid win-x64` |
| `LinuxX64` | `linux-x64` | Локально проверен механизм настольного экспорта и релизный архив | `dotnet run --project eng\Electron2D.Build -- package --rid linux-x64` |
| `MacOSArm64` | `osx-arm64` | Локально проверен механизм настольного экспорта и релизный архив | `dotnet run --project eng\Electron2D.Build -- package --rid osx-arm64` |
| `AndroidArm64` | `android-arm64` | Проверены debug APK и запуск на эмуляторе; финальная релизная проверка не закрыта | [Android arm64 export](android-arm64-export.md) |
| `IosArm64` | `ios-arm64` | Planner и staging есть; проверка на симуляторе или устройстве заблокирована окружением | [iOS arm64 export](ios-arm64-export.md) |
| `WebAssemblyBrowser` | `browser-wasm` | Проверены структура пакета и локальный smoke artifact; финальная браузерная проверка не закрыта | [WebAssembly browser export](webassembly-browser-export.md) |

Desktop export означает, что репозиторий может создать, опубликовать и запустить package пустого проекта на подходящем host. WebAssembly browser export умеет планировать static package layout, создавать host/loader/manifest files, копировать project resources и писать structured smoke artifact. Android сейчас умеет планировать и staging-ить debug APK и release AAB workflows, собирать debug APK при доступном локальном Android toolchain, устанавливать и запускать его через `adb`, а также создавать structured smoke artifact. iOS умеет планировать и staging-ить transient Xcode project и писать blocked smoke artifact, но остаётся заблокированным release path до успешной проверки на macOS/Xcode с симулятором или устройством.

<!-- export-doc:preset-file -->
## Файл export presets

Export presets хранятся в `export_presets.e2export.json`. Текущая модель описана в [Export preset model](export-preset-model.md).

Каждый preset задаёт target, configuration, runtime identifier, output directory, renderer profile, debug symbol policy и signing references. Файл не должен содержать secret values. Вместо passwords, private keys, keystore contents, certificates, provisioning profile contents или tokens используйте ссылки: имя CI secret, имя переменной окружения, certificate alias или signing identity label.

<!-- export-doc:desktop-verification -->
## Проверка desktop export

Запустите C#-команду сборки для нужной настольной платформы:

```bash
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- package --rid linux-x64
dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64
dotnet run --project eng/Electron2D.Build -- release verify
```

Каждая команда `package --rid` строит пакет библиотеки среды выполнения, `Electron2D.Editor`, инструмент разработчика `e2d`, манифест релиза, архив и SHA-256 для выбранного `runtime identifier`. `release verify` проверяет полный настольный набор `win-x64`, `linux-x64` и `osx-arm64`, имена архивов, контрольные суммы, манифест и отсутствие запрещённых рабочих файлов внутри релизных файлов. Команды не публикуют GitHub Release, не создают тег и не читают секреты подписи.

<!-- export-doc:mobile-status -->
## Состояние mobile export

`AndroidArm64` имеет planner, staging, debug APK build и device/emulator smoke commands:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-android --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-android --project <project-root> --output exports/android/debug --smoke-output .electron2d/export-smoke/android-smoke.json --adb-path <path-to-adb> --adb-serial <serial> --format json
```

`run-android` намеренно fail closed до появления подключённого авторизованного device или emulator. Используйте `--adb-serial`, когда подключено больше одного Android target. Для `x86_64` emulators команда собирает временный `android-x64` smoke package; это не заменяет production preset `android-arm64`.

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-ios --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json
```

`IosArm64` сейчас имеет CLI planner, transient Xcode project staging builder и writer для blocked smoke artifact. Из текущего состояния репозитория он не запускает `xcodebuild`, signing, install, launch или simulator/device smoke. Mobile export остаётся release gate, пока каждая platform task не даст device/simulator smoke checks, reference-game evidence и CI/reporting evidence.

<!-- export-doc:web-status -->
## Состояние WebAssembly browser

`WebAssemblyBrowser` с runtime identifier `browser-wasm` имеет planner, package и smoke commands:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-web --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

`plan-web` возвращает JSON plan для `wwwroot`, `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, `_framework`, `assets`, `project.e2d.json`, main scene path, browser policies и smoke criteria. `build-web --skip-publish true` создаёт static files без внешнего publish; обычный `build-web` сначала проверяет подходящие WebAssembly build tools, а затем запускает `dotnet publish`. `run-web` проверяет созданный package и пишет structured smoke artifact с launch instructions. Эти команды не ставят workspace jobs в очередь, не выполняют deploy на hosting и не публикуют remote artifacts.

<!-- export-doc:signing-credentials -->
## Signing и credentials

Signing data делится на две категории:

- публичные или non-secret labels: signing identity name, certificate alias, profile name, bundle identifier, package id;
- secret-bearing material: passwords, tokens, private keys, keystore contents, certificate contents, provisioning profile contents.

Файлы репозитория могут содержать только первую категорию. Secret-bearing material должен оставаться вне репозитория и ссылаться через `credentialReference` или будущую editor/CI secret configuration.

C#-команды сборки и проверки релиза не читают реальные секреты и не публикуют файлы.

<!-- export-doc:known-limitations -->
## Известные ограничения

- Desktop export проверяется на empty project template, а не на законченных reference games.
- Windows x64, Linux x64 glibc и macOS arm64 - единственные проверенные desktop targets.
- Linux musl, Linux ARM, Windows ARM, macOS x64, Android release signing с реальными credentials и iOS simulator/device smoke находятся вне текущего проверенного native export path.
- Automated browser launch, remote hosting deploy, PWA installation и service worker caching находятся вне текущей WebAssembly проверки.
- Android lifecycle, orientation, safe area, touch, package staging, debug APK build и emulator deployment/runtime smoke имеют локальную проверку в репозитории, но release signing publish и долгий reference-game soak ещё не являются завершёнными release paths.
- GitHub Release publication намеренно не входит в эти verifier commands и требует отдельной команды пользователя.

## Проверка документации

После изменения export-документации запустите:

```bash
dotnet run --project eng/Electron2D.Build -- update docs --check
dotnet run --project eng/Electron2D.Build -- verify docs
```

После изменения сборки настольного экспорта дополнительно запустите `dotnet run --project eng/Electron2D.Build -- release verify`.
