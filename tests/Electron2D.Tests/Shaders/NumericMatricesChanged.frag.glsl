#version 450
layout(location=0) out vec4 result;
layout(set=3,binding=0,std140) uniform Values { mat4 m3; mat4 m4; mat3 a3[1]; mat4 a4[2]; float tail; };
void main() { result=vec4(m3[0].xyz+m4[0].xyz+a3[0][0]+a4[0][0].xyz,tail); }
