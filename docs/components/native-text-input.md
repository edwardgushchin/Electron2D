# Native text input

DisplayServer.DialogInputText presents native single-line input with a title, plain-text description, initial text and Action<string> confirmation callback. It is useful for short names and identifiers in games or project tools. It requires an active display on its opening thread. Engine frames pause until the dialog closes.

## Native controls and availability

NativeDialogInput is true on the Windows SDL driver and on Linux when optional GTK 3.22 or later initializes with a default display matching SDL's active Wayland or X11 driver. The probe opens no dialog. First use on Linux can initialize the toolkit. Dummy, missing GTK, a mismatched default toolkit display and remaining native hosts without an adapter return false. Invocation fails explicitly when unavailable. No toolkit or display pointer is public.

The implementation uses native GTK dialog, label, entry and translated confirmation/cancel buttons. The main SDL window remains logically retained for the entire modal call. Its exported Wayland parent or X11 foreign-window wrapper supplies the native transient hint when available; a compositor may ignore it. GTK controls appearance, fonts, accessibility and keyboard input. The label uses plain text, including angle brackets and ampersands.

The Windows adapter builds a native dialog template and calls DialogBoxIndirectParamW with the retained SDL HWND. The description is plain text in a read-only scroll field when present. A standard Unicode single-line EDIT has the accessible Input label, focus and initial selection. The Input, OK and Cancel captions resolve through the default project translation domain with context 'native text dialog'. Enter activates the default button and Escape cancels through the Win32 dialog manager. See [DialogBoxIndirectParamW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-dialogboxindirectparamw).

## Text, callbacks and lifetime

Null arguments and embedded NUL characters reject before presentation. Each adapter reads back the initial text before showing the dialog. Invalid UTF-16 or native-buffer truncation raises ArgumentException. The native entry widget handles single-line editing.

Confirmation copies the native GTK UTF-8 or Win32 UTF-16 entry into a managed string before native widgets are destroyed. Windows rejects malformed UTF-16 or a CR/LF in initial input before opening and verifies the exact native edit readback before presentation. An empty accepted string is valid. Cancel and window close return without invoking the callback. All native references and parent retention are released before the owner-thread callback; its exception propagates, a new input dialog may open, and a manually owned display may be disposed there. Disposing Engine-owned services directly retains its ordinary lifecycle restriction. The callback still runs in its caller’s scene phase: invoking from a render callback does not permit recursive scene/input execution.

During the native modal loop, display disposal, SDL ProcessEvents/ForceProcessAndDropEvents and recursive text-input calls reject. Native callbacks cannot tear down the retained parent or reenter the SDL pump. Windows excludes an active file chooser when starting text input and excludes message, alert and file presentations during text input. File chooser completion retains its separate owner-pumped contract. A positive capability query does not guarantee later native creation success. This cold UI flow has no real-time or zero-allocation claim.

## Windows native verification

Build the SDK probe in the x64 Visual Studio Developer shell with cl /std:c++20 /EHsc /utf-8, tools/windows-text-input-probe.cpp and uiautomationcore.lib ole32.lib oleaut32.lib user32.lib uuid.lib. Build tests/WindowsTextInput.Consumer in Release. Pass the profile and the probe executable path to the consumer. The root runner uses ELECTRON2D_TEST_WINDOWS_TEXT_INPUT and the probe path. The profiles are raw, gpu, compatibility and unavailable. Select SDL_VIDEODRIVER=dummy for unavailable.

The SDK process selects the exact fixture PID and unique window title. It verifies native ownership, translated captions, an accessible edit name, focus and selection, and performs native edit and button or keyboard actions. Owner timers exercise modal exclusion, retirement and pump guards. Raw tests cover exact edited Unicode, empty confirmation, cancellation, Escape, window close, long input of 70,002 UTF-16 units, long literal description, callback failure, callback reentry and disposal. GPU and Compatibility scenes check the next rendered frame. On Windows x64, the root runner, public source consumer, Core package consumer and strict NativeAOT consumer each passed 164 raw, 18 GPU, 18 Compatibility and 11 dummy assertions with the source-qualified native DLL closure. The existing Windows file chooser suite passed 102 regression assertions. Physical typing, IME, native-heap accounting and other targets require their own qualification.

## Linux executable checks

Build tests/Electron2D.Tests in Release for linux-x64. Run its compiled DLL with SDL_VIDEODRIVER=dummy and ELECTRON2D_TEST_NATIVE_TEXT_INPUT=unsupported for unavailable-service/backend, null/NUL and owner-thread checks. The mismatch profile initializes GTK first with GDK_BACKEND=x11, then opens SDL with SDL_VIDEODRIVER=wayland: capability remains false and invocation rejects. Setting GDK_BACKEND alone after SDL initialization does not prove a mismatch; a decoration provider may already have initialized GTK.

With a real Linux desktop, the installed Python GI Atspi binding and accessibility bus, run:

```sh
SDL_VIDEODRIVER=wayland GDK_BACKEND=wayland \
ELECTRON2D_INPUT_TEST_OUTPUT=/absolute/output/directory \
/usr/bin/python3 -B tests/Electron2D.Tests/check-native-text-input.py
```

The controller inspects only uniquely titled test dialogs, edits and reads their real accessible text, verifies modal state and activates native confirmation/cancel buttons. Eight cases cover Unicode with combining marks, an empty result, cancellation, a native gtk_window_close request, callback failure/recovery, callback reentry and disposal. Native GLib timer callbacks exercise retirement, pump and recursive-input guards while the actual modal loop runs. The close case is requested by the independent controller and executed by the test on the native owner; it does not establish physical title-bar input. Invalid initial UTF-16 and an oversized initial entry reject before showing UI.

Repeat with ELECTRON2D_INPUT_TEST_PROFILE=gpu and compatibility in separate output directories. Each profile opens input from a running Engine scene and checks real red pixels on the following rendered frame, then normal scene/display/renderer shutdown. Set SDL_GPU_DRIVER=vulkan where the GPU driver needs explicit selection. The same controller can select X11 with SDL_VIDEODRIVER=x11 and GDK_BACKEND=x11.

Linux qualification on 2026-10-29 passed eight native dialogs on Wayland and eight on Xwayland, plus running GPU and Compatibility scenes, dummy rejection and a preinitialized mismatched GTK display. Existing message/file-dialog, private portal and Wayland theme regressions also passed. Qualification covers these executed Linux profiles only. Physical typing, human font inspection, independent X11 window-manager parenting, foreign hosts, NativeAOT and native-heap accounting need their own evidence.

The callback and platform boundary belongs to [ADR 0040](../decisions/display.md#adr-0040). GTK documents the [recursive modal loop](https://docs.gtk.org/gtk3/method.Dialog.run.html) and [borrowed entry text](https://docs.gtk.org/gtk3/method.Entry.get_text.html); copying precedes widget teardown.
