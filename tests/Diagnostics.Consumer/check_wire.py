"""Check actual child stdout/stderr separately from in-process writer callbacks."""
import errno
import os
import subprocess
import sys


def run_terminal(command, environment):
    import pty
    import termios

    master, slave = pty.openpty()
    try:
        try:
            attributes = termios.tcgetattr(slave)
            attributes[1] &= ~termios.OPOST
            termios.tcsetattr(slave, termios.TCSANOW, attributes)
            result = subprocess.run(
                command, stdin=subprocess.DEVNULL, stdout=slave, stderr=subprocess.PIPE,
                env=environment, check=True, timeout=20,
            )
        finally:
            os.close(slave)
        output = bytearray()
        while True:
            try:
                data = os.read(master, 4096)
            except OSError as failure:
                if failure.errno != errno.EIO:
                    raise
                # Linux reports a closed PTY slave as EIO instead of EOF.
                break
            if not data:
                break
            output.extend(data)
        return bytes(output), result.stderr
    finally:
        os.close(master)


result = subprocess.run(sys.argv[1:] + ["wire"], capture_output=True, check=True, timeout=20)
expected_out = "AБ\r\nA Б\r\nA\tБ\r\n".encode("utf-8")
expected_error = "AБ\r\nError: AБ [wire.cs:42 @ Origin]\r\nWarning: AБ [wire.cs:42 @ Origin]\r\n".encode("utf-8")
if sys.platform != "win32":
    expected_out = expected_out.replace(b"\r\n", b"\n")
    expected_error = expected_error.replace(b"\r\n", b"\n")
if result.stdout != expected_out or result.stderr != expected_error:
    raise AssertionError((result.stdout, result.stderr))
print("Six roles: exact UTF-8 child stdout/stderr bytes and platform newline passed.")

newline = b"\r\n" if sys.platform == "win32" else b"\n"
plain = "A\tБ😀".encode("utf-8") + newline + b"end" + newline
formatted = "A\t\x1b[92m\x1b[1mБ😀\x1b[22m\x1b[39m\x1b[0m".encode("utf-8") + newline + b"end" + newline
raw_rich_command = sys.argv[1:] + ["raw-rich-wire"]
raw_rich_environment = dict(os.environ)
raw_rich_environment.pop("NO_COLOR", None)
raw_rich_environment.pop("FORCE_COLOR", None)
for force_color, expected in ((False, plain), (True, formatted)):
    environment = dict(raw_rich_environment)
    if force_color:
        environment["FORCE_COLOR"] = "1"
    result = subprocess.run(
        raw_rich_command, capture_output=True, check=True, env=environment, timeout=20
    )
    if result.stdout != expected or result.stderr:
        raise AssertionError((force_color, expected, result.stdout, result.stderr))

print("Raw/rich roles: exact UTF-8 child bytes with default redirection and forced ANSI passed.")

if os.name == "posix":
    for terminal, expected in (("xterm", formatted), ("dumb", plain)):
        environment = dict(raw_rich_environment, TERM=terminal)
        stdout, stderr = run_terminal(raw_rich_command, environment)
        if terminal == "xterm":
            # System.Console may initialize xterm's application keypad on its first write.
            stdout = stdout.removeprefix(b"\x1b[?1h\x1b=")
        if stdout != expected or stderr:
            raise AssertionError((terminal, expected, stdout, stderr))
    print("Raw/rich roles: exact UTF-8 bytes through xterm and dumb pseudo-terminals passed.")
