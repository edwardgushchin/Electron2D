#!/usr/bin/python3
"""Run real Wayland clients against an isolated FileChooser D-Bus service."""
import json
import os
from pathlib import Path
import resource
import signal
import subprocess
import sys
import tempfile
import threading
import time

resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
ROOT = Path(__file__).resolve().parents[2]
DLL = ROOT / "tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll"
OUTPUT = Path(os.environ.get("ELECTRON2D_PORTAL_TEST_OUTPUT", ROOT / "bin/portal-file-chooser-tests"))
OUTPUT.mkdir(parents=True, exist_ok=True)

if "--in-session" not in sys.argv:
    with tempfile.TemporaryDirectory(prefix="e2d-file-portal-") as temporary:
        config = Path(temporary) / "bus.conf"
        config.write_text('<busconfig><type>session</type><listen>unix:tmpdir=' + temporary + '</listen><policy context="default"><allow own="*"/><allow send_destination="*"/><allow receive_sender="*"/></policy></busconfig>')
        daemon = subprocess.Popen(["dbus-daemon", "--nofork", "--print-address=1", "--config-file=" + str(config)], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        try:
            address = daemon.stdout.readline().strip()
            assert address
            result = subprocess.run([sys.executable, __file__, "--in-session"], env=dict(os.environ, DBUS_SESSION_BUS_ADDRESS=address, ELECTRON2D_TEST_PRIVATE_BUS_PID=str(daemon.pid)), timeout=100)
            raise SystemExit(result.returncode)
        finally:
            daemon.terminate()
            daemon.wait(timeout=5)

import dbus
import dbus.bus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib

dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
PORTAL = "org.freedesktop.portal.Desktop"
PATH = "/org/freedesktop/portal/desktop"
FILE = "org.freedesktop.portal.FileChooser"
REQUEST = "org.freedesktop.portal.Request"
folder = OUTPUT / "выбор с пробелом"
folder.mkdir(exist_ok=True)
(folder / "picked-dir").mkdir(exist_ok=True)
(folder / "выбранный #.txt").write_text("selected\n")
(folder / "другой.png").write_bytes(b"fixture")
observed = []
requests = []
service_bus = None
version = 4


class Request(dbus.service.Object):
    def __init__(self, bus, path):
        super().__init__(bus, path)
        self.path = path

    @dbus.service.signal(REQUEST, signature="ua{sv}")
    def Response(self, status, values):
        pass

    @dbus.service.method(REQUEST, in_signature="", out_signature="")
    def Close(self):
        self.remove_from_connection()


class Portal(dbus.service.Object):
    def __init__(self, bus):
        super().__init__(bus, PATH)

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="ss", out_signature="v")
    def Get(self, interface, property_name):
        assert property_name == "version"
        return dbus.UInt32(version if interface == FILE else 1, variant_level=1)

    @dbus.service.method("org.freedesktop.portal.Settings", in_signature="ss", out_signature="v")
    def Read(self, namespace, key):
        return dbus.UInt32(0, variant_level=2)

    @dbus.service.method(FILE, in_signature="ssa{sv}", out_signature="o", sender_keyword="sender")
    def OpenFile(self, parent, title, options, sender):
        return self.choose("OpenFile", parent, title, options, sender)

    @dbus.service.method(FILE, in_signature="ssa{sv}", out_signature="o", sender_keyword="sender")
    def SaveFile(self, parent, title, options, sender):
        return self.choose("SaveFile", parent, title, options, sender)

    def choose(self, method, parent, title, options, sender):
        title = str(title)
        if title == "launch-error":
            raise dbus.exceptions.DBusException("Fixture launch refused", name="org.freedesktop.portal.Error.NotAllowed")
        assert not parent or str(parent).startswith("wayland:")
        assert bool(options["modal"])
        assert bytes(options["current_folder"])[-1:] == b"\0"
        assert Path(os.fsdecode(bytes(options["current_folder"])[:-1])).resolve() == folder.resolve()
        assert method == ("SaveFile" if title == "save" else "OpenFile")
        assert bool(options.get("multiple", False)) == (title == "multi")
        assert bool(options.get("directory", False)) == (title == "directory")
        if title == "save":
            assert options["current_name"] == "новый #.txt"
        filters = options.get("filters", [])
        if title == "directory":
            assert not filters
        elif title in ("scene", "scene-cancel"):
            assert len(filters) == 4
            assert filters[0][0] == "All Recognized"
            assert list(filters[0][1]) == [(1, "image/png"), (1, "text/plain")]
        else:
            assert list(filters[0][1]) == [(1, "image/png"), (1, "image/jpeg")]
            assert list(filters[1][1]) == [(0, "*.txt"), (1, "text/plain")]
        choices = options.get("choices", [])
        if title.startswith("options-") and title != "options-empty":
            assert list(choices) == [("option_0", "Preview", [], "true"), ("option_1", "Format", [("0", "Text"), ("1", "Image")], "0"), ("option_2", "Format", [("0", "First"), ("1", "Last")], "1")]
        elif title in ("scene", "scene-cancel"):
            assert list(choices) == [("option_0", "Changed" if title == "scene-cancel" else "Preview", [], "true"), ("option_1", "Format", [("0", "Text"), ("1", "Image")], "0")]
        else:
            assert not choices
        observed.append({"title": title, "method": method, "parent": str(parent), "filters": str(filters), "choices": str(choices)})
        path = PATH + "/request/" + str(sender)[1:].replace(".", "_") + "/" + str(options["handle_token"])
        if title == "alternate":
            path += "_alternate"
        request = Request(service_bus, path)
        requests.append(request)
        selected = folder / ("picked-dir" if title == "directory" else "выбранный #.txt")
        uris = [selected.as_uri()]
        if title == "multi":
            uris.append((folder / "другой.png").as_uri())
        values = {"uris": dbus.Array(uris, signature="s")}
        if filters and title != "missing-filter":
            values["current_filter"] = filters[2 if title in ("scene", "scene-cancel") else 1]
        if title == "unknown-filter":
            values["current_filter"] = dbus.Struct(("Unconditional", dbus.Array([dbus.Struct((dbus.UInt32(1), "text/x-unknown"), signature="us")], signature="(us)")), signature="sa(us)")
        if choices:
            pairs = [("option_" + str(i), "false" if not choice[2] else "0" if i == 2 else "1") for i, choice in enumerate(choices)]
            if title == "options-checked": pairs[0] = ("option_0", "true")
            pairs.reverse()
            pairs.append(("x-provider-read-only", "true"))
            if title == "options-omitted": pairs.pop(1)
            if title == "options-duplicate": pairs.append(pairs[0])
            if title == "options-bool": pairs = [(key, "1" if key == "option_0" else val) for key, val in pairs]
            if title == "options-range": pairs[0] = ("option_2", "2")
            if title == "options-index": pairs[0] = ("option_2", "00")
            if title == "options-pair":
                values["choices"] = dbus.Array([dbus.Struct(("option_0", "false", "extra"), signature="sss")], signature="(sss)")
            elif title == "options-type": values["choices"] = dbus.UInt32(1)
            elif title != "options-missing": values["choices"] = dbus.Array([dbus.Struct(pair, signature="ss") for pair in pairs], signature="(ss)")
        if title in ("bad-uri", "options-bad-uri"):
            values["uris"] = dbus.Array(["https://example.invalid/file"], signature="s")
        if title == "bad-nul":
            values["uris"] = dbus.Array(["file:///tmp/%00invalid"], signature="s")
        if title == "bad-escape":
            values["uris"] = dbus.Array(["file:///tmp/%ZZinvalid"], signature="s")
        if title == "wrong-count":
            values["uris"] = dbus.Array([selected.as_uri(), selected.as_uri()], signature="s")
        if title == "wrong-type":
            values["uris"] = dbus.UInt32(8)
        if title == "empty":
            values["uris"] = dbus.Array([], signature="s")
        if title == "wrong-path":
            other = Request(service_bus, path + "_wrong")
            requests.append(other)
            other.Response(0, {"uris": dbus.Array(["file:///wrong-path"], signature="s")})
        if title == "wrong-sender":
            wrong_bus = dbus.bus.BusConnection(os.environ["DBUS_SESSION_BUS_ADDRESS"])
            wrong = Request(wrong_bus, path)
            wrong.Response(0, values)
            wrong_bus.flush()
            wrong_bus.close()
        if title == "bus-disconnect":
            GLib.timeout_add(20, lambda: os.kill(int(os.environ["ELECTRON2D_TEST_PRIVATE_BUS_PID"]), signal.SIGTERM))
        elif title == "disconnect":
            GLib.timeout_add(20, lambda: service_bus.close())
        else:
            status = 1 if title in ("cancel", "wrong-sender", "options-cancel", "scene-cancel") else 2 if title == "error" else 0
            if title in ("early", "alternate"):
                request.Response(status, values)
            else:
                GLib.timeout_add(20, lambda: (request.Response(status, values), False)[1])
        return dbus.ObjectPath(path)


