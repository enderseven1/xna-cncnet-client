using System.Collections.Generic;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Graphics;
using ClientCore.Properties;
using System.Linq;
using System;
using Rampastring.Tools;

namespace ClientCore.CnCNet5
{
    /// <summary>
    /// A class for storing the collection of supported CnCNet games.
    /// </summary>
    public class GameCollection
    {
        public List<CnCNetGame> GameList { get; private set; }

        public void Initialize(GraphicsDevice gd)
        {
            GameList = new List<CnCNetGame>();

            // Default supported games.
            CnCNetGame[] defaultGames = new CnCNetGame[]
            {
                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-dta",
                    ClientExecutableName = "DTA.exe",
                    GameBroadcastChannel = "#cncnet-dta-games",
                    InternalName = "dta",
                    RegistryInstallPath = "HKCU\\Software\\TheDawnOfTheTiberiumAge",
                    UIName = "泰伯利亚时代黎明",
                    Texture = AssetLoader.TextureFromImage(Resources.dtaicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ti",
                    ClientExecutableName = "TI_Launcher.exe",
                    GameBroadcastChannel = "#cncnet-ti-games",
                    InternalName = "ti",
                    RegistryInstallPath = "HKCU\\Software\\TwistedInsurrection",
                    UIName = "扭曲的暴动",
                    Texture = AssetLoader.TextureFromImage(Resources.tiicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ts",
                    ClientExecutableName = "TiberianSun.exe",
                    GameBroadcastChannel = "#cncnet-ts-games",
                    InternalName = "ts",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Sun",
                    UIName = "泰伯利亚之日",
                    Texture = AssetLoader.TextureFromImage(Resources.tsicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-mo",
                    ClientExecutableName = "MentalOmegaClient.exe",
                    GameBroadcastChannel = "#cncnet-mo-games",
                    InternalName = "mo",
                    RegistryInstallPath = "HKCU\\Software\\MentalOmega",
                    UIName = "心灵终结",
                    Texture = AssetLoader.TextureFromImage(Resources.moicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-yr",
                    ClientExecutableName = "CnCNetClientYR.exe",
                    GameBroadcastChannel = "#cncnet-yr-games",
                    InternalName = "yr",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Yuri's Revenge",
                    UIName = "尤里的复仇",
                    Texture = AssetLoader.TextureFromImage(Resources.yricon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#redres-lobby",
                    ClientExecutableName = "RRLauncher.exe",
                    GameBroadcastChannel = "#redres-games",
                    InternalName = "rr",
                    RegistryInstallPath = "HKML\\Software\\RedResurrection",
                    UIName = "红色复活",
                    Texture = AssetLoader.TextureFromImage(Resources.rricon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncreloaded",
                    ClientExecutableName = "CnCReloadedClient.exe",
                    GameBroadcastChannel = "#cncreloaded-games",
                    InternalName = "cncr",
                    RegistryInstallPath = "HKCU\\Software\\CnCReloaded",
                    UIName = "C&C: 重制版",
                    Texture = AssetLoader.TextureFromImage(Resources.cncricon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-wg",
                    ClientExecutableName = "HKLYClient.exe",
                    GameBroadcastChannel = "#cncnet-wg-games",
                    InternalName = "wg",
                    RegistryInstallPath = "HKCU\\Software\\WonderfulGarden",
                    UIName = "欢快乐园",
                    Texture = AssetLoader.TextureFromImage(Resources.hklyicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-lis",
                    ClientExecutableName = "HKLYClient.exe",
                    GameBroadcastChannel = "#cncnet-lis-games",
                    InternalName = "lis",
                    RegistryInstallPath = "HKCU\\Software\\LightInSunset",
                    UIName = "落日之光",
                    Texture = AssetLoader.TextureFromImage(Resources.lisicon)
                }
            };

            // CnCNet chat + unsupported games.
            CnCNetGame[] otherGames = new CnCNetGame[]
            {
                new CnCNetGame()
                {
                    ChatChannel = "#cncnet",
                    InternalName = "cncnet",
                    UIName = "通用CnCNet聊天",
                    AlwaysEnabled = true,
                    Texture = AssetLoader.TextureFromImage(Resources.cncneticon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-td",
                    InternalName = "td",
                    UIName = "泰伯利亚黎明",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.tdicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ra",
                    InternalName = "ra",
                    UIName = "红色警戒",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.raicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-d2",
                    InternalName = "d2",
                    UIName = "沙丘2000",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.unknownicon)
                }
            };

            GameList.AddRange(defaultGames);
            GameList.AddRange(GetCustomGames(defaultGames.Concat(otherGames).ToList()));
            GameList.AddRange(otherGames);

            if (GetGameIndexFromInternalName(ClientConfiguration.Instance.LocalGame) == -1)
            {
                throw new ClientConfigurationException("在游戏集合中找不到匹配LocalGame值为" +
                    ClientConfiguration.Instance.LocalGame + "的游戏。");
            }
        }

        private List<CnCNetGame> GetCustomGames(List<CnCNetGame> existingGames)
        {
            IniFile iniFile = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameCollectionConfig.ini");

            List<CnCNetGame> customGames = new List<CnCNetGame>();

            var section = iniFile.GetSection("CustomGames");

            if (section == null)
                return customGames;

            HashSet<string> customGameIDs = new HashSet<string>();
            foreach (var kvp in section.Keys)
            {
                if (!iniFile.SectionExists(kvp.Value))
                    continue;

                string ID = iniFile.GetStringValue(kvp.Value, "InternalName", string.Empty).ToLower();

                if (string.IsNullOrEmpty(ID))
                    throw new GameCollectionConfigurationException("InternalName " + kvp.Value + " 未定义或为空。");

                if (ID.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
                {
                    throw new GameCollectionConfigurationException("InternalGame " + kvp.Value + " 的最大长度为" +
                        ProgramConstants.GAME_ID_MAX_LENGTH + "个字符。");
                }

                if (existingGames.Find(g => g.InternalName == ID) != null || customGameIDs.Contains(ID))
                    throw new GameCollectionConfigurationException("游戏和InternalName " + ID.ToUpper() + " 已经存在于游戏集合。");

                string iconFilename = iniFile.GetStringValue(kvp.Value, "IconFilename", ID + "icon.png");
                customGames.Add(new CnCNetGame
                {
                    InternalName = ID,
                    UIName = iniFile.GetStringValue(kvp.Value, "UIName", ID.ToUpper()),
                    ChatChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "ChatChannel"),
                    GameBroadcastChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "GameBroadcastChannel"),
                    ClientExecutableName = iniFile.GetStringValue(kvp.Value, "ClientExecutableName", string.Empty),
                    RegistryInstallPath = iniFile.GetStringValue(kvp.Value, "RegistryInstallPath", "HKCU\\Software\\"
                    + ID.ToUpper()),
                    Texture = AssetLoader.AssetExists(iconFilename) ? AssetLoader.LoadTexture(iconFilename) :
                    AssetLoader.TextureFromImage(Resources.unknownicon)
                });
                customGameIDs.Add(ID);
            }

            return customGames;
        }

        private string GetIRCChannelNameFromIniFile(IniFile iniFile, string section, string key)
        {
            string channel = iniFile.GetStringValue(section, key, string.Empty);

            if (string.IsNullOrEmpty(channel))
                throw new GameCollectionConfigurationException(section + "的" + key + "未定义或为空。");

            if (channel.Contains(' ') || channel.Contains(',') || channel.Contains((char)7))
                throw new GameCollectionConfigurationException(section + "的" + key + "含有IRC频道名称中不允许的字符。");

            if (!channel.StartsWith("#"))
                return "#" + channel;

            return channel;
        }

        /// <summary>
        /// 根据 CnCNet 支持的游戏的内部名称获取其索引。
        /// </summary>
        /// <param name="gameName">游戏的内部名称（InternalName）。</param>
        /// <returns>指定 CnCNet 中的游戏的索引。-1 为未知或不支持的游戏。</returns>
        public int GetGameIndexFromInternalName(string gameName)
        {
            for (int gId = 0; gId < GameList.Count; gId++)
            {
                CnCNetGame game = GameList[gId];

                if (gameName.ToLower() == game.InternalName)
                    return gId;
            }

            return -1;
        }

        /// <summary>
        /// Seeks the supported game list for a specific game's internal name and if found,
        /// returns the game's full name. Otherwise returns the internal name specified in the param.
        /// </summary>
        /// <param name="gameName">The internal name of the game to seek for.</param>
        /// <returns>The full name of a supported game based on its internal name.
        /// Returns the given parameter if the name isn't found in the supported game list.</returns>
        public string GetGameNameFromInternalName(string gameName)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameName.ToLower());

            if (game == null)
                return gameName;

            return game.UIName;
        }

        /// <summary>
        /// Returns the full UI name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The UI name of the game.</returns>
        public string GetFullGameNameFromIndex(int gameIndex)
        {
            return GameList[gameIndex].UIName;
        }

        /// <summary>
        /// Returns the internal name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The internal name (suffix) of the game.</returns>
        public string GetGameIdentifierFromIndex(int gameIndex)
        {
            return GameList[gameIndex].InternalName;
        }

        public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLower());
            if (game == null)
                return null;
            return game.GameBroadcastChannel;
        }

        public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
        {
            CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLower());
            if (game == null)
                return null;
            return game.ChatChannel;
        }
    }

    /// <summary>
    /// An exception that is thrown when configuration for a game to add to game collection
    /// contains invalid or unexpected settings / data or required settings / data are missing.
    /// </summary>
    class GameCollectionConfigurationException : Exception
    {
        public GameCollectionConfigurationException(string message) : base(message)
        {
        }
    }
}
