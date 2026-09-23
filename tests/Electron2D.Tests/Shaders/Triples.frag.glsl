#version 450
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform Triples {
    vec3 numeric;
    ivec3 signedTriple;
    uvec3 unsignedTriple;
    vec3 numericArray[2];
    ivec3 signedArray[2];
    uvec3 unsignedArray[2];
};
void main() {
    bool valid = all(equal(signedTriple, ivec3(-1, 2, -3)))
        && all(equal(unsignedTriple, uvec3(0xffffffffu, 2u, 3u)))
        && all(equal(signedArray[0], signedTriple))
        && all(equal(signedArray[1], ivec3(4, 5, 6)))
        && all(equal(unsignedArray[0], unsignedTriple))
        && all(equal(unsignedArray[1], uvec3(4, 5, 6)))
        && all(equal(numericArray[0], vec3(1, 0, 0)))
        && all(equal(numericArray[1], vec3(0, 1, 0)));
    outputColor = valid ? vec4(numeric, 1) : vec4(1, 0, 1, 1);
}
