#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2DShadow pixels;
layout(set=1,binding=0,std430) buffer Output { vec4 result[]; };
void main(){ result[0]=vec4(textureLod(pixels,vec3(-1,.5,.5),0),textureLod(pixels,vec3(.5,2,.5),0),textureLod(pixels,vec3(.5,.5,.5),0),1); }
