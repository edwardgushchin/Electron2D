/* Copyright 2020 The Emscripten Authors. All rights reserved.
 * Bounded .NET 10 ABI adaptation from Emscripten 3.1.57 emscripten_setjmp.c.
 * Used under the MIT license in licence/Emscripten-LICENSE.txt. */
#include <assert.h>
#include <stdint.h>
#include <setjmp.h>

struct WasmLongjmpArgs { void *env; int val; };
struct JumpBuffer { void *invocation; uint32_t label; struct WasmLongjmpArgs arg; };
_Static_assert(sizeof(struct JumpBuffer) <= sizeof(jmp_buf), "Wasm jump buffer ABI size");

void __wasm_setjmp(void *env, uint32_t label, void *invocation) {
    struct JumpBuffer *buffer = env;
    assert(label != 0 && invocation != 0);
    buffer->invocation = invocation;
    buffer->label = label;
}

uint32_t __wasm_setjmp_test(void *env, void *invocation) {
    struct JumpBuffer *buffer = env;
    assert(buffer->label != 0 && invocation != 0);
    return buffer->invocation == invocation ? buffer->label : 0;
}
