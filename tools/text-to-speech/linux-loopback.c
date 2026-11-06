#include <pulse/simple.h>
#include <pulse/error.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <signal.h>
#include <time.h>
#include <unistd.h>
#include <math.h>

static void marker(const char* path) { FILE* file = fopen(path, "wb"); if (file) fclose(file); }
int main(int argc, char** argv) {
    if (argc != 3) return 2;
    const char* source = getenv("ELECTRON2D_TTS_PULSE_SOURCE");
    if (!source || !*source) { fprintf(stderr, "An explicit private Pulse monitor source is required.\n"); return 3; }
    int pid = atoi(argv[1]); if (pid <= 0 || kill(pid, 0)) return 4;
    char pcm[4096], ready[4096], stop[4096], metadata[4096];
    if (snprintf(pcm, sizeof pcm, "%s.pcm", argv[2]) >= sizeof pcm ||
        snprintf(ready, sizeof ready, "%s.ready", argv[2]) >= sizeof ready ||
        snprintf(stop, sizeof stop, "%s.stop", argv[2]) >= sizeof stop ||
        snprintf(metadata, sizeof metadata, "%s.txt", argv[2]) >= sizeof metadata) return 5;
    const pa_sample_spec format = {PA_SAMPLE_S16LE, 44100, 2}; int error = 0;
    const pa_buffer_attr buffering = {(uint32_t)-1, (uint32_t)-1, (uint32_t)-1, (uint32_t)-1, 441 * 4};
    pa_simple* capture = pa_simple_new(NULL, "Electron2D speech inspector", PA_STREAM_RECORD, source, "Private native speech PCM", &format, NULL, &buffering, &error);
    if (!capture) { fprintf(stderr, "%s\n", pa_strerror(error)); return 6; }
    FILE* file = fopen(pcm, "wb"); if (!file) { pa_simple_free(capture); return 7; }
    marker(ready); const time_t started = time(NULL); int result = 0, peak = 0; uint64_t count = 0, nonzero = 0; double energy = 0;
    while (time(NULL) - started < 20 && access(stop, F_OK) && !kill(pid, 0)) {
        int16_t samples[882];
        if (pa_simple_read(capture, samples, sizeof samples, &error) < 0) { fprintf(stderr, "%s\n", pa_strerror(error)); result = 8; break; }
        if (fwrite(samples, 1, sizeof samples, file) != sizeof samples) { result = 9; break; }
        for (size_t index = 0; index < 882; index++) { int value = samples[index]; int magnitude = abs(value); if (magnitude > peak) peak = magnitude; energy += (double)value * value; nonzero += value != 0; }
        count += 882;
    }
    fclose(file); pa_simple_free(capture); file = fopen(metadata, "wb"); if (!file) return 10;
    fprintf(file, "target_pid=%d\nsource=%s\nrate=44100 channels=2 bits=16\nframes=%llu\nnonzero=%llu\npeak=%d\nrms=%.8f\n", pid, source, (unsigned long long)(count / 2), (unsigned long long)nonzero, peak, count ? sqrt(energy / count) : 0);
    if (ferror(file)) result = 11; fclose(file); return result;
}
