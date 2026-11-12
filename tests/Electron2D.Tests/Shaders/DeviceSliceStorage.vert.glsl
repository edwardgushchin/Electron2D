#version 450
layout(set=0,binding=0,r32ui) uniform readonly uimage2D inputImage;
layout(location=0) flat out uint width;
void main() {
    vec2 p = vec2(float((gl_VertexIndex << 1) & 2), float(gl_VertexIndex & 2));
    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
    width = uint(imageSize(inputImage).x);
}
