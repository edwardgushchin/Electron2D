#!/usr/bin/python3
"""Drive only the uniquely titled real GTK input dialogs through AT-SPI."""
import json
import os
from pathlib import Path
import resource
import subprocess
import threading
import time

import gi
gi.require_version("Atspi", "2.0")
from gi.repository import Atspi, GLib

resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = Path(os.environ["ELECTRON2D_INPUT_TEST_OUTPUT"])
OUTPUT.mkdir(parents=True, exist_ok=True)
Atspi.init()
context = GLib.MainContext.default()


def children(node):
    return [node.get_child_at_index(i) for i in range(node.get_child_count())]


def wait(check):
    deadline = time.monotonic() + 15
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
    raise AssertionError("Native text input state did not converge")


def dialog(title):
    for app in children(Atspi.get_desktop(0)):
        if app:
            for window in children(app):
                if window:
                    window.clear_cache()
                    if window.get_name() == title and window.get_role() == Atspi.Role.DIALOG:
                        return window


def find(root, predicate):
    queue = [root]
    while queue:
        node = queue.pop(); node.clear_cache()
        if predicate(node):
            return node
        queue.extend(n for n in children(node) if n)


def click(widget):
    assert widget.get_action_iface().do_action(0)


def edit_and_respond(name, text, accepted=True, close=False, initial="Исходный 🌙 e\u0301"):
    root = wait(lambda: dialog("Electron2D input " + name))
    description = "Engine input" if name == "engine" else "Nested input" if name == "nested" else "Dispose in callback" if name == "disposal" else "Введите текст / Input <plain> & text"
    wait(lambda: find(root, lambda n: n.get_role() == Atspi.Role.LABEL and n.get_name() == description))
    field = wait(lambda: find(root, lambda n: n.get_role() == Atspi.Role.TEXT and n.get_editable_text_iface() is not None))
    assert Atspi.Text.get_text(field, 0, -1) == initial
    assert field.get_editable_text_iface().set_text_contents(text)
    assert Atspi.Text.get_text(field, 0, -1) == text
    assert root.get_state_set().contains(Atspi.StateType.MODAL)
    if name not in ("disposal", "engine"):
        wait(lambda: lines.count("INPUT_GUARDS_OK") >= expected_guards[0])
        expected_guards[0] += 1
    if close:
        child.stdin.write("close\n"); child.stdin.flush()
    else:
        # GTK translates the standard native captions using the host catalog.
        labels = ("OK", "ОК") if accepted else ("Cancel", "Отмена")
        button = wait(lambda: find(root, lambda n: n.get_role() == Atspi.Role.PUSH_BUTTON and n.get_name() in labels))
        click(button)
    steps.append({"dialog": name, "initial": initial, "edited": text, "accepted": accepted, "window_close": close})


profile = os.environ.get("ELECTRON2D_INPUT_TEST_PROFILE", "native")
env = dict(os.environ, ELECTRON2D_TEST_NATIVE_TEXT_INPUT=profile)
env.pop("NO_AT_BRIDGE", None)
child = subprocess.Popen(["dotnet", str(ROOT / "tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll")], env=env, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
lines = []
reader = threading.Thread(target=lambda: [lines.append(line.rstrip()) for line in child.stdout], daemon=True)
reader.start()
steps = []
expected_guards = [1]
try:
    names = ("engine",) if profile in ("gpu", "compatibility") else ("accept", "empty", "cancel", "close", "callback-error", "outer", "nested", "disposal")
    for name in names:
        text = "Engine edited" if name == "engine" else "" if name == "empty" else "Nested edited" if name == "nested" else "Disposed safely" if name == "disposal" else "Новый 🌌 e\u0301"
        initial = "Engine" if name == "engine" else "Nested" if name == "nested" else "Dispose" if name == "disposal" else "Исходный 🌙 e\u0301"
        edit_and_respond(name, text, accepted=name not in ("cancel", "close"), close=name == "close", initial=initial)
    child.wait(timeout=10); reader.join(timeout=2)
    assert child.returncode == 0, "\n".join(lines)
    assert ("Native text input engine " + profile + " passed." if profile in ("gpu", "compatibility") else "Native text input contract passed.") in lines
    (OUTPUT / "native-input.json").write_text(json.dumps({"returncode": child.returncode, "steps": steps, "guard_checks": lines.count("INPUT_GUARDS_OK")}, indent=2, ensure_ascii=False) + "\n")
    print("Native GTK input " + profile + ": " + str(len(steps)) + " native dialogs passed.")
except Exception:
    observed = []
    for name in names:
        root = dialog("Electron2D input " + name)
        if not root:
            continue
        queue = [root]
        while queue and len(observed) < 80:
            node = queue.pop(); node.clear_cache()
            observed.append({"name": node.get_name(), "role": node.get_role_name()})
            queue.extend(n for n in children(node) if n)
    (OUTPUT / "failed-native-tree.json").write_text(json.dumps(observed, ensure_ascii=False, indent=2) + "\n")
    raise
finally:
    if child.poll() is None:
        child.terminate(); child.wait(timeout=5)
    reader.join(timeout=2)
    (OUTPUT / "native-input.log").write_text("\n".join(lines) + "\n")
