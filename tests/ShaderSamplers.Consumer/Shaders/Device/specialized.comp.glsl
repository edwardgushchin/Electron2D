#version 450
layout(local_size_x=1)in;
layout(constant_id=7)const float scale=1;
layout(push_constant)uniform Push{vec4 tint;};
layout(set=0,binding=0)uniform sampler2D image0;
layout(set=1,binding=0,std430)buffer Output{vec4 result;};
void main(){result=textureLod(image0,vec2(.5),0)*tint*scale;}
