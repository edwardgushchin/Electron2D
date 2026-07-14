from __future__ import annotations

from datetime import datetime
import importlib.util
from pathlib import Path
import unittest


TOOL_PATH = Path(__file__).resolve().parents[1] / "capture_window.py"


def load_tool():
    if not TOOL_PATH.is_file():
        raise AssertionError(f"Tracked capture helper is missing: {TOOL_PATH}")
    specification = importlib.util.spec_from_file_location("capture_window", TOOL_PATH)
    if specification is None or specification.loader is None:
        raise AssertionError(f"Cannot load capture helper: {TOOL_PATH}")
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


class CaptureWindowToolTests(unittest.TestCase):
    def test_builds_exact_hwnd_capture_command_without_cursor(self) -> None:
        tool = load_tool()

        command = tool.build_ffmpeg_command(
            "ffmpeg.exe",
            "hwnd=0x1234",
            Path("capture.png"),
            include_cursor=False,
        )

        self.assertEqual(command[:8], [
            "ffmpeg.exe",
            "-hide_banner",
            "-loglevel",
            "error",
            "-y",
            "-f",
            "gdigrab",
            "-draw_mouse",
        ])
        self.assertIn("hwnd=0x1234", command)
        self.assertEqual(command[-3:], ["-frames:v", "1", "capture.png"])
        self.assertEqual(command[8], "0")

    def test_builds_desktop_crop_with_physical_window_bounds(self) -> None:
        tool = load_tool()

        command = tool.build_ffmpeg_command(
            "ffmpeg.exe",
            "desktop",
            Path("capture.png"),
            include_cursor=True,
            crop=(-20, 30, 1280, 720),
        )

        self.assertIn("-offset_x", command)
        self.assertEqual(command[command.index("-offset_x") + 1], "-20")
        self.assertEqual(command[command.index("-offset_y") + 1], "30")
        self.assertEqual(command[command.index("-video_size") + 1], "1280x720")
        self.assertEqual(command[8], "1")

    def test_default_output_is_a_timestamped_codex_temp_png(self) -> None:
        tool = load_tool()

        output = tool.default_output(
            Path("C:/Users/example"),
            datetime(2026, 7, 12, 15, 40, 5),
        )

        self.assertEqual(
            output.as_posix(),
            "C:/Users/example/.codex/.tmp/screenshots/window-20260712-154005.png",
        )

    def test_console_text_replaces_characters_missing_from_windows_codepage(self) -> None:
        tool = load_tool()
        self.assertTrue(hasattr(tool, "console_safe_text"), "console-safe output helper is missing")

        text = tool.console_safe_text("The Boys \u200e title", "cp1251")

        self.assertEqual(text, "The Boys ? title")

    def test_window_capture_prioritizes_visible_desktop_pixels_for_gpu_windows(self) -> None:
        tool = load_tool()
        self.assertTrue(hasattr(tool, "window_capture_attempts"), "capture strategy helper is missing")

        attempts = tool.window_capture_attempts(
            0x1234,
            "Tasks - Electron2D - Visual Studio Code",
            (-8, -8, 1936, 1048),
        )

        self.assertEqual(attempts[0], ("desktop", (-8, -8, 1936, 1048)))
        self.assertEqual(attempts[1], ("hwnd=0x1234", None))
        self.assertEqual(
            attempts[2],
            ("title=Tasks - Electron2D - Visual Studio Code", None),
        )

    def test_prepare_window_restores_and_activates_target_before_desktop_crop(self) -> None:
        tool = load_tool()
        self.assertTrue(hasattr(tool, "prepare_window"), "window preparation helper is missing")

        class FakeWindowApi:
            def __init__(self) -> None:
                self.calls = []

            def IsIconic(self, hwnd: int) -> bool:
                self.calls.append(("is-iconic", hwnd))
                return True

            def ShowWindow(self, hwnd: int, command: int) -> bool:
                self.calls.append(("show", hwnd, command))
                return True

            def SetForegroundWindow(self, hwnd: int) -> bool:
                self.calls.append(("foreground", hwnd))
                return True

            def GetForegroundWindow(self) -> int:
                return 0x1234

        api = FakeWindowApi()
        delays = []

        needs_release = tool.prepare_window(0x1234, api, delays.append)

        self.assertEqual(api.calls, [
            ("is-iconic", 0x1234),
            ("show", 0x1234, tool.SW_RESTORE),
            ("foreground", 0x1234),
        ])
        self.assertEqual(delays, [0.25])
        self.assertFalse(needs_release)

    def test_prepare_window_uses_native_switch_when_foreground_lock_rejects_activation(self) -> None:
        tool = load_tool()

        class LockedWindowApi:
            def __init__(self) -> None:
                self.calls = []

            def IsIconic(self, hwnd: int) -> bool:
                return False

            def SetForegroundWindow(self, hwnd: int) -> bool:
                self.calls.append(("foreground", hwnd))
                return False

            def GetForegroundWindow(self) -> int:
                self.calls.append(("read-foreground",))
                return 0x9999

            def SwitchToThisWindow(self, hwnd: int, activate: bool) -> None:
                self.calls.append(("switch", hwnd, activate))

            def SetWindowPos(self, hwnd: int, insert_after: int, x: int, y: int, width: int, height: int, flags: int) -> bool:
                self.calls.append(("position", hwnd, insert_after, x, y, width, height, flags))
                return True

        api = LockedWindowApi()

        needs_release = tool.prepare_window(0x1234, api, lambda _seconds: None)

        self.assertEqual(api.calls, [
            ("foreground", 0x1234),
            ("switch", 0x1234, True),
            ("read-foreground",),
            ("position", 0x1234, -1, 0, 0, 0, 0, 0x53),
        ])
        self.assertTrue(needs_release)

    def test_prepare_window_verifies_actual_foreground_after_false_success(self) -> None:
        tool = load_tool()

        class MisleadingWindowApi:
            def __init__(self) -> None:
                self.calls = []

            def IsIconic(self, hwnd: int) -> bool:
                return False

            def SetForegroundWindow(self, hwnd: int) -> bool:
                self.calls.append(("foreground", hwnd))
                return True

            def GetForegroundWindow(self) -> int:
                self.calls.append(("read-foreground",))
                return 0x9999

            def SwitchToThisWindow(self, hwnd: int, activate: bool) -> None:
                self.calls.append(("switch", hwnd, activate))

            def SetWindowPos(self, hwnd: int, insert_after: int, x: int, y: int, width: int, height: int, flags: int) -> bool:
                self.calls.append(("position", hwnd, insert_after, x, y, width, height, flags))
                return True

        api = MisleadingWindowApi()

        needs_release = tool.prepare_window(0x1234, api, lambda _seconds: None)

        self.assertEqual(api.calls, [
            ("foreground", 0x1234),
            ("read-foreground",),
            ("switch", 0x1234, True),
            ("read-foreground",),
            ("position", 0x1234, -1, 0, 0, 0, 0, 0x53),
        ])
        self.assertTrue(needs_release)

    def test_release_window_removes_temporary_topmost_state(self) -> None:
        tool = load_tool()
        self.assertTrue(hasattr(tool, "release_window"), "window release helper is missing")

        class FakeWindowApi:
            def __init__(self) -> None:
                self.calls = []

            def SetWindowPos(self, hwnd: int, insert_after: int, x: int, y: int, width: int, height: int, flags: int) -> bool:
                self.calls.append(("position", hwnd, insert_after, x, y, width, height, flags))
                return True

        api = FakeWindowApi()

        tool.release_window(0x1234, api)

        self.assertEqual(api.calls, [
            ("position", 0x1234, -2, 0, 0, 0, 0, 0x53),
        ])


if __name__ == "__main__":
    unittest.main()
