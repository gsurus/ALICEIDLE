using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ALICEIDLE.Gelbooru
{
    public class Gelbooru
    {
        private const string BaseUrl = "https://gelbooru.com/index.php";
        
        public Gelbooru()
        {
            
        }

        public static async Task<Root> SearchPosts(string tags, int limit = 100)
        {
            HttpClient httpClient = new HttpClient();
            
            var url = BuildSearchUrl(tags, limit);
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get posts. Status code: {response.StatusCode}");
            }
            Root booruPosts = JsonConvert.DeserializeObject<Root>(response.Content.ReadAsStringAsync().Result);
            return booruPosts;
        }

        private static string BuildSearchUrl(string tags, int limit)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["page"] = "dapi";
            queryString["s"] = "post";
            queryString["q"] = "index";
            queryString["json"] = "1";
            queryString["tags"] = tags;
            queryString["api_key"] = "0e54747fd8239defd6c7bcde63d9819b434119c6702391617c3bccd5967c894d";
            queryString["user_id"] = "245544";

            var urlBuilder = new UriBuilder(BaseUrl);
            urlBuilder.Query = queryString.ToString();
            Console.WriteLine(queryString.ToString());
            return urlBuilder.ToString();
        }
    }
    public class UserData
    {
        public List<User> Users { get; set; }
    }
    public class User
    {
        public string Username { get; set; }
        public Root Posts { get; set; }
        public int Page { get; set; }
    }
    public class Attributes
    {
        public int? limit { get; set; }
        public int? offset { get; set; }
        public int? count { get; set; }
    }

    public class Post
    {
        public int? id { get; set; }
        public string created_at { get; set; }
        public int? score { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public string md5 { get; set; }
        public string directory { get; set; }
        public string image { get; set; }
        public string rating { get; set; }
        public string source { get; set; }
        public int? change { get; set; }
        public string owner { get; set; }
        public int? creator_id { get; set; }
        public int? parent_id { get; set; }
        public int? sample { get; set; }
        public int? preview_height { get; set; }
        public int? preview_width { get; set; }
        public string tags { get; set; }
        public string title { get; set; }
        public string has_notes { get; set; }
        public string has_comments { get; set; }
        public string file_url { get; set; }
        public string preview_url { get; set; }
        public string sample_url { get; set; }
        public int? sample_height { get; set; }
        public int? sample_width { get; set; }
        public string status { get; set; }
        public int? post_locked { get; set; }
        public string has_children { get; set; }
    }

    public class Root
    {
        [JsonProperty("@attributes")]
        public Attributes attributes { get; set; }
        public List<Post> post { get; set; }
    }

}
