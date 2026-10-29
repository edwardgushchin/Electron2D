#include "bridge.h"
#include "memory.h"
#include "../enet-native/memory.h"
#include <vorbis/codec.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static char comment[8193];
static void check(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(1); } }
static uint32_t read32(const unsigned char *p) {
    return (uint32_t)p[0] | (uint32_t)p[1]<<8 | (uint32_t)p[2]<<16 | (uint32_t)p[3]<<24;
}
static int exercise(void) {
    vorbis_comment vc; vorbis_comment_init(&vc); vorbis_comment_add(&vc,comment);
    ogg_packet packet={0};int status=vorbis_commentheader_out(&vc,&packet);
    vorbis_comment_clear(&vc);
    if(status) {
        check(!packet.packet,"Refused comment header published a packet");
        return -1;
    }
    check(packet.bytes>20 && !memcmp(packet.packet,"\003vorbis",7),"Comment packet signature changed");
    size_t vendor=read32(packet.packet+7),cursor=11+vendor;
    check(cursor+8<=(size_t)packet.bytes,"Vendor length exceeds packet");
    check(read32(packet.packet+cursor)==1,"Comment count changed");cursor+=4;
    check(read32(packet.packet+cursor)==sizeof(comment)-1,"Comment length changed");cursor+=4;
    check(cursor+sizeof(comment)==(size_t)packet.bytes && !memcmp(packet.packet+cursor,comment,sizeof(comment)-1),"Comment payload changed");
    check(packet.packet[packet.bytes-1]==1 && packet.packetno==1 && !packet.b_o_s && !packet.e_o_s,"Comment framing changed");
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(state.nativeCurrent==(uint64_t)packet.bytes,"Published comment packet is outside its Ogg allocator");
    ogg_packet_clear(&packet);return 0;
}
int main(int argc,char **argv) {
    check(argc==2,"Expected allocation failure position");
    for(size_t i=0;i<sizeof(comment)-1;i++)comment[i]=(char)('!'+i%90);
    E2DMemoryFunctions memory;e2d_memory_functions(&memory);check(!e2d_ogv_memory(&memory),"Registration failed");
    size_t position=(size_t)strtoull(argv[1],NULL,10);e2d_test_arm(position);
    uint64_t epoch=e2d_ogv_failure_epoch();int status=exercise();size_t attempts=e2d_test_attempts();
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(status==(position?-1:0) && state.failures==!!position,"Comment header refusal was not reported");
    check((e2d_ogv_failure_epoch()!=epoch)==!!position,"Comment refusal epoch is absent");
    check(!state.nativeCurrent && state.allocated==state.retired,"Comment bit storage did not drain");
    e2d_test_arm(0);check(!exercise(),"Comment header retry failed");e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated==state.retired,"Comment retry did not drain");
    printf("{\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n",attempts,position,(unsigned long long)state.nativePeak);
    return 0;
}
