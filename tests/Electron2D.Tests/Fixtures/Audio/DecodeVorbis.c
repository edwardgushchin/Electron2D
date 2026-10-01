#include <stdio.h>
#include <vorbis/vorbisfile.h>

/* Independent fixture decoder: native libvorbisfile, interleaved host float PCM. */
int main(int argc, char **argv) {
    if (argc != 2) return 2;
    OggVorbis_File file;
    if (ov_fopen(argv[1], &file)) return 3;
    float **pcm;
    int section;
    long count;
    while ((count = ov_read_float(&file, &pcm, 4096, &section)) > 0) {
        int channels = ov_info(&file, section)->channels;
        for (long frame = 0; frame < count; frame++)
            for (int channel = 0; channel < channels; channel++)
                if (fwrite(&pcm[channel][frame], sizeof(float), 1, stdout) != 1) return 4;
    }
    ov_clear(&file);
    return count < 0 ? 5 : 0;
}
