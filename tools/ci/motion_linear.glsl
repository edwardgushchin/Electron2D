void movingPair(Hull a,Hull b,Geometry ga,Geometry gb)
{
    bool ignored=ga.data.z==7u||(ga.data.z==5u&&gb.data.z!=7u);
    vec2 motion=ignored?vec2(0):q.motionMargin.xy;
    bool oneWay=ga.data.z!=6u&&(shapeB.policy.w&4u)!=0u;
    bool directed=ga.data.z==6u||gb.data.z==6u;
    Contact initial=evaluate(a,b,ga,gb,vec2(0),0);
    initial.valid=initial.valid&&initial.depth>=0;
    if(initial.valid&&oneWay&&dot2(q.motionMargin.xy,passDirection())<0)return;
    float low=0,high=0,margin=0;
    Hull probe=a;Contact full;
    if(directed)
    {
        full=evaluate(a,b,ga,gb,motion,0);full.valid=full.valid&&full.depth>=0;
        if(!full.valid)return;
        if(initial.valid&&initial.depth<=0.05&&dot2(q.motionMargin.xy,initial.normal)>=-0.0001)return;
        high=initial.valid?0:1;
        if(!initial.valid)
        {
            float coefficient=0.5;
            [[dont_unroll]] for(uint refinement=0u;refinement<8u;refinement++)
            {
                float middle=low+(high-low)*coefficient;
                Contact contact=evaluate(a,b,ga,gb,motion*middle,0);
                contact.valid=contact.valid&&contact.depth>=0;
                if(contact.valid){high=middle;coefficient=refinement==0u||low>0?0.5:0.25;}
                else{low=middle;coefficient=refinement==0u||high<1?0.5:0.75;}
            }
        }
        if(low>=safe&&blocked)return;
        if(ga.data.z==6u&&gb.data.z!=7u&&!initial.valid)
        {
            float advance=dot2(normalized(vertex(a,1u)-vertex(a,0u)),motion);
            if(advance>0)high=clamp(1-full.depth/advance,low,high);
        }
        margin=q.motionMargin.z;
    }
    else
    {
        if(initial.valid)
        {
            if((initial.depth>0.05||dot2(q.motionMargin.xy,initial.normal)<-0.0001)&&oneWayContact(initial))
            {safe=unsafeFraction=0;motionShape=mapA.identity.z;blocked=true;}
            return;
        }
        if(safe==0||ignored)return;
        float fraction;vec2 normal;
        if(!querySweep(a,b,motion,vec2(0),a.radius+b.radius,0.00001,fraction,normal))return;
        vec2 bracket=querySweepBracket(fraction);low=bracket.x;high=bracket.y;
        if(low>=safe)return;
        if(!oneWay){safe=low;unsafeFraction=high;motionShape=mapA.identity.z;blocked=true;return;}
        probe.radius+=q.motionMargin.z;
    }
    probe.pose.xy+=high*motion;
    Contact contact=evaluate(probe,b,ga,gb,vec2(0),margin);
    contact.valid=contact.valid&&contact.depth>=0;
    if(!contact.valid){if(!directed)return;contact=full;}
    if(oneWay&&dot2(contact.normal,passDirection())>=0)return;
    safe=low;unsafeFraction=high;motionShape=mapA.identity.z;blocked=true;
}
