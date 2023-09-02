using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class for handling saved multiplayer games.
    /// </summary>
    public static class SavedGameManager
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        private static bool saveRenameInProgress = false;

        public static int GetSaveGameCount()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath() + "/";

            if (!AreSavedGamesAvailable())
                return 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!File.Exists(saveGameDirectory + string.Format("SVGM_{0}.NET", i.ToString("D3"))))
                {
                    return i;
                }
            }

            return 1000;
        }

        public static List<string> GetSaveGameTimestamps()
        {
            int saveGameCount = GetSaveGameCount();

            List<string> timestamps = new List<string>();

            string saveGameDirectory = GetSaveGameDirectoryPath() + "/";

            for (int i = 0; i < saveGameCount; i++)
            {
                string sgPath = saveGameDirectory + string.Format("SVGM_{0}.NET", i.ToString("D3"));

                DateTime dt = File.GetLastWriteTime(sgPath);

                timestamps.Add(dt.ToString());
            }

            return timestamps;
        }

        public static bool AreSavedGamesAvailable()
        {
            if (Directory.Exists(GetSaveGameDirectoryPath()))
                return true;

            return false;
        }

        private static string GetSaveGameDirectoryPath()
        {
            return ProgramConstants.GamePath + SAVED_GAMES_DIRECTORY;
        }

        /// <summary>
        /// Initializes saved MP games for a match.
        /// </summary>
        public static bool InitSavedGames()
        {
            bool success = EraseSavedGames();

            if (!success)
                return false;

            try
            {
                Logger.Log("写入存档的spawn.ini。");
                File.Delete(ProgramConstants.GamePath + SAVED_GAMES_DIRECTORY + "/spawnSG.ini");
                File.Copy(ProgramConstants.GamePath + "spawn.ini", ProgramConstants.GamePath + SAVED_GAMES_DIRECTORY + "/spawnSG.ini");
            }
            catch (Exception ex)
            {
                Logger.Log("写入存档的spawn.ini失败！异常信息：" + ex.Message);
                return false;
            }

            return true;
        }

        public static void RenameSavedGame()
        {
            Logger.Log("重命名存档。");

            if (saveRenameInProgress)
            {
                Logger.Log("正在保存重命名！");
                return;
            }

            string saveGameDirectory = GetSaveGameDirectoryPath() + "/";

            if (!File.Exists(saveGameDirectory + "SAVEGAME.NET"))
            {
                Logger.Log("SAVEGAME.NET不存在！");
                return;
            }

            saveRenameInProgress = true;

            int saveGameId = 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!File.Exists(saveGameDirectory + string.Format("SVGM_{0}.NET", i.ToString("D3"))))
                {
                    saveGameId = i;
                    break;
                }
            }

            if (saveGameId == 999)
            {
                if (File.Exists(saveGameDirectory + "SVGM_999.NET"))
                    Logger.Log("超过1000个存档！覆盖以前的MP保存。");
            }

            string sgPath = saveGameDirectory + string.Format("SVGM_{0}.NET", saveGameId.ToString("D3"));

            int tryCount = 0;

            while (true)
            {
                try
                {
                    File.Move(saveGameDirectory + "SAVEGAME.NET", sgPath);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log("重命名存档失败！异常信息：" + ex.Message);
                }

                tryCount++;

                if (tryCount > 40)
                {
                    Logger.Log("重命名存档失败超过40次！中止。");
                    return;
                }

                System.Threading.Thread.Sleep(250);
            }

            saveRenameInProgress = false;

            Logger.Log("存档SAVEGAME.NET成功改名为" + Path.GetFileName(sgPath));
        }

        public static bool EraseSavedGames()
        {
            Logger.Log("删除以前的MP存档。");

            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    File.Delete(GetSaveGameDirectoryPath() + 
                        "/" + string.Format("SVGM_{0}.NET", i.ToString("D3")));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("删除以前的MP存档失败！异常信息：" + ex.Message);
                return false;
            }

            Logger.Log("MP存档已成功删除。");
            return true;
        }
    }
}
