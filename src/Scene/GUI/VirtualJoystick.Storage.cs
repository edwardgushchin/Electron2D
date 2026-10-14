namespace Electron2D;

public partial class VirtualJoystick
{
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VirtualJoystick) ? CreateVirtualJoystick : base.CreateSceneInstanceFactory();
    private static Node CreateVirtualJoystick() => new VirtualJoystick();
    private static readonly PropertyDescriptor[] JoystickProperties =
    [
        new PropertyDescriptor<VirtualJoystick,VirtualJoystickMode>(nameof(JoystickMode),n=>n.JoystickMode,(n,v)=>n.JoystickMode=v,_=>VirtualJoystickMode.Fixed,stored:true),
        new PropertyDescriptor<VirtualJoystick,float>(nameof(JoystickSize),n=>n.JoystickSize,(n,v)=>n.JoystickSize=v,_=>100,stored:true),
        new PropertyDescriptor<VirtualJoystick,float>(nameof(TipSize),n=>n.TipSize,(n,v)=>n.TipSize=v,_=>50,stored:true),
        new PropertyDescriptor<VirtualJoystick,float>(nameof(DeadzoneRatio),n=>n.DeadzoneRatio,(n,v)=>n.DeadzoneRatio=v,_=>0,stored:true),
        new PropertyDescriptor<VirtualJoystick,float>(nameof(ClampzoneRatio),n=>n.ClampzoneRatio,(n,v)=>n.ClampzoneRatio=v,_=>1,stored:true),
        new PropertyDescriptor<VirtualJoystick,Vector2>(nameof(InitialOffsetRatio),n=>n.InitialOffsetRatio,(n,v)=>n.InitialOffsetRatio=v,_=>new(.5f,.5f),stored:true),
        new PropertyDescriptor<VirtualJoystick,JoystickVisibility>(nameof(VisibilityMode),n=>n.VisibilityMode,(n,v)=>n.VisibilityMode=v,_=>JoystickVisibility.Always,stored:true),
        new PropertyDescriptor<VirtualJoystick,string>(nameof(ActionLeft),n=>n.ActionLeft,(n,v)=>n.ActionLeft=v,_=>"ui_left",stored:true),
        new PropertyDescriptor<VirtualJoystick,string>(nameof(ActionRight),n=>n.ActionRight,(n,v)=>n.ActionRight=v,_=>"ui_right",stored:true),
        new PropertyDescriptor<VirtualJoystick,string>(nameof(ActionUp),n=>n.ActionUp,(n,v)=>n.ActionUp=v,_=>"ui_up",stored:true),
        new PropertyDescriptor<VirtualJoystick,string>(nameof(ActionDown),n=>n.ActionDown,(n,v)=>n.ActionDown=v,_=>"ui_down",stored:true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(JoystickProperties);
}
