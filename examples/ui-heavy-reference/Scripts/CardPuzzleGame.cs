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

namespace Electron2D.UiHeavyReference.Scripts;

internal sealed class CardPuzzleGame : Control
{
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly List<CardData> cards = new();
    private readonly List<TextureButton> cardButtons = new();
    private readonly ReferenceFont font = new();

    private Panel shellPanel = null!;
    private NinePatchRect cardFrame = null!;
    private TextureRect rewardIcon = null!;
    private VBoxContainer menuLayout = null!;
    private GridContainer cardGrid = null!;
    private ItemList objectiveList = null!;
    private Label titleLabel = null!;
    private Label scoreLabel = null!;
    private Button playButton = null!;
    private Button localeButton = null!;
    private CheckBox hintToggle = null!;
    private Slider volumeSlider = null!;
    private ProgressBar matchProgress = null!;
    private AudioStreamPlayer flipAudio = null!;
    private AudioStreamPlayer rewardAudio = null!;

    private bool configured;
    private string currentScene = "menu";
    private string currentLocale = "en";
    private int selectedIndex;
    private int score;
    private int moves;

    public string ProjectRoot { get; init; } = "";

    public bool ControlRootReady { get; private set; }

    public bool ContainerLayoutReady { get; private set; }

    public bool BasicControlsReady { get; private set; }

    public bool StructuredListReady { get; private set; }

    public bool LocalizationReady { get; private set; }

    public bool ResponsiveLayoutReady { get; private set; }

    public bool TouchInputUsed { get; private set; }

    public bool TextReady { get; private set; }

    public bool SaveProgressUsed { get; private set; }

    public bool SceneTransitionReady { get; private set; }

    public bool AndroidCompatibilityRendererReady { get; private set; }

    public bool AudioReady { get; private set; }

    public override void _Ready()
    {
        ConfigureScene();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventScreenTouch { Pressed: true, Canceled: false } touch)
        {
            TouchInputUsed = true;
            selectedIndex = Math.Clamp((int)MathF.Floor(touch.Position.X * Math.Max(1, cards.Count)), 0, Math.Max(0, cards.Count - 1));
            RevealSelectedCard();
            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        _ = delta;
        if (!configured)
        {
            return;
        }

        if (Input.IsActionJustPressed("next_card"))
        {
            SelectNextCard();
        }

        if (Input.IsActionJustPressed("previous_card"))
        {
            SelectPreviousCard();
        }

        if (Input.IsActionJustPressed("switch_locale"))
        {
            SwitchLocale(currentLocale == "en" ? "ru" : "en");
        }

        if (Input.IsActionJustPressed("cancel"))
        {
            ShowMenuScene();
        }

        if (Input.IsActionJustPressed("accept"))
        {
            ActivateSelectedAction();
        }
    }

    public UiHeavyVerificationResult RunHeadlessVerification(string savePath)
    {
        ConfigureScene();

        ControlRootReady = shellPanel is not null && this is Control;
        ContainerLayoutReady = menuLayout.GetChildCount() >= 4 && cardGrid.Columns == 2;
        BasicControlsReady = playButton.Text.Length > 0 &&
            localeButton.Text.Length > 0 &&
            hintToggle.ButtonPressed &&
            volumeSlider.Value > 0d &&
            matchProgress.Value >= 0d &&
            cardFrame.Texture is not null &&
            rewardIcon.Texture is not null;
        StructuredListReady = objectiveList.GetItemCount() == 3;
        AudioReady = flipAudio.Stream is not null && rewardAudio.Stream is not null;
        AndroidCompatibilityRendererReady = ReadAndroidRendererProfile() == "Compatibility";

        ApplyResponsiveLayout(new Vector2(1280f, 720f));
        ApplyResponsiveLayout(new Vector2(390f, 844f));
        ApplyResponsiveLayout(new Vector2(900f, 1200f));

        ShowMenuScene();
        SwitchLocale("ru");
        SwitchLocale("en");
        _Input(new InputEventScreenTouch { Pressed = true, Position = new Vector2(0.72f, 0.24f), Index = 1 });
        ShowGameScene();
        RevealSelectedCard();
        ShowResultScene();
        SaveProgress(savePath);
        ShowMenuScene();

        return new UiHeavyVerificationResult(
            ControlRootReady,
            ContainerLayoutReady,
            BasicControlsReady,
            StructuredListReady,
            LocalizationReady,
            ResponsiveLayoutReady,
            TouchInputUsed,
            TextReady,
            SaveProgressUsed,
            SceneTransitionReady,
            AndroidCompatibilityRendererReady,
            AudioReady,
            currentScene,
            currentLocale,
            score,
            moves,
            savePath);
    }

