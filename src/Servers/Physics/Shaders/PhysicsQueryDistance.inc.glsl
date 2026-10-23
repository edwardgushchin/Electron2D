// Distance between convex cores, optionally extruded by each full translation.
vec2 querySupportVertex(Hull h,vec2 d)
{
    vec2 best=vertex(h,0u);float projection=dot2(best,d);
    for(uint i=1u;i<h.count;i++){vec2 candidate=vertex(h,i);float value=dot2(candidate,d);if(value>projection){best=candidate;projection=value;}}
    return best;
}
vec2 querySupport(Hull a,Hull b,vec2 sweepA,vec2 sweepB,vec2 d)
{
    // GJK requires an extreme vertex; tolerant contact-face selection can cycle near crossing segments.
    vec2 pa=querySupportVertex(a,-d),pb=querySupportVertex(b,d);
    if(dot2(sweepA,-d)>0)pa+=sweepA;
    if(dot2(sweepB,d)>0)pb+=sweepB;
    return pb-pa;
}
vec3 lineWeights(vec2 a,vec2 b)
{
    vec2 d=b-a;float dd=dot2(d,d),t=dd>0?clamp(-dot2(a,d)/dd,0,1):0;
    return vec3(1-t,t,0);
}
vec3 triangleWeights(vec2 a,vec2 b,vec2 c)
{
    float denominator=cross2(b-a,c-a);
    if(denominator!=0)
    {
        vec3 weights=vec3(cross2(b,c),cross2(c,a),cross2(a,b))/denominator;
        if(all(greaterThanEqual(weights,vec3(0))))return weights;
    }
    vec3 ab=lineWeights(a,b),bc=lineWeights(b,c),ca=lineWeights(c,a);
    float dab=dot2(ab.x*a+ab.y*b,ab.x*a+ab.y*b),dbc=dot2(bc.x*b+bc.y*c,bc.x*b+bc.y*c),dca=dot2(ca.x*c+ca.y*a,ca.x*c+ca.y*a);
    return dab<=dbc&&dab<=dca?ab:dbc<=dca?vec3(0,bc.xy):vec3(ca.y,0,ca.x);
}
float queryDistance(Hull a,Hull b,vec2 sweepA,vec2 sweepB,out vec2 normal)
{
    if(a.boundary||b.boundary)
    {
        if(a.boundary&&b.boundary){normal=vec2(0);return 3.402823466e38;}
        bool flip=b.boundary;Hull plane=flip?b:a,other=flip?a:b;vec3 line=boundaryPlane(plane);
        vec2 sp=flip?sweepB:sweepA,so=flip?sweepA:sweepB;
        float minimum=dot2(line.xy,vertex(other,0u));
        for(uint i=1u;i<other.count;i++)minimum=min(minimum,dot2(line.xy,vertex(other,i)));
        normal=flip?-line.xy:line.xy;return max(0,minimum+min(0,dot2(line.xy,so))-line.z-max(0,dot2(line.xy,sp)));
    }
    vec2 simplex[3];uint count=1u;vec3 weights=vec3(1,0,0);
    simplex[0]=querySupport(a,b,sweepA,sweepB,vec2(1,0));
    vec2 v=simplex[0];
    [[dont_unroll]] for(uint iteration=0u;iteration<64u;iteration++)
    {
        float squared=dot2(v,v);if(!finite2(vec2(squared))){fail();normal=vec2(0);return 0;}
        if(squared<=1e-12)break;
        vec2 next=querySupport(a,b,sweepA,sweepB,-v);
        bool duplicate=false;for(uint i=0u;i<count;i++)if(next==simplex[i])duplicate=true;
        if(duplicate||squared-dot2(v,next)<=max(1e-12,1e-7*squared))break;
        if(count==3u){failQuery(4u);break;}
        simplex[count++]=next;
        weights=count==2u?lineWeights(simplex[0],simplex[1]):triangleWeights(simplex[0],simplex[1],simplex[2]);
        vec2 previous=v;v=vec2(0);uint kept=0u;vec3 reduced=vec3(0);
        for(uint i=0u;i<count;i++)if(weights[i]>0){v+=weights[i]*simplex[i];simplex[kept]=simplex[i];reduced[kept++]=weights[i];}
        count=kept;weights=reduced;
        if(count==3u){normal=vec2(0);return 0;}
        if(dot2(v,v)>=squared){v=previous;break;}
        if(iteration==63u)failQuery(2u);
    }
    normal=normalized(v);return length(v);
}
// Exact time slabs retain zero-width intersections between translating flat cores.
bool queryFlatSweep(Hull a,Hull b,vec2 motion,out float fraction,out vec2 normal)
{
    float low=0,high=1;normal=vec2(0);fraction=0;
    vec2 offset=b.pose.xy-a.pose.xy;a.pose.xy=b.pose.xy=vec2(0);
    uint ea=edges(a),eb=edges(b),extraA=a.count==2u?1u:0u,extraB=b.count==2u?1u:0u;
    [[dont_unroll]] for(uint i=0u;i<ea+eb+extraA+extraB;i++)
    {
        vec2 axis=i<ea?edgeNormal(a,i):i<ea+eb?edgeNormal(b,i-ea):
            i<ea+eb+extraA?normalized(vertex(a,1u)-vertex(a,0u)):normalized(vertex(b,1u)-vertex(b,0u));
        if(axis==vec2(0))continue;
        vec2 pa=project(a,axis),pb=project(b,axis);float speed=dot2(motion,axis);
        float shift=dot2(offset,axis);
        if(speed==0){if(pa.y<pb.x+shift||pb.y+shift<pa.x)return false;continue;}
        float enter=(shift+pb.x-pa.y)/speed,leave=(shift+pb.y-pa.x)/speed;
        if(speed<0){float swap=enter;enter=leave;leave=swap;}
        if(enter>low){low=enter;normal=speed>0?axis:-axis;}
        high=min(high,leave);if(low>high)return false;
    }
    fraction=low;return low<=1&&high>=0;
}
// Advances a translating convex core without sampling past a thin obstacle.
bool querySweep(Hull a,Hull b,vec2 motion,vec2 sweptB,float target,float tolerance,out float fraction,out vec2 normal)
{
    fraction=0;normal=vec2(0);float time=0;
    if(!a.boundary&&!b.boundary&&target==0&&sweptB==vec2(0)&&a.extension==vec2(0)&&b.extension==vec2(0)&&
        ((a.count>1u&&vertices[a.start+1u]!=vertices[a.start])||(b.count>1u&&vertices[b.start+1u]!=vertices[b.start])))return queryFlatSweep(a,b,motion,fraction,normal);
    // Disk queries preserve geometric radii instead of a solver penetration target.
    if(!a.boundary&&!b.boundary&&a.count==1u&&b.count==1u&&sweptB==vec2(0)&&a.extension==vec2(0)&&b.extension==vec2(0)&&a.radius+b.radius>0)
    {
        vec2 offset=vertex(a,0u)-vertex(b,0u);float radius=a.radius+b.radius;
        if(length(offset)<=radius){normal=normalized(-offset);return true;}
        float size=length(motion);if(size==0)return false;
        vec2 axis=motion/size;float projection=dot2(offset,axis),side=dot2(offset,vec2(-axis.y,axis.x));
        // Resolve the small perpendicular clearance without subtracting large squared distances.
        float disc=(radius-abs(side))*(radius+abs(side));
        if(projection>=0||disc<0)return false;
        float at=(-projection-sqrt(disc))/size;if(at<0||at>1)return false;
        fraction=at;normal=normalized(-offset-at*motion);return true;
    }
    [[dont_unroll]] for(uint i=0u;i<128u;i++)
    {
        Hull moved=a;moved.pose.xy+=time*motion;
        vec2 previous=normal;float distance=queryDistance(moved,b,vec2(0),sweptB,normal),gap=distance-target;
        if(normal==vec2(0)||(distance<=0.00001&&previous!=vec2(0)))normal=previous;
        if(gap<=tolerance){fraction=time;return true;}
        float closing=dot2(motion,normal);if(closing<=0)return false;
        float next=time+gap/closing;if(!finite2(vec2(next))){fail();return false;}if(next>1)return false;
        if(next==time){fraction=time;return true;}
        if(next<time){failQuery(8u);return false;}time=next;
    }
    failQuery(16u);return false;
}

vec2 querySweepBracket(float impact)
{
    float low=0,high=1,coefficient=0.5;
    for(uint step=0u;step<8u;step++)
    {
        float middle=low+(high-low)*coefficient;
        if(impact<=middle){high=middle;coefficient=step==0u||low>0?0.5:0.25;}
        else{low=middle;coefficient=step==0u||high<1?0.5:0.75;}
    }
    return vec2(low,clamp(impact,low,high));
}
