#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
struct FrameContact { uvec4 pair; uvec4 features; uvec4 owners; uvec4 links; vec4 positions; vec4 normalDepth; vec4 impulse; vec4 velocities; };
struct Report { uvec4 shapes; uvec2 collider; float depth; uint padding; vec4 positions; vec4 normalImpulse; vec4 velocities; };
layout(std430,set=0,binding=0) readonly buffer Points { ContactPoint points[]; };
layout(std430,set=0,binding=1) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=2) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=3) readonly buffer Centers { vec2 centers[]; };
layout(std430,set=0,binding=4) readonly buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=0,binding=5) readonly buffer Requests { uvec4 requests[]; };
layout(std430,set=1,binding=0) buffer Records { FrameContact records[]; };
layout(std430,set=1,binding=1) buffer Table { uint table[]; };
layout(std430,set=1,binding=2) buffer PointHeads { uint pointHeads[]; };
layout(std430,set=1,binding=3) buffer PointLinks { uvec2 pointLinks[]; };
layout(std430,set=1,binding=4) buffer BodyHeads { uint bodyHeads[]; };
layout(std430,set=1,binding=5) buffer Results { Report results[]; };
layout(std430,set=1,binding=6) buffer Counts { uint counts[]; };
layout(std430,set=1,binding=7) buffer Summary { uvec2 summary; };
layout(std140,set=2,binding=0) uniform Settings { uint stage; uint count; uint recordCount; uint tableCapacity; uint bodyCount; uint initialize; uint pointCount; uint capacity; };
const uint none=0xffffffffu, incoming=0x80000000u;
void fail(){atomicOr(summary.x,1u);}
uint hash(uvec4 pair,uvec4 features){return pairHash(pair.xy)^pairHash(pair.zw)^pairHash(features.xy)^pairHash(features.zw);}
bool matches(uint token,ContactPoint p)
{
    if((token&incoming)!=0u){uint at=token&~incoming;if(at>=pointCount){fail();return false;}return points[at].pair==p.pair&&points[at].features==p.features;}
    if(token>=recordCount){fail();return false;}return records[token].pair==p.pair&&records[token].features==p.features;
}
bool included(uint i){return points[i].normal.w==0;}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=count)return;
    if(stage==0u)
    {
        if(i<tableCapacity){table[i]=none;pointHeads[i]=none;}
        if(initialize!=0u&&i<bodyCount)bodyHeads[i]=none;
        return;
    }
    if(stage==1u)
    {
        FrameContact r=records[i];uint slot=hash(r.pair,r.features)&(tableCapacity-1u);
        for(uint probe=0u;probe<tableCapacity;probe++){if(atomicCompSwap(table[slot],none,i)==none)return;slot=(slot+1u)&(tableCapacity-1u);}
        fail();return;
    }
    if(stage==2u)
    {
        pointLinks[i]=uvec2(none);if(!included(i))return;
        ContactPoint p=points[i];uint slot=hash(p.pair,p.features)&(tableCapacity-1u);
        for(uint probe=0u;probe<tableCapacity;probe++)
        {
            uint token=atomicCompSwap(table[slot],none,incoming|i);
            if(token==none||matches(token,p)){uint previous=atomicExchange(pointHeads[slot],i);pointLinks[i]=uvec2(slot,previous);return;}
            slot=(slot+1u)&(tableCapacity-1u);
        }
        fail();return;
    }
    if(stage==3u)
    {
        uint slot=pointLinks[i].x;if(slot==none||pointHeads[slot]!=i)return;
        uint token=table[slot],at=i,last=i;vec2 total=vec2(0);float depth=-3.402823466e38;
        for(uint visited=0u;at!=none;visited++)
        {
            if(at>=pointCount||visited>=pointCount){fail();return;}
            ContactPoint p=points[at];vec2 value=impulses[at].physical.xy,n=p.normal.xy;
            total+=n*value.x+vec2(n.y,-n.x)*value.y;depth=max(depth,-p.normal.z);last=max(last,at);at=pointLinks[at].y;
        }
        ContactPoint p=points[last];Shape sa=shapes[p.pair.x],sb=shapes[p.pair.y];
        uint ai=sa.owner.x,bi=sb.owner.x;if(ai>=bodyCount||bi>=bodyCount){fail();return;}
        FrameContact r;
        if((token&incoming)!=0u)
        {
            token=atomicAdd(summary.y,1u);if(token>=capacity){fail();return;}
            r=FrameContact(p.pair,p.features,uvec4(ai,bi,sa.owner.y,sb.owner.y),uvec4(none),vec4(0),vec4(0,0,depth,0),vec4(0),vec4(0));
            r.links.x=atomicExchange(bodyHeads[ai],token);r.links.y=atomicExchange(bodyHeads[bi],token);
            table[slot]=token;
        }
        else r=records[token];
        r.positions=vec4(bodies[ai].pose.xy+rotate(bodies[ai].pose.zw,p.anchors.xy),bodies[bi].pose.xy+rotate(bodies[bi].pose.zw,p.anchors.zw));
        r.normalDepth=vec4(p.normal.xy,max(depth,r.normalDepth.z),p.normal.z);r.impulse.xy+=total;
        if(!finite4(r.positions)||!finite4(r.normalDepth)||!finite4(r.impulse)){fail();return;}
        records[token]=r;return;
    }
    if(stage==4u)
    {
        uint at=bodyHeads[i],previous=none;
        // Reverse the prepend list once, preserving first-encounter selection across intervals.
        for(uint visited=0u;at!=none;visited++)
        {
            if(at>=recordCount||visited>=recordCount){fail();return;}
            FrameContact r=records[at];bool second=r.owners.y==i;
            if(!second&&r.owners.x!=i){fail();return;}
            uint next=second?r.links.y:r.links.x;ResidentBody b=bodies[i];
            vec2 point=second?r.positions.zw:r.positions.xy;
            vec2 v=velocity(b,point-b.pose.xy-rotate(b.pose.zw,centers[i]));
            if(!finite4(vec4(v,0,0))){fail();return;}
            if(second){records[at].links.y=previous;records[at].velocities.zw=v;}
            else{records[at].links.x=previous;records[at].velocities.xy=v;}
            previous=at;at=next;
        }
        bodyHeads[i]=previous;return;
    }
    if(stage==5u)
    {
        uvec4 request=requests[i];uint selected=0u;
        if(request.x>=bodyCount){counts[i]=0u;return;}
        if(request.z==0u){counts[i]=0u;return;}
        uint at=bodyHeads[request.x];
        for(uint visited=0u;at!=none;visited++)
        {
            if(at>=recordCount||visited>=recordCount){fail();return;}
            FrameContact r=records[at];bool second=r.owners.y==request.x;
            if(!second&&r.owners.x!=request.x){fail();return;}
            at=second?r.links.y:r.links.x;
            if((second?r.owners.w:r.owners.z)!=request.y)continue;
            uint target=selected;
            if(selected==request.z)
            {
                // ponytail: O(receiver contacts * limit), matching direct-state cap selection; use a stable min-heap if measured large limits dominate.
                target=0u;for(uint j=1u;j<selected;j++)if(results[request.w+j].depth<results[request.w+target].depth)target=j;
                if(r.normalDepth.z<=results[request.w+target].depth)continue;
            }
            else selected++;
            float sign=second?1:-1;
            results[request.w+target]=Report(second?r.pair.ywxz:r.pair.xzyw,second?r.owners.xz:r.owners.yw,r.normalDepth.z,0u,
                second?r.positions.zwxy:r.positions,vec4(sign*r.normalDepth.xy,sign*r.impulse.xy),second?r.velocities.zwxy:r.velocities);
        }
        counts[i]=selected;return;
    }
    if(stage==6u){records[i].impulse=vec4(0);records[i].velocities=vec4(0);}
}
