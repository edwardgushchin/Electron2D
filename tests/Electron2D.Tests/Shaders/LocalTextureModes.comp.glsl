#version 450
layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;
layout(set = 3, binding = 7) uniform sampler2DArray sourceImage;
layout(set = 2, binding = 1, std140) uniform Sample { vec4 position; } sampleParameters;
layout(set = 9, binding = 4, std430) buffer Output { vec4 value; } outputData;
void main() { outputData.value = textureLod(sourceImage, sampleParameters.position.xyz, sampleParameters.position.w); }
