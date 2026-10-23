"""Exercise the diagnostic interposer against real pthread key ownership and failure."""
from pathlib import Path
import os
import re
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]
CONTROL = r'''
#include <assert.h>
#include <errno.h>
#include <pthread.h>
#include <stdio.h>
#include <string.h>
static pthread_key_t keys[2048];
static unsigned used, destroyed;
static void destroy(void *value) { assert(value == (void *)1); destroyed++; }
static void *churn(void *unused) {
    (void)unused;
    for (unsigned i = 0; i < 64; i++) {
        pthread_key_t key;
        errno = EDOM;
        assert(!pthread_key_create(&key, destroy) && errno == EDOM);
        assert(!pthread_key_delete(key) && errno == EDOM);
    }
    return 0;
}
static void *worker(void *unused) {
    (void)unused;
    for (unsigned i = 0; i < used; i++) assert(!pthread_setspecific(keys[i], (void *)1));
    return 0;
}
int main(int argc, char **argv) {
    assert(argc == 2);
    if (!strcmp(argv[1], "parallel")) {
        pthread_t threads[8];
        for (unsigned i = 0; i < 8; i++) assert(!pthread_create(&threads[i], 0, churn, 0));
        for (unsigned i = 0; i < 8; i++) assert(!pthread_join(threads[i], 0));
        printf("created=512\n");
        return 0;
    }
    unsigned limit = !strcmp(argv[1], "limit") ? 2048 : !strcmp(argv[1], "leak") ? 3 : 64;
    for (; used < limit; used++) {
        errno = EDOM;
        int result = pthread_key_create(&keys[used], destroy);
        assert(errno == EDOM);
        if (result) { assert(limit == 2048 && result == EAGAIN); break; }
    }
    if (limit == 2048) assert(used < limit);
    if (limit == 64) {
        pthread_t thread;
        assert(!pthread_create(&thread, 0, worker, 0));
        assert(!pthread_join(thread, 0) && destroyed == used);
    }
    if (limit != 3) for (unsigned i = 0; i < used; i++) {
        errno = EDOM;
        assert(!pthread_key_delete(keys[i]) && errno == EDOM);
    }
    printf("created=%u\n", used);
}
'''


with tempfile.TemporaryDirectory(prefix="e2d-tls-trace-") as directory:
    root = Path(directory)
    library, fixture = root / "trace.so", root / "fixture"
    (root / "fixture.c").write_text(CONTROL)
    flags = ["-std=c11", "-O2", "-Wall", "-Wextra", "-Werror", "-pthread"]
    subprocess.run(["cc", *flags, "-fPIC", "-shared", str(ROOT / "tools/ci/trace_tls.c"), "-ldl", "-o", str(library)], check=True)
    subprocess.run(["cc", *flags, str(root / "fixture.c"), "-o", str(fixture)], check=True)
    for mode in ("clean", "leak", "limit", "parallel"):
        baseline = subprocess.run([str(fixture), mode], capture_output=True, text=True, check=True)
        log = root / (mode + ".log")
        result = subprocess.run([str(fixture), mode], env=os.environ | {"LD_PRELOAD": str(library), "E2D_TLS_TRACE_FILE": str(log)}, capture_output=True, text=True, check=True)
        assert (baseline.stdout, baseline.stderr) == (result.stdout, result.stderr), (mode, result)
        trace = log.read_text()
        fields = re.search(r"summary created=(\d+) deleted=(\d+) live=(\d+) peak=(\d+) failures=(\d+) untracked=(\d+)", trace)
        assert fields, trace
        created, deleted, live, peak, failures, untracked = map(int, fields.groups())
        expected = int(result.stdout.removeprefix("created="))
        assert created == expected and untracked == 0
        assert 0 < peak <= 8 if mode == "parallel" else peak == expected
        assert (deleted, live) == ((0, 3) if mode == "leak" else (expected, 0))
        assert failures == (1 if mode == "limit" else 0)
        assert "caller=" + str(fixture) in trace
        assert log.stat().st_mode & 0o777 == 0o600
        print(f"TLS trace {mode}: {created} created, {deleted} deleted, {live} live, {failures} failures; native results/errno/destructors preserved.")
    for destination in ("", str(root / "absent/trace.log")):
        result = subprocess.run([str(fixture), "clean"], env=os.environ | {"LD_PRELOAD": str(library), "E2D_TLS_TRACE_FILE": destination}, capture_output=True)
        assert result.returncode == 126, result
    print("TLS trace rejects missing/unavailable output instead of losing diagnostics.")
