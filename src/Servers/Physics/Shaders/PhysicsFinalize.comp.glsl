// SPDX-License-Identifier: MIT
#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsBody.inc.glsl"
layout(local_size_x=64) in;
struct Input { vec4 pose; vec4 geometry; vec4 sleep; uvec4 identity; };
struct Result { vec4 motion; vec4 frame; vec2 position; uint bodyFlags; uint simFlags; uvec4 identity; };
layout(std430,set=0,binding=0) readonly buffer Bodies { Body bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Inputs { Input inputs[]; };
layout(std430,set=1,binding=0) buffer Results { Result results[]; };
layout(std140,set=2,binding=0) uniform Settings { vec4 step; uvec4 control; };
// A Newton correction can still round to the wrong neighbor at a midpoint.
// Compare the residual against the exact adjacent-root midpoint square.
float finalSqrt(float value)
{
    precise float r=sqrtRefined(value);
    if(r==0.0 || isinf(r) || isnan(r)) return r;
    for(int attempt=0;attempt<2;attempt++)
    {
        uint bits=floatBitsToUint(r);
        precise float residual=fma(-r,r,value);
        if(residual==0.0) break;
        bool upper=residual>0.0;
        precise float neighbor=uintBitsToFloat(upper?bits+1u:bits-1u);
        precise float gap=upper?neighbor-r:r-neighbor;
        precise float boundary=0.25*gap*gap;
        precise float distance=fma(upper?-r:r,gap,residual);
        bool change=upper?distance>boundary:distance<boundary;
        if(!change && !(distance==boundary && (bits&1u)!=0u)) break;
        r=neighbor;
    }
    return r;
}
float finalReciprocal(float value)
{
    precise float r=divideRefined(1.0,value);
    precise float residual=fma(-r,value,1.0);
    if(residual==0.0) return r;
    uint bits=floatBitsToUint(r); bool upper=residual>0.0;
    precise float neighbor=uintBitsToFloat(upper?bits+1u:bits-1u);
    precise float gap=upper?neighbor-r:r-neighbor;
    precise float boundary=(0.5*gap)*value;
    precise float distance=abs(residual);
    return distance>boundary || (distance==boundary && (bits&1u)!=0u)?neighbor:r;
}
void main()
{
    uint i=gl_GlobalInvocationID.x; if(i>=control.x) return;
    Body b=bodies[i]; Input p=inputs[i];
    precise vec2 v=b.velocity.xy; precise float w=b.velocity.z; uint locks=uint(b.velocity.w);
    if((locks&1u)!=0u) v.x=0;
    if((locks&2u)!=0u) v.y=0;
    if((locks&4u)!=0u) w=0;
    precise vec2 center=p.pose.xy+b.delta.xy;
    precise vec2 q=vec2(b.delta.z*p.pose.z-b.delta.w*p.pose.w,b.delta.w*p.pose.z+b.delta.z*p.pose.w);
    precise float magnitude=finalSqrt(q.y*q.y+q.x*q.x);
    q*=magnitude>0.0?finalReciprocal(magnitude):0.0;
    precise float linearSpeed=finalSqrt(v.x*v.x+v.y*v.y);
    precise float speed=linearSpeed+abs(w)*p.geometry.w;
    precise float correctionLinear=0.5*step.y*finalSqrt(b.delta.x*b.delta.x+b.delta.y*b.delta.y);
    precise float correctionAngular=0.5*step.y*abs(b.delta.w);
    bool sleepy=(control.y&1u)!=0u && (p.identity.w&2u)!=0u && linearSpeed<p.sleep.y && abs(w)<p.sleep.z && correctionLinear<p.sleep.y && correctionAngular<p.sleep.z;
    precise float sleepTime=sleepy?min(3.402823466e38,p.sleep.x+step.x):0.0;
    bool fast=!sleepy && (p.identity.w&1u)!=0u && (control.y&2u)!=0u && speed*step.x>0.5*p.geometry.z;
    bool awake=!sleepy||sleepTime<=step.z;
    precise vec2 offset=vec2(q.x*p.geometry.x-q.y*p.geometry.y,q.y*p.geometry.x+q.x*p.geometry.y);
    precise vec2 position=center-offset;
    Result result;
    result.motion=vec4(v,w,sleepTime); result.frame=vec4(center,q); result.position=position;
    result.bodyFlags=(p.identity.z&~104u)|(uint(b.flags.x)&96u);
    result.simFlags=(uint(b.flags.x)&~104u)|(fast?8u:0u);
    result.identity=uvec4(p.identity.xy,(fast?1u:0u)|(awake?2u:(p.identity.w&4u)),0u);
    results[i]=result;
}
