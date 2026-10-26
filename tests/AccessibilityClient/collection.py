"""Independent AT-SPI collection, selection, hierarchy and table probe."""
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
    return call('--dest', node[0], '--object-path', node[1], '--method', iface + '.' + name, '--', *map(str, values), address=address)


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
    raise AssertionError('Native collection state did not converge')


def app():
    for node in children(('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root')):
        if name(node) == 'Electron2D Native Collection Probe':
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


SEL = 'org.a11y.atspi.Selection'
T = 'org.a11y.atspi.Table'
TC = 'org.a11y.atspi.TableCell'


def prop(node, iface, label):
    return method(node, P, 'Get', iface, label)


def number(node, iface, label):
    return int(re.findall(r'-?\d+', prop(node, iface, label))[-1])


def ref(result):
    return re.findall(r"\('([^']*)',\s*(?:objectpath\s+)?'([^']+)'\)", result)[0]


def cell(table, row, column):
    return ref(method(table, T, 'GetAccessibleAt', row, column))


def index(node):
    return int(re.findall(r'-?\d+', method(node, A, 'GetIndexInParent'))[-1])


def position(node):
    return tuple(map(int, re.findall(r'-?\d+', prop(node, TC, 'Position').replace('int32', ''))))


def metadata(node):
    return dict(re.findall(r"'([^']+)': '([^']*)'", method(node, A, 'GetAttributes')))


def retired(node):
    try:
        method(node, A, 'GetRole')
    except RuntimeError as error:
        return 'UnknownObject' in str(error) or 'UnknownMethod' in str(error)
    return False


if '--failure' in sys.argv:
    owner = wait_for(lambda: find('Throwing collection'))
    try:
        method(owner, SEL, 'SelectChild', 0)
    except RuntimeError as error:
        assert 'Failed' in str(error)
        print('PASS native collection callback failure wire reply')
        sys.exit(0)
    raise AssertionError('Collection callback did not fail')


