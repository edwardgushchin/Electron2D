#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x = 64) in;
#include "PhysicsResidentGeometry.inc.glsl"
struct VertexEdit { uvec2 target; vec2 point; };
struct GeometryEdit { uvec4 target; Geometry value; };
struct ShapeEdit { uvec4 target; Shape value; };
struct Proxy { vec4 bounds; ivec4 data; };
struct Node { vec4 bounds; int typeMask; int proxy; int hasCategory; int shape; };
layout(std430, set=0, binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430, set=0, binding=1) readonly buffer VertexEdits { VertexEdit vertexEdits[]; };
layout(std430, set=0, binding=2) readonly buffer GeometryEdits { GeometryEdit geometryEdits[]; };
layout(std430, set=0, binding=3) readonly buffer ShapeEdits { ShapeEdit shapeEdits[]; };
layout(std430, set=0, binding=4) readonly buffer Nodes { Node nodes[]; };
layout(std430, set=0, binding=5) readonly buffer FilterPairs { uvec4 filterPairs[]; };
layout(std430, set=0, binding=6) readonly buffer Filters { uint filters[]; };
layout(std430, set=1, binding=6) buffer Centers { vec2 centers[]; };
layout(std430, set=0, binding=7) readonly buffer Corrections { vec4 corrections[]; };
layout(std430, set=1, binding=0) buffer Vertices { vec2 vertices[]; };
layout(std430, set=1, binding=1) buffer Geometries { Geometry geometries[]; };
layout(std430, set=1, binding=2) buffer Shapes { Shape shapes[]; };
layout(std430, set=1, binding=3) buffer Proxies { Proxy proxies[]; };
layout(std430, set=1, binding=4) buffer Summary { uvec2 summary; };
layout(std430, set=1, binding=5) buffer Pairs { uvec4 pairs[]; };
layout(std140, set=2, binding=0) uniform Settings { uvec4 work; uvec4 counts; vec4 tolerances; uvec4 filterInfo; };
vec2 rotatePoint(vec2 p, vec2 q) { return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y); }
bool finite4(vec4 v) { return !any(isnan(v)) && !any(isinf(v)); }
void fail(uint flag) { atomicOr(summary.x,flag); }

