#version 450
layout(set = 0, binding = 0, std140) uniform Settings { vec4 values; };
layout(location = 0) flat out vec2 vertexValues;

void main() {
    vec2 p = vec2(float((gl_VertexIndex << 1) & 2), float(gl_VertexIndex & 2));
    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
    vertexValues = values.xy;
}
