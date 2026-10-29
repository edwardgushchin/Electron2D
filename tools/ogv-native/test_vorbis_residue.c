#include <vorbis/vorbisenc.h>
#include <stdio.h>
#include <stdlib.h>
#include "bridge.h"
int main(void) {
    E2DMemoryFunctions memory = {malloc, calloc, realloc, free};
    if(e2d_ogv_memory(&memory)) return 2;
    const float qualities[] = {-.1f, .5f, 1.f};
    for (int channels = 1; channels <= 8; channels++) for (int q = 0; q < 3; q++) {
        vorbis_info info; vorbis_info_init(&info);
        int status = vorbis_encode_init_vbr(&info, channels, 48000, qualities[q]);
        vorbis_info_clear(&info);
        if (status) { fprintf(stderr, "Vorbis init failed: %d channels, quality %g\n", channels, qualities[q]); return 1; }
    }
    puts("Vorbis residue initialization/clear: 24 profiles passed"); return 0;
}
