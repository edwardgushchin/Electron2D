# Native modal alerts

Last updated: 2026-10-27

[OS.Alert](../classes/OS.md#alert) displays a native message with one acknowledgement button. It works before Engine.Start, before any DisplayServer exists and after shutdown, so a game can report a startup or renderer failure. It creates no engine scene, display service or renderer. The call blocks until the message is acknowledged or closed. The default title is `Alert!`; empty title/text are valid.

## Ownership and native presentation

The retained OS service enforces the active Engine lifecycle owner. If a DisplayServer exists, its owner check also applies and the main native window is supplied as a parent hint. Outside these lifecycles, the caller must use the host's main thread, following the native message-box contract. No new persistent main-thread identity is captured by first use.

The host provider controls appearance, button placement, window attachment, fonts and glyph coverage. Some system helper programs omit the parent hint. The message is passed as UTF-8; embedded null characters in title/body reject before presentation rather than silently truncating text. Null arguments reject. Native failure throws InvalidOperationException and leaves existing engine/display ownership intact. This is a cold blocking operation; no real-time allocation or rendering-throughput claim applies.

The same string validation covers DisplayServer.DialogShow titles, descriptions and button labels. Native buttons already marshaled before a later invalid label are released. DialogShow initializes the selected index to -1: providers that leave output unwritten on window close cannot accidentally confirm button zero. Explicit native button indices remain unchanged. The callback runs on the display owner before return, can open another alert, and exceptions still release the marshaled button storage. [Display dialogs](file-dialogs.md) retain their separate asynchronous file-chooser contract.

[Native text input](native-text-input.md) adds an independent single-line confirmation flow through DisplayServer.DialogInputText.

## Executable checks

`tests/NativeAlerts.Consumer` uses only public engine API. The `managed` profile checks null/NUL values and a worker rejected by the active manual Engine lifecycle, without requesting a dialog. The `failure` profile uses `SDL_VIDEO_DRIVER=dummy` and checks repeated native failure before/during/after a display, partial button-marshaling cleanup and retained display operation.

The `native` profile requests seven real dialogs: fresh pre-startup, manual lifecycle, active display, a multi-button close/callback, a nested alert in that callback, post-display shutdown and empty text/title. It checks wrong display-owner access, cancellation index and final service retirement. The Linux controller opens a private Mutter/Xwayland desktop and closes only the matching test dialogs with WM_DELETE_WINDOW. It checks actual window titles, records any transient relationship and captures pre-startup/owned window pixels. A private software GTK renderer avoids requiring physical monitor presentation. A native font/provider is still required for the requested text.

```sh
dotnet build tests/NativeAlerts.Consumer/NativeAlerts.Consumer.csproj -c Release -r linux-x64
dotnet tests/NativeAlerts.Consumer/bin/Release/net10.0/linux-x64/NativeAlerts.Consumer.dll managed
SDL_VIDEO_DRIVER=dummy dotnet tests/NativeAlerts.Consumer/bin/Release/net10.0/linux-x64/NativeAlerts.Consumer.dll failure
dbus-run-session -- python3 -B tests/NativeAlerts.Consumer/run_native.py clr /absolute/output/directory
```

Repeat with strict full-trim NativeAOT published into `tests/NativeAlerts.Consumer/bin/aot`, passing `aot` to the controller. The controller requires Mutter, Xwayland, xdotool, xprop, ImageMagick import and the host's native message-box provider. Its screenshot/native-window evidence does not establish native-heap costs, physical desktop input, all optional providers or foreign-platform acceptance. The direct X11 toolkit's non-ASCII font path remains separate from the checked system-helper text rendering.

Native lifecycle/thread semantics: [SDL_ShowMessageBox](https://wiki.libsdl.org/SDL3/SDL_ShowMessageBox). The engine retains the static service policy in [ADR 0095](../decisions/singleton-services.md#adr-0095) and the platform qualification gate in [ADR 0021](../decisions/product.md#adr-0021).
