/* Independent ALSA virtual MIDI source for the public input consumer tests. */
#include <alsa/asoundlib.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
_Static_assert(sizeof(snd_seq_event_t) == 28, "legacy event size");
_Static_assert(offsetof(snd_seq_event_t, data) == 16, "legacy union offset");
int main(int argc, char **argv) {
    snd_seq_t *seq = NULL;
    snd_midi_event_t *parser = NULL;
    if (argc != 2 || snd_seq_open(&seq, "default", SND_SEQ_OPEN_OUTPUT, 0) < 0) return 2;
    snd_seq_set_client_name(seq, argv[1]);
    int port = snd_seq_create_simple_port(seq, "source", SND_SEQ_PORT_CAP_READ | SND_SEQ_PORT_CAP_SUBS_READ,
        SND_SEQ_PORT_TYPE_MIDI_GENERIC | SND_SEQ_PORT_TYPE_APPLICATION);
    if (port < 0 || snd_midi_event_new(256, &parser) < 0) { snd_seq_close(seq); return 3; }
    puts("READY"); fflush(stdout);
    char line[1024]; int failed = 0;
    while (fgets(line, sizeof line, stdin)) {
        if (strncmp(line, "quit", 4) == 0) break;
        int lost = 0;
        if (strncmp(line, "invalid", 7) == 0) {
            snd_seq_event_t e; snd_seq_ev_clear(&e); snd_seq_ev_set_noteon(&e, 16, 60, 100);
            snd_seq_ev_set_source(&e, port); snd_seq_ev_set_subs(&e); snd_seq_ev_set_direct(&e);
            if (snd_seq_event_output_direct(seq, &e) < 0) failed = 6;
        } else if (strncmp(line, "flood", 5) == 0) {
            for (int i = 0; i < 20000; i++) {
                snd_seq_event_t e; snd_seq_ev_clear(&e); snd_seq_ev_set_noteon(&e, 0, 60, 100);
                snd_seq_ev_set_source(&e, port); snd_seq_ev_set_subs(&e); snd_seq_ev_set_direct(&e);
                if (snd_seq_event_output_direct(seq, &e) < 0) lost++;
            }
        } else {
            char *at = line, *end;
            while (*at) {
                unsigned long byte = strtoul(at, &end, 16);
                if (at == end) break;
                if (byte > 255) { failed = 4; break; }
                snd_seq_event_t e; snd_seq_ev_clear(&e);
                int result = snd_midi_event_encode_byte(parser, (int)byte, &e);
                if (result < 0) { failed = 5; break; }
                if (result == 1) {
                    snd_seq_ev_set_source(&e, port); snd_seq_ev_set_subs(&e); snd_seq_ev_set_direct(&e);
                    if (snd_seq_event_output_direct(seq, &e) < 0) { failed = 6; break; }
                }
                at = end;
            }
        }
        printf("%s %d\n", failed ? "FAILED" : "OK", lost); fflush(stdout);
        if (failed) break;
    }
    snd_midi_event_free(parser); snd_seq_close(seq);
    return failed;
}
