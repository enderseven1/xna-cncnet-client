using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using ClientCore;
using System.IO;
using Rampastring.Tools;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.Domain.Multiplayer;
using ClientGUI;
using System.Text;
using DTAClient.Domain;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base class for multiplayer game lobbies (CnCNet and LAN).
    /// </summary>
    public abstract class MultiplayerGameLobby : GameLobbyBase, ISwitchable
    {
        private const int MAX_DICE = 10;
        private const int MAX_DIE_SIDES = 100;

        public MultiplayerGameLobby(WindowManager windowManager, string iniName, 
            TopBar topBar, MapLoader mapLoader, DiscordHandler discordHandler)
            : base(windowManager, iniName, mapLoader, true, discordHandler)
        {
            TopBar = topBar;
            MapLoader = mapLoader;

            chatBoxCommands = new List<ChatBoxCommand>
            {
                new ChatBoxCommand("HIDEMAPS", "隐藏地图列表（仅房主）", true,
                    s => HideMapList()),
                new ChatBoxCommand("SHOWMAPS", "显示地图列表（仅房主）", true,
                    s => ShowMapList()),
                new ChatBoxCommand("FRAMESENDRATE", "更改延迟 / FrameSendRate （默认为7）（仅房主）", true,
                    s => SetFrameSendRate(s)),
                new ChatBoxCommand("MAXAHEAD", "更改MaxAhead（默认为0）（仅房主）", true,
                    s => SetMaxAhead(s)),
                new ChatBoxCommand("PROTOCOLVERSION", "更改ProtocolVersion（默认为2）（仅房主）", true,
                    s => SetProtocolVersion(s)),
                new ChatBoxCommand("LOADMAP", "从/Maps/Custom/使用既定名称载入地图", true, LoadCustomMap),
                new ChatBoxCommand("RANDOMSTARTS", "启用随机起始位置（仅泰伯利亚之日）", true,
                    s => SetStartingLocationClearance(s)),
                new ChatBoxCommand("ROLL", "投骰子，例如/roll 3d6（三个六面骰）", false, RollDiceCommand),
                new ChatBoxCommand("SAVEOPTIONS", "保存游戏选项预设", false, HandleGameOptionPresetSaveCommand),
                new ChatBoxCommand("LOADOPTIONS", "加载游戏选项预设", true, HandleGameOptionPresetLoadCommand)
            };
        }

        protected XNACheckBox[] ReadyBoxes;

        protected ChatListBox lbChatMessages;
        protected XNAChatTextBox tbChatInput;
        protected XNAClientButton btnLockGame;
        protected XNAClientCheckBox chkAutoReady;

        protected bool IsHost = false;

        private bool locked = false;
        protected bool Locked
        {
            get => locked;
            set
            {
                bool oldLocked = locked;
                locked = value;
                if (oldLocked != value)
                    UpdateDiscordPresence();
            }
        }

        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;
        protected EnhancedSoundEffect sndGetReadySound;
        protected EnhancedSoundEffect sndReturnSound;

        protected Texture2D[] PingTextures;

        protected TopBar TopBar;

        protected int FrameSendRate { get; set; } = 7;

        /// <summary>
        /// Controls the MaxAhead parameter. The default value of 0 means that 
        /// the value is not written to spawn.ini, which allows the spawner the
        /// calculate and assign the MaxAhead value.
        /// </summary>
        protected int MaxAhead { get; set; }

        protected int ProtocolVersion { get; set; } = 2;

        protected List<ChatBoxCommand> chatBoxCommands;

        private FileSystemWatcher fsw;

        private bool gameSaved = false;

        protected MapLoader MapLoader;

        private bool lastMapChangeWasInvalid = false;

        /// <summary>
        /// Allows derived classes to add their own chat box commands.
        /// </summary>
        /// <param name="command">The command to add.</param>
        protected void AddChatBoxCommand(ChatBoxCommand command) => chatBoxCommands.Add(command);

        public override void Initialize()
        {
            Name = nameof(MultiplayerGameLobby);

            base.Initialize();

            PingTextures = new Texture2D[5]
            {
                AssetLoader.LoadTexture("ping0.png"),
                AssetLoader.LoadTexture("ping1.png"),
                AssetLoader.LoadTexture("ping2.png"),
                AssetLoader.LoadTexture("ping3.png"),
                AssetLoader.LoadTexture("ping4.png")
            };

            InitPlayerOptionDropdowns();

            ReadyBoxes = new XNACheckBox[MAX_PLAYER_COUNT];

            int readyBoxX = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxX", 7);
            int readyBoxY = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxY", 4);

            for (int i = 0; i < MAX_PLAYER_COUNT; i++)
            {
                XNACheckBox chkPlayerReady = new XNACheckBox(WindowManager);
                chkPlayerReady.Name = "chkPlayerReady" + i;
                chkPlayerReady.Checked = false;
                chkPlayerReady.AllowChecking = false;
                chkPlayerReady.ClientRectangle = new Rectangle(readyBoxX, ddPlayerTeams[i].Y + readyBoxY,
                    0, 0);

                PlayerOptionsPanel.AddChild(chkPlayerReady);

                chkPlayerReady.DisabledClearTexture = chkPlayerReady.ClearTexture;
                chkPlayerReady.DisabledCheckedTexture = chkPlayerReady.CheckedTexture;

                ReadyBoxes[i] = chkPlayerReady;
                ddPlayerSides[i].AddItem("Spectator", AssetLoader.LoadTexture("spectatoricon.png"));
            }

            ddGameModeMapFilter.ClientRectangle = new Rectangle(
                MapPreviewBox.X - 12 - ddGameModeMapFilter.Width,
                MapPreviewBox.Y, ddGameModeMapFilter.Width,
                ddGameModeMapFilter.Height);

            lblGameModeSelect.ClientRectangle = new Rectangle(
                btnLaunchGame.X, ddGameModeMapFilter.Y + 1,
                lblGameModeSelect.Width, lblGameModeSelect.Height);

            lbGameModeMapList.ClientRectangle = new Rectangle(btnLaunchGame.X, 
                MapPreviewBox.Y + 23,
                MapPreviewBox.X - btnLaunchGame.X - 12,
                MapPreviewBox.Height - 23);

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameModeMapList.X, 
                GameOptionsPanel.Y,
               lbGameModeMapList.Width, GameOptionsPanel.Height - 24);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;
            lbChatMessages.DrawOrder = -1;
            lbChatMessages.UpdateOrder = -1;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.Suggestion = "在此输入聊天内容..";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X, 
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.MaximumTextLength = 150;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;
            tbChatInput.DrawOrder = 1;
            tbChatInput.UpdateOrder = 1;

            btnLockGame = new XNAClientButton(WindowManager);
            btnLockGame.Name = "btnLockGame";
            btnLockGame.ClientRectangle = new Rectangle(btnLaunchGame.Right + 12,
                btnLaunchGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLockGame.Text = "锁定房间";
            btnLockGame.LeftClick += BtnLockGame_LeftClick;

            chkAutoReady = new XNAClientCheckBox(WindowManager);
            chkAutoReady.Name = "chkAutoReady";
            chkAutoReady.ClientRectangle = new Rectangle(btnLaunchGame.Right + 12,
                btnLaunchGame.Y + 2, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            chkAutoReady.Text = "自动准备";
            chkAutoReady.CheckedChanged += ChkAutoReady_CheckedChanged;
            chkAutoReady.Disable();

            AddChild(lbChatMessages);
            AddChild(tbChatInput);
            AddChild(btnLockGame);
            AddChild(chkAutoReady);

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

            sndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
            sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
            sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);
            sndReturnSound = new EnhancedSoundEffect("return.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyReturnCooldown);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
                fsw.EnableRaisingEvents = false;
            }
            else
                Logger.Log("MultiplayerGameLobby: 存档不可用");
        }

        /// <summary>
        /// Performs initialization that is necessary after derived 
        /// classes have performed their own initialization.
        /// </summary>
        protected void PostInitialize()
        {
            InitializeWindow();
            CenterOnParent();
            LoadDefaultGameModeMap();
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            AddCallback(new Action<FileSystemEventArgs>(FSWEvent), e);
        }

        private void FSWEvent(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                if (!gameSaved)
                {
                    bool success = SavedGameManager.InitSavedGames();

                    if (!success)
                        return;
                }

                gameSaved = true;

                SavedGameManager.RenameSavedGame();
            }
        }

        protected override void StartGame()
        {
            if (fsw != null)
                fsw.EnableRaisingEvents = true;

            base.StartGame();
        }

        protected override void GameProcessExited()
        {
            gameSaved = false;

            if (fsw != null)
                fsw.EnableRaisingEvents = false;

            base.GameProcessExited();

            if (IsHost)
            {
                GenerateGameID();
                DdGameModeMapFilter_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks
            }
            else if (chkAutoReady.Checked)
            {
                RequestReadyStatus();
            }
        }

        private void GenerateGameID()
        {
            int i = 0;

            while (i < 20)
            {
                string s = DateTime.Now.Day.ToString() +
                    DateTime.Now.Month.ToString() +
                    DateTime.Now.Hour.ToString() +
                    DateTime.Now.Minute.ToString();

                UniqueGameID = int.Parse(i.ToString() + s);

                if (StatisticsManager.Instance.GetMatchWithGameID(UniqueGameID) == null)
                    break;

                i++;
            }
        }

        private void BtnLockGame_LeftClick(object sender, EventArgs e)
        {
            HandleLockGameButtonClick();
        }

        protected virtual void HandleLockGameButtonClick()
        {
            if (Locked)
                UnlockGame(true);
            else
                LockGame();
        }

        protected abstract void LockGame();

        protected abstract void UnlockGame(bool manual);

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            if (tbChatInput.Text.StartsWith("/"))
            {
                string text = tbChatInput.Text;
                string command;
                string parameters;

                int spaceIndex = text.IndexOf(' ');

                if (spaceIndex == -1)
                {
                    command = text.Substring(1).ToUpper();
                    parameters = string.Empty;
                }
                else
                {
                    command = text.Substring(1, spaceIndex - 1);
                    parameters = text.Substring(spaceIndex + 1);
                }
                
                tbChatInput.Text = string.Empty;

                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    if (command.ToUpper() == chatBoxCommand.Command)
                    {
                        if (!IsHost && chatBoxCommand.HostOnly)
                        {
                            AddNotice(string.Format("/{0} 仅房主可用。", chatBoxCommand.Command));
                            return;
                        }

                        chatBoxCommand.Action(parameters);
                        return;
                    }
                }

                StringBuilder sb = new StringBuilder("输入/<command>以使用指令。以下是可用的指令：");
                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
                    sb.Append($"{chatBoxCommand.Command}: {chatBoxCommand.Description}");
                }
                XNAMessageBox.Show(WindowManager, "聊天框指令帮助", sb.ToString());
                return;
            }

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        private void ChkAutoReady_CheckedChanged(object sender, EventArgs e)
        {
            btnLaunchGame.Enabled = !chkAutoReady.Checked;
            RequestReadyStatus();
        }

        protected void ResetAutoReadyCheckbox()
        {
            chkAutoReady.CheckedChanged -= ChkAutoReady_CheckedChanged;
            chkAutoReady.Checked = false;
            chkAutoReady.CheckedChanged += ChkAutoReady_CheckedChanged;
            btnLaunchGame.Enabled = true;
        }

        private void SetFrameSendRate(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("指令语法：/FrameSendRate <数字>");
                return;
            }

            FrameSendRate = intValue;
            AddNotice("FrameSendRate已更改为" + intValue);

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetMaxAhead(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("指令语法：/MaxAhead <数字>");
                return;
            }

            MaxAhead = intValue;
            AddNotice("MaxAhead已更改为" + intValue);

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetProtocolVersion(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("指令语法：/ProtocolVersion <数字>.");
                return;
            }

            if (!(intValue == 0 || intValue == 2))
            {
                AddNotice("ProtocolVersion只能设置为0和2");
                return;
            }

            ProtocolVersion = intValue;
            AddNotice("ProtocolVersion已更改为" + intValue);

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void SetStartingLocationClearance(string value)
        {
            bool removeStartingLocations = Conversions.BooleanFromString(value, RemoveStartingLocations);

            SetRandomStartingLocations(removeStartingLocations);

            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        /// <summary>
        /// Enables or disables completely random starting locations and informs
        /// the user accordingly.
        /// </summary>
        /// <param name="newValue">The new value of completely random starting locations.</param>
        protected void SetRandomStartingLocations(bool newValue)
        {
            if (newValue != RemoveStartingLocations)
            {
                RemoveStartingLocations = newValue;
                if (RemoveStartingLocations)
                    AddNotice("房主启用了随机起始位置（仅适用于常规地图）..");
                else
                    AddNotice("房主取消了随机起始位置");
            }
        }

        /// <summary>
        /// Handles the dice rolling command.
        /// </summary>
        /// <param name="dieType">The parameters given for the command by the user.</param>
        private void RollDiceCommand(string dieType)
        {
            int dieSides = 6;
            int dieCount = 1;

            if (!string.IsNullOrEmpty(dieType))
            {
                string[] parts = dieType.Split('d');
                if (parts.Length == 2)
                {
                    if (!int.TryParse(parts[0], out dieCount) || !int.TryParse(parts[1], out dieSides))
                    {
                        AddNotice("指定的骰子无效，格式：/roll <骰子数>d<骰子面数>");
                        return;
                    }
                }
            }

            if (dieCount > MAX_DICE || dieCount < 1)
            {
                AddNotice("一次只能投1~10个骰子");
                return;
            }
            
            if (dieSides > MAX_DIE_SIDES || dieSides < 2)
            {
                AddNotice("骰子只能有2~100个面");
                return;
            }

            int[] results = new int[dieCount];
            Random random = new Random();
            for (int i = 0; i < dieCount; i++)
            {
                results[i] = random.Next(1, dieSides + 1);
            }

            BroadcastDiceRoll(dieSides, results);
        }

        /// <summary>
        /// Handles custom map load command.
        /// </summary>
        /// <param name="mapName">Name of the map given as a parameter, without file extension.</param>
        private void LoadCustomMap(string mapName)
        {
            Map map = MapLoader.LoadCustomMap($"Maps/Custom/{mapName}", out string resultMessage);
            if (map != null)
                AddNotice(resultMessage);
            else
                AddNotice(resultMessage, Color.Red);
        }

        /// <summary>
        /// Override in derived classes to broadcast the results of rolling dice to other players.
        /// </summary>
        /// <param name="dieSides">The number of sides in the dice.</param>
        /// <param name="results">The results of the dice roll.</param>
        protected abstract void BroadcastDiceRoll(int dieSides, int[] results);

        /// <summary>
        /// Parses and lists the results of rolling dice.
        /// </summary>
        /// <param name="senderName">The player that rolled the dice.</param>
        /// <param name="result">The results of rolling dice, with each die separated by a comma
        /// and the number of sides in the die included as the first number.</param>
        /// <example>
        /// HandleDiceRollResult("Rampastring", "6,3,5,1") would mean that
        /// Rampastring rolled three six-sided dice and got 3, 5 and 1.
        /// </example>
        protected void HandleDiceRollResult(string senderName, string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            string[] parts = result.Split(',');
            if (parts.Length < 2 || parts.Length > MAX_DICE + 1)
                return;

            int[] intArray = Array.ConvertAll(parts, (s) => { return Conversions.IntFromString(s, -1); });
            int dieSides = intArray[0];
            if (dieSides < 1 || dieSides > MAX_DIE_SIDES)
                return;
            int[] results = new int[intArray.Length - 1];
            Array.ConstrainedCopy(intArray, 1, results, 0, results.Length);

            for (int i = 1; i < intArray.Length; i++)
            {
                if (intArray[i] < 1 || intArray[i] > dieSides)
                    return;
            }

            PrintDiceRollResult(senderName, dieSides, results);
        }

        /// <summary>
        /// Prints the result of rolling dice.
        /// </summary>
        /// <param name="senderName">The player who rolled dice.</param>
        /// <param name="dieSides">The number of sides in the die.</param>
        /// <param name="results">The results of the roll.</param>
        protected void PrintDiceRollResult(string senderName, int dieSides, int[] results)
        {
            AddNotice($"{senderName}使用了骰子{results.Length}d{dieSides}，结果为{string.Join(", ", results)}");
        }

        protected abstract void SendChatMessage(string message);

        /// <summary>
        /// Changes the game lobby's UI depending on whether the local player is the host.
        /// </summary>
        /// <param name="isHost">Determines whether the local player is the host of the game.</param>
        protected void Refresh(bool isHost)
        {
            IsHost = isHost;
            Locked = false;

            UpdateMapPreviewBoxEnabledStatus();
            PlayerExtraOptionsPanel.SetIsHost(isHost);
            //MapPreviewBox.EnableContextMenu = IsHost;

            btnLaunchGame.Text = IsHost ? BTN_LAUNCH_GAME : BTN_LAUNCH_READY;

            if (IsHost)
            {
                ShowMapList();
                BtnSaveLoadGameOptions.Enable();

                btnLockGame.Text = "锁定房间";
                btnLockGame.Enabled = true;
                btnLockGame.Visible = true;
                chkAutoReady.Disable();

                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.InputEnabled = true;
                    dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                {
                    checkBox.AllowChanges = true;
                    checkBox.Checked = checkBox.UserChecked;
                }

                GenerateGameID();
            }
            else
            {
                HideMapList();
                BtnSaveLoadGameOptions.Disable();

                btnLockGame.Enabled = false;
                btnLockGame.Visible = false;
                chkAutoReady.GetAttributes(ThemeIni);

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = false;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.AllowChanges = false;
            }

            LoadDefaultGameModeMap();

            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            lbChatMessages.AddItem("输入/显示聊天命令", Color.Silver, true);

            if (SavedGameManager.GetSaveGameCount() > 0)
            {
                lbChatMessages.AddItem("检测到上一局的存档，创建新存档将覆盖之前的存档。",
                    Color.Yellow, true);
            }
        }

        private void HideMapList()
        {
            lbChatMessages.ClientRectangle = new Rectangle(lbGameModeMapList.X,
                PlayerOptionsPanel.Y,
                lbGameModeMapList.Width,
                MapPreviewBox.Bottom - PlayerOptionsPanel.Y);
            lbChatMessages.Name = "lbChatMessages_Player";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.Name = "tbChatInput_Player";

            ddGameModeMapFilter.Disable();
            lblGameModeSelect.Disable();
            lbGameModeMapList.Disable();
            tbMapSearch.Disable();
            btnPickRandomMap.Disable();

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbGameModeMapList.GetAttributes(ThemeIni);
        }

        private void ShowMapList()
        {
            lbGameModeMapList.ClientRectangle = new Rectangle(btnLaunchGame.X,
                MapPreviewBox.Y + 23,
                MapPreviewBox.X - btnLaunchGame.X - 12,
                MapPreviewBox.Height - 23);

            lbChatMessages.ClientRectangle = new Rectangle(lbGameModeMapList.X,
                GameOptionsPanel.Y,
                lbGameModeMapList.Width, GameOptionsPanel.Height - 26);
            lbChatMessages.Name = "lbChatMessages_Host";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3,
                lbChatMessages.Width, 21);
            tbChatInput.Name = "tbChatInput_Host";

            ddGameModeMapFilter.Enable();
            lblGameModeSelect.Enable();
            lbGameModeMapList.Enable();
            tbMapSearch.Enable();
            btnPickRandomMap.GetAttributes(ThemeIni);

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbGameModeMapList.GetAttributes(ThemeIni);
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            int mTopIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (mTopIndex == -1 || Players[mTopIndex].SideId == ddPlayerSides[0].Items.Count - 1)
                return;

            ddPlayerStarts[mTopIndex].SelectedIndex = e.StartingLocationIndex;
        }

        private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
        {
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Handles the user's click on the "Launch Game" / "I'm ready" button.
        /// If the local player is the game host, checks if the game can be launched and then
        /// launches the game if it's allowed. If the local player isn't the game host,
        /// sends a ready request.
        /// </summary>
        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                RequestReadyStatus();
                return;
            }

            if (!Locked)
            {
                LockGameNotification();
                return;
            }

            var teamMappingsError = GetTeamMappingsError();
            if (!string.IsNullOrEmpty(teamMappingsError))
            {
                AddNotice(teamMappingsError);
                return;
            }

            List<int> occupiedColorIds = new List<int>();
            foreach (PlayerInfo player in Players)
            {
                if (occupiedColorIds.Contains(player.ColorId) && player.ColorId > 0)
                {
                    SharedColorsNotification();
                    return;
                }

                occupiedColorIds.Add(player.ColorId);
            }

            if (AIPlayers.Count(pInfo => pInfo.SideId == ddPlayerSides[0].Items.Count - 1) > 0)
            {
                AISpectatorsNotification();
                return;
            }

            if (Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in Players)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (Players.Concat(AIPlayers).ToList().Find(
                        p => p.StartingLocation == pInfo.StartingLocation && 
                        p.Name != pInfo.Name) != null)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int startingLocation = AIPlayers[aiId].StartingLocation;

                    if (startingLocation == 0)
                        continue;

                    int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                    if (index > -1 && index != aiId)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                    + AIPlayers.Count;

                int minPlayers = GameMode.MinPlayersOverride > -1 ? GameMode.MinPlayersOverride : Map.MinPlayers;
                if (totalPlayerCount < minPlayers)
                {
                    InsufficientPlayersNotification();
                    return;
                }

                if (Map.EnforceMaxPlayers && totalPlayerCount > Map.MaxPlayers)
                {
                    TooManyPlayersNotification();
                    return;
                }
            }

            int iId = 0;
            foreach (PlayerInfo player in Players)
            {
                iId++;

                if (player.Name == ProgramConstants.PLAYERNAME)
                    continue;

                if (!player.Verified)
                {
                    NotVerifiedNotification(iId - 1);
                    return;
                }

                if (!player.Ready)
                {
                    if (player.IsInGame)
                    {
                        StillInGameNotification(iId - 1);
                    }
                    else
                    {
                        GetReadyNotification();
                    }

                    return;
                }
            }

            HostLaunchGame();
        }

        protected virtual void LockGameNotification() =>
            AddNotice("锁定房间后才能开始游戏");

        protected virtual void SharedColorsNotification() =>
            AddNotice("多名人类玩家不能使用相同的颜色");

        protected virtual void AISpectatorsNotification() =>
            AddNotice("电脑玩家不能使用观察者模式");

        protected virtual void SharedStartingLocationNotification() =>
            AddNotice("多名玩家不能使用相同的起始位置");

        protected virtual void NotVerifiedNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
                AddNotice(string.Format("游戏启动失败，{0}未验证。", Players[playerIndex].Name));
        }

        protected virtual void StillInGameNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice("游戏启动失败，" + Players[playerIndex].Name +
                    "仍在上一局游戏中");
            }
        }

        protected virtual void GetReadyNotification()
        {
            AddNotice("房主不能启动游戏，因为不是所有玩家都已准备好");
            if (!IsHost && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
                sndGetReadySound.Play();
        }

        protected virtual void InsufficientPlayersNotification()
        {
            if (GameMode != null && GameMode.MinPlayersOverride > -1)
                AddNotice("游戏启动失败，" + GameMode.UIName + "不能少于" + GameMode.MinPlayersOverride + "人");
            else if (Map != null)
                AddNotice("游戏启动失败，该地图不能少于" + Map.MinPlayers + "人");
        }

        protected virtual void TooManyPlayersNotification()
        {
            if (Map != null)
                AddNotice("游戏启动失败：该地图不能超过" + Map.MaxPlayers + "人");
        }

        public virtual void Clear()
        {
            if (!IsHost)
                AIPlayers.Clear();

            Players.Clear();
        }

        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            ClearReadyStatuses();
            CopyPlayerDataToUI();
        }

        protected abstract void HostLaunchGame();

        protected override void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            if (IsHost)
            {
                base.CopyPlayerDataFromUI(sender, e);
                BroadcastPlayerOptions();
                return;
            }

            int mTopIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (mTopIndex == -1)
                return;

            int requestedSide = ddPlayerSides[mTopIndex].SelectedIndex;
            int requestedColor = ddPlayerColors[mTopIndex].SelectedIndex;
            int requestedStart = ddPlayerStarts[mTopIndex].SelectedIndex;
            int requestedTeam = ddPlayerTeams[mTopIndex].SelectedIndex;

            RequestPlayerOptions(requestedSide, requestedColor, requestedStart, requestedTeam);
        }

        protected override void CopyPlayerDataToUI()
        {
            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT)
                return;

            base.CopyPlayerDataToUI();

            ClearPingIndicators();

            if (IsHost)
            {
                for (int pId = 1; pId < Players.Count; pId++)
                    ddPlayerNames[pId].AllowDropDown = true;
            }

            for (int pId = 0; pId < Players.Count; pId++)
            {
                ReadyBoxes[pId].Checked = Players[pId].Ready;
                UpdatePlayerPingIndicator(Players[pId]);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                ReadyBoxes[aiId + Players.Count].Checked = true;
            }

            for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
            {
                ReadyBoxes[i].Checked = false;
            }
        }

        protected virtual void ClearPingIndicators()
        {
            foreach (XNAClientDropDown dd in ddPlayerNames)
            {
                dd.Items[0].Texture = null;
                dd.ToolTip.Text = string.Empty;
            }
        }

        protected virtual void UpdatePlayerPingIndicator(PlayerInfo pInfo)
        {
            XNAClientDropDown ddPlayerName = ddPlayerNames[pInfo.Index];
            ddPlayerName.Items[0].Texture = GetTextureForPing(pInfo.Ping);
            if (pInfo.Ping < 0)
                ddPlayerName.ToolTip.Text = "Ping: ? ms";
            else
                ddPlayerName.ToolTip.Text = $"Ping: {pInfo.Ping} ms";
        }

        private Texture2D GetTextureForPing(int ping)
        {
            switch (ping)
            {
                case int p when (p > 350):
                    return PingTextures[4];
                case int p when (p > 250):
                    return PingTextures[3];
                case int p when (p > 100):
                    return PingTextures[2];
                case int p when (p >= 0):
                    return PingTextures[1];
                default:
                    return PingTextures[0];
            }
        }

        protected abstract void BroadcastPlayerOptions();

        protected abstract void BroadcastPlayerExtraOptions();

        protected abstract void RequestPlayerOptions(int side, int color, int start, int team);

        protected abstract void RequestReadyStatus();

        // this public as it is used by the main lobby to notify the user of invitation failure
        public void AddWarning(string message)
        {
            AddNotice(message, Color.Yellow);
        }

        protected override bool AllowPlayerOptionsChange() => IsHost;

        protected override void ChangeMap(GameModeMap gameModeMap)
        {
            base.ChangeMap(gameModeMap);

            bool resetAutoReady = gameModeMap?.GameMode == null || gameModeMap?.Map == null;

            ClearReadyStatuses(resetAutoReady);

            if ((lastMapChangeWasInvalid || resetAutoReady) && chkAutoReady.Checked)
                RequestReadyStatus();

            lastMapChangeWasInvalid = resetAutoReady;

            //if (IsHost)
            //    OnGameOptionChanged();
        }

        protected override void ToggleFavoriteMap()
        {
            base.ToggleFavoriteMap();
            
            if (GameModeMap.IsFavorite || !IsHost)
                return;

            RefreshForFavoriteMapRemoved();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);
            iniFile.SetIntValue("Settings", "FrameSendRate", FrameSendRate);
            if (MaxAhead > 0)
                iniFile.SetIntValue("Settings", "MaxAhead", MaxAhead);
            iniFile.SetIntValue("Settings", "Protocol", ProtocolVersion);
        }

        protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
        {
            if (gameModeMap.Map.MaxPlayers > 3)
                return StatisticsManager.Instance.GetCoopRankForDefaultMap(gameModeMap.Map.Name, gameModeMap.Map.MaxPlayers);

            if (StatisticsManager.Instance.HasWonMapInPvP(gameModeMap.Map.Name, gameModeMap.GameMode.UIName, gameModeMap.Map.MaxPlayers))
                return 2;

            return -1;
        }

        public void SwitchOn() => Enable();

        public void SwitchOff() => Disable();

        public abstract string GetSwitchName();

        protected override void UpdateMapPreviewBoxEnabledStatus()
        {
            if (Map != null && GameMode != null)
            {
                bool disablestartlocs = (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations || GetPlayerExtraOptions().IsForceRandomStarts);
                MapPreviewBox.EnableContextMenu = disablestartlocs ? false : IsHost;
                MapPreviewBox.EnableStartLocationSelection = !disablestartlocs;
            }
            else
            {
                MapPreviewBox.EnableContextMenu = IsHost;
                MapPreviewBox.EnableStartLocationSelection = true;
            }
        }
    }
}
