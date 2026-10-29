#include "msdfgen.h"
#include <cstdio>
#include <cstdlib>
#include <cstddef>
#include <new>

struct alignas(std::max_align_t) Block { size_t size; };
static size_t current, countdown, failures;
static void require(bool value) { if (!value) std::abort(); }
void *e2d_text_allocate(size_t size, void *) {
    if (countdown && !--countdown) { ++failures; return nullptr; }
    auto block = static_cast<Block *>(std::malloc(sizeof(Block)+size));
    if (!block) return nullptr;
    block->size=size; current+=size; return block+1;
}
void e2d_text_release(void *value, void *) {
    if (!value) return;
    auto block=static_cast<Block *>(value)-1;require(current>=block->size);current-=block->size;std::free(block);
}
int main() {
    using namespace msdfgen;
    {
        Bitmap<float,4> original(3,3), target(2,2); target(0,0)[0]=42;
        const auto baseline=current;
        for(int kind=0;kind<3;kind++) {
            countdown=1;
            try {
                if(kind==0) target=original;
                if(kind==1) target=static_cast<BitmapConstRef<float,4>>(original);
                if(kind==2) target=original.getConstSection(0,0,3,3);
                std::abort();
            } catch(const std::bad_alloc &) { }
            countdown=0;require(target.width()==2&&target.height()==2&&target(0,0)[0]==42&&current==baseline);
        }
        target=original;require(target.width()==3&&target.height()==3);
    }
    require(!current);
    {
        EdgeHolder original(new LinearSegment({0,0},{1,1})), target(new LinearSegment({2,2},{3,3}));
        const auto baseline=current;countdown=1;
        try { target=original;std::abort(); } catch(const std::bad_alloc &) { }
        countdown=0;require(target->point(0)==Point2(2,2)&&current==baseline);
        target=original;require(target->point(0)==Point2(0,0));
    }
    require(!current);
    LinearSegment line({0,0},{1,1});QuadraticSegment quadratic({0,0},{1,1},{2,0});CubicSegment cubic({0,0},{1,1},{2,1},{3,0});
    for(auto edge:{static_cast<EdgeSegment *>(&line),static_cast<EdgeSegment *>(&quadratic),static_cast<EdgeSegment *>(&cubic)})
        for(size_t position=1;position<=3;position++) {
            countdown=position;
            try { e2d_msdf::Parts<EdgeSegment,3> parts;edge->splitInThirds(parts[0],parts[1],parts[2]);std::abort(); }
            catch(const std::bad_alloc &) { }
            countdown=0;require(!current);
        }
    try {
        e2d_msdf::Parts<EdgeSegment,3> parts;cubic.splitInThirds(parts[0],parts[1],parts[2]);
        e2d_msdf::vector<EdgeHolder> adopted;countdown=1;adopted.push_back(EdgeHolder(parts.take(0)));std::abort();
    } catch(const std::bad_alloc &) { }
    countdown=0;require(!current&&failures==14);
    std::puts("MSDF ownership: 14 bitmap/copy/split/adoption failures; prior owners preserved and all payloads drained.");
}
