# Electron2D UI-heavy reference game

This project is the `0.1.0 Preview` UI-heavy reference game. It is a valid `Electron2D.Editor` project and is intentionally kept as an ordinary user project: project settings, scenes, scripts, resources, export presets and ProjectTaskManager metadata are all stored inside the project directory.

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File ..\..\tools\Verify-UiHeavyReference.ps1
```

The verifier checks the project contract, UI-heavy gameplay markers, localization files, resource manifest, Android `Compatibility` export preset, headless run output and WebAssembly package contents.
