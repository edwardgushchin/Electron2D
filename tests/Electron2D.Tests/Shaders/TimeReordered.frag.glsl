#version 450
layout(location = 0) in vec4 color;
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform Material {
    float gain;
    float time;
};
layout(set = 3, binding = 1, std140) uniform Frame {
    float TIME;
    vec4 tint;
};
void main() {
    outputColor = vec4(fract(TIME * 4), gain, time, 1) * tint * color;
}
