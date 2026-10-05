#include <enet/enet.h>
#include <enet/time.h>
#include <fastlz.h>
#include <stdint.h>
#include <string.h>
#include <time.h>
#include <zlib.h>
#include <zstd.h>
#if defined(_WIN32)
#include <windows.h>
#include <bcrypt.h>
#define API __declspec(dllexport)
#define THREAD_LOCAL __declspec(thread)
#elif defined(__linux__)
#include <sys/random.h>
#define API __attribute__((visibility("default")))
#define THREAD_LOCAL _Thread_local
#else
#define API __attribute__((visibility("default")))
#define THREAD_LOCAL _Thread_local
#endif

typedef struct {
  void *data;
  size_t length;
} E2DBuffer;
typedef int (*Send)(int, uint32_t, uint16_t, const E2DBuffer *, size_t);
typedef int (*Receive)(int, uint32_t *, uint16_t *, void *, size_t);
typedef int (*Wait)(int, uint32_t *, uint32_t);
static Send send_cb;
static Receive receive_cb;
static Wait wait_cb;
typedef int (*Control)(int, int, int);
static Control control_cb;
static THREAD_LOCAL int creating_socket = -1;
static uint32_t time_offset;
API void e2d_enet_callbacks(Send s, Receive r, Wait w, Control c) {
  send_cb = s;
  receive_cb = r;
  wait_cb = w;
  control_cb = c;
}
int enet_initialize(void) { return 0; }
void enet_deinitialize(void) {}
uint32_t enet_time_get(void) {
#if defined(_WIN32)
  return (uint32_t)GetTickCount64() - time_offset;
#else
  struct timespec t;
  clock_gettime(CLOCK_MONOTONIC, &t);
  return (uint32_t)(t.tv_sec * 1000 + t.tv_nsec / 1000000) - time_offset;
#endif
}
uint32_t enet_host_random_seed(void) {
  uint32_t seed;
#if defined(_WIN32)
  if (BCryptGenRandom(NULL, (PUCHAR)&seed, sizeof(seed),
                     BCRYPT_USE_SYSTEM_PREFERRED_RNG) == 0)
    return seed;
#elif defined(__linux__)
  if (getrandom(&seed, sizeof(seed), 0) == sizeof(seed))
    return seed;
#else
  arc4random_buf(&seed, sizeof(seed));
  return seed;
#endif
  return enet_time_get();
}
void enet_time_set(uint32_t value) {
  time_offset = enet_time_get() + time_offset - value;
}
ENetSocket enet_socket_create(ENetSocketType type) {
  return type == ENET_SOCKET_TYPE_DATAGRAM ? (ENetSocket)creating_socket : ENET_SOCKET_NULL;
}
int enet_socket_bind(ENetSocket s, const ENetAddress *a) {
  return s != ENET_SOCKET_NULL ? 0 : -1;
}
int enet_socket_get_address(ENetSocket s, ENetAddress *a) {
  memset(a, 0, sizeof(*a));
  int port = control_cb(s, 200, 0);
  if (port < 0)
    return -1;
  a->port = (uint16_t)port;
  return 0;
}
int enet_socket_send(ENetSocket s, const ENetAddress *a, const ENetBuffer *b,
                     size_t n) {
  if (n > ENET_BUFFER_MAXIMUM)
    return -1;
  // ENet's Windows and Unix buffers have different field order; the engine ABI does not.
  E2DBuffer buffers[ENET_BUFFER_MAXIMUM];
  for (size_t i = 0; i < n; i++) {
    buffers[i].data = b[i].data;
    buffers[i].length = b[i].dataLength;
  }
  return send_cb((int)s, a->host, a->port, buffers, n);
}
int enet_socket_receive(ENetSocket s, ENetAddress *a, ENetBuffer *b, size_t n) {
  return n == 1 ? receive_cb(s, &a->host, &a->port, b[0].data, b[0].dataLength)
                : -1;
}
int enet_socket_wait(ENetSocket s, uint32_t *c, uint32_t t) {
  return wait_cb(s, c, t);
}
int enet_socket_set_option(ENetSocket s, ENetSocketOption o, int v) {
  return control_cb(s, o, v);
}
int enet_socket_get_option(ENetSocket s, ENetSocketOption o, int *v) {
  int value = control_cb(s, 100 + o, 0);
  if (value < 0)
    return -1;
  *v = value;
  return 0;
}
void enet_socket_destroy(ENetSocket s) {}
int enet_socket_listen(ENetSocket s, int b) { return -1; }
ENetSocket enet_socket_accept(ENetSocket s, ENetAddress *a) { return -1; }
int enet_socket_connect(ENetSocket s, const ENetAddress *a) { return -1; }
int enet_socket_shutdown(ENetSocket s, ENetSocketShutdown h) { return 0; }
int enet_socketset_select(ENetSocket s, ENetSocketSet *r, ENetSocketSet *w,
                          uint32_t t) {
  return -1;
}
int enet_address_set_host_ip(ENetAddress *a, const char *n) { return -1; }
int enet_address_set_host(ENetAddress *a, const char *n) { return -1; }
int enet_address_get_host_ip(const ENetAddress *a, char *n, size_t l) {
  return -1;
}
int enet_address_get_host(const ENetAddress *a, char *n, size_t l) {
  return -1;
}

