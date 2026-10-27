#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
#extension GL_EXT_shader_16bit_storage:require
layout(constant_id=0)const float16_t factor=float16_t(0.25);
layout(push_constant,std430)uniform Push { float16_t a; float16_t b; } pc;
layout(location=0)in vec2 color;layout(location=0)out vec4 outputColor;
void main(){outputColor=vec4(float((float16_t(color.x)+pc.a)*factor),float((float16_t(color.y)+pc.b)*factor),float(float16_t(0.125)),1);}
