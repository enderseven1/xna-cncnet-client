using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers
{
    public class LogFileStatisticsParser : GenericMatchParser
    {
        public LogFileStatisticsParser(MatchStatistics ms, bool isLoadedGame) : base(ms)
        {
            this.isLoadedGame = isLoadedGame;
        }

        private string fileName = "DTA.log";
        private string economyString = "Economy"; // RA2/YR do not have economy stat, but a number of built objects.
        private bool isLoadedGame;

        public void ParseStats(string gamepath, string fileName)
        {
            this.fileName = fileName;
            if (ClientConfiguration.Instance.UseBuiltStatistic) economyString = "建造数";
            ParseStatistics(gamepath);
        }

        protected override void ParseStatistics(string gamepath)
        {
            if (!File.Exists(gamepath + fileName))
            {
                Logger.Log("DTAStatisticsParser: 读取统计数据失败：日志文件不存在。");
                return;
            }

            Logger.Log("尝试从" + fileName + "读取统计数据");

            try
            {
                StreamReader reader = new StreamReader(File.OpenRead(gamepath + fileName));

                string line;

                List<PlayerStatistics> takeoverAIs = new List<PlayerStatistics>();
                PlayerStatistics currentPlayer = null;

                bool sawCompletion = false;
                int numPlayersFound = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(": Loser"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line.Substring(0, line.Length - 7);
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        Logger.Log("找到玩家" + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            Logger.Log("找到失败接管AI");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[takeoverAIs.Count - 1];
                        }

                        if (currentPlayer != null)
                            currentPlayer.SawEnd = true;
                    }
                    else if (line.Contains(": Winner"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line.Substring(0, line.Length - 8);
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        Logger.Log("找到玩家" + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            Logger.Log("找到胜利接管AI");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[takeoverAIs.Count - 1];
                        }

                        if (currentPlayer != null)
                        {
                            currentPlayer.SawEnd = true;
                            currentPlayer.Won = true;
                        }
                    }
                    else if (line.Contains("游戏循环结束。平均FPS"))
                    {
                        // Game loop finished. Average FPS = <integer>
                        string fpsString = line.Substring(34);
                        Statistics.AverageFPS = Int32.Parse(fpsString);
                    }

                    if (currentPlayer == null || line.Length < 1)
                        continue;

                    line = line.Substring(1);

                    if (line.StartsWith("Lost = "))
                        currentPlayer.Losses = Int32.Parse(line.Substring(7));
                    else if (line.StartsWith("Kills = "))
                        currentPlayer.Kills = Int32.Parse(line.Substring(8));
                    else if (line.StartsWith("Score = "))
                        currentPlayer.Score = Int32.Parse(line.Substring(8));
                    else if (line.StartsWith("Built = "))
                        currentPlayer.Economy = Int32.Parse(line.Substring(8));
                }

                reader.Close();

                // Check empty players for take-over by AIs
                if (takeoverAIs.Count == 1)
                {
                    PlayerStatistics ai = takeoverAIs[0];

                    PlayerStatistics ps = Statistics.GetFirstEmptyPlayer();

                    ps.Losses = ai.Losses;
                    ps.Kills = ai.Kills;
                    ps.Score = ai.Score;
                    ps.Economy = ai.Economy;
                }
                else if (takeoverAIs.Count > 1)
                {
                    // If there's multiple take-over AI players, we have no way of figuring out
                    // which AI represents which player, so let's just add the AIs into the player list
                    // (then the user viewing the statistics can figure it out themselves)
                    for (int i = 0; i < takeoverAIs.Count; i++)
                    {
                        takeoverAIs[i].SawEnd = false;
                        Statistics.AddPlayer(takeoverAIs[i]);
                    }
                }

                Statistics.SawCompletion = sawCompletion;
            }
            catch (Exception ex)
            {
                Logger.Log("DTAStatisticsParser: 解析匹配统计信息时出错！信息：" + ex.Message);
            }
        }
    }
}
