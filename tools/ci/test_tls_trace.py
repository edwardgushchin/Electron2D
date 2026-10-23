"""Exercise the diagnostic interposer against real pthread key ownership and failure."""
from pathlib import Path
import os
import re
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[2]
CONTROL = r'''
#define _GNU_SOURCE
#include <assert.h>
#include <errno.h>
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
static pthread_key_t keys[2048];
static unsigned used, destroyed;
static void destroy(void *value) { assert(value == (void *)1); destroyed++; }
static void *retire(void *unused) { (void)unused; assert(!pthread_key_delete(keys[0])); return 0; }
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
    if (!strcmp(argv[1], "reuse")) {
        int descriptors[2]; char destination[32], signal;
        assert(!pipe(descriptors));
        snprintf(destination, sizeof(destination), "%d", descriptors[1]);
        assert(!setenv("E2D_TLS_DELETE_FD", destination, 1));
        assert(!pthread_key_create(&keys[0], destroy));
        pthread_t thread;
        assert(!pthread_create(&thread, 0, retire, 0));
        assert(read(descriptors[0], &signal, 1) == 1);
        assert(!pthread_key_create(&keys[1], destroy) && keys[0] == keys[1]);
        assert(!pthread_join(thread, 0));
        assert(!pthread_key_delete(keys[1]));
        close(descriptors[0]); close(descriptors[1]);
        printf("created=2\n");
        return 0;
    }
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
DELAY = r'''
#define _GNU_SOURCE
#include <dlfcn.h>
#include <pthread.h>
#include <stdatomic.h>
#include <stdlib.h>
#include <time.h>
#include <unistd.h>
int pthread_key_delete(pthread_key_t key) {
    static int (*function)(pthread_key_t);
    static atomic_flag delayed = ATOMIC_FLAG_INIT;
    if (!function) function = (int (*)(pthread_key_t))dlsym(RTLD_NEXT, "pthread_key_delete");
    int result = function(key);
    if (!atomic_flag_test_and_set(&delayed)) {
        const char *descriptor = getenv("E2D_TLS_DELETE_FD");
        if (descriptor) {
            ssize_t written = write(atoi(descriptor), "d", 1); (void)written;
            struct timespec duration = {0, 200000000}; nanosleep(&duration, 0);
        }
    }
    return result;
}
'''


with tempfile.TemporaryDirectory(prefix="e2d-tls-trace-") as directory:
    root = Path(directory)
    library, fixture = root / "trace.so", root / "fixture"
    (root / "fixture.c").write_text(CONTROL)
    (root / "delay.c").write_text(DELAY)
    flags = ["-std=c11", "-O2", "-Wall", "-Wextra", "-Werror", "-pthread"]
    subprocess.run(["cc", *flags, "-fPIC", "-shared", str(ROOT / "tools/ci/trace_tls.c"), "-ldl", "-o", str(library)], check=True)
    subprocess.run(["cc", *flags, str(root / "fixture.c"), "-o", str(fixture)], check=True)
    delay = root / "delay.so"
    subprocess.run(["cc", *flags, "-fPIC", "-shared", str(root / "delay.c"), "-ldl", "-o", str(delay)], check=True)
    for mode in ("clean", "leak", "limit", "parallel", "reuse"):
        tail = str(delay) if mode == "reuse" else ""
        baseline = subprocess.run([str(fixture), mode], env=os.environ | {"LD_PRELOAD": tail}, capture_output=True, text=True, check=True)
        log = root / (mode + ".log")
        result = subprocess.run([str(fixture), mode], env=os.environ | {"LD_PRELOAD": str(library) + (":" + tail if tail else ""), "E2D_TLS_TRACE_FILE": str(log)}, capture_output=True, text=True, check=True)
        assert (baseline.stdout, baseline.stderr) == (result.stdout, result.stderr), (mode, result)
        trace = log.read_text()
        fields = re.search(r"summary created=(\d+) deleted=(\d+) live=(\d+) peak=(\d+) failures=(\d+) untracked=(\d+)", trace)
        assert fields, trace
        created, deleted, live, peak, failures, untracked = map(int, fields.groups())
        expected = int(result.stdout.removeprefix("created="))
        assert created == expected and untracked == 0
        if mode == "parallel": assert 0 < peak <= 8
        else: assert peak == (1 if mode == "reuse" else expected)
        assert (deleted, live) == ((0, 3) if mode == "leak" else (expected, 0))
        assert failures == (1 if mode == "limit" else 0)
        assert "caller=" + str(fixture) in trace
        assert log.stat().st_mode & 0o777 == 0o600
        print(f"TLS trace {mode}: {created} created, {deleted} deleted, {live} live, {failures} failures; native results/errno/destructors preserved.")
    for destination in ("", str(root / "absent/trace.log")):
        result = subprocess.run([str(fixture), "clean"], env=os.environ | {"LD_PRELOAD": str(library), "E2D_TLS_TRACE_FILE": destination}, capture_output=True)
        assert result.returncode == 126, result
    print("TLS trace rejects missing/unavailable output instead of losing diagnostics.")
