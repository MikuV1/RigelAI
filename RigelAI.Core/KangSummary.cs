using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RigelAI.Core
{
    public static class KangSummary
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string Model = "gemini-2.0-flash-lite";

        public static async Task<string> SummarizeAsync(List<object> historyToSummarize)
        {
            var apiKey = Environment.GetEnvironmentVariable("KANG_SUMMARY_API");
            if (string.IsNullOrEmpty(apiKey))
                return "❌ API key missing.";

            // Create summarization prompt
            var prompt = "Please provide a concise summary of the following conversation, focusing on the main ideas and important details and make sure to keep all the context:\n\n";

            // Build summarization payload
            var summarizationParts = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            };

            // Append conversation to be summarized
            summarizationParts.AddRange(historyToSummarize);

            var payload = new { contents = summarizationParts };
            var json = JsonConvert.SerializeObject(payload);
            var endpoint = $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent?key={apiKey}";

            try
            {
                var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Summary API error: {response.StatusCode}\nDetails: {responseString}";
                }

                dynamic result = JsonConvert.DeserializeObject(responseString);
                string summary = result?.candidates?[0]?.content?.parts?[0]?.text?.ToString();

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    Console.WriteLine($"[KangSummary] Generated summary with {summary.Length} characters");
                    return summary;
                }

                return "⚠️ Summary was empty.";
            }
            catch (Exception ex)
            {
                return $"❌ Error summarizing: {ex.Message}";
            }
        }
    }
}
