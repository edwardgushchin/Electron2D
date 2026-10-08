#version 450
layout(local_size_x = 64) in;
layout(set = 3, binding = 7, std430) buffer Values { float values[]; };
layout(set = 2, binding = 4, std140) uniform Settings { vec4 settings; };
void main() { uint i = gl_GlobalInvocationID.x; if(i < uint(settings.y)) values[i] = values[i] * settings.x + settings.z; }
