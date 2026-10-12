#version 450
layout(local_size_x = 1) in;
layout(constant_id = 0) const uint count = 2;
layout(set = 0, binding = 0, std140) uniform Input { float values[count]; } inputData;
layout(push_constant, std430) uniform Push { float values[count]; } pushData;
layout(set = 0, binding = 1, std430) buffer Output { float value; } result;
void main() { result.value = inputData.values[count - 1] + pushData.values[count - 1]; }
