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
        private static readonly string Model = "gemini-2.0-flash-lite";
        private static readonly string Endpoint = $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent?key={ApiKey}";

        private static readonly List<object> conversationHistory = new List<object>();

        public static int ChatHistoryCount => conversationHistory.Count;

        public static async Task<bool> LoadPersonaAsync(string filePath = "persona.txt")
        {
            // This method should delegate to PersonaManager
            return await PersonaManager.LoadPersonaAsync(filePath);
        }

        public static string GetPersonaText()
        {
            return PersonaManager.GetPersonaText();
        }

        public static void ResetChat()
        {
            conversationHistory.Clear();
        }

        public static async Task<string> ChatAsync(string userMessage)
        {
            if (string.IsNullOrEmpty(ApiKey))
                return "❌ API key missing.";

            if (conversationHistory.Count == 0)
            {
                // Initialize conversation with persona text if available
                string persona = GetPersonaText();
                if (!string.IsNullOrWhiteSpace(persona))
                {
                    conversationHistory.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = persona } }
                    });
                }
            }

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
                var response = await client.PostAsync(Endpoint, content);
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
