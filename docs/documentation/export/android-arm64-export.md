# Android arm64 export

## Status

`AndroidArm64` с runtime identifier `android-arm64` теперь имеет зафиксированный в репозитории baseline экспорта для Electron2D `0.1.0 Preview`:

- planner для debug APK и release AAB workflow;
- transient Android staging project в выбранной папке export output;
- команда `build-android --skip-publish true` для deterministic staging checks;
- обычная команда `build-android`, которая собирает debug APK при наличии Android SDK, NDK и JDK 17+;
- release AAB signing plan с не-секретными signing references;
- structured device-smoke artifact со статусом blocked, если подключённое устройство или emulator недоступны.

Цель **ещё не принята как полный Tier 1 runtime path** и is not a ready release path, потому что на текущей рабочей станции нет подключённого Android device и не установлен emulator package/AVD. Release-критерий с real-device или emulator smoke остаётся открытым.

## Commands

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

Запустить device-smoke command:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-android --project <project-root> --output exports/android/debug --smoke-output .electron2d/export-smoke/android-smoke.json --format json
```

На текущей рабочей станции эта команда записывает blocked smoke artifact с `E2D-EXPORT-ANDROID-0014`, потому что `adb devices -l` не возвращает подключённых устройств.

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
    Resources/
      values/
        electron2d_export.xml
  artifacts/
    debug/
    release/
  smoke/
```

Staging project является generated output. Он не становится canonical game project и не записывается обратно в исходный project root.

`Assets/electron2d/**` включает project settings, main scene и пользовательские game assets. Editor metadata вроде `.electron2d/tasks/**`, локальные task trackers, дневники и completed-task archives не копируются в Android package staging tree.

## Runtime policy captured by the staging project

Generated Android host фиксирует mobile runtime contract, который будущий device smoke должен проверить:

- touch input path через `OnTouchEvent`;
- lifecycle markers для pause, resume, stop и destroy;
- requested screen orientation из project display settings;
- immersive fullscreen flags;
- safe-area readiness marker;
- packaged project resources внутри Android assets;
- mobile graphics profile metadata;
- compatibility renderer fallback policy.

Runtime data model для touch, orientation и safe area покрыта существующими mobile input и display tests. Package smoke на реальном устройстве или emulator всё ещё обязателен до принятия target как завершённого.

## SDK and toolchain

`build-android` обнаруживает:

- .NET SDK через существующую SDK probe;
- Android SDK из `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `G:\Android\Sdk` или стандартной пользовательской папки SDK;
- Android NDK из `ANDROID_NDK_HOME` или первой папки SDK `ndk/**`;
- JDK 17+ из `JAVA_HOME` или `G:\Dev\jdk17`.

Если `JAVA_HOME` указывает на более старый JDK, CLI пропускает его и использует первый найденный путь JDK 17+. Это защищает Android manifest tooling от падения из-за несовместимой версии Java class files.

## Signing and credentials

Release AAB planning требует:

- `--signing-identity <alias>`;
- `--signing-credential-reference <reference>`.

Plan хранит только не-секретные labels и references. Он не хранит keystore contents, passwords, private keys, certificates или tokens. Полный release publish с реальными signing credentials остаётся заблокированным до появления пользовательского keystore и механизма доставки secret values.

## Current verification

Текущие проверки, выполненные 2026-06-23:

- focused integration tests для Android planner, package builder, device-smoke artifact и CLI Android export routes прошли;
- `e2d export build-android --project .temp/android-export-smoke/project --output exports/android/debug --format json` собрал debug Android package из empty project template;
- generated APK files найдены в `exports/android/debug/android/bin/Debug/net10.0-android/android-arm64/`;
- `e2d export build-android --configuration Release --skip-publish true` создал release AAB staging и signing plan без чтения секретов;
- `e2d export run-android` записал blocked smoke artifact, потому что device/emulator недоступен.

## Known limitations

- Real-device/emulator smoke не завершён на этой рабочей станции.
- `run-android` сейчас записывает blocked artifact вместо установки и запуска APK.
- Release AAB publish с реальными signing credentials не выполняется без пользовательской доставки keystore/password.
- Device lifecycle, render, input, audio, resources, filesystem и shutdown остаются непринятыми, пока не будет получен реальный Android smoke run.
