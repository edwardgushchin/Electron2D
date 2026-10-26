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
        context = globals().get("context")
        if context is not None:
            while context.pending(): context.iteration(False)
        result = check()
        if result:
            return result
        time.sleep(.01)
    raise AssertionError('Native collection state did not converge')


def app():
    for node in children(('org.a11y.atspi.Registry', '/org/a11y/atspi/accessible/root')):
        if name(node) == 'Electron2D Native Live Probe':
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


if '--native' in sys.argv:
    def popup(title, owner, request):
        retries = 0
        def present():
            nonlocal retries
            node = find(title, app())
            if node and state(node, 30): return node
            if retries < 3:
                retries += 1
                action(owner, request)
        return wait_for(present)
    line, choices = find('Native line'), find('Native choices')
    menu = popup('Native context', line, 'show-context-menu')
    clear = deep('Clear', menu)
    assert clear and rect(clear)[2] > 0
    action(clear, 'click')
    wait_for(lambda: retired(menu) and retired(clear))
    menu = popup('Native dropdown', choices, 'click')
    second = deep('Native second', menu)
    assert second and rect(second)[2] > 0
    action(second, 'click')
    wait_for(lambda: retired(menu) and retired(second))
    action(find('Finish native popups'), 'click')
    print('PASS independent native popup roots, rendered item geometry, ordinary edits/selection and surface retirement')
    sys.exit(0)


if '--failure' in sys.argv:
    owner = wait_for(lambda: find('Throwing tooltip'))
    try:
        action(owner, 'show-tooltip')
    except RuntimeError as error:
        assert 'Failed' in str(error)
        print('PASS native tooltip callback failure wire reply')
        sys.exit(0)
    raise AssertionError('Tooltip callback did not fail')

import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib
Atspi.init()
context = GLib.MainContext.default()
announcements = []
def announced(event, data=None):
    if event.source.get_application().get_name() == 'Electron2D Native Live Probe':
        announcements.append((event.source.get_name(), event.detail1, event.any_data))
listener = Atspi.EventListener.new(announced, None)
assert listener.register('object:announcement')

def metadata(node):
    return dict(re.findall(r"'([^']+)': '([^']*)'", method(node, A, 'GetAttributes')))

def live(title, priority, text):
    return any(owner == title and level == priority and value == text for owner, level, value in announcements)

def visible_menu(title, owner=None, request='show-context-menu'):
    retries = 0
    def current():
        nonlocal retries
        node = deep(title, owner or root)
        if node and state(node, 30): return node
        if owner and retries < 3:
            retries += 1
            action(owner, request)
        return None
    return wait_for(current)

def item(menu, title):
    return wait_for(lambda: find(title, menu))

next_phase = find('Next live phase')
status, alert = find('Status label'), find('Alert label')
hint, line, edit, rich = find('Hint owner'), find('Editable line'), find('Editor'), find('Rich')
choices, command = find('Choices'), find('Commands')
generic = find('Generic popup')
assert metadata(status)['live'] == 'polite' and metadata(alert)['live'] == 'assertive'
assert metadata(choices)['haspopup'] == 'listbox' and metadata(command)['haspopup'] == 'menu'
assert metadata(generic)['haspopup'] == 'tree' and metadata(find('Dialog popup'))['haspopup'] == 'dialog'
assert metadata(hint)['tooltip'] == 'Actual tooltip text' and state(hint, 13)
assert state(generic, 42) and metadata(generic)['tooltip'] == 'Generic hint'
for label in ('show-tooltip', 'hide-tooltip', 'show-context-menu'): action(generic, label)
# An explicit keyboard/assistive tooltip request must work even on MouseFilter.Ignore.
def show_hint():
    node = deep('Actual tooltip text', hint)
    if node and state(node, 30): return node
    action(hint, 'show-tooltip')
    return None
