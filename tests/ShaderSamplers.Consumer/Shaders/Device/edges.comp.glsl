#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0)uniform sampler2D image0;
layout(set=1,binding=0,std430)buffer Output{vec4 result[];};
void main(){result[0]=textureLod(image0,vec2(.5),0);result[1]=textureLod(image0,vec2(.5),1);result[2]=texelFetch(image0,ivec2(0),0);result[3]=vec4(textureSize(image0,0),textureQueryLevels(image0),0);}
