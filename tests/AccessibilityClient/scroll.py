"""Independent AT-SPI scrolling, geometry, typed payload and source probe."""
import atexit
import re
import subprocess
import threading
import time
import sys


def call(*args, address=None):
    start = ['gdbus', 'call', '--address', address] if address else ['gdbus', 'call', '--session']
    result = subprocess.run(start + list(args), capture_output=True, text=True, timeout=5)
    if result.returncode:
        raise RuntimeError(result.stderr)
    return result.stdout.strip()


address = re.search(r"'([^']+)'", call('--dest', 'org.a11y.Bus', '--object-path', '/org/a11y/bus', '--method', 'org.a11y.Bus.GetAddress'))[1]
A = 'org.a11y.atspi.Accessible'
P = 'org.freedesktop.DBus.Properties'
ACT = 'org.a11y.atspi.Action'
C = 'org.a11y.atspi.Component'


def method(node, iface, name, *values):
    return call('--dest', node[0], '--object-path', node[1], '--method', iface + '.' + name, *map(str, values), address=address)


def children(node):
    return re.findall(r"\('([^']+)',\s*(?:objectpath\s+)?'([^']+)'\)", method(node, A, 'GetChildren'))


def name(node):
    return re.search(r"<'(.*)'>", method(node, P, 'Get', A, 'Name'))[1]


def wait_for(check):
    deadline = time.monotonic() + 9
    while time.monotonic() < deadline:
        result = check()
        if result:
            return result
        time.sleep(.01)
    raise AssertionError('Native scroll state did not converge')


def app():
    for node in children(('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root')):
        if name(node) == 'Electron2D Native Scroll Probe':
            return node


root = wait_for(lambda: (roots[0] if (roots := children(wait_for(app))) else None))


def find(title, parent=root):
    for node in children(parent):
        if name(node) == title:
            return node


def deep(title, parent):
    direct = find(title, parent)
    if direct:
        return direct
    for node in children(parent):
        result = deep(title, node)
        if result:
            return result


def names(node):
    return re.findall(r"\('([^']+)',", method(node, ACT, 'GetActions'))


def action(node, label):
    options = names(node)
    assert label in options, (label, options)
    assert 'true' in method(node, ACT, 'DoAction', options.index(label))


def good(node, iface, command, *values):
    result = method(node, iface, command, *values)
    assert 'true' in result, (command, result)


def bad(node, iface, command, *values):
    try:
        method(node, iface, command, *values)
    except RuntimeError as error:
        assert 'InvalidArgs' in str(error), str(error)
        return
    raise AssertionError('Invalid native request was accepted')


def rect(node, kind=1):
    return tuple(map(int, re.findall(r'-?\d+', method(node, C, 'GetExtents', kind))))


