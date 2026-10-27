#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2DArray pixels;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
void main(){ result[0]=texelFetch(pixels,ivec3(0),0);result[1]=texelFetch(pixels,ivec3(1,0,0),0);result[2]=texelFetch(pixels,ivec3(0),1);result[3]=vec4(textureSize(pixels,1),textureQueryLevels(pixels)); }
