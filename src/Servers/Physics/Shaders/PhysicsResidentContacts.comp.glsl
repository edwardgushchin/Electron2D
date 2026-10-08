#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x = 64) in;
struct ContactPoint { uvec4 pair; uvec4 features; vec4 normal; vec4 anchors; };
layout(std430, set=0, binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430, set=0, binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430, set=0, binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430, set=0, binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430, set=0, binding=4) readonly buffer Pairs { uvec4 pairs[]; };
layout(std430, set=1, binding=0) buffer Contacts { ContactPoint contacts[]; };
layout(std430, set=1, binding=1) buffer Summary { uvec2 summary; };
layout(std140, set=2, binding=0) uniform Settings { uvec4 counts; vec4 tolerances; };
const float epsilon=1.1920928955078125e-7;
#include "PhysicsCollisionMath.inc.glsl"
struct Hull { uint start; uint count; float radius; float winding; vec4 pose; bool midpoint; };
struct Axis { vec2 normal; float separation; uint owner; uint edge; };
uvec4 activePair;
ResidentBody bodyA,bodyB;
float contactLimit;
bool sensor;
void fail() { atomicOr(summary.x,1u); }
bool finite2(vec2 v) { return !any(isnan(v))&&!any(isinf(v)); }
vec2 normalized(vec2 v)
{
    if(!finite2(v)){fail();return vec2(0);}
    float scale=max(abs(v.x),abs(v.y));
    if(scale==0)return vec2(0);
    v/=scale; return v/sqrt(dot2(v,v));
}
float cross2(vec2 a,vec2 b) { return a.x*b.y-a.y*b.x; }
vec2 vertex(Hull h,uint i)
{
    vec2 v=vertices[h.start+i];
    if(h.midpoint)v=0.5*v+0.5*vertices[h.start+1u];
    return transform(h.pose,v);
}
Hull hull(Shape s,Geometry g,ResidentBody body,uint piece)
{
    Hull h; h.start=g.data.x; h.count=g.data.y; h.radius=g.parameters.x; h.winding=g.parameters.z; h.midpoint=false;
    h.pose=vec4(body.pose.xy-bodyA.pose.xy+rotate(body.pose.zw,s.pose.xy),
        body.pose.z*s.pose.z-body.pose.w*s.pose.w,body.pose.w*s.pose.z+body.pose.z*s.pose.w);
    if(g.data.z==5u) {h.start+=2u*piece;h.count=2u;}
    if(g.data.z==1u||g.data.z==2u||g.data.z==5u)
    {
        vec2 d=vertices[h.start+1u]-vertices[h.start];
        if(dot2(d,d)<=tolerances.y*tolerances.y) {h.count=1u;h.midpoint=true;}
    }
    return h;
}
uint edges(Hull h) { return h.count==1u?0u:h.count; }
vec2 edgeNormal(Hull h,uint i)
{
    vec2 e=vertex(h,(i+1u)%h.count)-vertex(h,i);
    return normalized(vec2(e.y,-e.x))*h.winding;
}
vec2 closest(vec2 p,vec2 a,vec2 b)
{
    vec2 d=b-a; float dd=dot2(d,d),projection=dot2(p-a,d);
    if(!finite2(vec2(dd,projection))){fail();return a;}
    return dd==0?a:a+clamp(projection/dd,0,1)*d;
}
vec2 project(Hull h,vec2 n)
{
    float lo=dot2(vertex(h,0u),n),hi=lo;
    for(uint i=1u;i<h.count;i++) {float p=dot2(vertex(h,i),n);lo=min(lo,p);hi=max(hi,p);}
    return vec2(lo-h.radius,hi+h.radius);
}
bool axisTest(Hull a,Hull b,vec2 n,uint owner,uint edge,inout Axis best)
{
    if(!finite2(n)){fail();return false;}
    if(n==vec2(0))return true;
    vec2 pa=project(a,n),pb=project(b,n);float separation=pb.x-pa.y;
    if(!finite2(vec2(separation))) {fail();return false;}
    if(separation>contactLimit)return false;
    if(separation>best.separation+tolerances.z)best=Axis(n,separation,owner,edge);
    return true;
}
bool cornerAxis(Hull a,Hull b,vec2 axis,inout Axis best)
{
    vec2 n=normalized(axis);
    return axisTest(a,b,n,0u,0u,best)&&axisTest(a,b,-n,0u,0u,best);
}
bool separatingAxis(Hull a,Hull b,out Axis best)
{
    best=Axis(vec2(1,0),-3.402823466e38,0u,0u);
    for(uint i=0u;i<edges(a);i++)if(!axisTest(a,b,edgeNormal(a,i),1u,i,best))return false;
    for(uint i=0u;i<edges(b);i++)if(!axisTest(a,b,-edgeNormal(b,i),2u,i,best))return false;
    if(a.radius>0||b.radius>0||a.count<=2u||b.count<=2u)
    {
        // ponytail: direct edge/corner scans preserve complete contours; use support-map distance if large-contour cost dominates.
        for(uint i=0u;i<a.count;i++)
            for(uint j=0u;j<b.count;j++)
            {
                vec2 av=vertex(a,i),bv=vertex(b,j);
                if(!cornerAxis(a,b,bv-av,best))return false;
                if(b.count>1u&&!cornerAxis(a,b,closest(av,bv,vertex(b,(j+1u)%b.count))-av,best))return false;
                if(a.count>1u&&!cornerAxis(a,b,bv-closest(bv,av,vertex(a,(i+1u)%a.count)),best))return false;
            }
    }
    if(best.separation==-3.402823466e38)return axisTest(a,b,vec2(1,0),0u,0u,best);
    return true;
}
uint edgeFeature(Hull h,uint i,vec2 p)
{
    uint j=(i+1u)%h.count;
    if(length(p-vertex(h,i))<=tolerances.z)return 2u*i;
    if(length(p-vertex(h,j))<=tolerances.z)return 2u*j;
    return 2u*i+1u;
}
vec2 support(Hull h,vec2 n,vec2 toward,out uint feature)
{
    uint at=0u;float best=dot2(vertex(h,0u),n);
    for(uint i=1u;i<h.count;i++) {float p=dot2(vertex(h,i),n);if(p>best){best=p;at=i;}}
    vec2 result=vertex(h,at);feature=2u*at;
    for(uint i=0u;i<edges(h);i++)
    {
        vec2 a=vertex(h,i),b=vertex(h,(i+1u)%h.count);
        if(abs(dot2(a,n)-best)<=tolerances.z&&abs(dot2(b,n)-best)<=tolerances.z)
        {result=closest(toward,a,b);feature=edgeFeature(h,i,result);break;}
    }
    return result;
}
void emitPoint(vec2 a,vec2 b,vec2 normal,uvec4 features)
{
    float separation=dot2(b-a,normal);
    if(separation>contactLimit+tolerances.z)return;
    vec2 localA=inverseRotate(bodyA.pose.zw,a);
    vec2 localB=inverseRotate(bodyB.pose.zw,b-(bodyB.pose.xy-bodyA.pose.xy));
    if(!finite2(localA)||!finite2(localB)||!finite2(normal)||!finite2(vec2(separation))||abs(dot2(normal,normal)-1)>0.001)
    {fail();return;}
    uint at=atomicAdd(summary.y,1u);
    if(at==0xffffffffu){fail();return;}
    if(at<counts.y)contacts[at]=ContactPoint(activePair,features,vec4(normal,separation,sensor?1:0),vec4(localA,localB));
}
void facePoint(Hull refHull,Hull incHull,vec2 p,vec2 n,uint refEdge,uint incEdge,bool flip,uvec2 pieces)
{
    vec2 refVertex=vertex(refHull,refEdge);
    vec2 onPlane=p+dot2(refVertex-p,n)*n;
    vec2 a=onPlane+refHull.radius*n,b=p-incHull.radius*n;
    uint fa=edgeFeature(refHull,refEdge,onPlane),fb=incHull.count==1u?0u:edgeFeature(incHull,incEdge,p);
    if(flip)emitPoint(b,a,-n,uvec4(fb,fa,pieces));
    else emitPoint(a,b,n,uvec4(fa,fb,pieces));
}
void ordinary(Hull a,Hull b,uvec2 pieces)
{
    Axis axis;if(!separatingAxis(a,b,axis))return;
    if(axis.owner!=0u)
    {
        bool flip=axis.owner==2u;Hull r=flip?b:a,inc=flip?a:b;vec2 n=flip?-axis.normal:axis.normal;
        if(inc.count==1u) {facePoint(r,inc,vertex(inc,0u),n,axis.edge,0u,flip,pieces);return;}
        uint incident=0u;float minimum=3.402823466e38;
        for(uint i=0u;i<edges(inc);i++){float d=dot2(n,edgeNormal(inc,i));if(d<minimum){minimum=d;incident=i;}}
        vec2 from=vertex(r,axis.edge),to=vertex(r,(axis.edge+1u)%r.count),tangent=normalized(to-from);
        float span=dot2(to-from,tangent);
        vec2 p=vertex(inc,incident),q=vertex(inc,(incident+1u)%inc.count);
        float fp=dot2(p-from,tangent),fq=dot2(q-from,tangent);
        if(max(fp,fq)<0||min(fp,fq)>span)return;
        vec2 cp,cq;clipSegment(p,q,fp,fq,span,cp,cq);
        facePoint(r,inc,cp,n,axis.edge,incident,flip,pieces);
        if(length(cq-cp)>tolerances.z)facePoint(r,inc,cq,n,axis.edge,incident,flip,pieces);
        return;
    }
    uint fa,fb;vec2 pa=support(a,axis.normal,vertex(b,0u),fa);
    vec2 pb=support(b,-axis.normal,pa,fb);pa=support(a,axis.normal,pb,fa);
    emitPoint(pa+a.radius*axis.normal,pb-b.radius*axis.normal,axis.normal,uvec4(fa,fb,pieces));
}
bool rayCircle(vec2 from,vec2 d,vec2 center,float radius,out float fraction,out vec2 normal)
{
    fraction=0;normal=vec2(0);vec2 m=from-center;
    float aa=dot2(d,d),b=dot2(m,d),c=dot2(m,m)-radius*radius,discriminant=b*b-aa*c;
    if(!finite2(vec2(aa,b))||!finite2(vec2(c,discriminant))){fail();return false;}
    if(c<0||discriminant<0||aa==0)return false;
    fraction=(-b-sqrt(discriminant))/aa;
    if(fraction<0||fraction>1)return false;
    normal=normalized(from+fraction*d-center);
    return normal!=vec2(0)&&dot2(normal,-d)>=0.0001;
}
bool rayHit(Hull h,vec2 from,vec2 d,out float fraction,out vec2 normal,out uint feature)
{
    fraction=2;normal=vec2(0);feature=0u;
    if(h.count==1u)return rayCircle(from,d,vertex(h,0u),h.radius,fraction,normal);
    if(h.count==2u)
    {
        vec2 a=vertex(h,0u),b=vertex(h,1u),axis=normalized(b-a),n=vec2(axis.y,-axis.x);
        float extent=dot2(b-a,axis),origin=dot2(from-a,axis),speed=dot2(d,axis);
        if(length(from-closest(from,a,b))<h.radius)return false;
        for(int side=-1;side<=1;side+=2)
        {
            vec2 outNormal=float(side)*n;float denominator=dot2(d,outNormal);
            if(denominator>=-0.0001)continue;
            float t=(h.radius-dot2(from-a,outNormal))/denominator;
            float along=origin+t*speed;
            if(t>=0&&t<=1&&along>=0&&along<=extent&&t<fraction)
            {fraction=t;normal=outNormal;feature=1u;}
        }
        if(h.radius>0)
            for(uint i=0u;i<2u;i++)
            {
                float t;vec2 outNormal;
                if(!rayCircle(from,d,i==0u?a:b,h.radius,t,outNormal))continue;
                float along=origin+t*speed;
                if((i==0u?along<=0:along>=extent)&&t<fraction){fraction=t;normal=outNormal;feature=2u*i;}
            }
        return fraction<=1;
    }
    float entry=0,exit=1;
    for(uint i=0u;i<h.count;i++)
    {
        vec2 n=edgeNormal(h,i);float distance=dot2(n,vertex(h,i)-from),speed=dot2(n,d);
        if(speed==0){if(distance<0)return false;continue;}
        float t=distance/speed;
        if(speed<0&&t>entry){entry=t;normal=n;feature=2u*i+1u;}
        else if(speed>0)exit=min(exit,t);
        if(entry>exit)return false;
    }
    fraction=entry;return normal!=vec2(0)&&entry<=1&&dot2(normal,-d)>=0.0001;
}
void rayContact(Shape ra,Geometry rg,ResidentBody rb,Shape other,Geometry og,ResidentBody ob,bool flip)
{
    Hull ray=hull(ra,rg,rb,0u);vec2 from=vertex(ray,0u),d=vertex(ray,1u)-from;
    if(d==vec2(0))return;
    vec2 axis=normalized(d);d+=axis*contactLimit;
    float nearest=2;vec2 hitNormal=vec2(0);uint hitFeature=0u,hitPiece=0u;
    uint pieceCount=og.data.z==5u?og.data.y/2u:1u;
    for(uint i=0u;i<pieceCount;i++)
    {
        float t;vec2 n;uint f;
        if(rayHit(hull(other,og,ob,i),from,d,t,n,f)&&t<nearest){nearest=t;hitNormal=n;hitFeature=f;hitPiece=i;}
    }
    if(nearest>1)return;
    vec2 a=from+d;float depth=(1-nearest)*length(d);
    vec2 n=rg.parameters.y!=0?-hitNormal:axis,b=a-depth*n;
    if(flip)emitPoint(b,a,-n,uvec4(hitFeature,2u,hitPiece,0u));
    else emitPoint(a,b,n,uvec4(2u,hitFeature,0u,hitPiece));
}
void main()
{
    uint index=gl_GlobalInvocationID.x;if(index>=counts.x)return;
    activePair=pairs[index];
    if(activePair.x>=counts.z||activePair.y>=counts.z){fail();return;}
    Shape sa=shapes[activePair.x],sb=shapes[activePair.y];
    if(sa.policy.x!=activePair.z||sb.policy.x!=activePair.w||(sa.policy.w&1u)==0u||(sb.policy.w&1u)==0u||sa.owner.x>=counts.w||sb.owner.x>=counts.w){fail();return;}
    bodyA=bodies[sa.owner.x];bodyB=bodies[sb.owner.x];
    Geometry ga=geometries[sa.owner.z],gb=geometries[sb.owner.z];
    if(bodyA.flags.x!=sa.owner.y||bodyB.flags.x!=sb.owner.y||bodyA.flags.w==0u||bodyB.flags.w==0u||ga.data.w!=sa.owner.w||gb.data.w!=sb.owner.w){fail();return;}
    if(ga.data.y==0u||gb.data.y==0u)return;
    sensor=((sa.policy.w|sb.policy.w)&2u)!=0u;contactLimit=sensor?0:tolerances.x;
    if((ga.data.z==5u&&gb.data.z==5u)||(ga.data.z==6u&&gb.data.z==6u))return;
    if(ga.data.z==6u){rayContact(sa,ga,bodyA,sb,gb,bodyB,false);return;}
    if(gb.data.z==6u){rayContact(sb,gb,bodyB,sa,ga,bodyA,true);return;}
    uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
    for(uint i=0u;i<na;i++)for(uint j=0u;j<nb;j++)ordinary(hull(sa,ga,bodyA,i),hull(sb,gb,bodyB,j),uvec2(i,j));
}
