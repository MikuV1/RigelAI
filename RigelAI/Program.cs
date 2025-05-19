using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigelAI
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private static readonly string model = "gemini-2.0-flash";
        private static readonly string endpoint = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={apiKey}";

        // Chat history for context
        private static readonly List<object> conversationHistory = new List<object>();

        static async Task Main(string[] args)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("❌ API key not found. Please set the GEMINI_API_KEY environment variable.");
                return;
            }

            Console.WriteLine("Welcome to Rigel AI (Gemini Mode)");
            Console.WriteLine("Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // Add user's message to history
                conversationHistory.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userInput } }
                });

                string reply = await GetGeminiReply();
                Console.WriteLine("Rigel: " + reply);
            }
        }

        static async Task<string> GetGeminiReply()
        {
            try
            {
                var payload = new
                {
                    contents = conversationHistory
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gemini API error: " + response.StatusCode);
                    Console.WriteLine("Details: " + jsonResponse);
                    return "⚠️ Unable to get a response.";
                }

                dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                string reply = result?.candidates[0]?.content?.parts[0]?.text?.ToString();

                // Add Gemini's response to history
                conversationHistory.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });

                return reply ?? "⚠️ Empty response.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return "❌ An error occurred while processing.";
            }
        }
    }
}
