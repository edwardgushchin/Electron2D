#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2D pixels;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
void main(){result[0]=textureGather(pixels,vec2(1),0);result[1]=textureGrad(pixels,vec2(.5),vec2(1,0),vec2(0,1));result[2]=vec4(texelFetch(pixels,ivec2(1,0),0));result[3]=vec4(textureSize(pixels,0),textureQueryLevels(pixels),0);}
