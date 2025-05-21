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
        private static readonly HttpClient client = new HttpClient();
        private static readonly string ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        private static readonly string Model = "gemini-2.0-pro";
        private static readonly string Endpoint = $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent?key={ApiKey}";

        private static readonly List<object> chatHistory = new List<object>();

        public static async Task<string> ChatAsync(string userMessage)
        {
            if (string.IsNullOrEmpty(ApiKey))
                return "❌ API key missing.";

            // Add user message to chat history
            chatHistory.Add(new
            {
                author = "user",
                content = new { text = userMessage }
            });

            var payload = new
            {
                prompt = new
                {
                    messages = chatHistory
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(Endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Gemini API error: {response.StatusCode}\nDetails: {responseString}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseString);
                string botReply = result?.candidates?[0]?.content?.text?.ToString();

                // Add bot reply to chat history
                chatHistory.Add(new
                {
                    author = "assistant",
                    content = new { text = botReply }
                });

                return botReply ?? "⚠️ Empty response.";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }

        public static void ResetChat()
        {
            chatHistory.Clear();
        }
    }
}