bool excepted(uint a,uint b)
{
    if(filterInfo.x==0u)return false;
    uvec2 pair=uvec2(min(a,b),max(a,b));
    uvec4 identity=uvec4(pair,bodies[pair.x].flags.x,bodies[pair.y].flags.x);
    uint at=pairHash(pair)&(filterInfo.x-1u);
    for(uint probe=0u;probe<filterInfo.x;probe++)
    {
        uint slot=filters[at];if(slot==0xffffffffu)return false;
        if(slot>=filterInfo.y){fail(8u);return true;}
        if(identity==filterPairs[slot])return true;
        at=(at+1u)&(filterInfo.x-1u);
    }
    fail(8u);return true;
}
bool accept(uint aIndex, uint bIndex)
{
    if (bIndex == aIndex) return false;
    Shape a=shapes[aIndex], b=shapes[bIndex];
    if (a.owner.x == b.owner.x) return false;
    bool sensorA=(a.policy.w&2u)!=0u, sensorB=(b.policy.w&2u)!=0u;
    if (sensorA || sensorB)
    {
        // Body queries own mixed pairs, so a large Area cannot serialize a whole receiver population.
        if(sensorA&&!sensorB)return false;
        if(sensorA&&sensorB&&bIndex<aIndex)return false;
        return (sensorA && (a.policy.z&b.policy.y)!=0u) || (sensorB && (b.policy.z&a.policy.y)!=0u);
    }
    if(bIndex<aIndex)return false;
    if (excepted(a.owner.x,b.owner.x)) return false;
    if (bodies[a.owner.x].flags.y<2u && bodies[b.owner.x].flags.y<2u) return false;
    return (a.policy.z&b.policy.y)!=0u && (b.policy.z&a.policy.y)!=0u;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;
    if (i>=work.y) return;
    if (work.x==0u) { VertexEdit e=vertexEdits[i]; if(e.target.x>=counts.z) {fail(1u);return;} vertices[e.target.x]=e.point; return; }
    if (work.x==1u) { GeometryEdit e=geometryEdits[i]; if(e.target.x>=counts.y) {fail(1u);return;} geometries[e.target.x]=e.value; return; }
    if (work.x==2u) { ShapeEdit e=shapeEdits[i]; if(e.target.x>=work.w) {fail(1u);return;} shapes[e.target.x]=e.value; return; }
    if (work.x==3u)
    {
        Proxy p; p.bounds=vec4(0); p.data=ivec4(-1,int(i),0,0);
        if (i>=work.w) {proxies[i]=p;return;}
        Shape s=shapes[i];
        if ((s.policy.w&1u)==0u) {proxies[i]=p;return;}
        if (s.owner.x>=work.z || s.owner.z>=counts.y) {fail(1u);proxies[i]=p;return;}
        ResidentBody b=bodies[s.owner.x]; Geometry g=geometries[s.owner.z];
        if (b.flags.w==0u || b.flags.x!=s.owner.y || g.data.w!=s.owner.w || g.data.y==0u) {proxies[i]=p;return;}
        if (g.data.x>counts.z || g.data.y>counts.z-g.data.x) {fail(1u);proxies[i]=p;return;}
        vec2 lower=vec2(3.402823466e38), upper=-lower;float sweepRadius=0;
        for(uint v=0u;v<g.data.y;v++)
        {
            vec2 local=s.pose.xy+rotatePoint(vertices[g.data.x+v],s.pose.zw);
            vec2 world=b.pose.xy+rotatePoint(local,b.pose.zw);
            lower=min(lower,world);upper=max(upper,world);
            if(tolerances.y>0)sweepRadius=max(sweepRadius,length(local-centers[s.owner.x])+g.parameters.x);
        }
        p.bounds=vec4(lower-vec2(g.parameters.x+0.5*tolerances.x),upper+vec2(g.parameters.x+0.5*tolerances.x));
        if(tolerances.y>0&&b.flags.y!=0u&&(b.flags.z&16u)==0u)
        {
            vec3 motion=b.velocity.xyz;
            if(tolerances.z!=0)motion+=corrections[s.owner.x].xyz;
            vec2 shift=tolerances.y*motion.xy;float turn=tolerances.y*motion.z;
            float pad=2*sweepRadius*sin(0.5*min(abs(turn),3.141592653589793));
            if(!finite4(vec4(shift,turn,pad))){fail(2u);proxies[i]=p;return;}
            p.bounds=vec4(p.bounds.xy+min(shift,vec2(0))-pad,p.bounds.zw+max(shift,vec2(0))+pad);
        }
        if (!finite4(p.bounds) || !finite4(vec4(p.bounds.zw-p.bounds.xy,0,0))) {fail(2u);proxies[i]=p;return;}
        p.data=ivec4(b.flags.y>=2u?2:int(b.flags.y),int(i),1,(s.policy.w&2u)!=0u?8:0);
        proxies[i]=p;return;
    }
    Proxy query=proxies[i];
    if(query.data.z==0)return;
    Shape a=shapes[i];
    int types=(a.policy.w&2u)!=0u?8:query.data.x==2?7:12;
    int at=1;
    while(at!=0)
    {
        Node n=nodes[at];
        bool overlap=all(lessThanEqual(n.bounds.xy,query.bounds.zw)) && all(lessThanEqual(query.bounds.xy,n.bounds.zw));
        if(n.hasCategory!=0 && (n.typeMask&types)!=0 && overlap)
        {
            if(at<int(counts.x)) {at*=2;continue;}
            if(accept(i,uint(n.shape)))
            {
                // ponytail: a global counter can serialize dense worlds; use per-query counts/prefix scan if measured contention dominates.
                uint index=atomicAdd(summary.y,1u);
                if(index==0xffffffffu)fail(4u);
                if(index<counts.w)
                {
                    uint first=min(i,uint(n.shape)),second=max(i,uint(n.shape));
                    pairs[index]=uvec4(first,second,shapes[first].policy.x,shapes[second].policy.x);
                }
            }
        }
        while(at>1 && (at&1)!=0)at/=2;
        at=at==1?0:at+1;
    }
}
