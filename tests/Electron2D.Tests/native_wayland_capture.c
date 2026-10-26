#include <pipewire/pipewire.h>
#include <spa/param/buffers.h>
#include <spa/param/video/format-utils.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

static struct pw_main_loop *loop;
static struct pw_stream *stream;
static const char *marker;
static unsigned frames;
static int failed;

static void state_changed(void *data, enum pw_stream_state old, enum pw_stream_state state, const char *error)
{
    (void)data; (void)old;
    fprintf(stderr, "state %s: %s\n", pw_stream_state_as_string(state), error ? error : "");
    if (state == PW_STREAM_STATE_ERROR) { failed = 1; pw_main_loop_quit(loop); }
}

static void param_changed(void *data, uint32_t id, const struct spa_pod *param)
{
    (void)data;
    if (id != SPA_PARAM_Format || !param) return;
    struct spa_video_info_raw video = {0};
    if (spa_format_video_raw_parse(param, &video) < 0 || video.format != SPA_VIDEO_FORMAT_BGRx || !video.size.width || !video.size.height) {
        failed = 1; pw_main_loop_quit(loop); return;
    }
    fprintf(stderr, "format %u, %ux%u\n", video.format, video.size.width, video.size.height);
    uint8_t storage[1024];
    struct spa_pod_builder builder = SPA_POD_BUILDER_INIT(storage, sizeof storage);
    const struct spa_pod *buffers = spa_pod_builder_add_object(&builder,
        SPA_TYPE_OBJECT_ParamBuffers, SPA_PARAM_Buffers,
        SPA_PARAM_BUFFERS_buffers, SPA_POD_CHOICE_RANGE_Int(8, 2, 16),
        SPA_PARAM_BUFFERS_dataType, SPA_POD_Int(1 << SPA_DATA_MemFd));
    if (pw_stream_update_params(stream, &buffers, 1) < 0) { failed = 1; pw_main_loop_quit(loop); }
}

static void process(void *data)
{
    (void)data;
    struct pw_buffer *buffer;
    while ((buffer = pw_stream_dequeue_buffer(stream))) {
        struct spa_buffer *contents = buffer->buffer;
        if (contents->n_datas && contents->datas[0].chunk && contents->datas[0].chunk->size) {
            struct spa_data *pixels = &contents->datas[0];
            if (pixels->type != SPA_DATA_MemFd || !pixels->data || pixels->chunk->offset > pixels->maxsize || pixels->chunk->size > pixels->maxsize - pixels->chunk->offset) {
                failed = 1; pw_main_loop_quit(loop);
            } else {
                ++frames;
                char temporary[4096];
                int length = snprintf(temporary, sizeof temporary, "%s.tmp", marker);
                if (length < 0 || (size_t)length >= sizeof temporary) { failed = 1; pw_main_loop_quit(loop); }
                FILE *file = failed ? NULL : fopen(temporary, "w");
                if (!file) { failed = 1; pw_main_loop_quit(loop); }
                else {
                    int written = fprintf(file, "%u\n", frames);
                    int closed = fclose(file);
                    if (written < 0 || closed || rename(temporary, marker)) { failed = 1; pw_main_loop_quit(loop); }
                }
                if (frames == 1 || frames % 16 == 0) { printf("frames %u, bytes %u\n", frames, pixels->chunk->size); fflush(stdout); }
            }
        }
        pw_stream_queue_buffer(stream, buffer);
    }
}

int main(int argc, char **argv)
{
    if (argc != 3) return 2;
    char *end;
    errno = 0;
    unsigned long target = strtoul(argv[1], &end, 10);
    if (errno || !argv[1][0] || *end || target >= SPA_ID_INVALID) return 2;
    marker = argv[2]; alarm(180); pw_init(&argc, &argv);
    loop = pw_main_loop_new(NULL);
    if (!loop) return 3;
    const struct pw_stream_events events = {PW_VERSION_STREAM_EVENTS, .state_changed = state_changed, .param_changed = param_changed, .process = process};
    stream = pw_stream_new_simple(pw_main_loop_get_loop(loop), "Electron2D isolated capture drain",
        pw_properties_new(PW_KEY_MEDIA_TYPE, "Video", PW_KEY_MEDIA_CATEGORY, "Capture", PW_KEY_MEDIA_ROLE, "Screen", NULL), &events, NULL);
    if (!stream) { pw_main_loop_destroy(loop); return 4; }
    uint8_t storage[1024];
    struct spa_pod_builder builder = SPA_POD_BUILDER_INIT(storage, sizeof storage);
    const struct spa_pod *format = spa_pod_builder_add_object(&builder,
        SPA_TYPE_OBJECT_Format, SPA_PARAM_EnumFormat,
        SPA_FORMAT_mediaType, SPA_POD_Id(SPA_MEDIA_TYPE_video),
        SPA_FORMAT_mediaSubtype, SPA_POD_Id(SPA_MEDIA_SUBTYPE_raw),
        SPA_FORMAT_VIDEO_format, SPA_POD_Id(SPA_VIDEO_FORMAT_BGRx),
        SPA_FORMAT_VIDEO_size, SPA_POD_CHOICE_RANGE_Rectangle(&SPA_RECTANGLE(1280,960), &SPA_RECTANGLE(1,1), &SPA_RECTANGLE(16384,16384)),
        SPA_FORMAT_VIDEO_framerate, SPA_POD_Fraction(&SPA_FRACTION(0,1)),
        SPA_FORMAT_VIDEO_maxFramerate, SPA_POD_CHOICE_RANGE_Fraction(&SPA_FRACTION(60,1), &SPA_FRACTION(1,1), &SPA_FRACTION(60,1)));
    int connected = pw_stream_connect(stream, PW_DIRECTION_INPUT, (uint32_t)target, PW_STREAM_FLAG_AUTOCONNECT | PW_STREAM_FLAG_MAP_BUFFERS, &format, 1);
    if (connected >= 0) pw_main_loop_run(loop);
    pw_stream_destroy(stream); pw_main_loop_destroy(loop); pw_deinit();
    return connected < 0 || failed || !frames;
}
