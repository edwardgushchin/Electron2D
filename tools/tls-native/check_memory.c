#define _GNU_SOURCE
#include "memory.h"
#undef NDEBUG
#include <assert.h>
#include <dlfcn.h>
#include <openssl/ssl.h>
#include <openssl/err.h>
#include <openssl/evp.h>
#include <pthread.h>
#include <stdio.h>
#include <unistd.h>

static void (*arm)(size_t);
static size_t (*attempts)(void);
static E2DMemorySnapshot snapshot(void) { E2DMemorySnapshot state; e2d_memory_snapshot(&state); return state; }
static void protection(uintptr_t address, const char *expected) {
    FILE *maps = fopen("/proc/self/maps", "r"); assert(maps);
    char line[2048], permissions[5]; unsigned long first, last;
    while (fgets(line, sizeof(line), maps)) {
        if (sscanf(line, "%lx-%lx %4s", &first, &last, permissions) == 3 && first <= address && address < last) {
            assert(!strcmp(permissions, expected)); fclose(maps); return;
        }
    }
    fclose(maps); abort();
}
static void drained(void) {
    E2DMemorySnapshot state = snapshot();
    if (state.current || state.allocated != state.retired) {
        fprintf(stderr, "Unretired storage: %lu (%lu allocated, %lu retired)\n", state.current, state.allocated, state.retired);
        abort();
    }
}
static void primitives(void) {
    assert(!OPENSSL_malloc(0));
    unsigned char *p = OPENSSL_zalloc(73); assert(p && !p[0] && !p[72]);
    uint64_t current = snapshot().nativeCurrent;
    p[0] = 41; p[72] = 59;
    unsigned char *next = OPENSSL_realloc(p, 1001); assert(next && next[0] == 41 && next[72] == 59); p = next;
    assert(snapshot().nativeCurrent == current + 928);
    E2DMemorySnapshot stable = snapshot();
    next = OPENSSL_realloc(p, SIZE_MAX); assert(!next && p[0] == 41 && p[72] == 59);
    assert(snapshot().nativeCurrent >= stable.nativeCurrent); /* Error state can be allocated on refusal. */
    assert(!OPENSSL_realloc(p, 0));
    for (size_t a = 1; a <= 65536; a *= 2) {
        void *freeptr = NULL;
        p = CRYPTO_aligned_alloc(93, a, &freeptr, __FILE__, __LINE__);
        assert(p && freeptr && (uintptr_t)p % a == 0); memset(p, 61, 93); OPENSSL_free(freeptr);
    }
    stable = snapshot();
    assert(e2d_ssl_mmap(NULL, 0, PROT_READ, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0) == MAP_FAILED);
    assert(snapshot().current == stable.current && snapshot().peak == stable.peak && snapshot().failures == stable.failures + 1);
}
static int operation(const char *name) {
    if (!strcmp(name, "buffer")) {
        unsigned char *p = OPENSSL_zalloc(73); if (!p) return 0;
        p[0] = 41; p[72] = 59;
        unsigned char *next = OPENSSL_realloc(p, 1001);
        if (!next) { assert(p[0] == 41 && p[72] == 59); OPENSSL_clear_free(p, 73); return 0; }
        assert(next[0] == 41 && next[72] == 59); OPENSSL_clear_free(next, 1001); return 1;
    }
    if (!strcmp(name, "aligned")) {
        void *freeptr = NULL;
        unsigned char *p = CRYPTO_aligned_alloc(93, 4096, &freeptr, __FILE__, __LINE__);
        if (!p) { assert(!freeptr); return 0; }
        assert(freeptr && (uintptr_t)p % 4096 == 0); memset(p, 61, 93); OPENSSL_free(freeptr); return 1;
    }
    if (!strcmp(name, "secure")) {
        uint64_t before = snapshot().nativeCurrent;
        int result = CRYPTO_secure_malloc_init(65536, 32);
        if (!result) { assert(!CRYPTO_secure_malloc_initialized()); return 0; }
        assert(snapshot().nativeCurrent >= before + 65536 + 2 * (size_t)sysconf(_SC_PAGESIZE));
        uint64_t retained = snapshot().nativeCurrent;
        unsigned char *arena = OPENSSL_secure_zalloc(65536); assert(arena && CRYPTO_secure_actual_size(arena) == 65536);
        protection((uintptr_t)arena - 1, "---p"); protection((uintptr_t)arena, "rw-p");
        protection((uintptr_t)arena + 65536, "---p"); OPENSSL_secure_clear_free(arena, 65536);
        unsigned char *p = OPENSSL_secure_zalloc(73); assert(p && !p[72] && CRYPTO_secure_allocated(p));
        assert(snapshot().nativeCurrent == retained); /* The arena already owns this storage. */
        assert(!CRYPTO_secure_malloc_done()); OPENSSL_secure_clear_free(p, 73);
        assert(CRYPTO_secure_malloc_done() && !CRYPTO_secure_malloc_initialized()); return 1;
    }
    if (!strcmp(name, "context")) {
        SSL_CTX *ctx = SSL_CTX_new(TLS_method()); if (!ctx) return 0;
        SSL *ssl = SSL_new(ctx); if (!ssl) { SSL_CTX_free(ctx); return 0; }
        BIO *a = NULL, *b = NULL;
        int result = BIO_new_bio_pair(&a, 4096, &b, 4096);
        BIO_free(a); BIO_free(b); SSL_free(ssl); SSL_CTX_free(ctx); return result;
    }
    if (!strcmp(name, "digest")) {
        static const unsigned char expected[32] = {0xba,0x78,0x16,0xbf,0x8f,0x01,0xcf,0xea,0x41,0x41,0x40,0xde,0x5d,0xae,0x22,0x23,0xb0,0x03,0x61,0xa3,0x96,0x17,0x7a,0x9c,0xb4,0x10,0xff,0x61,0xf2,0x00,0x15,0xad};
        unsigned char output[EVP_MAX_MD_SIZE]; unsigned int size = 0;
        EVP_MD_CTX *ctx = EVP_MD_CTX_new(); if (!ctx) return 0;
        int result = EVP_DigestInit_ex(ctx, EVP_sha256(), NULL) && EVP_DigestUpdate(ctx, "abc", 3) && EVP_DigestFinal_ex(ctx, output, &size);
        if (result) assert(size == 32 && !memcmp(output, expected, 32));
        EVP_MD_CTX_free(ctx); return result;
    }
    abort();
}
static void *worker(void *unused) {
    (void)unused;
    for (int i = 0; i < 32; ++i) { assert(operation("context")); assert(operation("digest")); }
    OPENSSL_thread_stop(); return NULL;
}
static void *custom_malloc(size_t n, const char *file, int line) { (void)file; (void)line; return malloc(n); }
static void *custom_realloc(void *p, size_t n, const char *file, int line) { (void)file; (void)line; return realloc(p, n); }
static void custom_free(void *p, const char *file, int line) { (void)file; (void)line; free(p); }
int main(int argc, char **argv) {
    assert(argc >= 2 && OpenSSL_version_num() == 0x40100000L);
    if (!strcmp(argv[1], "custom")) {
        assert(CRYPTO_set_mem_functions(custom_malloc, custom_realloc, custom_free));
        void *p = OPENSSL_malloc(83); assert(p && !snapshot().nativeCurrent); OPENSSL_free(p); drained(); return 0;
    }
    if (!strcmp(argv[1], "early")) {
        OPENSSL_INIT_SETTINGS *settings = OPENSSL_INIT_new(); assert(settings);
        assert(OPENSSL_INIT_set_config_filename(settings, "/nonexistent/e2d-tls-test"));
        assert(OPENSSL_INIT_set_config_appname(settings, "e2d-test")); OPENSSL_INIT_free(settings);
        OPENSSL_cleanup(); drained(); return 0;
    }
    assert(OPENSSL_init_ssl(0, NULL));
    assert(operation("context") && operation("digest")); /* Warm lazy provider state before fault arming. */
    ERR_clear_error();
    if (!strcmp(argv[1], "exit")) { OPENSSL_cleanup(); return 0; }
    if (!strcmp(argv[1], "primitives")) primitives();
    else if (!strcmp(argv[1], "map-budget")) {
        E2DMemorySnapshot stable = snapshot();
        e2d_memory_lock(); assert(!e2d_memory_managed_locked(INT64_MAX - stable.nativeCurrent - 4096)); e2d_memory_unlock();
        assert(!CRYPTO_secure_malloc_init(65536, 32));
        assert(!CRYPTO_secure_malloc_initialized() && snapshot().failures == stable.failures + 1);
        e2d_memory_lock(); assert(!e2d_memory_managed_locked(0)); e2d_memory_unlock();
        assert(snapshot().nativeCurrent == stable.nativeCurrent && operation("secure"));
    }
    else if (!strcmp(argv[1], "threads")) {
        pthread_t workers[4];
        for (int i = 0; i < 4; ++i) assert(!pthread_create(&workers[i], NULL, worker, NULL));
        for (int i = 0; i < 4; ++i) assert(!pthread_join(workers[i], NULL));
    } else {
        size_t fail_at = argc > 2 ? strtoull(argv[2], NULL, 10) : 0;
        arm = dlsym(RTLD_DEFAULT, "e2d_test_arm"); attempts = dlsym(RTLD_DEFAULT, "e2d_test_attempts");
        if (arm) arm(fail_at);
        int result = operation(argv[1]);
        size_t calls = attempts ? attempts() : 0;
        if (arm) arm(0);
        if (!fail_at) assert(result);
        else { assert(snapshot().failures > 0); assert(operation(argv[1])); }
        printf("%s: result=%d attempts=%zu failure=%zu\n", argv[1], result, calls, fail_at);
    }
    ERR_clear_error(); OPENSSL_thread_stop(); OPENSSL_cleanup();
    printf("TLS retained until native destructor: %lu bytes.\n", snapshot().nativeCurrent);
}
