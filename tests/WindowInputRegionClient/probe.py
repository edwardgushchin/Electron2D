"""Real X11/XWayland pointer routing via one temporary kernel uinput mouse."""
import fcntl
import os
import struct
import sys
import time


def iow(number, size=4):
    return (1 << 30) | (size << 16) | (ord('U') << 8) | number


def command(text):
    print(text, flush=True)
    line = sys.stdin.readline()
    if not line: raise AssertionError('Native input host closed its control pipe')
    return list(map(int, line.strip().split('|')))


fd = os.open('/dev/uinput', os.O_WRONLY | os.O_NONBLOCK)
created = False
held = False
initial = None


def event(kind, code, value):
    os.write(fd, struct.pack('llHHi', 0, 0, kind, code, value))


def sync(): event(0, 0, 0)


def move(x, y):
    event(2, 0, x); event(2, 1, y); sync()


def sample(): return command('sample')


def position(x, y):
    origin = sample(); x += origin[10]; y += origin[11]
    gain_x = gain_y = 1.0
    # Compositors may repick a changed input region only after pointer motion.
    move(2, 1); time.sleep(.15)
    state = sample()
    for _ in range(50):
        dx, dy = x - state[2], y - state[3]
        if abs(dx) <= 4 and abs(dy) <= 4:
            time.sleep(.18)
            stable = sample()
            if abs(x - stable[2]) <= 4 and abs(y - stable[3]) <= 4: return stable
            state = stable
            continue
        sx = int(max(-100, min(100, dx / gain_x * .65)))
        sy = int(max(-100, min(100, dy / gain_y * .65)))
        if abs(dx) > 4 and sx == 0: sx = 1 if dx > 0 else -1
        if abs(dy) > 4 and sy == 0: sy = 1 if dy > 0 else -1
        move(sx, sy); time.sleep(.14)
        after = sample()
        if sx and after[2] != state[2]: gain_x = max(.2, min(8, abs((after[2] - state[2]) / sx)))
        if sy and after[3] != state[3]: gain_y = max(.2, min(8, abs((after[3] - state[3]) / sy)))
        state = after
    raise AssertionError(('Pointer calibration failed', x, y, state))


def native(action, *args):
    if action == 'click':
        if args[0] == 4: event(2, 11, 120); event(2, 8, 1); sync()
        else:
            event(1, 272, 1); sync(); time.sleep(.065)
            event(1, 272, 0); sync()
    elif action in ('mousedown', 'mouseup'):
        event(1, 272, 1 if action == 'mousedown' else 0); sync()
    elif action == 'mousemove':
        state = sample(); position(args[0] - state[10], args[1] - state[11])


def wait_changed(before, indexes):
    deadline = time.monotonic() + 3
    while True:
        after = sample()
        if any(after[i] != before[i] for i in indexes): return after
        assert time.monotonic() < deadline, ('Native input delivery timed out', before, after)
        time.sleep(.04)


def click(expected, x, y):
    before = position(x, y); native('click', 1)
    after = wait_changed(before, [4, 5]); deadline = time.monotonic() + 3
    while after[8] + after[9] <= before[8] + before[9] and time.monotonic() < deadline:
        time.sleep(.04); after = sample()
    actual = (after[4] - before[4], after[5] - before[5], after[8] - before[8], after[9] - before[9])
    assert actual == ((1, 0, 1, 0) if expected == 'root' else (0, 1, 0, 1)), (expected, before, after)
    return after


fcntl.ioctl(fd, iow(100), 1)
fcntl.ioctl(fd, iow(100), 2)
fcntl.ioctl(fd, iow(101), 272)
for code in [0, 1, 8, 11]: fcntl.ioctl(fd, iow(102), code)
fcntl.ioctl(fd, iow(110), 0)
name = ('Electron2D region probe ' + str(os.getpid())).encode()
setup = struct.pack('HHHH80sI', 3, 0x2d21, 1, 1, name, 0)
fcntl.ioctl(fd, iow(3, len(setup)), setup)
fcntl.ioctl(fd, (ord('U') << 8) | 1)
created = True
time.sleep(.8)
initial = sample(); held = False
width, height = initial[:2]
left, right, middle = width // 4, width * 3 // 4, height // 2
try:
    click('top', right, middle)
    native('click', 4); time.sleep(.2)
    baseline = sample(); native('click', 4); warm = wait_changed(baseline, [6, 7])
    assert warm[7] == baseline[7] + 1 and warm[6] == baseline[6], ('Prepared native wheel source', baseline, warm)
    command('polygon'); time.sleep(.15)
    click('top', left, middle); before = click('root', right, middle)
    # XInput2 establishes a scroll-valuator baseline after a focus change.
    native('click', 4); time.sleep(.2); before = sample()
    native('click', 4); after = wait_changed(before, [6, 7])
    assert after[6] == before[6] + 1 and after[7] == before[7], (before, after)
    command('bow'); time.sleep(.15)
    click('top', width // 8, height // 4); click('root', width // 2, height // 4)
    command('polygon'); command('pass'); time.sleep(.15); click('root', left, middle)
    command('restore'); time.sleep(.15)
    click('top', left, middle); click('root', right, middle)
    command('clear'); time.sleep(.15); click('top', right, middle)
    command('degenerate'); time.sleep(.15); click('root', left, middle)
    command('service'); time.sleep(.15)
    click('top', left, middle); click('root', right, middle)
    command('reopen'); time.sleep(.4)
    click('top', left, middle); click('root', right, middle)
    command('clear'); time.sleep(.15)
    before = position(left, middle); native('mousedown', 1); held = True
    wait_changed(before, [5]); command('pass'); time.sleep(.15)
    native('mouseup', 1); held = False; after = wait_changed(before, [8, 9])
    assert after[5] == before[5] + 1 and after[8] + after[9] == before[8] + before[9] + 1 and after[12] == 0, ('Native release and cleared polling', before, after)
    command('restore'); time.sleep(.15)
    previous = sample(); position(left + 10, middle); after = wait_changed(previous, [14])
    assert after[13] == 0 and after[12] == 0, ('Released button mask in the previous native owner', previous, after)
    native('mousemove', initial[2], initial[3]); command('done')
    print('PASS independent uinput/X11 native motion/button/wheel routing, polygon/override/reset, degenerate/service masks, native identity replacement and held-button release', file=sys.stderr, flush=True)
finally:
    try:
        if held: native('mouseup', 1)
    finally:
        if created: fcntl.ioctl(fd, (ord('U') << 8) | 2)
        os.close(fd)
