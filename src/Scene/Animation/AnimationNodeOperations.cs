namespace Electron2D;

/// <summary>Combines 2 graph inputs with a per-tree typed blend_amount parameter.</summary>
public sealed class AnimationNodeBlend2 : AnimationNodeSync
{
    /// <summary>The finite signed contribution parameter, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> BlendAmount = new("blend_amount", 0);
    /// <summary>Creates the standard input captions.</summary>
    public AnimationNodeBlend2() { AddInput("in"); AddInput("blend"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(BlendAmount);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Blend2";
    /// <inheritdoc />
    protected override bool OnHasFilter() => true;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { var amount = GetParameter(BlendAmount); var a = BlendInput(0, time, seek, isExternalSeeking, 1 - amount, FilterAction.Blend, Sync, testOnly); var ai = Current().Result; var b = BlendInput(1, time, seek, isExternalSeeking, amount, FilterAction.Pass, Sync, testOnly); if (amount <= .5) Current().Result = ai; return amount > .5 ? b : a; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeBlend2();
}
/// <summary>Combines 3 graph inputs with a per-tree typed blend_amount parameter.</summary>
public sealed class AnimationNodeBlend3 : AnimationNodeSync
{
    /// <summary>The finite signed contribution parameter, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> BlendAmount = new("blend_amount", 0);
    /// <summary>Creates the standard input captions.</summary>
    public AnimationNodeBlend3() { AddInput("-blend"); AddInput("in"); AddInput("+blend"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(BlendAmount);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Blend3";
    /// <inheritdoc />
    protected override bool OnHasFilter() => false;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { var amount = GetParameter(BlendAmount); var a = BlendInput(0, time, seek, isExternalSeeking, Math.Max(0, -amount), sync: Sync, testOnly: testOnly); var ai = Current().Result; var b = BlendInput(1, time, seek, isExternalSeeking, 1 - Math.Abs(amount), sync: Sync, testOnly: testOnly); var bi = Current().Result; var c = BlendInput(2, time, seek, isExternalSeeking, Math.Max(0, amount), sync: Sync, testOnly: testOnly); if (amount < -.5) Current().Result = ai; else if (amount <= .5) Current().Result = bi; return amount > .5 ? c : amount < -.5 ? a : b; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeBlend3();
}
/// <summary>Combines 2 graph inputs with a per-tree typed add_amount parameter.</summary>
public sealed class AnimationNodeAdd2 : AnimationNodeSync
{
    /// <summary>The finite signed contribution parameter, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> AddAmount = new("add_amount", 0);
    /// <summary>Creates the standard input captions.</summary>
    public AnimationNodeAdd2() { AddInput("in"); AddInput("add"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(AddAmount);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Add2";
    /// <inheritdoc />
    protected override bool OnHasFilter() => true;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { var result = BlendInput(0, time, seek, isExternalSeeking, 1, FilterAction.Ignore, Sync, testOnly); var info = Current().Result; BlendInput(1, time, seek, isExternalSeeking, GetParameter(AddAmount), FilterAction.Pass, Sync, testOnly); Current().Result = info; return result; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeAdd2();
}
/// <summary>Combines 3 graph inputs with a per-tree typed add_amount parameter.</summary>
public sealed class AnimationNodeAdd3 : AnimationNodeSync
{
    /// <summary>The finite signed contribution parameter, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> AddAmount = new("add_amount", 0);
    /// <summary>Creates the standard input captions.</summary>
    public AnimationNodeAdd3() { AddInput("-add"); AddInput("in"); AddInput("+add"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(AddAmount);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Add3";
    /// <inheritdoc />
    protected override bool OnHasFilter() => true;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { var amount = GetParameter(AddAmount); BlendInput(0, time, seek, isExternalSeeking, Math.Max(0, -amount), FilterAction.Pass, Sync, testOnly); var result = BlendInput(1, time, seek, isExternalSeeking, 1, FilterAction.Ignore, Sync, testOnly); var info = Current().Result; BlendInput(2, time, seek, isExternalSeeking, Math.Max(0, amount), FilterAction.Pass, Sync, testOnly); Current().Result = info; return result; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeAdd3();
}
/// <summary>Combines 2 graph inputs with a per-tree typed sub_amount parameter.</summary>
public sealed class AnimationNodeSub2 : AnimationNodeSync
{
    /// <summary>The finite signed contribution parameter, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> SubAmount = new("sub_amount", 0);
    /// <summary>Creates the standard input captions.</summary>
    public AnimationNodeSub2() { AddInput("in"); AddInput("sub"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(SubAmount);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Sub2";
    /// <inheritdoc />
    protected override bool OnHasFilter() => true;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { BlendInput(1, time, seek, isExternalSeeking, -GetParameter(SubAmount), FilterAction.Pass, Sync, testOnly); return BlendInput(0, time, seek, isExternalSeeking, 1, FilterAction.Ignore, Sync, testOnly); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeSub2();
}
/// <summary>Scales the relative time supplied to one graph input.</summary>
public sealed class AnimationNodeTimeScale : AnimationNode
{
    /// <summary>The finite signed time multiplier, initially one.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> Scale = new("scale", 1);
    /// <summary>Creates one input named in.</summary>
    public AnimationNodeTimeScale() { AddInput("in"); }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(Scale);
    /// <inheritdoc />
    protected override string OnGetCaption() => "TimeScale";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) => BlendInput(0, seek ? time : time * GetParameter(Scale), seek, isExternalSeeking, 1, testOnly: testOnly);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeTimeScale();
}
/// <summary>Consumes a per-tree absolute seek request before evaluating one graph input.</summary>
public sealed class AnimationNodeTimeSeek : AnimationNode
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationNodeTimeSeek, bool>(nameof(ExplicitElapse), n => n.ExplicitElapse, (n, v) => n.ExplicitElapse = v, _ => true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GraphProperties);

    /// <summary>The requested absolute seconds; negative means no request.</summary>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public static readonly AnimationParameter<double> SeekRequest = new("seek_request", -1);
    private bool _explicitElapse = true;
    /// <summary>Creates one input named in.</summary>
    public AnimationNodeTimeSeek() { AddInput("in"); }
    /// <summary>Gets or sets whether a request is treated as isExternalSeeking seeking; initially true.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool ExplicitElapse { get { ThrowIfDisposed(); return _explicitElapse; } set { ThrowIfDisposed(); _explicitElapse = value; EmitGraphChanged(); } }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(SeekRequest);
    /// <inheritdoc />
    protected override string OnGetCaption() => "TimeSeek";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly) { var request = GetParameter(SeekRequest); if (request >= 0) { time = request; seek = true; isExternalSeeking = _explicitElapse; SetParameter(SeekRequest, -1d); } return BlendInput(0, time, seek, isExternalSeeking, 1, testOnly: testOnly); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeTimeSeek();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { CopyNodeState((AnimationNode)target); ((AnimationNodeTimeSeek)target)._explicitElapse = _explicitElapse; }
}
