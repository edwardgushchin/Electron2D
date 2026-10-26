"""Independent AT-SPI semantic graph, validation, active descendant and anchor probe."""
import atexit
import re
import subprocess
import threading
import time
import sys


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
        if name(node) == 'Electron2D Native Text Probe':
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



if '--failure' in sys.argv:
    producer = wait_for(lambda: find('Throwing producer'))
    try:
        method(producer, 'org.a11y.atspi.EditableText', 'SetTextContents', 'fail')
    except RuntimeError as error:
        assert 'Failed' in str(error), str(error)
    else:
        raise AssertionError('Text callback did not fail')
    print('PASS native text callback failure wire reply')
    sys.exit(0)

import ast
T = 'org.a11y.atspi.Text'
E = 'org.a11y.atspi.EditableText'
single, multi, code, password, shaped = [wait_for(lambda title=title: find(title)) for title in ['Single', 'Multi', 'Code', 'Password', 'Shaped']]

def text(node):
    return ast.literal_eval(method(node, T, 'GetText', '0', '--', '-1'))[0]

def value(node, iface, key):
    result = method(node, P, 'Get', iface, key)
    return int(re.search(r'<(?:int32 )?(-?\d+)>', result)[1])

def good(node, iface, name, *args):
    result = method(node, iface, name, *[str(arg) for arg in args])
    assert 'true' in result, (name, result)

def invalid(node, iface, name, *args):
    try:
        method(node, iface, name, *[str(arg) for arg in args])
    except RuntimeError as error:
        assert 'InvalidArgs' in str(error), str(error)
    else:
        raise AssertionError('Invalid request accepted: ' + name)

events = []
monitor = subprocess.Popen(['gdbus', 'monitor', '--address', address, '--dest', single[0], '--object-path', single[1]], stdout=subprocess.PIPE, text=True, bufsize=1)
threading.Thread(target=lambda: [events.append(line) for line in monitor.stdout], daemon=True).start()
atexit.register(lambda: monitor.poll() is None and monitor.terminate())
time.sleep(.05)
producer = wait_for(lambda: find('Producer'))
assert text(producer) == 'abc😀'
if '--clipboard' in sys.argv:
    method(producer, E, 'CopyText', '3', '4')
    good(producer, E, 'PasteText', 0)
    assert text(producer) == '😀abc😀'
    good(producer, E, 'CutText', 1, 4)
    assert text(producer) == '😀😀'
    good(producer, E, 'SetTextContents', 'plain')
    good(producer, E, 'PasteText', 5)
    assert text(producer) == 'plainabc'
    method(single, E, 'CopyText', '1', '2')
    good(single, E, 'PasteText', 0)
    assert text(single).startswith('😀A😀')
    good(single, E, 'SetTextContents', 'A😀é שלום world.')

good(producer, E, 'SetTextContents', 'typed😀')
assert text(producer) == 'typed😀'
good(producer, T, 'AddSelection', 0, 5)
assert method(producer, T, 'GetSelection', '0') == '(0, 5)'
good(producer, E, 'SetTextContents', 'retire')
import gi
gi.require_version('Atspi', '2.0')
from gi.repository import Atspi, GLib
Atspi.init()
def lib_field():
    desktop = Atspi.get_desktop(0)
    for i in range(desktop.get_child_count()):
        candidate = desktop.get_child_at_index(i)
        if candidate.get_name() != 'Electron2D Native Text Probe':
            continue
        window = candidate.get_child_at_index(0)
        for j in range(window.get_child_count()):
            field = window.get_child_at_index(j)
            if field.get_name() == 'Single':
                return field
lib_single = wait_for(lib_field)
assert lib_single.get_text_iface() is not None
assert lib_single.get_editable_text_iface() is not None
assert Atspi.Text.get_text(lib_single.get_text_iface(), 0, -1) == 'A😀é שלום world.'
assert Atspi.Text.get_character_count(lib_single.get_text_iface()) == len('A😀é שלום world.')
lib_rect = Atspi.Text.get_character_extents(lib_single.get_text_iface(), 0, Atspi.CoordType.WINDOW)
assert lib_rect.width > 0 and lib_rect.height > 0

