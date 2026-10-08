#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsResidentJoint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Points { ContactPoint points[]; };
layout(std430,set=0,binding=1) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=2) readonly buffer Joints { ResidentJoint joints[]; };
layout(std430,set=0,binding=3) readonly buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
// Previous/current component parent, aggregate wake/ineligible bits, minimum clock bits, generation.
layout(std430,set=1,binding=1) buffer Graph { uvec4 graph[]; };
// Endpoints and generations retained to wake former neighbours after removal/teleport/filter edits.
layout(std430,set=1,binding=2) buffer Edges { uvec4 edges[]; };
layout(std430,set=1,binding=3) buffer Status { uint status; uint awakeCount; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; uvec4 previous; vec4 policy; vec4 gravity; };
const uint none=0xffffffffu;
void fail(){atomicOr(status,1u);}
uint root(uint at,uint limit)
{
    for(uint depth=0u;depth<limit;depth++)
    {
        if(at>=limit){fail();return none;}
        uint parent=atomicAdd(graph[at].x,0u);
        if(parent==at)return at;
        if(parent>at){fail();return none;}
        uint grand=atomicAdd(graph[parent].x,0u);
        if(grand>parent){fail();return none;}
        atomicMin(graph[at].x,grand);at=grand;
    }
    fail();return none;
}
void unite(uint a,uint b)
{
    for(uint retry=0u;retry<control.z;retry++)
    {
        a=root(a,control.z);b=root(b,control.z);if(a==none||b==none||a==b)return;
        uint hi=max(a,b),lo=min(a,b);
        if(atomicCompSwap(graph[hi].x,hi,lo)==hi)return;
    }
    fail();
}
bool dynamicBody(uint id) {return id!=none&&bodies[id].flags.w!=0u&&bodies[id].flags.y>=2u;}
bool changed(uint id,uint generation)
{
    return id!=none&&(bodies[id].flags.w==0u||bodies[id].flags.x!=generation||(bodies[id].flags.z&32u)!=0u);
}
void markOld(uint id,uint generation)
{
    if(id==none||id>=previous.y||graph[id].w!=generation)return;
    uint r=root(id,previous.y);if(r!=none)atomicOr(graph[r].y,3u);
}
void wake(uint id,bool reset)
{
    ResidentBody b=bodies[id];if(b.flags.w==0u||b.flags.y<2u)return;
    if((b.flags.z&16u)!=0u)
    {
        if(previous.w!=0u)bodyForces(b,policy.w,gravity.xy);
        b.flags.z&=~16u;b.velocity.w=0;
    }
    if(reset)b.velocity.w=0;
    if(!finite4(b.velocity)){fail();return;}
    bodies[id]=b;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    uint stage=control.x;
    if(stage==0u){graph[i].y=0u;graph[i].z=none;return;}
    if(stage==1u)
    {
        uvec4 e=edges[i];
        if(e.x==none)return;
        if(changed(e.x,e.z))markOld(e.y,e.w);
        if(changed(e.y,e.w))markOld(e.x,e.z);
        return;
    }
    if(stage==2u)
    {
        if(bodies[i].flags.x!=graph[i].w||(bodies[i].flags.z&64u)!=0u)return;
        uint r=root(i,previous.y);if(r!=none&&(graph[r].y&2u)!=0u)wake(i,true);
        return;
    }
    if(stage==3u){graph[i]=uvec4(i,0u,none,bodies[i].flags.x);return;}
    if(stage==4u)
    {
        uvec4 e=uvec4(none);
        if(i<control.w)
        {
            ContactPoint p=points[i];
            if(p.normal.w==0)
            {
                Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
                if(a.policy.x!=p.pair.z||b.policy.x!=p.pair.w){fail();return;}
                e=uvec4(a.owner.x,b.owner.x,a.owner.y,b.owner.y);
            }
        }
        else
        {
            ResidentJoint j=joints[i-control.w];
            if(j.ids.y!=3u)e=uvec4(j.ids.zw,j.identity.xy);
        }
        edges[i]=e;if(e.x==none)return;
        if(e.x>=control.z||(e.y!=none&&e.y>=control.z)){fail();return;}
        if(bodies[e.x].flags.x!=e.z||bodies[e.x].flags.w==0u||
            (e.y!=none&&(bodies[e.y].flags.x!=e.w||bodies[e.y].flags.w==0u))){fail();return;}
        if(dynamicBody(e.x)&&dynamicBody(e.y))unite(e.x,e.y);
        return;
    }
    if(stage==5u)
    {
        if(!dynamicBody(i))return;
        uint r=root(i,control.z);if(r==none)return;
        uint flags=bodies[i].flags.z,mask=(flags&16u)==0u?1u:0u;
        if(((flags&32u)!=0u&&(flags&64u)==0u)||gravity.z!=0)mask=3u;
        atomicOr(graph[r].y,mask);return;
    }
    if(stage==6u)
    {
        uvec4 e=edges[i];if(e.x==none)return;
        bool a=dynamicBody(e.x),b=dynamicBody(e.y),wakeA=false,wakeB=false;
        if(e.y!=none)
        {
            ResidentBody ba=bodies[e.x],bb=bodies[e.y];
            wakeA=a&&!b&&((bb.flags.z&32u)!=0u||bb.velocity.xyz!=vec3(0));
            wakeB=b&&!a&&((ba.flags.z&32u)!=0u||ba.velocity.xyz!=vec3(0));
        }
        if(i>=control.w)
        {
            ResidentJoint j=joints[i-control.w];bool driving=false;
            if(j.ids.y==0u)driving=(j.identity.z&2u)!=0u&&j.motorSpring.y>0&&j.motorSpring.x!=0;
            if(j.ids.y==2u)
            {
                ResidentBody ba=bodies[e.x],bb=e.y==none?worldBody():bodies[e.y];
                vec2 d=bb.pose.xy+rotate(bb.pose.zw,j.frameB.xy)-ba.pose.xy-rotate(ba.pose.zw,j.frameA.xy);
                float distance=length(d);
                if(isnan(distance)||isinf(distance)){fail();return;}
                driving=j.motorSpring.w!=0&&distance>1.1920929e-5&&abs(distance-j.motorSpring.z)>1.1920929e-5;
            }
            wakeA=wakeA||(a&&driving);wakeB=wakeB||(b&&driving);
        }
        if(wakeA){uint r=root(e.x,control.z);if(r!=none)atomicOr(graph[r].y,3u);}
        if(wakeB){uint r=root(e.y,control.z);if(r!=none)atomicOr(graph[r].y,3u);}
        return;
    }
    if(stage==7u)
    {
        uint r=root(i,control.z);if(r==none)return;
        if((graph[r].y&1u)!=0u)wake(i,(graph[r].y&2u)!=0u);
        bodies[i].flags.z&=~96u;return;
    }
    if(stage==8u)
    {
        if(!dynamicBody(i))return;
        ResidentBody b=bodies[i];uint r=root(i,control.z);if(r==none)return;
        bool asleep=(b.flags.z&16u)!=0u;
        vec3 motion=b.velocity.xyz,correction=corrections[i].xyz;
        bool eligible=(b.flags.z&8u)==0u&&length(motion.xy)<=policy.x&&abs(motion.z)<=policy.y&&
            length(correction.xy)<=policy.x&&abs(correction.z)<=policy.y;
        if(!finite4(vec4(motion,0))||!finite4(vec4(correction,0))){fail();return;}
        float clock=asleep?policy.z:eligible?min(policy.z,b.velocity.w+policy.w):0;
        bodies[i].velocity.w=clock;
        atomicMin(graph[r].z,floatBitsToUint(clock));
        if(!asleep&&!eligible)atomicOr(graph[r].y,4u);
        return;
    }
    if(stage==9u)
    {
        ResidentBody b=bodies[i];if(b.flags.w==0u)return;
        if(dynamicBody(i))
        {
            uint r=root(i,control.z);if(r==none)return;
            if((graph[r].y&4u)==0u&&uintBitsToFloat(graph[r].z)>=policy.z)
            {b.flags.z|=16u;b.velocity.xyz=vec3(0);bodies[i]=b;}
            if((b.flags.z&16u)==0u)atomicAdd(awakeCount,1u);
        }
        else if(b.velocity.xyz!=vec3(0))atomicAdd(awakeCount,1u);
    }
}
