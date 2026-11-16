"""Check actual child stdout/stderr separately from in-process writer callbacks."""
import subprocess
import sys

result = subprocess.run(sys.argv[1:] + ["wire"], capture_output=True, check=True)
expected_out = "AБ\r\nA Б\r\nA\tБ\r\n".encode("utf-8")
expected_error = "AБ\r\nError: AБ [wire.cs:42 @ Origin]\r\nWarning: AБ [wire.cs:42 @ Origin]\r\n".encode("utf-8")
if sys.platform != "win32":
    expected_out = expected_out.replace(b"\r\n", b"\n")
    expected_error = expected_error.replace(b"\r\n", b"\n")
if result.stdout != expected_out or result.stderr != expected_error:
    raise AssertionError((result.stdout, result.stderr))
print("Six roles: exact UTF-8 child stdout/stderr bytes and platform newline passed.")
