#version 450
layout(set=0,binding=0,std430) readonly buffer Values { uint value[]; };
layout(location=0) out vec4 color;
void main(){color=vec4(float(value[0])/10,float(value[1])/100,float(value[2])/100,1);}