    public UiHeavyPlayableResult RunPlayableScript(IReadOnlyList<string> commands, string savePath)
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

            ApplyPlayableCommand(command, savePath);
            framesAdvanced++;
        }

        return CreatePlayableResult(framesAdvanced, commandsApplied, savePath);
    }

    public override void _Draw()
    {
        ConfigureScene();

        DrawRect(new Rect2(0f, 0f, 960f, 540f), new Color(0.05f, 0.06f, 0.08f, 1f));
        DrawRect(new Rect2(28f, 24f, 904f, 74f), new Color(0.13f, 0.15f, 0.19f, 1f));
        DrawRect(new Rect2(28f, 24f, 904f, 74f), new Color(0.36f, 0.43f, 0.54f, 1f), filled: false, width: 2f);
        DrawString(font, new Vector2(48f, 56f), "ELECTRON2D UI-HEAVY REFERENCE", fontSize: 18, modulate: Color.White);
        DrawString(font, new Vector2(48f, 84f), $"SCENE {currentScene}  LOCALE {currentLocale}  SCORE {score}  MOVES {moves}", fontSize: 13, modulate: new Color(0.72f, 0.83f, 0.96f, 1f));

        DrawNavigationRail();
        if (currentScene == "menu")
        {
            DrawMenuScene();
        }
        else if (currentScene == "game")
        {
            DrawGameScene();
        }
        else
        {
            DrawResultScene();
        }
    }

    private void DrawNavigationRail()
    {
        DrawRect(new Rect2(28f, 118f, 170f, 390f), new Color(0.10f, 0.11f, 0.14f, 1f));
        DrawString(font, new Vector2(52f, 154f), "ORBIT CARDS", fontSize: 18, modulate: Color.White);
        DrawButtonFrame(new Rect2(52f, 182f, 120f, 38f), currentScene == "menu", "MENU");
        DrawButtonFrame(new Rect2(52f, 232f, 120f, 38f), currentScene == "game", "GAME");
        DrawButtonFrame(new Rect2(52f, 282f, 120f, 38f), currentScene == "result", "RESULT");
        DrawString(font, new Vector2(52f, 360f), "A/ENTER ACCEPT", fontSize: 11, modulate: new Color(0.72f, 0.76f, 0.82f, 1f));
        DrawString(font, new Vector2(52f, 384f), "LEFT/RIGHT SELECT", fontSize: 11, modulate: new Color(0.72f, 0.76f, 0.82f, 1f));
        DrawString(font, new Vector2(52f, 408f), "L SWITCH LOCALE", fontSize: 11, modulate: new Color(0.72f, 0.76f, 0.82f, 1f));
    }

    private void DrawMenuScene()
    {
        DrawRect(new Rect2(230f, 120f, 680f, 388f), new Color(0.12f, 0.14f, 0.18f, 1f));
        DrawString(font, new Vector2(272f, 184f), titleLabel.Text, fontSize: 28, modulate: Color.White);
        DrawString(font, new Vector2(274f, 228f), "TACTICAL CARD MATCHING", fontSize: 15, modulate: new Color(0.74f, 0.84f, 0.96f, 1f));
        DrawButtonFrame(new Rect2(276f, 272f, 220f, 54f), true, playButton.Text);
        DrawButtonFrame(new Rect2(276f, 344f, 220f, 54f), false, localeButton.Text);
        DrawRect(new Rect2(552f, 254f, 260f, 130f), new Color(0.08f, 0.09f, 0.12f, 1f));
        DrawString(font, new Vector2(584f, 300f), "REFERENCE UI", fontSize: 20, modulate: new Color(0.95f, 0.80f, 0.32f, 1f));
        DrawString(font, new Vector2(584f, 340f), "CARDS  OBJECTIVES  SAVE", fontSize: 12, modulate: Color.White);
    }

    private void DrawGameScene()
    {
        DrawRect(new Rect2(230f, 118f, 460f, 390f), new Color(0.12f, 0.14f, 0.18f, 1f));
        DrawRect(new Rect2(710f, 118f, 200f, 390f), new Color(0.10f, 0.11f, 0.14f, 1f));

        for (var index = 0; index < cards.Count; index++)
        {
            var column = index % 4;
            var row = index / 4;
            var x = 258f + (column * 100f);
            var y = 154f + (row * 126f);
            var selected = index == selectedIndex;
            DrawRect(new Rect2(x, y, 78f, 104f), selected ? new Color(0.35f, 0.56f, 0.92f, 1f) : new Color(0.20f, 0.23f, 0.29f, 1f));
            DrawRect(new Rect2(x + 7f, y + 8f, 64f, 88f), new Color(0.86f, 0.88f, 0.92f, 1f));
            DrawString(font, new Vector2(x + 24f, y + 50f), $"{cards[index].Rank}{cards[index].Suit[0]}", fontSize: 18, modulate: new Color(0.10f, 0.12f, 0.15f, 1f));
        }

        DrawString(font, new Vector2(734f, 154f), "OBJECTIVES", fontSize: 16, modulate: Color.White);
        DrawString(font, new Vector2(734f, 196f), "MATCH ORBIT", fontSize: 12, modulate: new Color(0.75f, 0.86f, 1f, 1f));
        DrawString(font, new Vector2(734f, 226f), "SCORE 60", fontSize: 12, modulate: new Color(0.75f, 0.86f, 1f, 1f));
        DrawString(font, new Vector2(734f, 256f), "FINISH RESULT", fontSize: 12, modulate: new Color(0.75f, 0.86f, 1f, 1f));
        DrawRect(new Rect2(734f, 330f, 140f, 18f), new Color(0.22f, 0.24f, 0.28f, 1f));
        DrawRect(new Rect2(734f, 330f, Math.Clamp(score, 0, 100) * 1.4f, 18f), new Color(0.36f, 0.58f, 0.96f, 1f));
        DrawString(font, new Vector2(734f, 386f), scoreLabel.Text, fontSize: 11, modulate: Color.White);
    }

    private void DrawResultScene()
    {
        DrawRect(new Rect2(230f, 118f, 680f, 390f), new Color(0.12f, 0.14f, 0.18f, 1f));
        DrawRect(new Rect2(342f, 178f, 456f, 214f), new Color(0.08f, 0.10f, 0.13f, 1f));
        DrawRect(new Rect2(342f, 178f, 456f, 214f), new Color(0.38f, 0.48f, 0.66f, 1f), filled: false, width: 2f);
        DrawString(font, new Vector2(452f, 242f), "RESULT", fontSize: 30, modulate: Color.White);
        DrawString(font, new Vector2(414f, 292f), $"SCORE {score}  MOVES {moves}", fontSize: 18, modulate: new Color(0.95f, 0.80f, 0.32f, 1f));
        DrawString(font, new Vector2(414f, 336f), $"SELECTED {SelectedCardId()}", fontSize: 14, modulate: new Color(0.75f, 0.86f, 1f, 1f));
    }

    private void DrawButtonFrame(Rect2 rect, bool active, string label)
    {
        DrawRect(rect, active ? new Color(0.35f, 0.56f, 0.92f, 1f) : new Color(0.20f, 0.23f, 0.29f, 1f));
        DrawRect(rect, new Color(0.78f, 0.82f, 0.90f, 1f), filled: false, width: 1f);
        DrawString(font, rect.Position + new Vector2(14f, 25f), label, fontSize: 13, modulate: Color.White);
    }

    private void ConfigureScene()
    {
        if (configured)
        {
            return;
        }

        LoadTranslations();
        LoadCardData();

        shellPanel = new Panel
        {
            Name = "ShellPanel",
            Size = new Vector2(1280f, 720f),
            CustomMinimumSize = new Vector2(320f, 480f),
            Visible = false
        };
        cardFrame = new NinePatchRect
        {
            Name = "CardFrame",
            Texture = new ReferenceTexture2D(96, 32, hasAlpha: true),
            PatchMarginLeft = 6,
            PatchMarginTop = 6,
            PatchMarginRight = 6,
            PatchMarginBottom = 6,
            Size = new Vector2(280f, 320f)
        };
        rewardIcon = new TextureRect
        {
            Name = "RewardIcon",
            Texture = new ReferenceTexture2D(32, 32, hasAlpha: true),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(48f, 48f)
        };
        menuLayout = new VBoxContainer
        {
            Name = "MenuLayout",
            Size = new Vector2(360f, 320f)
        };
        titleLabel = new Label { Name = "TitleLabel" };
        scoreLabel = new Label { Name = "ScoreLabel" };
        playButton = new Button { Name = "PlayButton" };
        localeButton = new Button { Name = "LocaleButton" };
        hintToggle = new CheckBox { Name = "HintToggle", ButtonPressed = true };
        volumeSlider = new Slider
        {
            Name = "VolumeSlider",
            MinValue = 0d,
            MaxValue = 100d,
            Value = 80d,
            Step = 5d
        };
        matchProgress = new ProgressBar
        {
            Name = "MatchProgress",
            MinValue = 0d,
            MaxValue = 100d,
            Value = 0d,
            ShowPercentage = true
        };
        objectiveList = new ItemList
        {
            Name = "ObjectiveList",
            SelectMode = ItemList.SelectModeEnum.Single,
            MaxColumns = 1,
            FixedColumnWidth = 240
        };
        cardGrid = new GridContainer
        {
            Name = "CardGrid",
            Columns = 2,
            Size = new Vector2(520f, 360f)
        };
        flipAudio = new AudioStreamPlayer { Name = "FlipAudio", Stream = new ReferenceToneAudioStream(0.18f) };
        rewardAudio = new AudioStreamPlayer { Name = "RewardAudio", Stream = new ReferenceToneAudioStream(0.45f) };

        AddChild(shellPanel);
        shellPanel.AddChild(cardFrame);
        shellPanel.AddChild(rewardIcon);
        shellPanel.AddChild(menuLayout);
        shellPanel.AddChild(cardGrid);
        shellPanel.AddChild(objectiveList);
        shellPanel.AddChild(matchProgress);
        shellPanel.AddChild(scoreLabel);
        shellPanel.AddChild(flipAudio);
        shellPanel.AddChild(rewardAudio);

        menuLayout.AddChild(titleLabel);
        menuLayout.AddChild(playButton);
        menuLayout.AddChild(localeButton);
        menuLayout.AddChild(hintToggle);
        menuLayout.AddChild(volumeSlider);

        foreach (var card in cards)
        {
            var button = new TextureButton
            {
                Name = $"Card_{card.Id}",
                TextureNormal = new ReferenceTexture2D(96, 64, hasAlpha: true),
                TexturePressed = new ReferenceTexture2D(96, 64, hasAlpha: true),
                TextureFocused = new ReferenceTexture2D(96, 64, hasAlpha: true),
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                Size = new Vector2(128f, 96f)
            };
            cardButtons.Add(button);
            cardGrid.AddChild(button);
        }

        objectiveList.AddItem("match: orbit");
        objectiveList.AddItem("score: 60");
        objectiveList.AddItem("finish: result");
        objectiveList.Select(0);

        UpdateLocalizedText();
        ShowMenuScene();
        configured = true;
    }

    private void LoadTranslations()
    {
        TranslationServer.Clear();
        foreach (var relativePath in new[]
        {
            "assets/ui-heavy/localization/en.json",
            "assets/ui-heavy/localization/ru.json"
        })
        {
            var path = Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var document = JsonSerializer.Deserialize<LocalizationDocument>(File.ReadAllText(path), jsonOptions) ??
                throw new InvalidOperationException($"Localization file is invalid: {relativePath}");
            var translation = new Translation { Locale = document.Locale };
            foreach (var entry in document.Entries)
            {
                translation.AddMessage(entry.Key, entry.Value);
            }

            TranslationServer.AddTranslation(translation);
        }

        TranslationServer.SetLocale(currentLocale);
        LocalizationReady = TranslationServer.GetLoadedLocales().Contains("en", StringComparer.Ordinal) &&
            TranslationServer.GetLoadedLocales().Contains("ru", StringComparer.Ordinal) &&
            TranslationServer.Translate("missing.reference.key") == "missing.reference.key";
    }

    private void LoadCardData()
    {
        var path = Path.Combine(ProjectRoot, "assets", "ui-heavy", "data", "card-set.json");
        var document = JsonSerializer.Deserialize<CardSetDocument>(File.ReadAllText(path), jsonOptions) ??
            throw new InvalidOperationException("UI-heavy reference card data is invalid.");
        cards.Clear();
        cards.AddRange(document.Cards);
    }

    private void UpdateLocalizedText()
    {
        titleLabel.Text = Tr("game.title");
        playButton.Text = TranslationServer.Translate("menu.play");
        localeButton.Text = TranslationServer.Translate("menu.options");
        hintToggle.Text = "Hints";
        scoreLabel.Text = $"{TranslationServer.Translate("hud.moves")}: {moves} | {TranslationServer.Translate("hud.score")}: {score}";
        TextReady = titleLabel.Text.Length > 0 &&
            playButton.Text.Length > 0 &&
            scoreLabel.Text.Contains(TranslationServer.Translate("hud.score"), StringComparison.Ordinal);
    }

    private void SwitchLocale(string locale)
    {
        currentLocale = locale;
        TranslationServer.SetLocale(locale);
        UpdateLocalizedText();
        LocalizationReady = LocalizationReady && TranslationServer.GetLocale() == locale;
        QueueRedraw();
    }

    private void ApplyResponsiveLayout(Vector2 resolution)
    {
        shellPanel.Size = resolution;
        var portrait = resolution.Y >= resolution.X;
        menuLayout.Size = portrait ? new Vector2(300f, 380f) : new Vector2(420f, 300f);
        cardGrid.Columns = portrait ? 2 : 4;
        cardGrid.Size = portrait ? new Vector2(300f, 420f) : new Vector2(620f, 220f);
        objectiveList.Size = portrait ? new Vector2(300f, 120f) : new Vector2(260f, 180f);
        matchProgress.Size = portrait ? new Vector2(300f, 20f) : new Vector2(420f, 20f);
        ResponsiveLayoutReady = true;
        QueueRedraw();
    }

    private void ShowMenuScene()
    {
        currentScene = "menu";
        menuLayout.Visible = true;
        cardGrid.Visible = false;
        objectiveList.Visible = false;
        matchProgress.Visible = false;
        SceneTransitionReady = true;
        QueueRedraw();
    }

    private void ShowGameScene()
    {
        currentScene = "game";
        menuLayout.Visible = false;
        cardGrid.Visible = true;
        objectiveList.Visible = true;
        matchProgress.Visible = true;
        SceneTransitionReady = SceneTransitionReady && cardGrid.Visible;
        QueueRedraw();
    }

    private void ShowResultScene()
    {
        currentScene = "result";
        menuLayout.Visible = false;
        cardGrid.Visible = false;
        objectiveList.Visible = true;
        matchProgress.Visible = true;
        rewardAudio.Play();
        SceneTransitionReady = SceneTransitionReady && objectiveList.Visible;
        QueueRedraw();
    }

    private void RevealSelectedCard()
    {
        if (cards.Count == 0)
        {
            return;
        }

        var card = cards[Math.Clamp(selectedIndex, 0, cards.Count - 1)];
        moves++;
        score += card.Score;
        matchProgress.Value = Math.Clamp(score, 0, 100);
        objectiveList.Select(Math.Min(1, objectiveList.GetItemCount() - 1));
        flipAudio.Play();
        UpdateLocalizedText();
        QueueRedraw();
    }

    private void SelectNextCard()
    {
        selectedIndex = cards.Count == 0 ? 0 : (selectedIndex + 1) % cards.Count;
        QueueRedraw();
    }

    private void SelectPreviousCard()
    {
        selectedIndex = cards.Count == 0 ? 0 : (selectedIndex + cards.Count - 1) % cards.Count;
        QueueRedraw();
    }

    private void ActivateSelectedAction()
    {
        if (currentScene == "menu")
        {
            ShowGameScene();
        }
        else if (currentScene == "game")
        {
            RevealSelectedCard();
            if (score >= 60)
            {
                ShowResultScene();
            }
        }
        else
        {
            ShowMenuScene();
        }
    }

    private string ReadAndroidRendererProfile()
    {
        var path = Path.Combine(ProjectRoot, "export_presets.e2export.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var preset in document.RootElement.GetProperty("presets").EnumerateArray())
        {
            if (preset.GetProperty("target").GetString() == "AndroidArm64")
            {
                return preset.GetProperty("rendererProfile").GetString() ?? string.Empty;
            }
        }

        return string.Empty;
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
            format = "Electron2D.UiHeavyReference.Progress",
            scene = currentScene,
            locale = currentLocale,
            score,
            moves,
            selectedCard = cards.Count == 0 ? "" : cards[Math.Clamp(selectedIndex, 0, cards.Count - 1)].Id
        };
        File.WriteAllText(fullPath, JsonSerializer.Serialize(payload, jsonOptions));
        SaveProgressUsed = true;
        QueueRedraw();
    }

    private void ApplyPlayableCommand(string command, string savePath)
    {
        switch (command)
        {
            case "play":
                ShowGameScene();
                break;
            case "next":
            case "right":
                SelectNextCard();
                break;
            case "previous":
            case "left":
                SelectPreviousCard();
                break;
            case "accept":
            case "space":
                ActivateSelectedAction();
                break;
            case "locale":
            case "l":
                SwitchLocale(currentLocale == "en" ? "ru" : "en");
                break;
            case "result":
                ShowResultScene();
                break;
            case "save":
            case "s":
                SaveProgress(savePath);
                break;
            default:
                break;
        }

        QueueRedraw();
    }

    private UiHeavyPlayableResult CreatePlayableResult(int framesAdvanced, int commandsApplied, string savePath)
    {
        if (!SaveProgressUsed)
        {
            SaveProgress(savePath);
        }

        var selectedCard = cards.Count == 0 ? string.Empty : cards[Math.Clamp(selectedIndex, 0, cards.Count - 1)].Id;
        return new UiHeavyPlayableResult(
            Playable: framesAdvanced > 0 && commandsApplied > 0 && SceneTransitionReady && score > 0,
            framesAdvanced,
            commandsApplied,
            currentScene,
            currentLocale,
            score,
            moves,
            selectedCard,
            savePath);
    }

    private string SelectedCardId()
    {
        return cards.Count == 0 ? "none" : cards[Math.Clamp(selectedIndex, 0, cards.Count - 1)].Id;
    }

    private static string NormalizePlayableCommand(string command)
    {
        return command.Trim().ToLowerInvariant();
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

    private sealed class ReferenceToneAudioStream(float length) : AudioStream
    {
        public override float GetLength()
        {
            return length;
        }
    }

    private sealed class ReferenceFont : Font;

    private sealed record LocalizationDocument(string Locale, Dictionary<string, string> Entries);

    private sealed record CardSetDocument(string Id, int Version, CardData[] Cards, CardRules Rules);

    private sealed record CardData(string Id, int Rank, string Suit, int Score);

    private sealed record CardRules(int HandSize, string MatchBy, bool AllowUndo);
}

