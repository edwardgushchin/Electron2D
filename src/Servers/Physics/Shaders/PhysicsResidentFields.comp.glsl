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
bool pointComponent(float value,int exponent,out precise float result)
{
    result=0;if(value==0)return true;
    int adjustment;precise float mantissa=frexp(value,adjustment);exponent+=adjustment;
    // Reject the range explicitly; an infinity sentinel must not enter field arithmetic.
    if(exponent>128)return false;
    result=ldexp(mantissa,exponent);return finiteField(vec4(result,0,0,0));
}
// Keep division in the normal mantissa range even for extreme authored coordinates.
float scaledComponent(float value,float mantissa,int exponent)
{
    if(value==0)return 0;
    int valueExponent;precise float valueMantissa=frexp(value,valueExponent);
    return ldexp(valueMantissa/mantissa,valueExponent-exponent);
}
vec2 scaledVector(vec2 value,float mantissa,int exponent)
{
    return vec2(scaledComponent(value.x,mantissa,exponent),scaledComponent(value.y,mantissa,exponent));
}
bool gravity(Field f,vec4 pose,vec2 position,out precise vec2 result)
{
    result=vec2(0);
    if(f.policy.y==0u){result=f.gravity.xy*f.gravity.z;return finiteField(vec4(result,0,0));}
    if(f.gravity.z==0)return true;
    // Preserve the guarded affine operation order through shader optimization.
    precise vec2 toward=pose.xy+rotate(pose.zw,f.gravity.xy)-position;
    float scale;int scaleExponent;float scaleMantissa;precise vec2 scaled;
    if(!finiteField(vec4(toward,0,0)))
    {
        scale=max(max(max(abs(pose.x),abs(pose.y)),max(abs(f.gravity.x),abs(f.gravity.y))),max(abs(position.x),abs(position.y)));
        scaleMantissa=frexp(scale,scaleExponent);
        scaled=scaledVector(pose.xy,scaleMantissa,scaleExponent)+rotate(pose.zw,scaledVector(f.gravity.xy,scaleMantissa,scaleExponent))-scaledVector(position,scaleMantissa,scaleExponent);
    }
    else
    {
        scale=max(abs(toward.x),abs(toward.y));if(scale==0)return true;
        scaleMantissa=frexp(scale,scaleExponent);scaled=scaledVector(toward,scaleMantissa,scaleExponent);
    }
    precise float distance=length(scaled);if(distance==0)return true;
    precise vec2 direction=scaled/distance;
    if(f.gravity.w<=0){result=direction*f.gravity.z;return finiteField(vec4(result,0,0));}
    int gravityExponent,unitExponent;
    float gravityMantissa=frexp(f.gravity.z,gravityExponent);
    float unitMantissa=frexp(f.gravity.w,unitExponent);
    precise float ratio=unitMantissa/(scaleMantissa*distance);
    precise float strength=gravityMantissa*ratio*ratio;
    int exponent=gravityExponent+2*(unitExponent-scaleExponent);
    bool x=pointComponent(direction.x*strength,exponent,result.x);
    bool y=pointComponent(direction.y*strength,exponent,result.y);
    return x&&y;
}
void linkArea(uint at,Shape a,Shape b)
{
    uint rank=lookup[a.owner.x];
    if(rank==none||!receivesBodyFields(bodies[b.owner.x])||(a.policy.z&b.policy.y)==0u)return;
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
    precise vec4 total=vec4(0);bool gravityValid=true;bvec3 done=bvec3(false);
    uint minimum=0u;
    if(receivesBodyFields(body))
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
                vec2 value;bool valid=gravity(f,pose,body.pose.xy,value);
                if(g<=2u){total.xy+=value;gravityValid=gravityValid&&valid;}else{total.xy=value;gravityValid=valid;}
                done.x=g==2u||g==3u;
            }
            if(!done.y&&l!=0u){if(l<=2u)total.z+=f.damping.x;else total.z=f.damping.x;done.y=l==2u||l==3u;}
            if(!done.z&&a!=0u){if(a<=2u)total.w+=f.damping.y;else total.w=f.damping.y;done.z=a==2u||a==3u;}
            if(all(done))break;
            minimum=rank+1u;
        }
        if(!done.x){vec2 value;bool valid=gravity(world,vec4(0,0,1,0),body.pose.xy,value);total.xy+=value;gravityValid=gravityValid&&valid;}
        if(!done.y)total.z+=world.damping.x;
        if(!done.z)total.w+=world.damping.y;
    }
    vec4 result=resolved[i];
    if(!gravityValid||!resolveBodyFields(body,result,total,delta)){fail();return;}
    resolved[i]=result;bodies[i]=body;
}
