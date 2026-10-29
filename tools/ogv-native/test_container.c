#include "bridge.h"
#include "memory.h"
#include "../enet-native/memory.h"
#include <ogg/ogg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static void check(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(1); } }
static uint8_t packet_bytes[512 * 1024], stream_bytes[600 * 1024];
static int exercise(void) {
    ogg_stream_state encoder = {0}, decoder = {0}; ogg_sync_state sync = {0};
    int status = -1; size_t bytes = 0, position = 0; int packets = 0;
    if (ogg_stream_init(&encoder, 1234) || ogg_stream_init(&decoder, 1234) || ogg_sync_init(&sync)) goto done;
    ogg_packet input = {packet_bytes, sizeof(packet_bytes), 1, 1, 999, 0};
    if (ogg_stream_packetin(&encoder, &input)) goto done;
    ogg_page page; int available;
    while ((available = ogg_stream_flush(&encoder, &page)) > 0) {
        size_t length = (size_t)page.header_len + page.body_len;
        check(bytes + length <= sizeof(stream_bytes), "Caller stream storage exceeded");
        memcpy(stream_bytes + bytes, page.header, (size_t)page.header_len); bytes += page.header_len;
        memcpy(stream_bytes + bytes, page.body, (size_t)page.body_len); bytes += page.body_len;
    }
    if (available < 0) goto done;
    while (position < bytes) {
        size_t take = bytes - position > 500 ? 500 : bytes - position;
        char *buffer = ogg_sync_buffer(&sync, (long)take);
        if (!buffer) goto done;
        memcpy(buffer, stream_bytes + position, take); position += take;
        if (ogg_sync_wrote(&sync, (long)take)) goto done;
        while ((available = ogg_sync_pageout(&sync, &page)) > 0) {
            if (ogg_stream_pagein(&decoder, &page)) goto done;
            ogg_packet output; int found;
            while ((found = ogg_stream_packetout(&decoder, &output)) > 0) {
                check(!packets++ && output.bytes == sizeof(packet_bytes) && output.e_o_s && output.b_o_s &&
                    output.granulepos == 999 && !memcmp(output.packet, packet_bytes, sizeof(packet_bytes)), "Container altered packet data or metadata");
            }
            if (found < 0) goto done;
        }
        if (available < 0) goto done;
    }
    check(packets == 1, "Incomplete container packet"); status = 0;
done:
    ogg_stream_clear(&encoder); ogg_stream_clear(&decoder); ogg_sync_clear(&sync);
    return status;
}
static void *thread_refusal(void *unused) {
    (void)unused;
    check(e2d_ogv_failure_epoch() == 0, "New thread inherited a refusal epoch");
    e2d_test_arm(1); check(exercise() < 0 && e2d_ogv_failure_epoch() == 1, "Worker refusal was not recorded");
    return NULL;
}
int main(int argc, char **argv) {
    check(argc == 2, "Expected failure position");
    for (size_t i = 0; i < sizeof(packet_bytes); ++i) packet_bytes[i] = (uint8_t)(i * 77U + i / 7);
    E2DMemoryFunctions memory; e2d_memory_functions(&memory); check(!e2d_ogv_memory(&memory), "Registration failed");
    size_t position = (size_t)strtoull(argv[1], NULL, 10);
    e2d_test_arm(position); uint64_t epoch = e2d_ogv_failure_epoch();
    int status = exercise(); size_t attempts = e2d_test_attempts();
    E2DMemorySnapshot state; e2d_memory_snapshot(&state);
    check(status == (position ? -1 : 0), "Container did not report allocation refusal");
    check((e2d_ogv_failure_epoch() != epoch) == !!position && state.failures == !!position, "Container refusal epoch is absent");
    check(!state.nativeCurrent && state.allocated == state.retired, "Container retained an allocation");
    e2d_test_arm(0); check(!exercise(), "Container retry failed"); e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated == state.retired, "Container retry did not drain");
    if (!position) {
        uint64_t parent = e2d_ogv_failure_epoch(); pthread_t thread;
        check(!pthread_create(&thread, NULL, thread_refusal, NULL) && !pthread_join(thread, NULL), "Worker lifetime failed");
        check(e2d_ogv_failure_epoch() == parent, "Worker refusal changed the parent epoch");
        e2d_memory_snapshot(&state); check(!state.nativeCurrent && state.allocated == state.retired, "Worker did not drain");
    }
    printf("{\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n", attempts, position, (unsigned long long)state.nativePeak);
    return 0;
}
