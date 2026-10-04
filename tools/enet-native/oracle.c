#include <enet/enet.h>
#include <stdio.h>
#include <string.h>
int main(void) {
  if (enet_initialize() != 0)
    return 1;
  ENetAddress address = {ENET_HOST_ANY, 0};
  ENetHost *host = enet_host_create(&address, 1, 3, 0, 0);
  if (!host)
    return 2;
  printf("%u\n", host->address.port);
  fflush(stdout);
  ENetEvent event;
  while (enet_host_service(host, &event, 10000) > 0) {
    if (event.type == ENET_EVENT_TYPE_RECEIVE) {
      ENetPacket *reply =
          enet_packet_create(event.packet->data, event.packet->dataLength,
                             ENET_PACKET_FLAG_RELIABLE);
      enet_packet_destroy(event.packet);
      if (enet_peer_send(event.peer, event.channelID, reply) < 0)
        return 3;
      enet_host_flush(host);
    }
    if (event.type == ENET_EVENT_TYPE_DISCONNECT) {
      enet_host_destroy(host);
      enet_deinitialize();
      return 0;
    }
  }
  enet_host_destroy(host);
  enet_deinitialize();
  return 4;
}
