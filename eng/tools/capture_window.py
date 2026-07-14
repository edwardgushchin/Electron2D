#!/usr/bin/env python3
"""Capture a Windows window or desktop to PNG through FFmpeg gdigrab."""

from __future__ import annotations

import argparse
import ctypes
from ctypes import wintypes
from datetime import datetime
from pathlib import Path
import shutil
import subprocess
import sys
import time
from typing import Optional, Sequence, Tuple


if sys.platform != "win32":
    raise SystemExit("capture_window.py works only on Windows.")


Crop = Tuple[int, int, int, int]
Window = Tuple[int, str, wintypes.RECT]
CaptureAttempt = Tuple[str, Optional[Crop]]

user32 = ctypes.WinDLL("user32", use_last_error=True)
dwmapi = ctypes.WinDLL("dwmapi", use_last_error=True)

DWMWA_EXTENDED_FRAME_BOUNDS = 9
SW_RESTORE = 9
HWND_TOPMOST = -1
HWND_NOTOPMOST = -2
SWP_NOSIZE = 0x0001
SWP_NOMOVE = 0x0002
SWP_NOACTIVATE = 0x0010
SWP_SHOWWINDOW = 0x0040
WINDOW_Z_ORDER_FLAGS = SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW

enum_windows_callback = ctypes.WINFUNCTYPE(
    wintypes.BOOL,
    wintypes.HWND,
    wintypes.LPARAM,
)
user32.EnumWindows.argtypes = [enum_windows_callback, wintypes.LPARAM]
user32.EnumWindows.restype = wintypes.BOOL
user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
user32.GetWindowTextLengthW.restype = ctypes.c_int
user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user32.GetWindowTextW.restype = ctypes.c_int
user32.IsWindowVisible.argtypes = [wintypes.HWND]
user32.IsWindowVisible.restype = wintypes.BOOL
user32.IsIconic.argtypes = [wintypes.HWND]
user32.IsIconic.restype = wintypes.BOOL
user32.ShowWindow.argtypes = [wintypes.HWND, ctypes.c_int]
user32.ShowWindow.restype = wintypes.BOOL
user32.SetForegroundWindow.argtypes = [wintypes.HWND]
user32.SetForegroundWindow.restype = wintypes.BOOL
user32.SwitchToThisWindow.argtypes = [wintypes.HWND, wintypes.BOOL]
user32.SwitchToThisWindow.restype = None
user32.SetWindowPos.argtypes = [
    wintypes.HWND,
    wintypes.HWND,
    ctypes.c_int,
    ctypes.c_int,
    ctypes.c_int,
    ctypes.c_int,
    wintypes.UINT,
]
user32.SetWindowPos.restype = wintypes.BOOL
user32.GetForegroundWindow.restype = wintypes.HWND
user32.GetWindowRect.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.RECT)]
user32.GetWindowRect.restype = wintypes.BOOL
dwmapi.DwmGetWindowAttribute.argtypes = [
    wintypes.HWND,
    wintypes.DWORD,
    wintypes.LPVOID,
    wintypes.DWORD,
]
dwmapi.DwmGetWindowAttribute.restype = ctypes.c_long

try:
    user32.SetProcessDPIAware()
except (AttributeError, OSError):
    pass


def window_title(hwnd: int) -> str:
    """Return the current native title for a window handle."""
    length = user32.GetWindowTextLengthW(hwnd)
    if length <= 0:
        return ""
    buffer = ctypes.create_unicode_buffer(length + 1)
    user32.GetWindowTextW(hwnd, buffer, len(buffer))
    return buffer.value


def window_rect(hwnd: int) -> wintypes.RECT:
    """Return physical extended frame bounds for a native window."""
    rect = wintypes.RECT()
    result = dwmapi.DwmGetWindowAttribute(
        hwnd,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        ctypes.byref(rect),
        ctypes.sizeof(rect),
    )
    if result != 0 and not user32.GetWindowRect(hwnd, ctypes.byref(rect)):
        raise ctypes.WinError(ctypes.get_last_error())
    return rect