monitor = subprocess.Popen(['gdbus', 'monitor', '--address', address, '--dest', root[0]], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
atexit.register(lambda: monitor.poll() is None and monitor.terminate())
events = []
threading.Thread(target=lambda: events.extend(monitor.stdout), daemon=True).start()
time.sleep(.08)
owner = wait_for(lambda: find('Manual table'))
assert number(owner, T, 'NRows') == 3 and number(owner, T, 'NColumns') == 3
span = find('Spanning cell', owner)
assert cell(owner, 1, 1) == span and position(span) == (0, 0)
assert number(span, TC, 'RowSpan') == 2 and number(span, TC, 'ColumnSpan') == 2
assert ref(prop(span, TC, 'Table')) == owner
assert 'true' in method(span, TC, 'GetRowColumnSpan')
assert find('Manual row 1', owner)[1] in method(span, TC, 'GetRowHeaderCells')
assert find('Manual col 2', owner)[1] in method(find('Middle right', owner), TC, 'GetColumnHeaderCells')
assert 'Manual row 1' in method(owner, T, 'GetRowDescription', 1)
assert 'Manual col 2' in method(owner, T, 'GetColumnDescription', 2)
assert int(re.findall(r'-?\d+', method(owner, T, 'GetIndexAt', 1, 1))[-1]) == index(span)
assert '0' in method(owner, T, 'GetRowAtIndex', index(span))
assert cell(owner, -1, 0)[1] == '/org/a11y/atspi/null'
assert metadata(span)['posinset'] == '1' and metadata(span)['level'] == '1'
action(span, 'collapse')
wait_for(lambda: not state(span, 10))
action(span, 'expand')
wait_for(lambda: state(span, 10))
assert number(owner, T, 'NSelectedRows') == 0
good(owner, T, 'AddRowSelection', 0)
wait_for(lambda: number(owner, T, 'NSelectedRows') == 1)
good(owner, T, 'AddRowSelection', 1)
wait_for(lambda: number(owner, T, 'NSelectedRows') == 2)
good(owner, SEL, 'SelectChild', index(find('Bottom span', owner)))
wait_for(lambda: number(owner, T, 'NSelectedRows') == 3 and number(owner, T, 'NSelectedColumns') == 3)
good(owner, SEL, 'ClearSelection')
wait_for(lambda: number(owner, SEL, 'NSelectedChildren') == 0)
good(owner, T, 'AddColumnSelection', 2)
wait_for(lambda: number(owner, T, 'NSelectedColumns') == 1)
good(owner, T, 'RemoveColumnSelection', 2)
wait_for(lambda: number(owner, SEL, 'NSelectedChildren') == 0)
bad(owner, SEL, 'SelectChild', -1)
assert 'false' in method(owner, T, 'AddRowSelection', -1)
extreme = find('Extreme table')
assert number(extreme, T, 'NRows') == 2147483647 and cell(extreme, 2147483646, 0)[1] != '/org/a11y/atspi/null'
bad(extreme, T, 'GetSelectedRows')

listing = find('List')
assert SEL in method(listing, A, 'GetInterfaces')
assert number(listing, SEL, 'NSelectedChildren') == 2
item9 = find('Item 9', listing)
assert metadata(item9)['posinset'] == '10' and metadata(item9)['setsize'] == '30'
good(listing, SEL, 'SelectChild', index(find('Item 7', listing)))
wait_for(lambda: number(listing, SEL, 'NSelectedChildren') == 3)
assert 'false' in method(listing, SEL, 'SelectChild', index(find('Item 3', listing)))
assert 'false' in method(listing, SEL, 'SelectChild', index(find('Item 4', listing)))
good(listing, SEL, 'SelectAll')
wait_for(lambda: number(listing, SEL, 'NSelectedChildren') == 28)
good(listing, SEL, 'DeselectSelectedChild', 0)
wait_for(lambda: number(listing, SEL, 'NSelectedChildren') == 27)
good(listing, SEL, 'ClearSelection')
wait_for(lambda: number(listing, SEL, 'NSelectedChildren') == 0)
action(item9, 'focus')
wait_for(lambda: state(item9, 23))
action(item9, 'click')

grid = find('Grid')
assert T in method(grid, A, 'GetInterfaces')
assert number(grid, T, 'NColumns') == 3
branch = find('Branch', grid)
folded = find('Folded child', grid)
assert state(branch, 9) and not state(branch, 10) and not state(folded, 30)
rows_before = number(grid, T, 'NRows')
action(branch, 'expand')
wait_for(lambda: number(grid, T, 'NRows') == rows_before + 1 and state(folded, 30))
assert branch[1] in method(folded, A, 'GetRelationSet') and metadata(folded)['level'] == '2'
action(branch, 'collapse')
wait_for(lambda: not state(folded, 30))
action(branch, 'expand')
wait_for(lambda: state(folded, 30))
assert find('Folded child', grid) == folded
merged = find('Merged row', grid)
row, col = position(merged)
assert cell(grid, row, 1) == merged and number(merged, TC, 'ColumnSpan') == 3
good(grid, T, 'AddColumnSelection', 1)
wait_for(lambda: state(merged, 23))
good(grid, T, 'RemoveColumnSelection', 1)
wait_for(lambda: not state(merged, 23) and not state(find('A 13', grid), 23))
good(grid, T, 'AddRowSelection', row)
wait_for(lambda: state(merged, 23))
good(grid, T, 'RemoveRowSelection', row)
wait_for(lambda: not state(merged, 23))
row0, old_column = find('Row 0', grid), find('B 1', grid)
far = find('Row 12', grid)
good(far, C, 'ScrollTo', 6)
wait_for(lambda: state(far, 25))
header = find('Column 2', grid)
vertical = attrs(grid).get('scroll-y')
good(header, C, 'ScrollTo', 4)
wait_for(lambda: state(header, 25))
assert attrs(grid).get('scroll-y') == vertical, 'Revealing a fixed-height header preserves vertical scroll'
good(grid, SEL, 'ClearSelection')
good(grid, SEL, 'SelectChild', index(find('A 1', grid)))
wait_for(lambda: state(find('A 1', grid), 23))
assert find('Column 1', grid)[1] in method(find('A 1', grid), TC, 'GetColumnHeaderCells')

tabs = find('Tabs')
selected_tab = find('Wide tab 1', tabs)
assert SEL in method(tabs, A, 'GetInterfaces') and number(tabs, SEL, 'NSelectedChildren') == 1
assert 'false' in method(tabs, SEL, 'SelectAll') and 'false' in method(tabs, SEL, 'ClearSelection')
assert 'false' in method(tabs, SEL, 'SelectChild', index(find('Wide tab 3', tabs)))
assert 'false' in method(tabs, SEL, 'SelectChild', index(find('Wide tab 5', tabs)))
reveal_tab = find('Wide tab 12', tabs)
good(reveal_tab, C, 'ScrollTo', 6)
wait_for(lambda: state(reveal_tab, 25))
good(tabs, SEL, 'SelectChild', index(reveal_tab))
wait_for(lambda: state(reveal_tab, 23))
action(item9, 'focus')
wait_for(lambda: number(listing, SEL, 'NSelectedChildren') == 1 and state(item9, 23))
next_phase = find('Next collection phase')
action(next_phase, 'click')
wait_for(lambda: metadata(item9)['posinset'] == '1')
assert find('Item 9', listing) == item9
clone = wait_for(lambda: find('Clone'))
assert find('Item 9', clone) != item9, 'Duplicated records receive distinct native identities'
destination = find('Destination')
wait_for(lambda: ref(prop(row0, TC, 'Table')) == destination)
assert 'false' in method(destination, SEL, 'SelectAll')
assert 'false' in method(destination, T, 'AddRowSelection', position(row0)[0])
assert find('Row 0', destination) == row0, 'Moving a row between live owners preserves its identity'
assert find('Wide tab 1', tabs) == selected_tab and metadata(selected_tab)['posinset'] == '9'
good(tabs, SEL, 'ClearSelection')
wait_for(lambda: number(tabs, SEL, 'NSelectedChildren') == 0)
event_start = len(events)
action(next_phase, 'click')
wait_for(lambda: retired(item9) and retired(old_column) and retired(folded))
assert not state(branch, 9) and not state(branch, 10)
wait_for(lambda: any(branch[1] in event and re.search(r"StateChanged \('expanded', 0,", event) for event in events[event_start:]))
assert number(grid, T, 'NColumns') == 2
retiring = find('Retiring collection')
good(retiring, SEL, 'SelectChild', index(find('Retire child', retiring)))
wait_for(lambda: retired(retiring))
action(next_phase, 'click')

import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi
Atspi.init()
def lib_grid():
    desktop = Atspi.get_desktop(0)
    for i in range(desktop.get_child_count()):
        app = desktop.get_child_at_index(i)
        if app.get_name() == 'Electron2D Native Collection Probe':
            frame = app.get_child_at_index(0)
            for j in range(frame.get_child_count()):
                node = frame.get_child_at_index(j)
                if node.get_name() == 'Grid': return node
lib = wait_for(lib_grid)
assert Atspi.Table.get_n_columns(lib.get_table_iface()) == 2
assert Atspi.Table.get_accessible_at(lib.get_table_iface(), 0, 0) is not None
assert lib.get_selection_iface() is not None
Atspi.exit()
wait_for(lambda: any('SelectionChanged' in event for event in events) and any('ModelChanged' in event for event in events) and any('expanded' in event for event in events))
print('PASS independent collection selection/table/cell, spans, sources, stable ownership, libatspi and retirement')