empty = wait_for(lambda: find('Empty line'))
assert text(empty) == '\n'
raw = wait_for(lambda: find('Raw text'))
assert text(raw) == 'a�b�'
assert value(raw, T, 'CharacterCount') == 4
assert method(raw, T, 'GetCharacterAtOffset', '1') == '(65533,)'
assert method(raw, T, 'GetCharacterAtOffset', '3') == '(65533,)'
original = 'A😀é שלום world.'
assert text(single) == original
assert state(single, 7) and state(single, 26) and state(single, 38), (method(single, A, "GetState"), method(single, A, "GetRole"))
assert state(multi, 17) and not state(multi, 26)
assert value(single, T, 'CharacterCount') == len(original)
assert ast.literal_eval(method(single, T, 'GetStringAtOffset', '0', '2'))[0] == original
assert '128512' in method(single, T, 'GetCharacterAtOffset', '1')
assert ast.literal_eval(method(single, T, 'GetStringAtOffset', '1', '0')) == ('😀', 1, 2)
assert 'world.' in method(single, T, 'GetStringAtOffset', str(original.index('w')), '1')
assert text(multi) == 'First 😀 line.\nSecond line!\n'
assert text(code) == 'var value = 1;\nreturn value;'
good(code, T, 'AddSelection', 0, 3)
good(code, T, 'AddSelection', 4, 9)
assert method(code, T, 'GetNSelections') == '(2,)'
assert method(code, T, 'GetSelection', '1') == '(4, 9)'
good(code, T, 'SetSelection', 1, 4, 8)
assert method(code, T, 'GetSelection', '1') == '(4, 8)'
good(code, T, 'RemoveSelection', 0)
assert method(code, T, 'GetNSelections') == '(1,)'
assert text(password) == '•' * len('private😀')
assert 'uint32 40' in method(password, A, 'GetRole')
invalid(password, E, 'CopyText', 0, 1)
assert 'false' in method(password, E, 'CutText', '0', '1')
assert text(shaped) == 'Shaped ﬁ שלום'
assert 'semantic value' in method(shaped, A, 'GetAttributes')
font_attributes = method(shaped, T, 'GetAttributeRun', '0', 'true')
assert all(key in font_attributes for key in ['family-name', 'size', 'weight', 'style']), font_attributes
assert 'rtl' in method(shaped, T, 'GetAttributeValue', '9', 'direction')
wrapped = wait_for(lambda: find('Wrapped'))
wrapped_text = text(wrapped)
first_line = ast.literal_eval(method(wrapped, T, 'GetStringAtOffset', '0', '3'))
assert 0 < first_line[2] < len(wrapped_text), first_line
caret = value(wrapped, T, 'CaretOffset')
good(wrapped, T, 'ScrollSubstringTo', len(wrapped_text) - 1, len(wrapped_text), 6)
wrap_bounds = ast.literal_eval(method(wrapped, 'org.a11y.atspi.Component', 'GetExtents', '1'))[0]
wait_for(lambda: wrap_bounds[1] <= ast.literal_eval(method(wrapped, T, 'GetCharacterExtents', str(len(wrapped_text) - 1), '1'))[1] < wrap_bounds[1] + wrap_bounds[3])
assert value(wrapped, T, 'CaretOffset') == caret
good(wrapped, T, 'ScrollSubstringToPoint', 0, 1, 1, 315, 355)
assert value(wrapped, T, 'CaretOffset') == caret

