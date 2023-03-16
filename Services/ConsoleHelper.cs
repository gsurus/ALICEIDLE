using ALICEIDLE;
using Colorful;
using Discord.WebSocket;
using System.Drawing;
using cConsole = Colorful.Console;
using Formatter = Colorful.Formatter;
using Console = System.Console;
using sysColor = System.Drawing.Color;
using ALICEIDLE.Services;
namespace aliceidle
{
    public class ConsoleHelper
    {   //green
        public static sysColor success = sysColor.FromArgb(25, 217, 0);
        //yellow
        public static sysColor warning = sysColor.FromArgb(255, 255, 83);
        //red
        public static sysColor error = sysColor.FromArgb(157, 47, 36);
        //blue
        public static sysColor info = sysColor.FromArgb(0, 109, 242);
        //orange
        public static sysColor status = sysColor.FromArgb(255, 168, 23);
        //light blue
        public static sysColor debug = sysColor.FromArgb(10, 163, 240);

        
        public static Dictionary<string, sysColor> colorKeywords = new Dictionary<string, sysColor>
        {
            //{ "Discord", success },
            //{ "Gateway", success },
            { "debug", debug },
            { "adding", info },
            { "Connected", success },
            { "Connecting", status },
            { "Ready", success },
            { "Discord.Net", success },
        };
        
        public static void WriteLineMixedColors(string message, Dictionary<string, Color> keywordColors, Color defaultColor = default)
        {
            if (message == null || keywordColors == null || keywordColors.Count == 0)
            {
                cConsole.WriteLine(message, defaultColor);
                return;
            }

            var words = message.Split(' ');

            foreach (string word in words)
            {
                string wordWithoutPunctuation = word.TrimEnd(',', '.', ':', ';', '!', '?');

                if (keywordColors.TryGetValue(wordWithoutPunctuation, out Color color))
                {
                    cConsole.Write($"{word} ", color);
                }
                else
                {
                    cConsole.Write($"{word} ", defaultColor);
                }
            }

            cConsole.WriteLine();
        }
        public static void WriteConnectedAsString()
        {
            Formatter[] fmt = new Formatter[]
            {
                new Formatter("Connected", success),
                new Formatter("ALICE", info)
            };
            
            cConsole.WriteLineFormatted(timeString() + " Gateway     {0} as -> [{1}]", sysColor.White, fmt);
        }
        public static void WriteLogMessage(SocketMessage msg)
        {
            var chnl = msg.Channel as SocketGuildChannel;
            var _msg = msg as SocketUserMessage;

            Formatter[] msgForm = new Formatter[]
            {
                new Formatter(msg.Author.Username, debug),
                new Formatter($"[{chnl.Guild.Name}]", info)
            };

            cConsole.WriteLineFormatted(timeString() + " {1} - {0} | " + _msg.Content, sysColor.White, msgForm);
        }

        public static async Task<StyleSheet> GetSysLogStyleSheet()
        {
            StyleSheet styleSheet = new StyleSheet(sysColor.White);

            foreach (var keyword in colorKeywords)
            {
                styleSheet.AddStyle(keyword.Key, keyword.Value);
            }

            return styleSheet;
        }

        public static string timeString()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }
    }
}