loop = GLib.MainLoop()
thread = threading.Thread(target=loop.run, daemon=True)
thread.start()
results = []
try:
    # No activation directories: this really is an absent service, not a mock result.
    for profile, service_version in [("contract", 0), ("contract", 2), ("gpu", 4), ("compatibility", 4), ("contract", 4), ("bus-loss", 4)]:
        version = service_version
        if service_version:
            service_bus = dbus.bus.BusConnection(os.environ["DBUS_SESSION_BUS_ADDRESS"])
            assert service_bus.request_name(PORTAL, dbus.bus.NAME_FLAG_DO_NOT_QUEUE) == dbus.bus.REQUEST_NAME_REPLY_PRIMARY_OWNER
            portal = Portal(service_bus)
        env = dict(os.environ, SDL_VIDEODRIVER="wayland", SDL_VIDEO_DRIVER="wayland", SDL_GPU_DRIVER="vulkan", NO_AT_BRIDGE="1", DOTNET_TieredCompilation="0", ELECTRON2D_TEST_FILE_PORTAL=profile, ELECTRON2D_PORTAL_VERSION=str(service_version), ELECTRON2D_PORTAL_FOLDER=str(folder))
        result = subprocess.run(["dotnet", str(DLL)], env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=25)
        label = profile + "-v" + str(service_version)
        (OUTPUT / (label + ".log")).write_text(result.stdout)
        results.append({"profile": label, "returncode": result.returncode, "libdecor_disabled": False})
        print(label, result.returncode, flush=True)
        if result.returncode:
            print(result.stdout[-4000:])
            raise RuntimeError(label + " failed")
        if service_version:
            portal.remove_from_connection()
            if service_bus.get_is_connected():
                service_bus.release_name(PORTAL)
                service_bus.close()
            time.sleep(.05)
finally:
    loop.quit()
    (OUTPUT / "protocol-results.json").write_text(json.dumps(results, indent=2) + "\n")
    (OUTPUT / "protocol-observations.json").write_text(json.dumps(observed, indent=2, ensure_ascii=False) + "\n")