typedef struct State State;
typedef struct {
  State *host;
  uint64_t queued;
} PeerBudget;
struct State {
  ENetHost *host;
  PeerBudget *budgets;
  uint64_t queued;
  uint32_t packets;
  int refuse;
};
typedef struct {
  int type, peer, channel;
  uint32_t data;
  ENetPacket *packet;
} Event;
API State *e2d_enet_create(int socket_id, int bind, uint16_t port, int count,
                           int channels, uint32_t in, uint32_t out) {
  State *s = calloc(1, sizeof(*s));
  if (!s)
    return NULL;
  creating_socket = socket_id;
  ENetAddress address = {0, port};
  s->host = enet_host_create(bind ? &address : NULL, count, channels, in, out);
  creating_socket = -1;
  if (!s->host) {
    free(s);
    return NULL;
  }
  s->budgets = calloc(count, sizeof(*s->budgets));
  if (!s->budgets) {
    enet_host_destroy(s->host);
    free(s);
    return NULL;
  }
  for (int i = 0; i < count; i++)
    s->budgets[i].host = s;
  s->host->maximumPacketSize = 1 << 24;
  s->host->maximumWaitingData = 64 << 20;
  return s;
}
API void e2d_enet_destroy(State *s) {
  enet_host_destroy(s->host);
  free(s->budgets);
  free(s);
}
API int e2d_enet_connect(State *s, uint32_t token, uint16_t port, int channels,
                         uint32_t data) {
  ENetAddress a = {token, port};
  ENetPeer *p = enet_host_connect(s->host, &a, channels, data);
  return p ? (int)(p - s->host->peers) : -1;
}
static THREAD_LOCAL State *servicing;
static int intercept(ENetHost *host, ENetEvent *event) {
  if (!servicing || !servicing->refuse || host->receivedDataLength < 2)
    return 0;
  uint16_t header;
  memcpy(&header, host->receivedData, 2);
  if ((ENET_NET_TO_HOST_16(header) & ENET_PROTOCOL_MAXIMUM_PEER_ID) !=
      ENET_PROTOCOL_MAXIMUM_PEER_ID)
    return 0;
  for (size_t i = 0; i < host->peerCount; i++) {
    ENetPeer *p = host->peers + i;
    if (p->state != ENET_PEER_STATE_DISCONNECTED &&
        p->address.host == host->receivedAddress.host &&
        p->address.port == host->receivedAddress.port)
      return 0;
  }
  return 1;
}
API int e2d_enet_service(State *s, Event *out, uint32_t timeout, int check) {
  servicing = s;
  ENetEvent e = {0};
  int r = check ? enet_host_check_events(s->host, &e)
                : enet_host_service(s->host, &e, timeout);
  servicing = NULL;
  memset(out, 0, sizeof(*out));
  out->type = r < 0 ? -1 : e.type;
  out->peer = e.peer ? (int)(e.peer - s->host->peers) : -1;
  out->channel = e.channelID;
  out->data = e.data;
  out->packet = e.packet;
  return r;
}
API void e2d_enet_flush(State *s) { enet_host_flush(s->host); }
static void release_budget(ENetPacket *p) {
  PeerBudget *b = p->userData;
  b->queued -= p->dataLength;
  b->host->queued -= p->dataLength;
  b->host->packets--;
}
API int e2d_enet_send(State *s, int index, uint8_t channel, const void *data,
                      size_t size, uint32_t flags) {
  PeerBudget *b = &s->budgets[index];
  if (size > (1 << 24) || s->queued + size > (64 << 20) || s->packets >= 65536)
    return -1;
  ENetPacket *p = enet_packet_create(data, size, flags);
  if (!p)
    return -1;
  p->userData = b;
  p->freeCallback = release_budget;
  b->queued += size;
  s->queued += size;
  s->packets++;
  int r = enet_peer_send(s->host->peers + index, channel, p);
  if (r < 0)
    enet_packet_destroy(p);
  return r;
}
API size_t e2d_enet_packet(ENetPacket *p, void **data, uint32_t *flags) {
  *data = p->data;
  *flags = p->flags;
  return p->dataLength;
}
API void e2d_enet_release(ENetPacket *p) { enet_packet_destroy(p); }
API uint32_t e2d_enet_peer(State *s, int i, int operation, uint32_t a,
                           uint32_t b, uint32_t c) {
  ENetPeer *p = s->host->peers + i;
  switch (operation) {
  case 0:
    return p->state;
  case 1:
    return p->channelCount;
  case 2:
    return p->address.host;
  case 3:
    return p->address.port;
  case 4:
    enet_peer_disconnect(p, a);
    break;
  case 5:
    enet_peer_disconnect_later(p, a);
    break;
  case 6:
    enet_peer_disconnect_now(p, a);
    break;
  case 7:
    enet_peer_reset(p);
    break;
  case 8:
    enet_peer_ping(p);
    break;
  case 9:
    enet_peer_ping_interval(p, a);
    break;
  case 10:
    enet_peer_timeout(p, a, b, c);
    break;
  case 11:
    enet_peer_throttle_configure(p, a, b, c);
    break;
  }
  return 0;
}
API double e2d_enet_stat(State *s, int index, int stat) {
  ENetPeer *p = index < 0 ? NULL : s->host->peers + index;
  uint32_t *v = NULL;
  if (!p) {
    switch (stat) {
    case 0:
      v = &s->host->totalSentData;
      break;
    case 1:
      v = &s->host->totalSentPackets;
      break;
    case 2:
      v = &s->host->totalReceivedData;
      break;
    case 3:
      v = &s->host->totalReceivedPackets;
      break;
    }
    if (!v)
      return -1;
    double r = *v;
    *v = 0;
    return r;
  }
  switch (stat) {
  case 0:
    return p->packetLoss;
  case 1:
    return p->packetLossVariance;
  case 2:
    return p->packetLossEpoch;
  case 3:
    return p->roundTripTime;
  case 4:
    return p->roundTripTimeVariance;
  case 5:
    return p->lastRoundTripTime;
  case 6:
    return p->lastRoundTripTimeVariance;
  case 7:
    return p->packetThrottle;
  case 8:
    return p->packetThrottleLimit;
  case 9:
    return p->packetThrottleCounter;
  case 10:
    return p->packetThrottleEpoch;
  case 11:
    return p->packetThrottleAcceleration;
  case 12:
    return p->packetThrottleDeceleration;
  case 13:
    return p->packetThrottleInterval;
  }
  return -1;
}
API uint32_t e2d_enet_host(State *s, int op, uint32_t a, uint32_t b) {
  switch (op) {
  case 0:
    return s->host->channelLimit;
  case 1:
    enet_host_bandwidth_limit(s->host, a, b);
    break;
  case 2:
    enet_host_channel_limit(s->host, a);
    break;
  case 3:
    enet_host_bandwidth_throttle(s->host);
    break;
  case 4:
    s->refuse = a;
    s->host->intercept = intercept;
    break;
  }
  return 0;
}

