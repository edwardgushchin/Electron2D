#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer LibraryInput { uint words[]; };

uvec4 e2dDecodeShared(uint pixel);
uint e2dEncodeShared(uvec4 values);

uvec4 e2dDecode(uvec4 parts, uint widths, uint kind) {
    if (kind==8) return e2dDecodeShared(parts.x);
    uvec4 result = uvec4(0);
    for (uint lane=0; lane<4; lane++) {
        uint bits = (widths >> (lane*8)) & 255;
        if (bits == 0) { result[lane] = lane==3 ? (kind==4 || kind==5 ? 1 : floatBitsToUint(1.0)) : 0; continue; }
        uint p = parts[lane];
        int signedPart = int(p << (32-bits)) >> (32-bits);
        if (kind==4) { result[lane]=p; continue; }
        if (kind==5) { result[lane]=uint(signedPart); continue; }
        float value;
        if (kind==0 || kind==9) value = float(p) / float(bits==32 ? 0xffffffffu : (1u<<bits)-1);
        else if (kind==1) value = max(float(signedPart) / float(bits==32 ? 0x7fffffffu : (1u<<(bits-1))-1), -1.0);
        else if (kind==2) value = float(p);
        else if (kind==3) value = float(signedPart);
        else if (kind==6) value = bits==32 ? uintBitsToFloat(p) : unpackHalf2x16(p).x;
        else if (kind==7) value = unpackHalf2x16(p << (15-bits)).x;
        else value = 0;
        if (kind==9 && lane!=3) value = value<=0.04045 ? value/12.92 : pow((value+0.055)/1.055, 2.4);
        result[lane]=floatBitsToUint(value);
    }
    return result;
}

uint e2dPackUnsignedFloat(float value, uint bits) {
    uint mantissa = bits-5;
    if (isnan(value)) return (31u<<mantissa) | (1u<<(mantissa-1));
    if (value<=0) return 0;
    if (value>=65536.0) return 31u<<mantissa;
    int exponent;
    frexp(value, exponent);
    int unbiased = max(exponent-1, -14);
    uint fraction = uint(roundEven(ldexp(value, int(mantissa)-unbiased)));
    if (fraction >= (2u<<mantissa)) { unbiased++; fraction >>= 1; }
    if (unbiased>15) return 31u<<mantissa;
    if (unbiased==-14 && fraction<(1u<<mantissa)) return fraction;
    return (uint(unbiased+15)<<mantissa) | (fraction-(1u<<mantissa));
}

uvec4 e2dEncode(uvec4 values, uint widths, uint kind) {
    if (kind==8) return uvec4(e2dEncodeShared(values),0,0,0);
    uvec4 result = uvec4(0);
    for (uint lane=0; lane<4; lane++) {
        uint bits = (widths >> (lane*8)) & 255;
        if (bits==0) continue;
        uint mask = bits==32 ? 0xffffffffu : (1u<<bits)-1;
        if (kind==4) { result[lane]=min(values[lane],mask); continue; }
        if (kind==5) {
            if (bits==32) result[lane]=values[lane];
            else { int bound=int(1u<<(bits-1)); result[lane]=uint(clamp(int(values[lane]),-bound,bound-1)) & mask; }
            continue;
        }
        float value = uintBitsToFloat(values[lane]);
        if (kind==9 && lane!=3) value = value<=0.0031308 ? value*12.92 : 1.055*pow(value,1.0/2.4)-0.055;
        if (kind==0 || kind==9) result[lane] = uint(roundEven(clamp(value,0,1)*float(mask)));
        else if (kind==1) result[lane] = uint(int(roundEven(clamp(value,-1,1)*float(mask>>1)))) & mask;
        else if (kind==2) result[lane] = uint(roundEven(clamp(value,0,float(mask))));
        else if (kind==3) result[lane] = uint(int(roundEven(clamp(value,-float(mask>>1)-1,float(mask>>1))))) & mask;
        else if (kind==6) result[lane] = bits==32 ? values[lane] : packHalf2x16(vec2(value,0)) & 65535;
        else if (kind==7) result[lane] = e2dPackUnsignedFloat(value,bits);
    }
    return result;
}

uvec4 e2dDecodeShared(uint pixel) {
    float scale = exp2(float(pixel>>27)-24);
    return floatBitsToUint(vec4(vec3(pixel&511,(pixel>>9)&511,(pixel>>18)&511)*scale,1));
}

uint e2dEncodeShared(uvec4 values) {
    vec3 rgb = clamp(uintBitsToFloat(values.xyz),0,65408);
    int exponent;
    frexp(max(max(rgb.x,rgb.y),rgb.z),exponent);
    uint biasedExponent = uint(max(-16,exponent-1)+16);
    vec3 mantissa = floor(rgb*exp2(24-float(biasedExponent))+0.5);
    if (max(max(mantissa.x,mantissa.y),mantissa.z)>=512) {
        biasedExponent++; mantissa=floor(rgb*exp2(24-float(biasedExponent))+0.5);
    }
    uvec3 m=uvec3(mantissa);
    return (biasedExponent<<27) | (m.z<<18) | (m.y<<9) | m.x;
}

void main() {
    uvec4 inputValue=uvec4(words[0],words[1],words[2],words[3]);
    uvec4 decoded=e2dDecode(inputValue,words[4],words[5]);
    uvec4 encoded=e2dEncode(decoded,words[4],words[5]);
    words[0]=encoded.x ^ e2dEncodeShared(e2dDecodeShared(words[6]));
}
