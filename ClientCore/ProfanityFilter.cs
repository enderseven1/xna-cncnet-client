using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClientCore
{
    public class ProfanityFilter
    {
        public IList<string> CensoredWords { get; private set; }
        public IList<string> CensoredWords2 { get; private set; }

        /// <summary>
        /// Creates a new profanity filter with a default set of censored words.
        /// </summary>
        public ProfanityFilter()
        {
            CensoredWords = new List<string>()
            {
                "cunt*",
                "*nigg*",
                "paki*",
                "shit",
                "fuck*",
                "admin*",
                "allahu*",
                "akbar",
                "twat",
                "cock",
                "pussy",
                "hitler*",
                "anal",
                "死全家",
                "死妈",
                "我靠",
                "乳房",
                "傻逼",
                "肏",
                "[草操][您]",
                "我[草操]",
                "[日]?你妈",
                "尼玛",
                "微信",
                "wx",
                "吃屎",
                "丢雷",
                "閪",
                "操蛋",
                "妈蛋",
                "屁事",
                "屌",
                "妈卖批",
                "狗日",
                "狗叫",
                "他妈的",
                "tmd",
                "毛泽东",
                "习近平",
                "毛主席",
                "习主席",
                "梁如萱",
                "国民党",
                "共产党",
                "民进党",
                "政府",
                "蔡英文",
                "陈独秀",
                "中共",
                "阴茎",
                "鸡巴",
                "阴道",
                "damn",
                "nnd",
                "cnm",
                "nmsl",
                "lrx",
                "丢雷楼某",
                "老母亲",
                "老母",
                "Liang Ruxuan",
                "贱逼",
                "贱种",
                "卖淫",
                "骚货",
                "骚逼",
                "屄",
                "尻",
                "贱货",
                "贱人",
                "qq",
                "QQ",
                "[你您我草操干凎淦]TM",
                "[你您我草操干凎淦][她他它][丫妈]",
                "日你",
                "我日",
                "小兔崽子",
                "逆天",
                "[您你个]大坝",
                "[0-9]{7,}",
                "wxid_[a-zA-Z0-9]*",
                "透透",
                "小穴",
                "屁眼[子]?",
                "皮燕[子]?",
                "杀",
                "死",
                "[凎淦]",
                "干[您你我他她它]",
                "[啊阿]?.?[西夕][巴八]",
                "爹",
                "爸",
                "[您你我他她它]?大爷",
                "[草操曹][你尼泥][马妈玛]",
                "狗叫",
                "屎"
            };
            CensoredWords2 = new List<string>()
            {
                "shit",
                "fuck",
                "damn",
                "nnd",
                "cnm",
                "nmsl",
                "死全家",
                "死妈",
                "我靠",
                "乳房",
                "傻逼",
                "肏",
                "操你",
                "草你",
                "我草",
                "我操",
                "你妈",
                "尼玛",
                "微信",
                "wx",
                "吃屎",
                "丢雷",
                "閪",
                "操蛋",
                "妈蛋",
                "屁事",
                "屌",
                "妈卖批",
                "狗日",
                "狗叫",
                "他妈的",
                "tmd",
                "毛泽东",
                "习近平",
                "毛主席",
                "习主席",
                "梁如萱",
                "国民党",
                "共产党",
                "民进党",
                "政府",
                "蔡英文",
                "陈独秀",
                "中共",
                "阴茎",
                "鸡巴",
                "阴道",
                "JB",
                "dick",
                "DICK",
                "Dick",
                "Fuck",
                "FUCK",
                "NMSL",
                "TMD",
                "CNM",
                "NND",
                "WX",
                "Wx",
                "丢雷楼某",
                "老母亲",
                "老母",
                "贱逼",
                "贱种",
                "卖淫",
                "骚货",
                "骚逼",
                "屄",
                "尻",
                "贱货",
                "贱人",
                "qq",
                "QQ",
                "踏马",
                "你他妈",
                "我他妈",
                "你TM",
                "我TM",
                "日你",
                "我日",
                "小兔崽子",
                "逆天",
                "你大坝",
                "个大坝",
                "透透",
                "小穴",
                "屁眼子",
                "皮燕子",
                "屁眼",
                "皮燕",
                "杀",
                "死",
                "凎",
                "淦",
                "SB",
                "sb",
                "sB",
                "Sb",
                "干你",
                "干我",
                "干他",
                "干她",
                "干它",
                "西巴",
                "西八",
                "夕八",
                "夕巴",
                "爹",
                "爸",
                "你大爷",
                "草泥马",
                "曹尼玛",
                "狗叫",
                "屎"
            };
        }

        public ProfanityFilter(IEnumerable<string> censoredWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException("censoredWords");
            CensoredWords = new List<string>(censoredWords);
        }

        public bool IsOffensive(string text)
        {
            string censoredText = text;
            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);
                censoredText = Regex.Replace(censoredText, regularExpression, "",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                if(string.IsNullOrEmpty(censoredText))
                    return true;
            }
            return false;
        }
        public string CrabsInRiver(string text)
        {
            if (ClientConfiguration.Instance.CrabsInRivers){
                string CIRedText = text;
                foreach (string censoredWord in CensoredWords2)
                {
                    if (text.Replace(censoredWord, new string('*', censoredWord.Length)) != text)
                    {
                        CIRedText = text.Replace(censoredWord, new string('*', censoredWord.Length));
                    }
                }
                return CIRedText;
            }
            else
            { 
                return text; 
            }
        }

        public string CensorText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            string censoredText = text;
            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);
                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return censoredText;
        }

        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;
            return new string('*', word.Length);
        }

        private string ToRegexPattern(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);
            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");
            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern;
            }
            regexPattern = @"\b" + regexPattern + @"\b";
            return regexPattern;
        }
    }
}
