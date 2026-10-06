# CharacterMovement

One scene for Windows, Linux, macOS, Android, Android TV, iOS, tvOS and Web. Use arrow keys on a computer, drag the character on a touchscreen, or use the directional buttons on a TV remote. The character stays inside the field. Escape or closing the desktop window exits the application.

Desktop windows are fixed at 800 by 600. Mobile and TV apps fill their native surface; Web fills the browser viewport and has a fullscreen button. On these surfaces the short side represents 600 logical units. The whole canvas scales uniformly while the field expands to the screen's aspect ratio, without letterboxing, cropping or stretching. Movement remains 160 logical units per second and the character remains 96 logical units wide and high.

![CharacterMovement on Linux x64/Wayland](../../docs/images/character-movement.png)

The example uses public Electron2D APIs and platform SDK entry points. [CharacterMovementGame.cs](CharacterMovementGame.cs) keeps the bundled images and font alive until exit. [CharacterMovementScene.cs](CharacterMovementScene.cs) creates the field; [Player.cs](Player.cs) handles movement and dragging. Desktop uses `Engine.Run`; browser and mobile hosts use `Engine.RunAsync`.

## Prerequisites

Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Git. Run the following commands from the repository root:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
```

The example references the engine source in this checkout. NuGet restores the selected `Electron2D.{Platform}` package and native dependencies automatically. The new `Engine.RunAsync` host is not in the published `Electron2D` version `0.1.0-alpha`; use the checkout for these mobile, TV and browser entry points until a release includes it. A desktop project using the published packages is described in the [main README](../../README.md#use-electron2d-in-your-game).

| Target | Additional tools |
| --- | --- |
| Windows, Linux, macOS | The target OS for execution |
| Android, Android TV | `android` workload, Android SDK, its `adb` platform tool and a compatible JDK |
| Web | `wasm-tools` workload, Chrome and Python 3 for the local HTTP server |
| iOS, tvOS | macOS, Xcode and the matching .NET `ios` or `tvos` workload; signing for real devices |

## Windows

Run on Windows:

```powershell
dotnet run --project examples/CharacterMovement -c Release -r win-x64
```

Choose `win-x86` or `win-arm64` for those architectures. For a standalone application:

```powershell
dotnet publish examples/CharacterMovement -c Release -r win-x64 --self-contained true -o bin/character-movement/windows
.\bin\character-movement\windows\CharacterMovement.exe
```

## Linux

Run on Linux:

```bash
dotnet run --project examples/CharacterMovement -c Release -r linux-x64
```

Use `linux-arm64` on ARM64. The desktop session chooses Wayland or X11. To select Wayland explicitly:

```bash
SDL_VIDEODRIVER=wayland dotnet run --project examples/CharacterMovement -c Release -r linux-x64
```

Publish and run without a separately installed .NET runtime:

```bash
dotnet publish examples/CharacterMovement -c Release -r linux-x64 --self-contained true -o bin/character-movement/linux
./bin/character-movement/linux/CharacterMovement
```

On first launch the app registers its icon and desktop entry under `$XDG_DATA_HOME`, or `~/.local/share` when unset. The entry points to that executable; publish to a permanent location to keep the shortcut usable.

## macOS

Build on macOS and open the application bundle so Finder and the Dock use the app icon:

```bash
dotnet build examples/CharacterMovement -c Release -r osx-arm64
open examples/CharacterMovement/bin/Release/net10.0/osx-arm64/CharacterMovement.app
```

Use `osx-x64` on Intel. For a self-contained bundle:

```bash
dotnet publish examples/CharacterMovement -c Release -r osx-arm64 --self-contained true -o bin/character-movement/macos
open bin/character-movement/macos/CharacterMovement.app
```

This local bundle is not a signed or notarized distribution package.

## Android Phone Or Tablet

Configure the Android SDK and JDK with the [.NET Android setup instructions](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies). Enable USB debugging and approve this computer's request on the device.

```bash
dotnet workload install android
adb devices -l
adb -s PHONE shell getprop ro.product.cpu.abi
dotnet build examples/CharacterMovement -c Release -r android-arm64
adb -s PHONE install --no-incremental -r examples/CharacterMovement/bin/Release/net10.0-android/android-arm64/org.electron2d.charactermovement-Signed.apk
adb -s PHONE shell am start -n org.electron2d.charactermovement/org.electron2d.GameActivity
```

Replace `PHONE` with the identifier from `adb devices`. Match the RID to its ABI:

| Android ABI | RID |
| --- | --- |
| `arm64-v8a` | `android-arm64` |
| `armeabi-v7a` | `android-arm` |
| `x86_64` | `android-x64` |
| `x86` | `android-x86` |

Replace both the RID argument and the RID directory in the APK path. The Release APK uses a development signing key for local testing. `install -r` updates the application without uninstalling it.

The app uses the full surface in the device's current orientation. Touch the character and drag it; lifting your finger releases it. A second finger does not take over an existing drag. Losing focus cancels dragging.

## Android TV

The same Android project and package include a TV launcher entry and banner. Enable network debugging, connect to the TV's displayed ADB address and approve the computer:

```bash
adb connect TV_ADDRESS:5555
adb devices -l
adb -s TV_ADDRESS:5555 shell getprop ro.product.cpu.abi
dotnet build examples/CharacterMovement -c Release -r android-arm
adb -s TV_ADDRESS:5555 install --no-incremental -r examples/CharacterMovement/bin/Release/net10.0-android/android-arm/org.electron2d.charactermovement-Signed.apk
adb -s TV_ADDRESS:5555 shell am start -n org.electron2d.charactermovement/org.electron2d.GameActivity
```

Use the TV's actual address and port. The command uses `android-arm` for a 32-bit ARM TV; an ARM64 TV needs `android-arm64` in both paths. The app fills the screen. Remote directional buttons use the same input actions as arrow keys and a gamepad D-pad.

## Web

Serve the published `wwwroot` directory over HTTP; do not open `index.html` as a local file.

```bash
dotnet workload install wasm-tools
dotnet publish examples/CharacterMovement -c Release -r browser-wasm -o bin/character-movement/web
python3 -m http.server 5187 --bind 127.0.0.1 --directory bin/character-movement/web/wwwroot
```

Open [http://127.0.0.1:5187/](http://127.0.0.1:5187/) in Chrome. Use a free port if 5187 is occupied. Keep the server running while playing; Ctrl+C stops it.

On a computer, click the canvas for keyboard focus and use the arrow keys. A touchscreen browser supports the phone app's drag gesture. The canvas uses all available viewport space at native pixel density. Click the fullscreen button to use the whole screen: browsers require a user gesture and can refuse automatic requests. Resizing or rotating the browser adapts the field.

## iOS

Run on macOS with the Xcode version supported by your .NET 10 workload:

```bash
dotnet workload install ios
xcrun simctl list devices available
dotnet build examples/CharacterMovement -c Release -r iossimulator-arm64 -p:EnableCodeSigning=false
```

Use `iossimulator-x64` on Intel. Choose an iPhone or iPad simulator identifier from the list. If it is shut down, boot it first:

```bash
xcrun simctl boot SIMULATOR_ID
xcrun simctl bootstatus SIMULATOR_ID -b
open -a Simulator
xcrun simctl install SIMULATOR_ID examples/CharacterMovement/bin/Release/net10.0-ios/iossimulator-arm64/CharacterMovement.app
xcrun simctl launch SIMULATOR_ID org.electron2d.charactermovement
```

Replace `SIMULATOR_ID` and the RID directory. The app opens fullscreen; use touch dragging on the device or the simulator's touch input.

For a real iPhone or iPad, use `ios-arm64`, a signing identity and a provisioning profile that permits the app identifier:

```bash
dotnet build examples/CharacterMovement -c Release -r ios-arm64 -p:CodesignKey="Apple Development" -p:CodesignProvision="YOUR_PROFILE"
xcrun devicectl list devices
xcrun devicectl device install app --device DEVICE_ID examples/CharacterMovement/bin/Release/net10.0-ios/ios-arm64/CharacterMovement.app
xcrun devicectl device process launch --device DEVICE_ID org.electron2d.charactermovement
```

Replace the signing values and device identifier with your own. An unsigned `ios-arm64` artifact cannot be installed as a working device app.

## Apple TV (tvOS)

On macOS, select an Apple TV simulator:

```bash
dotnet workload install tvos
xcrun simctl list devices available
dotnet build examples/CharacterMovement -c Release -r tvossimulator-arm64 -p:EnableCodeSigning=false
xcrun simctl boot SIMULATOR_ID
xcrun simctl bootstatus SIMULATOR_ID -b
open -a Simulator
xcrun simctl install SIMULATOR_ID examples/CharacterMovement/bin/Release/net10.0-tvos/tvossimulator-arm64/CharacterMovement.app
xcrun simctl launch SIMULATOR_ID org.electron2d.charactermovement
```

Skip `boot` for an already booted simulator. Use `tvossimulator-x64` on Intel and replace the RID directory. Send directions with the simulator remote or a connected controller.

For a physical Apple TV, use `tvos-arm64` and its development signing identity and profile:

```bash
dotnet build examples/CharacterMovement -c Release -r tvos-arm64 -p:CodesignKey="Apple Development" -p:CodesignProvision="YOUR_PROFILE"
xcrun devicectl device install app --device DEVICE_ID examples/CharacterMovement/bin/Release/net10.0-tvos/tvos-arm64/CharacterMovement.app
xcrun devicectl device process launch --device DEVICE_ID org.electron2d.charactermovement
```

The app fills the TV surface and uses directional input delivered by the platform's remote/controller integration.

## App Icon

All assets derive from the existing [app-icon.svg](../../docs/design/assets/sprite/app-icon.svg). Windows embeds the ICO, Linux registers the PNG and desktop entry, macOS bundles the ICNS, Android uses a launcher icon and TV banner, iOS/tvOS use asset catalogs, and Web has an SVG favicon.

Apple catalogs and the TV banner are checked in; normal builds need no image conversion tool. After an approved icon change, regenerate them with Pillow and `python3 tools/examples/character_movement_icons.py` from the repository root.

## Start Your Own Project

Keep a new consumer beside this example so its relative engine and shared-asset references remain valid. Copy source and platform assets, excluding generated `bin` and `obj` directories:

```bash
mkdir examples/MyGame
cp examples/CharacterMovement/*.cs examples/CharacterMovement/CharacterMovement.csproj examples/MyGame/
cp -R examples/CharacterMovement/Platforms examples/CharacterMovement/wwwroot examples/MyGame/
mv examples/MyGame/CharacterMovement.csproj examples/MyGame/MyGame.csproj
```

On Windows, use equivalent file-copy commands or File Explorer. Set your own `ApplicationId` in `MyGame.csproj` and matching Android/Apple manifest identifiers before installing beside CharacterMovement. Change display names, the Linux desktop-entry name and window title. In the macOS manifest, change `CFBundleExecutable` to `MyGame`; its bundle directory follows the assembly name automatically.

Replace the scene and player with your game's code, retain the platform entry points and keep resources alive until the run task completes. Use the commands above with `examples/MyGame/MyGame.csproj` and replace output assembly/bundle names with `MyGame`. This project still references the engine source.

## Verification

Check platform project selection and icon references without running foreign applications:

```bash
python3 -B tools/examples/test_character_movement.py
```

For a Linux self-contained publish, audit its native dependencies and required engine/application notices:

```bash
python3 -B tools/check_native_publish.py linux-x64 bin/character-movement/linux
python3 -B tools/licenses/check_publish.py bin/character-movement/linux --application-license editor/Assets/IBMPlexSans-OFL.txt
```

The project selects all 18 supported RIDs: three Windows, two Linux, two macOS, four Android, three iOS, three tvOS and one browser target. The corresponding SDK and target OS are required for a build or execution; a RID declaration does not prove the app ran there.

On 2026-10-06 the owner confirmed the phone, Android TV and local Chrome example worked, including adaptive layout and controls. Linux x64/Wayland has automated rendered checks. Windows, macOS, iOS and tvOS execution was not checked for this change. See [platform verification](../../docs/platform-verification.md#character-movement).

Run the focused Linux check:

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_CHARACTER_MOVEMENT=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
```

It checks GPU and compatibility hosts, fixed desktop pixels, four directions, key release, diagonal bounds, native touch, D-pad actions, synchronous/asynchronous exit, startup rejection/cancellation and cleanup. Five surface layouts are checked. Unmodified frames are saved to `bin/character-movement/`; readback belongs to tests, not to the example's public API.
