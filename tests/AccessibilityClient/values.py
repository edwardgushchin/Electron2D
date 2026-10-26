"""Independent native Value client for automatic and custom numeric producers."""
import atexit
import re
import subprocess
import sys
import threading
import time


def call(*args, address=None):
    start = ['gdbus', 'call', '--address', address] if address else ['gdbus', 'call', '--session']
    result = subprocess.run(start + list(args), capture_output=True, text=True, timeout=4)
    if result.returncode:
        raise RuntimeError(result.stderr)
    return result.stdout.strip()


address = re.search(r"'([^']+)'", call('--dest', 'org.a11y.Bus', '--object-path', '/org/a11y/bus', '--method', 'org.a11y.Bus.GetAddress'))[1]
A = 'org.a11y.atspi.Accessible'
V = 'org.a11y.atspi.Value'
P = 'org.freedesktop.DBus.Properties'
ACT = 'org.a11y.atspi.Action'


def method(node, iface, name, *values):
    return call('--dest', node[0], '--object-path', node[1], '--method', iface + '.' + name, *values, address=address)


def children(node):
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", method(node, A, 'GetChildren'))


def get(node, prop):
    return method(node, P, 'Get', V, prop)


def value(node):
    return float(re.search(r'<([^>]+)>', get(node, 'CurrentValue'))[1])


def wait_for(check):
    deadline = time.monotonic() + 6
    while time.monotonic() < deadline:
        result = check()
        if result:
            return result
        time.sleep(.01)
    raise AssertionError('Native state did not converge')


def app():
    for node in children(('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root')):
        if ('Electron2D Numeric Failure Probe' if '--failure' in sys.argv else 'Electron2D Numeric Accessibility Probe') in method(node, P, 'Get', A, 'Name'):
            return node


root = children(wait_for(app))[0]

def nodes():
    return {re.search(r"<'(.*)'>", method(n, P, 'Get', A, 'Name'))[1]: n for n in children(root)}


if '--failure' in sys.argv:
    failure = wait_for(lambda: nodes().get('Failing numeric'))
    try:
        method(failure, P, 'Set', V, 'CurrentValue', '<7.0>')
    except RuntimeError as error:
        assert 'Numeric callback failure' in str(error), error
    else:
        raise AssertionError('Numeric callback failure was swallowed')
    print('PASS numeric callback wire failure')
    sys.exit(0)

items = wait_for(lambda: (n if len(n := nodes()) >= 10 else None))
slider = items['Numeric slider']
peer = items['Shared slider']
spin = items['Numeric spin']
custom = items['Custom numeric']


def actions(node):
    return re.findall(r"\('([^']+)',", method(node, ACT, 'GetActions'))


def action(node, name):
    names = actions(node)
    assert name in names, names
    assert 'true' in method(node, ACT, 'DoAction', str(names.index(name)))


def write(node, requested):
    return method(node, P, 'Set', V, 'CurrentValue', '<' + str(requested) + '>')


def rejected(node, requested):
    try:
        write(node, requested)
    except RuntimeError as error:
        assert 'InvalidArgs' in str(error), error
    else:
        raise AssertionError('Invalid native value write succeeded')


for title, role in [('Numeric slider', 51), ('Shared slider', 51), ('Numeric spin', 52), ('Horizontal scroll', 48), ('Vertical scroll', 48), ('Progress', 42), ('Texture progress', 42)]:
    node = items[title]
    assert V in method(node, A, 'GetInterfaces'), title
    assert f'uint32 {role}' in method(node, A, 'GetRole'), (title, method(node, A, 'GetRole'))
assert '<-10.0>' in get(slider, 'MinimumValue')
assert '<100.0>' in get(slider, 'MaximumValue')
assert '<0.5>' in get(slider, 'MinimumIncrement')
assert '$' in get(spin, 'Text') and 'kg' in get(spin, 'Text')
assert 'nan' in get(custom, 'CurrentValue')
assert 'increment' not in actions(items['Progress'])
rejected(items['Progress'], 3.0)
rejected(items['Texture progress'], 3.0)
rejected(slider, 'nan')
rejected(slider, 'inf')
monitor = subprocess.Popen(['gdbus', 'monitor', '--address', address, '--dest', slider[0]], stdout=subprocess.PIPE, text=True)
atexit.register(lambda: monitor.terminate() if monitor.poll() is None else None)
events = []
ready = threading.Event()
def collect():
    for line in monitor.stdout:
        events.append(line)
        if 'Monitoring' in line or 'owned by' in line:
            ready.set()
threading.Thread(target=collect, daemon=True).start()
assert ready.wait(3)
write(slider, 21.3)
wait_for(lambda: value(slider) == 21.5 and value(peer) == 21.5 and value(items['Progress']) == 21.5)
action(slider, 'increment'); wait_for(lambda: value(slider) == 22)
action(slider, 'decrement'); wait_for(lambda: value(slider) == 21.5)
action(slider, 'page-increment'); wait_for(lambda: value(slider) == 31.5)
action(slider, 'page-decrement'); wait_for(lambda: value(slider) == 21.5)
write(slider, 999.0); wait_for(lambda: value(slider) == 90)
write(slider, 44.0); wait_for(lambda: value(slider) == 42 and value(peer) == 42)
# Semantic steps intentionally use Range.Step, independently of pointer-specific overrides.
action(items['Horizontal scroll'], 'increment'); wait_for(lambda: value(items['Horizontal scroll']) == 11)
action(items['Horizontal scroll'], 'increment'); wait_for(lambda: value(items['Horizontal scroll']) == 12)
action(spin, 'increment'); wait_for(lambda: value(spin) == 3.25)
action(spin, 'focus')
write(custom, 4.0); wait_for(lambda: value(custom) == 4)
action(custom, 'page-increment'); wait_for(lambda: value(custom) == 12)
action(custom, 'page-decrement'); wait_for(lambda: value(custom) == 4)
write(custom, 100.0); wait_for(lambda: value(custom) == 50)
action(items['Policy'], 'click')
wait_for(lambda: 'increment' not in actions(slider))
rejected(slider, 10.0); rejected(spin, 10.0)
action(items['Policy'], 'click'); wait_for(lambda: 'increment' in actions(slider))
action(items['Policy'], 'click'); rejected(slider, 10.0)
action(items['Policy'], 'click'); wait_for(lambda: value(slider) == 32 and value(peer) == 32)
retired = items['Retiring slider']
action(items['Policy'], 'click')
wait_for(lambda: 'Retiring slider' not in nodes())
try:
    write(retired, 3.0)
except RuntimeError:
    pass
else:
    raise AssertionError('Retired numeric identity accepted mutation')
action(items['Policy'], 'click'); rejected(custom, 1.0)
action(items['Policy'], 'click'); rejected(custom, 1.0)
assert 'page-increment' not in actions(custom)
action(items['Policy'], 'click')
assert 'page-increment' not in actions(custom) and '<0.0>' in get(custom, 'MinimumIncrement')
write(custom, 2.0); wait_for(lambda: value(custom) == 2)
assert any('accessible-value' in event for event in events), events
assert any('PropertiesChanged' in event and 'CurrentValue' in event for event in events), events
print('PASS independent AT-SPI numeric values, page/actions, read-only, rounding, sharing, reentry, retirement and value events')
