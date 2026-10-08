// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi
// SPDX-License-Identifier: MIT
float dot2(vec2 a, vec2 b) { precise float r = a.x*b.x + a.y*b.y; return r; }
vec2 rotate(vec2 q, vec2 v) { precise vec2 r = vec2(q.x*v.x-q.y*v.y, q.y*v.x+q.x*v.y); return r; }
vec2 inverseRotate(vec2 q, vec2 v) { precise vec2 r = vec2(q.x*v.x+q.y*v.y, -q.y*v.x+q.x*v.y); return r; }
vec2 lerp2(vec2 a, vec2 b, float t) { precise vec2 r = (1-t)*a + t*b; return r; }
vec2 addScaled(vec2 a, float s, vec2 b) { precise vec2 r = a + s*b; return r; }
vec2 unit(vec2 v) { float d = sqrt(dot2(v,v)); return d < epsilon ? vec2(0) : (1/d)*v; }
vec4 relativePose(vec4 a, vec4 b)
{
    precise vec2 q = vec2(a.z*b.z+a.w*b.w, a.z*b.w-a.w*b.z);
    return vec4(inverseRotate(a.zw,b.xy-a.xy),q);
}
vec2 transform(vec4 pose, vec2 v) { precise vec2 r = rotate(pose.zw,v)+pose.xy; return r; }
void clipSegment(vec2 p,vec2 q,float fp,float fq,float length,out vec2 cp,out vec2 cq)
{
    cp=p; cq=q;
    if(fp<0&&fq>0) cp=lerp2(p,q,-fp/(fq-fp));
    else if(fq<0&&fp>0) cq=lerp2(q,p,-fq/(fp-fq));
    if(fp>length&&fq<length) cp=lerp2(p,q,(fp-length)/(fp-fq));
    else if(fq>length&&fp<length) cq=lerp2(q,p,(fq-length)/(fq-fp));
}
// Closest fractions on two nondegenerate segments, preserving scalar operation order.
vec2 fractions(vec2 p1,vec2 q1,vec2 p2,vec2 q2)
{
    vec2 d1=q1-p1,d2=q2-p2,r=p1-p2;
    float dd1=dot2(d1,d1),dd2=dot2(d2,d2),rd1=dot2(r,d1),rd2=dot2(r,d2),d12=dot2(d1,d2);
    precise float denominator=dd1*dd2-d12*d12;
    float f1=denominator!=0?clamp((d12*rd2-rd1*dd2)/denominator,0,1):0;
    float f2=(d12*f1+rd2)/dd2;
    if(f2<0) { f2=0; f1=clamp(-rd1/dd1,0,1); }
    else if(f2>1) { f2=1; f1=clamp((d12-rd1)/dd1,0,1); }
    return vec2(f1,f2);
}