def visible_windows() -> list[Window]:
    """Enumerate visible, titled native windows with non-empty bounds."""
    windows: list[Window] = []

    @enum_windows_callback
    def collect(hwnd: int, _lparam: int) -> bool:
        if user32.IsWindowVisible(hwnd):
            title = window_title(hwnd).strip()
            if title:
                try:
                    rect = window_rect(hwnd)
                except OSError:
                    return True
                if rect.right > rect.left and rect.bottom > rect.top:
                    windows.append((int(hwnd), title, rect))
        return True

    if not user32.EnumWindows(collect, 0):
        raise ctypes.WinError(ctypes.get_last_error())
    return windows


def find_window(title_fragment: str) -> Window:
    """Select the largest visible window whose title contains a fragment."""
    needle = title_fragment.casefold()
    matches = [item for item in visible_windows() if needle in item[1].casefold()]
    if not matches:
        raise RuntimeError(f"Window containing {title_fragment!r} was not found.")
    exact = [item for item in matches if item[1].casefold() == needle]
    return max(
        exact or matches,
        key=lambda item: (item[2].right - item[2].left) * (item[2].bottom - item[2].top),
    )


def parse_hwnd(value: str) -> int:
    """Parse a decimal or hexadecimal native window handle."""
    try:
        return int(value, 0)
    except ValueError as error:
        raise argparse.ArgumentTypeError("HWND must be decimal or hexadecimal.") from error


def resolve_ffmpeg() -> str:
    """Return ffmpeg from PATH or fail with an actionable diagnostic."""
    executable = shutil.which("ffmpeg")
    if not executable:
        raise RuntimeError("ffmpeg was not found in PATH.")
    return executable


def console_safe_text(value: str, encoding: Optional[str] = None) -> str:
    """Replace characters unavailable in the active Windows console codepage."""
    selected_encoding = encoding or sys.stdout.encoding or "utf-8"
    return value.encode(selected_encoding, errors="replace").decode(
        selected_encoding,
        errors="replace",
    )


def build_ffmpeg_command(
    executable: str,
    source: str,
    output: Path,
    include_cursor: bool,
    crop: Optional[Crop] = None,
) -> list[str]:
    """Build one deterministic single-frame gdigrab command."""
    command = [
        executable,
        "-hide_banner",
        "-loglevel",
        "error",
        "-y",
        "-f",
        "gdigrab",
        "-draw_mouse",
        "1" if include_cursor else "0",
    ]
    if source == "desktop" and crop is not None:
        left, top, width, height = crop
        command.extend(
            [
                "-offset_x",
                str(left),
                "-offset_y",
                str(top),
                "-video_size",
                f"{width}x{height}",
            ]
        )
    command.extend(["-i", source, "-frames:v", "1", str(output)])
    return command


def run_capture(
    executable: str,
    source: str,
    output: Path,
    include_cursor: bool,
    crop: Optional[Crop] = None,
) -> Tuple[bool, str]:
    """Run one capture attempt and return success plus stderr."""
    process = subprocess.run(
        build_ffmpeg_command(executable, source, output, include_cursor, crop),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    return process.returncode == 0 and output.is_file(), process.stderr.strip()


def default_output(
    home: Optional[Path] = None,
    now: Optional[datetime] = None,
) -> Path:
    """Return a timestamped untracked output path in the Codex temp area."""
    directory = (home or Path.home()) / ".codex" / ".tmp" / "screenshots"
    timestamp = (now or datetime.now()).strftime("%Y%m%d-%H%M%S")
    return directory / f"window-{timestamp}.png"


def rect_crop(rect: wintypes.RECT) -> Crop:
    """Convert native bounds to an FFmpeg desktop crop."""
    return (
        rect.left,
        rect.top,
        rect.right - rect.left,
        rect.bottom - rect.top,
    )


def window_capture_attempts(hwnd: int, title: str, crop: Crop) -> list[CaptureAttempt]:
    """Prefer visible desktop pixels because GPU windows can yield black HWND frames."""
    attempts: list[CaptureAttempt] = [("desktop", crop), (f"hwnd=0x{hwnd:X}", None)]
    if title:
        attempts.append((f"title={title}", None))
    return attempts


def prepare_window(hwnd: int, window_api=user32, sleep=time.sleep) -> bool:
    """Expose a target for desktop crop and report whether TOPMOST needs cleanup."""
    if window_api.IsIconic(hwnd):
        window_api.ShowWindow(hwnd, SW_RESTORE)
    activated = window_api.SetForegroundWindow(hwnd)
    if not activated or int(window_api.GetForegroundWindow()) != hwnd:
        window_api.SwitchToThisWindow(hwnd, True)
    needs_release = False
    if int(window_api.GetForegroundWindow()) != hwnd:
        needs_release = bool(window_api.SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            0,
            0,
            0,
            0,
            WINDOW_Z_ORDER_FLAGS,
        ))
    sleep(0.25)
    return needs_release


