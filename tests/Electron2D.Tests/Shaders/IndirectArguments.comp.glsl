#version 450
layout(local_size_x = 1) in;
layout(set = 0, binding = 0, std430) buffer Values { float values[]; };
layout(set = 0, binding = 1, std430) buffer Arguments { uint arguments[]; };
void main() {
    arguments[4] = (uint(values[0]) + 63u) / 64u;
    arguments[5] = 1u;
    arguments[6] = 1u;
}
