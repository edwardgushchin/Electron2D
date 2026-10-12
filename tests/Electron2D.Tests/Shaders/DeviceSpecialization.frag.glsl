#version 450
layout(constant_id = 1) const bool red = true;
layout(constant_id = 2) const int bias = -1;
layout(constant_id = 3) const uint gain = 2;
layout(constant_id = 4) const float scale = 0.5;
layout(constant_id = 11) const uint count = 2;
layout(set = 0, binding = 0, std140) uniform Input { float values[count]; } inputData;
layout(push_constant, std430) uniform Push { float values[count]; } pushData;
layout(location = 0) out vec4 color;
void main() { float value = float((bias + 3) * int(gain)) * scale * inputData.values[count - 1] * pushData.values[count - 1]; color = red ? vec4(value,0,0,1) : vec4(0,0,value,1); }
