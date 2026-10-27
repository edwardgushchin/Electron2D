#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
#extension GL_EXT_shader_16bit_storage:require
layout(location=0)out f16vec2 color;
void main(){gl_Position=vec4(0,0,0,1);color=f16vec2(1,0);}
