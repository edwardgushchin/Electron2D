// Exercises the production bridge's private codec callbacks and allocation-failure cleanup.
#include "bridge.c"
#include <stdio.h>
#include <stdalign.h>

static size_t calls, fail_at, live, peak;
typedef union { max_align_t alignment; size_t size; } AllocationHeader;
static void *tracked_allocate(size_t size) {
  calls++;
  if (calls == fail_at || size > SIZE_MAX - sizeof(AllocationHeader)) return NULL;
  AllocationHeader *header = malloc(sizeof(*header) + size);
  if (!header) abort();
  header->size = size; live += size; if (live > peak) peak = live;
  return header + 1;
}
static void tracked_release(void *value) {
  if (!value) return;
  AllocationHeader *header = (AllocationHeader *)value - 1;
  if (live < header->size) abort();
  live -= header->size; free(header);
}
static void require(int condition, const char *message) {
  if (!condition) { fprintf(stderr, "%s (calls=%zu fail=%zu live=%zu)\n", message, calls, fail_at, live); abort(); }
}
static int send_unused(int socket, uint32_t address, uint16_t port, const E2DBuffer *buffers, size_t count) { return -1; }
static int receive_unused(int socket, uint32_t *address, uint16_t *port, void *buffer, size_t size) { return 0; }
static int wait_unused(int socket, uint32_t *condition, uint32_t timeout) { *condition = 0; return 0; }
static int control_unused(int socket, int operation, int value) { return operation == 200 ? 12345 : 0; }
static void host_case(int mode, int complete) {
  State *state = e2d_enet_create(1, 0, 0, 2, 2, 0, 0);
  if (!state) { require(!complete, "Complete host creation failed"); return; }
  int configured = e2d_enet_compress(state, mode);
  if (configured != 0) { require(!complete, "Complete compressor creation failed"); e2d_enet_destroy(state); return; }
  ENetAddress address = {1, 12345};
  ENetPeer *peer = enet_host_connect(state->host, &address, 2, 0);
  if (complete) require(peer != NULL, "Complete peer creation failed");
  unsigned char source[8192], encoded[16384], decoded[8192]; memset(source, 37, sizeof(source));
  ENetPacket *packet = enet_packet_create(source, sizeof(source), ENET_PACKET_FLAG_RELIABLE);
  if (packet) enet_packet_destroy(packet); else require(!complete, "Complete packet creation failed");
  ENetCompressor *codec = &state->host->compressor;
  if (mode != 0) {
    ENetBuffer buffer; buffer.data = source; buffer.dataLength = sizeof(source);
    size_t count = codec->compress(codec->context, &buffer, 1, sizeof(source), encoded, sizeof(encoded));
    if (count) {
      size_t result = codec->decompress(codec->context, encoded, count, decoded, sizeof(decoded));
      if (complete) require(result == sizeof(source) && memcmp(source, decoded, sizeof(source)) == 0, "Complete codec round trip failed");
    } else require(!complete, "Complete codec compression failed");
  }
  e2d_enet_destroy(state);
}
static void file_case(int complete) {
  unsigned char source[65536], encoded[131136], decoded[65536]; memset(source, 23, sizeof(source));
  int count = e2d_file_encode(2, source, sizeof(source), encoded, sizeof(encoded));
  if (count > 0) {
    int result = e2d_file_decode(2, encoded, count, decoded, sizeof(decoded));
    if (complete) require(result == sizeof(source) && memcmp(source, decoded, sizeof(source)) == 0, "Complete file round trip failed");
  } else require(!complete, "Complete file encode failed");
}
static void gzip_case(int compressing, int window, int complete) {
  unsigned char source[4096], encoded[8192], decoded[4096]; memset(source, 51, sizeof(source));
  z_stream stock = {0};
  require(deflateInit2(&stock, Z_DEFAULT_COMPRESSION, Z_DEFLATED, window, 8, Z_DEFAULT_STRATEGY) == Z_OK, "Stock codec setup failed");
  stock.next_in = source; stock.avail_in = sizeof(source); stock.next_out = encoded; stock.avail_out = sizeof(encoded);
  require(deflate(&stock, Z_FINISH) == Z_STREAM_END, "Stock encode failed"); size_t encoded_size = stock.total_out; deflateEnd(&stock);
  int error; GZIP *context = e2d_gzip_create(compressing, window, &error);
  if (!context) { require(!complete && error == Z_MEM_ERROR, "Unexpected codec creation error"); return; }
  uint32_t input_offset = 0, output_offset = 0;
  const unsigned char *input = compressing ? source : encoded;
  unsigned char *output = compressing ? encoded : decoded;
  uint32_t length = compressing ? sizeof(source) : (uint32_t)encoded_size;
  uint32_t capacity = compressing ? sizeof(encoded) : sizeof(decoded);
  int result = Z_OK;
  for (int i = 0; i < 10000 && result != Z_STREAM_END; i++) {
    uint32_t consumed = length - input_offset, written = capacity - output_offset;
    if (written > 19) written = 19;
    result = e2d_gzip_process(context, input + input_offset, &consumed, output + output_offset, &written, 1);
    input_offset += consumed; output_offset += written;
    require(!context->stream.next_in && !context->stream.next_out, "Codec retained a borrowed span");
    if (result == Z_MEM_ERROR) { require(!complete, "Unexpected codec processing allocation failure"); break; }
    require(result == Z_OK || result == Z_STREAM_END, "Unexpected codec processing result");
  }
  if (result == Z_STREAM_END) {
    require(input_offset == length, "Codec failed to consume input");
    if (compressing) {
      z_stream inflate_stock = {0}; require(inflateInit2(&inflate_stock, window) == Z_OK, "Stock inflate setup failed");
      inflate_stock.next_in = encoded; inflate_stock.avail_in = output_offset;
      inflate_stock.next_out = decoded; inflate_stock.avail_out = sizeof(decoded);
      require(inflate(&inflate_stock, Z_FINISH) == Z_STREAM_END && inflate_stock.total_out == sizeof(source), "Stock decoder rejected tracked wire bytes");
      inflateEnd(&inflate_stock);
      require(e2d_gzip_reset(context, window) == Z_STREAM_ERROR, "Compression accepted decoder reset");
    } else {
      require(output_offset == sizeof(source), "Tracked decoder output size differs");
      require(e2d_gzip_reset(context, window) == Z_OK, "Prepared decoder reset failed");
    }
    require(memcmp(source, decoded, sizeof(source)) == 0, "Codec wire contents differ");
  } else require(!complete, "Codec did not finish under output backpressure");
  e2d_gzip_destroy(context);
}
int main(void) {
  require(e2d_enet_memory(NULL, tracked_release) == -1, "Incomplete callbacks accepted");
  require(e2d_enet_memory(tracked_allocate, tracked_release) == 0, "Allocator initialization failed");
  require(e2d_enet_memory(tracked_allocate, tracked_release) == 0, "Idempotent allocator initialization failed");
  require(e2d_enet_memory(malloc, free) == -1, "Live allocator replacement accepted");
  e2d_enet_callbacks(send_unused, receive_unused, wait_unused, control_unused);
  // Independently cross-check the new custom-memory stream with the standard zlib entry points.
  {
    Codec codec = {0}; codec.mode = 3;
    unsigned char source[4096], encoded[8192], decoded[4096]; memset(source, 19, sizeof(source));
    ENetBuffer buffer; buffer.data = source; buffer.dataLength = sizeof(source);
    size_t count = encode(&codec, &buffer, 1, sizeof(source), encoded, sizeof(encoded));
    uLongf length = sizeof(decoded);
    require(count && uncompress(decoded, &length, encoded, count) == Z_OK && length == sizeof(source) &&
      memcmp(source, decoded, sizeof(source)) == 0, "Standard zlib cannot decode the tracked stream");
    length = sizeof(encoded);
    require(compress2(encoded, &length, source, sizeof(source), Z_DEFAULT_COMPRESSION) == Z_OK &&
      decode(&codec, encoded, length, decoded, sizeof(decoded)) == sizeof(source) &&
      memcmp(source, decoded, sizeof(source)) == 0, "Tracked zlib cannot decode the standard stream");
    require(live == 0, "Independent zlib wire checks leaked storage");
  }
  size_t trials = 0;
  for (int mode = 0; mode <= 9; mode++) {
    calls = fail_at = 0;
    if (mode >= 6) gzip_case(mode % 2, mode < 8 ? 15 : 31, 1); else if (mode == 5) file_case(1); else host_case(mode, 1);
    require(live == 0, "Complete operation leaked allocations");
    size_t count = calls;
    for (size_t failure = 1; failure <= count; failure++) {
      calls = 0; fail_at = failure;
      if (mode >= 6) gzip_case(mode % 2, mode < 8 ? 15 : 31, 0); else if (mode == 5) file_case(0); else host_case(mode, 0);
      require(calls >= failure && live == 0, "Allocation-failure position was missed or leaked allocations"); trials++;
    }
    printf("mode=%d allocation_positions=%zu live=%zu peak=%zu\n", mode, count, live, peak);
  }
  printf("%zu allocation-failure positions passed for ENet hosts/peers/packets and all codecs/files, including streaming gzip/zlib.\n", trials);
  return 0;
}
