#include <speech-dispatcher/libspeechd.h>
#include <stdio.h>

int main(int argc, char** argv) {
    if (argc != 2) return 2;
    SPDConnection* connection = spd_open("Electron2DInspector", "voices", NULL, SPD_MODE_SINGLE);
    if (!connection) return 3;
    SPDVoice** voices = spd_list_synthesis_voices(connection);
    if (!voices) { spd_close(connection); return 4; }
    FILE* file = fopen(argv[1], "wb");
    if (!file) { free_spd_voices(voices); spd_close(connection); return 5; }
    for (size_t index = 0; voices[index]; index++) {
        SPDVoice* voice = voices[index];
        fprintf(file, "%s|%s|%s_%s\n", voice->name, voice->name, voice->language, voice->variant);
    }
    int error = ferror(file); fclose(file); free_spd_voices(voices); spd_close(connection);
    return error ? 6 : 0;
}
