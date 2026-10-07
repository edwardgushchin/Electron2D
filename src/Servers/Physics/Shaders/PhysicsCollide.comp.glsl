// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi
// SPDX-License-Identifier: MIT
#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsContact.inc.glsl"
layout(local_size_x = 64) in;
struct Geometry { vec4 info; vec4 vertices[8]; };
struct Pair { vec4 ids; vec4 poseA; vec4 poseB; };
struct Result { vec4 normal; vec4 anchor1; vec4 point1; vec4 anchor2; vec4 point2; };
layout(std430, set = 1, binding = 0) buffer Shapes { Geometry shapes[]; };
layout(std430, set = 1, binding = 1) buffer Pairs { Pair pairs[]; };
layout(std430, set = 1, binding = 2) buffer Results { Result results[]; };
layout(std430, set = 1, binding = 3) buffer Matched { ContactHistory matched[]; };
layout(std430, set = 0, binding = 0) readonly buffer Solved { Contact solved[]; };
layout(std430, set = 0, binding = 1) readonly buffer UploadedHistory { ContactHistory uploadedHistory[]; };
layout(std140, set = 2, binding = 0) uniform Settings { uvec4 settings; };
const float epsilon = 1.1920928955078125e-7;
const float speculative = 0.02;
const float slop = 0.005;
struct Polygon { int count; float radius; vec2 vertices[8]; vec2 normals[8]; };
struct Manifold { vec2 normal; int count; vec2 anchors[2]; float separation[2]; uint ids[2]; };
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
Manifold emptyManifold()
{
    Manifold m; m.normal=vec2(0); m.count=0;
    for(int i=0;i<2;i++) { m.anchors[i]=vec2(0); m.separation[i]=0; m.ids[i]=0; }
    return m;
}
uint feature(int a, int b) { return (uint(a)<<8)|uint(b); }
void point(inout Manifold m, int i, vec2 p, float separation, uint id)
{ m.anchors[i]=p; m.separation[i]=separation; m.ids[i]=id; }
Manifold singlePoint(vec2 a, vec2 b, float ra, float rb)
{
    Manifold m=emptyManifold(); vec2 delta=b-a; float distance=sqrt(dot2(delta,delta));
    float separation=distance-ra-rb;
    if(separation>speculative) return m;
    m.normal=distance<epsilon?vec2(0):(1/distance)*delta;
    point(m,0,lerp2(addScaled(a,ra,m.normal),addScaled(b,-rb,m.normal),0.5),separation,0);
    m.count=1; return m;
}
Manifold capsuleCircle(Geometry a, vec2 center, float radius)
{
    vec2 p1=a.vertices[0].xy, p2=a.vertices[1].xy, e=p2-p1;
    float s1=dot2(center-p1,e), s2=dot2(p2-center,e);
    vec2 p=s1<0?p1:(s2<0?p2:addScaled(p1,s1/dot2(e,e),e));
    return singlePoint(p,center,a.info.y,radius);
}
Manifold polygonCircle(Geometry a, vec2 center, float radiusB)
{
    Manifold m=emptyManifold(); int count=int(a.info.z), edge=0;
    float separation=-3.402823466e38, radiusA=a.info.y, radius=radiusA+radiusB;
    for(int i=0;i<count;i++)
    { float s=dot2(a.vertices[i].zw,center-a.vertices[i].xy); if(s>separation) { separation=s; edge=i; } }
    if(separation>radius+speculative) return m;
    vec2 v1=a.vertices[edge].xy, v2=a.vertices[(edge+1)%count].xy;
    float u1=dot2(center-v1,v2-v1), u2=dot2(center-v2,v1-v2);
    vec2 normal, ca, cb;
    if((u1<0||u2<0)&&separation>epsilon)
    {
        vec2 v=u1<0?v1:v2; normal=unit(center-v); separation=dot2(center-v,normal);
        if(separation>radius+speculative) return m;
        ca=addScaled(v,radiusA,normal); cb=addScaled(center,-radiusB,normal);
        separation=dot2(cb-ca,normal);
    }
    else
    {
        normal=a.vertices[edge].zw;
        ca=addScaled(center,radiusA-dot2(center-v1,normal),normal);
        cb=addScaled(center,-radiusB,normal); separation-=radius;
    }
    m.normal=normal; m.count=1; point(m,0,lerp2(ca,cb,0.5),separation,0); return m;
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
void clipSegment(vec2 p,vec2 q,float fp,float fq,float length,out vec2 cp,out vec2 cq)
{
    cp=p; cq=q;
    if(fp<0&&fq>0) cp=lerp2(p,q,-fp/(fq-fp));
    else if(fq<0&&fp>0) cq=lerp2(q,p,-fq/(fp-fq));
    if(fp>length&&fq<length) cp=lerp2(p,q,(fp-length)/(fp-fq));
    else if(fq>length&&fp<length) cq=lerp2(q,p,(fq-length)/(fq-fp));
}
vec3 capsuleAxis(vec2 p,vec2 q,vec2 reference,vec2 direction)
{
    vec2 normal=vec2(-direction.y,direction.x);
    float s1=dot2(p-reference,normal),s2=dot2(q-reference,normal),sp=min(s1,s2),sn=min(-s1,-s2);
    return sp>sn?vec3(normal,sp):vec3(-normal,sn);
}
Manifold capsules(Geometry a,Geometry b,vec4 xf)
{
    Manifold m=emptyManifold(); vec2 p1=vec2(0),q1=a.vertices[1].xy-a.vertices[0].xy;
    vec2 p2=transform(xf,b.vertices[0].xy),q2=transform(xf,b.vertices[1].xy);
    vec2 f=fractions(p1,q1,p2,q2),closest1=addScaled(p1,f.x,q1-p1),closest2=addScaled(p2,f.y,q2-p2);
    vec2 delta=closest2-closest1; float ds=dot2(delta,delta),ra=a.info.y,rb=b.info.y,radius=ra+rb;
    if(ds>(radius+speculative)*(radius+speculative)) return m;
    float distance=sqrt(ds),l1=sqrt(dot2(q1,q1)),l2=sqrt(dot2(q2-p2,q2-p2));
    vec2 u1=(1/l1)*q1,u2=(1/l2)*(q2-p2);
    float fp2=dot2(p2-p1,u1),fq2=dot2(q2-p1,u1),fp1=dot2(p1-p2,u2),fq1=dot2(q1-p2,u2);
    bool outsideA=(fp2<=0&&fq2<=0)||(fp2>=l1&&fq2>=l1);
    bool outsideB=(fp1<=0&&fq1<=0)||(fp1>=l2&&fq1>=l2);
    if(!outsideA&&!outsideB)
    {
        vec3 axisA=capsuleAxis(p2,q2,p1,u1),axisB=capsuleAxis(p1,q1,p2,u2);
        bool flip=axisA.z+0.1*slop<axisB.z;
        vec2 n=flip?axisB.xy:axisA.xy,cp,cq;
        if(flip) clipSegment(p1,q1,fp1,fq1,l2,cp,cq);
        else clipSegment(p2,q2,fp2,fq2,l1,cp,cq);
        vec2 reference=flip?p2:p1;
        float sp=dot2(cp-reference,n),sq=dot2(cq-reference,n);
        if(sp<=distance+slop||sq<=distance+slop)
        {
            float r1=flip?rb:ra,r2=flip?ra:rb;
            m.normal=flip?-n:n; m.count=2;
            point(m,0,addScaled(cp,0.5*(r1-r2-sp),n),sp-radius,0);
            point(m,1,addScaled(cq,0.5*(r1-r2-sq),n),sq-radius,flip?256u:1u);
        }
    }
    if(m.count==0)
    {
        m.normal=ds>epsilon*epsilon?unit(delta):vec2(-u1.y,u1.x); m.count=1;
        point(m,0,lerp2(addScaled(closest1,ra,m.normal),addScaled(closest2,-rb,m.normal),0.5),
            distance-radius,feature(f.x==0?0:1,f.y==0?0:1));
    }
    return m;
}
Polygon polygon(Geometry g)
{
    Polygon p; p.count=int(g.info.z); p.radius=g.info.y;
    for(int i=0;i<p.count;i++) { p.vertices[i]=g.vertices[i].xy; p.normals[i]=g.vertices[i].zw; }
    if(int(g.info.x)!=3)
    {
        p.count=2; vec2 axis=unit(p.vertices[1]-p.vertices[0]);
        p.normals[0]=vec2(axis.y,-axis.x); p.normals[1]=-p.normals[0];
    }
    return p;
}
float maxSeparation(Polygon a,Polygon b,out int edge)
{
    float best=-3.402823466e38; edge=0;
    for(int i=0;i<a.count;i++)
    {
        float s=3.402823466e38;
        for(int j=0;j<b.count;j++) s=min(s,dot2(a.normals[i],b.vertices[j]-a.vertices[i]));
        if(s>best) { best=s; edge=i; }
    }
    return best;
}
Manifold clipPolygons(Polygon a,Polygon b,int edgeA,int edgeB,bool flip)
{
    Manifold m=emptyManifold(); Polygon p1=flip?b:a,p2=flip?a:b;
    int i11=flip?edgeB:edgeA,i12=(i11+1)%p1.count,i21=flip?edgeA:edgeB,i22=(i21+1)%p2.count;
    vec2 normal=p1.normals[i11],tangent=vec2(-normal.y,normal.x);
    vec2 v11=p1.vertices[i11],v12=p1.vertices[i12],v21=p2.vertices[i21],v22=p2.vertices[i22];
    float upper1=dot2(v12-v11,tangent),upper2=dot2(v21-v11,tangent),lower2=dot2(v22-v11,tangent);
    if(upper2<0||upper1<lower2) return m;
    vec2 lower=lower2<0&&upper2-lower2>epsilon?lerp2(v22,v21,-lower2/(upper2-lower2)):v22;
    vec2 upper=upper2>upper1&&upper2-lower2>epsilon?lerp2(v22,v21,(upper1-lower2)/(upper2-lower2)):v21;
    float sl=dot2(lower-v11,normal),su=dot2(upper-v11,normal),r1=p1.radius,r2=p2.radius;
    lower=addScaled(lower,0.5*(r1-r2-sl),normal); upper=addScaled(upper,0.5*(r1-r2-su),normal);
    m.count=2; m.normal=flip?-normal:normal;
    if(flip) { point(m,0,upper,su-r1-r2,feature(i21,i12)); point(m,1,lower,sl-r1-r2,feature(i22,i11)); }
    else { point(m,0,lower,sl-r1-r2,feature(i11,i22)); point(m,1,upper,su-r1-r2,feature(i12,i21)); }
    return m;
}
Manifold polygons(Geometry ga,Geometry gb,vec4 xf)
{
    Polygon a=polygon(ga),b=polygon(gb); vec2 origin=a.vertices[0];
    for(int i=0;i<a.count;i++) a.vertices[i]-=origin;
    for(int i=0;i<b.count;i++) { b.vertices[i]=transform(xf,b.vertices[i]); b.normals[i]=rotate(xf.zw,b.normals[i]); }
    int edgeA,edgeB; float sa=maxSeparation(a,b,edgeA),sb=maxSeparation(b,a,edgeB),radius=a.radius+b.radius;
    Manifold m=emptyManifold(); if(sa>radius+speculative||sb>radius+speculative) return m;
    bool flip=sa<sb; vec2 direction=flip?b.normals[edgeB]:a.normals[edgeA];
    float minimum=3.402823466e38; int incident=0, count=flip?a.count:b.count;
    for(int i=0;i<count;i++) { float d=dot2(direction,flip?a.normals[i]:b.normals[i]); if(d<minimum) { minimum=d; incident=i; } }
    if(flip) edgeA=incident; else edgeB=incident;
    if(sa>0.1*slop||sb>0.1*slop)
    {
        int ia2=(edgeA+1)%a.count,ib2=(edgeB+1)%b.count;
        vec2 f=fractions(a.vertices[edgeA],a.vertices[ia2],b.vertices[edgeB],b.vertices[ib2]);
        if((f.x==0||f.x==1)&&(f.y==0||f.y==1))
        {
            int ia=f.x==0?edgeA:ia2,ib=f.y==0?edgeB:ib2; vec2 va=a.vertices[ia],vb=b.vertices[ib];
            float distance=sqrt(dot2(vb-va,vb-va)); if(distance>radius+speculative) return m;
            m.normal=(1/distance)*(vb-va); m.count=1;
            point(m,0,lerp2(addScaled(va,a.radius,m.normal),addScaled(vb,-b.radius,m.normal),0.5),distance-radius,feature(ia,ib));
        }
        else m=clipPolygons(a,b,edgeA,edgeB,flip);
    }
    else m=clipPolygons(a,b,edgeA,edgeB,flip);
    return m;
}
void main()
{
    uint index=gl_GlobalInvocationID.x; if(index>=settings.x) return;
    Pair pair=pairs[index]; Result r=Result(vec4(0),vec4(0),vec4(0),vec4(0),vec4(0));
    if(pair.ids.z==0) { results[index]=r; matched[index]=ContactHistory(vec4(0),vec4(0)); return; }
    Geometry a=shapes[int(pair.ids.x)],b=shapes[int(pair.ids.y)]; int ta=int(a.info.x),tb=int(b.info.x);
    Manifold m; vec2 origin=vec2(0);
    if(tb==0)
    {
        vec2 center=transform(relativePose(pair.poseA,pair.poseB),b.vertices[0].xy);
        if(ta==0) m=singlePoint(a.vertices[0].xy,center,a.info.y,b.info.y);
        else if(ta==3) m=polygonCircle(a,center,b.info.y);
        else m=capsuleCircle(a,center,b.info.y);
    }
    else
    {
        origin=a.vertices[0].xy; vec4 shifted=pair.poseA; shifted.xy+=rotate(shifted.zw,origin);
        vec4 xf=relativePose(shifted,pair.poseB);
        if(ta!=3&&tb!=3) m=capsules(a,b,xf); else m=polygons(a,b,xf);
    }
    if(m.count>0)
    {
        r.normal=vec4(rotate(pair.poseA.zw,m.normal),m.count,0);
        for(int i=0;i<m.count;i++)
        {
            vec2 anchor=rotate(pair.poseA.zw,m.anchors[i]+origin);
            vec4 ap=vec4(anchor,m.separation[i],float(m.ids[i]));
            vec4 bp=vec4(anchor+(pair.poseA.xy-pair.poseB.xy),pair.poseA.xy+anchor);
            if(i==0) { r.anchor1=ap; r.point1=bp; } else { r.anchor2=ap; r.point2=bp; }
        }
    }
    ContactHistory old = ContactHistory(vec4(0),vec4(0));
    int source = int(pair.ids.w);
    if (source >= 0)
    {
        Contact c = solved[source];
        old = ContactHistory(vec4(c.impulses1.xy,c.impulses2.xy),vec4(c.impulses1.w,c.impulses2.w,c.ids.z,c.rolling.z));
    }
    else if (source < -1) old = uploadedHistory[-source-2];
    ContactHistory warm = ContactHistory(vec4(0),vec4(0));
    if (m.count > 0) warm.features = vec4(float(m.ids[0]),float(m.ids[1]),m.count,old.features.w);
    for (int i=0;i<m.count;i++)
    {
        for (int j=0;j<int(old.features.z);j++)
        {
            if (float(m.ids[i]) != old.features[j]) continue;
            vec2 impulse = j==0 ? old.impulses.xy : old.impulses.zw;
            if (i==0) warm.impulses.xy=impulse; else warm.impulses.zw=impulse;
            if (j==0) old.impulses.xy=vec2(0); else old.impulses.zw=vec2(0);
            r.normal.w += float(1<<i);
            break;
        }
    }
    results[index]=r; matched[index]=warm;
}
