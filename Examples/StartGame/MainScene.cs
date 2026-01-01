using System.Numerics;
using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    private readonly List<Player> players = [];

    protected override void EnterTree()
    {
        SceneTree?.ClearColor = new Color(47, 47, 56);
    }

    protected override void Ready()
    {
        AddChild(new DebugCamera());

        // Сколько игроков и какие ограничения
        const int count = 500;             // поменяй как надо
        const float radius = 20;          // в пределах 20 юнитов от (0,0)

        SpawnPlayers(count, radius, seed: 1684);

        SceneTree!.Paused = true;
    }

    private void SpawnPlayers(int count, float radius, int seed)
    {
        players.Clear();

        var rnd = new Random(seed);

        // На таких числах простой rejection работает нормально.
        // Если count слишком большой — будет больше попыток; лимит защитит от вечного цикла.
        var maxAttempts = count * 2000;

        var attempts = 0;
        while (players.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            //Console.WriteLine($"SpawnPlayers: {players.Count} is {count}");

            // Равномерно по площади круга: r = sqrt(u)*R, theta = 2πv
            var u = (float)rnd.NextDouble();
            var v = (float)rnd.NextDouble();

            var r = MathF.Sqrt(u) * radius;
            var theta = v * (MathF.PI * 2f);

            var pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * r;

            // Проверка минимальной дистанции до уже размещённых
           /* var ok = true;
            for (var i = 0; i < players.Count; i++)
            {
                var p = players[i];
                var dp = p.Transform.WorldPosition - pos;
                if (dp.LengthSquared() < minDist2)
                {
                    ok = false;
                    break;
                }
            }*/

            //if (!ok) continue;

            var player = new Player();
            players.Add(player);
            AddChild(player);

            // Поставить в мир
            player.Transform.WorldPosition = pos;
        }

        if (players.Count < count)
            Console.WriteLine($"SpawnPlayers: placed {players.Count}/{count} (attempts={attempts}). Try lower count or minDistance.");
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
