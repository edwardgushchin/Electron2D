#version 450
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform First { bvec4 quads[2]; float tail; uint number; };
layout(set = 3, binding = 1, std140) uniform Second { bvec3 triples[2]; bvec4 quad; bvec3 triple; uint afterTriple; bvec2 pairs[2]; bvec2 pair; bool switches[3]; bool enabled; };
float bits(bvec2 v) { return float(v.x) + 2 * float(v.y); }
float bits(bvec3 v) { return float(v.x) + 2 * float(v.y) + 4 * float(v.z); }
float bits(bvec4 v) { return float(v.x) + 2 * float(v.y) + 4 * float(v.z) + 8 * float(v.w); }
void main() {
    int group = int(gl_FragCoord.x) / 16;
    if (group == 0) outputColor = vec4(enabled ? .75 : .125, number / 255.0, tail, 1);
    else if (group == 1) outputColor = vec4(bits(pair) / 3, bits(triple) / 7, bits(quad) / 15, 1);
    else if (group == 2) outputColor = vec4(bits(bvec3(switches[0], switches[1], switches[2])) / 7, bits(pairs[0]) / 3, bits(pairs[1]) / 3, 1);
    else if (group == 3) outputColor = vec4(bits(triples[0]) / 7, bits(quads[0]) / 15, afterTriple / 255.0, 1);
    else outputColor = vec4(bits(triples[1]) / 7, bits(quads[1]) / 15, 0, 1);
}
