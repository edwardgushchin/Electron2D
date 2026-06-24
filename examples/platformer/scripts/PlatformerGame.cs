/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Text.Json;
using Electron2D;

namespace Platformer.scripts;

public partial class PlatformerGame : Node2D
{
    private const float Gravity = 2400f;
    private const float MoveSpeed = 170f;
    private const float JumpVelocity = -680f;
    private const int TileCollisionLayer = 0;

    private static readonly string[] RequiredActions =
    [
        "move_left",
        "move_right",
        "jump",
        "pause"
    ];

    private bool configured;
    private bool touchMoveRight;
    private bool touchJumpQueued;
    private string checkpointId = "start";
    private int coins;

    public string ProjectRoot { get; init; } = "";

    [Export]
    public TileMapLayer Ground { get; set; } = null!;

    [Export]
    public CharacterBody2D Player { get; set; } = null!;

    [Export]
    public Camera2D Camera { get; set; } = null!;

    [Export]
    public AnimatedSprite2D PlayerSprite { get; set; } = null!;

    [Export]
    public AnimationPlayer PlayerTimeline { get; set; } = null!;

    [Export]
    public AudioStreamPlayer JumpAudio { get; set; } = null!;

    [Export]
    public AudioStreamPlayer CheckpointAudio { get; set; } = null!;

    [Export]
    public Control PauseMenu { get; set; } = null!;

    [Export]
    public Label StatusLabel { get; set; } = null!;

    [Export]
    public Button ResumeButton { get; set; } = null!;

    public bool TileMapReady { get; private set; }

    public bool OneWayPlatformReady { get; private set; }

    public bool CharacterMoved { get; private set; }

    public bool CameraReady { get; private set; }

    public bool AnimationReady { get; private set; }

    public bool AudioReady { get; private set; }

    public bool InputReady { get; private set; }

    public bool GamepadBindingsReady { get; private set; }

    public bool TouchInputUsed { get; private set; }

    public bool PauseMenuUsed { get; private set; }

    public bool SaveProgressUsed { get; private set; }

