#version 450
layout(constant_id = 10) const float displacement = 0.0;
void main() {
    vec2 positions[3] = vec2[](vec2(-1,-1), vec2(3,-1), vec2(-1,3));
    gl_Position = vec4(positions[gl_VertexIndex] + vec2(displacement,0), 0, 1);
}
