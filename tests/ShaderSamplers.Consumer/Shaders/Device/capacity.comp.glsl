#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0)uniform sampler2D image0;
layout(set=1,binding=0,std140)uniform A{vec4 a;};
layout(set=1,binding=1,std140)uniform B{vec4 b;};
layout(set=1,binding=2,std140)uniform C{vec4 c;};
layout(set=1,binding=3,std140)uniform D{vec4 d;};
layout(set=2,binding=0,std430)buffer Output{vec4 result;};
void main(){result=textureLod(image0,vec2(.5),0)*(a+b+c+d);}
