using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using Updater;

namespace DTAConfig.OptionPanels
{
    class UpdaterOptionsPanel : XNAOptionsPanel
    {
        public UpdaterOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        public event EventHandler OnForceUpdate;

        private XNAListBox lbUpdateServerList;
        private XNAClientCheckBox chkAutoCheck;
        private XNAClientButton btnForceUpdate;

        public override void Initialize()
        {
            base.Initialize();

            Name = "UpdaterOptionsPanel";

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "使用上移/下移键切换服务器优先级";

            lbUpdateServerList = new XNAListBox(WindowManager);
            lbUpdateServerList.Name = "lblUpdateServerList";
            lbUpdateServerList.ClientRectangle = new Rectangle(lblDescription.X,
                lblDescription.Bottom + 12, Width - 24, 100);
            lbUpdateServerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbUpdateServerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            var btnMoveUp = new XNAClientButton(WindowManager);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.ClientRectangle = new Rectangle(lbUpdateServerList.X,
                lbUpdateServerList.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMoveUp.Text = "上移";
            btnMoveUp.LeftClick += btnMoveUp_LeftClick;

            var btnMoveDown = new XNAClientButton(WindowManager);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.ClientRectangle = new Rectangle(
                lbUpdateServerList.Right - UIDesignConstants.BUTTON_WIDTH_133,
                btnMoveUp.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMoveDown.Text = "下移";
            btnMoveDown.LeftClick += btnMoveDown_LeftClick;

            chkAutoCheck = new XNAClientCheckBox(WindowManager);
            chkAutoCheck.Name = "chkAutoCheck";
            chkAutoCheck.ClientRectangle = new Rectangle(lblDescription.X,
                btnMoveUp.Bottom + 24, 0, 0);
            chkAutoCheck.Text = "自动检查更新";

            btnForceUpdate = new XNAClientButton(WindowManager);
            btnForceUpdate.Name = "btnForceUpdate";
            btnForceUpdate.ClientRectangle = new Rectangle(btnMoveDown.X, btnMoveDown.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnForceUpdate.Text = "强制更新";
            btnForceUpdate.LeftClick += BtnForceUpdate_LeftClick;

            AddChild(lblDescription);
            AddChild(lbUpdateServerList);
            AddChild(btnMoveUp);
            AddChild(btnMoveDown);
            AddChild(chkAutoCheck);
            AddChild(btnForceUpdate);
        }

        private void BtnForceUpdate_LeftClick(object sender, EventArgs e)
        {
            var msgBox = new XNAMessageBox(WindowManager, "强制更新确认",
                    "警告：强制更新会重新下载验证所有文件，修复错误的同时也会删" + Environment.NewLine +
                    "除部分自定义修改，请不要在他人的游戏中进行。" +
                    Environment.NewLine + Environment.NewLine +
                    "确认后将关闭此窗口并继续检查更新。" + 
                    Environment.NewLine + Environment.NewLine +
                    "确定要强制更新吗？" + Environment.NewLine, XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = ForceUpdateMsgBox_YesClicked;
        }

        private void ForceUpdateMsgBox_YesClicked(XNAMessageBox obj)
        {
            CUpdater.ClearVersionInfo();
            OnForceUpdate?.Invoke(this, EventArgs.Empty);
        }

        private void btnMoveUp_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;

            if (selectedIndex < 1)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex - 1];
            lbUpdateServerList.Items[selectedIndex - 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex--;

            UpdateMirror umtmp = CUpdater.UPDATEMIRRORS[selectedIndex - 1];
            CUpdater.UPDATEMIRRORS[selectedIndex - 1] = CUpdater.UPDATEMIRRORS[selectedIndex];
            CUpdater.UPDATEMIRRORS[selectedIndex] = umtmp;
        }

        private void btnMoveDown_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;

            if (selectedIndex > lbUpdateServerList.Items.Count - 2 || selectedIndex < 0)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex + 1];
            lbUpdateServerList.Items[selectedIndex + 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex++;

            UpdateMirror umtmp = CUpdater.UPDATEMIRRORS[selectedIndex + 1];
            CUpdater.UPDATEMIRRORS[selectedIndex + 1] = CUpdater.UPDATEMIRRORS[selectedIndex];
            CUpdater.UPDATEMIRRORS[selectedIndex] = umtmp;
        }

        public override void Load()
        {
            base.Load();

            lbUpdateServerList.Clear();

            foreach (var updaterMirror in CUpdater.UPDATEMIRRORS)
                lbUpdateServerList.AddItem(updaterMirror.Name + " (" + updaterMirror.Location + ")");

            chkAutoCheck.Checked = IniSettings.CheckForUpdates;
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.CheckForUpdates.Value = chkAutoCheck.Checked;

            IniSettings.SettingsIni.EraseSectionKeys("DownloadMirrors");

            int id = 0;

            foreach (UpdateMirror um in CUpdater.UPDATEMIRRORS)
            {
                IniSettings.SettingsIni.SetStringValue("DownloadMirrors", id.ToString(), um.Name);
                id++;
            }

            return restartRequired;
        }

        public override void ToggleMainMenuOnlyOptions(bool enable)
        {
            btnForceUpdate.AllowClick = enable;
        }
    }
}
