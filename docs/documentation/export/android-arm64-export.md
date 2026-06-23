# Android arm64 export

## Статус

`AndroidArm64` с runtime identifier `android-arm64` имеет рабочий export baseline для Electron2D `0.1.0 Preview`:

- planner для debug APK и release AAB workflow;
- transient Android staging project в выбранной папке export output;
- `build-android --skip-publish true` для deterministic staging checks;
- обычный `build-android`, который собирает debug APK при наличии Android SDK, NDK и JDK 17+;
- release AAB signing plan с не-секретными signing references;
- `run-android`, который устанавливает debug APK на выбранное Android-устройство или emulator, запускает activity и пишет structured smoke artifact.

Основной production preset остаётся `AndroidArm64`/`android-arm64`/`arm64-v8a`. Если `run-android` выбран на emulator с ABI `x86_64`, команда собирает временный smoke package с runtime identifier `android-x64`; это нужно только для AVD-проверки и не заменяет arm64 APK/AAB.

## Команды

Сформировать план debug APK export:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-android --project <project-root> --format json
```

Создать staging files без запуска Android build:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --skip-publish true --format json
```

Собрать debug APK, если установлены Android SDK, NDK и JDK 17+:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --output exports/android/debug --format json
```

Сформировать план или staging для release AAB:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-android --project <project-root> --configuration Release --output exports/android/release --signing-identity <alias> --signing-credential-reference env:E2D_ANDROID_KEYSTORE --skip-publish true --format json
```

Запустить device или emulator smoke:

```powershell
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

`Assets/electron2d/**` включает project settings, main scene, пользовательские game assets и smoke-логотип. Editor metadata вроде `.electron2d/tasks/**`, локальные task trackers, дневники и completed-task archives не копируются в Android package staging tree.

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
- Android SDK из `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `G:\Android\Sdk` или стандартной пользовательской папки SDK;
- Android NDK из `ANDROID_NDK_HOME` или первой папки SDK `ndk/**`;
- JDK 17+ из `JAVA_HOME` или `G:\Dev\jdk17`;
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
