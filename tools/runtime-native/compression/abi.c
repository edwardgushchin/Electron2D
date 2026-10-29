#include "compression_memory.h"
__attribute__((visibility("default")))
uint32_t e2d_compression_memory_abi(void) { return e2d_memory_abi() == 1 ? 1 : 0; }
