#version 450
#extension GL_EXT_multiview : require
layout(constant_id=7)const float scale=1;
layout(push_constant)uniform Push{vec4 value;};
layout(set=1,binding=0)uniform sampler2D fragmentImage;
layout(location=0)in vec4 tint;
layout(location=1)out vec4 result;
void main(){result=tint*texture(fragmentImage,vec2(.5))*value*scale+vec4(0,0,float(gl_ViewIndex)/10,0);}
