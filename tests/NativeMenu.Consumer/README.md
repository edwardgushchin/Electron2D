# Native popup consumer

The public-only consumer shares NativeMenuChecks with the root runner. Build the independent Windows inspector with the current Windows SDK from a Developer PowerShell:

```powershell
$receipts = 'C:/absolute/native-menu-receipts'
New-Item -ItemType Directory -Force -Path $receipts
cl /nologo /EHsc /O2 /MT /std:c++17 /utf-8 tools/native-menu/oracle.cpp /Fe:"$receipts/oracle.exe" /Fo:"$receipts/oracle.obj" user32.lib gdi32.lib oleacc.lib oleaut32.lib ole32.lib
dotnet run --project tests/NativeMenu.Consumer/NativeMenu.Consumer.csproj -c Release -- "$receipts/oracle.exe" $receipts
```

Use a matching full native bundle following [native packaging](../../docs/native-packaging.md); stale platform-package overrides do not qualify the current runtime. All outputs and temporary build objects belong outside the source tree. With SDL_VIDEODRIVER=dummy, the `unavailable` argument verifies explicit rejection before Windows native entry. The root runner accepts ELECTRON2D_TEST_NATIVE_MENU=ORACLE and its first app argument is the receipts folder; `unavailable` selects only the unsupported-driver check.

The oracle scopes discovery and messages to its target PID, validates native labels/state/geometry/alpha icon pixels, uses MSAA default actions for selection and opens real nested menus. It captures only the tested popup window. No physical cursor movement or global input injection is used. The keyboard profile posts Down/Enter only after checking the exact tested foreground HWND/PID before every key pair, and cancels if that guard fails. Direct window keyboard messages cancel unfocused menus and are not selection evidence. Receipts distinguish source/fresh package/strict full-trim NativeAOT from other-platform, physical user and release-package qualification. See [the component](../../docs/components/native-menus.md).

ELECTRON2D_NATIVE_MENU_RENDERER=gpu runs the scene bridge and canvas fallback on the GPU renderer; the default is compatibility. On the qualified Windows host, SDL_GPU_DRIVER=vulkan selects the exercised GPU driver. OS popup flags/pixels remain independently inspected in either profile.
