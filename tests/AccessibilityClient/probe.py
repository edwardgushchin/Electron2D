"""Independent AT-SPI protocol client; never changes desktop accessibility settings."""
import atexit
import re
import subprocess
import time
import threading
import sys


def call(*args, address=None):
    start = ["gdbus", "call", "--address", address] if address else ["gdbus", "call", "--session"]
    result = subprocess.run(start + list(args), capture_output=True, text=True, timeout=4)
    if result.returncode:
        raise RuntimeError(result.stderr)
    return result.stdout.strip()


address = re.search(r"'([^']+)'", call("--dest", "org.a11y.Bus", "--object-path", "/org/a11y/bus", "--method", "org.a11y.Bus.GetAddress"))[1]


def method(bus, path, iface, name, *values):
    return call("--dest", bus, "--object-path", path, "--method", iface + "." + name, *values, address=address)


def refs(text):
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", text)


def children(bus, path):
    return refs(method(bus, path, "org.a11y.atspi.Accessible", "GetChildren"))


def name(bus, path):
    return method(bus, path, "org.freedesktop.DBus.Properties", "Get", "org.a11y.atspi.Accessible", "Name")


deadline = time.monotonic() + 12
app = None
while not app and time.monotonic() < deadline:
    for candidate in children("org.a11y.atspi.Registry", "/org/a11y/atspi/accessible/root"):
        if "Electron2D AT-SPI Foundation Probe" in name(*candidate):
            app = candidate
            break
    if not app:
        time.sleep(.02)
assert app, "The real accessibility registry did not discover the application"
roots = children(*app)
assert len(roots) == 1, roots
nodes = children(*roots[0])
while len(nodes) < 2 and time.monotonic() < deadline:
    time.sleep(.01)
    nodes = children(*roots[0])
assert len(nodes) == 2, nodes
label = next(node for node in nodes if "Waiting for native action" in name(*node))
button = next(node for node in nodes if "Activate probe" in name(*node))
assert "43" in method(*button, "org.a11y.atspi.Accessible", "GetRole")
extents = method(*button, "org.a11y.atspi.Component", "GetExtents", "1")
assert "(8, 48, 120, 32)" in extents, extents
# Invalid semantic requests must return wire errors without poisoning the display owner.
for interface, operation, arguments in [
    ("org.a11y.atspi.Action", "DoAction", ["int32 -1"]),
    ("org.a11y.atspi.Action", "DoAction", ["99"]),
    ("org.a11y.atspi.Component", "GetExtents", ["99"]),
]:
    try:
        method(*button, interface, operation, *arguments)
    except RuntimeError as error:
        assert "InvalidArgs" in str(error), error
    else:
        raise AssertionError("Invalid native request succeeded")
assert "/org/a11y/atspi/null" in method(*roots[0], "org.a11y.atspi.Accessible", "GetChildAtIndex", "99")
if "--automatic" in sys.argv:
    assert "Activates the native probe" in method(*button, "org.freedesktop.DBus.Properties", "Get", "org.a11y.atspi.Accessible", "Description")
# Read ordinary native properties after the invalid requests to prove that the host survived.
assert "Activate probe" in name(*button)
events = []
monitor = subprocess.Popen(["gdbus", "monitor", "--address", address, "--dest", app[0]], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, bufsize=1)

def stop_monitor():
    if monitor.poll() is None:
        monitor.terminate()
        monitor.wait(timeout=3)

atexit.register(stop_monitor)
ready = threading.Event()
def collect():
    for line in monitor.stdout:
        events.append(line)
        if "Monitoring" in line or "owned by" in line:
            ready.set()
thread = threading.Thread(target=collect, daemon=True)
thread.start()
assert ready.wait(3), "Native event monitor did not start"
assert "true" in method(*button, "org.a11y.atspi.Component", "GrabFocus")
if "--automatic" in sys.argv:
    focus_child = children(*button)
    assert len(focus_child) == 1 and "Composite focus target" in name(*focus_child[0]), focus_child
    state = method(*focus_child[0], "org.a11y.atspi.Accessible", "GetState")
    root_state = method(*roots[0], "org.a11y.atspi.Accessible", "GetState")
    active = bool(int(re.search(r"(?:uint32 )?(\d+)", root_state)[1]) & (1 << 1))
    assert bool(int(re.search(r"(?:uint32 )?(\d+)", state)[1]) & (1 << 12)) == active, (state, root_state)
else:
    assert "(116, 218, 120, 32)" in method(*button, "org.a11y.atspi.Component", "GetExtents", "0")
actions = method(*button, "org.a11y.atspi.Action", "GetActions")
assert "click" in actions and "focus" in actions, actions
assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
while "Native action received" not in name(*label) and time.monotonic() < deadline:
    time.sleep(.01)
