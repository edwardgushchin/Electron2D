"""Check timeout cleanup without building a runtime or attaching to a process."""
import ast
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time

# Load just the process helper; importing this CLI would parse arguments and build fixtures.
source = ast.parse(Path(__file__).with_name("run.py").read_text())
helper = next(node for node in source.body if isinstance(node, ast.FunctionDef) and node.name == "run_probe")
namespace = globals().copy()
exec(compile(ast.Module(body=[helper], type_ignores=[]), "run.py", "exec"), namespace)
run_probe = namespace["run_probe"]

with tempfile.TemporaryDirectory() as folder:
    pids = Path(folder) / "pids"
    program = ("import os,signal,subprocess,time; from pathlib import Path; "
               "child=subprocess.Popen(['sleep','60']); "
               "os.kill(child.pid,signal.SIGSTOP); "
               f"Path({str(pids)!r}).write_text(str(os.getpid())+' '+str(child.pid)); time.sleep(60)")
    options = dict(cwd=folder, env=os.environ, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        run_probe([sys.executable, "-c", program], **options, timeout=1)
    except subprocess.TimeoutExpired:
        pass
    else:
        raise AssertionError("The nested fixture must time out")
    assert pids.exists(), "The nested fixture started before the timeout"
    for pid in map(int, pids.read_text().split()):
        for _ in range(100):
            status = Path(f"/proc/{pid}/stat")
            if not status.exists() or status.read_text().split()[2] == "Z":
                break
            time.sleep(.01)
        else:
            raise AssertionError(f"Owned process {pid} survived the timeout")
    run_probe([sys.executable, "-c", "pass"], **options, timeout=5)
    try:
        run_probe([sys.executable, "-c", "raise SystemExit(7)"], **options, timeout=5)
    except subprocess.CalledProcessError as error:
        assert error.returncode == 7
    else:
        raise AssertionError("Nonzero exits must still reject the probe")
with tempfile.TemporaryDirectory() as folder:
    output = Path(folder) / "must-not-start"
    for extra in [[], ["--icd", "missing.json", "--network"], ["--icd", "missing.json", "--threaded"]]:
        result = subprocess.run([sys.executable, str(Path(__file__).with_name("run.py")), str(output), "--native-phases", *extra], capture_output=True, text=True)
        assert result.returncode == 2 and "Native phases require" in result.stderr and not output.exists()
with tempfile.TemporaryDirectory() as folder:
    output = Path(folder) / "must-not-start"
    for extra, error in [(["--unpaced"], "require --network"), (["--client-poll-delay-ms", "1"], "require --network"),
                         (["--network", "--client-backend", "cpu", "--client-poll-delay-ms", "-1"], "between 0 and 250"),
                         (["--network", "--client-backend", "cpu", "--client-poll-delay-ms", "251"], "between 0 and 250")]:
        result = subprocess.run([sys.executable, str(Path(__file__).with_name("run.py")), str(output), *extra], capture_output=True, text=True)
        assert result.returncode == 2 and error in result.stderr and not output.exists()
with tempfile.TemporaryDirectory() as folder:
    output = Path(folder) / "must-not-start"
    for extra, error in [(["--heaptrack-preload", "missing.so"], "require --native-phases"),
                         (["--native-phases", "--icd", "missing.json", "--heaptrack-preload", "missing.so"], "existing libheaptrack_preload.so")]:
        result = subprocess.run([sys.executable, str(Path(__file__).with_name("run.py")), str(output), *extra], capture_output=True, text=True)
        assert result.returncode == 2 and error in result.stderr and not output.exists()
print("PASS: timeout retires nested processes; success/nonzero exits and native-phase/network/heaptrack option preflight are preserved.")
