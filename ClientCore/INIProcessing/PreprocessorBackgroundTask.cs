using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.INIProcessing
{
    /// <summary>
    /// Background task for pre-processing INI files.
    /// Singleton.
    /// </summary>
    public class PreprocessorBackgroundTask
    {
        private PreprocessorBackgroundTask()
        {
        }

        private static PreprocessorBackgroundTask _instance;
        public static PreprocessorBackgroundTask Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PreprocessorBackgroundTask();

                return _instance;
            }
        }

        private Task task;

        public bool IsRunning => !task.IsCompleted;

        public void Run()
        {
            task = Task.Factory.StartNew(() => CheckFiles());
        }

        private void CheckFiles()
        {
            Logger.Log("启动INI文件的后台处理。");

            if (!Directory.Exists(ProgramConstants.GamePath + "INI/Base"))
            {
                Logger.Log("/INI/Base不存在，跳过INI文件的后台处理。");
                return;
            }

            IniPreprocessInfoStore infoStore = new IniPreprocessInfoStore();
            infoStore.Load();

            IniPreprocessor processor = new IniPreprocessor();

            string[] iniFiles = Directory.GetFiles(ProgramConstants.GamePath + "INI/Base", "*.ini", SearchOption.TopDirectoryOnly);
            iniFiles = Array.ConvertAll(iniFiles, s => Path.GetFileName(s));

            int processedCount = 0;

            foreach (string fileName in iniFiles)
            {
                if (!infoStore.IsIniUpToDate(fileName))
                {
                    Logger.Log("INI文件" + fileName + "未处理或过期，重新处理。");

                    string sourcePath = $"{ProgramConstants.GamePath}INI/Base/{fileName}";
                    string destinationPath = $"{ProgramConstants.GamePath}INI/{fileName}";

                    processor.ProcessIni(sourcePath, destinationPath);

                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(destinationPath);
                    infoStore.UpsertRecord(fileName, sourceHash, destinationHash);
                    processedCount++;
                }
                else
                {
                    Logger.Log("INI文件" + fileName + "已是最新。");
                }
            }

            if (processedCount > 0)
            {
                Logger.Log("编写预处理的INI信息存储。");
                infoStore.Write();
            }

            Logger.Log("结束INI文件的后台处理。");
        }
    }
}
