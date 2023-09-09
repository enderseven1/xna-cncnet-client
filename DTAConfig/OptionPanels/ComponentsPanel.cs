using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Updater;

namespace DTAConfig.OptionPanels
{
    class ComponentsPanel : XNAOptionsPanel
    {
        public ComponentsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        List<XNAClientButton> installationButtons = new List<XNAClientButton>();

        bool downloadCancelled = false;

        public override void Initialize()
        {
            base.Initialize();

            Name = "ComponentsPanel";

            int componentIndex = 0;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent c in CUpdater.CustomComponents)
            {
                string buttonText = "不可用";

                if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
                {
                    buttonText = "卸载";

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "更新";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "安装";
                    }
                }

                var btn = new XNAClientButton(WindowManager);
                btn.Name = "btn" + c.ININame;
                btn.ClientRectangle = new Rectangle(Width - 145,
                    12 + componentIndex * 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
                btn.Text = buttonText;
                btn.Tag = c;
                btn.LeftClick += Btn_LeftClick;

                var lbl = new XNALabel(WindowManager);
                lbl.Name = "lbl" + c.ININame;
                lbl.ClientRectangle = new Rectangle(12, btn.Y + 2, 0, 0);
                lbl.Text = c.GUIName;

                AddChild(btn);
                AddChild(lbl);

                installationButtons.Add(btn);

                componentIndex++;
            }
        }

        public override void Load()
        {
            base.Load();

            int componentIndex = 0;
            bool buttonEnabled;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent c in CUpdater.CustomComponents)
            {
                string buttonText = "不可用";
                buttonEnabled = false;

                if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
                {
                    buttonText = "下载";
                    buttonEnabled = true;

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "更新（" + GetSizeString(c.RemoteSize) + "）";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "安装（" + GetSizeString(c.RemoteSize) + "）";
                        buttonEnabled = true;
                    }
                }

                installationButtons[componentIndex].Text = buttonText;
                installationButtons[componentIndex].AllowClick = buttonEnabled;

                componentIndex++;
            }
        }

        public override bool Save()
        {
            return base.Save();
        }

        private void Btn_LeftClick(object sender, EventArgs e)
        {
            var btn = (XNAClientButton)sender;

            var cc = (CustomComponent)btn.Tag;

            if (cc.IsBeingDownloaded)
                return;

            if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
            {
                if (cc.LocalIdentifier == cc.RemoteIdentifier)
                {
                    File.Delete(ProgramConstants.GamePath + cc.LocalPath);
                    btn.Text = "安装";
                    return;
                }

                btn.AllowClick = false;

                cc.DownloadFinished += cc_DownloadFinished;
                cc.DownloadProgressChanged += cc_DownloadProgressChanged;
                Thread thread = new Thread(cc.DownloadComponent);
                thread.Start();
            }
            else
            {
                var msgBox = new XNAMessageBox(WindowManager, "需要确认",
                    "需要下载必要文件才能启用" + cc.GUIName + "。" +
                    Environment.NewLine + Environment.NewLine +
                    "这会占用" + GetSizeString(cc.RemoteSize) + "的空间。" +
                    Environment.NewLine + Environment.NewLine +
                    "完成之前将不能游玩，确定要下载吗？", XNAMessageBoxButtons.YesNo);
                msgBox.Tag = btn;

                msgBox.Show();
                msgBox.YesClickedAction = MsgBox_YesClicked;
            }
        }

        private void MsgBox_YesClicked(XNAMessageBox messageBox)
        {
            var btn = (XNAClientButton)messageBox.Tag;
            btn.AllowClick = false;

            var cc = (CustomComponent)btn.Tag;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            Thread thread = new Thread(cc.DownloadComponent);
            thread.Start();
        }

        public void InstallComponent(int id)
        {
            var btn = installationButtons[id];
            btn.AllowClick = false;

            var cc = (CustomComponent)btn.Tag;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            Thread thread = new Thread(cc.DownloadComponent);
            thread.Start();
        }

        /// <summary>
        /// Called whenever a custom component download's progress is changed.
        /// </summary>
        /// <param name="c">The CustomComponent object.</param>
        /// <param name="percentage">The current download progress percentage.</param>
        private void cc_DownloadProgressChanged(CustomComponent c, int percentage)
        {
            WindowManager.AddCallback(new Action<CustomComponent, int>(HandleDownloadProgressChanged), c, percentage);
        }

        private void HandleDownloadProgressChanged(CustomComponent cc, int percentage)
        {
            percentage = Math.Min(percentage, 100);

            var btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));
            btn.Text = "下载中.. " + percentage + "%";
        }

        /// <summary>
        /// Called whenever a custom component download is finished.
        /// </summary>
        /// <param name="c">The CustomComponent object.</param>
        /// <param name="success">True if the download succeeded, otherwise false.</param>
        private void cc_DownloadFinished(CustomComponent c, bool success)
        {
            WindowManager.AddCallback(new Action<CustomComponent, bool>(HandleDownloadFinished), c, success);
        }

        private void HandleDownloadFinished(CustomComponent cc, bool success)
        {
            cc.DownloadFinished -= cc_DownloadFinished;
            cc.DownloadProgressChanged -= cc_DownloadProgressChanged;

            var btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));
            btn.AllowClick = true;

            if (!success)
            {
                if (!downloadCancelled)
                {
                    XNAMessageBox.Show(WindowManager, "组件下载失败",
                        string.Format("{0}下载失败。" + Environment.NewLine +
                        "查看client.log获取详细信息。" + Environment.NewLine + Environment.NewLine +
                        "如果错误依然存在，请联系MOD作者。",
                        cc.GUIName));
                }

                btn.Text = "安装（" + GetSizeString(cc.RemoteSize) + "）";

                if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
                    btn.Text = "更新（" + GetSizeString(cc.RemoteSize) + "）";
            }
            else
            {
                XNAMessageBox.Show(WindowManager, "下载完成",
                    string.Format("{0}已下载完成。", cc.GUIName));
                btn.Text = "下载";
            }
        }

        public void CancelAllDownloads()
        {
            Logger.Log("取消全部下载");

            downloadCancelled = true;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent cc in CUpdater.CustomComponents)
            {
                if (cc.IsBeingDownloaded)
                    cc.StopDownload();
            }
        }

        public void Open()
        {
            downloadCancelled = false;
        }

        private string GetSizeString(long size)
        {
            if (size < 1048576)
            {
                return (size / 1024) + " KB";
            }
            else
            {
                return (size / 1048576) + " MB";
            }
        }
    }
}
