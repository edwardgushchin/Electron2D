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
EXTRA = os.environ.get("ELECTRON2D_PORTAL_LIVE_OPTIONS") == "1"
TITLE = "Electron2D options chooser " if EXTRA else "Electron2D MIME chooser "
PREFIX = "installed-options" if EXTRA else "installed-portal"
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


def set_native_options(root):
    def unique(predicate):
        queue = [root]; found = []
        while queue:
            node = queue.pop(); node.clear_cache()
            if predicate(node): found.append(node)
            queue.extend(item for item in children(node) if item)
        return found[0] if len(found) == 1 else None

    def showing(node):
        return node.get_state_set().contains(Atspi.StateType.SHOWING)

    def menu(node):
        return node.get_role() == Atspi.Role.MENU_ITEM and showing(node)

    def open_options():
        button = wait(lambda: unique(lambda node: node.get_role() == Atspi.Role.TOGGLE_BUTTON and node.get_name() == "Показать параметры" and showing(node)))
        assert button.get_action_iface().do_action(0)

    open_options()
    # This GTK menu exposes blank item names; identify its unique checked option by observed state.
    checkbox = wait(lambda: unique(lambda node: menu(node) and node.get_state_set().contains(Atspi.StateType.CHECKED)))
    assert checkbox.get_action_iface().do_action(0)
    open_options()
    submenu = wait(lambda: unique(lambda node: menu(node) and node.get_state_set().contains(Atspi.StateType.EXPANDABLE)))
    assert submenu.get_action_iface().do_action(0)
    choice = wait(lambda: unique(lambda node: menu(node) and node.get_state_set().contains(Atspi.StateType.CHECKABLE) and not node.get_state_set().contains(Atspi.StateType.CHECKED)))
    assert choice.get_action_iface().do_action(0)


env = dict(os.environ, SDL_VIDEODRIVER="wayland", SDL_VIDEO_DRIVER="wayland", ELECTRON2D_TEST_FILE_PORTAL="live-options" if EXTRA else "live")
env.pop("NO_AT_BRIDGE", None)
child = subprocess.Popen(["dotnet", str(ROOT / "tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll")], env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
steps = []
try:
    root = wait(lambda: chooser(TITLE + "accept"))
    if EXTRA:
        set_native_options(root)
        steps.append({"controls": "native menu checkbox and two-item choice", "observed_initial": "unique checked checkbox; first choice checked", "changed": {"Preview": False, "Format": 1}})
    wait(lambda: select_file(root))
    wait(lambda: action(root, "chooser.accept"))
    steps.append({"chooser": "accept", "observed_file": "выбранный #.txt", "action": "chooser.accept"})
    root = wait(lambda: chooser(TITLE + "cancel"))
    wait(lambda: action(root, "window.close"))
    steps.append({"chooser": "cancel", "action": "window.close"})
    output, _ = child.communicate(timeout=8)
    (OUTPUT / (PREFIX + ".log")).write_text(output)
    assert child.returncode == 0, output
    assert "NATIVE_DONE:accept" in output and "NATIVE_DONE:cancel" in output
    (OUTPUT / (PREFIX + ".json")).write_text(json.dumps({"returncode": child.returncode, "steps": steps, "selected_filter_index": 0}, indent=2, ensure_ascii=False) + "\n")
    print("Installed Wayland portal: native " + ("checkbox/choice edits, " if EXTRA else "") + "MIME file acceptance, selected filter and cancellation passed.")
finally:
    if child.poll() is None:
        child.terminate()
        child.wait(timeout=5)
