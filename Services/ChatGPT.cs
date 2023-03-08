using Discord;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ALICEIDLE.Services
{
    public class ChatGPT
    {
        public static string tempDir = @$"{AppContext.BaseDirectory}tempfiles\";
        public static Dictionary<ulong, string> ongoingConvo = new Dictionary<ulong, string>();
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
        public static async Task<string> GetChatGPTResponse(string query, ulong id)
        {
            if (!ongoingConvo.ContainsKey(id))
                ongoingConvo.Add(id, query);

            ongoingConvo[id] += $"USER: {query}\\n";
            Console.WriteLine(ongoingConvo[id]);
            string response = await QueryChatGPTApi(ongoingConvo[id]);
            ChatGPTRoot chatGPTResponse = JsonConvert.DeserializeObject<ChatGPTRoot>(response);
            ongoingConvo[id] += $"ChatGPT: {chatGPTResponse.choices.First().message.content}\\n";
            Console.WriteLine(ongoingConvo[id]);
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
