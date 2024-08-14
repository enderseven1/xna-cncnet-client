using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
// using System.Reflection.Metadata;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// The main menu of the client.
    /// </summary>
    class MainMenu : XNAWindow, ISwitchable
    {
        private const float MEDIA_PLAYER_VOLUME_FADE_STEP = 0.01f;
        private const float MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP = 0.025f;
        private const double UPDATE_RE_CHECK_THRESHOLD = 30.0;

        /// <summary>
        /// Creates a new instance of the main menu.
        /// </summary>
        public MainMenu(
            WindowManager windowManager,
            SkirmishLobby skirmishLobby,
            LANLobby lanLobby,
            TopBar topBar,
            OptionsWindow optionsWindow,
            CnCNetLobby cncnetLobby,
            CnCNetManager connectionManager,
            DiscordHandler discordHandler,
            CnCNetGameLoadingLobby cnCNetGameLoadingLobby,
            CnCNetGameLobby cnCNetGameLobby,
            PrivateMessagingPanel privateMessagingPanel,
            PrivateMessagingWindow privateMessagingWindow,
            GameInProgressWindow gameInProgressWindow,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.lanLobby = lanLobby;
            this.topBar = topBar;
            this.connectionManager = connectionManager;
            this.optionsWindow = optionsWindow;
            this.cncnetLobby = cncnetLobby;
            this.discordHandler = discordHandler;
            this.skirmishLobby = skirmishLobby;
            this.cnCNetGameLoadingLobby = cnCNetGameLoadingLobby;
            this.cnCNetGameLobby = cnCNetGameLobby;
            this.privateMessagingPanel = privateMessagingPanel;
            this.privateMessagingWindow = privateMessagingWindow;
            this.gameInProgressWindow = gameInProgressWindow;
            this.mapLoader = mapLoader;
            this.cncnetLobby.UpdateCheck += CncnetLobby_UpdateCheck;
            isMediaPlayerAvailable = IsMediaPlayerAvailable();
        }

        private MainMenuDarkeningPanel innerPanel;

        private XNALabel lblCnCNetPlayerCount;
        private XNALinkLabel lblUpdateStatus;
        private XNALinkLabel lblVersion;

        private CnCNetLobby cncnetLobby;

        private SkirmishLobby skirmishLobby;

        private LANLobby lanLobby;

        private CnCNetManager connectionManager;

        private OptionsWindow optionsWindow;

        private DiscordHandler discordHandler;

        private TopBar topBar;
        private readonly CnCNetGameLoadingLobby cnCNetGameLoadingLobby;
        private readonly CnCNetGameLobby cnCNetGameLobby;
        private readonly PrivateMessagingPanel privateMessagingPanel;
        private readonly PrivateMessagingWindow privateMessagingWindow;
        private readonly GameInProgressWindow gameInProgressWindow;
        private readonly MapLoader mapLoader;

        private XNAMessageBox firstRunMessageBox;
        private XNAMessageBox firstRunMessageBox2;
        private XNAMessageBox fangChenmiBox;

        private XNASuggestionTextBox dd年;
        private XNASuggestionTextBox dd月;
        private XNASuggestionTextBox dd日;

        private bool _updateInProgress;
        private bool UpdateInProgress
        {
            get { return _updateInProgress; }
            set
            {
                _updateInProgress = value;
                topBar.SetSwitchButtonsClickable(!_updateInProgress);
                topBar.SetOptionsButtonClickable(!_updateInProgress);
                SetButtonHotkeys(!_updateInProgress);
            }
        }

        private bool customComponentDialogQueued = false;

        private DateTime lastUpdateCheckTime;

        private Song themeSong;

        private static readonly object locker = new object();

        private bool isMusicFading = false;
        
        public static int age = 18;

        private readonly bool isMediaPlayerAvailable;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;

        // Main Menu Buttons
        private XNAClientButton btnNewCampaign;
        private XNAClientButton btnLoadGame;
        private XNAClientButton btnSkirmish;
        private XNAClientButton btnCnCNet;
        private XNAClientButton btnLan;
        private XNAClientButton btnOptions;
        private XNAClientButton btnMapEditor;
        private XNAClientButton btnStatistics;
        private XNAClientButton btnCredits;
        private XNAClientButton btnExtras;

        /// <summary>
        /// Initializes the main menu's controls.
        /// </summary>
        public override void Initialize()
        {
            topBar.SetSecondarySwitch(cncnetLobby);
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            Name = nameof(MainMenu);
            BackgroundTexture = AssetLoader.LoadTexture("MainMenu/mainmenubg.png");
            ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, BackgroundTexture.Width, BackgroundTexture.Height);

            WindowManager.CenterControlOnScreen(this);

            btnNewCampaign = new XNAClientButton(WindowManager);
            btnNewCampaign.Name = nameof(btnNewCampaign);
            btnNewCampaign.Text = "Campaign".L10N("Client:Main:NewCampaign");
            btnNewCampaign.IdleTexture = AssetLoader.LoadTexture("MainMenu/campaign.png");
            btnNewCampaign.HoverTexture = AssetLoader.LoadTexture("MainMenu/campaign_c.png");
            btnNewCampaign.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnNewCampaign.LeftClick += BtnNewCampaign_LeftClick;

            btnLoadGame = new XNAClientButton(WindowManager);
            btnLoadGame.Name = nameof(btnLoadGame);
            btnLoadGame.Text = "Load Game".L10N("Client:Main:LoadGame");
            btnLoadGame.IdleTexture = AssetLoader.LoadTexture("MainMenu/loadmission.png");
            btnLoadGame.HoverTexture = AssetLoader.LoadTexture("MainMenu/loadmission_c.png");
            btnLoadGame.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

            btnSkirmish = new XNAClientButton(WindowManager);
            btnSkirmish.Name = nameof(btnSkirmish);
            btnSkirmish.Text = "Skirmish".L10N("Client:Main:Skirmish");
            btnSkirmish.IdleTexture = AssetLoader.LoadTexture("MainMenu/skirmish.png");
            btnSkirmish.HoverTexture = AssetLoader.LoadTexture("MainMenu/skirmish_c.png");
            btnSkirmish.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnSkirmish.LeftClick += BtnSkirmish_LeftClick;

            btnCnCNet = new XNAClientButton(WindowManager);
            btnCnCNet.Name = nameof(btnCnCNet);
            btnCnCNet.Text = "CnCNet".L10N("Client:Main:CnCNet");
            btnCnCNet.IdleTexture = AssetLoader.LoadTexture("MainMenu/cncnet.png");
            btnCnCNet.HoverTexture = AssetLoader.LoadTexture("MainMenu/cncnet_c.png");
            btnCnCNet.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnCnCNet.LeftClick += BtnCnCNet_LeftClick;

            btnLan = new XNAClientButton(WindowManager);
            btnLan.Name = nameof(btnLan);
            btnLan.Text = "LAN".L10N("Client:Main:LAN");
            btnLan.IdleTexture = AssetLoader.LoadTexture("MainMenu/lan.png");
            btnLan.HoverTexture = AssetLoader.LoadTexture("MainMenu/lan_c.png");
            btnLan.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnLan.LeftClick += BtnLan_LeftClick;

            btnOptions = new XNAClientButton(WindowManager);
            btnOptions.Name = nameof(btnOptions);
            btnOptions.Text = "Options".L10N("Client:Main:Options");
            btnOptions.IdleTexture = AssetLoader.LoadTexture("MainMenu/options.png");
            btnOptions.HoverTexture = AssetLoader.LoadTexture("MainMenu/options_c.png");
            btnOptions.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnOptions.LeftClick += BtnOptions_LeftClick;

            btnMapEditor = new XNAClientButton(WindowManager);
            btnMapEditor.Name = nameof(btnMapEditor);
            btnMapEditor.Text = "Map Editor".L10N("Client:Main:MapEditor");
            btnMapEditor.IdleTexture = AssetLoader.LoadTexture("MainMenu/mapeditor.png");
            btnMapEditor.HoverTexture = AssetLoader.LoadTexture("MainMenu/mapeditor_c.png");
            btnMapEditor.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnMapEditor.LeftClick += BtnMapEditor_LeftClick;

            btnStatistics = new XNAClientButton(WindowManager);
            btnStatistics.Name = nameof(btnStatistics);
            btnStatistics.Text = "Statistics".L10N("Client:Main:Statistics");
            btnStatistics.IdleTexture = AssetLoader.LoadTexture("MainMenu/statistics.png");
            btnStatistics.HoverTexture = AssetLoader.LoadTexture("MainMenu/statistics_c.png");
            btnStatistics.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnStatistics.LeftClick += BtnStatistics_LeftClick;

            btnCredits = new XNAClientButton(WindowManager);
            btnCredits.Name = nameof(btnCredits);
            btnCredits.Text = "View Credits".L10N("Client:Main:ViewCredits");
            btnCredits.IdleTexture = AssetLoader.LoadTexture("MainMenu/credits.png");
            btnCredits.HoverTexture = AssetLoader.LoadTexture("MainMenu/credits_c.png");
            btnCredits.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnCredits.LeftClick += BtnCredits_LeftClick;

            btnExtras = new XNAClientButton(WindowManager);
            btnExtras.Name = nameof(btnExtras);
            btnExtras.Text = "Extras".L10N("Client:Main:Extras");
            btnExtras.IdleTexture = AssetLoader.LoadTexture("MainMenu/extras.png");
            btnExtras.HoverTexture = AssetLoader.LoadTexture("MainMenu/extras_c.png");
            btnExtras.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnExtras.LeftClick += BtnExtras_LeftClick;

            var btnExit = new XNAClientButton(WindowManager);
            btnExit.Name = nameof(btnExit);
            btnExit.Text = "Exit".L10N("Client:Main:Exit");
            btnExit.IdleTexture = AssetLoader.LoadTexture("MainMenu/exitgame.png");
            btnExit.HoverTexture = AssetLoader.LoadTexture("MainMenu/exitgame_c.png");
            btnExit.HoverSoundEffect = new EnhancedSoundEffect("MainMenu/button.wav");
            btnExit.LeftClick += BtnExit_LeftClick;

            XNALabel lblCnCNetStatus = new XNALabel(WindowManager);
            lblCnCNetStatus.Name = nameof(lblCnCNetStatus);
            lblCnCNetStatus.Text = "Players Online:".L10N("Client:Main:CnCNetOnlinePlayersCountText");
            //lblCnCNetStatus.Text = "DTA players on CnCNet:".L10N("Client:Main:CnCNetOnlinePlayersCountText");
            lblCnCNetStatus.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(12, 9, 0, 0);

            lblCnCNetPlayerCount = new XNALabel(WindowManager);
            lblCnCNetPlayerCount.Name = nameof(lblCnCNetPlayerCount);
            lblCnCNetPlayerCount.Text = "-";

            lblVersion = new XNALinkLabel(WindowManager);
            lblVersion.Name = nameof(lblVersion);
            lblVersion.LeftClick += LblVersion_LeftClick;

            lblUpdateStatus = new XNALinkLabel(WindowManager);
            lblUpdateStatus.Name = nameof(lblUpdateStatus);
            lblUpdateStatus.Text = "Updateing".L10N("Client:Main:UpdateStatus");
            lblUpdateStatus.LeftClick += LblUpdateStatus_LeftClick;
            lblUpdateStatus.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, UIDesignConstants.BUTTON_WIDTH_160, 20);

            AddChild(btnNewCampaign);
            AddChild(btnLoadGame);
            AddChild(btnSkirmish);
            AddChild(btnCnCNet);
            AddChild(btnLan);
            AddChild(btnOptions);
            AddChild(btnMapEditor);
            AddChild(btnStatistics);
            AddChild(btnCredits);
            AddChild(btnExtras);
            AddChild(btnExit);
            AddChild(lblCnCNetStatus);
            AddChild(lblCnCNetPlayerCount);

            if (!ClientConfiguration.Instance.ModMode)
            {
                // ModMode disables version tracking and the updater if it's enabled

                AddChild(lblVersion);
                AddChild(lblUpdateStatus);

                Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
                Updater.OnCustomComponentsOutdated += Updater_OnCustomComponentsOutdated;
            }

            base.Initialize(); // Read control attributes from INI

            innerPanel = new MainMenuDarkeningPanel(WindowManager, discordHandler, mapLoader);
            innerPanel.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0,
                Width,
                Height);
            innerPanel.DrawOrder = int.MaxValue;
            innerPanel.UpdateOrder = int.MaxValue;
            AddChild(innerPanel);
            innerPanel.Hide();

            lblVersion.Text = Updater.GameVersion;

            innerPanel.UpdateQueryWindow.UpdateDeclined += UpdateQueryWindow_UpdateDeclined;
            innerPanel.UpdateQueryWindow.UpdateAccepted += UpdateQueryWindow_UpdateAccepted;
            innerPanel.ManualUpdateQueryWindow.Closed += ManualUpdateQueryWindow_Closed;

            innerPanel.UpdateWindow.UpdateCompleted += UpdateWindow_UpdateCompleted;
            innerPanel.UpdateWindow.UpdateCancelled += UpdateWindow_UpdateCancelled;
            innerPanel.UpdateWindow.UpdateFailed += UpdateWindow_UpdateFailed;

            ClientRectangle = new Microsoft.Xna.Framework.Rectangle((WindowManager.RenderResolutionX - Width) / 2,
                (WindowManager.RenderResolutionY - Height) / 2,
                Width, Height);
            innerPanel.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0,
                Math.Max(WindowManager.RenderResolutionX, Width),
                Math.Max(WindowManager.RenderResolutionY, Height));

            CnCNetPlayerCountTask.CnCNetGameCountUpdated += CnCNetInfoController_CnCNetGameCountUpdated;
            cncnetPlayerCountCancellationSource = new CancellationTokenSource();
            CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);

            WindowManager.GameClosing += WindowManager_GameClosing;

            skirmishLobby.Exited += SkirmishLobby_Exited;
            lanLobby.Exited += LanLobby_Exited;
            optionsWindow.EnabledChanged += OptionsWindow_EnabledChanged;

            optionsWindow.OnForceUpdate += (s, e) => ForceUpdate();

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessStarting += SharedUILogic_GameProcessStarting;

            UserINISettings.Instance.SettingsSaved += SettingsSaved;

            Updater.Restart += Updater_Restart;

            SetButtonHotkeys(true);
        }

        private void SetButtonHotkeys(bool enableHotkeys)
        {
            if (!Initialized)
                return;

            if (enableHotkeys)
            {
                btnNewCampaign.HotKey = Keys.C;
                btnLoadGame.HotKey = Keys.L;
                btnSkirmish.HotKey = Keys.S;
                btnCnCNet.HotKey = Keys.M;
                btnLan.HotKey = Keys.N;
                btnOptions.HotKey = Keys.O;
                btnMapEditor.HotKey = Keys.E;
                btnStatistics.HotKey = Keys.T;
                btnCredits.HotKey = Keys.R;
                btnExtras.HotKey = Keys.X;
            }
            else
            {
                btnNewCampaign.HotKey = Keys.None;
                btnLoadGame.HotKey = Keys.None;
                btnSkirmish.HotKey = Keys.None;
                btnCnCNet.HotKey = Keys.None;
                btnLan.HotKey = Keys.None;
                btnOptions.HotKey = Keys.None;
                btnMapEditor.HotKey = Keys.None;
                btnStatistics.HotKey = Keys.None;
                btnCredits.HotKey = Keys.None;
                btnExtras.HotKey = Keys.None;
            }
        }

        private void OptionsWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (!optionsWindow.Enabled)
            {
                if (customComponentDialogQueued)
                    Updater_OnCustomComponentsOutdated();
            }
        }

        /// <summary>
        /// Refreshes settings. Called when the game process is starting.
        /// </summary>
        private void SharedUILogic_GameProcessStarting()
        {
            UserINISettings.Instance.ReloadSettings();

            try
            {
                optionsWindow.RefreshSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Refreshing settings failed! Exception message: " + ex.ToString());
                // We don't want to show the dialog when starting a game
                //XNAMessageBox.Show(WindowManager, "Saving settings failed",
                //    "Saving settings failed! Error message: " + ex.Message);
            }
        }

        private void Updater_Restart(object sender, EventArgs e) =>
            WindowManager.AddCallback(new Action(ExitClient), null);

        /// <summary>
        /// Applies configuration changes (music playback and volume)
        /// when settings are saved.
        /// </summary>
        private void SettingsSaved(object sender, EventArgs e)
        {
            if (isMediaPlayerAvailable)
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    if (!UserINISettings.Instance.PlayMainMenuMusic)
                        isMusicFading = true;
                }
                else if (topBar.GetTopMostPrimarySwitchable() == this &&
                    topBar.LastSwitchType == SwitchType.PRIMARY)
                {
                    PlayMusic();
                }
            }

            if (!connectionManager.IsConnected)
                ProgramConstants.PLAYERNAME = UserINISettings.Instance.PlayerName;

            if (UserINISettings.Instance.DiscordIntegration && !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled)
                discordHandler.Connect();
            else
                discordHandler.Disconnect();
        }

        /// <summary>
        /// Checks files which are required for the mod to function
        /// but not distributed with the mod (usually base game files
        /// for YR mods which can't be standalone).
        /// </summary>
        private void CheckRequiredFiles()
        {
            List<string> absentFiles = ClientConfiguration.Instance.RequiredFiles.ToList()
                .FindAll(f => !string.IsNullOrWhiteSpace(f) && !SafePath.GetFile(ProgramConstants.GamePath, f).Exists);

            if (absentFiles.Count > 0)
                XNAMessageBox.Show(WindowManager, "Missing Files".L10N("Client:Main:MissingFilesTitle"),
#if ARES
                    ("You are missing Yuri's Revenge files that are required\n" +
                    "to play this mod! Yuri's Revenge mods are not standalone,\n" +
                    "so you need a copy of following Yuri's Revenge (v. 1.001)\n" +
                    "files placed in the mod folder to play the mod:").L10N("Client:Main:MissingFilesText1Ares") +
#else
                    "The following required files are missing:".L10N("Client:Main:MissingFilesText1NonAres") +
#endif
                    Environment.NewLine + Environment.NewLine +
                    String.Join(Environment.NewLine, absentFiles) +
                    Environment.NewLine + Environment.NewLine +
                    "You won't be able to play without those files.".L10N("Client:Main:MissingFilesText2"));
        }

        private void CheckForbiddenFiles()
        {
            List<string> presentFiles = ClientConfiguration.Instance.ForbiddenFiles.ToList()
                .FindAll(f => !string.IsNullOrWhiteSpace(f) && SafePath.GetFile(ProgramConstants.GamePath, f).Exists);

            if (presentFiles.Count > 0)
                XNAMessageBox.Show(WindowManager, "Interfering Files Detected".L10N("Client:Main:InterferingFilesDetectedTitle"),
#if TS
                    ("You have installed the mod on top of a Tiberian Sun\n" +
                    "copy! This mod is standalone, therefore you have to\n" +
                    "install it in an empty folder. Otherwise the mod won't\n" +
                    "function correctly.\n\n" +
                    "Please reinstall the mod into an empty folder to play.").L10N("Client:Main:InterferingFilesDetectedTextTS")
#else
                    "The following interfering files are present:".L10N("Client:Main:InterferingFilesDetectedTextNonTS1") +
                    Environment.NewLine + Environment.NewLine +
                    String.Join(Environment.NewLine, presentFiles) +
                    Environment.NewLine + Environment.NewLine +
                    "The mod won't work correctly without those files removed.".L10N("Client:Main:InterferingFilesDetectedTextNonTS2")
#endif
                    );
        }
        class LRXDontLoveMeException : Exception
        {
            public LRXDontLoveMeException(string message) : base(message)
            {
            }
        }
        class AgeVerifyException : Exception
        {
            public AgeVerifyException(string message) : base(message)
            {
            }
        }
        /// <summary>
        /// Checks whether the client is running for the first time.
        /// If it is, displays a dialog asking the user if they'd like
        /// to configure settings.
        /// </summary>
        private void CheckIfFirstRun()
        {
            if (UserINISettings.Instance.IsFirstRun)
            {
                if (ClientConfiguration.Instance.AgeVerify)
                {
                    ShowFirstRunMessageBox();
                }
                else
                {
                    UserINISettings.Instance.IsFirstRun.Value = false;
                    UserINISettings.Instance.SaveSettings();
                    firstRunMessageBox2 = XNAMessageBox.ShowYesNoDialog(WindowManager,
                        "Initial Installation".L10N("Client:Main:InitialInstallationTitle"),
                        string.Format(("You have just installed {0}.\n" +
                            "It's highly recommended that you configure your settings before playing.\n" +
                            "Do you want to configure them now?").L10N("Client:Main:InitialInstallationText"),
                        ClientConfiguration.Instance.LocalGame));
                    firstRunMessageBox2.YesClickedAction = FirstRunMessageBox_YesClicked;
                    firstRunMessageBox2.NoClickedAction = FirstRunMessageBox_NoClicked;
                }
            }

            optionsWindow.PostInit();
        }

        private void ShowFirstRunMessageBox()
        {
            SetButtonHotkeys(false);
            // Create the darkening panel
            var darkeningFirstRunPanel = new XNATextBlock(WindowManager)
            {
                Name = "Darkening Panel firstRun",
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, Width, Height),
                BackgroundTexture = AssetLoader.CreateTexture(new Microsoft.Xna.Framework.Color(0, 0, 0, 192), 1, 1),
                DrawBorders = true
            };

            // Create the message box
            var firstRunMessageBox = new XNATextBlock(WindowManager)
            {
                Name = "FirstRun Message",
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 448, 280),
                BackgroundTexture = AssetLoader.LoadTexture("firstmsgboxform.png"),
                DrawBorders = true
            };
            firstRunMessageBox.CenterOnParent();

            DateTime currentDate = DateTime.Now;
            // 提取年、月、日
            string year = (currentDate.Year - 18).ToString();
            string month = currentDate.Month.ToString();
            string day = currentDate.Day.ToString();

            dd年 = new XNASuggestionTextBox(WindowManager);
            dd年.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(129, 133, 70, 20);
            dd年.Text = year;
            dd年.Suggestion = "Year".L10N("Client:Main:ddYear");

            dd月 = new XNASuggestionTextBox(WindowManager);
            dd月.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(209, 133, 50, 20);
            dd月.Text = month;
            dd月.Suggestion = "Month".L10N("Client:Main:ddMonth");

            dd日 = new XNASuggestionTextBox(WindowManager);
            dd日.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(269, 133, 50, 20);
            dd日.Text = day;
            dd日.Suggestion = "Day".L10N("Client:Main:ddDay");

            firstRunMessageBox.AddChild(dd年);
            firstRunMessageBox.AddChild(dd月);
            firstRunMessageBox.AddChild(dd日);

            // Create the installation label
            var lblInstallation = new XNALabel(WindowManager)
            {
                Name = "lblInstallation",
                Text = "Initial Installation (Verify Your Age)".L10N("Client:Main:InitialInstallationTitleAges"),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 20, 120, 20)
            };

            var lblInstallationText = new XNALabel(WindowManager)
            {
                Name = "lblInstallationText",
                Text = string.Format(("Welcome to play {0}.").L10N("Client:Main:InitialInstallationText1"),
                    ClientConfiguration.Instance.LocalGame),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 50, 120, 20)
            };            // Common button properties

            var lblInstallationText2 = new XNALabel(WindowManager)
            {
                Name = "lblInstallationText",
                Text = string.Format(("It's highly recommended that you configure your settings before \n" +
                        "playing. Once the settings are complete, you can configure the \n" +
                        "client by clicking on the options in the bottom right corner.").L10N("Client:Main:InitialInstallationText2"),
                    ClientConfiguration.Instance.LocalGame),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 75, 120, 20)
            };

            var lblInstallationText3 = new XNALabel(WindowManager)
            {
                Name = "lblInstallationText",
                Text = ("Your Birthday: ").L10N("Client:Main:InitialInstallationText3"),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 133, 120, 20)
            };

            var commonButtonProps = new
            {
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 110, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT),
                DrawBorders = true
            };

            // Text box for name
            var tbxName = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 185, 408, 25),
                Name = "nameTextBox",
                Suggestion = "Name must be less than ".L10N("Client:Main:tbxName1") + ClientConfiguration.Instance.MaxNameLength.ToString() + " characters in length".L10N("Client:Main:tbxName2")
            };

            // First label
            var firstLabel = new XNALabel(WindowManager)
            {
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(20, 162, 408, 42),
                Name = "firstLabel",
                Text = "Give yourself a name first: (Empty for default)".L10N("Client:Main:firstLabel")
            };

            // Yes button
            var btnYes = new XNAClientButton(WindowManager)
            {
                Text = "OK".L10N("Client:ClientGUI:ButtonOK"),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(84, 228, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT)
            };
            firstRunMessageBox.AddChild(btnYes);
            btnYes.LeftClick += (sender, e) =>
            {
                if (HandleNameConfirmation(tbxName, firstLabel, btnYes))
                {
                    if (age < 18)
                    {
                        if (DateTime.Now.Hour > 21 || DateTime.Now.Hour < 8)
                        {
                            fangChenmiBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                        "防沉迷系统提示", "根据监管部门要求，每日22时后至次日8时前不得向未成年人提供服务。");
                            fangChenmiBox.YesClickedAction += BtnExit_LeftClick;
                            fangChenmiBox.NoClickedAction += BtnExit_LeftClick;
                        }
                    }
                    else
                    {
                        CheckRequiredFiles();
                        CheckForbiddenFiles();
                    }
                    darkeningFirstRunPanel.Disable();
                }
            };

                // No button
                var btnNo = new XNAClientButton(WindowManager)
            {
                Text = "Cancel".L10N("Client:ClientGUI:ButtonCancel"),
                ClientRectangle = new Microsoft.Xna.Framework.Rectangle(277, 228, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT)
            };
            firstRunMessageBox.AddChild(btnNo);
            btnNo.LeftClick += BtnExit_LeftClick;

            // Add all controls to the message box
            firstRunMessageBox.AddChild(lblInstallation);
            firstRunMessageBox.AddChild(lblInstallationText);
            firstRunMessageBox.AddChild(lblInstallationText3);
            firstRunMessageBox.AddChild(lblInstallationText2);
            firstRunMessageBox.AddChild(firstLabel);
            firstRunMessageBox.AddChild(tbxName);
            darkeningFirstRunPanel.AddChild(firstRunMessageBox);
            AddChild(darkeningFirstRunPanel);
        }

        //验证生日输入
        public bool ValidateDate(string year, string month, string day)
        {
            // 定义日期格式
            string format = "yyyy-MM-dd";

            // 尝试将输入的字符串转换为日期
            if (DateTime.TryParseExact(year + "-" + month + "-" + day,
                                        format,
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None,
                                        out DateTime parsedDate))
            {
                Console.WriteLine("成功解析日期");
                return true; // 成功解析日期
            }
            else
            {
                Console.WriteLine("解析失败");
                return false; // 解析失败
            }
        }

        //计算年龄
        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (today.Month < birthDate.Month ||
                (today.Month == birthDate.Month && today.Day < birthDate.Day))
            {
                age--;
            }
            if (today.Month == birthDate.Month && today.Day == birthDate.Day && age == 18)
            {
                UserINISettings.Instance.IsFirstRun.Value = false;
                UserINISettings.Instance.SaveSettings();
                throw new AgeVerifyException("刚满十八岁~~~");
            }
            return age;
        }

        private bool HandleNameConfirmation(XNASuggestionTextBox tbxName, XNALabel firstLabel, XNAClientButton btnConfirm)
        {
            if (string.IsNullOrEmpty(tbxName.Text) || tbxName.Text == "Name must be less than ".L10N("Client:Main:tbxName1") + ClientConfiguration.Instance.MaxNameLength.ToString() + " characters in length".L10N("Client:Main:tbxName2"))
            {
                UserINISettings.Instance.IsFirstRun.Value = false;
                UserINISettings.Instance.SaveSettings();
            }
            else if (string.IsNullOrWhiteSpace(tbxName.Text) || tbxName.Text.Length > ClientConfiguration.Instance.MaxNameLength || int.TryParse(tbxName.Text.Substring(0, 1), out _) || tbxName.Text[0] == '-')
            {
                firstLabel.Text = "Name must be less than ".L10N("Client:Main:tbxName1") + ClientConfiguration.Instance.MaxNameLength.ToString() + " characters in length".L10N("Client:Main:tbxName2");
                tbxName.IdleBorderColor = Microsoft.Xna.Framework.Color.Red;
                //tbxName.BackColor = Color.Red;
                Task.Run(async () =>
                {
                    btnConfirm.Enabled = false;
                    await Task.Delay(800);
                    firstLabel.Text = "Give yourself a name first: ".L10N("Client:Main:firstLabel");
                    btnConfirm.Enabled = true;
                    tbxName.IdleBorderColor = BorderColor;
                    //tbxName.BackColor = Color.Transparent;
                });
                return false;
            }
            else{
                ProgramConstants.PLAYERNAME = tbxName.Text;
                UserINISettings.Instance.PlayerName.Value = tbxName.Text;
            }
            string year = dd年.Text.PadLeft(4, '0');
            string month = dd月.Text.PadLeft(2, '0');
            string day = dd日.Text.PadLeft(2, '0');
            if (dd年.Enabled)
            {
                if (ValidateDate(year, month, day))
                {
                    // 解析日期
                    DateTime birthDate;
                    if (DateTime.TryParseExact(year + "-" + month + "-" + day,
                                               "yyyy-MM-dd",
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.None,
                                               out birthDate))
                    {


                        //存档生日
                        string formattedBirthDate = birthDate.ToString("yyyyMMdd");
                        //转换为8位字符串
                        UserINISettings.Instance.Birthday.Value = formattedBirthDate;
                        UserINISettings.Instance.SaveSettings();

                        // 计算年龄
                        age = CalculateAge(birthDate);
                        if (age <= 3 || age > 116)
                        {
                            XNAMessageBox.ShowYesNoDialog(WindowManager,
                            "Error".L10N("Client:Main:Error"), ("Invalid Date. Please input a correct Date.").L10N("Client:Main:DateInvalid"));
                            return false;
                        }
                        /*
                        Console.WriteLine($"出生日期: {birthDate.ToShortDateString()}");
                        Console.WriteLine($"年龄: {age}岁");

                        dd年.Enabled = false;
                        dd月.Enabled = false;
                        dd日.Enabled = false;
                        */
                    }
                }
                else
                {
                    XNAMessageBox.ShowYesNoDialog(WindowManager,
                    "Error".L10N("Client:Main:Error"),("Invalid Date. Please input a correct Date.").L10N("Client:Main:DateInvalid"));
                    return false;
                }
            }
            UserINISettings.Instance.IsFirstRun.Value = false;
            UserINISettings.Instance.SaveSettings();
            SetButtonHotkeys(true);
            Logger.Log("Your birthday is: "+ dd年 +"."+ dd月 +"." + dd日);
            return true;
        }


        private void FirstRunMessageBox_NoClicked(XNAMessageBox messageBox)
        {
            if (customComponentDialogQueued)
                Updater_OnCustomComponentsOutdated();

            CheckRequiredFiles();
            CheckForbiddenFiles();
        }

        private void FirstRunMessageBox_YesClicked(XNAMessageBox messageBox)
        {
            optionsWindow.Open();
            CheckRequiredFiles();
            CheckForbiddenFiles();
        }

        private void SharedUILogic_GameProcessStarted() => MusicOff();

        private void WindowManager_GameClosing(object sender, EventArgs e) => Clean();

        private void SkirmishLobby_Exited(object sender, EventArgs e)
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
        }

        private void LanLobby_Exited(object sender, EventArgs e)
        {
            topBar.SetLanMode(false);

            if (UserINISettings.Instance.AutomaticCnCNetLogin)
                connectionManager.Connect();

            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
        }

        private void CnCNetInfoController_CnCNetGameCountUpdated(object sender, PlayerCountEventArgs e)
        {
            lock (locker)
            {
                if (e.PlayerCount == -1)
                    lblCnCNetPlayerCount.Text = "N/A".L10N("Client:Main:N/A");
                else
                    lblCnCNetPlayerCount.Text = e.PlayerCount.ToString();
            }
        }

        /// <summary>
        /// Attemps to "clean" the client session in a nice way if the user closes the game.
        /// </summary>
        private void Clean()
        {
            Updater.FileIdentifiersUpdated -= Updater_FileIdentifiersUpdated;

            if (cncnetPlayerCountCancellationSource != null) cncnetPlayerCountCancellationSource.Cancel();
            topBar.Clean();
            if (UpdateInProgress)
                Updater.StopUpdate();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        /// <summary>
        /// Starts playing music, initiates an update check if automatic updates
        /// are enabled and checks whether the client is run for the first time.
        /// Called after all internal client UI logic has been initialized.
        /// </summary>
        public void PostInit()
        {
            if (UserINISettings.Instance.IsFirstRun && ClientConfiguration.Instance.LocalGame == "LIS")
            {
                Random ran = new Random();
                int n = ran.Next(2011 * 8 * 14);
                if (n == 1)
                {
                    throw new LRXDontLoveMeException("呜呜呜，梁如萱不爱我┭┮﹏┭┮\n（恭喜你中了彩蛋！程序没有问题，重启一下就好啦！）".L10N("Client:Exceptions:LRXDontLoveMeException"));
                }
            }
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, skirmishLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cnCNetGameLoadingLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cnCNetGameLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);
            optionsWindow.SetTopBar(topBar);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);
            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(privateMessagingWindow);
            topBar.SetTertiarySwitch(privateMessagingWindow);
            topBar.SetOptionsWindow(optionsWindow);
            WindowManager.AddAndInitializeControl(gameInProgressWindow);

            skirmishLobby.Disable();
            cncnetLobby.Disable();
            cnCNetGameLobby.Disable();
            cnCNetGameLoadingLobby.Disable();
            lanLobby.Disable();
            privateMessagingWindow.Disable();
            optionsWindow.Disable();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(this);

            SwitchMainMenuMusicFormat();

            themeSong = AssetLoader.LoadSong(ClientConfiguration.Instance.MainMenuMusicName);

            PlayMusic();

            if (!ClientConfiguration.Instance.ModMode)
            {
                if (Updater.UpdateMirrors.Count < 1)
                {
                    lblUpdateStatus.Text = "No update download mirrors available.".L10N("Client:Main:NoUpdateMirrorsAvailable");
                    lblUpdateStatus.DrawUnderline = false;
                }
                else if (UserINISettings.Instance.CheckForUpdates)
                {
                    CheckForUpdates();
                }
                else
                {
                    lblUpdateStatus.Text = "Click to check for updates.".L10N("Client:Main:ClickToCheckUpdate");
                }
            }

            if (!UserINISettings.Instance.IsFirstRun && ClientConfiguration.Instance.AgeVerify)
            {
                string 生日 = UserINISettings.Instance.Birthday.Value;
                Logger.Log("Parsing birthday: " + 生日);
                if (生日 == "00000101")
                {
                    ShowFirstRunMessageBox();
                }
                else
                {
                    // 尝试解析生日字符串为 DateTime 对象
                    if (DateTime.TryParseExact(生日, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime birthDate))
                    {
                        // 计算年龄
                        int 年龄 = DateTime.Now.Year - birthDate.Year;
                        if (birthDate > DateTime.Now.AddYears(-年龄)) 年龄--;
                        Logger.Log("Your age is: " + 年龄.ToString());
                        if (年龄 < 18)
                        {
                            if (年龄 < 0)
                            {
                                Logger.Log("Strange! Who can play games before they are even born?");
                                throw new AgeVerifyException("获取您的年龄失败，请稍后重试。");
                            }
                            else if (年龄 == 0)
                            {
                                Logger.Log("Strange! Who is born to play the game?");
                                throw new AgeVerifyException("获取您的年龄失败，请稍后重试。");
                            }
                            else if (年龄 <= 3)
                            {
                                Logger.Log("Strange! Who can play games before they learn to sing, dance or rap?");
                                throw new AgeVerifyException("获取您的年龄失败，请稍后重试。");
                            }
                            else if (DateTime.Now.Hour > 21 || DateTime.Now.Hour < 8)
                            {
                                fangChenmiBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                        "防沉迷系统提示", "根据监管部门要求，每日22时后至次日8时前不得向未成年人提供服务。");
                                fangChenmiBox.YesClickedAction += BtnExit_LeftClick;
                                fangChenmiBox.NoClickedAction += BtnExit_LeftClick;
                            }
                        }
                        else if (年龄 > 116)
                        {
                            Logger.Log("You've lived so long! You can already go for the Guinness Book of World Records!");
                            throw new AgeVerifyException("获取您的年龄失败，请稍后重试。");
                        }
                        else
                        {
                            CheckRequiredFiles();
                            CheckForbiddenFiles();
                        }
                    }
                }
            }
            else
            {
                Logger.Log("Parsing birthday failed: " + 生日);
                fangChenmiBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                "防沉迷系统提示", "获取您的年龄失败，您可以重新输入生日信息，\n也可以稍后再次尝试。");
                fangChenmiBox.YesClickedAction += FangChenMi_LeftClick;
                fangChenmiBox.NoClickedAction += BtnExit_LeftClick;
            }
            }
            CheckIfFirstRun();
        }

        private void SwitchMainMenuMusicFormat()
        {
#if GL || DX
            FileInfo wmaMainMenuMusicFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"{ClientConfiguration.Instance.MainMenuMusicName}.wma"));

            if (!wmaMainMenuMusicFile.Exists)
                return;

            FileInfo wmaBackupMainMenuMusicFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"{ClientConfiguration.Instance.MainMenuMusicName}.bak"));

            if (!wmaBackupMainMenuMusicFile.Exists)
                wmaMainMenuMusicFile.CopyTo(wmaBackupMainMenuMusicFile.FullName);