def state(node, bit):
    values = re.findall(r'\d+', method(node, A, 'GetState').replace('uint32', ''))
    return bool(int(values[bit // 32]) & (1 << (bit % 32)))


def attrs(node):
    return {key: float(value) for key, value in re.findall(r"'(scroll-[^']+)': '([^']+)'", method(node, A, 'GetAttributes'))}


if '--failure' in sys.argv:
    producer = wait_for(lambda: find('Throwing scroll'))
    try:
        action(producer, 'scroll-to-end')
    except RuntimeError as error:
        assert 'Failed' in str(error)
        print('PASS native scroll callback failure wire reply')
        sys.exit(0)
    raise AssertionError('Scroll callback did not fail')


monitor = subprocess.Popen(['gdbus', 'monitor', '--address', address, '--dest', root[0]], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
atexit.register(lambda: monitor.poll() is None and monitor.terminate())
events = []
threading.Thread(target=lambda: events.extend(monitor.stdout), daemon=True).start()
time.sleep(.08)
manual = wait_for(lambda: find('Manual'))
child = wait_for(lambda: find('Manual child', manual))
assert state(child, 30) and not state(child, 25), 'Clipped descendants remain visible but are not showing'
expected = {'scroll-up', 'scroll-page-up', 'scroll-down', 'scroll-page-down', 'scroll-left', 'scroll-page-left', 'scroll-right', 'scroll-page-right', 'scroll-backward', 'scroll-forward', 'scroll-to-start', 'scroll-to-end'}
assert set(names(manual)) == expected
before = attrs(manual)
assert before['scroll-x'] == -7, 'Published offsets are not silently clamped to the declared range'
for option in sorted(expected - {'scroll-to-start', 'scroll-to-end'}):
    action(manual, option)
assert attrs(manual) == before, 'A request must not fabricate the producer state'
action(manual, 'scroll-to-end')
wait_for(lambda: attrs(manual).get('scroll-x') == 600 and attrs(manual).get('scroll-y') == 700)
action(manual, 'scroll-to-start')
wait_for(lambda: attrs(manual).get('scroll-x') == 0 and attrs(manual).get('scroll-y') == 0)
for hint in range(7):
    good(child, C, 'ScrollTo', hint)
    wait_for(lambda: state(child, 25))
    cx, cy, cw, ch = rect(child)
    px, py, pw, ph = rect(manual)
    assert cx >= px and cy >= py and cx + cw <= px + pw and cy + ch <= py + ph, (hint, rect(child), rect(manual))
extreme = find('Extreme')
action(extreme, 'scroll-to-end')
wait_for(lambda: attrs(extreme).get('scroll-x', 0) > 3e38)
action(extreme, 'scroll-to-start')
wait_for(lambda: attrs(extreme).get('scroll-x', 0) < -3e38)
assert 'false' in method(find('Singular child', find('Singular')), C, 'ScrollTo', 0)
bad(child, C, 'ScrollTo', 7)
bad(manual, ACT, 'DoAction', 100000)
hint_target = find('Hint')
for hint in range(7):
    good(hint_target, C, 'ScrollTo', hint)
point = find('Point', find('Point group'))
good(point, C, 'ScrollToPoint', 1, 123, 234)
good(point, C, 'ScrollToPoint', 2, 17, 18)
bad(point, C, 'ScrollToPoint', 3, 0, 0)
if rect(root, 0)[0] == -2147483648:
    assert 'false' in method(point, C, 'ScrollToPoint', 0, 0, 0), 'An unknown screen origin must not become anywhere scrolling'

group_child = find('Group child', find('Grouping'))
assert group_child[1] in method(root, C, 'GetAccessibleAtPoint', 160, 660, 1), 'Geometry-free grouping nodes must not hide their children from hit testing'
outer = find('Outer')
top = deep('Top level', outer)
assert state(top, 25), 'Independent canvas roots do not inherit a content clip'
before_top = attrs(outer)
assert 'false' in method(top, C, 'ScrollToPoint', 1, 40, 40)
assert attrs(outer) == before_top
for node in children(outer):
    if '48' in method(node, A, 'GetRole'):
        assert state(node, 25), 'Visible internal scroll bars must not be clipped by their content viewport'
        before_bar = attrs(outer)
        method(node, C, 'ScrollTo', 0)
        assert attrs(outer) == before_bar, 'Fixed scroll bars must not pan their own content'

action(outer, 'scroll-to-start')
action(outer, 'scroll-down')
wait_for(lambda: attrs(outer).get('scroll-y') == 9)
action(outer, 'scroll-right')
wait_for(lambda: attrs(outer).get('scroll-x') == 7)
action(outer, 'scroll-page-down')
wait_for(lambda: attrs(outer).get('scroll-y', 0) > 9)
target = deep('Deep target', outer)
action(outer, 'scroll-to-start')
wait_for(lambda: not state(target, 25))
good(target, C, 'ScrollTo', 0)
wait_for(lambda: state(target, 25))
action(target, 'click')
good(target, C, 'ScrollToPoint', 1, 60, 60)
wait_for(lambda: abs(rect(target)[0] - 60) <= 1 and abs(rect(target)[1] - 60) <= 1)

for title in ['List', 'Tree', 'Text', 'Code', 'Rich']:
    node = find(title)
    action(node, 'scroll-to-start')
    action(node, 'scroll-page-down')
    wait_for(lambda: attrs(node).get('scroll-y', 0) > 0)
    if title != 'Rich':
        assert 'scroll-right' in names(node), (title, attrs(node), rect(node))
        action(node, 'scroll-right')
        wait_for(lambda: attrs(node).get('scroll-x', 0) > 0)
tabs = find('Tabs')
action(tabs, 'scroll-to-start')
action(tabs, 'scroll-right')
wait_for(lambda: attrs(tabs).get('scroll-x', 0) > 0)
action(tabs, 'scroll-page-right')
graph = find('Graph')
assert 'scroll-to-end' not in names(graph), 'Free panning must not advertise fabricated finite endpoints'
action(graph, 'scroll-right')
action(graph, 'scroll-page-down')
wait_for(lambda: attrs(graph).get('scroll-x', 0) > 0 and attrs(graph).get('scroll-y', 0) > 0)
graph_node = deep('Graph node', graph)
good(graph_node, C, 'ScrollToPoint', 1, 650, 400)
wait_for(lambda: abs(rect(graph_node)[0] - 650) <= 1 and abs(rect(graph_node)[1] - 400) <= 1)

import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi
Atspi.init()
def lib_target():
    desktop = Atspi.get_desktop(0)
    def search(parent):
        for index in range(parent.get_child_count()):
            candidate = parent.get_child_at_index(index)
            if candidate.get_name() == 'Deep target':
                return candidate
            found = search(candidate)
            if found:
                return found
    for index in range(desktop.get_child_count()):
        candidate = desktop.get_child_at_index(index)
        if candidate.get_name() == 'Electron2D Native Scroll Probe':
            return search(candidate)
lib_deep = wait_for(lib_target)
assert Atspi.Component.scroll_to(lib_deep.get_component_iface(), Atspi.ScrollType.ANYWHERE)
Atspi.exit()
line = find('Line')
action(line, 'focus')
action(line, 'scroll-to-end')
wait_for(lambda: attrs(line).get('scroll-x', 0) > 0)
next_phase = find('Next scroll phase')
action(next_phase, 'click')
wait_for(lambda: not state(hint_target, 30))
assert 'false' in method(hint_target, C, 'ScrollTo', 0)
assert 'false' in method(point, C, 'ScrollToPoint', 1, 10, 10)
good(target, C, 'ScrollTo', 6)
wait_for(lambda: state(target, 25))
assert rect(target)[2] >= 600 and rect(target)[3] >= 400
tx, ty, tw, th = rect(target)
assert 'false' in method(target, C, 'Contains', tx + tw - 1, ty + 1, 1), 'A clipped part of a showing component must not contain an off-viewport point'
for title in ['List', 'Tree', 'Tabs']:
    node = find(title)
    wait_for(lambda: attrs(node).get('scroll-x-min', 0) < 0)
    action(node, 'scroll-to-end')
    action(node, 'scroll-left')
    wait_for(lambda: attrs(node).get('scroll-x', 0) < 0)
action(next_phase, 'click')
action(manual, 'scroll-to-end')
wait_for(lambda: find('Manual') is None)
action(next_phase, 'click')
wait_for(lambda: any('AttributesChanged' in event for event in events) and any('VisibleDataChanged' in event for event in events))
print('PASS independent native scrolling, payloads, nested geometry, RTL, readonly sources, libatspi and retirement')
