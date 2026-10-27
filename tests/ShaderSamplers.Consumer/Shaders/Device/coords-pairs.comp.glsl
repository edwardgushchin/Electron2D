#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform texture2D image0;
layout(set=0,binding=1) uniform sampler point0;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
const vec2 points[8]=vec2[](vec2(.5,.5),vec2(1.5,.5),vec2(.5,1.5),vec2(1.5,1.5),vec2(1,1),vec2(0,.5),vec2(-.5,.5),vec2(2.5,1.5));
vec4 fetch_pixel(texture2D image,sampler point0,vec2 point){return vec4(textureLod(sampler2D(image,point0),point,0));}
void main(){ uint i=gl_GlobalInvocationID.x; result[i]=fetch_pixel(image0,point0,points[i%8]); }
