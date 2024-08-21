
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShellProgressBar;
#nullable disable

namespace IAG
{
    class Program
    {
        private static IConfiguration configuration;
        private static Args arg;
        private static object mutex = new();
        private static JArray res = new();
        private static List<string> questions = new();
        private static int count;
        private static Random rand = new();
        private static int index = 0;
        const string negativePrompt = 
            "Assume that you are an ai instruction following data generator." +
            "Come up with 6 misleading instructions with existing objects " +
            "but with wrong attributes in the image with different" +
            "styles. The instructions should contain interrogative sentences. " +
            "Please also explain the reason. Output format as follows json:" +
            "{Instruction:" +
            ",Reason:}";

        private const string positivePrompt = 
            "Assume that you are an ai instruction following data generator." +
            "Come up with 6 instructions for the image with different styles and accurate answers. " +
            "The generated instruction-answer pairs should be reasonable and instruction should not imply answer" +
            "The instructions should contain interrogative sentences" +
            "nothing should be answered such as explanation neither any comment. " +
            "The answers should be no less than 3 words." +
            "Output instruction-answer pair should as follows json format,put all of them in a single json" +
            "{Instruction: ,Answer:} * 6";

        private static string verifyPrompt =
            @"Suppose you are a smart teacher, after looking at the image information above, please" +
            "score each of the instruction-answer pairs(0-10) belows according to the following criteria:" +
            "1: whether the response directly follows the instruction." +
            "2: whether the response is accurate concerning the image content." +
            "3: how the instructions diversity are." +
            "{0}" +
            "output format should add Score to the original json above as follows, put all of them in a single json:" +
            "{{Instruction: , Answer: , Score: }}";
        private static string GLM4VFormat { get; } = @"{{
	""model"":""glm-4v"",
	""messages"":[
	  {{
		""role"": ""user"",
        ""content"": [
		  {{
			""type"": ""text"",
            ""text"": ""{0}""
		  }},
          {{
			""type"": ""image_url"",
            ""image_url"": {{
				""url"" : ""{1}""
			}}
		  }}
        ]
      }}
    ]
}}";
        
        static async Task Main(string[] args)
        {
            var rs = new RequestService();
            try
            {
                InitConfig(args);
                var info = await DataPreparation();
                var data = (info["images"] as JArray)!.Take(arg.ProcessNum);
                
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = arg.MaxThreadNum
                };
                var requestOption = new GetRequestOptions
                {
                    ApiFormat = GLM4VFormat,
                    ApiKey = arg.API_KEY!,
                    Url = arg.url!,
                    Content = positivePrompt
                }; 
                using var processBar = new ProgressBar(arg.ProcessNum, "processing data", ConsoleColor.White);
                var pLResult = Parallel.ForEach(data!, options, d =>
                {
                    var tuple = rs.GetRequestAsync(requestOption, d["coco_url"].ToString()).GetAwaiter().GetResult();
                    processBar.Tick();
                    var verifyOptions = new GetRequestOptions
                    {
                        ApiFormat = GLM4VFormat,
                        ApiKey = arg.API_KEY!,
                        Url = arg.url!,
                        Content = string.Format(verifyPrompt, tuple.Item2)
                            .Replace("\"","")
                            .Replace("\n","")
                    };

                    var verifyTuple = rs.GetRequestAsync(verifyOptions, d["coco_url"].ToString()).GetAwaiter().GetResult();
                    WriteAsJson(verifyTuple, p=>(int)p["Score"] > 7 );
                    
                });
                if (pLResult.IsCompleted)
                {
                    SaveResult();
                }
            }
            catch (InvalidDataException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        
        static void SaveResult()
        {
            if(!File.Exists(arg.GenerationPath))
                File.Create(arg.GenerationPath!).Close();
            using var writer = new StreamWriter(arg.GenerationPath!);
            writer.Write(JsonConvert.SerializeObject(res));
        }
        static void InitConfig(string[] args)
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("Properties/launchSettings.json", false, true)
                .AddCommandLine(args)
                .AddJsonFile("Properties/UserSettings.json", false, true)
                .Build();
            arg = configuration.Get<Args>() ?? throw new KeyNotFoundException("args not found");
            questions = configuration.GetSection("Questions").Get<List<string>>();
            count = questions.Count;
        }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           

        static async Task<JObject> DataPreparation()
        {
            using var reader = new StreamReader(arg.DataPath!);
          
            var cts = new CancellationTokenSource(5000);
            var token = cts.Token;
            using var progressBar = new ConsoleBar(50, "loading data");
            var barTask = progressBar.RunAsync(token);
            var data = await reader.ReadToEndAsync(token);
            await cts.CancelAsync();
            await barTask;
            return JsonConvert.DeserializeObject<JObject>(data);
        }
        static void WriteAsJson(Tuple<string, string> tuple, Func<JToken, bool> selectAction = null)
        {
            var (url, answer) = tuple;
            answer = answer
                .Replace("```json", "")
                .Replace("```", "");
            try
            {
                lock (mutex)
                {
                    var ans = JsonConvert.DeserializeObject(answer) as JArray;

                    ans = new JArray((ans ?? throw new Exception("failed to transfer string to json")).Where(p=>selectAction?.Invoke(p) != false));
                    var job = new JObject();
                    job.Add("Id", url);
                    job.Add("Response", ans);
                    res.Add(job);
                    index++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}