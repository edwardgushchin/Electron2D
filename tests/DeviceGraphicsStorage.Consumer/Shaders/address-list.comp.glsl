#version 450
#extension GL_EXT_buffer_reference : require
layout(local_size_x=1) in;
layout(buffer_reference) buffer Node;
layout(buffer_reference,std430,buffer_reference_align=8) buffer Node { Node next; uint value; };
layout(push_constant,std430) uniform Root { Node head; } pc;
void main() { Node cursor = pc.head; uint sum = 0; for (uint i=0; i<9; ++i) { sum += cursor.value; cursor = cursor.next; } pc.head.value = sum; pc.head.next = pc.head.next.next; }
