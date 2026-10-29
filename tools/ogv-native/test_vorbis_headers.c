#include "bridge.h"
#include "memory.h"
#include "../enet-native/memory.h"
#include <vorbis/codec.h>
#include <vorbis/vorbisenc.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
extern void e2d_test_arm(size_t);
extern size_t e2d_test_attempts(void);
static char comment[8193];
static void check(int value, const char *message) { if (!value) { fprintf(stderr, "%s\n", message); exit(1); } }
static void drained(void) {
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(!state.nativeCurrent && state.allocated==state.retired,"Header storage did not drain");
}
static int headers(vorbis_dsp_state *dsp, vorbis_comment *comment, E2DOGVOwner *owner, uint64_t *previous) {
    E2DMemorySnapshot before;e2d_memory_snapshot(&before);size_t capacity=owner->capacity;
    ogg_packet packets[3]={{0}};
    int status=vorbis_analysis_headerout(dsp,comment,packets,packets+1,packets+2);
    if(status) {
        for(int i=0;i<3;i++)check(!packets[i].packet && !packets[i].bytes,"Refused header was published");
        drained();return -1;
    }
    uint64_t bytes=0;
    for(int i=0;i<3;i++) {
        check(packets[i].bytes>7 && packets[i].packet[0]==1+2*i && !memcmp(packets[i].packet+1,"vorbis",6),"Header signature changed");
        check(packets[i].packetno==i && packets[i].b_o_s==!i && !packets[i].e_o_s,"Header packet metadata changed");
        bytes+=(uint64_t)packets[i].bytes;
    }
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(state.nativeCurrent==before.nativeCurrent-*previous+bytes+(owner->capacity-capacity)*sizeof(void *),"Published DSP headers are outside the shared allocator");
    *previous=bytes;
    vorbis_info info;vorbis_info_init(&info);vorbis_comment decoded;vorbis_comment_init(&decoded);
    for(int i=0;i<3;i++)check(!vorbis_synthesis_headerin(&info,&decoded,packets+i),"Published header cannot be decoded");
    check(info.channels==2 && info.rate==48000 && decoded.comments==1 && decoded.comment_lengths[0]==8192 && !memcmp(decoded.user_comments[0],comment->user_comments[0],8192),"Decoded header configuration changed");
    vorbis_comment_clear(&decoded);vorbis_info_clear(&info);return 0;
}
static int exercise(size_t *attempts) {
    E2DOGVOwner *owner=e2d_ogv_context_zero(sizeof(*owner));
    if(!owner){*attempts=e2d_test_attempts();return -1;}
    E2DOGVScope scope;check(!e2d_ogv_scope_enter(&scope,owner),"Scope entry failed");
    if(setjmp(scope.jump)) {
        *attempts=e2d_test_attempts();e2d_ogv_scope_leave(&scope);
        e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);drained();return -1;
    }
    vorbis_info info;vorbis_info_init(&info);
    check(!vorbis_encode_init_vbr(&info,2,48000,.4f),"Encoder configuration failed");
    vorbis_dsp_state dsp;check(!vorbis_analysis_init(&dsp,&info),"DSP initialization failed");
    vorbis_comment vc;vorbis_comment_init(&vc);vorbis_comment_add(&vc,comment);
    uint64_t previous=0;
    for(int i=0;i<3;i++) {
        check(!headers(&dsp,&vc,owner,&previous),"Header packing failed");
        if(i==1) {
            E2DMemorySnapshot before,after;e2d_memory_snapshot(&before);
            check(vorbis_analysis_buffer(&dsp,1)!=NULL,"PCM buffer acquisition failed");
            e2d_memory_snapshot(&after);
            check(before.nativeCurrent-after.nativeCurrent==previous,"PCM request did not retire exactly its header bytes");previous=0;
        }
    }
    *attempts=e2d_test_attempts();
    vorbis_dsp_clear(&dsp);vorbis_comment_clear(&vc);vorbis_info_clear(&info);
    check(!owner->count,"DSP owner retained payload after normal cleanup");
    e2d_ogv_scope_leave(&scope);e2d_ogv_owner_clear(owner);e2d_ogv_release(owner);drained();return 0;
}
int main(int argc,char **argv) {
    check(argc==2,"Expected allocation failure position");
    for(size_t i=0;i<sizeof(comment)-1;i++)comment[i]=(char)('!'+i%90);
    E2DMemoryFunctions memory;e2d_memory_functions(&memory);check(!e2d_ogv_memory(&memory),"Registration failed");
    size_t position=(size_t)strtoull(argv[1],NULL,10);e2d_test_arm(position);
    size_t attempts,retry_attempts;
    uint64_t epoch=e2d_ogv_failure_epoch();int status=exercise(&attempts);
    E2DMemorySnapshot state;e2d_memory_snapshot(&state);
    check(status==(position?-1:0) && state.failures==!!position,"Header allocation refusal was not reported");
    check((e2d_ogv_failure_epoch()!=epoch)==!!position,"Header refusal epoch is absent");
    e2d_test_arm(0);check(!exercise(&retry_attempts),"New-DSP header retry failed");drained();
    printf("{\"attempts\":%zu,\"failure\":%zu,\"peak\":%llu}\n",attempts,position,(unsigned long long)state.nativePeak);
    return 0;
}
