#include <png.h>
#include <string.h>
#include "../enet-native/memory.h"

#define PNG_LIMIT (64u * 1024u * 1024u)

typedef struct {
    const unsigned char *bytes;
    size_t remaining;
} PNGInput;

typedef struct {
    unsigned char **bytes;
    size_t *length;
    size_t capacity;
} PNGOutput;

static png_voidp allocate(png_structp png, png_alloc_size_t size)
{
    (void)png;
    return e2d_memory_malloc(size);
}

static void release(png_structp png, png_voidp pointer)
{
    (void)png;
    e2d_memory_free(pointer);
}

static void fail(png_structp png, png_const_charp message)
{
    (void)message;
    png_longjmp(png, 1);
}

static void warn(png_structp png, png_const_charp message)
{
    (void)png;
    (void)message;
}

static void read_bytes(png_structp png, png_bytep bytes, size_t length)
{
    PNGInput *input = png_get_io_ptr(png);
    if (length > input->remaining)
        png_error(png, "Truncated PNG input");
    memcpy(bytes, input->bytes, length);
    input->bytes += length;
    input->remaining -= length;
}

static void write_bytes(png_structp png, png_bytep bytes, size_t length)
{
    PNGOutput *output = png_get_io_ptr(png);
    if (length > PNG_LIMIT - *output->length)
        png_error(png, "PNG output exceeds its limit");
    size_t required = *output->length + length;
    if (required > output->capacity) {
        size_t capacity = output->capacity ? 2 * output->capacity : 4096;
        if (capacity > PNG_LIMIT)
            capacity = PNG_LIMIT;
        if (capacity < required)
            capacity = required;
        void *replacement = e2d_memory_realloc(*output->bytes, capacity);
        if (!replacement)
            png_error(png, "Cannot allocate PNG output");
        *output->bytes = replacement;
        output->capacity = capacity;
    }
    memcpy(*output->bytes + *output->length, bytes, length);
    *output->length = required;
}

#if !defined(_WIN32)
__attribute__((visibility("default")))
#endif
int e2d_png_decode(const unsigned char *bytes, size_t length, int width, int height,
    unsigned char **pixels, int *channels)
{
    *pixels = NULL;
    *channels = 0;
    if (!bytes || !length || length > PNG_LIMIT || width <= 0 || height <= 0)
        return -1;
    png_structp png = png_create_read_struct_2(PNG_LIBPNG_VER_STRING, NULL, fail, warn,
        NULL, allocate, release);
    if (!png)
        return -2;
    png_infop info = png_create_info_struct(png);
    if (!info) {
        png_destroy_read_struct(&png, NULL, NULL);
        return -2;
    }
    uint64_t epoch = e2d_memory_failure_epoch();
    if (setjmp(png_jmpbuf(png))) {
        png_destroy_read_struct(&png, &info, NULL);
        e2d_memory_free(*pixels);
        *pixels = NULL;
        return epoch == e2d_memory_failure_epoch() ? -1 : -2;
    }

    PNGInput input = {bytes, length};
    png_set_read_fn(png, &input, read_bytes);
    png_set_crc_action(png, PNG_CRC_ERROR_QUIT, PNG_CRC_WARN_DISCARD);
    png_set_user_limits(png, (png_uint_32)width, (png_uint_32)height);
    png_set_chunk_malloc_max(png, 8u * 1024u * 1024u);
    // The engine consumes pixels and transfer metadata, without retaining text or EXIF.
    static const png_byte ignored[] = "tEXt\0zTXt\0iTXt\0eXIf";
    png_set_keep_unknown_chunks(png, PNG_HANDLE_CHUNK_NEVER, ignored, 4);
    png_read_info(png, info);
    if (png_get_image_width(png, info) != (png_uint_32)width ||
        png_get_image_height(png, info) != (png_uint_32)height)
        png_error(png, "Unexpected PNG dimensions");

    png_set_expand(png);
    png_set_alpha_mode_fixed(png, PNG_ALPHA_PNG, PNG_DEFAULT_sRGB);
    if (png_get_bit_depth(png, info) == 16)
        png_set_scale_16(png);
    int passes = png_set_interlace_handling(png);
    png_read_update_info(png, info);
    int components = png_get_channels(png, info);
    size_t row_bytes = png_get_rowbytes(png, info);
    if (components < 1 || components > 4 || png_get_bit_depth(png, info) != 8 ||
        row_bytes != (uint64_t)width * components || row_bytes > PNG_LIMIT / (unsigned)height)
        png_error(png, "Decoded PNG exceeds the pixel limit");
    *pixels = e2d_memory_calloc((size_t)height, row_bytes);
    if (!*pixels)
        png_error(png, "Cannot allocate PNG pixels");
    for (int pass = 0; pass < passes; ++pass)
        for (int y = 0; y < height; ++y)
            png_read_row(png, *pixels + (size_t)y * row_bytes, NULL);
    png_read_end(png, NULL);
    if (epoch != e2d_memory_failure_epoch())
        png_error(png, "Cannot allocate PNG working memory");
    png_destroy_read_struct(&png, &info, NULL);
    *channels = components;
    return 0;
}

#if !defined(_WIN32)
__attribute__((visibility("default")))
#endif
int e2d_png_encode(const unsigned char *pixels, int width, int height, int channels,
    unsigned char **bytes, size_t *length)
{
    static const int color_types[] = {PNG_COLOR_TYPE_GRAY, PNG_COLOR_TYPE_GRAY_ALPHA,
        PNG_COLOR_TYPE_RGB, PNG_COLOR_TYPE_RGB_ALPHA};
    *bytes = NULL;
    *length = 0;
    if (!pixels || width <= 0 || height <= 0 || channels < 1 || channels > 4 ||
        (uint64_t)width * channels > PNG_LIMIT / (unsigned)height)
        return -1;
    png_structp png = png_create_write_struct_2(PNG_LIBPNG_VER_STRING, NULL, fail, warn,
        NULL, allocate, release);
    if (!png)
        return -1;
    png_infop info = png_create_info_struct(png);
    if (!info) {
        png_destroy_write_struct(&png, NULL);
        return -1;
    }
    uint64_t epoch = e2d_memory_failure_epoch();
    if (setjmp(png_jmpbuf(png))) {
        png_destroy_write_struct(&png, &info);
        e2d_memory_free(*bytes);
        *bytes = NULL;
        *length = 0;
        return -1;
    }

    PNGOutput output = {bytes, length, 0};
    png_set_write_fn(png, &output, write_bytes, NULL);
    png_set_IHDR(png, info, (png_uint_32)width, (png_uint_32)height, 8, color_types[channels - 1],
        PNG_INTERLACE_NONE, PNG_COMPRESSION_TYPE_BASE, PNG_FILTER_TYPE_BASE);
    png_set_sRGB(png, info, PNG_sRGB_INTENT_PERCEPTUAL);
    png_write_info(png, info);
    size_t row_bytes = (size_t)width * channels;
    for (int y = 0; y < height; ++y)
        png_write_row(png, pixels + (size_t)y * row_bytes);
    png_write_end(png, info);
    if (epoch != e2d_memory_failure_epoch())
        png_error(png, "Cannot allocate PNG working memory");
    png_destroy_write_struct(&png, &info);
    return 0;
}
