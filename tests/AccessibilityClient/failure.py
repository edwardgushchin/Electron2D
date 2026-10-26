"""Checks native callback failure without importing the provider implementation."""
import re
import subprocess
import time


def run(address, bus, path, method, *args):
    return subprocess.run(["gdbus", "call", "--address", address, "--dest", bus, "--object-path", path, "--method", method, *args], text=True, capture_output=True, timeout=4)


address = re.search(r"'([^']+)'", subprocess.check_output(["gdbus", "call", "--session", "--dest", "org.a11y.Bus", "--object-path", "/org/a11y/bus", "--method", "org.a11y.Bus.GetAddress"], text=True, timeout=4))[1]
refs = lambda text: re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", text)
deadline = time.monotonic() + 10
app = None
while app is None and time.monotonic() < deadline:
    for bus, path in refs(run(address, "org.a11y.atspi.Registry", "/org/a11y/atspi/accessible/root", "org.a11y.atspi.Accessible.GetChildren").stdout):
        if "Electron2D AT-SPI Failure Probe" in run(address, bus, path, "org.freedesktop.DBus.Properties.Get", "org.a11y.atspi.Accessible", "Name").stdout:
            app = (bus, path)
            break
    if app is None:
        time.sleep(.01)
assert app, "Failure probe was not discoverable"
window = refs(run(address, *app, "org.a11y.atspi.Accessible.GetChildren").stdout)[0]
button = refs(run(address, *window, "org.a11y.atspi.Accessible.GetChildren").stdout)[0]
result = run(address, *button, "org.a11y.atspi.Action.DoAction", "0")
assert result.returncode and "org.freedesktop.DBus.Error.Failed" in result.stderr and "Probe callback failure" in result.stderr, result
print("PASS independent native callback failure reply")
