#version 450
layout(local_size_x=64) in;
layout(set=0,binding=0,std430) readonly buffer Current { float current[]; };
layout(set=0,binding=1,std430) readonly buffer Previous { float previous[]; };
layout(set=0,binding=2,std430) readonly buffer Commands { uint commands[]; };
layout(set=0,binding=3,std430) readonly buffer LODs { uvec4 lods[]; };
layout(set=1,binding=0,std430) writeonly buffer Instances { vec4 instances[]; };
layout(set=1,binding=1,std430) writeonly buffer Draw { uint draw[]; };
#ifdef E2D_MODEL_MATRICES
layout(set=1,binding=2,std430) writeonly buffer Models { vec4 models[]; };
#endif
layout(set=2,binding=0,std140) uniform Parameters {
    vec4 canvas;
    vec4 origin;
    vec4 modulation;
    uvec4 layoutData; // capacity, stride, flags, visible
    uvec4 geometry; // surface, base vertex, index offset, vertex count
    uvec4 levels; // offset, count, relaxed device, unused
    vec4 physics; // fraction, quality, dispatch row width, unused
};
bool finiteValue(float value) { return !isnan(value) && !isinf(value); }
vec4 readCurrent(uint o) { return vec4(current[o],current[o+1],current[o+2],current[o+3]); }
vec4 readPrevious(uint o) { return vec4(previous[o],previous[o+1],previous[o+2],previous[o+3]); }
vec4 blendValue(vec4 a,vec4 b,float f) { return (1-f)*a+f*b; }
bool decomposition(vec4 a,vec4 b,out vec4 d) {
    vec2 x=vec2(a.x,b.x),y=vec2(a.y,b.y);
    float determinant=x.x*y.y-x.y*y.x;
    vec2 scale=vec2(length(x),sign(determinant)*length(y));
    float skew=acos(dot(normalize(x),sign(determinant)*normalize(y)))-1.5707963267948966;
    d=vec4(atan(x.y,x.x),scale,skew);
    return determinant!=0 && finiteValue(determinant) && finiteValue(scale.x) && finiteValue(scale.y) && finiteValue(skew);
}
float remainder(float v) { return v-trunc(v/6.283185307179586)*6.283185307179586; }
float angle(float a,float b,float f) { float delta=remainder(b-a); return a+(remainder(2*delta)-delta)*f; }
vec3 linearColor(vec3 v) { return mix(pow((v+0.055)/1.055,vec3(2.4)),v/12.92,lessThanEqual(v,vec3(0.04045))); }
void main() {
    uint i=gl_GlobalInvocationID.x+gl_GlobalInvocationID.y*uint(physics.z);
    uint flags=layoutData.z;
    uint first=geometry.x*5;
    uint count=(flags&16)!=0 ? commands[first] : geometry.w;
    uint n=(flags&16)!=0 ? commands[first+1] : layoutData.w;
    uint start=(flags&16)!=0 ? commands[first+2] : 0;
    uint offset=(flags&16)!=0 ? commands[first+3] : 0;
    uint instance=(flags&32)!=0 ? ((flags&16)!=0 ? commands[first+4] : 0) : offset;
    n=min(n,layoutData.x-min(instance,layoutData.x));
    if(i==0 && levels.y==0) {
        draw[0]=count; draw[1]=n;
        draw[2]=start+((flags&32)!=0 ? geometry.z : geometry.y);
        draw[3]=(flags&32)!=0 ? offset+geometry.y : 0;
        draw[4]=0;
    }
    if(i>=layoutData.x) return;
    if(levels.y!=0) {
        draw[i*5]=count; draw[i*5+1]=i<n ? 1 : 0;
        draw[i*5+2]=start+geometry.z; draw[i*5+3]=offset+geometry.y; draw[i*5+4]=levels.z!=0 ? 0 : i;
    }
    if(i>=n) return;
    uint o=(i+instance)*layoutData.y;
    vec4 a=readCurrent(o),b=readCurrent(o+4);
    float f=physics.x;
    if(f<1) {
        vec4 pa=readPrevious(o),pb=readPrevious(o+4);
        bool same=all(equal(pa.xyw,a.xyw)) && all(equal(pb.xyw,b.xyw));
        vec4 p,c;
        bool high=f>0 && !same &&physics.y>0 && decomposition(pa,pb,p) && decomposition(a,b,c) && sign(p.z)==sign(c.z);
        if(!same) { a=f==0 ? pa : blendValue(pa,a,f); b=f==0 ? pb : blendValue(pb,b,f); }
        if(high) {
            float rotation=angle(p.x,c.x,f),skew=angle(p.w,c.w,f);
            vec2 scale=(1-f)*p.yz+f*c.yz;
            a.xy=vec2(cos(rotation)*scale.x,-sin(rotation+skew)*scale.y);
            b.xy=vec2(sin(rotation)*scale.x,cos(rotation+skew)*scale.y);
        }
    }
#ifdef E2D_MODEL_MATRICES
    models[i*2]=vec4(a.x,b.x,a.y,b.y); models[i*2+1]=vec4(a.w,b.w,0,0);
#endif
    vec2 x=canvas.xy*a.x+canvas.zw*b.x,y=canvas.xy*a.y+canvas.zw*b.y;
    vec2 translation=canvas.xy*a.w+canvas.zw*b.w+origin.xy;
    if(levels.y!=0 && count==geometry.w && start==0 && offset==0) {
        // Normalize before squaring so large finite transforms keep their error bound.
        float m=max(max(abs(x.x),abs(x.y)),max(abs(y.x),abs(y.y)));
        vec2 nx=m==0 ? vec2(0) : x/m,ny=m==0 ? vec2(0) : y/m;
        float xx=dot(nx,nx),yy=dot(ny,ny),xy=dot(nx,ny);
        float scale=m*sqrt((xx+yy+length(vec2(xx-yy,2*xy)))*0.5);
        for(uint l=0;l<levels.y;l++) {
            uvec4 lod=lods[levels.x+l];
            if(uintBitsToFloat(lod.x)*scale>1) break;
            draw[i*5]=lod.z; draw[i*5+2]=geometry.z+lod.y;
        }
    }
    vec4 tint=vec4(1),custom=vec4(0);
    if((flags&1)!=0) { tint=readCurrent(o+8); if(f<1) tint=blendValue(readPrevious(o+8),tint,f); }
    if((flags&2)!=0) { uint c=o+8+((flags&1)!=0 ? 4 : 0); custom=readCurrent(c); if(f<1) custom=blendValue(readPrevious(c),custom,f); }
    if((flags&4)!=0) tint.rgb=linearColor(tint.rgb);
    tint*=modulation;
    if((flags&8)!=0) tint.rgb*=tint.a;
    instances[i*4]=vec4(x,y); instances[i*4+1]=vec4(translation,float((i+instance)&65535u),float((i+instance)>>16)+1); instances[i*4+2]=tint; instances[i*4+3]=custom;
}
