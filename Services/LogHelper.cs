using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALICEIDLE.Logging
{
    public class LogHelper
    {
        public static List<Server> servers { get; set; } = new List<Server>();

        public static void AddServerMessage(SocketMessage msg)
        {
            var chnl = msg.Channel as SocketGuildChannel;
            Server targetServer = servers.FirstOrDefault(s => s.name == chnl.Guild.Name);

            if (targetServer == null)
            {
                targetServer = new Server()
                {
                    name = chnl.Guild.Name,
                    channels = new List<Channel>()
                };
                servers.Add(targetServer);
            }

            Channel targetChannel = targetServer.channels.FirstOrDefault(c => c.name == chnl.Name);

            if (targetChannel == null)
            {
                targetChannel = new Channel()
                {
                    name = chnl.Name,
                    messages = new List<Message>()
                };
                targetServer.channels.Add(targetChannel);
            }

            targetChannel.messages.Add(new Message()
            {
                author = msg.Author.Username,
                content = msg.Content,
                timestamp = msg.Timestamp
            });

            SaveDataToJson($"{Program.basePath}\\Server_Messages_{msg.Timestamp.LocalDateTime.ToString("MM-dd HH-mm")}.json");
        }

        public static void SaveDataToJson(string filePath)
        {
            var jsonData = JsonConvert.SerializeObject(servers, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);
        }
    }

    public class Server
    {
        public string name { get; set; }
        public List<Channel> channels { get; set; }
    }
    public class Channel
    {
        public string name { get; set; }
        public List<Message> messages { get; set; }
    }
    public class Message
    {
        public string author { get; set; }
        public string content { get; set; }
        public DateTimeOffset timestamp { get; set; }
    }
}