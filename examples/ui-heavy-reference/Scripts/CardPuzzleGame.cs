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
            CustomMinimumSize = new Vector2(320f, 480f)
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
    }

    private void ShowMenuScene()
    {
        currentScene = "menu";
        menuLayout.Visible = true;
        cardGrid.Visible = false;
        objectiveList.Visible = false;
        matchProgress.Visible = false;
        SceneTransitionReady = true;
    }

    private void ShowGameScene()
    {
        currentScene = "game";
        menuLayout.Visible = false;
        cardGrid.Visible = true;
        objectiveList.Visible = true;
        matchProgress.Visible = true;
        SceneTransitionReady = SceneTransitionReady && cardGrid.Visible;
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
