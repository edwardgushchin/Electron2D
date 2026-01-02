using System;
using System.Collections.Generic;
using System.Numerics;
using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    private readonly List<Player> _players = [];
    private readonly List<Box> _boxes = [];

    private Texture _playerTexture;
    private Texture _boxTexture;

    protected override void EnterTree()
    {
        SceneTree?.ClearColor = new Color(47, 47, 56);
    }

    protected override void Ready()
    {
        //AddChild(new DebugCamera());

        _playerTexture = Resources.GetTexture("player_idle.png");
        _boxTexture = Resources.GetTexture("RTS_Crate.png");

        const int playerCount = 500;
        const float spawnRadius = 20f;

        SpawnPlayers(playerCount, spawnRadius, seed: 1684);

        // Если нужны коробки — раскомментируй:
        SpawnBoxes(boxCount: 100, spawnRadius, seed: 777);

        // ВАЖНО: Paused=true “замораживает” Process у большинства нод.
        // Оставь true только если это намеренно.
        SceneTree!.Paused = false;
    }

    private void SpawnPlayers(int count, float radius, int seed)
    {
        _players.Clear();
        if (_players.Capacity < count) _players.Capacity = count;

        var rnd = new Random(seed);

        for (var i = 0; i < count; i++)
        {
            var pos = NextPointInCircle(rnd, radius);

            var player = new Player(_playerTexture);
            _players.Add(player);
            AddChild(player);

            player.Transform.WorldPosition = pos;
        }
    }

    private void SpawnBoxes(int boxCount, float radius, int seed)
    {
        _boxes.Clear();
        if (_boxes.Capacity < boxCount) _boxes.Capacity = boxCount;

        var rnd = new Random(seed);

        for (var i = 0; i < boxCount; i++)
        {
            var pos = NextPointInCircle(rnd, radius);

            var box = new Box(_boxTexture);
            _boxes.Add(box);
            AddChild(box);

            box.Transform.WorldPosition = pos;
        }
    }

    private static Vector2 NextPointInCircle(Random rnd, float radius)
    {
        // Равномерно по площади: r = sqrt(u)*R, theta = 2πv
        var u = (float)rnd.NextDouble();
        var v = (float)rnd.NextDouble();

        var r = MathF.Sqrt(u) * radius;
        var theta = v * (MathF.PI * 2f);

        return new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * r;
    }

    protected override void Process(float delta)
    {
        var f = Profiler.LastFrame;
        if (f.IsValid && (f.FrameIndex % 144) == 0)
        {
            Console.SetCursorPosition(0, 0);
            Console.Write(f.ToPrettyString());
        }
    }

    protected override void HandleUnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is { Type: InputEventType.KeyDown, Code: KeyCode.Escape })
            SceneTree?.Quit();
    }
}
