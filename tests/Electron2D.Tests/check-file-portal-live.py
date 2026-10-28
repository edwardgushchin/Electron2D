#!/usr/bin/python3
"""Accept and cancel uniquely titled real desktop-portal choosers via AT-SPI."""
import json
import os
from pathlib import Path
import resource
import subprocess
import time

import gi
gi.require_version("Atspi", "2.0")
from gi.repository import Atspi, GLib

resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = Path(os.environ["ELECTRON2D_PORTAL_TEST_OUTPUT"])
FOLDER = Path(os.environ["ELECTRON2D_PORTAL_FOLDER"])
Atspi.init()
context = GLib.MainContext.default()


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


def wait(check):
    deadline = time.monotonic() + 12
    while time.monotonic() < deadline:
        while context.pending():
            context.iteration(False)
        try:
            value = check()
            if value:
                return value
        except GLib.Error:
            pass
        time.sleep(.03)
    raise AssertionError("Native chooser state did not converge")


def chooser(title):
    # Inspect top-level windows first; unrelated application trees can be large.
    for app in children(Atspi.get_desktop(0)):
        if app:
            for window in children(app):
                if window:
                    window.clear_cache()
                    if window.get_name() == title:
                        return window


def select_file(root):
    queue = [root]
    while queue:
        node = queue.pop()
        node.clear_cache()
        entries = [n for n in children(node) if n]
        if node.get_role() == Atspi.Role.TABLE:
            for i, entry in enumerate(entries):
                if entry.get_name().startswith("выбранный #.txt"):
                    assert node.get_selection_iface().select_child(i)
                    return True
        queue.extend(entries)


def action(node, name):
    node.clear_cache()
    actions = node.get_action_iface()
    for i in range(actions.get_n_actions()):
        if actions.get_localized_name(i) == name:
            assert actions.do_action(i)
            return True


env = dict(os.environ, SDL_VIDEODRIVER="wayland", SDL_VIDEO_DRIVER="wayland", ELECTRON2D_TEST_FILE_PORTAL="live")
env.pop("NO_AT_BRIDGE", None)
child = subprocess.Popen(["dotnet", str(ROOT / "tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll")], env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
steps = []
try:
    root = wait(lambda: chooser("Electron2D MIME chooser accept"))
    wait(lambda: select_file(root))
    wait(lambda: action(root, "chooser.accept"))
    steps.append({"chooser": "accept", "observed_file": "выбранный #.txt", "action": "chooser.accept"})
    root = wait(lambda: chooser("Electron2D MIME chooser cancel"))
    wait(lambda: action(root, "window.close"))
    steps.append({"chooser": "cancel", "action": "window.close"})
    output, _ = child.communicate(timeout=8)
    (OUTPUT / "installed-portal.log").write_text(output)
    assert child.returncode == 0, output
    assert "NATIVE_DONE:accept" in output and "NATIVE_DONE:cancel" in output
    (OUTPUT / "installed-portal.json").write_text(json.dumps({"returncode": child.returncode, "steps": steps, "selected_filter_index": 0}, indent=2, ensure_ascii=False) + "\n")
    print("Installed Wayland portal: native MIME file acceptance, selected filter and cancellation passed.")
finally:
    if child.poll() is None:
        child.terminate()
        child.wait(timeout=5)
