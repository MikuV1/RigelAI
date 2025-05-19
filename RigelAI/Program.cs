using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigelAI
{
    class Program
    {
        private static readonly string ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private static readonly string ModelName = "gemini-2.0-flash";
        private static readonly string GeminiEndpoint = $"https://generativelanguage.googleapis.com/v1/models/{ModelName}:generateContent?key={ApiKey}";

        // Static HttpClient shared for all requests
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                Console.WriteLine("❌ API key kagak ada. Tolong set GEMINI_API_KEY environment variable nya yaaa");
                return;
            }

            Console.WriteLine("Selamat datang dan selamat menderita");

            while (true)
            {
                Console.Write("You: ");
                string userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userInput)) continue;
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string response = await GetGeminiResponse(userInput);
                Console.WriteLine("Rigel: " + response);
            }
        }

        static async Task<string> GetGeminiResponse(string userInput)
        {
            try
            {
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = userInput }
                            }
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Use the static HttpClient instance here
                var response = await httpClient.PostAsync(GeminiEndpoint, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gemini API ada yang salah: " + response.StatusCode);
                    Console.WriteLine("Liat sini: " + jsonResponse);
                    return "Maaf ehh gak ngerti";
                }

                dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                return result.candidates[0].content.parts[0].text.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return "Ada yang salah, tolong di debug sebelum stress";
            }
        }
    }
}
