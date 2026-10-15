#include "bridge.h"
#include <theora/theoraenc.h>
#include <vorbis/vorbisenc.h>
#include <stdlib.h>
#include <string.h>
#include <limits.h>
#include <math.h>

typedef struct {
    ogg_stream_state stream;
    uint8_t bytes[65307];
    size_t header, length;
    double time, previous_time;
    int ready, initialized;
} e2d_ogv_stream;
typedef struct {
    th_enc_ctx *video;
    th_ycbcr_buffer planes;
    vorbis_info audio_info;
    vorbis_comment audio_comment;
    vorbis_dsp_state audio;
    vorbis_block block;
    int audio_initialized, block_initialized;
    e2d_ogv_stream v, a;
    e2d_ogv_writer writer;
    void *user;
    uint8_t *yuv;
    int width, height, frame_width, frame_height, channels, output_channels, has_frame, first_audio, finished, failed;
} e2d_ogv;

static int fail(e2d_ogv *c, int code) { c->failed = code; return code; }
static int write_bytes(e2d_ogv *c, const uint8_t *data, size_t length) {
    if (!length) return 0;
    return c->writer(data, length, c->user) ? 0 : fail(c, -4);
}
static int write_page(e2d_ogv *c, ogg_page *p) {
    if (write_bytes(c, p->header, (size_t)p->header_len)) return c->failed;
    return write_bytes(c, p->body, (size_t)p->body_len);
}
static int flush_headers(e2d_ogv *c, e2d_ogv_stream *s) {
    ogg_page page;
    int status;
    while ((status = ogg_stream_flush(&s->stream, &page)) > 0) if (write_page(c, &page)) return c->failed;
    return status < 0 ? fail(c, -3) : 0;
}
static int queue_page(e2d_ogv *c, e2d_ogv_stream *s, int video) {
    if (s->ready) return 0;
    ogg_page page; int status = ogg_stream_flush(&s->stream, &page);
    if (status < 0) return fail(c, -3);
    if (!status) return 0;
    s->header = (size_t)page.header_len; s->length = s->header + (size_t)page.body_len;
    if (s->length > sizeof(s->bytes)) return fail(c, -3);
    memcpy(s->bytes, page.header, s->header); memcpy(s->bytes + s->header, page.body, (size_t)page.body_len);
    ogg_int64_t granule = ogg_page_granulepos(&page);
    s->time = granule < 0 ? s->previous_time : video ? th_granule_time(c->video, granule) : vorbis_granule_time(&c->audio, granule);
    s->ready = 1; return 0;
}
static int mux(e2d_ogv *c, int final) {
    for (;;) {
        if (queue_page(c, &c->v, 1) || queue_page(c, &c->a, 0)) return c->failed;
        if ((!c->v.ready || !c->a.ready) && !final) return 0;
        if (!c->v.ready && !c->a.ready) return 0;
        e2d_ogv_stream *s = !c->a.ready || (c->v.ready && c->v.time <= c->a.time) ? &c->v : &c->a;
        if (write_bytes(c, s->bytes, s->length)) return c->failed;
        s->previous_time = s->time; s->ready = 0;
    }
}
static int pull_video(e2d_ogv *c, int last) {
    ogg_packet packet; int status;
    while ((status = th_encode_packetout(c->video, last, &packet)) > 0)
        if (ogg_stream_packetin(&c->v.stream, &packet)) return fail(c, -3);
    return status < 0 ? fail(c, -3) : 0;
}
static int pull_audio(e2d_ogv *c) {
    ogg_packet packet; int status;
    while ((status = vorbis_analysis_blockout(&c->audio, &c->block)) > 0) {
        if (vorbis_analysis(&c->block, NULL) || vorbis_bitrate_addblock(&c->block)) return fail(c, -3);
        while (vorbis_bitrate_flushpacket(&c->audio, &packet) > 0) {
            if (ogg_stream_packetin(&c->a.stream, &packet)) return fail(c, -3);
            // Isolate the zero-duration priming packet so short streams retain their exact initial clock.
            if (!c->first_audio) { c->first_audio = 1; if (flush_headers(c, &c->a)) return c->failed; }
        }
    }
    return status < 0 ? fail(c, -3) : 0;
}
E2D_API void e2d_ogv_destroy(void *context) {
    e2d_ogv *c = context; if (!c) return;
    if (c->video) th_encode_free(c->video);
    if (c->block_initialized) vorbis_block_clear(&c->block);
    if (c->audio_initialized) vorbis_dsp_clear(&c->audio);
    vorbis_comment_clear(&c->audio_comment); vorbis_info_clear(&c->audio_info);
    if (c->v.initialized) ogg_stream_clear(&c->v.stream);
    if (c->a.initialized) ogg_stream_clear(&c->a.stream);
    free(c->yuv); free(c);
}
E2D_API int e2d_ogv_create(int width, int height, int fps, int rate, int channels, float vq, float aq, int speed, int keyframes, e2d_ogv_writer writer, void *user, void **result) {
    if (!result) return -1; *result = NULL;
    if (!writer || width <= 0 || height <= 0 || (width & 1) || (height & 1) || width > 1048560 || height > 1048560 || (uint64_t)width * (uint64_t)height > SIZE_MAX / 3 || fps <= 0 || rate < 1000 || rate > 200000 ||
        (channels != 2 && channels != 4 && channels != 6 && channels != 8) || !isfinite(vq) || vq < 0 || vq > 1 || !isfinite(aq) || aq < -.1f || aq > 1 || speed < 1 || speed > 4 || keyframes < 1 || keyframes > 1024) return -1;
    e2d_ogv *c = calloc(1, sizeof(*c)); if (!c) return -2; *result = c;
    c->writer = writer; c->user = user; c->width = width; c->height = height; c->frame_width = (width + 15) & ~15; c->frame_height = (height + 15) & ~15;
    c->channels = channels; c->output_channels = channels == 4 ? 6 : channels;
    vorbis_info_init(&c->audio_info); vorbis_comment_init(&c->audio_comment);
    if (ogg_stream_init(&c->v.stream, 0x45324456)) return fail(c, -2); c->v.initialized = 1;
    if (ogg_stream_init(&c->a.stream, 0x45324441)) return fail(c, -2); c->a.initialized = 1;
    if (vorbis_encode_init_vbr(&c->audio_info, c->output_channels, rate, aq)) return fail(c, -3);
    if (vorbis_analysis_init(&c->audio, &c->audio_info)) return fail(c, -3); c->audio_initialized = 1;
    if (vorbis_block_init(&c->audio, &c->block)) return fail(c, -3); c->block_initialized = 1;
    th_info info; th_info_init(&info);
    info.frame_width = (ogg_uint32_t)c->frame_width; info.frame_height = (ogg_uint32_t)c->frame_height;
    info.pic_width = (ogg_uint32_t)width; info.pic_height = (ogg_uint32_t)height; info.fps_numerator = (ogg_uint32_t)fps; info.fps_denominator = 1;
    info.aspect_numerator = info.aspect_denominator = 1; info.pixel_fmt = TH_PF_420; info.quality = (int)(vq * 63);
    while ((1U << info.keyframe_granule_shift) < (unsigned)keyframes) info.keyframe_granule_shift++;
    c->video = th_encode_alloc(&info); th_info_clear(&info); if (!c->video) return fail(c, -3);
    ogg_uint32_t interval = (ogg_uint32_t)keyframes; int maximum;
    if (th_encode_ctl(c->video, TH_ENCCTL_SET_KEYFRAME_FREQUENCY_FORCE, &interval, sizeof(interval)) ||
        th_encode_ctl(c->video, TH_ENCCTL_GET_SPLEVEL_MAX, &maximum, sizeof(maximum))) return fail(c, -3);
    if (speed > maximum) speed = maximum;
    if (th_encode_ctl(c->video, TH_ENCCTL_SET_SPLEVEL, &speed, sizeof(speed))) return fail(c, -3);
    uint64_t count = (uint64_t)c->frame_width * (uint64_t)c->frame_height;
    if (count > SIZE_MAX / 3 * 2) return fail(c, -2);
    size_t pixels = (size_t)count;
    c->yuv = malloc(pixels + pixels / 2); if (!c->yuv) return fail(c, -2);
    for (int i = 0; i < 3; i++) { c->planes[i].width = c->frame_width >> (i != 0); c->planes[i].height = c->frame_height >> (i != 0); c->planes[i].stride = c->planes[i].width; }
    c->planes[0].data = c->yuv; c->planes[1].data = c->yuv + pixels; c->planes[2].data = c->yuv + pixels + pixels / 4;
    th_comment comment; th_comment_init(&comment); ogg_packet packet; int status;
    status = th_encode_flushheader(c->video, &comment, &packet); if (status <= 0 || ogg_stream_packetin(&c->v.stream, &packet)) { th_comment_clear(&comment); return fail(c, -3); }
    if (flush_headers(c, &c->v)) { th_comment_clear(&comment); return c->failed; }
    ogg_packet audio_header, audio_comment, audio_setup;
    if (vorbis_analysis_headerout(&c->audio, &c->audio_comment, &audio_header, &audio_comment, &audio_setup) || ogg_stream_packetin(&c->a.stream, &audio_header)) { th_comment_clear(&comment); return fail(c, -3); }
    if (flush_headers(c, &c->a)) { th_comment_clear(&comment); return c->failed; }
    while ((status = th_encode_flushheader(c->video, &comment, &packet)) > 0) if (ogg_stream_packetin(&c->v.stream, &packet)) { th_comment_clear(&comment); return fail(c, -3); }
    th_comment_clear(&comment); if (status < 0) return fail(c, -3);
    if (ogg_stream_packetin(&c->a.stream, &audio_comment) || ogg_stream_packetin(&c->a.stream, &audio_setup)) return fail(c, -3);
    if (flush_headers(c, &c->v) || flush_headers(c, &c->a)) return c->failed;
    return 0;
}
static void convert_rgb(e2d_ogv *c, const uint8_t *rgb) {
    for (int y = 0; y < c->frame_height; y++) for (int x = 0; x < c->frame_width; x++) {
        int px = x < c->width ? x : c->width - 1, py = y < c->height ? y : c->height - 1;
        const uint8_t *p = rgb + ((size_t)py * c->width + px) * 3;
        c->planes[0].data[(size_t)y * c->frame_width + x] = (uint8_t)(16 + ((66 * p[0] + 129 * p[1] + 25 * p[2] + 128) >> 8));
    }
    for (int y = 0; y < c->frame_height; y += 2) for (int x = 0; x < c->frame_width; x += 2) {
        int r = 0, g = 0, b = 0;
        for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++) {
            int px = x + i < c->width ? x + i : c->width - 1, py = y + j < c->height ? y + j : c->height - 1;
            const uint8_t *p = rgb + ((size_t)py * c->width + px) * 3; r += p[0]; g += p[1]; b += p[2];
        }
        r = (r + 2) / 4; g = (g + 2) / 4; b = (b + 2) / 4; size_t at = (size_t)(y / 2) * (c->frame_width / 2) + x / 2;
        c->planes[1].data[at] = (uint8_t)(128 + ((-38 * r - 74 * g + 112 * b + 128) >> 8));
        c->planes[2].data[at] = (uint8_t)(128 + ((112 * r - 94 * g - 18 * b + 128) >> 8));
    }
}
E2D_API int e2d_ogv_frame(void *context, const uint8_t *rgb, size_t length, const int32_t *pcm, int samples) {
    e2d_ogv *c = context; if (!c || !rgb || samples < 0 || (!pcm && samples) || (uint64_t)c->width * (uint64_t)c->height > SIZE_MAX / 3 || length != (size_t)c->width * c->height * 3) return -1;
    if (c->failed || c->finished) return c->failed ? c->failed : -1;
    if (pull_video(c, 0)) return c->failed;
    convert_rgb(c, rgb); if (th_encode_ycbcr_in(c->video, c->planes)) return fail(c, -3);
    if (samples) {
        float **buffer = vorbis_analysis_buffer(&c->audio, samples); if (!buffer) return fail(c, -2);
        static const int map6[] = {0, 2, 1, 4, 5, 3}, map8[] = {0, 2, 1, 6, 7, 4, 5, 3};
        for (int ch = 0; ch < c->output_channels; ch++) {
            int source = c->channels == 2 ? ch : c->channels == 8 ? map8[ch] : map6[ch];
            if (c->channels == 4 && (ch == 3 || ch == 4)) source = -1;
            for (int i = 0; i < samples; i++) buffer[ch][i] = source < 0 ? 0 : (float)(pcm[(size_t)i * c->channels + source] / 2147483648.0);
        }
        if (vorbis_analysis_wrote(&c->audio, samples)) return fail(c, -3);
    }
    c->has_frame = 1; if (pull_audio(c)) return c->failed;
    return mux(c, 0);
}
E2D_API int e2d_ogv_finish(void *context) {
    e2d_ogv *c = context; if (!c || c->finished) return -1; if (c->failed) return c->failed;
    c->finished = 1;
    if (vorbis_analysis_wrote(&c->audio, 0)) return fail(c, -3);
    if (pull_audio(c) || pull_video(c, 1)) return c->failed;
    if (mux(c, 1)) return c->failed;
    if (!c->has_frame) {
        // A header-only video stream ends with a page containing no packets.
        uint8_t header[27] = {'O','g','g','S',0,4}; memset(header + 6, 255, 8);
        uint32_t serial = (uint32_t)c->v.stream.serialno, number = (uint32_t)c->v.stream.pageno;
        for (int i = 0; i < 4; i++) { header[14 + i] = (uint8_t)(serial >> (8 * i)); header[18 + i] = (uint8_t)(number >> (8 * i)); }
        ogg_page page = {header, 27, NULL, 0}; ogg_page_checksum_set(&page); return write_bytes(c, header, sizeof(header));
    }
    return 0;
}
