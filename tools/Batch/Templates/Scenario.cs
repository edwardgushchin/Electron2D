using Electron2D;
using Electron2D.Tools.Batch;

return BatchHost.Run(args, static () => new Scenario());

internal sealed class Scenario : BatchScenario
{
    protected override void Configure(BatchContext context)
    {
        ResourceFileTypes.RegisterNode("sample.Actor", Actor.Create);
        InputMap.AddAction("move");
        ProjectSettings.Set(ProjectSettings.DefaultClearColor, Colors.Black);
    }
    protected override Node CreateScene(BatchContext context)
    {
        var root = new Node { Name = "Main" };
        var actor = new Actor { Name = "Actor", Position = new(8, 8) };
        root.AddChild(actor); actor.Owner = root;
        return root;
    }
    protected override void Author(BatchContext context)
    {
        var actor = context.Root.GetNode<Actor>("Actor");
        context.Check("detached-authoring", !context.Root.IsInsideTree && actor.ReadyCalls == 0 && actor.PhysicsSteps == 0, "Authoring must not activate gameplay.");
    }
    protected override void AfterStep(BatchContext context)
    {
        var actor = context.Root.GetNode<Actor>("Actor");
        context.Observe("position-x", actor.Position.X);
        context.Observe("physics-steps", actor.PhysicsSteps);
        if (context.Step != 6) return;
        if (context.Mode == BatchMode.Rendered)
        {
            using var pixels = context.Root.GetViewport()!.GetTexture().GetImage()!;
            var color = pixels.GetPixel(12, 10);
            context.Check("rendered-output", pixels.Width == context.Width && pixels.Height == context.Height && color.G > .6f && color.R < .4f, "The actual completed canvas must contain the authored rectangle.");
        }
        context.Check("lifecycle", actor.ReadyCalls == 1 && actor.PhysicsSteps == 6, "The ordinary ready and physics lifecycle must execute exactly once per requested step.");
        context.Check("input-state-and-delivery", actor.InputCalls == 2 && actor.PressedEdges == 1, "The press and release must commit input state and reach the scene before their physics steps.");
        context.Check("movement", MathF.Abs(actor.Position.X - (8 + 3 * actor.Speed / context.FPS)) < .001f, "The three pressed physics steps must move the actor by their fixed duration.");
        context.Observe("seeded-value", context.Random.Randf());
        context.Complete();
    }
}

internal sealed class Actor : Entity
{
    internal static Actor Create() => new();
    private static readonly PropertyDescriptor[] Properties =
    [new PropertyDescriptor<Actor, float>(nameof(Speed), n => n.Speed, (n, v) => n.Speed = v, _ => DefaultSpeed, stored: true)];
    private const float DefaultSpeed = 30;
    public float Speed { get; set; } = DefaultSpeed;
    internal int ReadyCalls, PhysicsSteps, InputCalls, PressedEdges;
    protected override Func<Node> CreateSceneInstanceFactory() => Create;
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    protected override void OnReady() => ReadyCalls++;
    protected override void OnInput(InputEvent input)
    {
        if (input is InputEventAction { Action: "move" }) InputCalls++;
    }
    protected override void OnPhysicsProcess(double delta)
    {
        PhysicsSteps++;
        if (Input.IsActionJustPressed("move")) PressedEdges++;
        if (Input.IsActionPressed("move")) Position += new Vector2(Speed * (float)delta, 0);
    }
    protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.LimeGreen);
}
