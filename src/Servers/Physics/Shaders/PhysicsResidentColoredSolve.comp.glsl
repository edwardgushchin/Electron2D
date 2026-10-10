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
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; vec4 correctionPolicy; };
void main()
{
    uint at=gl_GlobalInvocationID.x;if(at>=control.y||status.x!=0u)return;
    uint i=order[history.y+at];
    Constraint c=constraints[i];ContactImpulse p=impulses[i];
    ResidentBody a=bodies[c.bodies.x],b=bodies[c.bodies.y];
    vec2 ma=inverseMass(a),mb=inverseMass(b),n=c.normal.xy,t=vec2(n.y,-n.x);
    vec3 av=a.velocity.xyz+a.surface.xyz,bv=b.velocity.xyz+b.surface.xyz;
    float vn=dot2(bv.xy-av.xy,n)+bv.z*c.normal.w-av.z*c.normal.z;
    float vt=dot2(bv.xy-av.xy,t)+bv.z*c.tangent.w-av.z*c.tangent.z;
    float pn=max(0,p.physical.x+c.parameters.x*(c.parameters.z-vn));
    float pt=clamp(p.physical.y-c.parameters.y*vt,-c.tangent.x*pn,c.tangent.x*pn);
    vec2 delta=vec2(pn,pt)-p.physical.xy;
    vec3 ca=corrections[c.bodies.x].xyz,cb=corrections[c.bodies.y].xyz;
    float speed=dot2(cb.xy-ca.xy,n)+cb.z*c.normal.w-ca.z*c.normal.z;
    float pc=max(0,p.correction.x+c.parameters.x*(c.parameters.w-speed)),dc=pc-p.correction.x;
    vec2 impulse=n*delta.x+t*delta.y;
    vec3 va=a.velocity.xyz-vec3(ma.x*impulse,ma.y*(c.normal.z*delta.x+c.tangent.z*delta.y));
    vec3 vb=b.velocity.xyz+vec3(mb.x*impulse,mb.y*(c.normal.w*delta.x+c.tangent.w*delta.y));
    ca-=vec3(ma.x*n*dc,ma.y*c.normal.z*dc);cb+=vec3(mb.x*n*dc,mb.y*c.normal.w*dc);
    // These deltas are already applied; a later joint gather must not apply them again.
    p.physical=vec4(pn,pt,0,0);p.correction.xy=vec2(pc,0);
    if(!finite4(p.physical)||!finite4(p.correction)||!finite4(vec4(va,0))||!finite4(vec4(vb,0))||!finite4(vec4(ca,0))||!finite4(vec4(cb,0)))
    {atomicOr(status.x,1u);return;}
    if(ma.x>0){bodies[c.bodies.x].velocity.xyz=va;corrections[c.bodies.x].xyz=ca;}
    if(mb.x>0){bodies[c.bodies.y].velocity.xyz=vb;corrections[c.bodies.y].xyz=cb;}
    impulses[i]=p;
}
