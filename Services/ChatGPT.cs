﻿using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using OpenAI_API;

namespace ALICEIDLE.Services
{
    public class ChatGPT
    {
        public static string tempDir = @$"{AppContext.BaseDirectory}tempfiles\";
        public static Dictionary<ulong, string> ongoingConvo = new Dictionary<ulong, string>();
        public static Dictionary<ulong, OpenAI_API.Chat.Conversation> userChatDict = new Dictionary<ulong, OpenAI_API.Chat.Conversation>();
        public static async Task<string> GetWhisperResponse(IAttachment file, bool isTranslation)
        {
            string filePath = $"{tempDir}{file.Filename}";
            Console.WriteLine(filePath);
            await DownloadFileAsync(file.Url, filePath);
            
            var openaiApiKey = Program._config["OpenAISecret"];
            var httpClient = new HttpClient();

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent("whisper-1"), "model");

            var fileStreamContent = new StreamContent(File.OpenRead(filePath));
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp3");

            formData.Add(fileStreamContent, "file", Path.GetFileName(filePath));
            formData.Add(new StringContent("response_format"), "verbose_json");

            HttpRequestMessage request = new HttpRequestMessage();

            if (isTranslation)
                request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/translations");
            else
                request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openaiApiKey);
            request.Content = formData;

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            fileStreamContent.Dispose();

            Console.WriteLine(responseContent);
            WhisperResponse wResponse = JsonConvert.DeserializeObject<WhisperResponse>(responseContent);
            Console.WriteLine(wResponse.text);

            DirectoryInfo directoryInfo = new DirectoryInfo(tempDir);
            FileInfo[] files = directoryInfo.GetFiles();

            foreach (FileInfo _file in files)
            {
                _file.Delete();
            }

            return wResponse.text;
        }
        public static async Task<string> ChatGPTQuery(string systemMessage, string userMessage, ulong id, bool noContext)
        {
            OpenAIAPI api = new OpenAIAPI(Program._config["OpenAISecret"]);
            OpenAI_API.Chat.Conversation chat = null;
            if (noContext)
            {
                chat = api.Chat.CreateConversation();
                chat.AppendSystemMessage(systemMessage);
            }
            else if (!userChatDict.ContainsKey(id))
                chat = api.Chat.CreateConversation();
            else
                chat = userChatDict[id];

            chat.AppendUserInput(userMessage);
            string response = await chat.GetResponseFromChatbot();
            
            return response;
        }
        public static async Task<string> GetChatGPTResponse(string query, ulong id, bool noContext)
        {
            if (!ongoingConvo.ContainsKey(id))
                ongoingConvo.Add(id, query);
            if (noContext)
                ongoingConvo[id] = $"USER: {query}\\n";
            else
                ongoingConvo[id] += $"USER: {query}\\n";
            //Console.WriteLine(JsonConvert.ToString(ongoingConvo[id]));

            string response = await QueryChatGPTApi(ongoingConvo[id]);
            ChatGPTRoot chatGPTResponse = JsonConvert.DeserializeObject<ChatGPTRoot>(response);
            ongoingConvo[id] += $"ChatGPT: {chatGPTResponse.choices.First().message.content}\\n";

            //Console.WriteLine(ongoingConvo[id]);
            string finalResonse = chatGPTResponse.choices.First().message.content.Replace("ChatGPT: ", "");

            return finalResonse;
        }
        public static async Task<string> QueryChatGPTApi(string query)
        {
            HttpClient client = new HttpClient();

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://api.openai.com/v1/chat/completions"),
                Method = HttpMethod.Post,
                Headers = {
                    { "Authorization", $"Bearer {Program._config["OpenAISecret"]}" },
                    },
                    Content = new StringContent($@"{{
                    ""model"": ""gpt-3.5-turbo"",
                    ""messages"": [{{""role"": ""user"", ""content"": ""{query}""}}]
                    }}", Encoding.UTF8, "application/json")
            };
            Console.WriteLine(query);
            // Send the request and process the response
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseContent);
            return responseContent;
        }
        
        public static async Task DownloadFileAsync(string url, string filePath)
        {
            Console.WriteLine("downloading");
            using var httpClient = new HttpClient();

            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var content = response.Content;
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await content.CopyToAsync(fileStream);

            content.Dispose();
            await fileStream.DisposeAsync();
        }
        
        public static async Task<List<EmbedBuilder>> SplitResponse(string gptResponse, int maxDescLength, string type)
        {
            
            int numEmbeds = (int)Math.Ceiling((double)gptResponse.Length / maxDescLength);

            List<EmbedBuilder> embedBuilders = new List<EmbedBuilder>();
            int currentIndex = 0;

            while (currentIndex < gptResponse.Length)
            {
                int length = Math.Min(maxDescLength, gptResponse.Length - currentIndex);

                if (currentIndex + length < gptResponse.Length)
                {
                    int lastSpaceIndex = gptResponse.LastIndexOf(' ', currentIndex + length, length);
                    if (lastSpaceIndex > currentIndex)
                    {
                        length = lastSpaceIndex - currentIndex;
                    }
                }

                string partialResponse = gptResponse.Substring(currentIndex, length);

                EmbedBuilder emb = new EmbedBuilder()
                    .WithDescription(partialResponse)
                    .WithColor(EmbedColors.successColor);

                switch (type)
                {
                    case "uwu":
                        emb.WithTitle($"Uwu Translation ({embedBuilders.Count + 1}/{numEmbeds})");
                        break;
                    case "tsundere":
                        break;
                    case "chat":
                        emb.WithTitle($"ChatGPT Response ({embedBuilders.Count + 1}/{numEmbeds})");
                        break;
                }
                embedBuilders.Add(emb);
                currentIndex += length;
            }

            return embedBuilders;
        }
    }
    public class WhisperResponse
    {
        public string text { get; set; }
    }
    
    public class Choice
    {
        public Message message { get; set; }
        public string finish_reason { get; set; }
        public int index { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class ChatGPTRoot
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

}
