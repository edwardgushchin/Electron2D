#version 450
layout(set=0,binding=0) uniform sampler2D source;
layout(set=0,binding=1,r32ui) uniform uimage2D firstCounter;
layout(set=0,binding=2,r32ui) uniform uimage2D secondCounter;
layout(set=0,binding=3,std430) buffer Results { uint result[]; };
layout(location=0) out vec4 pixel;
void main(){ivec2 p=ivec2(gl_FragCoord.xy);uint a=imageAtomicAdd(firstCounter,p,1u);uint b=imageAtomicAdd(secondCounter,p,2u);result[p.y*4+p.x]=a*10u+b;
pixel=vec4(textureLod(source,(vec2(p)+.5)/vec2(textureSize(source,0)),0).r,float(b),.5,1);}
