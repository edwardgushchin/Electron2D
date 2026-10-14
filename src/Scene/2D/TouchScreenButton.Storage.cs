namespace Electron2D;

public partial class TouchScreenButton
{
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TouchScreenButton) ? CreateTouchScreenButton : base.CreateSceneInstanceFactory();
    private static Node CreateTouchScreenButton() => new TouchScreenButton();
    private static readonly PropertyDescriptor[] TouchProperties =
    [
        new PropertyDescriptor<TouchScreenButton,string>(nameof(Action), n=>n.Action,(n,v)=>n.Action=v,_=>"",stored:true),
        new PropertyDescriptor<TouchScreenButton,Texture?>(nameof(TextureNormal),n=>n.TextureNormal,(n,v)=>n.TextureNormal=v,_=>null,stored:true),
        new PropertyDescriptor<TouchScreenButton,Texture?>(nameof(TexturePressed),n=>n.TexturePressed,(n,v)=>n.TexturePressed=v,_=>null,stored:true),
        new PropertyDescriptor<TouchScreenButton,BitMap?>(nameof(Bitmask),n=>n.Bitmask,(n,v)=>n.Bitmask=v,_=>null,stored:true),
        new PropertyDescriptor<TouchScreenButton,Shape?>(nameof(Shape),n=>n.Shape,(n,v)=>n.Shape=v,_=>null,stored:true),
        new PropertyDescriptor<TouchScreenButton,bool>(nameof(ShapeCentered),n=>n.ShapeCentered,(n,v)=>n.ShapeCentered=v,_=>true,stored:true),
        new PropertyDescriptor<TouchScreenButton,bool>(nameof(ShapeVisible),n=>n.ShapeVisible,(n,v)=>n.ShapeVisible=v,_=>true,stored:true),
        new PropertyDescriptor<TouchScreenButton,bool>(nameof(PassbyPress),n=>n.PassbyPress,(n,v)=>n.PassbyPress=v,_=>false,stored:true),
        new PropertyDescriptor<TouchScreenButton,TouchButtonVisibility>(nameof(VisibilityMode),n=>n.VisibilityMode,(n,v)=>n.VisibilityMode=v,_=>TouchButtonVisibility.Always,stored:true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(TouchProperties);
}
