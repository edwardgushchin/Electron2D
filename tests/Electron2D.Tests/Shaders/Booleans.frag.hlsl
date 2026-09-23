cbuffer Values : register(b0, space3) {
    bool enabled; uint number; bool switches[3]; bool2 pair; bool3 triple; uint afterTriple;
    bool4 quad; bool2 pairs[2]; bool3 triples[2]; bool4 quads[2]; float tail;
};
float bits(bool2 v) { return float(v.x) + 2 * float(v.y); }
float bits(bool3 v) { return float(v.x) + 2 * float(v.y) + 4 * float(v.z); }
float bits(bool4 v) { return float(v.x) + 2 * float(v.y) + 4 * float(v.z) + 8 * float(v.w); }
float4 main(float4 position : SV_Position) : SV_Target0 {
    int group = int(position.x) / 16;
    if (group == 0) return float4(enabled ? .75 : .125, number / 255.0, tail, 1);
    if (group == 1) return float4(bits(pair) / 3, bits(triple) / 7, bits(quad) / 15, 1);
    if (group == 2) return float4(bits(bool3(switches[0], switches[1], switches[2])) / 7, bits(pairs[0]) / 3, bits(pairs[1]) / 3, 1);
    if (group == 3) return float4(bits(triples[0]) / 7, bits(quads[0]) / 15, afterTriple / 255.0, 1);
    return float4(bits(triples[1]) / 7, bits(quads[1]) / 15, 0, 1);
}
