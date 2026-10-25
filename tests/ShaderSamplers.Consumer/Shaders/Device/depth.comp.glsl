#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0)uniform sampler2DShadow shadow;
layout(set=0,binding=1)uniform sampler2D image0;
layout(set=1,binding=0,std430)buffer Output{vec4 result[];};
void main(){result[0]=vec4(textureLod(shadow,vec3(.5,.5,.25),0));result[1]=textureGather(shadow,vec2(.5),.25);result[2]=texelFetch(image0,ivec2(0),0);result[3]=vec4(textureSize(shadow,0),textureQueryLevels(shadow),0);}
