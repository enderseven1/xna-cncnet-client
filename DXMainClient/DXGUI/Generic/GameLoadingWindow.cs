using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A window for loading saved singleplayer games.
    /// </summary>
    public class GameLoadingWindow : XNAWindow
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        public GameLoadingWindow(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private XNAMultiColumnListBox lbSaveGameList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnDelete;
        private XNAClientButton btnCancel;

        private List<SavedGame> savedGames = new List<SavedGame>();

        public override void Initialize()
        {
            Name = "GameLoadingWindow";
            BackgroundTexture = AssetLoader.LoadTexture("loadmissionbg.png");

            ClientRectangle = new Rectangle(0, 0, 600, 380);
            CenterOnParent();

            lbSaveGameList = new XNAMultiColumnListBox(WindowManager);
            lbSaveGameList.Name = nameof(lbSaveGameList);
            lbSaveGameList.ClientRectangle = new Rectangle(13, 13, 574, 317);
            lbSaveGameList.AddColumn("存档名称", 400);
            lbSaveGameList.AddColumn("日期/时间", 174);
            lbSaveGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbSaveGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbSaveGameList.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            lbSaveGameList.AllowKeyboardInput = true;

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = nameof(btnLaunch);
            btnLaunch.ClientRectangle = new Rectangle(125, 345, 110, 23);
            btnLaunch.Text = "载入";
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.ClientRectangle = new Rectangle(btnLaunch.Right + 10, btnLaunch.Y, 110, 23);
            btnDelete.Text = "删除";
            btnDelete.AllowClick = false;
            btnDelete.LeftClick += BtnDelete_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(btnDelete.Right + 10, btnLaunch.Y, 110, 23);
            btnCancel.Text = "取消";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lbSaveGameList);
            AddChild(btnLaunch);
            AddChild(btnDelete);
            AddChild(btnCancel);

            base.Initialize();

            ListSaves();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSaveGameList.SelectedIndex == -1)
            {
                btnLaunch.AllowClick = false;
                btnDelete.AllowClick = false;
            }
            else
            {
                btnLaunch.AllowClick = true;
                btnDelete.AllowClick = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            Logger.Log("加载存档" + sg.FileName);

            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);
            StreamWriter sw = new StreamWriter(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);
            sw.WriteLine("; generated by DTA Client");
            sw.WriteLine("[Settings]");
            sw.WriteLine("Scenario=spawnmap.ini");
            sw.WriteLine("SaveGameName=" + sg.FileName);
            sw.WriteLine("LoadSaveGame=Yes");
            sw.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            sw.WriteLine("Firestorm=No");
            sw.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
            sw.WriteLine();
            sw.Close();

            File.Delete(ProgramConstants.GamePath + "spawnmap.ini");
            sw = new StreamWriter(ProgramConstants.GamePath + "spawnmap.ini");
            sw.WriteLine("[Map]");
            sw.WriteLine("Size=0,0,50,50");
            sw.WriteLine("LocalSize=0,0,50,50");
            sw.WriteLine();
            sw.Close();

            discordHandler?.UpdatePresence(sg.GUIName, true);

            Enabled = false;
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess();
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            var msgBox = new XNAMessageBox(WindowManager, "删除确认",
                    "将删除以下存档：" + Environment.NewLine +
                    Environment.NewLine +
                    "文件名：" + sg.FileName + Environment.NewLine +
                    "存档名：" + Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex) + Environment.NewLine +
                    "日期/时间：" + sg.LastModified.ToString() + Environment.NewLine +
                    Environment.NewLine +
                    "你确定要继续吗？", XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = DeleteMsgBox_YesClicked;
        }

        private void DeleteMsgBox_YesClicked(XNAMessageBox obj)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];

            Logger.Log("删除存档" + sg.FileName);
            File.Delete(ProgramConstants.GamePath + SAVED_GAMES_DIRECTORY + Path.DirectorySeparatorChar + sg.FileName);
            ListSaves();
        }

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            discordHandler?.UpdatePresence();
        }

        public void ListSaves()
        {
            savedGames.Clear();
            lbSaveGameList.ClearItems();
            lbSaveGameList.SelectedIndex = -1;

            if (!Directory.Exists(ProgramConstants.GamePath + SAVED_GAMES_DIRECTORY))
            {
                Logger.Log("找不到存档文件夹！");
                return;
            }

            string[] files = Directory.GetFiles(ProgramConstants.GamePath + 
                SAVED_GAMES_DIRECTORY + Path.DirectorySeparatorChar,
                "*.SAV", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                ParseSaveGame(file);
            }

            savedGames = savedGames.OrderBy(sg => sg.LastModified.Ticks).ToList();
            savedGames.Reverse();

            foreach (SavedGame sg in savedGames)
            {
                string[] item = new string[] {
                    Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex),
                    sg.LastModified.ToString() };
                lbSaveGameList.AddItem(item, true);
            }
        }

        private void ParseSaveGame(string fileName)
        {
            string shortName = Path.GetFileName(fileName);

            SavedGame sg = new SavedGame(shortName);
            if (sg.ParseInfo())
                savedGames.Add(sg);
        }
    }
}
