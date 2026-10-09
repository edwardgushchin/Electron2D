#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
layout(local_size_x=64) in;
struct Field { uvec4 owner; uvec4 policy; vec4 gravity; vec4 damping; };
layout(std430,set=0,binding=0) readonly buffer Profiles { Field profiles[]; };
layout(std430,set=0,binding=1) readonly buffer Contacts { ContactPoint contacts[]; };
layout(std430,set=0,binding=2) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=1) buffer Resolved { vec4 resolved[]; };
layout(std430,set=1,binding=2) buffer Heads { uint heads[]; };
layout(std430,set=1,binding=3) buffer Links { uvec2 links[]; };
layout(std430,set=1,binding=4) buffer Lookup { uint lookup[]; };
layout(std430,set=1,binding=5) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; uint areaCount; float delta; uint padding1; uint padding2; Field world; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
vec2 rotate(vec2 q,vec2 p){return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y);}
vec2 gravity(Field f,vec4 pose,vec2 position)
{
    if(f.policy.y==0u)return f.gravity.xy*f.gravity.z;
    vec2 toward=pose.xy+rotate(pose.zw,f.gravity.xy)-position;
    float squared=dot(toward,toward);if(squared==0)return vec2(0);
    float strength=f.gravity.w>0?f.gravity.z*f.gravity.w*f.gravity.w/squared:f.gravity.z;
    return (toward/sqrt(squared))*strength;
}
void linkArea(uint at,Shape a,Shape b)
{
    uint rank=lookup[a.owner.x];
    if(rank==none||bodies[b.owner.x].flags.y==0u||(a.policy.z&b.policy.y)==0u)return;
    uint old=atomicExchange(heads[b.owner.x],at);links[at]=uvec2(rank,old);
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==0u){heads[i]=lookup[i]=none;return;}
    if(control.x==1u)
    {
        Field f=profiles[i];
        if(f.owner.x>=control.z||bodies[f.owner.x].flags.w==0u||bodies[f.owner.x].flags.x!=f.owner.y||bodies[f.owner.x].flags.y!=0u){fail();return;}
        lookup[f.owner.x]=i;return;
    }
    if(control.x==2u)
    {
        ContactPoint p=contacts[i];if(p.normal.w==0||p.normal.z>0.05001)return;
        Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
        if(a.owner.x>=control.z||b.owner.x>=control.z||a.policy.x!=p.pair.z||b.policy.x!=p.pair.w){fail();return;}
        linkArea(2u*i,a,b);linkArea(2u*i+1u,b,a);return;
    }
    ResidentBody body=bodies[i];if(body.flags.w==0u)return;
    vec4 total=vec4(0);bvec3 done=bvec3(false);
    uint minimum=0u;
    if(body.flags.y!=0u)
    {
        // ponytail: selection is O(overlap links * distinct Areas); sort receiver/rank pairs if heavily nested fields dominate measured cost.
        for(uint count=0u;count<areaCount;count++)
        {
            uint rank=none,at=heads[i];
            for(uint depth=0u;at!=none;depth++)
            {
                if(at>=2u*control.w||depth>=2u*control.w){fail();return;}
                uvec2 link=links[at];if(link.x>=minimum)rank=min(rank,link.x);at=link.y;
            }
            if(rank==none)break;
            if(rank>=areaCount){fail();return;}
            Field f=profiles[rank];vec4 pose=bodies[f.owner.x].pose;
            uint g=f.owner.z,l=f.owner.w,a=f.policy.x;
            if(!done.x&&g!=0u)
            {
                vec2 value=gravity(f,pose,body.pose.xy);
                if(g<=2u)total.xy+=value;else total.xy=value;
                done.x=g==2u||g==3u;
            }
            if(!done.y&&l!=0u){if(l<=2u)total.z+=f.damping.x;else total.z=f.damping.x;done.y=l==2u||l==3u;}
            if(!done.z&&a!=0u){if(a<=2u)total.w+=f.damping.y;else total.w=f.damping.y;done.z=a==2u||a==3u;}
            if(all(done))break;
            minimum=rank+1u;
        }
        if(!done.x)total.xy+=gravity(world,vec4(0,0,1,0),body.pose.xy);
        if(!done.y)total.z+=world.damping.x;
        if(!done.z)total.w+=world.damping.y;
    }
    vec4 result=resolved[i];
    if(!resolveBodyFields(body,result,total,delta)){fail();return;}
    resolved[i]=result;bodies[i]=body;
}
