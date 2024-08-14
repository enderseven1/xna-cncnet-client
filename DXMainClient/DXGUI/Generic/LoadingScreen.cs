using System;
using System.Drawing;
using System.Numerics;
// using System.Reflection.Metadata;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using ClientCore.Extensions;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
        }

        private static readonly object locker = new object();

        private MapLoader mapLoader;

        private PrivateMessagingPanel privateMessagingPanel;

        private bool visibleSpriteCursor;

        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        public override void Initialize()
        {
            ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            if (UserINISettings.Instance.EnableChineseNotice && System.Threading.Thread.CurrentThread.CurrentCulture.Name == "zh-CN")
            {
                /*
                var lblGameNameAndVersion = new XNALabel(WindowManager);
                lblGameNameAndVersion.Name = "lblGameNameAndVersion";
                lblGameNameAndVersion.Text = ClientConfiguration.Instance.LongGameName + " v." + CUpdater.GameVersion;
                lblGameNameAndVersion.FontIndex = 1;
                lblGameNameAndVersion.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(0,0,0,0);
                */

                var lblJianKangYouXiZhongGao = new XNALabel(WindowManager);
                lblJianKangYouXiZhongGao.Name = "lblJianKangYouXiZhongGao";
                lblJianKangYouXiZhongGao.Text = "抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗上当。" +
                Environment.NewLine + "适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。".L10N("Client:ClientCore:JianKangYouXiZhongGao");
                lblJianKangYouXiZhongGao.FontIndex = 0;
                Microsoft.Xna.Framework.Vector2 textSize2 = Renderer.GetTextDimensions(lblJianKangYouXiZhongGao.Text, lblJianKangYouXiZhongGao.FontIndex);
                lblJianKangYouXiZhongGao.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(
                    (UserINISettings.Instance.ClientResolutionX - (int)textSize2.X) / 2,
                     UserINISettings.Instance.ClientResolutionY - (int)textSize2.Y - 30,
                    (int)textSize2.X, (int)textSize2.Y);

                var lblJianKangYouXiZhongGaoTitle = new XNALabel(WindowManager);
                lblJianKangYouXiZhongGaoTitle.Name = "lblJianKangYouXiZhongGao";
                lblJianKangYouXiZhongGaoTitle.Text = "健康游戏忠告".L10N("Client:ClientCore:JianKangYouXiZhongGaoTitle");
                lblJianKangYouXiZhongGaoTitle.FontIndex = 1;
                Microsoft.Xna.Framework.Vector2 textSize = Renderer.GetTextDimensions(lblJianKangYouXiZhongGaoTitle.Text, lblJianKangYouXiZhongGaoTitle.FontIndex);
                lblJianKangYouXiZhongGaoTitle.ClientRectangle = new Microsoft.Xna.Framework.Rectangle(
                    (UserINISettings.Instance.ClientResolutionX - (int)textSize.X) / 2,
                     UserINISettings.Instance.ClientResolutionY - (int)textSize.Y * 3 - 30,
                    (int)textSize.X, (int)textSize.Y);

                // AddChild(lblGameNameAndVersion);
                AddChild(lblJianKangYouXiZhongGao);
                AddChild(lblJianKangYouXiZhongGaoTitle);
            }

            base.Initialize();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
            {
                updaterInitTask = new Task(InitUpdater);
                updaterInitTask.Start();
            }

            mapLoadTask = mapLoader.LoadMapsAsync();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? 
                "N/A" : Updater.GameVersion;

            MainMenu mainMenu = serviceProvider.GetService<MainMenu>();

            WindowManager.AddAndInitializeControl(mainMenu);
            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
        }
    }
}
