#version 450
layout(set = 0, binding = 0, std140) uniform Settings { vec4 values; };
layout(location = 0) flat in vec2 vertexValues;
layout(location = 0) out vec4 color;

void main() { color = vec4(vertexValues, values.zw); }
