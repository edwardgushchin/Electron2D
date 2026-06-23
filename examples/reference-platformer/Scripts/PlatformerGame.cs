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

namespace Electron2D.ReferencePlatformer.Scripts;

internal sealed class PlatformerGame : Node
{
    private const float Gravity = 2400f;
    private const float MoveSpeed = 170f;
    private const float JumpVelocity = -680f;
    private const double FixedDelta = 1d / 60d;

    private TileMapLayer ground = null!;
    private CharacterBody2D player = null!;
    private Camera2D camera = null!;
    private AnimatedSprite2D animatedSprite = null!;
    private AnimationPlayer animationPlayer = null!;
    private AudioStreamPlayer jumpAudio = null!;
    private AudioStreamPlayer checkpointAudio = null!;
    private Control pauseMenu = null!;
    private bool configured;
    private bool touchMoveRight;
    private bool touchJumpQueued;
    private string checkpointId = "start";
    private int coins;

    public string ProjectRoot { get; init; } = "";

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
                touchMoveRight = touch.Position.X >= 0.5f;
                touchJumpQueued = touch.Position.Y <= 0.4f;
                break;
            case InputEventScreenTouch { Pressed: false }:
                TouchInputUsed = true;
                touchMoveRight = false;
                break;
            case InputEventScreenDrag drag:
                TouchInputUsed = true;
                touchMoveRight = drag.Relative.X > 0f;
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!configured)
        {
            return;
        }

        var vector = Input.GetVector("move_left", "move_right", "jump", "pause");
        var jumpRequested = Input.IsActionJustPressed("jump") || touchJumpQueued;
        var direction = touchMoveRight ? 1f : vector.X;
        StepGameplay(direction, jumpRequested, delta);
        touchJumpQueued = false;
    }

    public PlatformerVerificationResult RunHeadlessVerification(string savePath)
    {
        ConfigureScene();

        var actionNames = InputMap.GetActions();
        InputReady = actionNames.Contains("move_left", StringComparer.Ordinal) &&
            actionNames.Contains("move_right", StringComparer.Ordinal) &&
            actionNames.Contains("jump", StringComparer.Ordinal) &&
            actionNames.Contains("pause", StringComparer.Ordinal);
        GamepadBindingsReady = InputMap.ActionGetEvents("jump").OfType<InputEventJoypadButton>().Any() &&
            InputMap.ActionGetEvents("move_left").OfType<InputEventJoypadButton>().Any() &&
            InputMap.ActionGetEvents("move_right").OfType<InputEventJoypadButton>().Any();

        _Input(new InputEventScreenTouch { Pressed = true, Position = new Vector2(0.82f, 0.25f), Index = 1 });
        _Input(new InputEventScreenDrag { Position = new Vector2(0.9f, 0.24f), Relative = new Vector2(0.08f, 0f), Index = 1 });
        _PhysicsProcess(FixedDelta);

        player.Position = new Vector2(0f, 0f);
        player.Velocity = new Vector2(MoveSpeed, Gravity);
        StepGameplay(direction: 1f, jumpRequested: false, FixedDelta);

        player.Velocity = new Vector2(MoveSpeed, JumpVelocity);
        StepGameplay(direction: 1f, jumpRequested: true, FixedDelta);

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

    private void ConfigureScene()
    {
        if (configured)
        {
            return;
        }

        ground = CreateGround();
        player = CreatePlayer();
        camera = new Camera2D
        {
            Name = "PlayerCamera",
            Position = new Vector2(0f, -24f),
            Zoom = new Vector2(1.5f, 1.5f)
        };
        animatedSprite = CreateAnimatedSprite();
        animationPlayer = new AnimationPlayer { Name = "PlayerTimeline" };
        jumpAudio = new AudioStreamPlayer { Name = "JumpAudio", Stream = new ReferenceToneAudioStream(0.25f) };
        checkpointAudio = new AudioStreamPlayer { Name = "CheckpointAudio", Stream = new ReferenceToneAudioStream(0.5f) };
        pauseMenu = new Control { Name = "PauseMenu", Visible = false };

        AddChild(ground);
        AddChild(player);
        player.AddChild(animatedSprite);
        player.AddChild(animationPlayer);
        player.AddChild(camera);
        AddChild(jumpAudio);
        AddChild(checkpointAudio);
        AddChild(pauseMenu);

        camera.MakeCurrent();
        animatedSprite.Play("idle");
        animationPlayer.AssignedAnimation = "checkpoint_pulse";

        TileMapReady = ground.GetUsedCells().Length > 0;
        CameraReady = camera.IsCurrent();
        AnimationReady = animatedSprite.SpriteFrames is not null && animatedSprite.IsPlaying();
        AudioReady = jumpAudio.Stream is not null && checkpointAudio.Stream is not null;
        configured = true;
    }

    private TileMapLayer CreateGround()
    {
        var tileSet = new TileSet { TileSize = new Vector2I(120, 10) };
        var source = new TileSetAtlasSource
        {
            Texture = new ReferenceTexture2D(120, 10, hasAlpha: false),
            TextureRegionSize = new Vector2I(120, 10)
        };
        source.CreateTile(Vector2I.Zero);
        var tileData = source.GetTileData(Vector2I.Zero)!;
        tileData.SetCollisionPolygonsCount(0, 1);
        tileData.SetCollisionPolygonPoints(
            0,
            0,
            [
                new Vector2(0f, 0f),
                new Vector2(120f, 0f),
                new Vector2(120f, 10f),
                new Vector2(0f, 10f)
            ]);
        tileData.SetCollisionPolygonOneWay(0, 0, true);
        tileData.SetCollisionPolygonOneWayMargin(0, 0, 1f);
        tileSet.AddSource(source, atlasSourceIdOverride: 1);

        var layer = new TileMapLayer
        {
            Name = "PlatformTileMap",
            TileSet = tileSet
        };
        layer.SetCell(new Vector2I(0, 5), sourceId: 1, atlasCoords: Vector2I.Zero);
        OneWayPlatformReady = tileData.IsCollisionPolygonOneWay(0, 0);
        return layer;
    }

    private static CharacterBody2D CreatePlayer()
    {
        var body = new CharacterBody2D
        {
            Name = "Player",
            Position = Vector2.Zero,
            FloorSnapLength = 4f
        };
        body.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(10f, 10f) }
        });
        return body;
    }

    private static AnimatedSprite2D CreateAnimatedSprite()
    {
        var frames = new SpriteFrames();
        frames.AddFrame("default", new ReferenceTexture2D(16, 20, hasAlpha: true));
        frames.SetAnimationSpeed("default", 4f);
        frames.AddAnimation("idle");
        frames.SetAnimationSpeed("idle", 4f);
        frames.AddFrame("idle", new ReferenceTexture2D(16, 20, hasAlpha: true));
        frames.AddAnimation("walk");
        frames.SetAnimationSpeed("walk", 8f);
        frames.AddFrame("walk", new ReferenceTexture2D(16, 20, hasAlpha: true));
        frames.AddFrame("walk", new ReferenceTexture2D(16, 20, hasAlpha: true));

        return new AnimatedSprite2D
        {
            Name = "PlayerSprite",
            SpriteFrames = frames,
            Animation = "idle",
            Autoplay = "idle"
        };
    }

    private void StepGameplay(float direction, bool jumpRequested, double delta)
    {
        if (direction != 0f)
        {
            animatedSprite.Play("walk");
        }

        player.Velocity = new Vector2(direction * MoveSpeed, player.Velocity.Y + Gravity * (float)delta);
        if (jumpRequested)
        {
            player.Velocity = new Vector2(player.Velocity.X, JumpVelocity);
            jumpAudio.Play();
        }

        var collided = player.MoveAndSlide();
        CharacterMoved = CharacterMoved || collided || !player.Position.IsZeroApprox();
        camera.Position = player.Position + new Vector2(0f, -24f);
    }

    private void TogglePauseMenu()
    {
        pauseMenu.Visible = !pauseMenu.Visible;
        PauseMenuUsed = pauseMenu.Visible;
    }

    private void CheckpointReached(string id)
    {
        checkpointId = id;
        checkpointAudio.Play();
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
            format = "Electron2D.ReferencePlatformer.Progress",
            checkpointId,
            coins
        };
        File.WriteAllText(fullPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        SaveProgressUsed = true;
    }

    private sealed class ReferenceToneAudioStream(float length) : AudioStream
    {
        public override float GetLength()
        {
            return length;
        }
    }

    private sealed class ReferenceTexture2D(int width, int height, bool hasAlpha) : Texture2D
    {
        public override int GetWidth()
        {
            return width;
        }

        public override int GetHeight()
        {
            return height;
        }

        public override bool HasAlpha()
        {
            return hasAlpha;
        }
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