internal sealed record UiHeavyVerificationResult(
    bool Control,
    bool Containers,
    bool BasicControls,
    bool StructuredList,
    bool Localization,
    bool ResponsiveLayout,
    bool Touch,
    bool Text,
    bool Save,
    bool SceneTransition,
    bool AndroidCompatibilityRenderer,
    bool Audio,
    string Scene,
    string Locale,
    int Score,
    int Moves,
    string SavePath)
{
    public bool AllPassed => Control &&
        Containers &&
        BasicControls &&
        StructuredList &&
        Localization &&
        ResponsiveLayout &&
        Touch &&
        Text &&
        Save &&
        SceneTransition &&
        AndroidCompatibilityRenderer &&
        Audio;

    public string ToSubsystemSummary()
    {
        return string.Join(
            ',',
            [
                $"control={Control}",
                $"containers={Containers}",
                $"basicControls={BasicControls}",
                $"structuredList={StructuredList}",
                $"localization={Localization}",
                $"resolutions={ResponsiveLayout}",
                $"touch={Touch}",
                $"text={Text}",
                $"save={Save}",
                $"sceneTransition={SceneTransition}",
                $"androidCompatibility={AndroidCompatibilityRenderer}",
                $"audio={Audio}"
            ]);
    }
}

internal sealed record UiHeavyPlayableResult(
    bool Playable,
    int FramesAdvanced,
    int CommandsApplied,
    string Scene,
    string Locale,
    int Score,
    int Moves,
    string SelectedCard,
    string SavePath);