def release_window(hwnd: int, window_api=user32) -> None:
    """Remove temporary TOPMOST state after a desktop crop attempt."""
    window_api.SetWindowPos(
        hwnd,
        HWND_NOTOPMOST,
        0,
        0,
        0,
        0,
        WINDOW_Z_ORDER_FLAGS,
    )


def parse_arguments(arguments: Optional[Sequence[str]] = None) -> argparse.Namespace:
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(description=__doc__)
    target = parser.add_mutually_exclusive_group()
    target.add_argument("--title", help="Select the largest visible window containing this title.")
    target.add_argument("--hwnd", type=parse_hwnd, help="Select a window by decimal or hexadecimal HWND.")
    target.add_argument("--desktop", action="store_true", help="Capture the complete virtual desktop.")
    parser.add_argument("--list", action="store_true", help="List visible windows and exit.")
    parser.add_argument("--output", "-o", type=Path, help="PNG output path.")
    parser.add_argument("--delay", type=float, default=0.0, help="Delay before capture in seconds.")
    parser.add_argument("--cursor", action="store_true", help="Include the mouse cursor.")
    parsed = parser.parse_args(arguments)
    if parsed.delay < 0:
        parser.error("--delay cannot be negative.")
    return parsed


def main(arguments: Optional[Sequence[str]] = None) -> int:
    """Run window discovery and a single-frame FFmpeg capture."""
    args = parse_arguments(arguments)
    if args.list:
        for hwnd, title, rect in visible_windows():
            print(console_safe_text(
                f"0x{hwnd:08X}\t{rect.right - rect.left}x{rect.bottom - rect.top}"
                f"+{rect.left}+{rect.top}\t{title}"
            ))
        return 0

    if args.delay:
        time.sleep(args.delay)

    output = (args.output or default_output()).expanduser().resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    executable = resolve_ffmpeg()

    if args.desktop:
        ok, error = run_capture(executable, "desktop", output, args.cursor)
        if not ok:
            raise RuntimeError(error or "FFmpeg desktop capture failed.")
        print(console_safe_text(str(output)))
        return 0

    if args.title:
        hwnd, title, rect = find_window(args.title)
    else:
        hwnd = args.hwnd or int(user32.GetForegroundWindow())
        if not hwnd:
            raise RuntimeError("No foreground window was found.")
        title = window_title(hwnd).strip()
        rect = window_rect(hwnd)

    needs_release = prepare_window(hwnd)
    try:
        rect = window_rect(hwnd)
        errors: list[str] = []
        for source, crop in window_capture_attempts(hwnd, title, rect_crop(rect)):
            ok, error = run_capture(executable, source, output, args.cursor, crop)
            if ok:
                print(console_safe_text(str(output)))
                return 0
            errors.append(f"{source}: {error}")
        raise RuntimeError("\n".join(errors))
    finally:
        if needs_release:
            release_window(hwnd)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, RuntimeError) as error:
        print(
            console_safe_text(f"capture_window: {error}", sys.stderr.encoding),
            file=sys.stderr,
        )
        raise SystemExit(1)
