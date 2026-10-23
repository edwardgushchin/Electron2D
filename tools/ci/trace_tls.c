// SPDX-License-Identifier: MIT
#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <execinfo.h>
#include <fcntl.h>
#include <limits.h>
#include <pthread.h>
#include <stdatomic.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

typedef int (*Create)(pthread_key_t *, void (*)(void *));
typedef int (*Delete)(pthread_key_t);
static _Atomic(Create) create_function;
static _Atomic(Delete) delete_function;
static _Atomic unsigned char tracked[4096];
static _Atomic unsigned created, deleted, live, peak, failures, untracked;
static atomic_flag failure_stack = ATOMIC_FLAG_INIT;
static _Atomic int output_descriptor = -1;

static int output(void)
{
    int descriptor = atomic_load(&output_descriptor);
    if (descriptor < 0) {
        const char *path = getenv("E2D_TLS_TRACE_FILE");
        if (!path || !*path) _exit(126);
        descriptor = open(path, O_WRONLY | O_CREAT | O_APPEND | O_CLOEXEC, 0600);
        if (descriptor < 0) _exit(126);
        int expected = -1;
        if (!atomic_compare_exchange_strong(&output_descriptor, &expected, descriptor)) {
            close(descriptor);
            descriptor = expected;
        }
    }
    return descriptor;
}

static void emit(const char *operation, unsigned key, int result, void *caller)
{
    Dl_info info = {0};
    dladdr(caller, &info);
    char line[1024];
    int length = snprintf(line, sizeof(line),
        "E2D_TLS pid=%ld %s key=%u result=%d live=%u caller=%s+0x%lx\n",
        (long)getpid(), operation, key, result, atomic_load(&live),
        info.dli_fname ? info.dli_fname : "?", (unsigned long)((uintptr_t)caller - (uintptr_t)info.dli_fbase));
    if (length > 0) {
        ssize_t written = write(output(), line, (size_t)length < sizeof(line) ? (size_t)length : sizeof(line) - 1);
        (void)written;
    }
}

int pthread_key_create(pthread_key_t *key, void (*destructor)(void *))
{
    int initial_errno = errno;
    Create function = atomic_load(&create_function);
    if (!function) {
        function = (Create)dlsym(RTLD_NEXT, "pthread_key_create");
        if (!function) _exit(127);
        atomic_store(&create_function, function);
    }
    errno = initial_errno;
    int result = function(key, destructor), saved_errno = errno;
    if (!result) {
        atomic_fetch_add(&created, 1);
        if (*key < sizeof(tracked) / sizeof(tracked[0])) {
            if (!atomic_exchange(&tracked[*key], 1)) {
                unsigned count = atomic_fetch_add(&live, 1) + 1, maximum = atomic_load(&peak);
                while (count > maximum && !atomic_compare_exchange_weak(&peak, &maximum, count)) { }
            }
        } else atomic_fetch_add(&untracked, 1);
    } else atomic_fetch_add(&failures, 1);
    emit("create", result ? UINT_MAX : *key, result, __builtin_extract_return_addr(__builtin_return_address(0)));
    if (result && !atomic_flag_test_and_set(&failure_stack)) {
        void *frames[24];
        int count = backtrace(frames, 24);
        backtrace_symbols_fd(frames, count, output());
    }
    errno = saved_errno;
    return result;
}

int pthread_key_delete(pthread_key_t key)
{
    int initial_errno = errno;
    Delete function = atomic_load(&delete_function);
    if (!function) {
        function = (Delete)dlsym(RTLD_NEXT, "pthread_key_delete");
        if (!function) _exit(127);
        atomic_store(&delete_function, function);
    }
    errno = initial_errno;
    int result = function(key), saved_errno = errno;
    if (!result && key < sizeof(tracked) / sizeof(tracked[0]) && atomic_exchange(&tracked[key], 0)) {
        atomic_fetch_add(&deleted, 1);
        atomic_fetch_sub(&live, 1);
    }
    emit("delete", key, result, __builtin_extract_return_addr(__builtin_return_address(0)));
    errno = saved_errno;
    return result;
}

__attribute__((destructor)) static void summary(void)
{
    char line[256];
    int length = snprintf(line, sizeof(line),
        "E2D_TLS pid=%ld summary created=%u deleted=%u live=%u peak=%u failures=%u untracked=%u\n",
        (long)getpid(), atomic_load(&created), atomic_load(&deleted), atomic_load(&live),
        atomic_load(&peak), atomic_load(&failures), atomic_load(&untracked));
    if (length > 0) { ssize_t written = write(output(), line, (size_t)length); (void)written; }
}
