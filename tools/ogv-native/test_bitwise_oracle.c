#undef main
#include "bridge.h"
#include "../enet-native/memory.h"
#include <stdio.h>
int e2d_ogg_selftest_main(void);
int main(void) {
    E2DMemoryFunctions memory;e2d_memory_functions(&memory);
    if(e2d_ogv_memory(&memory))return 1;
    int result=e2d_ogg_selftest_main();
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    if(result||state.nativeCurrent||state.allocated!=state.retired||!state.allocations)return 2;
    printf("Pinned bitwise oracle: %llu allocations, peak %llu; full drain.\n",(unsigned long long)state.allocations,(unsigned long long)state.nativePeak);
    return 0;
}
