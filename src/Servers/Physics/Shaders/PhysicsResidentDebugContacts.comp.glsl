#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Contacts { ContactPoint contacts[]; };
layout(std430,set=0,binding=1) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=2) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=0) buffer Points { vec2 points[]; };
layout(std430,set=1,binding=1) buffer Summary { uvec2 summary; };
layout(std140,set=2,binding=0) uniform Settings { uint count; uint shapeCount; uint bodyCount; uint limit; };
vec2 rotate(vec2 q,vec2 v){return vec2(q.x*v.x-q.y*v.y,q.y*v.x+q.x*v.y);}
bool awake(ResidentBody b){return b.flags.y!=0u&&(b.flags.z&16u)==0u;}
void main()
{
    uint i=gl_GlobalInvocationID.x;
    if(count==0u){if(i==0u)summary=uvec2(0);return;}
    if(i>=count)return;
    ContactPoint p=contacts[i];if(p.normal.w!=0||p.normal.z>=0)return;
    if(p.pair.x>=shapeCount||p.pair.y>=shapeCount){atomicOr(summary.x,1u);return;}
    Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
    if(a.policy.x!=p.pair.z||b.policy.x!=p.pair.w||a.owner.x>=bodyCount||b.owner.x>=bodyCount){atomicOr(summary.x,1u);return;}
    ResidentBody ba=bodies[a.owner.x],bb=bodies[b.owner.x];
    if(ba.flags.w==0u||bb.flags.w==0u||ba.flags.x!=a.owner.y||bb.flags.x!=b.owner.y){atomicOr(summary.x,1u);return;}
    if(!awake(ba)&&!awake(bb))return;
    vec4 pair=vec4(ba.pose.xy+rotate(ba.pose.zw,p.anchors.xy),bb.pose.xy+rotate(bb.pose.zw,p.anchors.zw));
    if(!finiteField(pair)){atomicOr(summary.x,1u);return;}
    uint at=atomicAdd(summary.y,2u);
    if(at<limit)points[at]=pair.xy;
    if(at+1u<limit)points[at+1u]=pair.zw;
}
