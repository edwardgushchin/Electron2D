#version 450
layout(local_size_x = 64) in;
struct Proxy { vec4 bounds; ivec4 data; };
struct Node { vec4 bounds; int typeMask; int proxy; int hasCategory; int shape; };
layout(std430, set = 0, binding = 0) readonly buffer Updates { Proxy updates[]; };
layout(std430, set = 1, binding = 0) buffer Proxies { Proxy proxies[]; };
layout(std430, set = 1, binding = 1) buffer Nodes { Node nodes[]; };
layout(std430, set = 1, binding = 2) buffer Order { uvec2 order[]; };
layout(std140, set = 2, binding = 0) uniform Settings { uvec4 settings; uvec4 sorting; };

uint spread16(uint x)
{
    x &= 0xffffu;
    x = (x | (x << 8)) & 0x00ff00ffu;
    x = (x | (x << 4)) & 0x0f0f0f0fu;
    x = (x | (x << 2)) & 0x33333333u;
    return (x | (x << 1)) & 0x55555555u;
}
bool greater(uvec2 a, uvec2 b) { return a.x > b.x || (a.x == b.x && a.y > b.y); }

void main()
{
    uint i = gl_GlobalInvocationID.x;
    if (i >= settings.x) return;
    uint operation = settings.z;
    if (operation == 0) { Proxy p = updates[i]; proxies[p.data.y] = p; return; }
    if (operation == 1) { order[i] = uvec2(0, i); return; }
    if (operation == 2)
    {
        Proxy p = proxies[order[i].y];
        Node n;
        bool present = p.data.x != -1 && p.data.z != 0;
        n.bounds = present ? p.bounds : vec4(3.402823466e38, 3.402823466e38, -3.402823466e38, -3.402823466e38);
        n.typeMask = present ? ((1 << (p.data.x & 3)) | p.data.w) : 0;
        n.proxy = p.data.x; n.hasCategory = present ? 1 : 0; n.shape = p.data.y;
        nodes[settings.y + i] = n;
        return;
    }
    if (operation == 3)
    {
        uint at = settings.w + i;
        Node a = nodes[2*at], b = nodes[2*at+1], n;
        n.bounds = vec4(min(a.bounds.xy,b.bounds.xy), max(a.bounds.zw,b.bounds.zw));
        n.typeMask = a.typeMask | b.typeMask; n.hasCategory = a.hasCategory | b.hasCategory;
        n.proxy = -1; n.shape = -1; nodes[at] = n;
        return;
    }
    if (operation == 4)
    {
        Proxy p = proxies[i];
        uint key = 0xffffffffu;
        if (p.data.x != -1 && p.data.z != 0)
        {
            vec4 root = nodes[1].bounds;
            vec2 center = 0.5*p.bounds.xy + 0.5*p.bounds.zw;
            vec2 extent = 0.5*root.zw - 0.5*root.xy;
            vec2 relative = clamp((0.5*center - 0.5*root.xy) / max(extent,vec2(1e-30)), vec2(0), vec2(1));
            uvec2 xy = uvec2(min(relative * 65536.0, vec2(65535.0)));
            key = spread16(xy.x) | (spread16(xy.y) << 1);
        }
        order[i] = uvec2(key, i);
        return;
    }
    uint other = i ^ sorting.y;
    if (other <= i) return;
    uvec2 a = order[i], b = order[other];
    bool ascending = (i & sorting.x) == 0;
    if (ascending ? greater(a,b) : greater(b,a)) { order[i] = b; order[other] = a; }
}
