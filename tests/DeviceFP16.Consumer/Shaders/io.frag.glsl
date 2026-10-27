#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
#extension GL_EXT_shader_16bit_storage:require
layout(location=0)in f16vec2 color;layout(location=0)out vec4 outputColor;
void main(){outputColor=vec4(vec2(color),0,1);}
