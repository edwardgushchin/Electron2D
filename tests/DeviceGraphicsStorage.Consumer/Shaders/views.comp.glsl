#version 450
layout(local_size_x=4,local_size_y=4) in;
layout(set=0,binding=0) uniform sampler2D source;
layout(set=0,binding=1,r32ui) uniform uimage2D counter;
layout(set=0,binding=2,std430) buffer Results { uint result[]; };
layout(set=0,binding=3) uniform sampler2DArray layered;
void main(){ivec2 p=ivec2(gl_GlobalInvocationID.xy);uint i=uint(p.y*4+p.x)*4u;
result[i]=floatBitsToUint(textureLod(source,(vec2(p)+.5)/vec2(textureSize(source,0)),0).r);
result[i+1u]=floatBitsToUint(textureLod(layered,vec3((vec2(p)+.5)/vec2(textureSize(layered,1).xy),2),1).r);
result[i+2u]=imageAtomicAdd(counter,p,1u);
result[i+3u]=uint(textureSize(source,0).x)|(uint(textureSize(layered,1).x)<<8)|(uint(textureQueryLevels(source))<<16)|(uint(textureQueryLevels(layered))<<24);}
