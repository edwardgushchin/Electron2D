#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
layout(local_size_x=64) in;
struct Snapshot { vec4 pose; vec4 velocity; vec4 fields; float clock; uint flags; uvec2 padding; };
struct Change { uvec4 identity; Snapshot state; };
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Fields { vec4 fields[]; };
layout(std430,set=1,binding=0) buffer History { Change history[]; };
layout(std430,set=1,binding=1) buffer Changes { Change changes[]; };
layout(std430,set=1,binding=2) buffer Status { uint count; uint error; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 settings; };
bool finite4(vec4 v) { return !any(isnan(v)) && !any(isinf(v)); }
void main()
{
    uint i=gl_GlobalInvocationID.x;
    if(i>=settings.x)return;
    ResidentBody b=bodies[i];
    Change previous=Change(uvec4(0),Snapshot(vec4(0),vec4(0),vec4(0),0,0,uvec2(0)));
    if(i<settings.y)previous=history[i];
    bool alive=b.flags.w!=0u;
    Change current=Change(uvec4(i,b.flags.x,previous.identity.w!=0u?previous.identity.y:0u,alive?1u:0u),
        Snapshot(vec4(0),vec4(0),vec4(0),0,0,uvec2(0)));
    if(alive)
    {
        current.state=Snapshot(b.pose,vec4(b.velocity.xyz+b.surface.xyz,0),fields[i],b.velocity.w,b.flags.z,uvec2(b.flags.y,0));
        if(!finite4(current.state.pose)||!finite4(current.state.velocity)||!finite4(current.state.fields)) {atomicOr(error,1u);return;}
    }
    bool changed=alive!=(previous.identity.w!=0u);
    if(alive)
    {
        // Public snapshot policy bits: rotation lock, sleep eligibility/state, CCD, omission and initialized fields.
        const uint policy=4u|8u|16u|768u|1024u|8192u;
        changed=changed||current.identity.y!=previous.identity.y||current.state.pose!=previous.state.pose||
            current.state.velocity!=previous.state.velocity||current.state.fields!=previous.state.fields||
            ((current.state.flags^previous.state.flags)&policy)!=0u||current.state.padding.x!=previous.state.padding.x;
    }
    history[i]=current;
    if(changed)changes[atomicAdd(count,1u)]=current;
}
