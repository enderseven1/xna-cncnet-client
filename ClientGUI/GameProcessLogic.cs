using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using ClientCore;
using Rampastring.Tools;
using ClientCore.INIProcessing;
using System.Threading;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public static class GameProcessLogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        public static void StartGameProcess()
        {
            Logger.Log("即将启动游戏主程序。");

            // In the relatively unlikely event that INI preprocessing is still going on, just wait until it's done.
            // TODO ideally this should be handled in the UI so the client doesn't appear just frozen for the user.
            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    MessageBox.Show("INI 预处理未完成，请尝试重新启动游戏。如果问题仍然存在，请联系游戏或MOD作者寻求支持。");
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string gameExecutableName;
            string additionalExecutableName = string.Empty;

            if (osVersion == OSVersion.UNIX)
                gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
            else
            {
                string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
                if (string.IsNullOrEmpty(launcherExecutableName))
                    gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
                else
                {
                    gameExecutableName = launcherExecutableName;
                    additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
                }
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            File.Delete(ProgramConstants.GamePath + "DTA.LOG");
            File.Delete(ProgramConstants.GamePath + "TI.LOG");
            File.Delete(ProgramConstants.GamePath + "TS.LOG");

            GameProcessStarting?.Invoke();
            
            if (UserINISettings.Instance.WindowedMode && UseQres)
			{
                Logger.Log("窗口化模式启动 - 使用QRes。");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" "  + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + additionalExecutableName  + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                Logger.Log("启动程序：" + QResProcess.StartInfo.FileName);
                Logger.Log("启动参数：" + QResProcess.StartInfo.Arguments);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("QRes启动出错：" + ex.Message);
                    MessageBox.Show("" + ProgramConstants.QRES_EXECUTABLE + "启动出错。请检查你的防病毒软件是否阻止了CnCNet客户端。你也可以尝试以管理员身份运行客户端。" + Environment.NewLine + Environment.NewLine + "你无法参与。" +
                        Environment.NewLine + Environment.NewLine + "返回错误：" + ex.Message,
                        "游戏启动出错", MessageBoxButtons.OK);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = gameExecutableName;
                DtaProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    DtaProcess.StartInfo.Arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    DtaProcess.StartInfo.Arguments = additionalExecutableName + "-SPAWN";
                DtaProcess.EnableRaisingEvents = true;
                DtaProcess.Exited += new EventHandler(Process_Exited);
                Logger.Log("启动程序：" + DtaProcess.StartInfo.FileName);
                Logger.Log("启动参数：" + DtaProcess.StartInfo.Arguments);
                try
                {
                    DtaProcess.Start();
                    Logger.Log("GameProcessLogic: 进程开始。");
                }
                catch (Exception ex)
                {
                    Logger.Log(gameExecutableName + "启动出错：" + ex.Message);
                    MessageBox.Show(gameExecutableName + "启动出错，请检查防病毒软件或以管理员身份运行客户端。" + Environment.NewLine + Environment.NewLine + "您无法参与。" + 
                        Environment.NewLine + Environment.NewLine + "返回错误：" + ex.Message,
                        "游戏启动出错", MessageBoxButtons.OK);
                    Process_Exited(DtaProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    DtaProcess.ProcessorAffinity = (IntPtr)2;
            }

            GameProcessStarted?.Invoke();

            Logger.Log("等待qres.dat或" + gameExecutableName + "退出。");
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            Logger.Log("GameProcessLogic: 进程退出。");
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
        }
    }
}
