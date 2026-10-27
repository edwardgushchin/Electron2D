#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2DArray pixels;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
const float points[16]=float[](-3,-1.25,-.875,-.625,-.375,-.125,0,.125,.375,.625,.875,1,1.125,1.625,2.25,3.25);
void main(){ uint i=gl_GlobalInvocationID.x; uint pointIndex=i%16; vec2 p=vec2(points[pointIndex],points[(pointIndex*5+3)%16]); result[i]=vec4(textureLod(pixels,vec3(p,1),float(i/16))); }
