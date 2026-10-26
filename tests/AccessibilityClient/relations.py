"""Independent AT-SPI semantic graph, validation, active descendant and anchor probe."""
import atexit
import re
import subprocess
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
P = 'org.freedesktop.DBus.Properties'
ACT = 'org.a11y.atspi.Action'
V = 'org.a11y.atspi.Value'
H = 'org.a11y.atspi.Hyperlink'


def method(node, iface, name, *values):
    return call('--dest', node[0], '--object-path', node[1], '--method', iface + '.' + name, *values, address=address)


def refs(text):
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", text)


def children(node):
    return refs(method(node, A, 'GetChildren'))


def name(node):
    return re.search(r"<'(.*)'>", method(node, P, 'Get', A, 'Name'))[1]


def wait_for(check):
    deadline = time.monotonic() + 7
    while time.monotonic() < deadline:
        result = check()
        if result:
            return result
        time.sleep(.01)
    raise AssertionError('Semantic graph did not converge')


def app():
    for node in children(('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root')):
        if name(node) == 'Electron2D Semantic Relations Probe':
            return node


root = wait_for(lambda: (roots[0] if (roots := children(wait_for(app))) else None))

def find(title):
    for node in children(root):
        if name(node) == title:
            return node


def relation_list(node, kind):
    text = method(node, A, 'GetRelationSet')
    # Read protocol tuples, not engine identifiers.
    for number, targets in re.findall(r'\((?:uint32 )?(\d+),\s*(\[.*?\])\)', text):
        if int(number) == kind:
            return refs(targets)
    return []


def relation(node, kind):
    return set(relation_list(node, kind))


def action(node, label):
    names = re.findall(r"\('([^']+)',", method(node, ACT, 'GetActions'))
    assert label in names, (label, names)
    assert 'true' in method(node, ACT, 'DoAction', str(names.index(label)))


def state(node, bit):
    numbers = re.findall(r'\d+', method(node, A, 'GetState').replace('uint32', ''))
    return bool(int(numbers[bit // 32]) & (1 << (bit % 32)))


wait_for(lambda: find('Manual radio B'))
slider = next(node for node in children(root) if 'uint32 51' in method(node, A, 'GetRole'))
caption, alternate, help_text, meter, next_button, chooser, form, stereo, mono, link, manual_a, manual_b = [find(t) for t in ['Gain', 'Alternative gain', 'Choose the audio gain', 'Gain meter', 'Next phase', 'Chooser', 'Form group', 'Stereo', 'Mono', 'Internal option link', 'Manual radio A', 'Manual radio B']]
assert not state(wait_for(lambda: find('Detached survivor')), 12)
assert all([caption, alternate, help_text, meter, next_button, chooser, form, stereo, mono, link, manual_a, manual_b])
unnamed = next(node for node in children(root) if name(node) == '' and 'uint32 43' in method(node, A, 'GetRole'))
wait_for(lambda: relation(slider, 2) == {caption} and relation(unnamed, 2) == {caption})
assert relation(caption, 1) == {slider, unnamed}
assert relation(slider, 18) == {help_text} and relation(help_text, 17) == {slider, manual_a}
assert relation(slider, 19) == {help_text} and relation(help_text, 20) == {slider}
assert relation(slider, 3) == {meter} and relation(meter, 4) == {slider, manual_a}
assert relation(slider, 10) == {next_button} and relation(next_button, 11) == {slider}
assert relation(slider, 5) == {form}
assert relation(stereo, 5) == {mono} and relation(mono, 5) == {stereo}
assert relation(manual_a, 5) == {manual_b} and relation(manual_b, 5) == {manual_a}
assert relation(manual_a, 3) == {meter} and relation(manual_a, 18) == {help_text}
assert relation(manual_a, 10) == {manual_b} and relation(manual_b, 11) == {manual_a}
assert relation(manual_b, 10) == {manual_a} and relation(manual_a, 11) == {manual_b}
options = {name(n): n for n in children(chooser)}
assert H in method(link, A, 'GetInterfaces')
assert options['Option B'] in refs(method(link, H, 'GetObject', '0'))
assert "''" in method(link, H, 'GetURI', '0') and '-1' in method(link, P, 'Get', H, 'StartIndex')
try:
    method(link, H, 'GetObject', '1')
except RuntimeError as error:
    assert 'InvalidArgs' in str(error)
else:
    raise AssertionError('Invalid anchor index succeeded')
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
method(slider, P, 'Set', V, 'CurrentValue', '<95.0>')
error_node = wait_for(lambda: find('Gain must not exceed 80'))
wait_for(lambda: relation(slider, 21) == {error_node})
assert relation(error_node, 22) == {slider}, method(error_node, A, 'GetRelationSet')
assert state(slider, 36), method(slider, A, 'GetState')
method(slider, P, 'Set', V, 'CurrentValue', '<40.0>')
wait_for(lambda: not relation(slider, 21) and not state(slider, 36))
action(mono, 'click'); wait_for(lambda: state(mono, 4) and not state(stereo, 4))
action(chooser, 'focus')
wait_for(lambda: state(options['Option A'], 12) and not state(chooser, 12))
action(next_button, 'click'); wait_for(lambda: relation(slider, 2) == {caption, alternate})
action(next_button, 'click'); wait_for(lambda: not relation(slider, 2) and not relation(unnamed, 2))
action(next_button, 'click'); restored = wait_for(lambda: find('Restored gain'))
wait_for(lambda: relation(slider, 2) == {alternate} and relation(unnamed, 2) == {restored, alternate})
assert restored != caption and relation(restored, 1) == {unnamed}
assert relation_list(unnamed, 2) == [restored, alternate]
order_start = len(events)
action(next_button, 'click'); wait_for(lambda: any('ActiveDescendantChanged' in event and options['Option B'][1] in event for event in events))
wait_for(lambda: state(chooser, 31) and state(options['Option B'], 12) and not state(options['Option A'], 12))
wait_for(lambda: relation_list(unnamed, 2) == [alternate, restored])
wait_for(lambda: any('accessible-relation-set' in event and unnamed[1] in event for event in events[order_start:]))
action(next_button, 'click'); wait_for(lambda: not state(chooser, 31) and state(chooser, 12) and not state(options['Option B'], 12))
assert options['Option B'] in refs(method(link, H, 'GetObject', '0'))
action(next_button, 'click'); wait_for(lambda: 'false' in method(link, H, 'IsValid'))
assert H not in method(link, A, 'GetInterfaces') and re.search(r'<(?:int32 )?0>', method(link, P, 'Get', H, 'NAnchors'))
action(next_button, 'click'); wait_for(lambda: not relation(stereo, 5) and not relation(mono, 5))
action(next_button, 'click'); wait_for(lambda: relation(stereo, 5) == {mono} and relation(mono, 5) == {stereo})
action(next_button, 'click'); wait_for(lambda: not relation(stereo, 5) and not relation(mono, 5) and not relation(slider, 5))
action(next_button, 'click'); wait_for(lambda: relation(stereo, 5) == {mono} and relation(mono, 5) == {stereo} and relation(slider, 5) == {form})
assert any('accessible-relation-set' in event for event in events)
assert any('invalid-entry' in event for event in events)
print('PASS independent AT-SPI relations, reciprocals, manual/source provenance, path repair, validation, radio groups, active descendants and native hyperlink anchors')
