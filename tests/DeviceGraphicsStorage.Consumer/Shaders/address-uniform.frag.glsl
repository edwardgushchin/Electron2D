#version 450
layout(set=0,binding=0,std140) uniform Input { uvec4 value; } inputData;
layout(location=0) out vec4 color;
void main() { color = vec4(inputData.value.xy, 0, 1); }
