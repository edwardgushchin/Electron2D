#version 450
layout(local_size_x_id = 5, local_size_y = 1, local_size_z = 1) in;
layout(constant_id = 1) const bool enabled = true;
layout(constant_id = 2) const int bias = -2;
layout(constant_id = 3) const uint count = 2;
layout(constant_id = 4) const float multiplier = 0.5;
layout(set = 0, binding = 0, std430) buffer Output { float values[]; } result;
void main() {
    uint i = gl_GlobalInvocationID.x;
    result.values[i] = enabled ? float((bias * -7 + 4) ^ int(count << 1)) * multiplier + float(i) : -1.0;
}
