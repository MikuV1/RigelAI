﻿using System;
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
        private static readonly string Model = "gemini-2.5-flash";
        private static string apiKey = null;

        static GeminiClient()
        {
            try
            {
                // .env loading is now handled centrally
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("❌ GEMINI_API_KEY is missing in environment variables or .env");
                }
                else
                {
                    Console.WriteLine("✅ GEMINI_API_KEY loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load GEMINI_API_KEY: {ex.Message}");
            }
        }

        public static void SetHttpClient(HttpClient clientInstance)
        {
            client = clientInstance;
        }

        public static async Task<string> ChatAsync(string userMessage, List<object> conversationHistory)
        {
            return await ChatWithPartsAsync(AppendTextPart(conversationHistory, userMessage));
        }

        public static async Task<string> ChatWithPartsAsync(List<object> conversationHistory)
        {
            if (string.IsNullOrEmpty(apiKey))
                return "❌ API key missing.";

            var endpoint = $"https://generativelanguage.googleapis.com/v1/models/{Model}:generateContent?key={apiKey}";

            var payload = new { contents = conversationHistory };
            var json = JsonConvert.SerializeObject(payload);
            int inputTokenEstimate = EstimateTokenCount(json);
            await Task.Delay(100); // 100ms pause



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

                int replyTokenEstimate = EstimateTokenCount(botReply ?? "");

                Console.WriteLine($"[GeminiClient] Estimated input tokens: {inputTokenEstimate}");
                Console.WriteLine($"[GeminiClient] Estimated output tokens: {replyTokenEstimate}");

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

        private static List<object> AppendTextPart(List<object> history, string text)
        {
            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = text } }
            });
            return history;
        }

        private static int EstimateTokenCount(string text)
        {
            return (text.Length / 4) + 1;
        }
    }
}
