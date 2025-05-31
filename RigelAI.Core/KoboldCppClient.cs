using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigelAI.Core
{
    public static class KoboldCppClient
    {
        private static HttpClient client = new HttpClient();
        private static readonly string Endpoint = "http://localhost:5001/v1/chat/completions";

        public static void SetHttpClient(HttpClient customClient)
        {
            client = customClient;
        }

        public static async Task<string> ChatAsync(string userMessage, List<object> conversationHistory)
        {
            if (conversationHistory == null)
                throw new ArgumentNullException(nameof(conversationHistory));

            // Append user message to the conversation
            conversationHistory.Add(new
            {
                role = "user",
                content = userMessage
            });

            var payload = new
            {
                model = "koboldcpp",
                messages = conversationHistory,
                temperature = 0.7,
                max_tokens = 256,
                stream = false
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(Endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ KoboldCpp API error: {response.StatusCode}\nDetails: {responseString}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseString);
                string reply = result?.choices?[0]?.message?.content?.ToString();

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    conversationHistory.Add(new
                    {
                        role = "assistant",
                        content = reply
                    });

                    return reply;
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
