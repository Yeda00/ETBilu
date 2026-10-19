using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using OneMoreTime.Core.Controls;
using OneMoreTime.Core.Menu;
using OneMoreTime.Gameplay.Audio;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Language;
using OneMoreTime.Gameplay.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// The menu scene: title screen, main menu, sound, controls, credits, the mission map and the story that
    /// scrolls before the stage. The order of screens and the highlighted entries live in Core (ScreenFlow,
    /// MenuCursor, CrawlScroll, SoundLevels); this component draws them with UI Toolkit (UI/Menu.uxml and
    /// Menu.uss), plays the sounds and loads the stage. Words come from the string table (<see cref="L10n"/>) and
    /// <see cref="MissionData"/>; the Language screen switches between the languages of the project and the
    /// screens redraw their words at once. Works with keyboard and gamepad (MenuInputReader) and mouse. The controls
    /// screen lets the player change the keys and buttons of the game (the Input System's interactive rebinding).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] MenuInputReader input;
        [SerializeField] SfxPlayer sfx;
        [SerializeField] MusicPlayer music;
        [Tooltip("Missions in the order of the map. Only the first one exists for now; the other slots show as locked.")]
        [SerializeField] MissionData[] missions;
        [Tooltip("The ship shown behind the title and the main menu. Optional.")]
        [SerializeField] ShowcaseSpin showcase;
        [Tooltip("The picture of the ship that the menu draws above the logo, so the ship passes in front of it. Optional.")]
        [SerializeField] ShowcaseView showcaseView;
        [SerializeField, Range(3, 9)] int mapSlots = 7;
        [Header("Story")]
        [SerializeField, Min(10f)] float crawlSpeed = 40f;
        [SerializeField, Min(100f)] float crawlViewportHeight = 480f;
        [SerializeField, Min(0.05f)] float fadeSeconds = 0.5f;

        enum MainItem { Play, Controls, Sound, Language, Credits, Quit }
        static readonly MainItem[] MainItems = { MainItem.Play, MainItem.Controls, MainItem.Sound, MainItem.Language, MainItem.Credits, MainItem.Quit };

        readonly Dictionary<MenuScreen, VisualElement> screens = new();
        readonly List<VisualElement> mainBars = new();
        readonly List<VisualElement> soundRows = new();
        readonly List<VisualElement[]> soundCells = new();
        readonly List<VisualElement> slots = new();
        readonly List<VisualElement> links = new();
        readonly List<Label[]> controlCells = new();
        readonly List<VisualElement> languageBars = new();
        List<LanguageOption> languages = new();

        ScreenFlow flow;
        MenuCursor mainCursor, soundCursor, missionCursor, controlsCursor, languageCursor;
        int controlsColumn;   // 0 keyboard, 1 gamepad
        InputActionRebindingExtensions.RebindingOperation rebinding;
        bool waitingForKey;
        Label resetBar, controlsHint;
        SoundLevels levels;
        CrawlScroll crawl;
        MissionData chosen;

        VisualElement root, fade, logo, introPicture, introViewport;
        Label pressStart, introText, missionNumber, missionPlace, missionTitle;
        float blinkTimer;
        float fadeValue = 1f;
        bool loading;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            controlsCursor = new MenuCursor(ControlLayout.Rows.Count + 1);
            controlsColumn = 0;
            waitingForKey = false;
            levels = SoundLevelsStore.Load();
            flow = new ScreenFlow(MenuScreen.Title);
            mainCursor = new MenuCursor(MainItems.Length);
            soundCursor = new MenuCursor(2);
            missionCursor = new MenuCursor(mapSlots);
            languages = L10n.Languages();
            languageCursor = new MenuCursor(Mathf.Max(1, languages.Count));
            languageCursor.Set(Mathf.Max(0, languages.FindIndex(l => l.Code == L10n.CurrentCode)));
            loading = false;
            fadeValue = 1f;

            BindScreens();
            BuildMainMenu();
            BuildSoundScreen();
            BuildControlsScreen();
            BuildCreditsScreen();
            BuildLanguageScreen();
            BuildMissionMap();
            ApplyLevels();
            Show(MenuScreen.Title, silent: true);
            L10n.Changed += OnLanguageChanged;
        }

        void Start() => music?.Play(MusicTrack.Title);

        void OnDisable()
        {
            L10n.Changed -= OnLanguageChanged;
            rebinding?.Dispose();
            rebinding = null;
        }

        // ---------------------------------------------------------------- building the screens

        void BindScreens()
        {
            screens.Clear();
            foreach (MenuScreen screen in System.Enum.GetValues(typeof(MenuScreen)))
                screens[screen] = root.Q($"screen-{screen.ToString().ToLowerInvariant()}");

            fade = root.Q("fade");
            logo = root.Q("logo");
            var shipView = root.Q("ship-view");
            if (shipView != null && showcaseView != null && showcaseView.Texture != null)
                shipView.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(showcaseView.Texture));
            pressStart = root.Q<Label>("press-start");
            introPicture = root.Q("intro-picture");
            introViewport = root.Q("intro-viewport");
            introText = root.Q<Label>("intro-text");
            missionNumber = root.Q<Label>("mission-number");
            missionPlace = root.Q<Label>("mission-place");
            missionTitle = root.Q<Label>("mission-title");

            screens[MenuScreen.Title].RegisterCallback<ClickEvent>(_ => OnTitleConfirm());
            ApplyStaticTexts();
        }

        // The words that sit in Menu.uxml; drawn again whenever the language changes.
        void ApplyStaticTexts()
        {
            root.Q<Label>("tagline").text = L10n.Get("menu.tagline");
            pressStart.text = L10n.Get("menu.pressStart");
            root.Q<Label>("main-hint").text = L10n.Get("menu.hint.list");
            root.Q<Label>("sound-hint").text = L10n.Get("menu.hint.sound");
            controlsHint = root.Q<Label>("controls-hint");
            controlsHint.text = L10n.Get("menu.hint.controls");
            root.Q<Label>("credits-hint").text = L10n.Get("menu.hint.back");
            root.Q<Label>("language-hint").text = L10n.Get("menu.hint.list");
            root.Q<Label>("missions-hint").text = L10n.Get("menu.hint.map");
            root.Q<Label>("intro-hint").text = L10n.Get("menu.hint.intro");
            root.Q<Label>("sound-title").text = L10n.Get("menu.sound.title");
            root.Q<Label>("controls-title").text = L10n.Get("menu.controls.title");
            root.Q<Label>("credits-title").text = L10n.Get("menu.credits.title");
            root.Q<Label>("language-title").text = L10n.Get("menu.language.title");
            root.Q<Label>("missions-title").text = L10n.Get("menu.missions.title");
        }

        void BuildMainMenu()
        {
            var list = root.Q("main-list");
            list.Clear();
            mainBars.Clear();
            string[] labels = { L10n.Get("menu.play"), L10n.Get("menu.controls"), L10n.Get("menu.sound"), L10n.Get("menu.language"), L10n.Get("menu.credits"), L10n.Get("menu.quit") };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var bar = new Label(labels[i]);
                bar.AddToClassList("bar");
                bar.RegisterCallback<PointerEnterEvent>(_ => HoverMain(index));
                bar.RegisterCallback<ClickEvent>(_ => { HoverMain(index); ActivateMain(); });
                list.Add(bar);
                mainBars.Add(bar);
            }
        }

        void BuildSoundScreen()
        {
            var rows = root.Q("sound-rows");
            rows.Clear();
            soundRows.Clear();
            soundCells.Clear();
            string[] labels = { L10n.Get("menu.sound.music"), L10n.Get("menu.sound.effects") };
            for (int r = 0; r < 2; r++)
            {
                int row = r;
                var element = new VisualElement();
                element.AddToClassList("sound-row");
                element.RegisterCallback<PointerEnterEvent>(_ => { soundCursor.Set(row); RefreshSound(); });
                element.Add(new Label(labels[r]) { name = $"sound-label-{r}" }.WithClass("sound-label"));
                var meter = new VisualElement();
                meter.AddToClassList("sound-meter");
                var cells = new VisualElement[SoundLevels.MaxStep];
                for (int c = 0; c < cells.Length; c++)
                {
                    int step = c + 1;
                    var cell = new VisualElement();
                    cell.AddToClassList("sound-cell");
                    cell.RegisterCallback<ClickEvent>(_ => SetLevel(row, step));
                    meter.Add(cell);
                    cells[c] = cell;
                }
                element.Add(meter);
                rows.Add(element);
                soundRows.Add(element);
                soundCells.Add(cells);
            }
        }

        void BuildControlsScreen()
        {
            var grid = root.Q("controls-grid");
            grid.Clear();
            controlCells.Clear();

            var head = new VisualElement();
            head.AddToClassList("ctl-row");
            head.Add(new Label("").WithClass("ctl-label"));
            head.Add(new Label(L10n.Get("menu.controls.keyboard")).WithClass("ctl-head"));
            head.Add(new Label(L10n.Get("menu.controls.gamepad")).WithClass("ctl-head"));
            grid.Add(head);

            for (int r = 0; r < ControlLayout.Rows.Count; r++)
            {
                int row = r;
                var line = new VisualElement();
                line.AddToClassList("ctl-row");
                line.Add(new Label(L10n.Get("control." + ControlLayout.Rows[r].Key)).WithClass("ctl-label"));
                var cells = new Label[2];
                for (int c = 0; c < 2; c++)
                {
                    int column = c;
                    var cell = new Label().WithClass("ctl-cell");
                    cell.RegisterCallback<PointerEnterEvent>(_ => HoverControl(row, column));
                    cell.RegisterCallback<ClickEvent>(_ => { HoverControl(row, column); ActivateControl(); });
                    line.Add(cell);
                    cells[c] = cell;
                }
                controlCells.Add(cells);
                grid.Add(line);
            }

            resetBar = new Label(L10n.Get("menu.controls.reset")).WithClass("ctl-reset");
            int resetRow = ControlLayout.Rows.Count;
            resetBar.RegisterCallback<PointerEnterEvent>(_ => HoverControl(resetRow, controlsColumn));
            resetBar.RegisterCallback<ClickEvent>(_ => { HoverControl(resetRow, controlsColumn); ActivateControl(); });
            grid.Add(resetBar);
            RefreshControls();
        }

        void BuildCreditsScreen() => root.Q<Label>("credits-body").text = L10n.Get("menu.credits.body");

        void BuildMissionMap()
        {
            var map = root.Q("map");
            map.Clear();
            slots.Clear();
            links.Clear();
            for (int i = 0; i < mapSlots; i++)
            {
                int index = i;
                if (i > 0)
                {
                    var link = new VisualElement();
                    link.AddToClassList("link");
                    map.Add(link);
                    links.Add(link);
                }
                var slot = new VisualElement();
                slot.AddToClassList("slot");
                var code = new Label(MissionAt(i) != null ? MissionAt(i).mapCode : "?");
                code.AddToClassList("slot-code");
                slot.Add(code);
                slot.RegisterCallback<PointerEnterEvent>(_ => { missionCursor.Set(index); RefreshMissions(); });
                slot.RegisterCallback<ClickEvent>(_ => { missionCursor.Set(index); RefreshMissions(); ActivateMission(); });
                map.Add(slot);
                slots.Add(slot);
            }
        }

        MissionData MissionAt(int index) => missions != null && index >= 0 && index < missions.Length ? missions[index] : null;

        // ---------------------------------------------------------------- frame

        void Update()
        {
            UpdateFade();
            UpdateBlink();
            if (loading || fadeValue > 0.6f || waitingForKey)
                return;

            switch (flow.Current)
            {
                case MenuScreen.Title:
                    if (input != null && input.AnyKeyPressed)
                        OnTitleConfirm();
                    break;
                case MenuScreen.Main:
                    UpdateMain();
                    break;
                case MenuScreen.Sound:
                    UpdateSound();
                    break;
                case MenuScreen.Controls:
                    UpdateControls();
                    break;
                case MenuScreen.Credits:
                    if (input != null && (input.CancelPressed || input.SubmitPressed))
                        GoBack();
                    break;
                case MenuScreen.Language:
                    UpdateLanguage();
                    break;
                case MenuScreen.Missions:
                    UpdateMissions();
                    break;
                case MenuScreen.Intro:
                    UpdateIntro();
                    break;
            }
        }

        void UpdateFade()
        {
            float target = loading ? 1f : 0f;
            float step = Time.unscaledDeltaTime / fadeSeconds;
            fadeValue = Mathf.MoveTowards(fadeValue, target, step);
            if (fade != null)
                fade.style.opacity = fadeValue;
            if (loading && fadeValue >= 1f)
                LoadStage();
        }

        void UpdateBlink()
        {
            blinkTimer += Time.unscaledDeltaTime;
            if (pressStart != null)
                pressStart.EnableInClassList("dim", blinkTimer % 1.1f > 0.7f);
        }

        // ---------------------------------------------------------------- screens

        void Show(MenuScreen screen, bool silent = false)
        {
            foreach (var pair in screens)
                pair.Value?.EnableInClassList("hidden", pair.Key != screen);

            logo?.EnableInClassList("hidden", screen != MenuScreen.Title);

            bool shipVisible = screen == MenuScreen.Title || screen == MenuScreen.Main;
            if (showcase != null)
            {
                showcase.gameObject.SetActive(shipVisible);
                showcase.SetMenuPose(screen != MenuScreen.Title);
            }

            switch (screen)
            {
                case MenuScreen.Main:
                    RefreshMain();
                    break;
                case MenuScreen.Sound:
                    RefreshSound();
                    break;
                case MenuScreen.Controls:
                    RefreshControls();
                    break;
                case MenuScreen.Language:
                    languageCursor.Set(Mathf.Max(0, languages.FindIndex(l => l.Code == L10n.CurrentCode)));
                    RefreshLanguage();
                    break;
                case MenuScreen.Missions:
                    RefreshMissions();
                    music?.Play(MusicTrack.Missions);
                    break;
                case MenuScreen.Intro:
                    BeginIntro();
                    music?.Play(MusicTrack.Intro);
                    break;
            }
            if (screen == MenuScreen.Main || screen == MenuScreen.Title)
                music?.Play(MusicTrack.Title);
        }

        void GoTo(MenuScreen screen, SoundCue cue = SoundCue.MenuSelect)
        {
            Play(cue);
            flow.Go(screen);
            Show(screen);
        }

        void GoBack()
        {
            if (!flow.Back())
                return;
            Play(SoundCue.MenuBack);
            Show(flow.Current);
        }

        // ---------------------------------------------------------------- title

        void OnTitleConfirm()
        {
            if (loading || flow.Current != MenuScreen.Title)
                return;
            GoTo(MenuScreen.Main, SoundCue.MenuStart);
        }

        // ---------------------------------------------------------------- main menu

        void UpdateMain()
        {
            if (input == null)
                return;
            if (input.Up) MoveMain(-1);
            if (input.Down) MoveMain(1);
            if (input.SubmitPressed) ActivateMain();
            if (input.CancelPressed) GoBack();
        }

        void MoveMain(int delta)
        {
            if (mainCursor.Move(delta))
                Play(SoundCue.MenuMove);
            RefreshMain();
        }

        void HoverMain(int index)
        {
            if (flow.Current != MenuScreen.Main || mainCursor.Index == index)
                return;
            mainCursor.Set(index);
            Play(SoundCue.MenuMove);
            RefreshMain();
        }

        void RefreshMain()
        {
            for (int i = 0; i < mainBars.Count; i++)
                mainBars[i].EnableInClassList("selected", i == mainCursor.Index);
        }

        void ActivateMain()
        {
            switch (MainItems[mainCursor.Index])
            {
                case MainItem.Play: GoTo(MenuScreen.Missions); break;
                case MainItem.Controls: GoTo(MenuScreen.Controls); break;
                case MainItem.Sound: GoTo(MenuScreen.Sound); break;
                case MainItem.Language: GoTo(MenuScreen.Language); break;
                case MainItem.Credits: GoTo(MenuScreen.Credits); break;
                case MainItem.Quit: Quit(); break;
            }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- sound

        void UpdateSound()
        {
            if (input == null)
                return;
            if (input.Up || input.Down)
            {
                soundCursor.Move(input.Up ? -1 : 1);
                Play(SoundCue.MenuMove);
                RefreshSound();
            }
            if (input.Left) AdjustLevel(-1);
            if (input.Right) AdjustLevel(1);
            if (input.CancelPressed || input.SubmitPressed) GoBack();
        }

        void AdjustLevel(int delta)
        {
            var channel = soundCursor.Index == 0 ? SoundChannel.Music : SoundChannel.Effects;
            if (!levels.Adjust(channel, delta))
                return;
            LevelsChanged();
        }

        void SetLevel(int row, int step)
        {
            soundCursor.Set(row);
            levels.Set(row == 0 ? SoundChannel.Music : SoundChannel.Effects, step);
            LevelsChanged();
        }

        void LevelsChanged()
        {
            SoundLevelsStore.Save(levels);
            ApplyLevels();
            RefreshSound();
            Play(SoundCue.MenuMove);   // the effects slider is heard right away; the music one is heard in the track
        }

        void ApplyLevels()
        {
            music?.SetLevel(levels.Gain(SoundChannel.Music));
            sfx?.SetLevel(levels.Gain(SoundChannel.Effects));
        }

        void RefreshSound()
        {
            for (int r = 0; r < soundRows.Count; r++)
            {
                soundRows[r].EnableInClassList("selected", r == soundCursor.Index);
                int value = levels.Get(r == 0 ? SoundChannel.Music : SoundChannel.Effects);
                for (int c = 0; c < soundCells[r].Length; c++)
                    soundCells[r][c].EnableInClassList("on", c < value);
            }
        }

        // ---------------------------------------------------------------- controls

        ControlDevice Device => controlsColumn == 0 ? ControlDevice.Keyboard : ControlDevice.Gamepad;
        bool OnResetRow => controlsCursor.Index >= ControlLayout.Rows.Count;

        void UpdateControls()
        {
            if (input == null)
                return;
            if (input.Up || input.Down)
            {
                controlsCursor.Move(input.Up ? -1 : 1);
                Play(SoundCue.MenuMove);
                RefreshControls();
            }
            if ((input.Left || input.Right) && !OnResetRow)
            {
                controlsColumn = input.Left ? 0 : 1;
                Play(SoundCue.MenuMove);
                RefreshControls();
            }
            if (input.SubmitPressed) ActivateControl();
            if (input.CancelPressed) GoBack();
        }

        void HoverControl(int row, int column)
        {
            if (flow.Current != MenuScreen.Controls || waitingForKey || (controlsCursor.Index == row && controlsColumn == column))
                return;
            controlsCursor.Set(row);
            controlsColumn = column;
            Play(SoundCue.MenuMove);
            RefreshControls();
        }

        void RefreshControls()
        {
            if (controlCells.Count == 0)
                return;
            var asset = input != null ? input.Controls : null;
            for (int r = 0; r < controlCells.Count; r++)
            {
                var row = ControlLayout.Rows[r];
                for (int c = 0; c < 2; c++)
                {
                    var device = c == 0 ? ControlDevice.Keyboard : ControlDevice.Gamepad;
                    bool selected = r == controlsCursor.Index && c == controlsColumn;
                    var cell = controlCells[r][c];
                    cell.text = selected && waitingForKey ? L10n.Get("menu.controls.waiting") : ControlsCatalog.Describe(asset, row, device);
                    cell.EnableInClassList("fixed", !row.IsEditable(device));
                    cell.EnableInClassList("selected", selected);
                    cell.EnableInClassList("waiting", selected && waitingForKey);
                }
            }
            resetBar.EnableInClassList("selected", OnResetRow);
            controlsHint.text = L10n.Get(waitingForKey ? "menu.hint.waiting" : "menu.hint.controls");
        }

        void ActivateControl()
        {
            if (waitingForKey || input == null)
                return;
            var asset = input.Controls;
            if (OnResetRow)
            {
                ControlsStore.ResetAll(asset);
                Play(SoundCue.MenuSelect);
                RefreshControls();
                return;
            }

            var row = ControlLayout.Rows[controlsCursor.Index];
            if (!row.IsEditable(Device))
            {
                Play(SoundCue.MenuDenied);
                controlsHint.text = L10n.Get("menu.hint.fixed");
                return;
            }
            BeginRebind(asset, row, controlsCursor.Index, Device);
        }

        void BeginRebind(InputActionAsset asset, ControlRow row, int rowIndex, ControlDevice device)
        {
            var action = ControlsCatalog.FindAction(asset, row);
            int bindingIndex = ControlsCatalog.FindBindingIndex(action, row, device);
            if (bindingIndex < 0)
            {
                Play(SoundCue.MenuDenied);
                return;
            }

            string previousPath = action.bindings[bindingIndex].effectivePath;
            bool keyboard = device == ControlDevice.Keyboard;
            if (action.enabled)
                action.Disable();
            Play(SoundCue.MenuSelect);
            waitingForKey = true;
            RefreshControls();

            rebinding = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsHavingToMatchPath(keyboard ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithCancelingThrough(keyboard ? "<Keyboard>/escape" : "<Gamepad>/start")
                .WithExpectedControlType("Button")
                .OnComplete(_ => FinishRebind(asset, action, bindingIndex, previousPath, rowIndex, device))
                .OnCancel(_ => EndRebind(asset, saved: false))
                .Start();
        }

        // If another line of the same device already had the control, the two swap, so no control does two things.
        void FinishRebind(InputActionAsset asset, InputAction action, int bindingIndex, string previousPath, int rowIndex, ControlDevice device)
        {
            string chosen = action.bindings[bindingIndex].effectivePath;
            int other = BindingConflicts.Find(ControlsCatalog.EffectivePaths(asset, device), rowIndex, chosen);
            if (other >= 0)
            {
                var otherRow = ControlLayout.Rows[other];
                var otherAction = ControlsCatalog.FindAction(asset, otherRow);
                int otherIndex = ControlsCatalog.FindBindingIndex(otherAction, otherRow, device);
                if (otherIndex >= 0)
                    otherAction.ApplyBindingOverride(otherIndex, previousPath);
            }
            EndRebind(asset, saved: true);
        }

        void EndRebind(InputActionAsset asset, bool saved)
        {
            rebinding?.Dispose();
            rebinding = null;
            waitingForKey = false;
            if (saved)
                ControlsStore.Save(asset);
            Play(saved ? SoundCue.MenuStart : SoundCue.MenuBack);
            RefreshControls();
        }

        // ---------------------------------------------------------------- language

        void BuildLanguageScreen()
        {
            var list = root.Q("language-list");
            list.Clear();
            languageBars.Clear();
            for (int i = 0; i < languages.Count; i++)
            {
                int index = i;
                var bar = new Label(languages[i].Name);
                bar.AddToClassList("bar");
                bar.RegisterCallback<PointerEnterEvent>(_ => HoverLanguage(index));
                bar.RegisterCallback<ClickEvent>(_ => { HoverLanguage(index); ActivateLanguage(); });
                list.Add(bar);
                languageBars.Add(bar);
            }
        }

        void UpdateLanguage()
        {
            if (input == null)
                return;
            if (input.Up) MoveLanguage(-1);
            if (input.Down) MoveLanguage(1);
            if (input.SubmitPressed) ActivateLanguage();
            if (input.CancelPressed) GoBack();
        }

        void MoveLanguage(int delta)
        {
            if (languageCursor.Move(delta))
                Play(SoundCue.MenuMove);
            RefreshLanguage();
        }

        void HoverLanguage(int index)
        {
            if (flow.Current != MenuScreen.Language || languageCursor.Index == index)
                return;
            languageCursor.Set(index);
            Play(SoundCue.MenuMove);
            RefreshLanguage();
        }

        void RefreshLanguage()
        {
            for (int i = 0; i < languageBars.Count; i++)
            {
                languageBars[i].EnableInClassList("selected", i == languageCursor.Index);
                languageBars[i].EnableInClassList("current", languages[i].Code == L10n.CurrentCode);
            }
        }

        void ActivateLanguage()
        {
            if (languageCursor.Index >= languages.Count)
                return;
            Play(SoundCue.MenuSelect);
            L10n.SetLanguage(languages[languageCursor.Index].Code);   // OnLanguageChanged redraws the words
            RefreshLanguage();
        }

        // Every screen draws its words again; the cursors and the screen in view stay where they are.
        void OnLanguageChanged()
        {
            if (root == null)
                return;
            ApplyStaticTexts();
            BuildMainMenu();
            BuildSoundScreen();
            BuildControlsScreen();
            BuildCreditsScreen();
            RefreshMain();
            RefreshSound();
            RefreshMissions();
            RefreshLanguage();
        }

        // ---------------------------------------------------------------- mission map

        void UpdateMissions()
        {
            if (input == null)
                return;
            if (input.Left) MoveMission(-1);
            if (input.Right) MoveMission(1);
            if (input.SubmitPressed) ActivateMission();
            if (input.CancelPressed) GoBack();
        }

        void MoveMission(int delta)
        {
            if (missionCursor.Move(delta, wrap: false))
                Play(SoundCue.MenuMove);
            RefreshMissions();
        }

        void RefreshMissions()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                bool locked = MissionAt(i) == null;
                slots[i].EnableInClassList("locked", locked);
                slots[i].EnableInClassList("selected", i == missionCursor.Index);
                if (i > 0)
                    links[i - 1].EnableInClassList("locked", locked);
            }

            var mission = MissionAt(missionCursor.Index);
            if (mission != null)
            {
                missionNumber.text = L10n.Format("menu.mission.number", mission.number);
                missionPlace.text = mission.Place;
                missionTitle.text = mission.Title;
            }
            else
            {
                missionNumber.text = L10n.Get("menu.locked");
                missionPlace.text = L10n.Get("menu.locked.hint");
                missionTitle.text = "";
            }
        }

        void ActivateMission()
        {
            var mission = MissionAt(missionCursor.Index);
            if (mission == null)
            {
                Play(SoundCue.MenuDenied);
                return;
            }
            chosen = mission;
            GoTo(MenuScreen.Intro);
        }

        // ---------------------------------------------------------------- story

        void BeginIntro()
        {
            if (chosen == null)
                return;
            introPicture.style.backgroundImage = chosen.storyPicture != null
                ? new StyleBackground(chosen.storyPicture)
                : new StyleBackground(StyleKeyword.None);
            introViewport.style.height = crawlViewportHeight;

            string story = string.Join("\n\n", chosen.Story);
            introText.text = story;
            float width = introViewport.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f)
                width = 1040f;
            Vector2 size = introText.MeasureTextSize(story, width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
            crawl = new CrawlScroll(crawlViewportHeight, size.y, crawlSpeed);
            PlaceCrawl();
        }

        void UpdateIntro()
        {
            if (crawl == null)
            {
                StartMission();
                return;
            }
            crawl.Advance(Time.unscaledDeltaTime, input != null && input.FastHeld);
            PlaceCrawl();
            if (input != null && input.CancelPressed)
            {
                GoBack();
                return;
            }
            if (crawl.IsFinished || (input != null && input.SubmitPressed))
                StartMission();
        }

        void PlaceCrawl() => introText.style.translate = new Translate(0f, crawl.TopY);

        // ---------------------------------------------------------------- starting the stage

        void StartMission()
        {
            if (loading || chosen == null)
                return;
            Play(SoundCue.MenuStart);
            loading = true;
            music?.Stop();
            MissionSelection.Select(chosen);
            RunCheckpoint.Clear();
        }

        void LoadStage() => SceneManager.LoadScene(chosen.sceneName);

        void Play(SoundCue cue) => sfx?.Play(cue, Vector3.zero);
    }

    static class LabelExtensions
    {
        public static Label WithClass(this Label label, string className)
        {
            label.AddToClassList(className);
            return label;
        }
    }
}
