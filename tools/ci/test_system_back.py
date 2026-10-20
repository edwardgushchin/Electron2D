#!/usr/bin/env python3
"""Check that native producer evidence counts entered methods, not symbol names."""
import struct
from run_system_back import producer_entries


def trace(owner, method, calls):
    header = ("*version\n3\ndata-file-overflow=false\nclock=dual\n*methods\n"
              f"0x10\t{owner}\t{method}\t()V\tGameActivity.java\n*end\n").encode()
    binary = struct.pack("<4sHHQH", b"SLOW", 3, 32, 0, 14).ljust(32, b"\0")
    return header + binary + b"".join(struct.pack("<HIII", 1, call, 0, 0) for call in calls)


for sdk, owner, method in [(32, "org.electron2d.GameActivity", "dispatchKeyEvent"),
                            (36, "org.electron2d.GameActivity_BackCallback", "onBackInvoked")]:
    assert producer_entries(trace(owner, method, [16, 17, 16, 17]), sdk) == 2
    assert producer_entries(trace(owner, method, [17]), sdk) == 0
    assert producer_entries(trace(owner, method, []), sdk) == 0
    assert producer_entries(trace(owner, "differentMethod", [16]), sdk) == 0
    try:
        producer_entries(trace(owner, method, [16]).replace(b"overflow=false", b"overflow=true"), sdk)
    except AssertionError:
        pass
    else:
        raise AssertionError("Overflowed traces cannot prove producer counts.")
print("System-back trace evidence checks passed.")