#endif
#if DX
            wmaBackupMainMenuMusicFile.CopyTo(wmaMainMenuMusicFile.FullName, true);
#elif GL
            FileInfo oggMainMenuMusicFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"{ClientConfiguration.Instance.MainMenuMusicName}.ogg"));

            if (oggMainMenuMusicFile.Exists)
                oggMainMenuMusicFile.CopyTo(wmaMainMenuMusicFile.FullName, true);
#endif
        }

        #region Updating / versioning system

        private void UpdateWindow_UpdateFailed(object sender, UpdateFailureEventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "Updating failed! Click to retry.".L10N("Client:Main:UpdateFailedClickToRetry");
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            UpdateInProgress = false;

            innerPanel.Show(null); // Darkening
            XNAMessageBox msgBox = new XNAMessageBox(WindowManager, "Update failed".L10N("Client:Main:UpdateFailedTitle"),
                string.Format(("An error occured while updating. Returned error was: {0}\n\nIf you are connected to the Internet and your firewall isn't blocking\n{1}, and the issue is reproducible, contact us at\n{2} for support.").L10N("Client:Main:UpdateFailedText"),
                e.Reason, Path.GetFileName(ProgramConstants.StartupExecutable), MainClientConstants.SUPPORT_URL_SHORT), XNAMessageBoxButtons.OK);
            msgBox.OKClickedAction = MsgBox_OKClicked;
            msgBox.Show();
        }

        private void MsgBox_OKClicked(XNAMessageBox messageBox)
        {
            innerPanel.Hide();
        }

        private void UpdateWindow_UpdateCancelled(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "The update was cancelled. Click to retry.".L10N("Client:Main:UpdateCancelledClickToRetry");
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            UpdateInProgress = false;
        }

        private void UpdateWindow_UpdateCompleted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = string.Format("{0} was succesfully updated to v.{1}".L10N("Client:Main:UpdateSuccess"),
                MainClientConstants.GAME_NAME_SHORT, Updater.GameVersion);
            lblVersion.Text = Updater.GameVersion;
            UpdateInProgress = false;
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = false;
        }

        private void LblUpdateStatus_LeftClick(object sender, EventArgs e)
        {
            Logger.Log(Updater.VersionState.ToString());

            if (Updater.VersionState == VersionState.OUTDATED ||
                Updater.VersionState == VersionState.MISMATCHED ||
                Updater.VersionState == VersionState.UNKNOWN ||
                Updater.VersionState == VersionState.UPTODATE)
            {
                CheckForUpdates();
            }
        }

        private void LblVersion_LeftClick(object sender, EventArgs e)
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.ChangelogURL);
        }

        private void ForceUpdate()
        {
            UpdateInProgress = true;
            innerPanel.Hide();
            innerPanel.UpdateWindow.ForceUpdate();
            innerPanel.Show(innerPanel.UpdateWindow);
            lblUpdateStatus.Text = "Force updating...".L10N("Client:Main:ForceUpdating");
        }

        /// <summary>
        /// Starts a check for updates.
        /// </summary>
        private void CheckForUpdates()
        {
            if (Updater.UpdateMirrors.Count < 1)
                return;

            Updater.CheckForUpdates();
            lblUpdateStatus.Enabled = false;
            lblUpdateStatus.Text = "Checking for updates..."
                .L10N("Client:Main:CheckingForUpdates");
            lastUpdateCheckTime = DateTime.Now;
        }

        private void Updater_FileIdentifiersUpdated()
            => WindowManager.AddCallback(new Action(HandleFileIdentifierUpdate), null);

        /// <summary>
        /// Used for displaying the result of an update check in the UI.
        /// </summary>
        private void HandleFileIdentifierUpdate()
        {
            if (UpdateInProgress)
            {
                return;
            }

            if (Updater.VersionState == VersionState.UPTODATE)
            {
                lblUpdateStatus.Text = string.Format("{0} is up to date.".L10N("Client:Main:GameUpToDate"), MainClientConstants.GAME_NAME_SHORT);
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = false;
            }
            else if (Updater.VersionState == VersionState.OUTDATED && Updater.ManualUpdateRequired)
            {
                lblUpdateStatus.Text = "An update is available. Manual download & installation required.".L10N("Client:Main:UpdateAvailableManualDownloadRequired");
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = false;
                innerPanel.ManualUpdateQueryWindow.SetInfo(Updater.ServerGameVersion, Updater.ManualDownloadURL);

                if (!string.IsNullOrEmpty(Updater.ManualDownloadURL))
                    innerPanel.Show(innerPanel.ManualUpdateQueryWindow);
            }
            else if (Updater.VersionState == VersionState.OUTDATED)
            {
                lblUpdateStatus.Text = "An update is available.".L10N("Client:Main:UpdateAvailable");
                innerPanel.UpdateQueryWindow.SetInfo(Updater.ServerGameVersion, Updater.UpdateSizeInKb);
                innerPanel.Show(innerPanel.UpdateQueryWindow);
            }
            else if (Updater.VersionState == VersionState.UNKNOWN)
            {
                lblUpdateStatus.Text = "Checking for updates failed! Click to retry.".L10N("Client:Main:CheckUpdateFailedClickToRetry");
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = true;
            }
        }

        /// <summary>
        /// Asks the user if they'd like to update their custom components.
        /// Handles an event raised by the updater when it has detected
        /// that the custom components are out of date.
        /// </summary>
        private void Updater_OnCustomComponentsOutdated()
        {
            if (innerPanel.UpdateQueryWindow.Visible)
                return;

            if (UpdateInProgress)
                return;

            if ((firstRunMessageBox != null && firstRunMessageBox.Visible) || optionsWindow.Enabled)
            {
                // If the custom components are out of date on the first run
                // or the options window is already open, don't show the dialog
                customComponentDialogQueued = true;
                return;
            }

            customComponentDialogQueued = false;

            XNAMessageBox ccMsgBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                "Custom Component Updates Available".L10N("Client:Main:CustomUpdateAvailableTitle"),
                ("Updates for custom components are available. Do you want to open\nthe Options menu where you can update the custom components?").L10N("Client:Main:CustomUpdateAvailableText"));
            ccMsgBox.YesClickedAction = CCMsgBox_YesClicked;
        }

        private void CCMsgBox_YesClicked(XNAMessageBox messageBox)
        {
            optionsWindow.Open();
            optionsWindow.SwitchToCustomComponentsPanel();
        }

        /// <summary>
        /// Called when the user has declined an update.
        /// </summary>
        private void UpdateQueryWindow_UpdateDeclined(object sender, EventArgs e)
        {
            UpdateQueryWindow uqw = (UpdateQueryWindow)sender;
            innerPanel.Hide();
            lblUpdateStatus.Text = "An update is available, click to install.".L10N("Client:Main:UpdateAvailableClickToInstall");
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = true;
        }

        /// <summary>
        /// Called when the user has accepted an update.
        /// </summary>
        private void UpdateQueryWindow_UpdateAccepted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            innerPanel.UpdateWindow.SetData(Updater.ServerGameVersion);
            innerPanel.Show(innerPanel.UpdateWindow);
            lblUpdateStatus.Text = "Updating...".L10N("Client:Main:Updating");
            UpdateInProgress = true;
            Updater.StartUpdate();
        }

        private void ManualUpdateQueryWindow_Closed(object sender, EventArgs e)
            => innerPanel.Hide();

        #endregion

        private void BtnOptions_LeftClick(object sender, EventArgs e)
            => optionsWindow.Open();

        private void BtnNewCampaign_LeftClick(object sender, EventArgs e)
            => innerPanel.Show(innerPanel.CampaignSelector);

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
            => innerPanel.Show(innerPanel.GameLoadingWindow);

        private void BtnLan_LeftClick(object sender, EventArgs e)
        {
            lanLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();

            topBar.SetLanMode(true);
        }

        private void BtnCnCNet_LeftClick(object sender, EventArgs e) => topBar.SwitchToSecondary();

        private void BtnSkirmish_LeftClick(object sender, EventArgs e)
        {
            skirmishLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();
        }

        private void BtnMapEditor_LeftClick(object sender, EventArgs e) => LaunchMapEditor();

        private void BtnStatistics_LeftClick(object sender, EventArgs e) =>
            innerPanel.Show(innerPanel.StatisticsWindow);

        private void BtnCredits_LeftClick(object sender, EventArgs e)
        {
            ProcessLauncher.StartShellProcess(MainClientConstants.CREDITS_URL);
        }

        private void BtnExtras_LeftClick(object sender, EventArgs e) =>
            innerPanel.Show(innerPanel.ExtrasWindow);

        private void BtnExit_LeftClick(object sender, EventArgs e)
        {
            XNAMessageBox messageBox = new XNAMessageBox(WindowManager, "Exit Confirm".L10N("UI:Main:clientQuitWindows"),
        "Do you really want to quit the client?".L10N("UI:Main:clientQuit"), XNAMessageBoxButtons.YesNo);
            messageBox.Show();
            messageBox.YesClickedAction += BtnExit_LeftClick;
        }

        private void BtnExit_LeftClick(XNAMessageBox messageBox)
        {
#if WINFORMS
            WindowManager.HideWindow();
#endif
            FadeMusicExit();
        }

        private void FangChenMi_LeftClick(XNAMessageBox messageBox)
        {
            ShowFirstRunMessageBox();
        }

        private void SharedUILogic_GameProcessExited() =>
            AddCallback(new Action(HandleGameProcessExited), null);

        private void HandleGameProcessExited()
        {
            innerPanel.GameLoadingWindow.ListSaves();
            innerPanel.Hide();

            // If music is disabled on menus, check if the main menu is the top-most
            // window of the top bar and only play music if it is
            // LAN has the top bar disabled, so to detect the LAN game lobby
            // we'll check whether the top bar is enabled
            if (!UserINISettings.Instance.StopMusicOnMenu ||
                (topBar.Enabled && topBar.LastSwitchType == SwitchType.PRIMARY &&
                topBar.GetTopMostPrimarySwitchable() == this))
                PlayMusic();
        }

        /// <summary>
        /// Switches to the main menu and performs a check for updates.
        /// </summary>
        private void CncnetLobby_UpdateCheck(object sender, EventArgs e)
        {
            CheckForUpdates();
            topBar.SwitchToPrimary();
        }

        public override void Update(GameTime gameTime)
        {
            if (isMusicFading)
                FadeMusic(gameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            lock (locker)
            {
                base.Draw(gameTime);
            }
        }

        /// <summary>
        /// Attempts to start playing the menu music.
        /// </summary>
        private void PlayMusic()
        {
            if (!isMediaPlayerAvailable)
                return; // SharpDX fails at music playback on Vista

            if (themeSong != null && UserINISettings.Instance.PlayMainMenuMusic)
            {
                isMusicFading = false;
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = (float)UserINISettings.Instance.ClientVolume;

                try
                {
                    MediaPlayer.Play(themeSong);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Log("Playing main menu music failed! " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Lowers the volume of the menu music, or stops playing it if the
        /// volume is unaudibly low.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void FadeMusic(GameTime gameTime)
        {
            if (!isMediaPlayerAvailable || !isMusicFading || themeSong == null)
                return;

            // Fade during 1 second
            float step = SoundPlayer.Volume * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (MediaPlayer.Volume > step)
                MediaPlayer.Volume -= step;
            else
            {
                MediaPlayer.Stop();
                isMusicFading = false;
            }
        }

        /// <summary>
        /// Exits the client. Quickly fades the music if it's playing.
        /// </summary>
        private void FadeMusicExit()
        {
            if (!isMediaPlayerAvailable || themeSong == null)
            {
                ExitClient();
                return;
            }

            float step = MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP * (float)UserINISettings.Instance.ClientVolume;

            if (MediaPlayer.Volume > step)
            {
                MediaPlayer.Volume -= step;
                AddCallback(new Action(FadeMusicExit), null);
            }
            else
            {
                MediaPlayer.Stop();
                ExitClient();
            }
        }

        private void ExitClient()
        {
            Logger.Log("Exiting.");
            WindowManager.CloseGame();
            themeSong?.Dispose();
#if !XNA
            Thread.Sleep(1000);
            Environment.Exit(0);
#endif
        }

        public void SwitchOn()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();

            if (!ClientConfiguration.Instance.ModMode && UserINISettings.Instance.CheckForUpdates)
            {
                // Re-check for updates

                if ((DateTime.Now - lastUpdateCheckTime) > TimeSpan.FromSeconds(UPDATE_RE_CHECK_THRESHOLD))
                    CheckForUpdates();
            }
        }

        public void SwitchOff()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();
        }

        private void MusicOff()
        {
            try
            {
                if (isMediaPlayerAvailable &&
                    MediaPlayer.State == MediaState.Playing)
                {
                    isMusicFading = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Turning music off failed! Message: " + ex.ToString());
            }
        }

        /// <summary>
        /// Checks if media player is available currently.
        /// It is not available on Windows Vista or other systems without the appropriate media player components.
        /// </summary>
        /// <returns>True if media player is available, false otherwise.</returns>
        private bool IsMediaPlayerAvailable()
        {
            try
            {
                MediaState state = MediaPlayer.State;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Error encountered when checking media player availability. Error message: " + ex.ToString());
                return false;
            }
        }

        private void LaunchMapEditor()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();
            using var mapEditorProcess = new Process();

            if (osVersion != OSVersion.UNIX)
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath);
            else
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

            mapEditorProcess.StartInfo.UseShellExecute = false;

            try
            {
                mapEditorProcess.Start();
            }
            catch
            {
                XNAMessageBox.ShowYesNoDialog(WindowManager,
                    "No Map Editor".L10N("Client:Main:NoMapEditor"),("There is no map editor. Check your map editor file.").L10N("Client:Main:NoMapEditorText"));
            }
        }

        public string GetSwitchName() => "Main Menu".L10N("Client:Main:MainMenu");
    }
}