#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform usampler2D pixels;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
const vec2 points[8]=vec2[](vec2(-1,.5),vec2(2,.5),vec2(.5,-1),vec2(.5,2),vec2(.5,.5),vec2(0,.5),vec2(-1,-1),vec2(.5,0));
void main(){ uint i=gl_GlobalInvocationID.x; result[i]=vec4(textureLod(pixels,points[i%8],float(i/8))); }