assert "Native action received" in name(*label)
cache = method(app[0], "/org/a11y/atspi/cache", "org.a11y.atspi.Cache", "GetItems")
assert "Activate probe" in cache and "Native action received" in cache, cache
if "--automatic" not in sys.argv:
    assert "(12, 48, 120, 32)" in method(*button, "org.a11y.atspi.Component", "GetExtents", "1")
    assert "(120, 218, 120, 32)" in method(*button, "org.a11y.atspi.Component", "GetExtents", "0")
    assert "256, 156" in method(*roots[0], "org.a11y.atspi.Component", "GetSize")
    assert refs(method(*roots[0], "org.a11y.atspi.Component", "GetAccessibleAtPoint", "14", "50", "1")) == [button]
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    assert refs(method(*roots[0], "org.a11y.atspi.Component", "GetAccessibleAtPoint", "14", "50", "1")) == [roots[0]]
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    assert "/org/a11y/atspi/null" in method(*roots[0], "org.a11y.atspi.Component", "GetAccessibleAtPoint", "306", "50", "1")
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    assert refs(method(*roots[0], "org.a11y.atspi.Component", "GetAccessibleAtPoint", "306", "50", "1")) == [button]
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    saturated = method(*button, "org.a11y.atspi.Component", "GetExtents", "0")
    assert "(2147483647, 218, 20, 32)" in saturated, saturated
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    subtree = next(node for node in children(*roots[0]) if "Deferred subtree" in name(*node))
    deferred = children(*subtree)[0]
    state = method(*deferred, "org.a11y.atspi.Accessible", "GetState")
    assert int(re.search(r"(?:uint32 )?(\d+)", state)[1]) & (1 << 25), state
    assert any(deferred[1] in line and "StateChanged" in line and "showing', 1" in line for line in events), events
if "--automatic" in sys.argv:
    # Reparenting occurs inside the ordinary Pressed handler, retiring the very object
    # whose native method is currently executing.
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    group = None
    while not group and time.monotonic() < deadline:
        group = next((node for node in children(*roots[0]) if "Moved group" in name(*node)), None)
        if not group:
            time.sleep(.01)
    assert group, "Native topology did not reflect reparenting"
    try:
        name(*button)
    except RuntimeError as error:
        assert "Unknown" in str(error), error
    else:
        raise AssertionError("Retired native object remained callable")
    moved = children(*group)
    current = next(node for node in moved if "Activate probe" in name(*node))
    assert current != button, moved
    region = next(node for node in moved if "Landmark probe" in name(*node))
    assert "110" in method(*region, "org.a11y.atspi.Accessible", "GetRole")
    matrix = next(node for node in moved if "Role matrix" in name(*node))
    expected_roles = [67, 43, 106, 107, 116, 20, 39, 43, 88, 7, 44, 62, 48, 49, 53, 51, 52, 42, 79, 61, 9, 55, 56, 90, 99, 47, 10, 65, 91, 31, 32, 98, 32, 38, 37, 39, 34, 33, 35, 8, 45, 27, 23, 104, 16, 64, 110, 61]
    # IDs and bit positions come from the pinned native role/state enums.
    roles = children(*matrix)
    assert len(roles) == 48, roles
    for index, element in enumerate(roles):
        assert "role:" + str(index) in name(*element)
        role = method(*element, "org.a11y.atspi.Accessible", "GetRole")
        assert int(re.search(r"(?:uint32 )?(\d+)", role)[1]) == expected_roles[index], (index, role)
        bits = [int(item) for item in re.findall(r"(?:uint32 )?(\d+)", method(*element, "org.a11y.atspi.Accessible", "GetState"))]
        state = bits[0] | bits[1] << 32
        assert all(state & (1 << bit) for bit in [3, 4, 16, 18, 33, 40, 43]), (index, bits)
        assert not any(state & (1 << bit) for bit in [8, 24, 25, 30]), (index, bits)
    button = current
    assert "Activate probe" in name(*button)
    assert "true" in method(*button, "org.a11y.atspi.Component", "GrabFocus")
    assert "true" in method(*button, "org.a11y.atspi.Action", "DoAction", "0")
    assert "20" in method(*region, "org.a11y.atspi.Accessible", "GetRole")
    assert "false" in method(*button, "org.a11y.atspi.Action", "DoAction", "0"), "Hidden/disabled control accepted another activation"
    states = [int(item) for item in re.findall(r"(?:uint32 )?(\d+)", method(*button, "org.a11y.atspi.Accessible", "GetState"))]
    assert states and not states[0] & (1 << 8) and not states[0] & (1 << 25), states
while not any("accessible-name" in line and "Native action received" in line for line in events) and time.monotonic() < deadline:
    time.sleep(.01)
monitor.terminate()
monitor.wait(timeout=3)
thread.join(timeout=1)
assert any("accessible-name" in line and "Native action received" in line for line in events), events
assert any("StateChanged" in line and "focused" in line for line in events), events
if "--automatic" in sys.argv:
    assert any("ChildrenChanged" in line and "remove" in line for line in events), events
    assert any("RemoveAccessible" in line for line in events), events
    assert any("ChildrenChanged" in line and "add" in line for line in events), events
print("PASS independent registry, names/description, role, bounds, invalid requests, focus/action, live properties/cache, reentrant topology/retirement, hidden/disabled guard and native events")
