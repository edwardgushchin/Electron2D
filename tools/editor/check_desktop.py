#!/usr/bin/env python3
"""Exercise Linux editor registration and desktop launching, including Exec escaping."""
import configparser
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile


def launch(command, environment):
    process = subprocess.Popen(command, cwd="/tmp", env=environment, start_new_session=True,
                               stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    try:
        _, output = process.communicate(timeout=4)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGTERM)
        _, output = process.communicate(timeout=10)
    assert 'set_app_id("Electron2D.Editor")' in output, output[-2000:]
    assert 'set_title("Electron2D")' in output, output[-2000:]


def main():
    source = Path(sys.argv[1]).resolve()
    assert source.name == "Electron2D.Editor" and source.is_file()
    with tempfile.TemporaryDirectory(prefix="e2d-desktop-") as temporary:
        directory = Path(temporary) / 'Проверка пробел $`"%\\'
        directory.mkdir()
        for entry in source.parent.iterdir():
            if entry == source:
                target = directory / entry.name
                target.write_bytes(entry.read_bytes())
                target.chmod(entry.stat().st_mode)
            else:
                (directory / entry.name).symlink_to(entry)
        environment = os.environ.copy()
        environment.pop("LD_LIBRARY_PATH", None)
        environment.update(XDG_DATA_HOME=str(Path(temporary) / "data"),
                           SDL_VIDEODRIVER="wayland", WAYLAND_DEBUG="client")
        launch([str(directory / source.name)], environment)
        desktop = Path(environment["XDG_DATA_HOME"]) / "applications/Electron2D.Editor.desktop"
        subprocess.run(["desktop-file-validate", str(desktop)], check=True)
        entry = configparser.ConfigParser(interpolation=None)
        entry.read(desktop)
        assert entry["Desktop Entry"]["Name"] == "Electron2D"
        assert entry["Desktop Entry"]["Icon"] == "Electron2D.Editor"
        icon = Path(environment["XDG_DATA_HOME"]) / "icons/hicolor/scalable/apps/Electron2D.Editor.svg"
        assert icon.read_bytes() == (source.parent / "Assets/mark-dark.svg").read_bytes()
        launch(["gio", "launch", str(desktop)], environment)
    print("Editor apphost, desktop identity, canonical icon and escaped launcher passed.")


if __name__ == "__main__":
    main()
