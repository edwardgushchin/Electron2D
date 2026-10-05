#include <setjmp.h>
#include <stdio.h>

int main(void) {
    jmp_buf buffer;
    int value = setjmp(buffer);
    if (value == 0) longjmp(buffer, 42);
    if (value != 42) return 1;
    puts("SDK-matched Wasm setjmp/longjmp returns 42 through the native exception ABI.");
    return 0;
}
