# Android arm64 export

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0091`.
Обновлено: 2026-06-23.

## Назначение

Electron2D `0.1-preview` должен собирать один проект в Android runtime package без переписывания игровой логики. Android является платформой запуска и экспорта, но не платформой редактирования проекта.

Android export в этой спецификации означает:

- `AndroidArm64` export target;
- runtime identifier `android-arm64`;
- обязательный ABI `arm64-v8a`;
- debug APK для быстрой установки на устройство или emulator;
- release AAB для production-подготовки;
- signing только через пользовательские ссылки на keystore и внешние секреты, без сохранения паролей или содержимого keystore в репозитории;
- mobile runtime policy для touch, lifecycle, orientation, safe area, audio, resources, filesystem и renderer fallback.

`x86_64` разрешён только как дополнительный emulator ABI. Он не заменяет обязательный `arm64-v8a`.
Если `run-android` выбран на emulator с ABI `x86_64`, команда может собрать временный smoke package с runtime identifier `android-x64`; это не меняет production preset `AndroidArm64` и не считается заменой arm64 APK/AAB.

## Export target

Preset для Android export использует:

- `target`: `AndroidArm64`;
- `runtimeIdentifier`: `android-arm64`;
- `configuration`: `Debug` или `Release`;
- `selfContained`: `true`;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `outputDirectory`: корневая папка Android export artifacts;
- `includeDebugSymbols`: разрешён для debug workflow;
- `signing.required`: `false` для debug APK и `true` для release AAB.

Если `Release` preset не требует signing, planner должен fail closed. Если `Debug` preset требует signing, planner может сохранить signing plan, но не должен требовать secret material для debug build.

## Toolchain

Минимальный Android toolchain:

- .NET SDK с Android workload;
- Android SDK;
- Android NDK, совместимый с workload;
- JDK 17 или новее;
- Android build-tools и platform package;
- `adb` для install/run/smoke;
- connected Android arm64 device или emulator для smoke.

Validator не запускает build, install, signing, deploy или публикацию. Он получает обнаруженные факты окружения и возвращает deterministic diagnostics. Package builder и CLI-команды должны fail closed, если нужный toolchain или device отсутствует.

Stable diagnostics:

- `E2D-EXPORT-ANDROID-0001` - Android SDK недоступен;
- `E2D-EXPORT-ANDROID-0002` - Android NDK недоступен;
- `E2D-EXPORT-ANDROID-0003` - Android preset использует target, отличный от `AndroidArm64`;
- `E2D-EXPORT-ANDROID-0004` - Android preset использует runtime identifier, отличный от `android-arm64`;
- `E2D-EXPORT-ANDROID-0005` - Android preset не self-contained;
- `E2D-EXPORT-ANDROID-0006` - Android release preset требует signing references;
- `E2D-EXPORT-ANDROID-0007` - project file path отсутствует;
- `E2D-EXPORT-ANDROID-0008` - project settings отсутствуют;
- `E2D-EXPORT-ANDROID-0009` - project settings некорректны;
- `E2D-EXPORT-ANDROID-0010` - внешний Android build/publish не запустился или завершился с ошибкой;
- `E2D-EXPORT-ANDROID-0011` - Android package staging не удалось записать;
- `E2D-EXPORT-ANDROID-0012` - обязательный package файл отсутствует или не читается;
- `E2D-EXPORT-ANDROID-0013` - путь ресурса выходит за пределы project root;
- `E2D-EXPORT-ANDROID-0014` - connected Android device или emulator недоступен;
- `E2D-EXPORT-ANDROID-0015` - device smoke criterion не прошёл;
- `E2D-EXPORT-ANDROID-0016` - JDK 17+ недоступен.

Сообщения diagnostics должны объяснять, какой preset или smoke заблокирован и почему. Они не должны раскрывать пароли, токены, credential references, keystore contents или локальные secret paths.

## Package layout

Planner должен возвращать deterministic Android staging layout:

```text
<outputDirectory>/
  android/
    Electron2D.Android.csproj
    MainActivity.cs
    AndroidManifest.xml
    Assets/
      electron2d/
        project.e2d.json
        <main scene path>
        assets/
    Resources/
      values/
        electron2d_export.xml
  artifacts/
    debug/
    release/
  smoke/
