#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int8:require
#extension GL_EXT_shader_8bit_storage:require
layout(constant_id=0)const int8_t factor=int8_t(2);
layout(push_constant,std430)uniform Push { int8_t a; int8_t b; } pc;
layout(location=0)in vec2 color;layout(location=0)out vec4 outputColor;
void main(){outputColor=vec4(float((int8_t(color.x)+pc.a)*factor)/64,float((int8_t(color.y)+pc.b)*factor)/64,.125,1);}
