using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Console;
using Centauri64.Basic;
using Centauri64.Machine;
using Centauri64.Game;
using Centauri64.Network;
using Centauri64.Publishing;
using Centauri64.Session;
using Centauri64.Settings;

namespace Centauri64;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly SettingsService _settingsService = new();
    private CentauriSettings _settings = CentauriSettings.CreateDefaults();

    private RenderTarget2D _codeEditorRenderTarget = null!;

    private RenderTarget2D _gameRenderTarget = null!;

    private BitmapFont _font = null!;
    private PrintFonts _printFonts = null!;

    private TextConsole _console = null!;
    private TextConsole _programConsole = null!;

    private Texture2D _pixel = null!;

    private BasicMachine _basicMachine = null!;

    private CentauriMachine _machine = null!;

    private NetworkService _network = null!;

    private KeyboardState _previousKeyboardState;

    private bool _finishedProgramInputArmed;

    private GameMode _gameMode = GameMode.ModeSelect;

    private bool _waitForInputRelease;

    private bool _computerPoweringOn;
    private double _powerOnTimer;

    private const double POWER_ON_DELAY = 0.5;

    private ComputerRoom _computerRoom;

    private ProgrammingManual _programmingManual = null!;

    private SoftwareShelf _softwareShelf = null!;

    private MagazinesScreen _magazinesScreen = null!;

    private MailScreen _mailScreen = null!;

    private SettingsScreen _settingsScreen = null!;

    private HistoricalIntroScreen _historicalIntro = null!;
    private ModeSelectScreen _modeSelect = null!;
    private CareerNameScreen _careerName = null!;
    private BundleSelectScreen _bundleSelect = null!;
    private SystemMenuScreen _systemMenu = null!;
    private ConfirmTypeScreen _confirmType = null!;

    private readonly CareerRepository _careers = new();
    private CareerService _career = null!;

    private int _pendingSlot;
    private string _pendingName = string.Empty;
    private GameMode _settingsReturn = GameMode.ComputerRoom;
    private GameMode _manualReturn = GameMode.ComputerRoom;
    private int _deleteSlot;
    private string _lockNotice = string.Empty;
    private double _lockNoticeTimer;
    private string _unlockNotice = string.Empty;
    private double _unlockNoticeTimer;
    private float _fade;
    private int _fadeDirection;
    private string _fadeCaption = string.Empty;
    private Action? _afterFade;
    private bool _replayIntro;

    private static readonly Color EditorBackground = new(205, 198, 170);

    private static readonly Color EditorFrame = CentauriPalette.Get(17); // Navy

    private static readonly Color EditorText = CentauriPalette.Get(16); // Midnight Blue

    private static readonly Color EditorAccent = CentauriPalette.Get(3); // Teal

    private const int CODE_EDITOR_ROWS = 55;

    public Game1()
    {
        _settings = _settingsService.Load();
        _settings.WindowScale = CentauriSettings.ClampScale(_settings.WindowScale, 5);

        _graphics = new GraphicsDeviceManager(this);
        _graphics.SynchronizeWithVerticalRetrace = _settings.VSync;
        IsFixedTimeStep = _settings.VSync;

        _graphics.PreferredBackBufferWidth =
            CentauriMachine.DEVELOPMENT_WIDTH * _settings.WindowScale;

        _graphics.PreferredBackBufferHeight =
            CentauriMachine.DEVELOPMENT_HEIGHT * _settings.WindowScale;

        _graphics.IsFullScreen =
            _settings.WindowMode == WindowModeSetting.Fullscreen;

        _graphics.ApplyChanges();

        Window.Title = "Centauri64";

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void UnloadContent()
    {
        _network?.Dispose();
        base.UnloadContent();
    }

    protected override void LoadContent()
    {
        _spriteBatch    = new SpriteBatch(GraphicsDevice);

        _codeEditorRenderTarget =new RenderTarget2D(
        GraphicsDevice,
        CentauriMachine.DEVELOPMENT_WIDTH,
        CentauriMachine.DEVELOPMENT_HEIGHT);

        _gameRenderTarget =new RenderTarget2D(
        GraphicsDevice,
        CentauriMachine.GAME_WIDTH,
        CentauriMachine.GAME_HEIGHT);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        var fontTexture = Content.Load<Texture2D>("Fonts/centauri64-font");

        _font           = new BitmapFont(fontTexture);
        _printFonts = new PrintFonts(
            Content.Load<SpriteFont>("Fonts/print-title"),
            Content.Load<SpriteFont>("Fonts/print-body"),
            Content.Load<SpriteFont>("Fonts/print-caption"));

        _console        = new TextConsole(80, CODE_EDITOR_ROWS);
        _programConsole = new TextConsole(80, 60);
        _programConsole.LineEntered += OnProgramInput;

        // Development editor palette.
        _console.Foreground = 16; // Midnight Blue
        _console.Background = 31; // Warm White
        _console.Clear();


        _machine = new CentauriMachine(_console,_programConsole);
        _machine.SpriteEditor.Notice += OnSpriteEditorNotice;
        _machine.MapEditor.Notice += OnMapEditorNotice;
        _machine.ImageEditor.Notice += OnImageEditorNotice;

        _network = new NetworkService();
        _basicMachine = new BasicMachine(_console, _machine, _network);

        _career = new CareerService(new SessionCareerProgressStore(_careers));

        _computerRoom = new ComputerRoom(_font,_pixel);

        _programmingManual = new ProgrammingManual(_font,_pixel);

        var cassetteShelf = Content.Load<Texture2D>("UI/cassette-stripes");
        var cassetteSideA = Content.Load<Texture2D>("UI/cassette-side-a");

        _softwareShelf = new SoftwareShelf(
            _font,
            _pixel,
            cassetteShelf,
            cassetteSideA,
            _basicMachine);

        _magazinesScreen = new MagazinesScreen(
            _pixel,
            _basicMachine,
            _career,
            GraphicsDevice,
            _printFonts);
        _mailScreen = new MailScreen(_font, _pixel, _career);
        _settingsScreen = new SettingsScreen(
            _font,
            _pixel,
            _settingsService,
            _career,
            ApplySettings);

        _historicalIntro = new HistoricalIntroScreen(_font, _pixel);
        _modeSelect = new ModeSelectScreen(_font, _pixel);
        _careerName = new CareerNameScreen(_font, _pixel);
        _bundleSelect = new BundleSelectScreen(_printFonts, _pixel);
        _systemMenu = new SystemMenuScreen(_font, _pixel);
        _confirmType = new ConfirmTypeScreen(_font, _pixel);

        WireBootFlow();

        _computerRoom.ComputerSelected += OnComputerSelected;

        _computerRoom.ManualSelected += () => OpenManual(GameMode.ComputerRoom);

        _computerRoom.SoftwareSelected += () => OpenSoftware(GameMode.ComputerRoom);

        _computerRoom.MagazinesSelected += () => OpenMagazines(GameMode.ComputerRoom, false);

        _computerRoom.MailSelected += () =>
        {
            _mailScreen.Open();
            _gameMode = GameMode.Mail;
        };

        _computerRoom.SettingsSelected += () => OpenSettings(GameMode.ComputerRoom);

        _computerRoom.RebootSelected += RebootToModeSelect;

        _programmingManual.ExitSelected += () => _gameMode = _manualReturn;

        _softwareShelf.ExitSelected += () => _gameMode = _manualReturn;

        _magazinesScreen.ExitSelected += () => _gameMode = _manualReturn;
        _magazinesScreen.CareerChanged += PersistActiveCareerAndFeatures;

        _mailScreen.ExitSelected += ReturnToBedroom;

        _settingsScreen.ExitSelected += () => _gameMode = _settingsReturn;

        _settingsScreen.CareerReset += OnCareerReset;
        _settingsScreen.CareerDeleted += OnCareerDeletedFromSettings;
        _settingsScreen.ReplayIntroduction += ReplayHistoricalIntro;
        _settingsScreen.DebugGrantFeature += OnDebugGrantFeature;
        _settingsScreen.DebugOwnMagazineThrough += OnDebugOwnMagazineThrough;

        ApplySettings(_settings);

        if (!_settings.HasSeenIntroduction)
        {
            _historicalIntro.Open();
            _gameMode = GameMode.HistoricalIntro;
        }
        else
        {
            ShowModeSelect();
        }
    }

    private void ReturnToBedroom()
    {
        if (!GameSession.IsBedroom)
        {
            _gameMode = GameMode.SystemMenu;
            return;
        }

        _career.DeliverPendingResponses();
        RefreshBedroomCareerStatus();
        _gameMode = GameMode.ComputerRoom;
    }

    private void WireBootFlow()
    {
        _historicalIntro.Finished += () =>
        {
            if (!_replayIntro && !_settings.HasSeenIntroduction)
            {
                _settings.HasSeenIntroduction = true;
                _settingsService.ReplaceCurrent(_settings);
                _settingsService.Save();
            }

            _replayIntro = false;
            ShowModeSelect();
        };

        _modeSelect.BedroomSlotChosen += slot =>
        {
            var career = _careers.TryLoad(slot);
            if (career == null)
                return;

            BeginFade("LOADING...", () => EnterBedroom(slot, career));
        };

        _modeSelect.NewCareerChosen += slot =>
        {
            _pendingSlot = slot;
            _pendingName = string.Empty;
            _careerName.Open();
            _gameMode = GameMode.CareerName;
        };

        _modeSelect.HardcoreChosen += () =>
            BeginFade("CENTAURI64", EnterHardcore);

        _modeSelect.DeleteSlotRequested += slot =>
        {
            var summary = _careers.LoadSummaries()[slot - 1];
            _deleteSlot = slot;
            _confirmType.Open(
                "DELETE CAREER?",
                "THIS WILL DELETE CAREER PROGRESS FOR:\n\n" +
                summary.Name + " — " + summary.Rank +
                "\n\nYOUR PROGRAMS WILL NOT BE DELETED.",
                "DELETE");
            _gameMode = GameMode.ConfirmType;
        };

        _careerName.Cancelled += ShowModeSelect;
        _careerName.NameAccepted += name =>
        {
            _pendingName = name;
            _bundleSelect.Open();
            _gameMode = GameMode.BundleSelect;
        };

        _bundleSelect.Cancelled += () =>
        {
            _pendingName = string.Empty;
            _careerName.Open();
            _gameMode = GameMode.CareerName;
        };

        _bundleSelect.BundleChosen += CommitNewCareer;

        _systemMenu.ResumeSelected += () => _gameMode = GameMode.Computer;
        _systemMenu.ManualSelected += () => OpenManual(GameMode.SystemMenu);
        _systemMenu.SoftwareSelected += () => OpenSoftware(GameMode.SystemMenu);
        _systemMenu.MagazinesSelected += () => OpenMagazines(GameMode.SystemMenu, true);
        _systemMenu.SettingsSelected += () => OpenSettings(GameMode.SystemMenu);
        _systemMenu.RebootSelected += RebootToModeSelect;

        _confirmType.Cancelled += ShowModeSelect;
        _confirmType.Confirmed += () =>
        {
            _careers.Delete(_deleteSlot);
            ShowModeSelect();
        };
    }

    private void ShowModeSelect()
    {
        GameSession.Clear();
        _modeSelect.Open(_careers.LoadSummaries());
        _gameMode = GameMode.ModeSelect;
        _waitForInputRelease = true;
    }

    private void EnterBedroom(int slot, CareerState career)
    {
        GameSession.EnterBedroom(slot, career);
        RefreshBedroomCareerStatus();
        _computerRoom.ResetPowerState();
        _gameMode = GameMode.ComputerRoom;
        _waitForInputRelease = true;
    }

    private void EnterHardcore()
    {
        GameSession.EnterHardcore();
        _computerRoom.ResetPowerState();
        _computerRoom.SetComputerPoweredOn();
        _gameMode = GameMode.Computer;
        _waitForInputRelease = true;
        _computerPoweringOn = true;
        _powerOnTimer = 0;
        _machine.ResetDisplay();
        _machine.Beep(440, 100);
    }

    private void CommitNewCareer(string bundleId)
    {
        var bundle = ComputerBundleCatalog.Find(bundleId);
        var career = CareerState.CreateNew(_pendingName, bundleId);
        if (bundle != null)
            SoftwareGrant.GrantTapes(bundle.SoftwareTapes);

        career.BundleSoftwareGranted = true;
        _careers.Save(_pendingSlot, career);
        var slot = _pendingSlot;
        _pendingName = string.Empty;
        BeginFade("WELCOME HOME", () => EnterBedroom(slot, career));
    }

    private void RebootToModeSelect()
    {
        PersistActiveCareer();
        CloseEditors();
        BeginFade("REBOOTING...", () =>
        {
            _computerRoom.ResetPowerState();
            ShowModeSelect();
        });
    }

    private void PersistActiveCareer()
    {
        if (GameSession.Career == null || GameSession.ActiveSlot == null)
            return;

        _careers.Save(GameSession.ActiveSlot.Value, GameSession.Career);
    }

    private void CloseEditors()
    {
        if (_machine.SpriteEditor.IsActive)
            _machine.SpriteEditor.Close();
        if (_machine.MapEditor.IsActive)
            _machine.MapEditor.Close();
        if (_machine.ImageEditor.IsActive)
            _machine.ImageEditor.Close();
    }

    private void OpenManual(GameMode returnMode)
    {
        _manualReturn = returnMode;
        _programmingManual.Open();
        _gameMode = GameMode.ProgrammingManual;
    }

    private void OpenSoftware(GameMode returnMode)
    {
        _manualReturn = returnMode;
        _softwareShelf.Show(_basicMachine.GetTapeNames());
        _gameMode = GameMode.Software;
    }

    private void OpenMagazines(GameMode returnMode, bool referenceLibrary)
    {
        _manualReturn = returnMode;
        _magazinesScreen.Open(referenceLibrary);
        _gameMode = GameMode.Magazines;
    }

    private void OpenSettings(GameMode returnMode)
    {
        _settingsReturn = returnMode;
        var label = GameSession.Career == null
            ? string.Empty
            : GameSession.Career.Name + " — " + GameSession.Career.Rank;
        _settingsScreen.Open(GameSession.IsBedroom, label);
        _gameMode = GameMode.Settings;
    }

    private void OnCareerReset()
    {
        if (GameSession.Career == null || GameSession.ActiveSlot == null)
            return;

        GameSession.Career.ResetProgressKeepIdentity();
        _careers.Save(GameSession.ActiveSlot.Value, GameSession.Career);
        GameSession.RefreshFeatures();
        RefreshBedroomCareerStatus();
    }

    private void OnCareerDeletedFromSettings()
    {
        if (GameSession.ActiveSlot == null)
            return;

        var slot = GameSession.ActiveSlot.Value;
        GameSession.Clear();
        _careers.Delete(slot);
        ShowModeSelect();
    }

    private void ReplayHistoricalIntro()
    {
        _replayIntro = true;
        _historicalIntro.Open();
        _gameMode = GameMode.HistoricalIntro;
    }

    private void PersistActiveCareerAndFeatures()
    {
        PersistActiveCareer();
        GameSession.RefreshFeatures();
    }

    private void OnDebugOwnMagazineThrough(int issueNumber)
    {
        if (GameSession.Career == null || GameSession.ActiveSlot == null)
            return;

        if (issueNumber <= 0)
            issueNumber = MagazineProgression.HighestOwnedNumber(GameSession.Career) + 1;

        if (issueNumber > 10)
            return;

        var result = MagazineProgression.OwnThrough(GameSession.Career, issueNumber);
        PersistActiveCareerAndFeatures();

        if (result.NewFeatures.Count > 0)
            ShowUnlock(result.NewFeatures[result.NewFeatures.Count - 1]);
    }

    private void OnDebugGrantFeature(FeatureId feature)
    {
        if (GameSession.Career == null || GameSession.ActiveSlot == null)
            return;

        if (!GameSession.Career.Unlock(feature))
            return;

        _careers.Save(GameSession.ActiveSlot.Value, GameSession.Career);
        GameSession.RefreshFeatures();
        ShowUnlock(feature);
    }

    private void ShowUnlock(FeatureId feature)
    {
        var name = feature switch
        {
            FeatureId.Graphics => "BASIC GRAPHICS",
            FeatureId.Sprites => "SPRITE GRAPHICS",
            FeatureId.Maps => "MAP GRAPHICS",
            FeatureId.Images => "IMAGE GRAPHICS",
            FeatureId.Networking => "NETWORKING",
            _ => feature.ToString().ToUpperInvariant()
        };

        _unlockNotice = "NEW CENTAURI64 FEATURE\n\n" + name;
        _unlockNoticeTimer = 3.5;
    }

    private void BeginFade(string caption, Action after)
    {
        _fadeCaption = caption;
        _afterFade = after;
        _fadeDirection = 1;
        _fade = Math.Max(_fade, 0.01f);
    }

    private void UpdateFade(GameTime gameTime)
    {
        if (_fadeDirection == 0)
            return;

        _fade += _fadeDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * 2.2f;
        if (_fadeDirection > 0 && _fade >= 1f)
        {
            _fade = 1f;
            _afterFade?.Invoke();
            _afterFade = null;
            _fadeDirection = -1;
        }
        else if (_fadeDirection < 0 && _fade <= 0f)
        {
            _fade = 0f;
            _fadeDirection = 0;
            _fadeCaption = string.Empty;
        }
    }

    private bool TryOpenAuthoringTool(FeatureId feature, Action open)
    {
        if (FeatureGate.Current.IsAvailable(feature))
        {
            open();
            return true;
        }

        _lockNotice = FeatureGate.ToolLockedNotice(feature);
        _lockNoticeTimer = 3.5;
        return false;
    }

    private void RefreshBedroomCareerStatus()
    {
        var progress = _career.LoadProgress();
        _computerRoom.SetCareerStatus(
            progress.CashPennies,
            progress.HasUnreadMail);
    }

    private void OnProgramInput(string line)
    {
        _basicMachine.SubmitInput(line);
    }

    private void OnSpriteEditorNotice(string text)
    {
        _console.WriteLine("");
        _console.WriteLine(text);
        _console.WriteLine("");
    }

    private void OnImageEditorNotice(string text)
    {
        _console.WriteLine("");
        _console.WriteLine(text);
        _console.WriteLine("");
    }

    private void OnMapEditorNotice(string text)
    {
        _console.WriteLine("");
        _console.WriteLine(text);
        _console.WriteLine("");
    }

    private void OnComputerSelected()
    {
        _gameMode = GameMode.Computer;
        _waitForInputRelease = true;

        // Already powered on - simply return to the computer.
        if (_computerRoom.ComputerPoweredOn)
        {
            return;
        }

        // First use - perform the real power-on sequence.
        _computerRoom.SetComputerPoweredOn();

        _computerPoweringOn = true;
        _powerOnTimer = 0.0;

        _machine.ResetDisplay();

        _machine.Beep(440, 100);
    }

    private void ApplySettings(CentauriSettings settings)
    {
        _settings = settings;
        _settingsService.ReplaceCurrent(settings);

        var maxScale = GetMaxWindowScale();
        settings.WindowScale = CentauriSettings.ClampScale(settings.WindowScale, maxScale);

        _graphics.SynchronizeWithVerticalRetrace = settings.VSync;
        IsFixedTimeStep = settings.VSync;

        var wantFullscreen = settings.WindowMode == WindowModeSetting.Fullscreen;

        if (_graphics.IsFullScreen != wantFullscreen)
            _graphics.ToggleFullScreen();

        if (!_graphics.IsFullScreen)
        {
            _graphics.PreferredBackBufferWidth =
                CentauriMachine.DEVELOPMENT_WIDTH * settings.WindowScale;

            _graphics.PreferredBackBufferHeight =
                CentauriMachine.DEVELOPMENT_HEIGHT * settings.WindowScale;
        }

        _graphics.ApplyChanges();

        _machine?.SetAudioVolume(settings.EffectiveSfxGain());
    }

    private static int GetMaxWindowScale()
    {
        try
        {
            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var maxW = display.Width / CentauriMachine.DEVELOPMENT_WIDTH;
            var maxH = display.Height / CentauriMachine.DEVELOPMENT_HEIGHT;
            return Math.Clamp(Math.Min(maxW, maxH), 1, 5);
        }
        catch
        {
            return 4;
        }
    }

    private void SetWindowScale(int scale)
    {
        _settings.WindowScale = CentauriSettings.ClampScale(scale, GetMaxWindowScale());
        _settings.WindowMode = WindowModeSetting.Windowed;
        ApplySettings(_settings);
        _settingsService.Save();
    }

    private void ToggleFullscreen()
    {
        _settings.WindowMode = _graphics.IsFullScreen
            ? WindowModeSetting.Windowed
            : WindowModeSetting.Fullscreen;
        ApplySettings(_settings);
        _settingsService.Save();
    }

    private bool KeyPressed(KeyboardState current,Keys key)
    {
        return current.IsKeyDown(key) &&
            _previousKeyboardState.IsKeyUp(key);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (KeyPressed(keyboardState, Keys.F1))
            SetWindowScale(1);

        if (KeyPressed(keyboardState, Keys.F2))
            SetWindowScale(2);

        if (KeyPressed(keyboardState, Keys.F3))
            SetWindowScale(3);

        if (KeyPressed(keyboardState, Keys.F4))
            SetWindowScale(4);

        if (KeyPressed(keyboardState, Keys.F5) &&
            _gameMode == GameMode.Computer &&
            !_basicMachine.IsRunning &&
            !_machine.SpriteEditor.IsActive &&
            !_machine.MapEditor.IsActive &&
            !_machine.ImageEditor.IsActive)
        {
            TryOpenAuthoringTool(FeatureId.Sprites, () => _machine.SpriteEditor.Open());
        }

        if (KeyPressed(keyboardState, Keys.F6) &&
            _gameMode == GameMode.Computer &&
            !_basicMachine.IsRunning &&
            !_machine.SpriteEditor.IsActive &&
            !_machine.MapEditor.IsActive &&
            !_machine.ImageEditor.IsActive)
        {
            TryOpenAuthoringTool(FeatureId.Maps, () => _machine.MapEditor.Open());
        }

        if (KeyPressed(keyboardState, Keys.F7) &&
            _gameMode == GameMode.Computer &&
            !_basicMachine.IsRunning &&
            !_machine.SpriteEditor.IsActive &&
            !_machine.MapEditor.IsActive &&
            !_machine.ImageEditor.IsActive)
        {
            TryOpenAuthoringTool(FeatureId.Images, () => _machine.ImageEditor.Open());
        }
        
        if (KeyPressed(keyboardState, Keys.F11))
            ToggleFullscreen();

        var altDown =
            keyboardState.IsKeyDown(Keys.LeftAlt) ||
            keyboardState.IsKeyDown(Keys.RightAlt);

        if (altDown &&
            KeyPressed(keyboardState, Keys.Enter))
        {
            ToggleFullscreen();
        }

        UpdateFade(gameTime);

        if (_lockNoticeTimer > 0)
            _lockNoticeTimer -= gameTime.ElapsedGameTime.TotalSeconds;

        if (_unlockNoticeTimer > 0)
            _unlockNoticeTimer -= gameTime.ElapsedGameTime.TotalSeconds;

        if (_fadeDirection != 0)
        {
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        var virtualMouse = GetVirtualMouse(
            CentauriMachine.DEVELOPMENT_WIDTH,
            CentauriMachine.DEVELOPMENT_HEIGHT);

        if (_gameMode == GameMode.HistoricalIntro)
        {
            _historicalIntro.Update(gameTime);
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.ModeSelect)
        {
            if (_waitForInputRelease)
            {
                if (keyboardState.GetPressedKeyCount() == 0)
                    _waitForInputRelease = false;
            }
            else
            {
                _modeSelect.Update(gameTime, virtualMouse);
            }

            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.CareerName)
        {
            _careerName.Update(gameTime);
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.BundleSelect)
        {
            _bundleSelect.Update(gameTime, GetMetaMouse());
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.SystemMenu)
        {
            _systemMenu.Update(gameTime);
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.ConfirmType)
        {
            _confirmType.Update(gameTime);
            _previousKeyboardState = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.ComputerRoom)
        {
            _computerRoom.Update(gameTime);

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.ProgrammingManual)
        {
            _programmingManual.Update();

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.Software)
        {
            _softwareShelf.Update(
                gameTime,
                GetVirtualMouse(CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT));

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.Magazines)
        {
            _magazinesScreen.Update(gameTime, GetMetaMouse());

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.Mail)
        {
            _mailScreen.Update(gameTime);

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.Settings)
        {
            _settingsScreen.Update(gameTime);

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_computerPoweringOn)
        {
            _powerOnTimer +=
                gameTime.ElapsedGameTime.TotalSeconds;

            if (_powerOnTimer >= POWER_ON_DELAY)
            {
                _computerPoweringOn = false;

                _basicMachine.ShowBootMessage();
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_waitForInputRelease)
        {
            if (keyboardState.GetPressedKeyCount() == 0)
            {
                _waitForInputRelease = false;
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (KeyPressed(keyboardState, Keys.F12))
        {
            if (GameSession.IsHardcore)
            {
                CloseEditors();
                _systemMenu.Open();
                _gameMode = GameMode.SystemMenu;
            }
            else
            {
                ReturnToBedroom();
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_basicMachine.IsRunning &&
            keyboardState.IsKeyDown(Keys.Escape))
        {
            _basicMachine.Stop();

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_basicMachine.ProgramFinished)
        {
            // The key that launched RUN may still be held.
            // Don't allow dismissal until every key has been released.
            if (!_finishedProgramInputArmed)
            {
                if (keyboardState.GetPressedKeyCount() == 0)
                {
                    _finishedProgramInputArmed = true;
                }

                _previousKeyboardState = keyboardState;

                base.Update(gameTime);
                return;
            }

            // We've seen a completely released keyboard.
            // The next key press dismisses the finished program.
            if (keyboardState.GetPressedKeyCount() > 0)
            {
                _basicMachine.DismissFinishedProgram();

                _finishedProgramInputArmed = false;
                _waitForInputRelease = true;

                _previousKeyboardState = keyboardState;

                base.Update(gameTime);
                return;
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        _machine.UpdateInput();
        _machine.UpdateSprites(gameTime);

        if (!_basicMachine.IsRunning &&
            !_machine.SpriteEditor.IsActive &&
            !_machine.MapEditor.IsActive &&
            !_machine.ImageEditor.IsActive)
        {
            _console.Update(gameTime);
        }

        _network.BeginFrame();
        _basicMachine.Update();
        _network.EndFrame();

        if (_basicMachine.IsWaitingForInput &&
            !_machine.SpriteEditor.IsActive &&
            !_machine.MapEditor.IsActive &&
            !_machine.ImageEditor.IsActive)
        {
            if (_machine.DisplayMode == CentauriDisplayMode.Console)
                _console.Update(gameTime);
            else
                _programConsole.Update(gameTime);
        }

        if (_machine.SpriteEditor.IsActive ||
            _machine.MapEditor.IsActive ||
            _machine.ImageEditor.IsActive)
        {
            if (_machine.SpriteEditor.IsActive)
            {
                _machine.UpdateSpriteEditor(
                    gameTime,
                    virtualMouse,
                    keyboardState,
                    _previousKeyboardState);
            }
            else if (_machine.MapEditor.IsActive)
            {
                _machine.UpdateMapEditor(
                    gameTime,
                    virtualMouse,
                    keyboardState,
                    _previousKeyboardState);
            }
            else
            {
                _machine.UpdateImageEditor(
                    gameTime,
                    virtualMouse,
                    keyboardState,
                    _previousKeyboardState,
                    _settings,
                    () => _settingsService.Save());
            }
        }

        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    private void DrawComputerRoom()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _computerRoom.Draw(_spriteBatch);

        DrawSessionOverlaysOnTarget();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    private void DrawBootScreen(Action<SpriteBatch> draw)
    {
        GraphicsDevice.SetRenderTarget(_codeEditorRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        draw(_spriteBatch);
        DrawSessionOverlaysOnTarget();
        GraphicsDevice.SetRenderTarget(null);
        DrawRenderTargetToWindow(_codeEditorRenderTarget);
    }

    private void DrawProgrammingManual()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _programmingManual.Draw(_spriteBatch);

        DrawSessionOverlaysOnTarget();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    private void DrawSoftwareShelf()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _softwareShelf.Draw(_spriteBatch);

        DrawSessionOverlaysOnTarget();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    private void DrawMagazines()
    {
        DrawMetaScreen((spriteBatch, transform) =>
            _magazinesScreen.Draw(spriteBatch, transform));
    }

    private void DrawMail()
    {
        GraphicsDevice.SetRenderTarget(_codeEditorRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        _mailScreen.Draw(_spriteBatch);
        GraphicsDevice.SetRenderTarget(null);
        DrawRenderTargetToWindow(_codeEditorRenderTarget);
    }

    private void DrawSettings()
    {
        GraphicsDevice.SetRenderTarget(_codeEditorRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        _settingsScreen.Draw(_spriteBatch);
        GraphicsDevice.SetRenderTarget(null);
        DrawRenderTargetToWindow(_codeEditorRenderTarget);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_computerPoweringOn)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.HistoricalIntro)
        {
            DrawBootScreen(sb => _historicalIntro.Draw(sb));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ModeSelect)
        {
            DrawBootScreen(sb => _modeSelect.Draw(sb));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.CareerName)
        {
            DrawBootScreen(sb => _careerName.Draw(sb));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.BundleSelect)
        {
            DrawMetaScreen((sb, transform) => _bundleSelect.Draw(sb, transform));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.SystemMenu)
        {
            DrawBootScreen(sb => _systemMenu.Draw(sb));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ConfirmType)
        {
            DrawBootScreen(sb => _confirmType.Draw(sb));
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ComputerRoom)
        {
            DrawComputerRoom();
            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ProgrammingManual)
        {
            DrawProgrammingManual();

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.Software)
        {
            DrawSoftwareShelf();

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.Magazines)
        {
            DrawMagazines();

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.Mail)
        {
            DrawMail();

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.Settings)
        {
            DrawSettings();

            base.Draw(gameTime);
            return;
        }

        if (_basicMachine.IsRunning || _basicMachine.ProgramFinished)
        {
            switch (_machine.DisplayMode)
            {
                case CentauriDisplayMode.Console:
                    DrawCodeEditor();
                    break;

                case CentauriDisplayMode.HighResolution:
                    DrawTextMode();
                    break;

                case CentauriDisplayMode.Arcade:
                    DrawGame();
                    break;
            }
        }
        else
        {
            DrawCodeEditor();
        }

        base.Draw(gameTime);
    }

    private void DrawTextMode()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        // paper → IMAGE/BG0/BG1 → graphics → tiles → sprites → FG → text
        _machine.DrawBackgroundImages(_spriteBatch, _pixel);

        _machine.DrawGraphics(
            _spriteBatch,
            _pixel);

        _machine.DrawTiles(
            _spriteBatch,
            _pixel);

        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        _machine.DrawForegroundImage(_spriteBatch, _pixel);

        _programConsole.Draw(
                                _spriteBatch,
                                _font,
                                _pixel,
                                Color.White,
                                new Color(40, 40, 160),
                                drawBackground: false,
                                drawCursor: !_basicMachine.IsRunning || _basicMachine.IsWaitingForInput,
                                visibleRows: 60);

        _machine.DrawText(
            _spriteBatch,
            _font);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget,
            applyCrt: true);
    }

    private void DrawCodeEditor()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        if (_machine.SpriteEditor.IsActive)
        {
            _machine.DrawSpriteEditor(
                _spriteBatch,
                _font,
                _pixel);
        }
        else if (_machine.MapEditor.IsActive)
        {
            _machine.DrawMapEditor(
                _spriteBatch,
                _font,
                _pixel);
        }
        else if (_machine.ImageEditor.IsActive)
        {
            _machine.DrawImageEditor(
                _spriteBatch,
                _font,
                _pixel);
        }
        else
        {
            DrawEditorChrome();

           _console.Draw(_spriteBatch,_font,_pixel,EditorText,EditorBackground,drawBackground: false,drawCursor: true,offsetY: 24,  visibleRows: CODE_EDITOR_ROWS );

            _machine.DrawSprites(
                _spriteBatch,
                _pixel);
        }

        _spriteBatch.End();

        DrawSessionOverlaysOnTarget();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(_codeEditorRenderTarget, applyCrt: true);
    }

    private void DrawGame()
    {
        GraphicsDevice.SetRenderTarget(
            _gameRenderTarget);

        // BORDER
        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        // PAPER
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,0,
                CentauriMachine.ARCADE_WIDTH,
                CentauriMachine.ARCADE_HEIGHT),
            CentauriPalette.Get(_programConsole.Background));

        // IMAGE / BG0 / BG1
        _machine.DrawBackgroundImages(_spriteBatch, _pixel);

        // GRAPHICS
        _machine.DrawGraphics(
            _spriteBatch,
            _pixel);

        // TILES
        _machine.DrawTiles(
            _spriteBatch,
            _pixel);

        // SPRITES
        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        // FOREGROUND
        _machine.DrawForegroundImage(_spriteBatch, _pixel);

        _programConsole.Draw(_spriteBatch,_font,_pixel,Color.White,CentauriPalette.Get(_programConsole.Background),drawBackground: false, drawCursor: !_basicMachine.IsRunning || _basicMachine.IsWaitingForInput, visibleRows: 30);

        // HUD / PRINTAT
        _machine.DrawText(
            _spriteBatch,
            _font);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _gameRenderTarget,
            applyCrt: true);
    }

    private MouseState GetMetaMouse()
    {
        var viewport = GraphicsDevice.Viewport;
        return MetaUi.ToLogical(
            Mouse.GetState(),
            MetaUi.Destination(viewport.Width, viewport.Height));
    }

    private void DrawMetaScreen(Action<SpriteBatch, Matrix> draw)
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(new Color(18, 14, 10));
        var viewport = GraphicsDevice.Viewport;
        var destination = MetaUi.Destination(viewport.Width, viewport.Height);
        draw(_spriteBatch, MetaUi.Transform(destination));

        if (_fade <= 0f)
            return;

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * _fade);
        if (_fade >= 0.55f && !string.IsNullOrEmpty(_fadeCaption))
        {
            var x = (viewport.Width - _fadeCaption.Length * 8) / 2;
            var y = viewport.Height / 2 - 8;
            _font.Draw(_spriteBatch, _fadeCaption, new Vector2(x, y), Color.White);
        }

        _spriteBatch.End();
    }

    private MouseState GetVirtualMouse(int virtualWidth, int virtualHeight)
    {
        var mouse = Mouse.GetState();
        var viewportWidth = GraphicsDevice.Viewport.Width;
        var viewportHeight = GraphicsDevice.Viewport.Height;
        var scale = MathF.Min(
            viewportWidth / (float)virtualWidth,
            viewportHeight / (float)virtualHeight);
        var destinationWidth = (int)(virtualWidth * scale);
        var destinationHeight = (int)(virtualHeight * scale);
        var destinationX = (viewportWidth - destinationWidth) / 2;
        var destinationY = (viewportHeight - destinationHeight) / 2;
        var x = (int)((mouse.X - destinationX) / scale);
        var y = (int)((mouse.Y - destinationY) / scale);

        return new MouseState(
            x,
            y,
            mouse.ScrollWheelValue,
            mouse.LeftButton,
            mouse.MiddleButton,
            mouse.RightButton,
            mouse.XButton1,
            mouse.XButton2);
    }

    private void DrawRenderTargetToWindow(
        RenderTarget2D renderTarget,
        bool applyCrt = false)
    {
        GraphicsDevice.Clear(Color.Black);

        var viewportWidth =
            GraphicsDevice.Viewport.Width;

        var viewportHeight =
            GraphicsDevice.Viewport.Height;

        var scaleX =
            viewportWidth / (float)renderTarget.Width;

        var scaleY =
            viewportHeight / (float)renderTarget.Height;

        var scale = MathF.Min(scaleX, scaleY);

        if (_settings.ScalingMode == ScalingModeSetting.PixelPerfect)
        {
            var integerScale = MathF.Floor(scale);
            scale = integerScale < 1f ? scale : integerScale;
        }

        var destinationWidth =
            (int)(renderTarget.Width * scale);

        var destinationHeight =
            (int)(renderTarget.Height * scale);

        var destinationX =
            (viewportWidth - destinationWidth) / 2;

        var destinationY =
            (viewportHeight - destinationHeight) / 2;

        var destinationRectangle =
            new Rectangle(
                destinationX,
                destinationY,
                destinationWidth,
                destinationHeight);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(
            renderTarget,
            destinationRectangle,
            Color.White);

        if (applyCrt &&
            _settings.CrtFilter != CrtPreset.Off)
        {
            DrawCrtOverlay(destinationRectangle, _settings.CrtFilter);
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Lightweight CRT presentation for the fictional monitor only.
    /// Keeps text readable — no heavy blur or flicker.
    /// </summary>
    private void DrawCrtOverlay(Rectangle destination, CrtPreset preset)
    {
        var lineAlpha = preset == CrtPreset.Subtle ? 20 : 42;
        var step = preset == CrtPreset.Subtle ? 3 : 2;
        var lineColour = new Color((byte)0, (byte)0, (byte)0, (byte)lineAlpha);

        for (var y = destination.Y; y < destination.Bottom; y += step)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(destination.X, y, destination.Width, 1),
                lineColour);
        }

        if (preset != CrtPreset.NineteenEightySix)
            return;

        var edge = Math.Max(6, destination.Width / 28);
        var vignette = new Color((byte)0, (byte)0, (byte)0, (byte)55);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(destination.X, destination.Y, edge, destination.Height),
            vignette);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(destination.Right - edge, destination.Y, edge, destination.Height),
            vignette);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(destination.X, destination.Y, destination.Width, edge / 2),
            vignette);

        _spriteBatch.Draw(
            _pixel,
            new Rectangle(destination.X, destination.Bottom - edge / 2, destination.Width, edge / 2),
            vignette);
    }

    private void DrawEditorChrome()
    {
        // Main editor background.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            EditorBackground);

        // Header.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                24),
            EditorFrame);

       _font.Draw(
            _spriteBatch,
            "CENTAURI64 BASIC",
            new Vector2(16, 8),
            Color.White);

        var memoryText =
            $"{_basicMachine.BasicMemoryFree} BASIC BYTES FREE";

        var memoryX =
            (CentauriMachine.DEVELOPMENT_WIDTH -
            memoryText.Length * 8) / 2;

        _font.Draw(
            _spriteBatch,
            memoryText,
            new Vector2(memoryX, 8),
            EditorAccent);

        _font.Draw(
            _spriteBatch,
            "64K",
            new Vector2(
                CentauriMachine.DEVELOPMENT_WIDTH - 40,
                8),
            EditorAccent);

        // Footer.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                CentauriMachine.DEVELOPMENT_HEIGHT - 16,
                CentauriMachine.DEVELOPMENT_WIDTH,
                16),
            EditorFrame);

        _font.Draw(
            _spriteBatch,
            "EDIT",
            new Vector2(
                16,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            EditorAccent);

        _font.Draw(
            _spriteBatch,
            BuildEditorShortcutLine(),
            new Vector2(
                160,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            Color.White);

        _font.Draw(
            _spriteBatch,
            GameSession.IsHardcore ? "F12 SYSTEM" : "F12 BEDROOM",
            new Vector2(
                520,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            Color.White);
    }

    private string BuildEditorShortcutLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (FeatureGate.Current.IsAvailable(FeatureId.Sprites))
            parts.Add("F5 SPRITES");
        if (FeatureGate.Current.IsAvailable(FeatureId.Maps))
            parts.Add("F6 MAPS");
        if (FeatureGate.Current.IsAvailable(FeatureId.Images))
            parts.Add("F7 IMAGES");
        return string.Join("  ", parts);
    }

    private void DrawSessionOverlaysOnTarget()
    {
        if (_fade <= 0f &&
            _lockNoticeTimer <= 0 &&
            _unlockNoticeTimer <= 0)
            return;

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_unlockNoticeTimer > 0)
            DrawNoticePanel(_unlockNotice);

        if (_lockNoticeTimer > 0)
            DrawNoticePanel(_lockNotice);

        if (_fade > 0f)
        {
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    0,
                    0,
                    CentauriMachine.DEVELOPMENT_WIDTH,
                    CentauriMachine.DEVELOPMENT_HEIGHT),
                Color.Black * _fade);

            if (_fade >= 0.55f && !string.IsNullOrEmpty(_fadeCaption))
            {
                var x = (CentauriMachine.DEVELOPMENT_WIDTH - _fadeCaption.Length * 8) / 2;
                _font.Draw(_spriteBatch, _fadeCaption, new Vector2(x, 220), Color.White);
            }
        }

        _spriteBatch.End();
    }

    private void DrawNoticePanel(string text)
    {
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(80, 140, 480, 160),
            new Color(14, 28, 38, 230));

        var y = 164;
        foreach (var line in text.Split('\n'))
        {
            var x = (CentauriMachine.DEVELOPMENT_WIDTH - line.Length * 8) / 2;
            _font.Draw(_spriteBatch, line, new Vector2(x, y), BootUi.Yellow);
            y += 20;
        }
    }
}
