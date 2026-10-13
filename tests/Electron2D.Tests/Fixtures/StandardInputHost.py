import os
import subprocess
import sys

mode, root = sys.argv[1:3]
command = sys.argv[3:]
if mode == "file":
    with open(os.path.join(root, "input.bin"), "rb") as source, open(os.path.join(root, "output.json"), "wb") as output, open(os.path.join(root, "error.txt"), "wb") as error:
        result = subprocess.run(command, stdin=source, stdout=output, stderr=error, timeout=15)
    sys.stdout.buffer.write(open(os.path.join(root, "output.json"), "rb").read())
    sys.exit(result.returncode)
if mode == "null":
    with open(os.devnull, "rb") as source:
        result = subprocess.run(command, stdin=source, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=15)
    sys.stdout.buffer.write(result.stdout)
    sys.stderr.buffer.write(result.stderr)
    sys.exit(result.returncode)
if mode == "terminal":
    master, slave = os.openpty()
    try:
        child = subprocess.Popen(command, stdin=slave, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        os.write(master, b"tty\n")
        output, error = child.communicate(timeout=15)
        sys.stdout.buffer.write(output)
        sys.stderr.buffer.write(error)
        sys.exit(child.returncode)
    finally:
        if child.poll() is None:
            child.kill()
            child.wait()
        os.close(master)
        os.close(slave)
raise ValueError(mode)
