#include "msdfgen.h"
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <limits>
#include <string>
#include <vector>

struct Edge { int32_t kind, contour; double ax, ay, bx, by, cx, cy, dx, dy; };
extern "C" {
int e2d_text_memory(void *(*)(const void *, size_t), void *(*)(const void *, void *, size_t), void (*)(const void *, void *));
int32_t e2d_font_msdf_generate(const Edge *, int32_t, int32_t, int32_t, uint8_t **, int32_t *, int32_t *, double *, double *);
void e2d_font_msdf_free(uint8_t *);
}
static void require(bool value, const char *message) { if (!value) { std::fprintf(stderr, "%s\n", message); std::abort(); } }
struct alignas(std::max_align_t) Block { size_t size; };
static size_t current, peak, calls, fail_at, failures;
static void *allocate(const void *, size_t size) {
    if (++calls == fail_at) { ++failures; return nullptr; }
    if (size > std::numeric_limits<size_t>::max() - sizeof(Block)) return nullptr;
    auto block = static_cast<Block *>(std::malloc(sizeof(Block) + size));
    if (!block) return nullptr;
    block->size = size; current += size; peak = std::max(peak, current); return block + 1;
}
static void release(const void *, void *value) {
    if (!value) return;
    auto block = static_cast<Block *>(value) - 1;
    require(current >= block->size, "Invalid MSDF allocation ownership"); current -= block->size; std::free(block);
}
static void *resize(const void *, void *, size_t) { std::abort(); }
struct Image { int32_t width = 0, height = 0; double left = 0, top = 0; std::vector<uint8_t> pixels; };
static Image reference(const std::vector<Edge> &edges, int reverse) {
    msdfgen::Shape shape;
    for (const auto &e : edges) {
        if (e.contour == static_cast<int>(shape.contours.size())) shape.addContour();
        auto &contour = shape.contours[e.contour];
        if (e.kind == 1) contour.addEdge(new msdfgen::LinearSegment({e.ax,e.ay}, {e.bx,e.by}));
        else if (e.kind == 2) contour.addEdge(new msdfgen::QuadraticSegment({e.ax,e.ay}, {e.bx,e.by}, {e.cx,e.cy}));
        else contour.addEdge(new msdfgen::CubicSegment({e.ax,e.ay}, {e.bx,e.by}, {e.cx,e.cy}, {e.dx,e.dy}));
    }
    if (reverse) for (auto &c : shape.contours) c.reverse();
    shape.setYAxisOrientation(msdfgen::Y_DOWNWARD); shape.normalize(); require(shape.validate(), "Invalid reference outline");
    const auto bounds = shape.getBounds(4); const int width = static_cast<int>(bounds.r-bounds.l), height = static_cast<int>(bounds.t-bounds.b);
    msdfgen::edgeColoringSimple(shape, 3.0); msdfgen::Bitmap<float,4> bitmap(width,height);
    msdfgen::generateMTSDF(bitmap,shape,msdfgen::Projection({1,1},{-bounds.l,-bounds.b}),msdfgen::Range(4),msdfgen::MSDFGeneratorConfig(true,msdfgen::ErrorCorrectionConfig()));
    Image image; image.width=width+2; image.height=height+2; image.left=bounds.l-1; image.top=bounds.t+1;
    image.pixels.resize(static_cast<size_t>(image.width)*image.height*4);
    for(int y=0;y<height;y++) for(int x=0;x<width;x++) for(int c=0;c<4;c++)
        image.pixels[((y+1)*image.width+x+1)*4+c]=static_cast<uint8_t>(std::clamp(bitmap(x,y)[c]*256.f,0.f,255.f));
    return image;
}
int main(int argc, char **argv) {
    if (argc != 3) return 1;
    const std::string mode=argv[1];
    std::vector<Edge> edges={{1,0,0,0,24,0},{1,0,24,0,0,24},{1,0,0,24,0,0}};
    if(mode=="quadratic") edges={{2,0,0,0,15,-5,30,0},{2,0,30,0,15,25,0,0}};
    if(mode=="cubic") edges={{3,0,0,0,20,0,0,20,0,0}};
    if(mode=="teardrop") edges={{3,0,0,0,10,0,20,20,20,0},{3,0,20,0,20,-20,0,-20,0,0}};
    if(mode=="hole") edges={{1,0,0,0,30,0},{1,0,30,0,30,30},{1,0,30,30,0,30},{1,0,0,30,0,0},{1,1,10,10,10,20},{1,1,10,20,20,20},{1,1,20,20,20,10},{1,1,20,10,10,10}};
    if(mode=="invalid") edges.back().kind=4;
    if(mode=="budget") edges={{1,0,0,0,10000,0},{1,0,10000,0,0,10000},{1,0,0,10000,0,0}};
    if(mode=="empty") edges.clear();
    const int normal=mode=="invalid"?1:mode=="budget"?2:0, reverse=mode=="reverse";
    Image expected; if(!normal&&!edges.empty()) expected=reference(edges,reverse);
    uint8_t *pixels=nullptr; int32_t width=0,height=0; double left=0,top=0;
    if(!edges.empty()) require(e2d_font_msdf_generate(edges.data(),static_cast<int32_t>(edges.size()),4,reverse,&pixels,&width,&height,&left,&top)==3&&pixels==nullptr,
        "Unregistered MSDF generation bypassed the native allocator");
    require(e2d_text_memory(allocate,resize,release)==0,"MSDF callback registration failed");
    fail_at=std::strtoull(argv[2],nullptr,10);
    const int status=e2d_font_msdf_generate(edges.data(),static_cast<int32_t>(edges.size()),4,reverse,&pixels,&width,&height,&left,&top);
    const size_t observed=calls;
    require(!fail_at||(calls>=fail_at&&failures==1),"Requested MSDF failure was not exercised");
    require(status==(failures?3:normal),"MSDF error type changed");
    if(status) require(!pixels&&!width&&!height&&!left&&!top&&current==0,"Failed MSDF generation retained storage or published output");
    else {
        require(width==expected.width&&height==expected.height&&left==expected.left&&top==expected.top,"MSDF bounds changed");
        require(current==expected.pixels.size(),"MSDF result payload bypassed the native ledger");
        if(pixels) require(!std::memcmp(pixels,expected.pixels.data(),expected.pixels.size())&&peak>current,"MSDF pixels changed or transient storage was missed");
    }
    e2d_font_msdf_free(pixels); require(current==0,"MSDF output did not drain"); fail_at=0;
    require(e2d_font_msdf_generate(edges.data(),static_cast<int32_t>(edges.size()),4,reverse,&pixels,&width,&height,&left,&top)==normal,"MSDF retry failed");
    e2d_font_msdf_free(pixels); require(current==0,"MSDF retry leaked");
    std::printf("{\"Calls\":%zu,\"Peak\":%zu,\"Failures\":%zu,\"Current\":%zu}\n",observed,peak,failures,current);
}