```

`Electron2D.Android.csproj` - transient .NET Android project для packaging. Он не становится canonical game project и не записывается в исходный project root.

`MainActivity.cs` - Android host, который фиксирует lifecycle hooks, immersive mode, orientation, safe area snapshot, touch path и smoke marker. Реальная игровая логика продолжает жить в исходном C# project; package staging содержит runtime metadata и ресурсы, нужные для запуска.

`AndroidManifest.xml` - manifest с package id, label, orientation и activity settings.

`Assets/electron2d/**` содержит project settings, main scene и игровые resources. Служебные файлы редактора, включая `.taskboard/**`, local-only task tracker, дневник и completed archives, не входят в APK/AAB staging.

Сгенерированный Android host должен использовать брендинг движка как проверяемую часть smoke-сцены:

- launcher icon берётся из ассетов Electron2D и указывается в manifest как `android:icon`/`android:roundIcon`;
- application label берётся из имени проекта, поэтому тестовый проект с именем `Electron2D` устанавливается как приложение `Electron2D`, а не как имя шаблона или IDE;
- первая сцена smoke рисует контрастный логотип Electron2D на чистом черном фоне;
- для черного фона используется светлая визуальная версия логотипа из файла `electron2d_logo_dark.png`, потому что dark variant в текущем brand pack означает вариант для темного фона;
- game activity занимает весь экран, скрывает status/navigation bars, задаёт им черный цвет и не оставляет видимых системных бордеров вокруг game view.
- manifest использует современный `targetSdkVersion` вместе с `android:resizeableActivity="true"` и высоким `android:maxAspectRatio`, чтобы Android не помещал game view в compatibility letterbox на широких телефонах.
- smoke activity удерживает экран включенным на время проверки через стандартные window flags и современные activity methods, потому что real-device smoke не должен проваливаться только из-за короткого screen timeout.

## Build workflow

`e2d export plan-android` должен формировать workflow без выполнения внешних процессов.

Debug APK workflow:

```text
dotnet build <android csproj> --configuration Debug --framework net10.0-android -p:RuntimeIdentifier=android-arm64 -p:RuntimeIdentifiers=android-arm64 -p:AndroidPackageFormat=apk
```

Release AAB workflow:

```text
dotnet publish <android csproj> --configuration Release --framework net10.0-android -p:RuntimeIdentifier=android-arm64 -p:RuntimeIdentifiers=android-arm64 -p:AndroidPackageFormat=aab
```

Если release signing включён, command plan содержит только non-secret signing labels and references. Реальные secret values могут поступать только из внешнего окружения в момент сборки; они не пишутся в source, package manifest, diagnostics или smoke artifact.

`e2d export build-android` должен создать staging layout и затем, если `--skip-publish true` не указан, запустить соответствующую Android build/publish command. Режим `--skip-publish true` не объявляет APK/AAB собранными, но создаёт проверяемый staging layout для planner/layout/diagnostics tests.

## Runtime policies

Android runtime должен явно фиксировать:

- touch events проходят через существующие `InputEventScreenTouch` и `InputEventScreenDrag`;
- generated Android host фиксирует touch на уровне `DispatchTouchEvent` activity и основного view, чтобы smoke видел событие независимо от того, какой слой получил первый callback;
- lifecycle `pause`, `resume`, `stop` и `destroy` мапится в runtime pause/resume/shutdown state;
- requested orientation берётся из project display settings;
- safe area snapshot доступен через `DisplayServer`;
- immersive fullscreen mode включается для game activity до установки основного view, а status/navigation bars остаются черными, если платформа временно показывает системные области;
- первая frame smoke-сцены показывает контрастный логотип Electron2D на черном полноэкранном фоне;
- audio starts unlocked only after platform focus is ready; smoke проверяет play/pause route, а не secret device logs;
- save data использует app sandbox storage;
- resource loading идёт из package assets;
- renderer profile `Automatic` выбирает mobile graphics profile и может перейти в `Compatibility` fallback с structured reason.

## Device smoke

`e2d export run-android` должен установить debug APK на connected device/emulator, запустить activity, собрать smoke artifact и вернуть failure, если device/emulator отсутствует.

Если к host одновременно подключены несколько Android-устройств или emulator, команда должна поддерживать явный выбор через `--adb-serial <serial>`. При заданном serial все `adb`-операции smoke используют только этот serial. Если выбранный serial отсутствует, находится не в состоянии `device` или не авторизован, команда сохраняет blocked artifact с diagnostic `E2D-EXPORT-ANDROID-0014` и не переключается на другое доступное устройство.

Smoke-driver должен перед проверкой logcat отправить deterministic tap в центр текущего экрана и центр landscape-координат, затем может использовать `monkey` как дополнительный fallback. Если физическая прошивка запрещает shell-инъекцию touch-событий даже при разблокированном экране, такой real-device run фиксируется как failed по `input`, а полный automated smoke должен выполняться на устройстве или emulator, где input injection разрешён.

Минимальный smoke artifact проверяет:

1. install;
2. launch;
3. first frame / rendering readiness;
4. touch input path;
5. pause/resume через background/foreground cycle;
6. orientation;
7. safe area;
8. audio;
9. resource loading;
10. filesystem save data;
11. renderer fallback policy;
12. engine logo on black fullscreen scene;
13. clean shutdown.

Если automated device smoke невозможен, команда обязана сохранить blocked artifact с diagnostics и не объявлять задачу успешной.

## Критерии приёмки

- Export preset model round-trip поддерживает `AndroidArm64`.
- Toolchain validator fail closed, если Android SDK, NDK, JDK, signing identity или signing credential reference отсутствуют для соответствующего preset.
- Android planner создаёт deterministic debug APK и release AAB build plans, ABI policy, orientation/safe-area policy, mobile graphics profile, fallback policy и smoke criteria.
- Android planner fail closed для неправильного target, runtime identifier, deployment mode, release signing gaps и отсутствующих project settings.
- Android package builder создаёт Android staging project, manifest, activity host, export metadata, project settings, main scene и `assets/**`, но не копирует `.taskboard/**`.
- Android package builder добавляет engine launcher icon, application label из project settings и контрастный логотип `electron2d_logo_dark.png` для полноэкранной black smoke scene без видимых системных бордеров.
- CLI `export build-android --skip-publish true` создаёт проверяемый staging layout без workspace job, а обычный `export build-android` fail closed, если Android toolchain не соответствует текущему SDK.
- CLI `export run-android` создаёт structured device smoke artifact; при отсутствии device/emulator он возвращает blocked/failure с `E2D-EXPORT-ANDROID-0014` и не объявляет smoke успешным.
- CLI `export run-android --adb-serial <serial>` запускает install/run/smoke только на выбранном Android serial и fail closed, если этот serial недоступен.
- На connected Android arm64 device или emulator выполнен real-device smoke для pause/resume, render, input, audio, resources, filesystem и полноэкранной black scene с логотипом Electron2D.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.

## Фактическое состояние, ограничения и проверки

## Статус

`AndroidArm64` с runtime identifier `android-arm64` имеет рабочий export baseline для Electron2D `0.1-preview`:

- planner для debug APK и release AAB workflow;
- transient Android staging project в выбранной папке export output;
- `build-android --skip-publish true` для deterministic staging checks;
- обычный `build-android`, который собирает debug APK при наличии Android SDK, NDK и JDK 17+;
- release AAB signing plan с не-секретными signing references;
- `run-android`, который устанавливает debug APK на выбранное Android-устройство или emulator, запускает activity и пишет structured smoke artifact.

Основной production preset остаётся `AndroidArm64`/`android-arm64`/`arm64-v8a`. Если `run-android` выбран на emulator с ABI `x86_64`, команда собирает временный smoke package с runtime identifier `android-x64`; это нужно только для AVD-проверки и не заменяет arm64 APK/AAB.

## Команды

Сформировать план debug APK export:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-android --project <project-root> --format json
```

Создать staging files без запуска Android build:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --skip-publish true --format json
```

Собрать debug APK, если установлены Android SDK, NDK и JDK 17+:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --format json
```

Сформировать план или staging для release AAB:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --configuration Release --output exports/android/release --signing-identity <alias> --signing-credential-reference env:E2D_ANDROID_KEYSTORE --skip-publish true --format json
```

Запустить device или emulator smoke:

```bash
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-android --project <project-root> --output exports/android/debug --smoke-output .electron2d/export-smoke/android-smoke.json --adb-path <path-to-adb> --adb-serial <serial> --format json
```

`--adb-path` необязателен, если `adb` находится через Android SDK или `PATH`. `--adb-serial` рекомендуется всегда, когда к host подключено больше одного Android-устройства или emulator. Если выбранный serial отсутствует, не авторизован или не находится в состоянии `device`, команда пишет blocked artifact с diagnostic `E2D-EXPORT-ANDROID-0014` и не переключается на другое устройство.

## Staging layout

`build-android` записывает transient Android project:

```text
<outputDirectory>/
  android/
    Electron2D.Android.csproj
    MainActivity.cs
    AndroidManifest.xml
    Assets/
      electron2d/
        project.e2d.json
        <main scene path>
        assets/
        branding/
          electron2d_logo_dark.png
    Resources/
      drawable/
        electron2d_icon.png
      values/
        electron2d_export.xml
  artifacts/
    debug/
    release/
  smoke/
```

Staging project является generated output. Он не становится canonical game project и не записывается обратно в исходный project root.

`Assets/electron2d/**` включает project settings, main scene, пользовательские game assets и smoke-логотип. Editor metadata вроде `.taskboard/**`, локальные task trackers, дневники и completed-task archives не копируются в Android package staging tree.

## Runtime policy

Generated Android host фиксирует mobile runtime contract, который проверяет device/emulator smoke:

- application label берётся из project settings; тестовый проект `Electron2D` устанавливается как приложение `Electron2D`;
- launcher icon берётся из engine branding asset `electron2d_icon.png`;
- black smoke scene рисует контрастный логотип `electron2d_logo_dark.png` на черном фоне;
- activity включает fullscreen/immersive mode, черные status/navigation bars, cutout mode `ShortEdges`, `targetSdkVersion=34`, `resizeableActivity=true` и `maxAspectRatio=3.0`;
- touch input фиксируется через `DispatchTouchEvent` activity и основной view;
- lifecycle markers пишутся для pause, resume, stop и destroy;
- requested screen orientation берётся из project display settings;
- safe-area readiness, packaged resources, app sandbox filesystem, audio route и renderer fallback фиксируются smoke markers.

## Device и emulator smoke

`run-android` выполняет такой порядок:

1. выбирает `adb` и Android serial;
2. если выбранный serial имеет ABI `x86_64`, создаёт временный `android-x64` smoke package;
3. собирает debug APK, если в output ещё нет APK;
4. устанавливает APK через `adb install -r -t`;
5. запускает activity через deterministic component name;
6. отправляет deterministic center tap и затем touch-only `monkey` fallback;
7. выполняет background/foreground cycle;
8. читает filtered logcat только по tag `Electron2D`;
9. force-stop приложения и пишет artifact.

Smoke artifact проверяет `install`, `launch`, `render`, `input`, `pauseResume`, `orientation`, `safeArea`, `audio`, `resources`, `filesystem`, `logoOnBlack`, `rendererFallback` и `shutdown`.

На некоторых физических прошивках shell input может быть запрещён даже при разблокированном экране. В этом случае real-device run честно падает по `input`; для полного automated smoke нужен emulator или устройство, где USB input injection разрешён настройками разработчика.

## SDK and toolchain

`build-android` и `run-android` обнаруживают:

- .NET SDK через существующую SDK probe;
- Android SDK из `ANDROID_HOME`, `ANDROID_SDK_ROOT` или стандартной пользовательской папки SDK;
- Android NDK из `ANDROID_NDK_HOME` или первой папки SDK `ndk/**`;
- JDK 17+ из `JAVA_HOME` или стандартных vendor directories under `ProgramFiles` / `ProgramFilesX86`;
- `adb` из `--adb-path`, Android SDK `platform-tools` или `PATH`.

Если `JAVA_HOME` указывает на более старый JDK, CLI пропускает его и использует первый найденный путь JDK 17+. Это защищает Android manifest tooling от падения из-за несовместимой версии Java class files.

## Signing and credentials

Release AAB planning требует:

- `--signing-identity <alias>`;
- `--signing-credential-reference <reference>`.

Plan хранит только не-секретные labels и references. Он не хранит keystore contents, passwords, private keys, certificates или tokens. Полный release publish с реальными signing credentials остаётся заблокированным до появления пользовательского keystore и механизма доставки secret values.

## Текущая проверка

Проверки, выполненные 2026-06-23:

- focused integration tests для Android planner, package builder, CLI `run-android`, `--adb-serial`, fullscreen/logo/icon/label и touch driver прошли;
- real Android phone `641d225b0510` собрал и установил arm64 APK, запустил `Electron2D`, показал black fullscreen scene с контрастным engine logo без боковых полей, подтвердил `install`, `launch`, `render`, `pauseResume`, `orientation`, `safeArea`, `audio`, `resources`, `filesystem`, `logoOnBlack`, `rendererFallback` и `shutdown`;
- тот же real phone запретил shell touch injection с `SecurityException: INJECT_EVENTS`, поэтому `input` на нём остался честным failed criterion;
- emulator `emulator-5554` прошёл полный automated `run-android` smoke со статусом `passed`, включая `input`;
- arm64 APK badging подтвердил `application-label='Electron2D'`, `application-icon='res/drawable/electron2d_icon.png'`, package id `dev.electron2d.electron2d` и `targetSdkVersion='34'`;
- screenshots физического телефона и emulator имеют размер `2400 x 1080`, черные edge samples `#000000` на левой, правой, верхней и нижней границе, контрастный логотип расположен по центру.

## Known limitations

- Release AAB publish с реальными signing credentials не выполняется без пользовательской доставки keystore/password.
- Физические устройства с запретом shell input injection требуют ручного включения соответствующей настройки разработчика или проверяются через emulator для automated input criterion.
- Emulator smoke для `x86_64` использует temporary `android-x64` package и не заменяет обязательный `android-arm64` production preset.
- Android reference-game coverage, long soak и release signing остаются отдельными release-gate проверками более высокого уровня; эти шаги пока are not a ready release path.
