#include "bridge.h"
#include "memory.h"
#include <limits.h>
#include <math.h>
#include <stdlib.h>
#include <string.h>
#include <ogg/ogg.h>
#include <theora/theoradec.h>
#include <vorbis/codec.h>

typedef struct { ogg_packet *packets; size_t count, capacity; } packet_list;
typedef struct {
    th_info ti;
    th_comment tc;
    th_setup_info *setup;
    th_dec_ctx *video;
    vorbis_info vi;
    vorbis_comment vc;
    vorbis_dsp_state audio;
    vorbis_block block;
    int audio_init, block_init;
    packet_list videos, audios;
    int64_t video_cursor, audio_cursor;
    uint8_t *rgba;
    e2d_video_info info;
} video_reader;
typedef struct { ogg_stream_state state; int kind, headers; } input_stream;

static void clear_packets(packet_list *list) {
    for (size_t i = 0; i < list->count; i++) e2d_ogv_release(list->packets[i].packet);
    e2d_ogv_release(list->packets);
}
void e2d_video_destroy(void *context) {
    video_reader *r = context;
    if (!r) return;
    if (r->block_init) vorbis_block_clear(&r->block);
    if (r->audio_init) vorbis_dsp_clear(&r->audio);
    vorbis_comment_clear(&r->vc); vorbis_info_clear(&r->vi);
    if (r->video) th_decode_free(r->video);
    if (r->setup) th_setup_free(r->setup);
    th_comment_clear(&r->tc); th_info_clear(&r->ti);
    clear_packets(&r->videos); clear_packets(&r->audios);
    e2d_ogv_release(r->rgba); e2d_ogv_release(r);
}
static int append_packet(packet_list *list, const ogg_packet *packet) {
    if (packet->bytes < 0 || list->count >= 16777216) return -1;
    if (list->count == list->capacity) {
        size_t capacity = list->capacity ? list->capacity * 2 : 64;
        void *replacement = e2d_ogv_resize(list->packets, capacity * sizeof(ogg_packet));
        if (!replacement) return -2;
        list->packets = replacement; list->capacity = capacity;
    }
    uint8_t *data = e2d_ogv_allocate(packet->bytes ? (size_t)packet->bytes : 1);
    if (!data) return -2;
    if (packet->bytes) memcpy(data, packet->packet, (size_t)packet->bytes);
    list->packets[list->count] = *packet;
    list->packets[list->count++].packet = data;
    return 0;
}
static int parse(video_reader *r, const uint8_t *data, size_t bytes, int track) {
    ogg_sync_state sync;
    input_stream streams[64];
    int count = 0, video_selected = 0, video_headers = 0, audio_headers = 0, status = -1;
    memset(streams, 0, sizeof(streams));
    if (ogg_sync_init(&sync)) return -2;
    char *buffer = ogg_sync_buffer(&sync, (long)bytes);
    if (!buffer) { ogg_sync_clear(&sync); return -2; }
    memcpy(buffer, data, bytes); ogg_sync_wrote(&sync, (long)bytes);
    ogg_page page;
    int available;
    while ((available = ogg_sync_pageout(&sync, &page)) > 0) {
        int serial = ogg_page_serialno(&page), index = 0;
        while (index < count && streams[index].state.serialno != serial) index++;
        if (index == count) {
            if (!ogg_page_bos(&page) || count == 64) goto done;
            if (ogg_stream_init(&streams[count].state, serial)) { status = -2; goto done; }
            count++;
        } else if (ogg_page_bos(&page)) goto done;
        input_stream *s = &streams[index];
        uint64_t epoch = e2d_ogv_failure_epoch();
        if (ogg_stream_pagein(&s->state, &page)) {
            if (e2d_ogv_failure_epoch() != epoch) status = -2;
            goto done;
        }
        ogg_packet packet;
        int packet_status;
        while ((packet_status = ogg_stream_packetout(&s->state, &packet)) > 0) {
            if (!s->kind) {
                s->kind = -1;
                if (!video_selected && packet.bytes >= 7 && packet.packet[0] == 0x80 && !memcmp(packet.packet + 1, "theora", 6)) {
                    s->kind = 1; video_selected = 1;
                } else if (packet.bytes >= 7 && packet.packet[0] == 1 && !memcmp(packet.packet + 1, "vorbis", 6)) {
                    if (r->info.audio_tracks == track) s->kind = 2;
                    r->info.audio_tracks++;
                }
            }
            if (s->kind < 0) continue;
            if (s->headers < 3) {
                if (s->kind == 1) {
                    if (th_decode_headerin(&r->ti, &r->tc, &r->setup, &packet) <= 0) goto done;
                    video_headers = ++s->headers;
                } else {
                    if (vorbis_synthesis_headerin(&r->vi, &r->vc, &packet)) goto done;
                    audio_headers = ++s->headers;
                }
            } else {
                int appended = append_packet(s->kind == 1 ? &r->videos : &r->audios, &packet);
                if (appended) { status = appended; goto done; }
            }
        }
        if (packet_status < 0) { status = -1; goto done; }
    }
    status = -1;
    if (available < 0 || sync.fill != sync.returned || video_headers != 3 ||
        (audio_headers && audio_headers != 3)) goto done;
    for (int i = 0; i < count; i++)
        if (streams[i].kind > 0 && streams[i].state.lacing_packet != streams[i].state.lacing_fill) goto done;
    status = 0;
done:
    for (int i = 0; i < count; i++) ogg_stream_clear(&streams[i].state);
    ogg_sync_clear(&sync);
    return status;
}
static uint8_t pixel(int value) { return (uint8_t)(value < 0 ? 0 : value > 255 ? 255 : value); }
static int copy_frame(video_reader *r) {
    th_ycbcr_buffer planes;
    if (th_decode_ycbcr_out(r->video, planes)) return -1;
    int sx = r->ti.pixel_fmt != TH_PF_444, sy = r->ti.pixel_fmt == TH_PF_420;
    for (int y = 0; y < r->info.height; y++) {
        int py = y + (int)r->ti.pic_y;
        uint8_t *out = r->rgba + (size_t)y * r->info.width * 4;
        const uint8_t *luma = planes[0].data + py * planes[0].stride;
        const uint8_t *u = planes[1].data + (py >> sy) * planes[1].stride;
        const uint8_t *v = planes[2].data + (py >> sy) * planes[2].stride;
        for (int x = 0; x < r->info.width; x++) {
            int px = x + (int)r->ti.pic_x;
            int c = (int)luma[px] - 16, d = (int)u[px >> sx] - 128, e = (int)v[px >> sx] - 128;
            out[x * 4] = pixel((298 * c + 409 * e + 128) >> 8);
            out[x * 4 + 1] = pixel((298 * c - 100 * d - 208 * e + 128) >> 8);
            out[x * 4 + 2] = pixel((298 * c + 516 * d + 128) >> 8);
            out[x * 4 + 3] = 255;
        }
    }
    return 0;
}
int e2d_video_frame(void *context, int64_t frame, const uint8_t **rgba) {
    video_reader *r = context;
    if (!r || !rgba || frame < 0 || (uint64_t)frame >= r->videos.count) return -1;
    *rgba = r->rgba;
    if (frame == r->video_cursor) return 0;
    if (frame < r->video_cursor) {
        ogg_int64_t granule = 0;
        if (th_decode_ctl(r->video, TH_DECCTL_SET_GRANPOS, &granule, sizeof(granule))) return -1;
        r->video_cursor = -1;
    }
    while (r->video_cursor < frame) {
        ogg_packet *packet = &r->videos.packets[++r->video_cursor];
        ogg_int64_t granule;
        int status = th_decode_packetin(r->video, packet, &granule);
        if (status != 0 && status != TH_DUPFRAME) return -1;
    }
    return copy_frame(r) ? -1 : 1;
}
int e2d_video_audio(void *context, float *pcm, int frames) {
    video_reader *r = context;
    if (!r || frames < 0 || (frames && !pcm)) return -1;
    if (!r->audio_init || !frames) return 0;
    int written = 0;
    while (written < frames) {
        float **channels;
        int available = vorbis_synthesis_pcmout(&r->audio, &channels);
        if (available > 0) {
            int take = available < frames - written ? available : frames - written;
            for (int i = 0; i < take; i++) for (int channel = 0; channel < r->info.channels; channel++) {
                float sample = channels[channel][i];
                if (!isfinite(sample)) return -1;
                pcm[(size_t)(written + i) * r->info.channels + channel] = sample;
            }
            if (vorbis_synthesis_read(&r->audio, take)) return -1;
            written += take;
        } else {
            if ((uint64_t)r->audio_cursor >= r->audios.count) break;
            ogg_packet *packet = &r->audios.packets[r->audio_cursor++];
            if (vorbis_synthesis(&r->block, packet) || vorbis_synthesis_blockin(&r->audio, &r->block)) return -1;
        }
    }
    return written;
}
int e2d_video_seek_audio(void *context, int64_t frame) {
    video_reader *r = context;
    if (!r || frame < 0) return -1;
    if (!r->audio_init) return 0;
    if (vorbis_synthesis_restart(&r->audio)) return -1;
    r->audio_cursor = 0;
    float scratch[256 * 8];
    while (frame > 0) {
        int count = e2d_video_audio(r, scratch, frame > 256 ? 256 : (int)frame);
        if (count < 0) return count;
        if (!count) break;
        frame -= count;
    }
    return 0;
}
int e2d_video_create(const uint8_t *data, size_t bytes, int track, e2d_video_info *info, void **result) {
    if (result) *result = NULL;
    if (!data || !bytes || bytes > 536870912 || bytes > LONG_MAX || track < -1 || !info || !result) return -1;
    if (!e2d_ogv_memory_ready()) return -5;
    video_reader *r = e2d_ogv_zero(1, sizeof(*r));
    if (!r) return -2;
    th_info_init(&r->ti); th_comment_init(&r->tc);
    vorbis_info_init(&r->vi); vorbis_comment_init(&r->vc);
    int status = parse(r, data, bytes, track);
    if (status) goto failed;
    if (!r->ti.pic_width || !r->ti.pic_height || r->ti.pic_width > 16384 || r->ti.pic_height > 16384 ||
        r->ti.frame_width > 16384 || r->ti.frame_height > 16384 ||
        !r->ti.fps_numerator || !r->ti.fps_denominator || r->ti.fps_numerator > INT_MAX || r->ti.fps_denominator > INT_MAX ||
        r->ti.pixel_fmt == TH_PF_RSVD) { status = -3; goto failed; }
    r->info.width = r->ti.pic_width; r->info.height = r->ti.pic_height;
    r->info.fps_numerator = r->ti.fps_numerator; r->info.fps_denominator = r->ti.fps_denominator;
    r->info.video_frames = (int64_t)r->videos.count;
    r->info.duration = (double)r->videos.count * r->ti.fps_denominator / r->ti.fps_numerator;
    r->video = th_decode_alloc(&r->ti, r->setup);
    if (!r->video) { status = -2; goto failed; }
    th_setup_free(r->setup); r->setup = NULL;
    r->rgba = e2d_ogv_zero((size_t)r->info.width * r->info.height, 4);
    if (!r->rgba) { status = -2; goto failed; }
    r->video_cursor = -1;
    if (r->vi.channels) {
        if (r->vi.channels > 8 || r->vi.rate <= 0 || r->vi.rate > INT_MAX) { status = -3; goto failed; }
        r->info.channels = r->vi.channels; r->info.rate = (int)r->vi.rate;
        if (vorbis_synthesis_init(&r->audio, &r->vi)) { status = -1; goto failed; }
        r->audio_init = 1;
        if (vorbis_block_init(&r->audio, &r->block)) { status = -1; goto failed; }
        r->block_init = 1;
        for (size_t i = 0; i < r->audios.count; i++)
            if (r->audios.packets[i].granulepos > r->info.audio_frames) r->info.audio_frames = r->audios.packets[i].granulepos;
        double duration = (double)r->info.audio_frames / r->info.rate;
        if (duration > r->info.duration) r->info.duration = duration;
    }
    /* Validate every packet and reserve codec working peaks before prepared replay. */
    const uint8_t *pixels;
    if (r->videos.count && e2d_video_frame(r, (int64_t)r->videos.count - 1, &pixels) < 0) { status = -1; goto failed; }
    float scratch[256 * 8];
    do { status = e2d_video_audio(r, scratch, 256); } while (status > 0);
    if (status < 0 || e2d_video_seek_audio(r, 0)) { status = -1; goto failed; }
    if (r->videos.count && e2d_video_frame(r, 0, &pixels) < 0) { status = -1; goto failed; }
    *info = r->info; *result = r;
    return 0;
failed:
    e2d_video_destroy(r);
    return status;
}