typedef struct {
  int mode;
  unsigned char source[8192], encoded[16384];
  ZSTD_CCtx *c;
  ZSTD_DCtx *d;
} Codec;
static size_t encode(void *ctx, const ENetBuffer *buffers, size_t count,
                     size_t limit, unsigned char *out, size_t cap) {
  Codec *c = ctx;
  size_t n = 0;
  for (size_t i = 0; i < count && n < limit; i++) {
    size_t take = buffers[i].dataLength;
    if (take > limit - n)
      take = limit - n;
    if (take > sizeof(c->source) - n)
      return 0;
    memcpy(c->source + n, buffers[i].data, take);
    n += take;
  }
  if (n < 16)
    return 0;
  size_t size = 0;
  if (c->mode == 2)
    size = fastlz_compress(c->source, (int)n, c->encoded);
  else if (c->mode == 3) {
    uLongf len = sizeof(c->encoded);
    if (compress2(c->encoded, &len, c->source, n, Z_DEFAULT_COMPRESSION) !=
        Z_OK)
      return 0;
    size = len;
  } else {
    size = ZSTD_compressCCtx(c->c, c->encoded, sizeof(c->encoded), c->source, n,
                             3);
    if (ZSTD_isError(size))
      return 0;
  }
  if (size >= n || size > cap)
    return 0;
  memcpy(out, c->encoded, size);
  return size;
}
static size_t decode(void *ctx, const unsigned char *in, size_t n,
                     unsigned char *out, size_t cap) {
  Codec *c = ctx;
  if (c->mode == 2) {
    int r = fastlz_decompress(in, (int)n, out, (int)cap);
    return r > 0 ? (size_t)r : 0;
  }
  if (c->mode == 3) {
    uLongf len = cap;
    return uncompress(out, &len, in, n) == Z_OK ? len : 0;
  }
  size_t r = ZSTD_decompressDCtx(c->d, out, cap, in, n);
  return ZSTD_isError(r) ? 0 : r;
}
static void codec_free(void *ctx) {
  Codec *c = ctx;
  ZSTD_freeCCtx(c->c);
  ZSTD_freeDCtx(c->d);
  free(c);
}
API int e2d_enet_compress(State *s, int mode) {
  if (mode == 0) {
    enet_host_compress(s->host, NULL);
    return 0;
  }
  if (mode == 1)
    return enet_host_compress_with_range_coder(s->host);
  if (mode < 2 || mode > 4)
    return -1;
  Codec *c = calloc(1, sizeof(*c));
  if (!c)
    return -1;
  c->mode = mode;
  if (mode == 4) {
    c->c = ZSTD_createCCtx();
    c->d = ZSTD_createDCtx();
    if (!c->c || !c->d) {
      codec_free(c);
      return -1;
    }
    ZSTD_DCtx_setParameter(c->d, ZSTD_d_windowLogMax, 13);
  }
  ENetCompressor codec = {c, encode, decode, codec_free};
  enet_host_compress(s->host, &codec);
  return 0;
}
