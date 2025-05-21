using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigelAI.Core
{
    public static class GeminiClient
    {
        private static HttpClient client = new HttpClient();
        private static readonly string Model = "gemini-2.0-flash-lite";

        public static void SetHttpClient(HttpClient clientInstance)
        {
            client = clientInstance;
        }

        public static async Task<string> ChatAsync(string userMessage, List<object> conversationHistory)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                return "❌ API key missing.";

            var endpoint = $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent?key={apiKey}";

            // Add user message to conversation history
            conversationHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var payload = new
            {
                contents = conversationHistory
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Gemini API error: {response.StatusCode}\nDetails: {responseString}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseString);
                string botReply = result?.candidates?[0]?.content?.parts?[0]?.text?.ToString();

                if (!string.IsNullOrWhiteSpace(botReply))
                {
                    conversationHistory.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = botReply } }
                    });
                    return botReply;
                }
                else
                {
                    return "⚠️ Empty response.";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }
    }
}
