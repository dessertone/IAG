
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IAG;

public class RequestService
{
    private Random rand = new();
    public async Task<Tuple<string,string>> GetRequestAsync(GetRequestOptions options, string url = null)
        {
            try
            {
                var count = options.Questions?.Count;
                var question = options.Questions?[rand.Next(0,count??0)];

                var format = string.Format(options.ApiFormat, options.Content, url);
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add ("Authorization", "Bearer " + options.ApiKey);
                httpClient.DefaultRequestHeaders.Add ("ContentType", "application/json");
	       
                HttpContent message = new StringContent (format);
                message.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue ("application/json");
                var response = await httpClient.PostAsync(options.Url, message);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var a = JsonConvert.DeserializeObject (json) as JObject;
                try
                {
                    var answer = a["choices"][0]["message"]["content"].ToString();
                    return new (url, answer);
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine(json);
                }
                
            }
            catch(HttpRequestException e)
            {
                Console.WriteLine(e.Message);
            }
            catch(Exception e)
            {
                Console.WriteLine (e);
            }
            return new ("Error","Error");
        }
}
public class GetRequestOptions
{
            
    public string ApiFormat { get; init; }
    public string ApiKey { get; init; }
    public string Url { get; init; }
    public string Content { get; init; }
    public List<string>? Questions { get; set; }
}