    public override void _Ready()
    {
        ConfigureScene();
    }

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventScreenTouch { Pressed: true } touch:
                TouchInputUsed = true;
                var normalized = NormalizeTouchPosition(touch.Position);
                touchMoveRight = normalized.X >= 0.5f;
                touchJumpQueued = normalized.Y <= 0.4f;
                break;
            case InputEventScreenTouch { Pressed: false }:
                TouchInputUsed = true;
                touchMoveRight = false;
                break;
            case InputEventScreenDrag drag:
                TouchInputUsed = true;
                var dragDelta = drag.ScreenRelative.IsZeroApprox() ? drag.Relative : drag.ScreenRelative;
                touchMoveRight = dragDelta.X > 0f;
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!configured)
        {
            return;
        }

        var horizontalDirection = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        var jumpRequested = Input.IsActionJustPressed("jump") || touchJumpQueued;
        if (Input.IsActionJustPressed("pause"))
        {
            TogglePauseMenu();
        }

        var direction = touchMoveRight ? 1f : horizontalDirection;
        if (!PauseMenu.Visible)
        {
            StepGameplay(direction, jumpRequested, delta);
        }

        touchJumpQueued = false;
        UpdateHud();
    }

    internal PlatformerVerificationResult RunHeadlessVerification(string savePath, double delta)
    {
        ConfigureScene();

        InputReady = RequiredActions.All(InputMap.HasAction) &&
            RequiredActions.All(HasKeyboardBinding);
        GamepadBindingsReady = HasGamepadBinding("jump") &&
            HasGamepadBinding("move_left") &&
            HasGamepadBinding("move_right");

        _Input(new InputEventScreenTouch { Pressed = true, Position = new Vector2(1050f, 180f), Index = 1 });
        _Input(new InputEventScreenDrag
        {
            Position = new Vector2(1140f, 175f),
            Relative = new Vector2(90f, -5f),
            ScreenRelative = new Vector2(90f, -5f),
            Index = 1
        });
        _PhysicsProcess(delta);

        Player.Velocity = new Vector2(MoveSpeed, Gravity);
        StepGameplay(direction: 1f, jumpRequested: false, delta);

        Player.Velocity = new Vector2(MoveSpeed, JumpVelocity);
        StepGameplay(direction: 1f, jumpRequested: true, delta);

        TogglePauseMenu();
        CheckpointReached("checkpoint-01");
        coins = 1;
        SaveProgress(savePath);

        return new PlatformerVerificationResult(
            TileMapReady,
            OneWayPlatformReady,
            CharacterMoved,
            CameraReady,
            AnimationReady,
            AudioReady,
            InputReady,
            GamepadBindingsReady,
            TouchInputUsed,
            PauseMenuUsed,
            SaveProgressUsed,
            checkpointId,
            coins,
            savePath);
    }

    internal PlatformerPlayableResult RunPlayableScript(IReadOnlyList<string> commands, string savePath, double delta)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

        ConfigureScene();
        var framesAdvanced = 0;
        var commandsApplied = 0;

        foreach (var rawCommand in commands)
        {
            var command = NormalizePlayableCommand(rawCommand);
            if (command.Length == 0)
            {
                continue;
            }

            commandsApplied++;
            if (command == "quit")
            {
                break;
            }

            ApplyPlayableCommand(command, savePath, delta);
            framesAdvanced++;
        }

        return CreatePlayableResult(framesAdvanced, commandsApplied, savePath);
    }

    private void ConfigureScene()
    {
        if (configured)
        {
            return;
        }

        Camera.MakeCurrent();
        PlayerSprite.Play("idle");
        PlayerTimeline.AssignedAnimation = "checkpoint_pulse";
        PlayerTimeline.Play();
        ResumeButton.Connect("pressed", Callable.From(TogglePauseMenu));

        TileMapReady = Ground.GetUsedCells().Any();
        OneWayPlatformReady = HasOneWayCollision(Ground);
        CameraReady = Camera.IsCurrent();
        AnimationReady = PlayerSprite.SpriteFrames is not null &&
            PlayerSprite.IsPlaying() &&
            !PlayerTimeline.AssignedAnimation.IsEmpty();
        AudioReady = JumpAudio.Stream is not null && CheckpointAudio.Stream is not null;
        UpdateHud();
        configured = true;
    }

    private void StepGameplay(float direction, bool jumpRequested, double delta)
    {
        if (direction != 0f)
        {
            PlayerSprite.Play("walk");
        }

        Player.Velocity = new Vector2(direction * MoveSpeed, Player.Velocity.Y + Gravity * (float)delta);
        if (jumpRequested && Player.IsOnFloor())
        {
            Player.Velocity = new Vector2(Player.Velocity.X, JumpVelocity);
            JumpAudio.Play();
        }

        var collided = Player.MoveAndSlide();

        CharacterMoved = CharacterMoved || collided || !Player.Position.IsZeroApprox();
        Camera.Position = new Vector2(0f, -24f);
        if (Player.Position.X >= 5f && checkpointId == "start")
        {
            CheckpointReached("checkpoint-01");
            coins = Math.Max(coins, 1);
        }

        UpdateHud();
    }

    private void TogglePauseMenu()
    {
        var tree = GetTree();
        var paused = tree is null ? !PauseMenu.Visible : !GetTree()!.Paused;
        if (tree is not null)
        {
            tree.Paused = paused;
        }

        PauseMenu.Visible = paused;
        PauseMenuUsed |= paused;
        UpdateHud();
    }

    private void CheckpointReached(string id)
    {
        checkpointId = id;
        CheckpointAudio.Play();
        UpdateHud();
    }

    private void SaveProgress(string savePath)
    {
        var fullPath = Path.GetFullPath(savePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var payload = new
        {
            format = "Platformer.Progress",
            checkpointId,
            coins
        };
        File.WriteAllText(fullPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        SaveProgressUsed = SavedProgressMatches(fullPath, checkpointId, coins);
        UpdateHud();
    }

    private static bool HasKeyboardBinding(string action)
    {
        return InputMap.ActionGetEvents(action).OfType<InputEventKey>().Any();
    }

    private static bool HasGamepadBinding(string action)
    {
        return InputMap.ActionGetEvents(action).Any(static inputEvent =>
            inputEvent is InputEventJoypadButton or InputEventJoypadMotion);
    }

    private static bool HasOneWayCollision(TileMapLayer layer)
    {
        if (layer.TileSet is null)
        {
            return false;
        }

        foreach (var coords in layer.GetUsedCells())
        {
            var tileData = layer.GetCellTileData(coords);
            if (tileData is null)
            {
                continue;
            }

            var polygonsCount = tileData.GetCollisionPolygonsCount(TileCollisionLayer);
            for (var polygon = 0; polygon < polygonsCount; polygon++)
            {
                if (tileData.IsCollisionPolygonOneWay(TileCollisionLayer, polygon))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Vector2 NormalizeTouchPosition(Vector2 position)
    {
        if (position.X is >= 0f and <= 1f && position.Y is >= 0f and <= 1f)
        {
            return position;
        }

        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1280f, 720f);
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
        {
            return Vector2.Zero;
        }

        return new Vector2(
            Mathf.Clamp(position.X / viewportSize.X, 0f, 1f),
            Mathf.Clamp(position.Y / viewportSize.Y, 0f, 1f));
    }

    private static bool SavedProgressMatches(string fullPath, string expectedCheckpointId, int expectedCoins)
    {
        using var saved = JsonDocument.Parse(File.ReadAllText(fullPath));
        var root = saved.RootElement;
        return root.TryGetProperty("format", out var formatElement) &&
            string.Equals(formatElement.GetString(), "Platformer.Progress", StringComparison.Ordinal) &&
            root.TryGetProperty("checkpointId", out var checkpointElement) &&
            string.Equals(checkpointElement.GetString(), expectedCheckpointId, StringComparison.Ordinal) &&
            root.TryGetProperty("coins", out var coinsElement) &&
            coinsElement.GetInt32() == expectedCoins;
    }

    private void UpdateHud()
    {
        if (StatusLabel is null)
        {
            return;
        }

        StatusLabel.Text = $"{checkpointId.ToUpperInvariant()}  COINS {coins}  X {Player.Position.X:0}";
    }

    private void ApplyPlayableCommand(string command, string savePath, double delta)
    {
        switch (command)
        {
            case "left":
            case "a":
                StepGameplay(direction: -1f, jumpRequested: false, delta);
                break;
            case "right":
            case "d":
                StepGameplay(direction: 1f, jumpRequested: false, delta);
                break;
            case "jump":
            case "space":
                StepGameplay(direction: 1f, jumpRequested: true, delta);
                break;
            case "pause":
            case "p":
                TogglePauseMenu();
                break;
            case "save":
            case "s":
                SaveProgress(savePath);
                break;
            default:
                StepGameplay(direction: 0f, jumpRequested: false, delta);
                break;
        }
    }

    private PlatformerPlayableResult CreatePlayableResult(int framesAdvanced, int commandsApplied, string savePath)
    {
        if (!SaveProgressUsed)
        {
            SaveProgress(savePath);
        }

        return new PlatformerPlayableResult(
            Playable: framesAdvanced > 0 && commandsApplied > 0 && (CharacterMoved || PauseMenuUsed || SaveProgressUsed),
            framesAdvanced,
            commandsApplied,
            Player.Position,
            checkpointId,
            coins,
            GetTree()?.Paused ?? PauseMenu.Visible,
            savePath);
    }

    private static string NormalizePlayableCommand(string command)
    {
        return command.Trim().ToLowerInvariant();
    }

}

internal sealed record PlatformerVerificationResult(
    bool TileMap,
    bool OneWayPlatform,
    bool CharacterBody,
    bool Camera,
    bool Animation,
    bool Audio,
    bool Input,
    bool Gamepad,
    bool Touch,
    bool Pause,
    bool Save,
    string CheckpointId,
    int Coins,
    string SavePath)
{
    public bool AllPassed => TileMap &&
        OneWayPlatform &&
        CharacterBody &&
        Camera &&
        Animation &&
        Audio &&
        Input &&
        Gamepad &&
        Touch &&
        Pause &&
        Save;

    public string ToSubsystemSummary()
    {
        return string.Join(
            ',',
            [
                $"tilemap={TileMap}",
                $"oneWay={OneWayPlatform}",
                $"character={CharacterBody}",
                $"camera={Camera}",
                $"animation={Animation}",
                $"audio={Audio}",
                $"keyboard={Input}",
                $"gamepad={Gamepad}",
                $"touch={Touch}",
                $"pause={Pause}",
                $"save={Save}"
            ]);
    }
}

internal sealed record PlatformerPlayableResult(
    bool Playable,
    int FramesAdvanced,
    int CommandsApplied,
    Vector2 PlayerPosition,
    string CheckpointId,
    int Coins,
    bool Paused,
    string SavePath)
{
    public string FormatPlayerPosition()
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{PlayerPosition.X:0.##},{PlayerPosition.Y:0.##}");
    }
}
