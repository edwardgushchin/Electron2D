#version 450
layout(location = 0) in vec4 color;
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform Material {
    vec4 tint;
    float gain;
    int mode;
    vec2 offset;
};
layout(set = 3, binding = 1, std140) uniform Extra {
    float weights[2];
    ivec2 shift;
    uint enabled;
};
void main() {
    vec4 result = tint * color;
    result.rgb *= gain * (weights[0] + weights[1]);
    if (mode == 1) result = result.bgra;
    if (enabled == 0 || gl_FragCoord.x < offset.x + shift.x || gl_FragCoord.y < offset.y + shift.y) result = vec4(0, 0, 0, 1);
    outputColor = result;
}