action(hint, 'show-tooltip')
shown = wait_for(show_hint)
assert state(shown, 30)
action(hint, 'hide-tooltip')
wait_for(lambda: retired(shown))
action(next_phase, 'click')
wait_for(lambda: live('Status label', 1, 'Status final') and live('Alert label', 2, 'Alert final') and live('Live number', 1, '7.5') and live('Manual live', 1, 'Manual final') and live('Inherited region', 1, 'Inherited final') and live('Mode region', 1, 'Mode region'))
assert not any(any(word in str(text) for word in ('SECRET', 'HIDDEN', 'SILENT', 'RETIRED', 'INHERITED OFF', 'GHOST', 'intermediate')) for _, _, text in announcements), announcements
# The real line menu retains custom commands and executes ordinary edits.
action(line, 'show-context-menu')
line_menu = visible_menu('Line context', line)
action(item(line_menu, 'Custom line command'), 'click')
wait_for(lambda: not state(line_menu, 30))
action(line, 'show-context-menu')
action(item(visible_menu('Line context', line), 'Select All'), 'click')
wait_for(lambda: not state(line_menu, 30))
action(line, 'show-context-menu')
action(item(visible_menu('Line context', line), 'Clear'), 'click')
wait_for(lambda: not state(line_menu, 30))
action(line, 'show-context-menu')
action(item(visible_menu('Line context', line), 'Text Direction'), 'click')
direction = wait_for(lambda: deep('Right to Left', line_menu))
wait_for(lambda: state(direction, 30))
action(direction, 'click')
action(edit, 'show-context-menu')
action(item(visible_menu('Editor context', edit), 'Custom editor command'), 'click')
action(item(visible_menu('Editor context', edit), 'Select All'), 'click')
action(rich, 'show-context-menu')
action(item(visible_menu('Rich context', rich), 'Custom rich command'), 'click')
action(item(visible_menu('Rich context', rich), 'Select All'), 'click')
# Option activation flows through its owned PopupMenu and normal ItemSelected event.
action(choices, 'click')
choice_menu = visible_menu('Choice menu', choices, 'click')
choice0, choice1 = item(choice_menu, 'Choice 0'), item(choice_menu, 'Choice 1')
disabled = item(choice_menu, 'Choice 3')
assert 'false' in method(choice_menu, SEL, 'SelectChild', index(disabled))
action(item(choice_menu, 'Choice 2'), 'click')
wait_for(lambda: not state(choice_menu, 30))
action(command, 'click')
commands = visible_menu('Command menu', command, 'click')
check = item(commands, 'Checked command')
assert state(check, 4) and state(item(commands, 'Radio command'), 4)
action(check, 'click')
assert state(check, 4), 'Activation does not invent a checked-state mutation'
nested = item(commands, 'Nested')
assert state(nested, 42) and metadata(nested)['haspopup'] == 'menu'
action(nested, 'click')
wait_for(lambda: state(nested, 10))
action(item(visible_menu('Submenu'), 'Nested command'), 'click')
wait_for(lambda: not state(nested, 10))
action(next_phase, 'click')
wait_for(lambda: metadata(status)['live'] == 'off' and 'show-context-menu' not in names(line))
assert 'show-context-menu' not in names(edit) and 'show-context-menu' not in names(rich)
assert not state(line, 42) and 'haspopup' not in metadata(line)
assert not any(text == 'OVERRIDE MUST NOT ANNOUNCE' for _, _, text in announcements)
action(choices, 'click')
wait_for(lambda: find('Choice renamed', visible_menu('Choice menu', choices, 'click')) == choice1)
action(item(choice_menu, 'Choice 2'), 'click')
wait_for(lambda: not state(choice_menu, 30))
action(next_phase, 'click')
wait_for(lambda: live('Status label', 2, 'Status restored'))
action(line, 'show-context-menu')
read_menu = visible_menu('Line context', line)
clear = item(read_menu, 'Clear')
assert not state(clear, 8)
assert 'false' in method(clear, ACT, 'DoAction', names(clear).index('click'))
action(item(read_menu, 'Select All'), 'click')
action(next_phase, 'click')
wait_for(lambda: retired(choice0) and metadata(choice1)['posinset'] == '1')
action(choices, 'click')
action(item(visible_menu('Choice menu', choices, 'click'), 'Choice renamed'), 'click')
retiring = find('Retiring tooltip')
action(retiring, 'show-tooltip')
wait_for(lambda: retired(retiring))
action(next_phase, 'click')
action(next_phase, 'click')
listener.deregister('object:announcement')
Atspi.exit()
print('PASS independent live announcements, policy, tooltip presentation, menu selection/actions, context edits, ownership and libatspi events')
