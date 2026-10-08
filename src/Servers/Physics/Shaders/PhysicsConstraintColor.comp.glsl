// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x=64) in;
struct Change { ivec4 identity; ivec4 state; };
layout(std430,set=0,binding=0) readonly buffer Changes { Change changes[]; };
layout(std430,set=1,binding=0) buffer Masks { uint masks[]; };
layout(std430,set=1,binding=1) buffer Results { int results[]; };
layout(std140,set=2,binding=0) uniform Settings { ivec4 sizes; ivec4 limits; };
void main()
{
    int index=int(gl_GlobalInvocationID.x), base=sizes.z*limits.x;
    bool invalid=limits.x<1 || limits.x>31 || limits.y<1 || limits.y>limits.x;
    if(sizes.w==0)
    {
        if(index>=sizes.y || invalid) return;
        uint value=0u, bit=1u<<uint(index%32);
        for(int color=0;color<limits.x;color++)
            if((masks[color*sizes.z+index/32]&bit)!=0u) value|=1u<<uint(color);
        masks[base+index]=value; return;
    }
    if(index!=0) return;
    results[sizes.x]=invalid?1:0;
    if(invalid) return;
    // ponytail: ordered greedy assignment is serial; parallel dependency waves are the next scaling step.
    for(int i=0;i<sizes.x;i++)
    {
        Change op=changes[i]; int a=op.identity.z,b=op.identity.w,dynamic=op.state.x,color=op.state.y;
        if(a<0 || a>=sizes.y || b<0 || b>=sizes.y || dynamic<0 || dynamic>3 || color< -1 || color>limits.x)
        { results[sizes.x]=1; return; }
        bool add=color<0;
        uint usedA=masks[base+a], usedB=masks[base+b];
        if(add)
        {
            uint allowed=dynamic==3?(1u<<uint(limits.y))-1u:dynamic==0?0u:((1u<<uint(limits.x))-1u)&~1u;
            uint occupied=((dynamic&1)!=0?usedA:0u)|((dynamic&2)!=0?usedB:0u);
            uint free=allowed&~occupied;
            color=free==0u?limits.x:dynamic==3?findLSB(free):findMSB(free);
        }
        if(color!=limits.x)
        {
            uint bit=1u<<uint(color);
            if((dynamic&1)!=0) masks[base+a]=add?usedA|bit:usedA&~bit;
            if((dynamic&2)!=0) masks[base+b]=add?usedB|bit:usedB&~bit;
        }
        results[i]=color;
    }
}
