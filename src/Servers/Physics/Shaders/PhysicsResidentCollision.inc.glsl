// Shared resident convex/segment/ray geometry in body-A-relative coordinates.
struct Hull { uint start; uint count; float radius; float winding; vec4 pose; bool midpoint; };
struct Axis { vec2 normal; float separation; uint owner; uint edge; };
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
