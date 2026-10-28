#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_shader_8bit_storage : require
#extension GL_EXT_shader_16bit_storage : require
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int16 : require
#extension GL_EXT_shader_explicit_arithmetic_types_float16 : require
layout(local_size_x=1) in;
layout(buffer_reference) buffer Node;
layout(buffer_reference,std430,buffer_reference_align=8) buffer Node { Node next; uint8_t tiny; int16_t narrow; float16_t halfValue; };
layout(push_constant,std430) uniform Root { Node head; } pc;
void main() { pc.head.tiny = pc.head.next.tiny + uint8_t(2); pc.head.narrow = pc.head.next.narrow - int16_t(2); pc.head.halfValue = pc.head.next.halfValue + float16_t(.5); pc.head.next = pc.head.next.next; }
