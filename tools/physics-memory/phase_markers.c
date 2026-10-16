#include <stdint.h>
#include <stdlib.h>

__attribute__((visibility("default"), noinline)) uint32_t e2d_memory_phase(uint32_t phase)
{
    if (phase > 6)
        return 0;
    uint32_t size = 17 + phase;
    volatile unsigned char *bytes = malloc(size);
    if (!bytes)
        return 0;
    bytes[0] = (unsigned char)phase;
    bytes[size - 1] = (unsigned char)phase;
    free((void *)bytes);
    return size;
}

#ifdef MEMORY_PHASE_CALIBRATION
#include <stdio.h>

int main(void)
{
    volatile unsigned char *held = malloc(4096), *temporary;
    if (!held)
        return 1;
    held[0] = 1;
    if (e2d_memory_phase(0) != 17)
        return 1;
    temporary = malloc(8192);
    if (!temporary)
        return 1;
    temporary[0] = 2;
    if (e2d_memory_phase(1) != 18)
        return 1;
    free((void *)temporary);
    if (e2d_memory_phase(2) != 19)
        return 1;
    free((void *)held);
    for (uint32_t phase = 3; phase <= 6; phase++)
        if (e2d_memory_phase(phase) != 17 + phase)
            return 1;
    if (e2d_memory_phase(7) || e2d_memory_phase(UINT32_MAX))
        return 1;
    puts("{\"Markers\":7,\"MarkerBytes\":140,\"InvalidPhasesRejected\":true}");
    return 0;
}
#endif
