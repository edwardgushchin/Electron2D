#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=1) readonly buffer Order { uint order[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=1) buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=2) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=3) buffer Status { uvec2 status; };
layout(std430,set=1,binding=4) buffer SolverBodies { SolverBody solverBodies[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; vec4 correctionPolicy; };
void main()
{
    uint at=gl_GlobalInvocationID.x;if(at>=control.y||status.x!=0u)return;
    uint i=(history.w&4u)!=0u?history.y+at:order[history.y+at];
    Constraint c=constraints[i];ContactImpulse p=impulses[i];
    bool packed=(history.w&8u)!=0u;
    vec2 ma,mb,n=c.normal.xy,t=vec2(n.y,-n.x);vec3 av,bv,va0,vb0,ca,cb;
    if(packed)
    {
        SolverBody a=solverBodies[c.bodies.x],b=solverBodies[c.bodies.y];
        ma=vec2(a.velocity.w,a.correction.w);mb=vec2(b.velocity.w,b.correction.w);
        va0=a.velocity.xyz;vb0=b.velocity.xyz;av=va0+a.surface.xyz;bv=vb0+b.surface.xyz;ca=a.correction.xyz;cb=b.correction.xyz;
    }
    else
    {
        ResidentBody a=bodies[c.bodies.x],b=bodies[c.bodies.y];ma=inverseMass(a);mb=inverseMass(b);
        va0=a.velocity.xyz;vb0=b.velocity.xyz;av=va0+a.surface.xyz;bv=vb0+b.surface.xyz;ca=corrections[c.bodies.x].xyz;cb=corrections[c.bodies.y].xyz;
    }
    if(!responds(c,false))ma=vec2(0);if(!responds(c,true))mb=vec2(0);
    float vn=dot2(bv.xy-av.xy,n)+bv.z*c.normal.w-av.z*c.normal.z;
    float vt=dot2(bv.xy-av.xy,t)+bv.z*c.tangent.w-av.z*c.tangent.z;
    float pn=max(0,p.physical.x+c.parameters.x*(c.parameters.z-vn));
    float pt=clamp(p.physical.y-c.parameters.y*vt,-c.tangent.x*pn,c.tangent.x*pn);
    vec2 delta=vec2(pn,pt)-p.physical.xy;
    float speed=dot2(cb.xy-ca.xy,n)+cb.z*c.normal.w-ca.z*c.normal.z;
    float pc=max(0,p.correction.x+c.parameters.x*(c.parameters.w-speed)),dc=pc-p.correction.x;
    vec2 impulse=n*delta.x+t*delta.y;
    vec3 va=va0-vec3(ma.x*impulse,ma.y*(c.normal.z*delta.x+c.tangent.z*delta.y));
    vec3 vb=vb0+vec3(mb.x*impulse,mb.y*(c.normal.w*delta.x+c.tangent.w*delta.y));
    ca-=vec3(ma.x*n*dc,ma.y*c.normal.z*dc);cb+=vec3(mb.x*n*dc,mb.y*c.normal.w*dc);
    // These deltas are already applied; a later joint gather must not apply them again.
    p.physical=vec4(pn,pt,0,0);p.correction.xy=vec2(pc,0);
    if(!finite4(p.physical)||!finite4(p.correction)||!finite4(vec4(va,0))||!finite4(vec4(vb,0))||!finite4(vec4(ca,0))||!finite4(vec4(cb,0)))
    {atomicOr(status.x,1u);return;}
    if(ma.x>0){if(packed){solverBodies[c.bodies.x].velocity.xyz=va;solverBodies[c.bodies.x].correction.xyz=ca;}else{bodies[c.bodies.x].velocity.xyz=va;corrections[c.bodies.x].xyz=ca;}}
    if(mb.x>0){if(packed){solverBodies[c.bodies.y].velocity.xyz=vb;solverBodies[c.bodies.y].correction.xyz=cb;}else{bodies[c.bodies.y].velocity.xyz=vb;corrections[c.bodies.y].xyz=cb;}}
    impulses[i]=p;
}
