#version 450
void main() {
    gl_Position = vec4(gl_VertexIndex == 0 ? -.75 : .75, 0, .25, 1);
}
