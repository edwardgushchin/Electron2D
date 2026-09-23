#version 450
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform First {
    uvec4 quads[2];
    uvec2 pairs[2];
    uvec4 quad;
    uvec2 pair;
};
layout(set = 3, binding = 1, std140) uniform Second {
    vec4 rectangles[2];
    vec3 rgbArray[2];
    vec4 rectangle;
    float tail;
    vec3 rgb;
};
void main() {
    bool valid = tail == 0.75 && all(equal(rectangle, vec4(0.125, 0.25, 0.5, 1)))
        && all(equal(rectangles[0], vec4(1, 2, 3, 4))) && all(equal(rectangles[1], vec4(-1, -2, -3, -4)))
        && all(equal(pair, uvec2(0x80000000u, 0xffffffffu)))
        && all(equal(quad, uvec4(0xffffffffu, 0x80000000u, 0x7fffffffu, 123456789u)))
        && all(equal(pairs[0], pair)) && all(equal(pairs[1], uvec2(0, 1)))
        && all(equal(quads[0], quad)) && all(equal(quads[1], uvec4(1, 2, 3, 4)));
    outputColor = valid ? vec4(rgb * rgbArray[0] + rgbArray[1], 1) : vec4(1, 0, 1, 1);
}