fragments = wait_for(lambda: find('Fragments'))
assert ast.literal_eval(method(fragments, T, 'GetStringAtOffset', '0', '3')) == ('left right', 0, 10)
shaped_rect = ast.literal_eval(method(shaped, T, 'GetCharacterExtents', '0', '1'))
assert shaped_rect[0] >= 310 and shaped_rect[1] >= 304 and shaped_rect[2] > 0, shaped_rect
shaped_run = children(children(shaped)[0])[0]
run_bounds = ast.literal_eval(method(shaped_run, 'org.a11y.atspi.Component', 'GetExtents', '1'))[0]
assert run_bounds[0] == 310 and run_bounds[1] == 304 and run_bounds[2] > 0, run_bounds
single_run_bounds = ast.literal_eval(method(children(single)[0], 'org.a11y.atspi.Component', 'GetExtents', '1'))[0]
assert single_run_bounds[0] >= 12 and single_run_bounds[1] >= 12 and single_run_bounds[2] > 0, single_run_bounds
attrs = method(shaped, T, 'GetDefaultAttributes')
assert all(s in attrs for s in ['center', 'single', '65535,0,0', 'he', 'unused placeholder']), attrs
rect = ast.literal_eval(method(single, T, 'GetCharacterExtents', '0', '1'))
assert rect[2] > 0 and rect[3] > 0, rect
assert method(single, T, 'GetOffsetAtPoint', str(rect[0] + rect[2] // 2), str(rect[1] + rect[3] // 2), '1') == '(0,)'
assert ast.literal_eval(method(single, T, 'GetRangeExtents', '0', '1', '1')) == rect
assert original[0] in method(single, T, 'GetBoundedRanges', *[str(x) for x in rect], '1', '0', '0')
assert 'true' in method(single, T, 'SetCaretOffset', '2')
assert value(single, T, 'CaretOffset') == 2
good(single, T, 'AddSelection', 2, 4)
assert method(single, T, 'GetSelection', '0') == '(2, 4)'
good(single, E, 'DeleteText', 2, 4)
good(single, E, 'InsertText', 2, 'Z', 1)
wait_for(lambda: text(single) == 'A😀Z שלום world.')
invalid(single, T, 'GetCharacterAtOffset', 999)
invalid(single, T, 'GetStringAtOffset', 0, 99)
invalid(single, E, 'InsertText', 0, '😀', 1)
action(find('Next phase'), 'click')
wait_for(lambda: 'false' in method(single, E, 'SetTextContents', 'blocked'))
while GLib.MainContext.default().pending():
    GLib.MainContext.default().iteration(False)
assert lib_single.get_editable_text_iface() is None

good(multi, T, 'SetCaretOffset', 3)
assert 'false' in method(multi, E, 'DeleteText', '0', '1')
action(find('Next phase'), 'click')
wait_for(lambda: text(single) == 'ab')
good(single, E, 'InsertText', 2, '😀XYZ', 7)
wait_for(lambda: text(single) == 'ab😀X')
action(find('Next phase'), 'click')
wait_for(lambda: text(single) == 'Short')
assert 'false' in method(single, T, 'SetCaretOffset', '1')
good(single, E, 'SetTextContents', 'still editable')
wait_for(lambda: text(single) == 'still editable')
action(find('Next phase'), 'click')
wait_for(lambda: text(single) == 'Changed after publication' and text(multi) == 'new\nlast')
good(multi, T, 'AddSelection', 1, 6)
assert method(multi, T, 'GetSelection', '0') == '(1, 6)'
good(multi, E, 'DeleteText', 1, 6)
wait_for(lambda: text(multi) == 'nst')
assert ast.literal_eval(method(multi, T, 'GetStringAtOffset', '0', '3')) == ('nst', 0, 3)
action(find('Next phase'), 'click')
wait_for(lambda: text(shaped) == 'semantic value')
assert method(shaped, T, 'GetNSelections') == '(0,)'
action(find('Next phase'), 'click')
wait_for(lambda: ast.literal_eval(method(fragments, T, 'GetStringAtOffset', '0', '3')) == ('left ', 0, 5))
action(find('Next phase'), 'click')
wait_for(lambda: text(fragments) == 'right')
assert ast.literal_eval(method(fragments, T, 'GetStringAtOffset', '0', '3')) == ('right', 0, 5)
wait_for(lambda: all(any(event in line for line in events) for event in ['TextChanged', 'TextSelectionChanged', 'TextCaretMoved']))
monitor.terminate()
Atspi.exit()
print('PASS independent native text, scalar selection/editing, geometry, masking, history/length policies and source retirement')
