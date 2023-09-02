using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using DTAClient.Domain;
using Rampastring.Tools;
using ClientCore;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;

namespace DTAClient
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    static class PreStartup
    {
        /// <summary>
        /// Initializes various basic systems like the client's logger, 
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file, 
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExcept);

            Environment.CurrentDirectory = ProgramConstants.GamePath;

            CheckPermissions();

            Logger.Initialize(ProgramConstants.ClientUserFilesPath, "client.log");
            Logger.WriteLogFile = true;

            if (!Directory.Exists(ProgramConstants.ClientUserFilesPath))
                Directory.CreateDirectory(ProgramConstants.ClientUserFilesPath);

            File.Delete(ProgramConstants.ClientUserFilesPath + "client.log");

            MainClientConstants.Initialize();

            Logger.Log("*** " + MainClientConstants.GAME_NAME_LONG + "客户端日志 ***");
            Logger.Log("客户端版本：" + Application.ProductVersion);

            // Log information about given startup params
            if (parameters.NoAudio)
                Logger.Log("启动参数：无音频");

            if (parameters.MultipleInstanceMode)
                Logger.Log("启动参数：允许多个客户端实例");

            parameters.UnknownStartupParams.ForEach(p => Logger.Log("未知启动参数：" + p));

            Logger.Log("加载设置。");

            UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

            // Delete obsolete files from old target project versions

            File.Delete(ProgramConstants.GamePath + "mainclient.log");
            File.Delete(ProgramConstants.GamePath + "launchupdt.dat");
            try
            {
                File.Delete(ProgramConstants.GamePath + "wsock32.dll");
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除wsock32.dll失败！请关闭所有可能正在使用该文件的应用程序，然后重启客户端。"
                    + Environment.NewLine + Environment.NewLine +
                    "信息：" + ex.Message,
                    "CnCNet客户端");
                Environment.Exit(0);
            }

            Application.EnableVisualStyles();

            new Startup().Execute();
        }

        static void HandleExcept(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            Logger.Log("KABOOOOOOM!!! 信息：");
            Logger.Log("信息：" + ex.Message);
            Logger.Log("源：" + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite.Name);
            Logger.Log("堆栈跟踪：" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                Logger.Log("InnerException信息：");
                Logger.Log("信息：" + ex.InnerException.Message);
                Logger.Log("堆栈跟踪：" + ex.InnerException.StackTrace);
            }

            string errorLogPath = Environment.CurrentDirectory.Replace("\\", "/") + "/Client/ClientCrashLogs/ClientCrashLog" +
                DateTime.Now.ToString("_yyyy_MM_dd_HH_mm") + ".txt";
            bool crashLogCopied = false;

            try
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "/Client/ClientCrashLogs"))
                    Directory.CreateDirectory(Environment.CurrentDirectory + "/Client/ClientCrashLogs");

                File.Copy(Environment.CurrentDirectory + "/Client/client.log", errorLogPath, true);
                crashLogCopied = true;
            }
            catch { }

            MessageBox.Show(string.Format("{0}崩溃了。错误信息：" + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
                "崩溃日志已保存到以下文件：" + Environment.NewLine + Environment.NewLine +
                errorLogPath + Environment.NewLine + Environment.NewLine : "") +
                "如果问题重复出现，请通过{2}" + (crashLogCopied ? "联系{1}工作人员并提供崩溃日志文件" : "") + "。",
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT),
                "KABOOOOOOOM", MessageBoxButtons.OK);
        }

        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(Environment.CurrentDirectory, FileSystemRights.Modify))
                return;

            DialogResult dr = MessageBox.Show(string.Format("你似乎正在从写保护目录运行{0}。" + Environment.NewLine + Environment.NewLine +
                "{1}需要管理员权限才能在只读目录下正常运行。" + Environment.NewLine + Environment.NewLine +
                "以管理员权限重新启动客户端？" + Environment.NewLine + Environment.NewLine +
                "还请确保你的安全软件没有阻止{1}。", MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT),
                "需要管理员权限", MessageBoxButtons.YesNo);

            if (dr == DialogResult.No)
                Environment.Exit(0);

            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = Application.ExecutablePath.Replace('\\', '/');
            psInfo.Verb = "runas";
            Process.Start(psInfo);
            Environment.Exit(0);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
#if WINDOWSGL
            // Mono doesn't implement everything necessary for the below to work,
            // so we'll just return to make the client able to run on non-Windows
            // platforms
            // On Windows you rarely have a reason for using the OpenGL build anyway
            return true;
#endif

#pragma warning disable 0162
            var currentUser = WindowsIdentity.GetCurrent();
#pragma warning restore 0162
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (Environment.CurrentDirectory.Contains(progfiles) || Environment.CurrentDirectory.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }
    }
}
